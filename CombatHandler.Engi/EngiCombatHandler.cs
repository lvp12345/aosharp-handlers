using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CombatHandler.Engineer
{
    class EngiCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleRez = false;

        public static bool _syncPets;

        private const float DelayBetweenDiverTrims = 305;
        private const float TrimCooldown = 1;

        protected Dictionary<PetType, bool> petTrimmedAggressive = new Dictionary<PetType, bool>();
        protected Dictionary<PetType, bool> petTrimmedAggDef = new Dictionary<PetType, bool>();

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
        private static Window _petCommandWindow;
        private static Window _buffWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;

        private static View _buffView;
        private static View _petView;
        private static View _petCommandView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;

        private double _lastTrimTime = 0;

        private static double _ncuUpdateTime;

        public EngiCombatHandler(string pluginDir) : base(pluginDir)
        {
            try
            {
                IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalRez, OnGlobalRezMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetAttack, OnPetAttack);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetWait, OnPetWait);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetFollow, OnPetFollow);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetWarp, OnPetWarp);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetSyncOn, SyncPetsOnMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetSyncOff, SyncPetsOffMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.ClearBuffs, OnClearBuffs);
                IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

                Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentageChangedEvent += BioCocoonPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetNameChangedEvent += StimTargetName_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentageChangedEvent += StimHealthPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentageChangedEvent += StimNanoPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentageChangedEvent += KitHealthPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentageChangedEvent += KitNanoPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelayChangedEvent += CycleSpherePerkDelay_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelayChangedEvent += CycleWitOfTheAtroxPerkDelay_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentageChangedEvent += SelfHealPerkPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentageChangedEvent += SelfNanoPerkPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentageChangedEvent += TeamHealPerkPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentageChangedEvent += TeamNanoPerkPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentageChangedEvent += BodyDevAbsorbsItemPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentageChangedEvent += StrengthAbsorbsItemPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].BioRegrowthPercentageChangedEvent += BioRegrowthPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleBioRegrowthPerkDelayChangedEvent += CycleBioRegrowthPerkDelay_Changed;

                _settings.AddVariable("Buffing", true);
                _settings.AddVariable("Composites", true);

                _settings.AddVariable("GlobalBuffing", true);
                _settings.AddVariable("GlobalComposites", true);
                _settings.AddVariable("GlobalRez", true);

                _settings.AddVariable("SharpObjects", true);
                _settings.AddVariable("Grenades", true);

                _settings.AddVariable("TauntTool", false);

                _settings.AddVariable("StimTargetSelection", (int)StimTargetSelection.Self);

                _settings.AddVariable("Kits", true);

                _settings.AddVariable("SyncPets", true);
                _settings.AddVariable("SpawnPets", true);
                _settings.AddVariable("BuffPets", true);
                _settings.AddVariable("HealPets", false);
                _settings.AddVariable("WarpPets", false);

                _settings.AddVariable("PetDefensiveNanos", false);
                _settings.AddVariable("PetArmorBuff", false);

                _settings.AddVariable("TauntTrimmer", true);
                _settings.AddVariable("AggDefTrimmer", true);
                _settings.AddVariable("DivertHpTrimmer", false);
                _settings.AddVariable("DivertOffTrimmer", true);

                _settings.AddVariable("InitBuffSelection", (int)InitBuffSelection.Self);
                _settings.AddVariable("TeamArmorBuff", true);
                _settings.AddVariable("PistolTeam", true);
                _settings.AddVariable("GrenadeTeam", true);
                _settings.AddVariable("ShadowlandReflectBase", true);
                _settings.AddVariable("DamageShields", false);
                _settings.AddVariable("SLMap", false);
                _settings.AddVariable("MEBuff", false);
                _settings.AddVariable("SelfBlockers", false);
                _settings.AddVariable("TeamBlockers", false);

                _settings.AddVariable("BuffingAuraSelection", (int)BuffingAuraSelection.None);
                _settings.AddVariable("DebuffingAuraSelection", (int)DebuffingAuraSelection.None);

                _settings.AddVariable("PetPerkSelection", (int)PetPerkSelection.None);
                _settings.AddVariable("PetProcSelection", (int)PetProcSelection.None);

                _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.ReactiveArmor);
                _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.AssaultForceRelief);

                _settings.AddVariable("LegShot", false);

                RegisterSettingsWindow("Engi Handler", "EngineerSettingsView.xml");

                Game.TeleportEnded += OnZoned;

                //Pet heals
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetHealing).OrderByStackingOrder(), PetHealing);

                //Pet Aura 
                //buffing
                RegisterSpellProcessor(RelevantNanos.ArmorAura, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericAuraBuff(spell, fightingTarget, ref actionTarget, BuffingAuraSelection.Armor));

                RegisterSpellProcessor(RelevantNanos.ReflectAura, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericAuraBuff(spell, fightingTarget, ref actionTarget, BuffingAuraSelection.Reflect));

                RegisterSpellProcessor(RelevantNanos.DamageAura, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericAuraBuff(spell, fightingTarget, ref actionTarget, BuffingAuraSelection.Damage));

                RegisterSpellProcessor(RelevantNanos.ShieldAura, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericAuraBuff(spell, fightingTarget, ref actionTarget, BuffingAuraSelection.Shield));

                //debuffing
                RegisterSpellProcessor(RelevantNanos.Blinds, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericDebuffingAura(spell, fightingTarget, ref actionTarget, DebuffingAuraSelection.Blind));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerPetAOESnareBuff).OrderByStackingOrder(), (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericDebuffingAura(spell, fightingTarget, ref actionTarget, DebuffingAuraSelection.PetSnare));

                RegisterSpellProcessor(RelevantNanos.ShieldRippers, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericDebuffingAura(spell, fightingTarget, ref actionTarget, DebuffingAuraSelection.ShieldRipper));

                RegisterSpellProcessor(RelevantNanos.IntrusiveAuraCancellation, AuraCancellation);

                //Buffs
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonComabtTeamBuff(buffSpell, fightingTarget, ref actionTarget, "TeamArmorBuff"));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonComabtTeamBuff(buffSpell, fightingTarget, ref actionTarget, "DamageShields"));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadowlandReflectBase).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonCombatBuff(spell, ref actionTarget, fightingTarget, "ShadowlandReflectBase"));

                RegisterSpellProcessor(RelevantNanos.EngineeringBuff,
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonCombatBuff(spell, ref actionTarget, fightingTarget, "MEBuff"));

               
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolTeam);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.GrenadeBuffs).OrderByStackingOrder(), Grenade);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SpecialAttackAbsorberBase).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonCombatBuff(spell, ref actionTarget, fightingTarget, "TeamBlockers"));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerSpecialAttackAbsorber).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonCombatBuff(spell, ref actionTarget, fightingTarget, "SelfBlockers"));
                
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);

                
                RegisterSpellProcessor(RelevantNanos.BoostedTendons,
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                       => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));

                RegisterSpellProcessor(RelevantNanos.DamageBuffLineA,
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                        => NonComabtTeamBuff(spell, fightingTarget, ref actionTarget));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.GeneralMechanicalEngineeringBuff).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));

                //Pets
                //pet spawners
                RegisterSpellProcessor(PetsList.Pets.Where(c => c.Value.PetType == PetType.Attack).Select(c => c.Key).ToArray(), CastPets);
                RegisterSpellProcessor(PetsList.Pets.Where(c => c.Value.PetType == PetType.Support).Select(c => c.Key).ToArray(), CastPets);

                //pet buffs
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerMiniaturization).OrderByStackingOrder(), PetBuff);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetShortTermDamageBuffs).OrderByStackingOrder(), PetBuff);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPPetInitiativeBuffs).OrderByStackingOrder(), PetBuff);
                RegisterSpellProcessor(RelevantNanos.DamageBuffLineA, PetBuff);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => SettingPetBuff(buffSpell, fightingTarget, ref actionTarget, "PetArmorBuff"));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDefensiveNanos).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => SettingPetBuff(buffSpell, fightingTarget, ref actionTarget, "PetDefensiveNanos"));

                RegisterSpellProcessor(RelevantNanos.ShieldOfObedientServant, ShieldOfTheObedientServant);

                RegisterSpellProcessor(RelevantNanos.PetCleanse, PetCleanse);

                RegisterSpellProcessor(RelevantNanos.PetWarp, PetWarp, CombatActionPriority.High);

                //pet procs
                RegisterSpellProcessor(RelevantNanos.MastersBidding,
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => GenericPetProc(spell, fightingTarget, ref actionTarget, PetProcSelection.MastersBidding));


                RegisterSpellProcessor(RelevantNanos.SedativeInjectors,
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => GenericPetProc(spell, fightingTarget, ref actionTarget, PetProcSelection.SedativeInjectors));

                //pet perks
                RegisterPerkProcessor(PerkHash.TauntBox,
                    (PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => GenericPetPerk(perkAction, fightingTarget, ref actionTarget, PetPerkSelection.TauntBox));

                RegisterPerkProcessor(PerkHash.ChaoticEnergy,
                    (PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => GenericPetPerk(perkAction, fightingTarget, ref actionTarget, PetPerkSelection.ChaoticBox));

                RegisterPerkProcessor(PerkHash.SiphonBox,
                    (PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => GenericPetPerk(perkAction, fightingTarget, ref actionTarget, PetPerkSelection.SiphonBox));

                //pet trimmers
                RegisterItemProcessor(RelevantTrimmers.IncreaseAggressiveness,
                    (Item item, SimpleChar target, ref (SimpleChar Target, bool ShouldSetTarget) action) =>
                    PetTrimmer(item, target, ref action, "TauntTrimmer", CanTauntTrim, petType =>
                    {
                        petTrimmedAggressive[petType] = true;
                    }, petType => _lastTrimTime = Time.NormalTime));

                RegisterItemProcessor(RelevantTrimmers.PositiveAggressiveDefensive,
                    (Item item, SimpleChar target, ref (SimpleChar Target, bool ShouldSetTarget) action) =>
                    PetTrimmer(item, target, ref action, "AggDefTrimmer", CanAggDefTrim, petType =>
                    {
                        petTrimmedAggDef[petType] = true;
                    }, petType => _lastTrimTime = Time.NormalTime));

                RegisterItemProcessor(RelevantTrimmers.DivertEnergyToHitpoints,
                    (Item item, SimpleChar target, ref (SimpleChar Target, bool ShouldSetTarget) action) =>
                    PetTrimmer(item, target, ref action, "DivertHpTrimmer", CanDivertHpTrim, petType =>
                    {
                        petTrimmedHpDiv[petType] = true;
                    }, petType => _lastPetTrimDivertHpTime[petType] = Time.NormalTime));

                RegisterItemProcessor(RelevantTrimmers.DivertEnergyToOffense,
                    (Item item, SimpleChar target, ref (SimpleChar Target, bool ShouldSetTarget) action) =>
                    PetTrimmer(item, target, ref action, "DivertOffTrimmer", CanDivertOffTrim, petType =>
                    {
                        petTrimmedOffDiv[petType] = true;
                    }, petType => _lastPetTrimDivertOffTime[petType] = Time.NormalTime));


                ResetTrimmers();

                //LE Procs
                RegisterPerkProcessor(PerkHash.LEProcEngineerReactiveArmor, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerDestructiveTheorem, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerEnergyTransfer, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerEndureBarrage, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerDestructiveSignal, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerSplinterPreservation, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerCushionBlows, LEProc1, CombatActionPriority.Low);

                RegisterPerkProcessor(PerkHash.LEProcEngineerAssaultForceRelief, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerDroneMissiles, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerDroneExplosives, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerCongenialEncasement, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerPersonalProtection, LEProc2, CombatActionPriority.Low);

                PluginDirectory = pluginDir;

                BioCocoonPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage;
                StimTargetName = Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetName;
                StimHealthPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentage;
                StimNanoPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentage;
                KitHealthPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage;
                KitNanoPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage;
                CycleSpherePerkDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelay;
                CycleWitOfTheAtroxPerkDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelay;
                SelfHealPerkPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentage;
                SelfNanoPerkPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentage;
                TeamHealPerkPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentage;
                TeamNanoPerkPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentage;
                BodyDevAbsorbsItemPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentage;
                StrengthAbsorbsItemPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentage;
                BioRegrowthPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].BioRegrowthPercentage;
                CycleBioRegrowthPerkDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].CycleBioRegrowthPerkDelay;
            }

            catch (Exception ex)
            {
                var errorMessage = "An error occurred on line " + GetLineNumber(ex) + ": " + ex.Message;

                if (errorMessage != previousErrorMessage)
                {
                    Chat.WriteLine(errorMessage);
                    Chat.WriteLine("Stack Trace: " + ex.StackTrace);
                    previousErrorMessage = errorMessage;
                }
            }
        }

        public Window[] _windows => new Window[] { _petWindow, _petCommandWindow, _buffWindow, _procWindow, _itemWindow, _perkWindow };

        #region Callbacks

        private void syncPetsOnEnabled()
        {
            _syncPets = true;
        }
        private void syncPetsOffDisabled()
        {
            _syncPets = false;
        }

        public static void OnRemainingNCUMessage(int sender, IPCMessage msg)
        {
            RemainingNCUMessage ncuMessage = (RemainingNCUMessage)msg;
            SettingsController.RemainingNCU[ncuMessage.Character] = ncuMessage.RemainingNCU;
        }
        private void OnGlobalBuffingMessage(int sender, IPCMessage msg)
        {
            GlobalBuffingMessage buffMsg = (GlobalBuffingMessage)msg;

            if (DynelManager.LocalPlayer.Identity.Instance == sender) { return; }

            _settings[$"Buffing"] = buffMsg.Switch;
            _settings[$"GlobalBuffing"] = buffMsg.Switch;
        }
        private void OnGlobalCompositesMessage(int sender, IPCMessage msg)
        {
            GlobalCompositesMessage compMsg = (GlobalCompositesMessage)msg;

            if (DynelManager.LocalPlayer.Identity.Instance == sender) { return; }

            _settings[$"Composites"] = compMsg.Switch;
            _settings[$"GlobalComposites"] = compMsg.Switch;
        }

        private void OnGlobalRezMessage(int sender, IPCMessage msg)
        {
            GlobalRezMessage rezMsg = (GlobalRezMessage)msg;

            if (DynelManager.LocalPlayer.Identity.Instance == sender) { return; }

            _settings[$"GlobalRez"] = rezMsg.Switch;
            _settings[$"GlobalRez"] = rezMsg.Switch;

        }

        private void SyncPetsOnMessage(int sender, IPCMessage msg)
        {
            _settings["SyncPets"] = true;
            syncPetsOnEnabled();
        }

        private void SyncPetsOffMessage(int sender, IPCMessage msg)
        {
            _settings["SyncPets"] = false;
            syncPetsOffDisabled();
        }

        public static void OnPetAttack(int sender, IPCMessage msg)
        {
            PetAttackMessage attackMsg = (PetAttackMessage)msg;
            DynelManager.LocalPlayer.Pets.Attack(attackMsg.Target);
        }

        private static void OnPetWait(int sender, IPCMessage msg)
        {
            if (DynelManager.LocalPlayer.Pets.Length > 0)
            {
                foreach (Pet pet in DynelManager.LocalPlayer.Pets)
                {
                    pet.Wait();
                }
            }
        }

        private static void OnPetWarp(int sender, IPCMessage msg)
        {
            if (DynelManager.LocalPlayer.Pets.Length > 0)
            {
                Spell warp = Spell.List.FirstOrDefault(x => RelevantNanos.Warps.Contains(x.Id));
                if (warp != null)
                {
                    warp.Cast(DynelManager.LocalPlayer, false);
                }
            }
        }

        private static void OnPetFollow(int sender, IPCMessage msg)
        {
            if (DynelManager.LocalPlayer.Pets.Length > 0)
            {
                foreach (Pet pet in DynelManager.LocalPlayer.Pets)
                {
                    pet.Follow();
                }
            }
        }

        #endregion

        #region Handles

        private void PetAttackClicked(object s, ButtonBase button)
        {
            if (DynelManager.LocalPlayer.Pets.Length > 0)
            {
                foreach (Pet pet in DynelManager.LocalPlayer.Pets.Where(c => c.Type != PetType.Heal))
                {
                    pet.Attack((Identity)Targeting.Target?.Identity);
                    IPCChannel.Broadcast(new PetAttackMessage()
                    {
                        Target = (Identity)Targeting.Target?.Identity
                    });
                }
            }
        }

        private void PetWaitClicked(object s, ButtonBase button)
        {
            PetWaitCommand(null, null, null);
        }

        private void PetWarpClicked(object s, ButtonBase button)
        {
            PetWarpCommand(null, null, null);
        }

        private void PetFollowClicked(object s, ButtonBase button)
        {
            PetFollowCommand(null, null, null);
        }

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

        private void HandlePetCommandViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_petCommandView)) { return; }

                _petCommandView = View.CreateFromXml(PluginDirectory + "\\UI\\EngineerPetCommandView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Commands", XmlViewName = "EngineerPetCommandView" }, _petCommandView);
            }
            else if (_petCommandWindow == null || (_petCommandWindow != null && !_petCommandWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_petCommandWindow, PluginDir, new WindowOptions() { Name = "Commands", XmlViewName = "EngineerPetCommandView" }, _petCommandView, out var container);
                _petCommandWindow = container;
            }
        }

        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_perkView)) { return; }

                _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\EngineerPerksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Perks", XmlViewName = "EngineerPerksView" }, _perkView);

                window.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);
                window.FindView("SphereDelayBox", out TextInputView sphereInput);
                window.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                window.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                window.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                window.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                window.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);
                window.FindView("BioRegrowthPercentageBox", out TextInputView bioRegrowthPercentageInput);
                window.FindView("BioRegrowthDelayBox", out TextInputView bioRegrowthDelayInput);

                if (bioCocoonInput != null)
                    bioCocoonInput.Text = $"{BioCocoonPercentage}";
                if (sphereInput != null)
                    sphereInput.Text = $"{CycleSpherePerkDelay}";
                if (witOfTheAtroxInput != null)
                    witOfTheAtroxInput.Text = $"{CycleWitOfTheAtroxPerkDelay}";
                if (selfHealInput != null)
                    selfHealInput.Text = $"{SelfHealPerkPercentage}";
                if (selfNanoInput != null)
                    selfNanoInput.Text = $"{SelfNanoPerkPercentage}";
                if (teamHealInput != null)
                    teamHealInput.Text = $"{TeamHealPerkPercentage}";
                if (teamNanoInput != null)
                    teamNanoInput.Text = $"{TeamNanoPerkPercentage}";
                if (bioRegrowthPercentageInput != null)
                    bioRegrowthPercentageInput.Text = $"{BioRegrowthPercentage}";
                if (bioRegrowthDelayInput != null)
                    bioRegrowthDelayInput.Text = $"{CycleBioRegrowthPerkDelay}";
            }
            else if (_perkWindow == null || (_perkWindow != null && !_perkWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "EngineerPerksView" }, _perkView, out var container);
                _perkWindow = container;

                container.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);
                container.FindView("SphereDelayBox", out TextInputView sphereInput);
                container.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                container.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                container.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                container.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                container.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);
                container.FindView("BioRegrowthPercentageBox", out TextInputView bioRegrowthPercentageInput);
                container.FindView("BioRegrowthDelayBox", out TextInputView bioRegrowthDelayInput);

                if (bioCocoonInput != null)
                    bioCocoonInput.Text = $"{BioCocoonPercentage}";
                if (sphereInput != null)
                    sphereInput.Text = $"{CycleSpherePerkDelay}";
                if (witOfTheAtroxInput != null)
                    witOfTheAtroxInput.Text = $"{CycleWitOfTheAtroxPerkDelay}";
                if (selfHealInput != null)
                    selfHealInput.Text = $"{SelfHealPerkPercentage}";
                if (selfNanoInput != null)
                    selfNanoInput.Text = $"{SelfNanoPerkPercentage}";
                if (teamHealInput != null)
                    teamHealInput.Text = $"{TeamHealPerkPercentage}";
                if (teamNanoInput != null)
                    teamNanoInput.Text = $"{TeamNanoPerkPercentage}";
                if (bioRegrowthPercentageInput != null)
                    bioRegrowthPercentageInput.Text = $"{BioRegrowthPercentage}";
                if (bioRegrowthDelayInput != null)
                    bioRegrowthDelayInput.Text = $"{CycleBioRegrowthPerkDelay}";
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

        private void HandleItemViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_itemView)) { return; }

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\EngineerItemsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "EngineerItemsView" }, _itemView);

                window.FindView("StimTargetBox", out TextInputView stimTargetInput);
                window.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                window.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                window.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                window.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);
                window.FindView("BodyDevAbsorbsItemPercentageBox", out TextInputView bodyDevInput);
                window.FindView("StrengthAbsorbsItemPercentageBox", out TextInputView strengthInput);

                if (stimTargetInput != null)
                    stimTargetInput.Text = $"{StimTargetName}";
                if (stimHealthInput != null)
                    stimHealthInput.Text = $"{StimHealthPercentage}";
                if (stimNanoInput != null)
                    stimNanoInput.Text = $"{StimNanoPercentage}";
                if (kitHealthInput != null)
                    kitHealthInput.Text = $"{KitHealthPercentage}";
                if (kitNanoInput != null)
                    kitNanoInput.Text = $"{KitNanoPercentage}";
                if (bodyDevInput != null)
                    bodyDevInput.Text = $"{BodyDevAbsorbsItemPercentage}";
                if (strengthInput != null)
                    strengthInput.Text = $"{StrengthAbsorbsItemPercentage}";
            }
            else if (_itemWindow == null || (_itemWindow != null && !_itemWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "EngineerItemsView" }, _itemView, out var container);
                _itemWindow = container;

                container.FindView("StimTargetBox", out TextInputView stimTargetInput);
                container.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                container.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                container.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                container.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);
                container.FindView("BodyDevAbsorbsItemPercentageBox", out TextInputView bodyDevInput);
                container.FindView("StrengthAbsorbsItemPercentageBox", out TextInputView strengthInput);

                if (stimTargetInput != null)
                    stimTargetInput.Text = $"{StimTargetName}";
                if (stimHealthInput != null)
                    stimHealthInput.Text = $"{StimHealthPercentage}";
                if (stimNanoInput != null)
                    stimNanoInput.Text = $"{StimNanoPercentage}";
                if (kitHealthInput != null)
                    kitHealthInput.Text = $"{KitHealthPercentage}";
                if (kitNanoInput != null)
                    kitNanoInput.Text = $"{KitNanoPercentage}";
                if (bodyDevInput != null)
                    bodyDevInput.Text = $"{BodyDevAbsorbsItemPercentage}";
                if (strengthInput != null)
                    strengthInput.Text = $"{StrengthAbsorbsItemPercentage}";
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

            try
            {


                if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 1.6)
                    return;

                base.OnUpdate(deltaTime);

                if (Time.NormalTime > _ncuUpdateTime + 1.0f)
                {
                    RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                    IPCChannel.Broadcast(ncuMessage);

                    OnRemainingNCUMessage(0, ncuMessage);

                    _ncuUpdateTime = Time.NormalTime;
                }

                if (IsSettingEnabled("SyncPets"))
                    SynchronizePetCombatStateWithOwner();

                CancelBuffs();

                CancelHostileAuras(RelevantNanos.Blinds);
                CancelHostileAuras(RelevantNanos.ShieldRippers);

                #region UI

                var window = SettingsController.FindValidWindow(_windows);

                if (window != null && window.IsValid)
                {
                    window.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);
                    window.FindView("StimTargetBox", out TextInputView stimTargetInput);
                    window.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                    window.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                    window.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                    window.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);
                    window.FindView("SphereDelayBox", out TextInputView sphereInput);
                    window.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                    window.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                    window.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                    window.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                    window.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);
                    window.FindView("BodyDevAbsorbsItemPercentageBox", out TextInputView bodyDevInput);
                    window.FindView("StrengthAbsorbsItemPercentageBox", out TextInputView strengthInput);
                    window.FindView("BioRegrowthPercentageBox", out TextInputView bioRegrowthPercentageInput);
                    window.FindView("BioRegrowthDelayBox", out TextInputView bioRegrowthDelayInput);

                    if (bioCocoonInput != null && !string.IsNullOrEmpty(bioCocoonInput.Text))
                        if (int.TryParse(bioCocoonInput.Text, out int bioCocoonValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage != bioCocoonValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage = bioCocoonValue;

                    if (stimTargetInput != null)
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetName != stimTargetInput.Text)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetName = stimTargetInput.Text;

                    if (stimHealthInput != null && !string.IsNullOrEmpty(stimHealthInput.Text))
                        if (int.TryParse(stimHealthInput.Text, out int stimHealthValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentage != stimHealthValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentage = stimHealthValue;

                    if (stimNanoInput != null && !string.IsNullOrEmpty(stimNanoInput.Text))
                        if (int.TryParse(stimNanoInput.Text, out int stimNanoValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentage != stimNanoValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentage = stimNanoValue;

                    if (kitHealthInput != null && !string.IsNullOrEmpty(kitHealthInput.Text))
                        if (int.TryParse(kitHealthInput.Text, out int kitHealthValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage != kitHealthValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage = kitHealthValue;

                    if (kitNanoInput != null && !string.IsNullOrEmpty(kitNanoInput.Text))
                        if (int.TryParse(kitNanoInput.Text, out int kitNanoValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage != kitNanoValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage = kitNanoValue;

                    if (sphereInput != null && !string.IsNullOrEmpty(sphereInput.Text))
                        if (int.TryParse(sphereInput.Text, out int sphereValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelay != sphereValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelay = sphereValue;

                    if (witOfTheAtroxInput != null && !string.IsNullOrEmpty(witOfTheAtroxInput.Text))
                        if (int.TryParse(witOfTheAtroxInput.Text, out int witOfTheAtroxValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelay != witOfTheAtroxValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelay = witOfTheAtroxValue;

                    if (selfHealInput != null && !string.IsNullOrEmpty(selfHealInput.Text))
                        if (int.TryParse(selfHealInput.Text, out int selfHealValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentage != selfHealValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentage = selfHealValue;

                    if (selfNanoInput != null && !string.IsNullOrEmpty(selfNanoInput.Text))
                        if (int.TryParse(selfNanoInput.Text, out int selfNanoValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentage != selfNanoValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentage = selfNanoValue;

                    if (teamHealInput != null && !string.IsNullOrEmpty(teamHealInput.Text))
                        if (int.TryParse(teamHealInput.Text, out int teamHealValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentage != teamHealValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentage = teamHealValue;

                    if (teamNanoInput != null && !string.IsNullOrEmpty(teamNanoInput.Text))
                        if (int.TryParse(teamNanoInput.Text, out int teamNanoValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentage != teamNanoValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentage = teamNanoValue;

                    if (bodyDevInput != null && !string.IsNullOrEmpty(bodyDevInput.Text))
                        if (int.TryParse(bodyDevInput.Text, out int bodyDevValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentage != bodyDevValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentage = bodyDevValue;

                    if (strengthInput != null && !string.IsNullOrEmpty(strengthInput.Text))
                        if (int.TryParse(strengthInput.Text, out int strengthValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentage != strengthValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentage = strengthValue;

                    if (bioRegrowthPercentageInput != null && !string.IsNullOrEmpty(bioRegrowthPercentageInput.Text))
                        if (int.TryParse(bioRegrowthPercentageInput.Text, out int bioRegrowthPercentageValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].BioRegrowthPercentage != bioRegrowthPercentageValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].BioRegrowthPercentage = bioRegrowthPercentageValue;

                    if (bioRegrowthDelayInput != null && !string.IsNullOrEmpty(bioRegrowthDelayInput.Text))
                        if (int.TryParse(bioRegrowthDelayInput.Text, out int bioRegrowthDelayValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleBioRegrowthPerkDelay != bioRegrowthDelayValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleBioRegrowthPerkDelay = bioRegrowthDelayValue;

                    //attack
                    if (window.FindView("CombatHandlerPetAttack", out Button PetAttack))
                    {
                        PetAttack.Tag = window;
                        PetAttack.Clicked = PetAttackClicked;
                    }

                    //wait
                    if (window.FindView("CombatHandlerPetWait", out Button PetWait))
                    {
                        PetWait.Tag = window;
                        PetWait.Clicked = PetWaitClicked;
                    }

                    //warp
                    if (window.FindView("CombatHandlerPetWarp", out Button PetWarp))
                    {
                        PetWarp.Tag = window;
                        PetWarp.Clicked = PetWarpClicked;
                    }

                    //follow
                    if (window.FindView("CombatHandlerPetFollow", out Button PetFollow))
                    {
                        PetFollow.Tag = window;
                        PetFollow.Clicked = PetFollowClicked;
                    }
                }


                if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
                {
                    if (SettingsController.settingsWindow.FindView("ItemsView", out Button itemView))
                    {
                        itemView.Tag = SettingsController.settingsWindow;
                        itemView.Clicked = HandleItemViewClick;
                    }

                    if (SettingsController.settingsWindow.FindView("PerksView", out Button perkView))
                    {
                        perkView.Tag = SettingsController.settingsWindow;
                        perkView.Clicked = HandlePerkViewClick;
                    }

                    if (SettingsController.settingsWindow.FindView("PetsView", out Button petView))
                    {
                        petView.Tag = SettingsController.settingsWindow;
                        petView.Clicked = HandlePetViewClick;
                    }

                    if (SettingsController.settingsWindow.FindView("PetCommandView", out Button petCommandView))
                    {
                        petCommandView.Tag = SettingsController.settingsWindow;
                        petCommandView.Clicked = HandlePetCommandViewClick;
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

                    if (!_settings["SyncPets"].AsBool() && _syncPets)
                    {
                        IPCChannel.Broadcast(new PetSyncOffMessage());
                        Chat.WriteLine("SyncPets disabled");
                        syncPetsOffDisabled();
                    }

                    if (_settings["SyncPets"].AsBool() && !_syncPets)
                    {
                        IPCChannel.Broadcast(new PetSyncOnMessag());
                        Chat.WriteLine("SyncPets enabled.");
                        syncPetsOnEnabled();
                    }

                    #endregion

                    #region GlobalBuffing

                    if (!_settings["GlobalBuffing"].AsBool() && ToggleBuffing)
                    {
                        IPCChannel.Broadcast(new GlobalBuffingMessage()
                        {
                            Switch = false
                        });

                        ToggleBuffing = false;
                        _settings["Buffing"] = false;
                        _settings["GlobalBuffing"] = false;
                    }

                    if (_settings["GlobalBuffing"].AsBool() && !ToggleBuffing)
                    {
                        IPCChannel.Broadcast(new GlobalBuffingMessage()
                        {
                            Switch = true
                        });

                        ToggleBuffing = true;
                        _settings["Buffing"] = true;
                        _settings["GlobalBuffing"] = true;
                    }

                    #endregion

                    #region Global Composites

                    if (!_settings["GlobalComposites"].AsBool() && ToggleComposites)
                    {
                        IPCChannel.Broadcast(new GlobalCompositesMessage()
                        {
                            Switch = false
                        });

                        ToggleComposites = false;
                        _settings["Composites"] = false;
                        _settings["GlobalComposites"] = false;
                    }
                    if (_settings["GlobalComposites"].AsBool() && !ToggleComposites)
                    {
                        IPCChannel.Broadcast(new GlobalCompositesMessage()
                        {
                            Switch = true
                        });

                        ToggleComposites = true;
                        _settings["Composites"] = true;
                        _settings["GlobalComposites"] = true;
                    }

                    #endregion

                    #region Global Resurrection

                    if (!_settings["GlobalRez"].AsBool() && ToggleRez)
                    {
                        IPCChannel.Broadcast(new GlobalRezMessage()
                        {

                            Switch = false
                        });

                        ToggleRez = false;
                        _settings["GlobalRez"] = false;
                    }
                    if (_settings["GlobalRez"].AsBool() && !ToggleRez)
                    {
                        IPCChannel.Broadcast(new GlobalRezMessage()
                        {
                            Switch = true
                        });

                        ToggleRez = true;
                        _settings["GlobalRez"] = true;
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                var errorMessage = "An error occurred on line " + GetLineNumber(ex) + ": " + ex.Message;

                if (errorMessage != previousErrorMessage)
                {
                    Chat.WriteLine(errorMessage);
                    Chat.WriteLine("Stack Trace: " + ex.StackTrace);
                    previousErrorMessage = errorMessage;
                }
            }
        }

        #region Buffs

        private bool Grenade(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam && IsSettingEnabled("GrenadeTeam"))
                return TeamBuffExclusionWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade)
                        || TeamBuffExclusionWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);

            if (DynelManager.LocalPlayer.Buffs.Contains(269482)) { return false; }

            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade)
                    || BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (InitBuffSelection.None == (InitBuffSelection)_settings["InitBuffSelection"].AsInt32()
                || !GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(CharacterWieldedWeapon.Ranged)) { return false; }

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

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        #endregion

        #region Pets

        #region Pet Spawners

        private bool CastPets(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 0) { return false; }

            if (PetSpawner(PetsList.Pets, spell, fightingTarget, ref actionTarget))
            {
                ResetTrimmers();

                return true;
            }
            return false;
        }

        #endregion

        #region Buffs

        private bool PetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(spell.Nanoline, PetType.Attack, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(spell.Nanoline, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool SettingPetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, string settingName)
        {
            if (!_settings[settingName].AsBool()) { return false; }

            return PetTargetBuff(spell.Nanoline, PetType.Attack, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(spell.Nanoline, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool ShieldOfTheObedientServant(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            if (!CanCast(spell)) { return false; }

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

        #endregion

        #region Warp

        private bool PetWarp(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("WarpPets") || !CanCast(spell) || !CanLookupPetsAfterZone()) { return false; }

            return DynelManager.LocalPlayer.Pets.Any(c => c.Character == null);
        }

        #endregion

        #region Healing

        private bool PetHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("HealPets") || !CanLookupPetsAfterZone()) { return false; }

            if (!CanCast(spell)) { return false; }

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

        #region Auras

        private bool GenericAuraBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, BuffingAuraSelection auraType)
        {
            BuffingAuraSelection currentSetting = (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32();

            if (currentSetting != auraType)
            {
                return false;
            }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        private bool GenericDebuffingAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget)
            actionTarget, DebuffingAuraSelection debuffType)
        {
            DebuffingAuraSelection currentSetting = (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32();

            if (currentSetting != debuffType || fightingTarget == null)
            {
                return false;
            }

            switch (debuffType)
            {
                case DebuffingAuraSelection.Blind:
                case DebuffingAuraSelection.ShieldRipper:
                    return CheckDebuffCondition();

                case DebuffingAuraSelection.PetSnare:
                    return SpamSnare(spell.Nanoline, spell, fightingTarget, ref actionTarget);

                default:
                    return false;
            }
        }

        private bool SpamSnare(NanoLine buffNanoLine, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if ( fightingTarget == null) { return false; }
            
            if (!CanCast(spell)) { return false; }

            if (!spell.IsReady) { return false; }

            Pet target = DynelManager.LocalPlayer.Pets
                    .Where(c => c.Type == PetType.Attack)
                    .FirstOrDefault();

            if (target != null && target.Character != null)
            {
                actionTarget.Target = target.Character;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        private bool CheckDebuffCondition()
        {
            return DynelManager.NPCs.Any(c => c.Health > 0
                && c.FightingTarget?.Buffs.Contains(202732) == false && c.FightingTarget?.Buffs.Contains(214879) == false
                && c.FightingTarget?.Buffs.Contains(284620) == false && c.FightingTarget?.Buffs.Contains(216382) == false
                && c.FightingTarget?.IsPet == false
                && c.Position.DistanceFrom(DynelManager.LocalPlayer.Position) <= 9f);
        }

        private bool AuraCancellation(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            // This already checks if fightingTarget is null
            if (fightingTarget != null) { return false; }

            // Check if DynelManager.LocalPlayer.Pets is null or if it contains a null pet
            if (DynelManager.LocalPlayer.Pets == null) { return false; }

            // Safely search for the pet and then the character
            var pet = DynelManager.LocalPlayer.Pets
                .FirstOrDefault(c => c != null && c.Character != null && c.Character.Buffs.Contains(NanoLine.EngineerPetAOESnareBuff));

            // Check if the pet was found and if its Character is not null
            if (pet != null && pet.Character != null)
            {
                actionTarget.Target = pet.Character;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }


        #endregion

        #region Proc

        private bool GenericPetProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, PetProcSelection petProcSelection)
        {
            if (!CanCast(spell)) { return false; }

            PetProcSelection currentSetting = (PetProcSelection)_settings["PetProcSelection"].AsInt32();

            if (currentSetting != petProcSelection || !CanLookupPetsAfterZone() || !IsSettingEnabled("BuffPets"))
            {
                return false;
            }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (!pet.Character.Buffs.Contains(NanoLine.SiphonBox683)
                    && (pet.Type == PetType.Attack || pet.Type == PetType.Support))
                {
                    if (spell.IsReady)
                    {
                        spell.Cast(pet.Character, true);
                    }
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Perks

        private bool GenericPetPerk(PerkAction perkAction, SimpleChar fightingTarget,
                     ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, PetPerkSelection petPerkSelection)
        {
            PetPerkSelection currentSetting = (PetPerkSelection)_settings["PetPerkSelection"].AsInt32();

            if (currentSetting == PetPerkSelection.None || currentSetting != petPerkSelection
                || !CanLookupPetsAfterZone() || !IsSettingEnabled("BuffPets"))
            {
                return false;
            }

            int[] relevantNano;
            switch (currentSetting)
            {
                case PetPerkSelection.TauntBox:
                    relevantNano = RelevantNanos.PerkTauntBox;
                    break;
                case PetPerkSelection.ChaoticBox:
                    relevantNano = RelevantNanos.PerkChaoticBox;
                    break;
                case PetPerkSelection.SiphonBox:
                    relevantNano = RelevantNanos.PerkSiphonBox;
                    break;
                default:
                    return false;
            }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null)
                {
                    continue;
                }

                if (!relevantNano.Any(nano => pet.Character.Buffs.Contains(nano)))
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

        private bool PetTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget,
                        string settingName, Func<Pet, bool> canTrimFunc, Action<PetType> updateStatus, Action<PetType> updateTime)
        {

            if (!IsSettingEnabled(settingName) || !CanLookupPetsAfterZone() || !CanTrim())
            {
                return false;
            }

            if (IsTimeForNextTrim(settingName, PetType.Attack) || IsTimeForNextTrim(settingName, PetType.Support))
            {
                return false;
            }

            Pet _attackPet = DynelManager.LocalPlayer.Pets
                .FirstOrDefault(c => c.Character != null && c.Type == PetType.Attack && canTrimFunc(c));

            Pet _supportPet = DynelManager.LocalPlayer.Pets
                .FirstOrDefault(c => c.Character != null && c.Type == PetType.Support && canTrimFunc(c));

            if (_attackPet != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _attackPet.Character;
                updateStatus(PetType.Attack);
                updateTime(PetType.Attack);
                return true;
            }

            if (_supportPet != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _supportPet.Character;
                updateStatus(PetType.Support);
                updateTime(PetType.Support);
                return true;
            }

            return false;
        }

        private bool IsTimeForNextTrim(string settingName, PetType petType)
        {
            double currentTime = Time.NormalTime;
            double lastTrimTime = 0;
            if (settingName == "DivertHpTrimmer")
            {
                lastTrimTime = _lastPetTrimDivertHpTime[petType];
            }
            else if (settingName == "DivertOffTrimmer")
            {
                lastTrimTime = _lastPetTrimDivertOffTime[petType];
            }
            return currentTime - lastTrimTime < DelayBetweenDiverTrims;
        }

        #endregion

        #endregion

        #region Misc

        private bool SnareMobExists()
        {
            return DynelManager.NPCs
                .Where(c => c.Name == "Flaming Vengeance" ||
                    c.Name == "Hand of the Colonel" || c.Name == "Punching Bag")
                .Any();
        }

        private bool CanTrim()
        {
            return _lastTrimTime + 1 < Time.NormalTime;
        }

        private bool CanDivertOffTrim(Pet pet)
        {
            return _lastPetTrimDivertOffTime[pet.Type] + DelayBetweenDiverTrims < Time.NormalTime || !petTrimmedOffDiv[pet.Type];
        }

        private bool CanDivertHpTrim(Pet pet)
        {
            return _lastPetTrimDivertHpTime[pet.Type] + DelayBetweenDiverTrims < Time.NormalTime || !petTrimmedHpDiv[pet.Type];
        }


        private bool CanAggDefTrim(Pet pet)
        {

            return !petTrimmedAggDef[pet.Type];
        }

        private bool CanTauntTrim(Pet pet)
        {
            return !petTrimmedAggressive[pet.Type];
        }

        private void ResetTrimmers()
        {
            petTrimmedAggressive[PetType.Attack] = false;
            petTrimmedAggressive[PetType.Support] = false;
            petTrimmedOffDiv[PetType.Attack] = false;
            petTrimmedOffDiv[PetType.Support] = false;
            petTrimmedHpDiv[PetType.Attack] = false;
            petTrimmedHpDiv[PetType.Support] = false;
            petTrimmedAggDef[PetType.Attack] = false;
            petTrimmedAggDef[PetType.Support] = false;
        }

        private void OnZoned(object s, EventArgs e)
        {
            _lastZonedTime = Time.NormalTime;
            _lastCombatTime = double.MinValue;
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

        private bool ShouldCancelHostileAuras()
        {
            return Time.NormalTime - _lastCombatTime > 5;
        }


        private static void PetWaitCommand(string command, string[] param, ChatWindow chatWindow)
        {
            IPCChannel.Broadcast(new PetWaitMessage());
            OnPetWait(0, null);
        }

        private static void PetWarpCommand(string command, string[] param, ChatWindow chatWindow)
        {
            IPCChannel.Broadcast(new PetWarpMessage());
            OnPetWarp(0, null);
        }

        private void PetFollowCommand(string command, string[] param, ChatWindow chatWindow)
        {
            IPCChannel.Broadcast(new PetFollowMessage());
            OnPetFollow(0, null);
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
            //public const int PetHealingGreater = 270351;
            public const int PetWarp = 209488;
            public static readonly int[] Warps = {
                209488
            };

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
            //public static readonly int[] PetHealing = { 116791, 116795, 116796, 116792, 116797, 116794, 116793 };
            public static readonly int[] ShieldOfObedientServant = { 270790, 202260 };
            public static readonly int[] EngineeringBuff = { 273346, 227667, 227657 };

        }

        private static class RelevantTrimmers
        {
            public static readonly int[] IncreaseAggressiveness = { 154940, 154939 }; // Mech. Engi
            public static readonly int[] PositiveAggressiveDefensive = { 88384, 88383 }; // Mech. Engi
            public static readonly int[] DivertEnergyToHitpoints = { 88382, 88381 }; // Lock skill Elec. Engi for 5m.
            public static readonly int[] DivertEnergyToOffense = { 88378, 88377 }; // Lock skill Mech. Engi for 5m.

        }

        public enum PetPerkSelection
        {
            None, TauntBox, ChaoticBox, SiphonBox
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
        public enum InitBuffSelection
        {
            None, Self, Team
        }
       
        public enum ProcType1Selection
        {
            ReactiveArmor = 1146377031,
            DestructiveTheorem = 1380274768,
            EnergyTransfer = 1145654611,
            EndureBarrage = 1146245699,
            DestructiveSignal = 1095717441,
            SplinterPreservation = 1162171474,
            CushionBlows = 1146242392
        }

        public enum ProcType2Selection
        {
            AssaultForceRelief = 1380995154,
            DroneMissiles = 1145394248,
            DroneExplosives = 1112425541,
            CongenialEncasement = 1381254213,
            PersonalProtection = 1145395030
        }

        #endregion

    }
}
