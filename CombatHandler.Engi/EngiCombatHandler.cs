using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System.Linq;
using System;
using AOSharp.Common.GameData.UI;
using AOSharp.Core.IPC;
using System.Threading.Tasks;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System.Threading;
using SmokeLounge.AOtomation.Messaging.Messages;
using System.Collections.Generic;
using AOSharp.Core.Inventory;
using CombatHandler.Generic;

namespace CombatHandler.Engineer
{
    class EngiCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

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
        private double _recastBlinds = Time.NormalTime;

        private static double _ncuUpdateTime;

        public EngiCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            Game.TeleportEnded += OnZoned;

            _settings.AddVariable("SyncPets", true);
            _settings.AddVariable("SpawnPets", true);
            _settings.AddVariable("BuffPets", true);
            _settings.AddVariable("HealPets", false);

            _settings.AddVariable("DivertHpTrimmer", true);
            _settings.AddVariable("DivertOffTrimmer", true);
            _settings.AddVariable("TauntTrimmer", true);
            _settings.AddVariable("AggDefTrimmer", true);

            _settings.AddVariable("BuffingAuraSelection", (int)BuffingAuraSelection.Damage);
            _settings.AddVariable("DebuffingAuraSelection", (int)DebuffingAuraSelection.Blind);

            _settings.AddVariable("PetPerkSelection", (int)PetPerkSelection.Off);
            _settings.AddVariable("PetProcSelection", (int)PetProcSelection.None);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.ReactiveArmor);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.AssaultForceRelief);

            _settings.AddVariable("SpamBlindAura", false);
            _settings.AddVariable("SpamSnareAura", false);

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

            //Leg Shot
            RegisterPerkProcessor(PerkHash.LegShot, LegShot);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolMasteryBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.GrenadeBuffs).OrderByStackingOrder(), PistolGrenadeBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadowlandReflectBase).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SpecialAttackAbsorberBase).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerSpecialAttackAbsorber).OrderByStackingOrder(), GenericBuff);


            RegisterSpellProcessor(RelevantNanos.BoostedTendons, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.DamageBuffLineA, TeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(), TeamBuff);
            RegisterSpellProcessor(RelevantNanos.Blinds, BlindAura);
            RegisterSpellProcessor(RelevantNanos.ShieldRippers, ShieldRipperAura);
            RegisterSpellProcessor(RelevantNanos.ArmorAura, ArmorAura);
            RegisterSpellProcessor(RelevantNanos.DamageAura, DamageAura);
            RegisterSpellProcessor(RelevantNanos.ReflectAura, ReflectAura);
            RegisterSpellProcessor(RelevantNanos.ShieldAura, ShieldAura);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerPetAOESnareBuff).OrderByStackingOrder(), SnareAura);
            RegisterSpellProcessor(RelevantNanos.IntrusiveAuraCancellation, AuraCancellation);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);

            //Pet Spawners
            RegisterSpellProcessor(PetsList.Pets.Where(x => x.Value.PetType == PetType.Attack).Select(x => x.Key).ToArray(), PetSpawner);
            RegisterSpellProcessor(PetsList.Pets.Where(x => x.Value.PetType == PetType.Support).Select(x => x.Key).ToArray(), PetSpawner);

            RegisterSpellProcessor(RelevantNanos.PetCleanse, PetCleanse);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerMiniaturization).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetShortTermDamageBuffs).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDefensiveNanos).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPPetInitiativeBuffs).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerMiniaturization).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(), PetTargetBuff);

            RegisterSpellProcessor(RelevantNanos.PetHealing, PetHealing);
            RegisterSpellProcessor(RelevantNanos.PetHealingCH, PetHealingCH);

            RegisterSpellProcessor(RelevantNanos.ShieldOfObedientServant, ShieldOfTheObedientServant);
            RegisterSpellProcessor(RelevantNanos.MastersBidding, MastersBidding);
            RegisterSpellProcessor(RelevantNanos.SedativeInjectors, SedativeInjectors);
            RegisterSpellProcessor(RelevantNanos.DamageBuffLineA, PetDamage);

            RegisterPerkProcessor(PerkHash.ChaoticEnergy, ChaoticEnergyBox);
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

            //Pet Shells
            foreach (PetSpellData petData in PetsList.Pets.Values)
            {
                RegisterItemProcessor(petData.ShellId, petData.ShellId2, PetSpawnerItem);
            }

            PluginDirectory = pluginDir;
        }

        public Window[] _windows => new Window[] { _petWindow, _buffWindow };

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

        private void HandlePetViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //Cannot stop Multi-Tabs. Easy fix would be correct naming of views to reference against WindowOptions - options.Name
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
                //Cannot re-use the view, as crashes client. I don't know why.
                //Cannot stop Multi-Tabs. Easy fix would be correct naming of views to reference against WindowOptions - options.Name
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
                //Cannot re-use the view, as crashes client. I don't know why.

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

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
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

            }

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

            if (!IsSettingEnabled("SpamBlindAura"))
            {
                CancelBuffs(DebuffingAuraSelection.ShieldRipper == (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()
                    ? RelevantNanos.Blinds : RelevantNanos.ShieldRippers);
                CancelHostileAuras(RelevantNanos.Blinds);
            }

            CancelHostileAuras(RelevantNanos.ShieldRippers);
        }

        #region Perks


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

        private bool AuraCancellation(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(fightingTarget != null) { return false; }

            Pet petWithSnareAura = FindPetThat(pet => HasBuffNanoLine(NanoLine.EngineerPetAOESnareBuff, pet.Character));

            if (petWithSnareAura != null)
            {
                actionTarget.Target = petWithSnareAura.Character;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        private bool LegShot(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("LegShot")) { return false; }

            return LegShotPerk(perk, fightingTarget, ref actionTarget);
        }

        private bool ShouldSpamAoeSnare()
        {
            return DynelManager.NPCs
                .Where(c => c.Name == "Flaming Vengeance" ||
                    c.Name == "Hand of the Colonel")
                .Any();
        }

        protected bool PistolGrenadeBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam)
            {
                return TeamBuffNoNTWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade) || TeamBuffNoNTWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
            }
            else
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.AssaultRifleBuffs)) { return false; }

                return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade) || BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
            }
        }

        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffInitEngi(spell, fightingTarget, ref actionTarget);
        }

        private bool MastersBidding(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PetProcSelection.MastersBidding != (PetProcSelection)_settings["PetProcSelection"].AsInt32()) { return false; }

            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            Pet petToBuff = FindPetThat(pet => !pet.Character.Buffs.Contains(NanoLine.SiphonBox683)
                && (pet.Type == PetType.Attack || pet.Type == PetType.Support));

            if (petToBuff != null)
            {
                spell.Cast(petToBuff.Character, true);
            }

            return false;
        }
        private bool SedativeInjectors(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PetProcSelection.SedativeInjectors != (PetProcSelection)_settings["PetProcSelection"].AsInt32()) { return false; }

            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            Pet petToBuff = FindPetThat(pet => !pet.Character.Buffs.Contains(NanoLine.SiphonBox683)
                && (pet.Type == PetType.Attack || pet.Type == PetType.Support));

            if (petToBuff != null)
            {
                spell.Cast(petToBuff.Character, true);
            }

            return false;
        }

        private bool ArmorAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.Armor != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DamageAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.Damage != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool ReflectAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.Reflect != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool ShieldAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.Shield != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool SnareAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (IsSettingEnabled("SpamSnareAura") && ShouldSpamAoeSnare())
            {
                Pet petToCastOn = FindPetThat(pet => true);
                if (petToCastOn != null)
                {
                    actionTarget.Target = petToCastOn.Character;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            if (DebuffingAuraSelection.PetSnare != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()
                || fightingTarget == null) { return false; }

            return PetTargetBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool ShieldRipperAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DebuffingAuraSelection.ShieldRipper != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool BlindAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (IsSettingEnabled("SpamBlindAura"))
            {
                if (Time.NormalTime - _recastBlinds > 9)
                {
                    _recastBlinds = Time.NormalTime;
                    return true;
                }
            }

            if (fightingTarget == null) { return false; }

            if (DebuffingAuraSelection.Blind == (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32())
            {
                if (Time.NormalTime - _recastBlinds > 9)
                {
                    _recastBlinds = Time.NormalTime;
                    return true;
                }
            }

            return false;
        }

        private bool PetHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("HealPets") || !CanLookupPetsAfterZone()) { return false; }

            Pet pettoheal = FindPetNeedsHeal(90);
            if (pettoheal != null)
            {
                actionTarget.Target = pettoheal.Character;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        private bool PetHealingCH(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("HealPets") || !CanLookupPetsAfterZone()) { return false; }

            Pet pettoheal = FindPetNeedsHeal(90);
            if (pettoheal != null)
            {
                actionTarget.Target = pettoheal.Character;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        private bool ChaoticEnergyBox(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PetPerkSelection.Off != (PetPerkSelection)_settings["PetPerkSelection"].AsInt32()
                || !CanLookupPetsAfterZone()) { return false; }

            Pet petToPerk = FindPetsWithoutBuff(RelevantNanos.PerkChaoticEnergy);

            CancelBuffs(RelevantNanos.PerkTauntBox);
            CancelBuffs(RelevantNanos.PerkSiphonBox);

            if (petToPerk != null)
            {
                actionTarget.Target = petToPerk.Character;
                actionTarget.ShouldSetTarget = true;
                return true;
            }
            return false;
        }

        private bool SiphonBox(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PetPerkSelection.Def != (PetPerkSelection)_settings["PetPerkSelection"].AsInt32()
                || !CanLookupPetsAfterZone()) { return false; }

            Pet petToPerk = FindPetsWithoutBuff(RelevantNanos.PerkSiphonBox);

            CancelBuffs(RelevantNanos.PerkChaoticEnergy);

            if (petToPerk != null)
            {
                if (petToPerk.Type == PetType.Attack) { return false; }

                actionTarget.Target = petToPerk.Character;
                actionTarget.ShouldSetTarget = true;
                return true;
            }
            return false;
        }

        private bool TauntBox(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PetPerkSelection.Def != (PetPerkSelection)_settings["PetPerkSelection"].AsInt32()
                || !CanLookupPetsAfterZone()) { return false; }

            Pet petToPerk = FindPetsWithoutBuff(RelevantNanos.PerkTauntBox);

            CancelBuffs(RelevantNanos.PerkChaoticEnergy);

            if (petToPerk != null)
            {
                if (petToPerk.Type == PetType.Support) { return false; }

                actionTarget.Target = petToPerk.Character;
                actionTarget.ShouldSetTarget = true;
                return true;
            }
            return false;
        }

        protected bool PetDivertHpTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("DivertHpTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

            Pet petToTrim = FindSupportPetThat(CanDivertHpTrim);

            if (petToTrim != null)
            {
                actiontarget.Target = petToTrim.Character;
                actiontarget.ShouldSetTarget = true;
                petTrimmedHpDiv[petToTrim.Type] = true;
                _lastPetTrimDivertHpTime[petToTrim.Type] = Time.NormalTime;
                _lastTrimTime = Time.NormalTime;
                return true;
            }
            return false;
        }


        protected bool PetDivertOffTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("DivertOffTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

            Pet petToTrim;

            if (IsSettingEnabled("DivertHpTrimmer"))
            {
                petToTrim = FindAttackPetThat(CanDivertOffTrim);

                if (petToTrim != null)
                {
                    actiontarget.Target = petToTrim.Character;
                    actiontarget.ShouldSetTarget = true;
                    petTrimmedOffDiv[petToTrim.Type] = true;
                    _lastPetTrimDivertOffTime[petToTrim.Type] = Time.NormalTime;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }
            }
            else
            {
                petToTrim = FindPetThat(CanDivertOffTrim);

                if (petToTrim != null)
                {
                    actiontarget.Target = petToTrim.Character;
                    actiontarget.ShouldSetTarget = true;
                    petTrimmedOffDiv[petToTrim.Type] = true;
                    _lastPetTrimDivertOffTime[petToTrim.Type] = Time.NormalTime;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }
            }

            return false;
        }

        protected bool PetAggDefTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("AggDefTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

            Pet petToTrim = FindPetThat(CanAggDefTrim);                
            if (petToTrim != null)
            {
                actiontarget.Target = petToTrim.Character;
                actiontarget.ShouldSetTarget = true;
                petTrimmedAggDef[petToTrim.Type] = true;
                _lastTrimTime = Time.NormalTime;
                return true;
            }
            return false;
        }

        protected bool PetAggressiveTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("TauntTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

            Pet petToTrim = FindPetThat(CanTauntTrim);

            if (petToTrim != null)
            {
                if (petToTrim.Type == PetType.Support) { return false; }

                actiontarget = (petToTrim.Character, true);
                attackPetTrimmedAggressive = true;
                _lastTrimTime = Time.NormalTime;
                return true;
            }
            return false;
        }

        protected bool PetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PetSpawner(PetsList.Pets, spell, fightingTarget, ref actionTarget))
            {
                ResetTrimmers();
                return true;
            }
            return false;
        }

        protected virtual bool PetSpawnerItem(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetSpawnerItem(PetsList.Pets, item, fightingTarget, ref actionTarget);
        }

        protected bool PetTargetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(spell.Nanoline, PetType.Attack, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(spell.Nanoline, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        public bool PetCleanse(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanLookupPetsAfterZone()) { return false; }

            List<Pet> pets = DynelManager.LocalPlayer.Pets
                .Where(x => x.Character.Buffs.Contains(NanoLine.Root) || x.Character.Buffs.Contains(NanoLine.Snare)
                || x.Character.Buffs.Contains(NanoLine.Mezz))
                .ToList();

            if (pets?.Count > 1)
            {
                return true;
            }

            return false;
        }

        protected bool ShieldOfTheObedientServant(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            return FindPetThat(pet => !HasBuffNanoLine(NanoLine.ShieldoftheObedientServant, pet.Character)) != null;
        }

        protected bool PetDamage(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            //if (IsSettingEnabled("DamageAura")) { return false; } Issue

            Pet petToBuff = FindPetThat(pet => !pet.Character.Buffs.Contains(NanoLine.DamageBuffs_LineA)
                && (pet.Type == PetType.Attack || pet.Type == PetType.Support));

            if (petToBuff != null)
            {
                spell.Cast(petToBuff.Character, true);
            }

            return false;
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

        private bool CanPerkBox(Pet pet)
        {
            return !pet.Character.Buffs.Any(buff => buff.Nanoline == NanoLine.GadgeteerPetProcs);
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
            public const int PetHealingCH = 270351;

            public static readonly Spell[] DamageBuffLineA = Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA)
                .Where(spell => spell.Id != RelevantNanos.BoostedTendons).OrderByStackingOrder().ToArray();

            public static readonly int[] PerkTauntBox = { 229131, 229130, 229129, 229128, 229127, 229126 };
            public static readonly int[] PerkSiphonBox = { 229657, 229656, 229655, 229654 };
            public static readonly int[] PerkChaoticEnergy = { 227787 };
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
            Off, Def
        }
        public enum PetProcSelection
        {
            None, MastersBidding, SedativeInjectors
        }

        public enum BuffingAuraSelection
        {
            Armor, Reflect, Damage, Shield
        }
        public enum DebuffingAuraSelection
        {
            Blind, PetSnare, ShieldRipper
        }
        public enum ProcType1Selection
        {
            ReactiveArmor, DestructiveTheorem, EnergyTransfer, EndureBarrage, DestructiveSignal, SplinterPreservation, CushionBlows
        }

        public enum ProcType2Selection
        {
            AssaultForceRelief, DroneMissiles, DroneExplosives, CongenialEncasement, PersonalProtection
        }
    }
}
