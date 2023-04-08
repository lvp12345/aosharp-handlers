using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CombatHandler.Engineer
{
    class EngiCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        //private static bool ToggleDebuffing = false;

        public static bool _syncPets;

        //private const float DelayBetweenTrims = 1;
        private const float DelayBetweenDiverTrims = 305;

        //private bool petTrimmedAggressive = false;

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
        private static Window _buffWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;

        private static View _buffView;
        private static View _petView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;

        private double _lastTrimTime = 0;

        private static double _ncuUpdateTime;

        public EngiCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
            //IPCChannel.RegisterCallback((int)IPCOpcode.GlobalDebuffing, OnGlobalDebuffingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.PetSyncOn, SyncPetsOnMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.PetSyncOff, SyncPetsOffMessage);

            Config.CharSettings[Game.ClientInst].BioCocoonPercentageChangedEvent += BioCocoonPercentage_Changed;
            Config.CharSettings[Game.ClientInst].StimTargetNameChangedEvent += StimTargetName_Changed;
            Config.CharSettings[Game.ClientInst].StimHealthPercentageChangedEvent += StimHealthPercentage_Changed;
            Config.CharSettings[Game.ClientInst].StimNanoPercentageChangedEvent += StimNanoPercentage_Changed;
            Config.CharSettings[Game.ClientInst].KitHealthPercentageChangedEvent += KitHealthPercentage_Changed;
            Config.CharSettings[Game.ClientInst].KitNanoPercentageChangedEvent += KitNanoPercentage_Changed;
            Config.CharSettings[Game.ClientInst].CycleSpherePerkDelayChangedEvent += CycleSpherePerkDelay_Changed;
            Config.CharSettings[Game.ClientInst].CycleWitOfTheAtroxPerkDelayChangedEvent += CycleWitOfTheAtroxPerkDelay_Changed;
            Config.CharSettings[Game.ClientInst].SelfHealPerkPercentageChangedEvent += SelfHealPerkPercentage_Changed;
            Config.CharSettings[Game.ClientInst].SelfNanoPerkPercentageChangedEvent += SelfNanoPerkPercentage_Changed;
            Config.CharSettings[Game.ClientInst].TeamHealPerkPercentageChangedEvent += TeamHealPerkPercentage_Changed;
            Config.CharSettings[Game.ClientInst].TeamNanoPerkPercentageChangedEvent += TeamNanoPerkPercentage_Changed;
            Config.CharSettings[Game.ClientInst].BodyDevAbsorbsItemPercentageChangedEvent += BodyDevAbsorbsItemPercentage_Changed;
            Config.CharSettings[Game.ClientInst].StrengthAbsorbsItemPercentageChangedEvent += StrengthAbsorbsItemPercentage_Changed;
            Config.CharSettings[Game.ClientInst].BioRegrowthPercentageChangedEvent += BioRegrowthPercentage_Changed;
            Config.CharSettings[Game.ClientInst].CycleBioRegrowthPerkDelayChangedEvent += CycleBioRegrowthPerkDelay_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("GlobalBuffing", true);
            _settings.AddVariable("GlobalComposites", true);
            //_settings.AddVariable("GlobalDebuffs", true);

            _settings.AddVariable("SharpObjects", true);
            _settings.AddVariable("Grenades", true);

            _settings.AddVariable("StimTargetSelection", (int)StimTargetSelection.Self);

            _settings.AddVariable("Kits", true);

            _settings.AddVariable("SyncPets", true);
            _settings.AddVariable("SpawnPets", true);
            _settings.AddVariable("BuffPets", true);
            _settings.AddVariable("HealPets", false);
            _settings.AddVariable("WarpPets", false);

            _settings.AddVariable("PetDefensiveNanos", false);
            _settings.AddVariable("PetArmorBuff", false);

            Game.TeleportEnded += OnZoned;

            _settings.AddVariable("TauntTrimmer", false);
            _settings.AddVariable("AggDefTrimmer", false);
            _settings.AddVariable("DivertHpTrimmer", false);
            _settings.AddVariable("DivertOffTrimmer", false);

            _settings.AddVariable("InitBuffSelection", (int)InitBuffSelection.Self);
            _settings.AddVariable("TeamArmorBuff", true);
            _settings.AddVariable("PistolTeam", true);
            _settings.AddVariable("GrenadeTeam", true);
            _settings.AddVariable("ShadowlandReflectBase", true);
            _settings.AddVariable("DamageShields", false);
            _settings.AddVariable("MEBuff", false);

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

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(), TeamArmorBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolTeam);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.GrenadeBuffs).OrderByStackingOrder(), Grenade);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadowlandReflectBase).OrderByStackingOrder(), ShadowlandReflectBase);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(), DamageShields);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SpecialAttackAbsorberBase).OrderByStackingOrder(), GlobalGenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerSpecialAttackAbsorber).OrderByStackingOrder(), GlobalGenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);

            RegisterSpellProcessor(RelevantNanos.BoostedTendons, GlobalGenericBuff);
            RegisterSpellProcessor(RelevantNanos.DamageBuffLineA, GlobalGenericTeamBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.GeneralMechanicalEngineeringBuff).OrderByStackingOrder(), GlobalGenericBuff);
            RegisterSpellProcessor(RelevantNanos.EngineeringBuff, EngineeringBuff);

            //Pets

            //Pet Spawners
            RegisterSpellProcessor(PetsList.Pets.Where(c => c.Value.PetType == PetType.Attack).Select(c => c.Key).ToArray(), CastPets);
            RegisterSpellProcessor(PetsList.Pets.Where(c => c.Value.PetType == PetType.Support).Select(c => c.Key).ToArray(), CastPets);

            //Pet Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerMiniaturization).OrderByStackingOrder(), PetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetShortTermDamageBuffs).OrderByStackingOrder(), PetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPPetInitiativeBuffs).OrderByStackingOrder(), PetBuff);
            RegisterSpellProcessor(RelevantNanos.DamageBuffLineA, PetBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(), PetArmorBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDefensiveNanos).OrderByStackingOrder(), PetDefensiveNanos);

            RegisterSpellProcessor(RelevantNanos.ShieldOfObedientServant, ShieldOfTheObedientServant);

            RegisterSpellProcessor(RelevantNanos.MastersBidding, MastersBidding);
            RegisterSpellProcessor(RelevantNanos.SedativeInjectors, SedativeInjectors);

            RegisterSpellProcessor(RelevantNanos.PetCleanse, PetCleanse);

            RegisterSpellProcessor(RelevantNanos.PetWarp, PetWarp, CombatActionPriority.High);

            //Pet heals
            RegisterSpellProcessor(RelevantNanos.PetHealing, PetHealing);
            RegisterSpellProcessor(RelevantNanos.PetHealingGreater, PetHealingGreater);

            //Pet Aura
            RegisterSpellProcessor(RelevantNanos.Blinds, BlindAura);
            RegisterSpellProcessor(RelevantNanos.ShieldRippers, ShieldRipperAura);
            RegisterSpellProcessor(RelevantNanos.ArmorAura, ArmorAura);
            RegisterSpellProcessor(RelevantNanos.DamageAura, DamageAura);
            RegisterSpellProcessor(RelevantNanos.ReflectAura, ReflectAura);
            RegisterSpellProcessor(RelevantNanos.ShieldAura, ShieldAura);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerPetAOESnareBuff).OrderByStackingOrder(), SnareAura);
            //RegisterSpellProcessor(RelevantNanos.IntrusiveAuraCancellation, AuraCancellation);

            //Pet Perks
            RegisterPerkProcessor(PerkHash.TauntBox, TauntBox);
            RegisterPerkProcessor(PerkHash.ChaoticEnergy, ChaoticBox);
            RegisterPerkProcessor(PerkHash.SiphonBox, SiphonBox);

            ////Pet Items
            //RegisterItemProcessor(RelevantTrimmers.PositiveAggressiveDefensive, RelevantTrimmers.PositiveAggressiveDefensive, PetAggDefTrimmer);
            //RegisterItemProcessor(new int[] { RelevantTrimmers.DivertEnergyToHitpointsLow, RelevantTrimmers.DivertEnergyToHitpointsHigh }, PetDivertHpTrimmer);
            //RegisterItemProcessor(new int[] { RelevantTrimmers.DivertEnergyToOffenseLow, RelevantTrimmers.DivertEnergyToOffenseHigh }, PetDivertOffTrimmer);
            //RegisterItemProcessor(new int[] { RelevantTrimmers.IncreaseAggressivenessLow, RelevantTrimmers.IncreaseAggressivenessHigh }, PetAggressiveTrimmer);

            //Pet Trimmers
            RegisterItemProcessor(RelevantTrimmers.IncreaseAggressiveness, PetAggressiveTrimmer);
            RegisterItemProcessor(RelevantTrimmers.PositiveAggressiveDefensive, PetAggDefTrimmer);
            RegisterItemProcessor(RelevantTrimmers.DivertEnergyToHitpoints, PetDivertHpTrimmer);
            RegisterItemProcessor(RelevantTrimmers.DivertEnergyToOffense, PetDivertOffTrimmer);

            ResetTrimmers();

            PluginDirectory = pluginDir;

            BioCocoonPercentage = Config.CharSettings[Game.ClientInst].BioCocoonPercentage;
            StimTargetName = Config.CharSettings[Game.ClientInst].StimTargetName;
            StimHealthPercentage = Config.CharSettings[Game.ClientInst].StimHealthPercentage;
            StimNanoPercentage = Config.CharSettings[Game.ClientInst].StimNanoPercentage;
            KitHealthPercentage = Config.CharSettings[Game.ClientInst].KitHealthPercentage;
            KitNanoPercentage = Config.CharSettings[Game.ClientInst].KitNanoPercentage;
            CycleSpherePerkDelay = Config.CharSettings[Game.ClientInst].CycleSpherePerkDelay;
            CycleWitOfTheAtroxPerkDelay = Config.CharSettings[Game.ClientInst].CycleWitOfTheAtroxPerkDelay;
            SelfHealPerkPercentage = Config.CharSettings[Game.ClientInst].SelfHealPerkPercentage;
            SelfNanoPerkPercentage = Config.CharSettings[Game.ClientInst].SelfNanoPerkPercentage;
            TeamHealPerkPercentage = Config.CharSettings[Game.ClientInst].TeamHealPerkPercentage;
            TeamNanoPerkPercentage = Config.CharSettings[Game.ClientInst].TeamNanoPerkPercentage;
            BodyDevAbsorbsItemPercentage = Config.CharSettings[Game.ClientInst].BodyDevAbsorbsItemPercentage;
            StrengthAbsorbsItemPercentage = Config.CharSettings[Game.ClientInst].StrengthAbsorbsItemPercentage;
            BioRegrowthPercentage = Config.CharSettings[Game.ClientInst].BioRegrowthPercentage;
            CycleBioRegrowthPerkDelay = Config.CharSettings[Game.ClientInst].CycleBioRegrowthPerkDelay;
        }

        public Window[] _windows => new Window[] { _petWindow, _buffWindow, _procWindow, _itemWindow, _perkWindow };

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

        //private void OnGlobalDebuffingMessage(int sender, IPCMessage msg)
        //{
        //    GlobalDebuffingMessage debuffMsg = (GlobalDebuffingMessage)msg;

        //    _settings[$"Debuffing"] = debuffMsg.Switch;
        //    _settings[$"Debuffing"] = debuffMsg.Switch;
        //}

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
            base.OnUpdate(deltaTime);

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
                        if (Config.CharSettings[Game.ClientInst].BioCocoonPercentage != bioCocoonValue)
                            Config.CharSettings[Game.ClientInst].BioCocoonPercentage = bioCocoonValue;

                if (stimTargetInput != null)
                    if (Config.CharSettings[Game.ClientInst].StimTargetName != stimTargetInput.Text)
                        Config.CharSettings[Game.ClientInst].StimTargetName = stimTargetInput.Text;

                if (stimHealthInput != null && !string.IsNullOrEmpty(stimHealthInput.Text))
                    if (int.TryParse(stimHealthInput.Text, out int stimHealthValue))
                        if (Config.CharSettings[Game.ClientInst].StimHealthPercentage != stimHealthValue)
                            Config.CharSettings[Game.ClientInst].StimHealthPercentage = stimHealthValue;

                if (stimNanoInput != null && !string.IsNullOrEmpty(stimNanoInput.Text))
                    if (int.TryParse(stimNanoInput.Text, out int stimNanoValue))
                        if (Config.CharSettings[Game.ClientInst].StimNanoPercentage != stimNanoValue)
                            Config.CharSettings[Game.ClientInst].StimNanoPercentage = stimNanoValue;

                if (kitHealthInput != null && !string.IsNullOrEmpty(kitHealthInput.Text))
                    if (int.TryParse(kitHealthInput.Text, out int kitHealthValue))
                        if (Config.CharSettings[Game.ClientInst].KitHealthPercentage != kitHealthValue)
                            Config.CharSettings[Game.ClientInst].KitHealthPercentage = kitHealthValue;

                if (kitNanoInput != null && !string.IsNullOrEmpty(kitNanoInput.Text))
                    if (int.TryParse(kitNanoInput.Text, out int kitNanoValue))
                        if (Config.CharSettings[Game.ClientInst].KitNanoPercentage != kitNanoValue)
                            Config.CharSettings[Game.ClientInst].KitNanoPercentage = kitNanoValue;

                if (sphereInput != null && !string.IsNullOrEmpty(sphereInput.Text))
                    if (int.TryParse(sphereInput.Text, out int sphereValue))
                        if (Config.CharSettings[Game.ClientInst].CycleSpherePerkDelay != sphereValue)
                            Config.CharSettings[Game.ClientInst].CycleSpherePerkDelay = sphereValue;

                if (witOfTheAtroxInput != null && !string.IsNullOrEmpty(witOfTheAtroxInput.Text))
                    if (int.TryParse(witOfTheAtroxInput.Text, out int witOfTheAtroxValue))
                        if (Config.CharSettings[Game.ClientInst].CycleWitOfTheAtroxPerkDelay != witOfTheAtroxValue)
                            Config.CharSettings[Game.ClientInst].CycleWitOfTheAtroxPerkDelay = witOfTheAtroxValue;

                if (selfHealInput != null && !string.IsNullOrEmpty(selfHealInput.Text))
                    if (int.TryParse(selfHealInput.Text, out int selfHealValue))
                        if (Config.CharSettings[Game.ClientInst].SelfHealPerkPercentage != selfHealValue)
                            Config.CharSettings[Game.ClientInst].SelfHealPerkPercentage = selfHealValue;

                if (selfNanoInput != null && !string.IsNullOrEmpty(selfNanoInput.Text))
                    if (int.TryParse(selfNanoInput.Text, out int selfNanoValue))
                        if (Config.CharSettings[Game.ClientInst].SelfNanoPerkPercentage != selfNanoValue)
                            Config.CharSettings[Game.ClientInst].SelfNanoPerkPercentage = selfNanoValue;

                if (teamHealInput != null && !string.IsNullOrEmpty(teamHealInput.Text))
                    if (int.TryParse(teamHealInput.Text, out int teamHealValue))
                        if (Config.CharSettings[Game.ClientInst].TeamHealPerkPercentage != teamHealValue)
                            Config.CharSettings[Game.ClientInst].TeamHealPerkPercentage = teamHealValue;

                if (teamNanoInput != null && !string.IsNullOrEmpty(teamNanoInput.Text))
                    if (int.TryParse(teamNanoInput.Text, out int teamNanoValue))
                        if (Config.CharSettings[Game.ClientInst].TeamNanoPerkPercentage != teamNanoValue)
                            Config.CharSettings[Game.ClientInst].TeamNanoPerkPercentage = teamNanoValue;

                if (bodyDevInput != null && !string.IsNullOrEmpty(bodyDevInput.Text))
                    if (int.TryParse(bodyDevInput.Text, out int bodyDevValue))
                        if (Config.CharSettings[Game.ClientInst].BodyDevAbsorbsItemPercentage != bodyDevValue)
                            Config.CharSettings[Game.ClientInst].BodyDevAbsorbsItemPercentage = bodyDevValue;

                if (strengthInput != null && !string.IsNullOrEmpty(strengthInput.Text))
                    if (int.TryParse(strengthInput.Text, out int strengthValue))
                        if (Config.CharSettings[Game.ClientInst].StrengthAbsorbsItemPercentage != strengthValue)
                            Config.CharSettings[Game.ClientInst].StrengthAbsorbsItemPercentage = strengthValue;

                if (bioRegrowthPercentageInput != null && !string.IsNullOrEmpty(bioRegrowthPercentageInput.Text))
                    if (int.TryParse(bioRegrowthPercentageInput.Text, out int bioRegrowthPercentageValue))
                        if (Config.CharSettings[Game.ClientInst].BioRegrowthPercentage != bioRegrowthPercentageValue)
                            Config.CharSettings[Game.ClientInst].BioRegrowthPercentage = bioRegrowthPercentageValue;

                if (bioRegrowthDelayInput != null && !string.IsNullOrEmpty(bioRegrowthDelayInput.Text))
                    if (int.TryParse(bioRegrowthDelayInput.Text, out int bioRegrowthDelayValue))
                        if (Config.CharSettings[Game.ClientInst].CycleBioRegrowthPerkDelay != bioRegrowthDelayValue)
                            Config.CharSettings[Game.ClientInst].CycleBioRegrowthPerkDelay = bioRegrowthDelayValue;
            }

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

                if (!_settings["SyncPets"].AsBool() && _syncPets) // Farming off
                {
                    IPCChannel.Broadcast(new PetSyncOffMessage());
                    Chat.WriteLine("SyncPets disabled");
                    syncPetsOffDisabled();
                }

                if (_settings["SyncPets"].AsBool() && !_syncPets) // farming on
                {
                    IPCChannel.Broadcast(new PetSyncOnMessag());
                    Chat.WriteLine("SyncPets enabled.");
                    syncPetsOnEnabled();
                }

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

                #region Global Debuffing

                //if (!_settings["GlobalDebuffing"].AsBool() && ToggleDebuffing)
                //{
                //    IPCChannel.Broadcast(new GlobalDebuffingMessage()
                //    {

                //        Switch = false
                //    });

                //    ToggleDebuffing = false;
                //    _settings["GlobalDebuffing"] = false;
                //}
                //if (_settings["GlobalDebuffing"].AsBool() && !ToggleDebuffing)
                //{
                //    IPCChannel.Broadcast(new GlobalDebuffingMessage()
                //    {
                //        Switch = true
                //    });

                //    ToggleDebuffing = true;
                //    _settings["GlobalDebuffing"] = true;
                //}

                #endregion

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

        #region Buffs

        private bool TeamArmorBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("TeamArmorBuff"))
                return GenericTeamBuff(spell, ref actionTarget);

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool PistolTeam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam && IsSettingEnabled("PistolTeam"))
                return TeamBuffExclusionWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);

            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        private bool Grenade(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam && IsSettingEnabled("GrenadeTeam"))
                return TeamBuffExclusionWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade)
                        || TeamBuffExclusionWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);

            if (DynelManager.LocalPlayer.Buffs.Contains(269482)) { return false; }

            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade)
                    || BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        private bool ShadowlandReflectBase(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("ShadowlandReflectBase")) { return false; }

            return GenericBuff(spell, ref actionTarget);
        }

        private bool DamageShields(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("DamageShields"))
                return GenericTeamBuff(spell, ref actionTarget);

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
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

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool EngineeringBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("MEBuff") || DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.EngineeringBuff)) { return false; }

            return GenericBuff(spell, ref actionTarget);
        }

        #endregion

        #region Pets

        #region Pet Spawners

        private bool CastPets(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 0) { return false; }

            if (PetSpawner(PetsList.Pets, spell, fightingTarget, ref actionTarget))
            {
                ResetTrimmers();

                if (!CanCast(spell)) { return false; }

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

        private bool PetArmorBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("PetArmorBuff")) { return false; }

            return PetTargetBuff(spell.Nanoline, PetType.Attack, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(spell.Nanoline, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool PetDefensiveNanos(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("PetDefensiveNanos")) { return false; }

            return PetTargetBuff(spell.Nanoline, PetType.Attack, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(spell.Nanoline, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool ShieldOfTheObedientServant(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
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
                    && (pet.Type == PetType.Attack || pet.Type == PetType.Support))
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

        #region Auras

        private bool BlindAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (DynelManager.LocalPlayer.NanoPercent < 30) { return false; }

            if (DebuffingAuraSelection.Blind != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()) { return false; }

            return DynelManager.NPCs.Any(c => c.Health > 0
                && c.FightingTarget?.Buffs.Contains(202732) == false && c.FightingTarget?.Buffs.Contains(214879) == false
                && c.FightingTarget?.Buffs.Contains(284620) == false && c.FightingTarget?.Buffs.Contains(216382) == false
                && c.FightingTarget?.IsPet == false
                && c.Position.DistanceFrom(DynelManager.LocalPlayer.Position) <= 9f);
        }

        private bool ShieldRipperAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.NanoPercent < 30) { return false; }

            if (DebuffingAuraSelection.ShieldRipper != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()) { return false; }

            return DynelManager.NPCs.Any(c => c.Health > 0
                && c.FightingTarget?.Buffs.Contains(202732) == false && c.FightingTarget?.Buffs.Contains(214879) == false
                && c.FightingTarget?.Buffs.Contains(284620) == false && c.FightingTarget?.Buffs.Contains(216382) == false
                && c.FightingTarget?.IsPet == false
                && c.Position.DistanceFrom(DynelManager.LocalPlayer.Position) <= 9f);
        }

        private bool ArmorAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.Armor != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool DamageAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.Damage != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool ReflectAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.Reflect != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool ShieldAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.Shield != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }
        private bool SnareAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (DynelManager.LocalPlayer.NanoPercent < 30) { return false; }

            if (DebuffingAuraSelection.PetSnare != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()) { return false; }

            if (SnareMobExists())
                if (CanCast(spell))
                    return spell.IsReady;

            return PetBuff(spell, fightingTarget, ref actionTarget);
        }

        //private bool AuraCancellation(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (fightingTarget != null) { return false; }

        //    actionTarget.Target = DynelManager.LocalPlayer.Pets
        //        .Where(c => c.Character.Buffs.Contains(NanoLine.EngineerPetAOESnareBuff))
        //        .FirstOrDefault().Character;

        //    if (actionTarget.Target != null)
        //    {
        //        actionTarget.ShouldSetTarget = true;
        //        return true;
        //    }

        //    return false;
        //}

        #endregion

        #region Perks

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

        #endregion

        #region Trimmers

        //You feel a faint vibration from the trimmer.
        private bool PetAggressiveTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("TauntTrimmer")) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (pet.Type == PetType.Attack
                    && CanTrim(pet)
                    && CanTauntTrim(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    petTrimmedAggressive[PetType.Attack] = true;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }

                if (pet.Type == PetType.Support
                    && CanTrim(pet)
                    && CanTauntTrim(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    petTrimmedAggressive[PetType.Support] = true;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }
            }

            return false;
        }

        //You hear a ring inside the robot.
        private bool PetAggDefTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AggDefTrimmer")) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (pet.Type == PetType.Support
                    && CanTrim(pet)
                    && CanAggDefTrim(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    petTrimmedAggDef[PetType.Support] = true;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }

                if (pet.Type == PetType.Attack
                    && CanTrim(pet)
                    && CanAggDefTrim(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    petTrimmedAggDef[PetType.Attack] = true;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }
            }

            return false;
        }
        // Lock skill Elec. Engi for 5m. The robot straightens its back.
        private bool PetDivertHpTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("DivertHpTrimmer")) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (pet.Type == PetType.Attack
                    && CanTrim(pet)
                    && CanDivertHpTrim(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    petTrimmedHpDiv[PetType.Attack] = true;
                    _lastPetTrimDivertHpTime[PetType.Attack] = Time.NormalTime;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }

                if (pet.Type == PetType.Support
                    && CanTrim(pet)
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
        // Lock skill Mech. Engi for 5m. The arms of your robot jerk briefly.
        private bool PetDivertOffTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("DivertOffTrimmer")) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (pet.Type == PetType.Support
                    && CanTrim(pet)
                    && CanDivertOffTrim(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    petTrimmedOffDiv[PetType.Support] = true;
                    _lastPetTrimDivertOffTime[PetType.Support] = Time.NormalTime;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                };

                if (pet.Type == PetType.Attack
                    && CanTrim(pet)
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

            return false;
        }

        #endregion

        #endregion

        #region Misc



        private bool SnareMobExists()
        {
            return DynelManager.NPCs
                .Where(c => c.Name == "Flaming Vengeance" ||
                    c.Name == "Hand of the Colonel")
                .Any();
        }

        private bool CanTrim(Pet pet)
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
            public static readonly int[] EngineeringBuff = { 273346, 227667, 227657 };

        }

        private static class RelevantTrimmers
        {
            public static readonly int[] IncreaseAggressiveness = { 154940, 154939 }; // Mech. Engi
            public static readonly int[] PositiveAggressiveDefensive = { 88384, 88383 }; // Mech. Engi
            public static readonly int[] DivertEnergyToHitpoints = { 88382, 88381 }; // Lock skill Elec. Engi for 5m.
            public static readonly int[] DivertEnergyToOffense = { 88378, 88377 }; // Lock skill Mech. Engi for 5m.

            //public const int IncreaseAggressivenessLow = 154939;
            //public const int IncreaseAggressivenessHigh = 154940;

            //public const int DivertEnergyToOffenseLow = 88377;
            //public const int DivertEnergyToOffenseHigh = 88378;

            //public const int PositiveAggressiveDefensiveHigh = 88384;

            //public const int DivertEnergyToHitpointsLow = 88381;
            //public const int DivertEnergyToHitpointsHigh = 88382;
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

        #endregion

    }
}
