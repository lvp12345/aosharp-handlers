using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CombatHandler.Engineer
{
    class EngiCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static int EngiBioCocoonPercentage;

        private const float DelayBetweenTrims = 1;
        private const float DelayBetweenDiverTrims = 305;

        private bool attackPetTrimmedAggressive = false;

        private Dictionary<PetType, bool> petTrimmedAggDef = new Dictionary<PetType, bool>();
        private Dictionary<PetType, bool> petTrimmedHpDiv = new Dictionary<PetType, bool>();
        private Dictionary<PetType, bool> petTrimmedOffDiv = new Dictionary<PetType, bool>();

        private Dictionary<PetType, double> _lastPetTrimDivertOffTime = new Dictionary<PetType, double>()
        {
            { PetType.Attack, 0 },
            { PetType.Support, 0 }
        };
        private Dictionary<PetType, double> _lastPetTrimDivertHpTime = new Dictionary<PetType, double>()
        {
            { PetType.Attack, 0 },
            { PetType.Support, 0 }
        };

        private static Window _petWindow;
        private static Window _buffWindow;
        private static Window _procWindow;

        private static View _buffView;
        private static View _petView;
        private static View _procView;

        private double _lastTrimTime = 0;

        private static double _ncuUpdateTime;

        public EngiCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            Config.CharSettings[Game.ClientInst].EngiBioCocoonPercentageChangedEvent += EngiBioCocoonPercentage_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            Game.TeleportEnded += OnZoned;

            _settings.AddVariable("SyncPets", true);
            _settings.AddVariable("SpawnPets", true);
            _settings.AddVariable("BuffPets", true);
            _settings.AddVariable("HealPets", false);
            _settings.AddVariable("WarpPets", false);

            _settings.AddVariable("DivertHpTrimmer", true);
            _settings.AddVariable("DivertOffTrimmer", true);
            _settings.AddVariable("TauntTrimmer", true);
            _settings.AddVariable("AggDefTrimmer", true);

            _settings.AddVariable("InitBuffSelection", (int)InitBuffSelection.Self);

            _settings.AddVariable("BuffingAuraSelection", (int)BuffingAuraSelection.Damage);
            _settings.AddVariable("DebuffingAuraSelection", (int)DebuffingAuraSelection.Blind);

            _settings.AddVariable("PetPerkSelection", (int)PetPerkSelection.ChaoticBox);
            _settings.AddVariable("PetProcSelection", (int)PetProcSelection.None);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.ReactiveArmor);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.AssaultForceRelief);

            _settings.AddVariable("LegShot", false);

            RegisterSettingsWindow("Engi Handler", "EngineerSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcEngineerReactiveArmor, ReactiveArmor, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEngineerDestructiveTheorem, DestructiveTheorem, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEngineerEnergyTransfer, EnergyTransfer, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEngineerEndureBarrage, EndureBarrage, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEngineerDestructiveSignal, DestructiveSignal, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEngineerSplinterPreservation, SplinterPreservation, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEngineerCushionBlows, CushionBlows, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcEngineerAssaultForceRelief, AssaultForceRelief, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEngineerDroneMissiles, DroneMissiles, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEngineerDroneExplosives, DroneExplosives, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEngineerCongenialEncasement, CongenialEncasement, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEngineerPersonalProtection, PersonalProtection, CombatActionPriority.Low);

            //Perks
            RegisterPerkProcessor(PerkHash.LegShot, LegShot);
            RegisterPerkProcessor(PerkHash.BioCocoon, BioCocoon);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), Pistol);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.GrenadeBuffs).OrderByStackingOrder(), Grenade);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadowlandReflectBase).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SpecialAttackAbsorberBase).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerSpecialAttackAbsorber).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(RelevantNanos.PetWarp, PetWarp, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.BoostedTendons, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.DamageBuffLineA, GenericTeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(), GenericTeamBuff);
            RegisterSpellProcessor(RelevantNanos.Blinds, BlindAura);
            RegisterSpellProcessor(RelevantNanos.ShieldRippers, ShieldRipperAura);
            RegisterSpellProcessor(RelevantNanos.ArmorAura, ArmorAura);
            RegisterSpellProcessor(RelevantNanos.DamageAura, DamageAura);
            RegisterSpellProcessor(RelevantNanos.ReflectAura, ReflectAura);
            RegisterSpellProcessor(RelevantNanos.ShieldAura, ShieldAura);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerPetAOESnareBuff).OrderByStackingOrder(), SnareAura);
            //RegisterSpellProcessor(RelevantNanos.IntrusiveAuraCancellation, AuraCancellation);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);

            //Pet Spawners
            RegisterSpellProcessor(PetsList.Pets.Where(c => c.Value.PetType == PetType.Attack).Select(c => c.Key).ToArray(), PetSpawner);
            RegisterSpellProcessor(PetsList.Pets.Where(c => c.Value.PetType == PetType.Support).Select(c => c.Key).ToArray(), PetSpawner);

            RegisterSpellProcessor(RelevantNanos.PetCleanse, PetCleanse);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerMiniaturization).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetShortTermDamageBuffs).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDefensiveNanos).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPPetInitiativeBuffs).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerMiniaturization).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(RelevantNanos.DamageBuffLineA, PetTargetBuff);

            RegisterSpellProcessor(RelevantNanos.PetHealing, PetHealing);
            RegisterSpellProcessor(RelevantNanos.PetHealingGreater, PetHealingGreater);

            RegisterSpellProcessor(RelevantNanos.ShieldOfObedientServant, ShieldOfTheObedientServant);
            RegisterSpellProcessor(RelevantNanos.MastersBidding, MastersBidding);
            RegisterSpellProcessor(RelevantNanos.SedativeInjectors, SedativeInjectors);

            RegisterPerkProcessor(PerkHash.ChaoticEnergy, ChaoticBox);
            RegisterPerkProcessor(PerkHash.SiphonBox, SiphonBox);
            RegisterPerkProcessor(PerkHash.TauntBox, TauntBox);

            ResetTrimmers();
            RegisterItemProcessor(RelevantTrimmers.PositiveAggressiveDefensive, RelevantTrimmers.PositiveAggressiveDefensive, PetAggDefTrimmer);

            RegisterItemProcessor(RelevantTrimmers.IncreaseAggressivenessLow, RelevantTrimmers.IncreaseAggressivenessLow, PetAggressiveTrimmer);
            RegisterItemProcessor(RelevantTrimmers.IncreaseAggressivenessHigh, RelevantTrimmers.IncreaseAggressivenessHigh, PetAggressiveTrimmer);

            RegisterItemProcessor(RelevantTrimmers.DivertEnergyToOffenseLow, RelevantTrimmers.DivertEnergyToOffenseLow, PetDivertOffTrimmer);
            RegisterItemProcessor(RelevantTrimmers.DivertEnergyToOffenseHigh, RelevantTrimmers.DivertEnergyToOffenseHigh, PetDivertOffTrimmer);

            RegisterItemProcessor(RelevantTrimmers.DivertEnergyToHitpointsLow, RelevantTrimmers.DivertEnergyToHitpointsLow, PetDivertHpTrimmer);
            RegisterItemProcessor(RelevantTrimmers.DivertEnergyToHitpointsHigh, RelevantTrimmers.DivertEnergyToHitpointsHigh, PetDivertHpTrimmer);

            PluginDirectory = pluginDir;

            EngiBioCocoonPercentage = Config.CharSettings[Game.ClientInst].EngiBioCocoonPercentage;
        }

        public Window[] _windows => new Window[] { _petWindow, _buffWindow, _procWindow };

        #region Callbacks

        public static void OnRemainingNCUMessage(int sender, IPCMessage msg)
        {
            try
            {
                if (Game.IsZoning)
                    return;

                RemainingNCUMessage ncuMessage = (RemainingNCUMessage)msg;
                SettingsController.RemainingNCU[ncuMessage.Character] = ncuMessage.RemainingNCU;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e);
            }
        }

        #endregion

        #region Handles
        private void HandlePetViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_petView)) { return; }

                _petView = View.CreateFromXml(PluginDirectory + "\\UI\\EngineerPetsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Pets", XmlViewName = "EngineerPetsView" }, _petView);
            }
            else if (_petWindow == null || (_petWindow != null && !_petWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_petWindow, PluginDir, new WindowOptions() { Name = "Pets", XmlViewName = "EngineerPetsView" }, _petView, out var container);
                _petWindow = container;
            }
        }

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_buffView)) { return; }

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\EngineerBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "EngineerBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "EngineerBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }
        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\EngineerProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "EngineerProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "EngineerProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }
        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            if (Time.NormalTime > _ncuUpdateTime + 0.5f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }

            if (IsSettingEnabled("SyncPets"))
                SynchronizePetCombatStateWithOwner();

            //var window = SettingsController.FindValidWindow(_windows);

            //if (window != null && window.IsValid)
            //{
            //    window.FindView("EngiBioCocoonPercentageBox", out TextInputView bioCocoonInput);

            //    if (bioCocoonInput != null && !string.IsNullOrEmpty(bioCocoonInput.Text))
            //    {
            //        if (int.TryParse(bioCocoonInput.Text, out int bioCocoonValue))
            //        {
            //            if (Config.CharSettings[Game.ClientInst].EngiBioCocoonPercentage != bioCocoonValue)
            //            {
            //                Config.CharSettings[Game.ClientInst].EngiBioCocoonPercentage = bioCocoonValue;
            //                EngiBioCocoonPercentage = bioCocoonValue;
            //                Config.Save();
            //            }
            //        }
            //    }
            //}

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("EngiBioCocoonPercentageBox", out TextInputView bioCocoonInput);

                if (bioCocoonInput != null && !string.IsNullOrEmpty(bioCocoonInput.Text))
                    if (int.TryParse(bioCocoonInput.Text, out int bioCocoonValue))
                        if (Config.CharSettings[Game.ClientInst].EngiBioCocoonPercentage != bioCocoonValue)
                            Config.CharSettings[Game.ClientInst].EngiBioCocoonPercentage = bioCocoonValue;

                if (SettingsController.settingsWindow.FindView("PetsView", out Button petView))
                {
                    petView.Tag = SettingsController.settingsWindow;
                    petView.Clicked = HandlePetViewClick;
                }

                if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                {
                    buffView.Tag = SettingsController.settingsWindow;
                    buffView.Clicked = HandleBuffViewClick;
                }

                if (SettingsController.settingsWindow.FindView("ProcsView", out Button procView))
                {
                    procView.Tag = SettingsController.settingsWindow;
                    procView.Clicked = HandleProcViewClick;
                }

                CancelBuffs();
            }

            CancelHostileAuras(RelevantNanos.Blinds);
            CancelHostileAuras(RelevantNanos.ShieldRippers);
        }

        #region LE Procs

        private bool CushionBlows(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.CushionBlows != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DestructiveSignal(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.DestructiveSignal != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DestructiveTheorem(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.DestructiveTheorem != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool EndureBarrage(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.EndureBarrage != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool EnergyTransfer(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.EnergyTransfer != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool ReactiveArmor(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.ReactiveArmor != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool SplinterPreservation(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.SplinterPreservation != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }


        private bool AssaultForceRelief(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.AssaultForceRelief != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool CongenialEncasement(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.CongenialEncasement != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DroneExplosives(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.DroneExplosives != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DroneMissiles(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.DroneMissiles != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool PersonalProtection(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.PersonalProtection != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Perks

        private bool BioCocoon(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.HealthPercent > EngiBioCocoonPercentage) { return false; }

            return CyclePerks(perk, fightingTarget, ref actionTarget);
        }

        private bool LegShot(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("LegShot")) { return false; }

            return CyclePerks(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Buffs

        #region Auras
        private bool ArmorAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.Armor != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool DamageAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.Damage != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool ReflectAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.Reflect != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool ShieldAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.Shield != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool SnareAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (DebuffingAuraSelection.PetSnare != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()
                || !SnareMobExists()) { return false; }

            return PetTargetBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool ShieldRipperAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DebuffingAuraSelection.ShieldRipper != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()) { return false; }

            return DynelManager.NPCs.Any(c => c.Health > 0
                && c.FightingTarget?.Buffs.Contains(202732) == false && c.FightingTarget?.Buffs.Contains(214879) == false
                && c.FightingTarget?.Buffs.Contains(284620) == false && c.FightingTarget?.Buffs.Contains(216382) == false
                && c.FightingTarget?.IsPet == false
                && c.Position.DistanceFrom(DynelManager.LocalPlayer.Position) <= 9f);
        }

        private bool BlindAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (DebuffingAuraSelection.Blind != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()) { return false; }

            return DynelManager.NPCs.Any(c => c.Health > 0
                && c.FightingTarget?.Buffs.Contains(202732) == false && c.FightingTarget?.Buffs.Contains(214879) == false
                && c.FightingTarget?.Buffs.Contains(284620) == false && c.FightingTarget?.Buffs.Contains(216382) == false
                && c.FightingTarget?.IsPet == false
                && c.Position.DistanceFrom(DynelManager.LocalPlayer.Position) <= 9f);
        }
        #endregion


        protected bool Grenade(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam)
                return TeamBuffExclusionWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade)
                        || TeamBuffExclusionWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);

            if (DynelManager.LocalPlayer.Buffs.Contains(269482)) { return false; }

            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade)
                    || BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        protected bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (InitBuffSelection.Team == (InitBuffSelection)_settings["InitBuffSelection"].AsInt32())
            {
                if (Team.IsInTeam)
                {
                    SimpleChar target = DynelManager.Players
                        .Where(c => c.IsInLineOfSight
                            && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                            && c.Health > 0
                            && c.Profession != Profession.Doctor && c.Profession != Profession.NanoTechnician
                            && GetWieldedWeapons(c).HasFlag(CharacterWieldedWeapon.Ranged)
                            && SpellChecksOther(spell, spell.Nanoline, c))
                        .FirstOrDefault();

                    if (target != null)
                    {
                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = target;
                        return true;
                    }
                }
            }

            if (InitBuffSelection.None == (InitBuffSelection)_settings["InitBuffSelection"].AsInt32()
                || !GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(CharacterWieldedWeapon.Ranged)) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Pets

        #region Warp

        protected bool PetWarp(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("WarpPets") || !CanCast(spell) || !CanLookupPetsAfterZone()) { return false; }

            return DynelManager.LocalPlayer.Pets.Any(c => c.Character == null);
        }

        #endregion

        #region Healing

        private bool PetHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("HealPets") || !CanLookupPetsAfterZone()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (pet.Character.HealthPercent <= 90)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        private bool PetHealingGreater(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("HealPets") || !CanLookupPetsAfterZone()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (pet.Character.HealthPercent <= 90)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Trimmers

        protected bool PetDivertHpTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("DivertHpTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (pet.Type == PetType.Support
                        && CanDivertHpTrim(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    petTrimmedHpDiv[PetType.Support] = true;
                    _lastPetTrimDivertHpTime[PetType.Support] = Time.NormalTime;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }
            }

            return false;
        }

        protected bool PetDivertOffTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("DivertOffTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

            if (IsSettingEnabled("DivertHpTrimmer"))
            {
                foreach (Pet pet in DynelManager.LocalPlayer.Pets)
                {
                    if (pet.Character == null) continue;

                    if (pet.Type == PetType.Attack
                            && CanDivertOffTrim(pet))
                    {
                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = pet.Character;
                        petTrimmedOffDiv[PetType.Attack] = true;
                        _lastPetTrimDivertOffTime[PetType.Attack] = Time.NormalTime;
                        _lastTrimTime = Time.NormalTime;
                        return true;
                    }
                }
            }
            else
            {
                foreach (Pet pet in DynelManager.LocalPlayer.Pets)
                {
                    if (pet.Character == null) continue;

                    if (pet.Type == PetType.Attack
                            && CanDivertOffTrim(pet))
                    {
                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = pet.Character;
                        petTrimmedOffDiv[PetType.Attack] = true;
                        _lastPetTrimDivertOffTime[PetType.Attack] = Time.NormalTime;
                        _lastTrimTime = Time.NormalTime;
                        return true;
                    }

                    if (pet.Type == PetType.Support
                            && CanDivertOffTrim(pet))
                    {
                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = pet.Character;
                        petTrimmedOffDiv[PetType.Support] = true;
                        _lastPetTrimDivertOffTime[PetType.Support] = Time.NormalTime;
                        _lastTrimTime = Time.NormalTime;
                        return true;
                    }
                }
            }

            return false;
        }

        protected bool PetAggDefTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AggDefTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (pet.Type == PetType.Attack
                        && CanAggDefTrim(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    petTrimmedAggDef[PetType.Attack] = true;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }

                if (pet.Type == PetType.Support
                    && CanAggDefTrim(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    petTrimmedAggDef[PetType.Support] = true;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }
            }

            return false;
        }

        protected bool PetAggressiveTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("TauntTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (pet.Type == PetType.Attack
                        && CanTauntTrim(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    attackPetTrimmedAggressive = true;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Perks

        private bool SiphonBox(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PetPerkSelection.SiphonBox != (PetPerkSelection)_settings["PetPerkSelection"].AsInt32()
                || !CanLookupPetsAfterZone() || actionTarget.Target != null) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (!pet.Character.Buffs.Contains(RelevantNanos.PerkSiphonBox))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        private bool ChaoticBox(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PetPerkSelection.ChaoticBox != (PetPerkSelection)_settings["PetPerkSelection"].AsInt32()
                || !CanLookupPetsAfterZone() || actionTarget.Target != null) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (!pet.Character.Buffs.Contains(RelevantNanos.PerkChaoticBox))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        private bool TauntBox(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PetPerkSelection.TauntBox != (PetPerkSelection)_settings["PetPerkSelection"].AsInt32()
                || !CanLookupPetsAfterZone()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (!pet.Character.Buffs.Contains(RelevantNanos.PerkTauntBox))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Buffs

        protected bool ShieldOfTheObedientServant(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (!pet.Character.Buffs.Contains(NanoLine.ShieldoftheObedientServant))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        private bool MastersBidding(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PetProcSelection.MastersBidding != (PetProcSelection)_settings["PetProcSelection"].AsInt32()) { return false; }

            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (!pet.Character.Buffs.Contains(NanoLine.SiphonBox683)
                    && pet.Type == PetType.Attack)
                {
                    if (spell.IsReady)
                        spell.Cast(pet.Character, true);
                }
            }

            return false;
        }

        private bool SedativeInjectors(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PetProcSelection.SedativeInjectors != (PetProcSelection)_settings["PetProcSelection"].AsInt32()) { return false; }

            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (!pet.Character.Buffs.Contains(NanoLine.SiphonBox683)
                    && (pet.Type == PetType.Attack || pet.Type == PetType.Support))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #endregion

        #region Misc

        protected bool PetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PetSpawner(PetsList.Pets, spell, fightingTarget, ref actionTarget))
            {
                ResetTrimmers();

                if (!CanCast(spell)) { return false; }

                return true;
            }
            return false;
        }

        protected bool PetTargetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(spell.Nanoline, PetType.Attack, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(spell.Nanoline, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool SnareMobExists()
        {
            return DynelManager.NPCs
                .Where(c => c.Name == "Flaming Vengeance" ||
                    c.Name == "Hand of the Colonel")
                .Any();
        }

        protected bool CanTrim()
        {
            return _lastTrimTime + DelayBetweenTrims < Time.NormalTime;
        }

        protected bool CanDivertOffTrim(Pet pet)
        {
            return _lastPetTrimDivertOffTime[pet.Type] + DelayBetweenDiverTrims < Time.NormalTime || !petTrimmedOffDiv[pet.Type];
        }

        protected bool CanDivertHpTrim(Pet pet)
        {
            return _lastPetTrimDivertHpTime[pet.Type] + DelayBetweenDiverTrims < Time.NormalTime || !petTrimmedHpDiv[pet.Type];
        }


        protected bool CanAggDefTrim(Pet pet)
        {
            return !petTrimmedAggDef[pet.Type];
        }

        protected bool CanTauntTrim(Pet pet)
        {
            return pet.Type == PetType.Attack && !attackPetTrimmedAggressive;
        }

        private void ResetTrimmers()
        {
            attackPetTrimmedAggressive = false;
            petTrimmedOffDiv[PetType.Attack] = false;
            petTrimmedOffDiv[PetType.Support] = false;
            petTrimmedHpDiv[PetType.Attack] = false;
            petTrimmedHpDiv[PetType.Support] = false;
            petTrimmedAggDef[PetType.Attack] = false;
            petTrimmedAggDef[PetType.Support] = false;
        }

        private void OnZoned(object s, EventArgs e)
        {

            ResetTrimmers();
        }

        private void CancelBuffs()
        {
            if (BuffingAuraSelection.Shield != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ShieldAura);
            }

            if (BuffingAuraSelection.Damage != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.DamageAura);
            }

            if (BuffingAuraSelection.Armor != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ArmorAura);
            }

            if (BuffingAuraSelection.Reflect != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ReflectAura);
            }

            CancelBuffs(DebuffingAuraSelection.ShieldRipper == (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()
                ? RelevantNanos.Blinds : RelevantNanos.ShieldRippers);
        }

        protected bool ShouldCancelHostileAuras()
        {
            return Time.NormalTime - _lastCombatTime > 5;
        }

        private static class RelevantNanos
        {
            public const int CompositeAttribute = 223372;
            public const int CompositeNano = 223380;
            public const int MastersBidding = 268171;
            public const int SedativeInjectors = 302254;
            public const int CompositeUtility = 287046;
            public const int CompositeRanged = 223348;
            public const int CompositeRangedSpec = 223364;
            public const int SympatheticReactiveCocoon = 154550;
            public const int IntrusiveAuraCancellation = 204372;
            public const int BoostedTendons = 269463;
            public const int PetHealingGreater = 270351;
            public const int PetWarp = 209488;

            public static readonly Spell[] DamageBuffLineA = Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA)
                .Where(spell => spell.Id != RelevantNanos.BoostedTendons).OrderByStackingOrder().ToArray();

            public static readonly int[] PerkTauntBox = { 229131, 229130, 229129, 229128, 229127, 229126 };
            public static readonly int[] PerkSiphonBox = { 229657, 229656, 229655, 229654 };
            public static readonly int[] PerkChaoticBox = { 227787 };
            public static readonly int[] PetCleanse = { 269870, 269869 };

            public static readonly int[] ShieldRippers = { 154725, 154726, 154727, 154728 };
            public static readonly int[] Blinds = { 154715, 154716, 154717, 154718, 154719 };
            public static readonly int[] ShieldAura = { 154550, 154551, 154552, 154553 };
            public static readonly int[] DamageAura = { 154560, 154561 };
            public static readonly int[] ArmorAura = { 154562, 154563, 154564, 154565, 154566, 154567 };
            public static readonly int[] ReflectAura = { 154557, 154558, 154559 };
            public static readonly int[] PetHealing = { 116791, 116795, 116796, 116792, 116797, 116794, 116793 };
            public static readonly int[] ShieldOfObedientServant = { 270790, 202260 };
        }

        private static class RelevantTrimmers
        {
            public const int IncreaseAggressivenessLow = 154939;
            public const int IncreaseAggressivenessHigh = 154940;
            public const int DivertEnergyToOffenseLow = 88377;
            public const int DivertEnergyToOffenseHigh = 88378;
            public const int PositiveAggressiveDefensive = 88384;
            public const int DivertEnergyToHitpointsLow = 88381;
            public const int DivertEnergyToHitpointsHigh = 88382;
        }

        public enum PetPerkSelection
        {
            TauntBox, ChaoticBox, SiphonBox
        }
        public enum PetProcSelection
        {
            None, MastersBidding, SedativeInjectors
        }

        public enum BuffingAuraSelection
        {
            None, Armor, Reflect, Damage, Shield
        }
        public enum DebuffingAuraSelection
        {
            None, Blind, PetSnare, ShieldRipper
        }
        public enum ProcType1Selection
        {
            ReactiveArmor, DestructiveTheorem, EnergyTransfer, EndureBarrage, DestructiveSignal, SplinterPreservation, CushionBlows
        }
        public enum InitBuffSelection
        {
            None, Self, Team
        }
        public enum ProcType2Selection
        {
            AssaultForceRelief, DroneMissiles, DroneExplosives, CongenialEncasement, PersonalProtection
        }

        public static void EngiBioCocoonPercentage_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].EngiBioCocoonPercentage = e;
            EngiBioCocoonPercentage = e;
            Config.Save();
        }

        #endregion

        // WUT
        private bool AuraCancellation(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null) { return false; }

            actionTarget.Target = DynelManager.LocalPlayer.Pets
                .Where(c => c.Character.Buffs.Contains(NanoLine.EngineerPetAOESnareBuff))
                .FirstOrDefault().Character;

            if (actionTarget.Target != null)
            {
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }
    }
}
