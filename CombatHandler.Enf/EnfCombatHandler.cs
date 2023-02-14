using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System.Linq;
using System;
using AOSharp.Core.IPC;
using CombatHandler.Generic;
using System.Diagnostics;

namespace CombatHandler.Enf
{
    class EnfCombatHandler : GenericCombatHandler
    { 
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        //private static bool ToggleDebuffing = false;

        private static Window _buffWindow;
        private static Window _tauntWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;

        private static View _buffView;
        private static View _tauntView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;

        private static double _absorbs;
        private static double _challenger;
        private static double _rage;
        private static double _mongo;
        private static double _singleTaunt;

        private static double _ncuUpdateTime;

        public EnfCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
            //IPCChannel.RegisterCallback((int)IPCOpcode.GlobalDebuffing, OnGlobalDebuffingMessage);

            Config.CharSettings[Game.ClientInst].BioCocoonPercentageChangedEvent += BioCocoonPercentage_Changed;
            Config.CharSettings[Game.ClientInst].SingleTauntDelayChangedEvent += SingleTauntDelay_Changed;
            Config.CharSettings[Game.ClientInst].MongoDelayChangedEvent += MongoDelay_Changed;
            Config.CharSettings[Game.ClientInst].CycleAbsorbsDelayChangedEvent += CycleAbsorbsDelay_Changed;
            Config.CharSettings[Game.ClientInst].CycleChallengerDelayChangedEvent += CycleChallengerDelay_Changed;
            Config.CharSettings[Game.ClientInst].CycleRageDelayChangedEvent += CycleRageDelay_Changed;
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
            Config.CharSettings[Game.ClientInst].DuckAbsorbsItemPercentageChangedEvent += DuckAbsorbsItemPercentage_Changed;
            Config.CharSettings[Game.ClientInst].BodyDevAbsorbsItemPercentageChangedEvent += BodyDevAbsorbsItemPercentage_Changed;
            Config.CharSettings[Game.ClientInst].StrengthAbsorbsItemPercentageChangedEvent += StrengthAbsorbsItemPercentage_Changed;
            Config.CharSettings[Game.ClientInst].BioRegrowthPercentageChangedEvent += BioRegrowthPercentage_Changed;
            Config.CharSettings[Game.ClientInst].CycleBioRegrowthPerkDelayChangedEvent += CycleBioRegrowthPerkDelay_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("GlobalBuffing", true);
            _settings.AddVariable("GlobalComposites", true);
            _settings.AddVariable("Ost", false);

            _settings.AddVariable("SharpObjects", true);
            _settings.AddVariable("Grenades", true);

            _settings.AddVariable("StimTargetSelection", (int)StimTargetSelection.Self);

            _settings.AddVariable("Kits", true);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.RagingBlow);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.ViolationBuffer);

            _settings.AddVariable("SingleTauntsSelection", (int)SingleTauntsSelection.None);

            _settings.AddVariable("Mongo", true);
            _settings.AddVariable("CycleAbsorbs", false);
            _settings.AddVariable("CycleChallenger", false);
            _settings.AddVariable("CycleRage", false);
            _settings.AddVariable("TauntProc", true);
            _settings.AddVariable("AbsorbACBuff", true);
            _settings.AddVariable("TargetedHpBuff", true);
            _settings.AddVariable("InitiativeBuffs", true);
            _settings.AddVariable("DamageShields", false);


            _settings.AddVariable("StrengthBuffSelection", (int)StrengthBuffSelection.None);

            _settings.AddVariable("TrollForm", false);

            _settings.AddVariable("ScorpioTauntTool", false);

            RegisterSettingsWindow("Enforcer Handler", "EnforcerSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcEnforcerVortexOfHate, VortexOfHate, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerRagingBlow, RagingBlow, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerShieldOfTheOgre, ShieldOfTheOgre, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerInspireRage, InspireRage, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerAirOfHatred, AirOfHatred, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerTearLigaments, TearLigaments, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerVileRage, VileRage, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcEnforcerViolationBuffer, ViolationBuffer, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerInspireIre, InspireIre, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerShrugOffHits, ShrugOffHits, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerBustKneecaps, BustKneecaps, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerIgnorePain, IgnorePain, CombatActionPriority.Low);

            //Troll Form
            RegisterPerkProcessor(PerkHash.TrollForm, TrollForm);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HPBuff).OrderByStackingOrder(), GlobalGenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MongoBuff).OrderByStackingOrder(), Mongo, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.SingleTargetTaunt, SingleTargetTaunt, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageChangeBuffs).OrderByStackingOrder(), DamageChange);
            RegisterSpellProcessor(RelevantNanos.FortifyBuffs, CycleAbsorbs, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Rage).OrderByStackingOrder(), CycleRage, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Challenger).OrderByStackingOrder(), CycleChallenger, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(), GlobalGenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EnforcerTauntProcs).OrderByStackingOrder(), TauntProc);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), GlobalGenericBuff);
            RegisterSpellProcessor(RelevantNanos.Melee1HB, Melee1HBBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.Melee1HE, Melee1HEBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.Melee2HE, Melee2HEBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.Melee2HB, Melee2HBBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.MeleePierce, MeleePierceBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.MeleeEnergy, MeleeEnergyBuffWeapon);

            //Team buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitiativeBuffs);
            RegisterSpellProcessor(RelevantNanos.TargetedDamageShields, DamageShields);
            RegisterSpellProcessor(RelevantNanos.TargetedHpBuff, TargetedHpBuff);
            //RegisterSpellProcessor(RelevantNanos.FOCUSED_ANGER, GlobalGenericTeamBuff);
            RegisterSpellProcessor(RelevantNanos.AbsorbACBuff, AbsorbACBuff);
            RegisterSpellProcessor(RelevantNanos.ProdigiousStrength, StrengthBuff);

            RegisterItemProcessor(244655, 244655, TauntTool);

            //if (TauntTools.CanUseTauntTool())
            //{
            //    Item tauntTool = TauntTools.GetBestTauntTool();
            //    RegisterItemProcessor(tauntTool.LowId, tauntTool.HighId, TauntTool);
            //}

            PluginDirectory = pluginDir;

            BioCocoonPercentage = Config.CharSettings[Game.ClientInst].BioCocoonPercentage;
            SingleTauntDelay = Config.CharSettings[Game.ClientInst].SingleTauntDelay;
            MongoDelay = Config.CharSettings[Game.ClientInst].MongoDelay;
            CycleAbsorbsDelay = Config.CharSettings[Game.ClientInst].CycleAbsorbsDelay;
            CycleChallengerDelay = Config.CharSettings[Game.ClientInst].CycleChallengerDelay;
            CycleRageDelay = Config.CharSettings[Game.ClientInst].CycleRageDelay;
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
            DuckAbsorbsItemPercentage = Config.CharSettings[Game.ClientInst].DuckAbsorbsItemPercentage;
            BodyDevAbsorbsItemPercentage = Config.CharSettings[Game.ClientInst].BodyDevAbsorbsItemPercentage;
            StrengthAbsorbsItemPercentage = Config.CharSettings[Game.ClientInst].StrengthAbsorbsItemPercentage;
            BioRegrowthPercentage = Config.CharSettings[Game.ClientInst].BioRegrowthPercentage;
            CycleBioRegrowthPerkDelay = Config.CharSettings[Game.ClientInst].CycleBioRegrowthPerkDelay;
        }

        public Window[] _windows => new Window[] { _buffWindow, _tauntWindow, _procWindow, _itemWindow, _perkWindow };

        #region Callbacks
        public static void OnRemainingNCUMessage(int sender, IPCMessage msg)
        {
            if (Game.IsZoning)
                return;

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

        #endregion

        #region Handles

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\EnforcerBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "EnforcerBuffsView" }, _buffView);

                window.FindView("AbsorbsDelayBox", out TextInputView absorbsInput);
                window.FindView("ChallengerDelayBox", out TextInputView challengerInput);
                window.FindView("RageDelayBox", out TextInputView rageInput);

                if (absorbsInput != null)
                    absorbsInput.Text = $"{CycleAbsorbsDelay}";
                if (challengerInput != null)
                    challengerInput.Text = $"{CycleChallengerDelay}";
                if (rageInput != null)
                    rageInput.Text = $"{CycleRageDelay}";
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "EnforcerBuffsView" }, _buffView, out var container);
                _buffWindow = container;

                container.FindView("AbsorbsDelayBox", out TextInputView absorbsInput);
                container.FindView("ChallengerDelayBox", out TextInputView challengerInput);
                container.FindView("RageDelayBox", out TextInputView rageInput);

                if (absorbsInput != null)
                    absorbsInput.Text = $"{CycleAbsorbsDelay}";
                if (challengerInput != null)
                    challengerInput.Text = $"{CycleChallengerDelay}";
                if (rageInput != null)
                    rageInput.Text = $"{CycleRageDelay}";
            }
        }

        private void HandleTauntViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                _tauntView = View.CreateFromXml(PluginDirectory + "\\UI\\EnforcerTauntsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Taunts", XmlViewName = "EnforcerTauntsView" }, _tauntView);

                window.FindView("SingleTauntDelayBox", out TextInputView singleInput);
                window.FindView("MongoDelayBox", out TextInputView mongoInput);

                if (singleInput != null)
                    singleInput.Text = $"{SingleTauntDelay}";
                if (mongoInput != null)
                    mongoInput.Text = $"{MongoDelay}";
            }
            else if (_tauntWindow == null || (_tauntWindow != null && !_tauntWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_tauntWindow, PluginDir, new WindowOptions() { Name = "Taunts", XmlViewName = "EnforcerTauntsView" }, _tauntView, out var container);
                _tauntWindow = container;

                container.FindView("SingleTauntDelayBox", out TextInputView singleInput);
                container.FindView("MongoDelayBox", out TextInputView mongoInput);

                if (singleInput != null)
                    singleInput.Text = $"{SingleTauntDelay}";
                if (mongoInput != null)
                    mongoInput.Text = $"{MongoDelay}";
            }
        }
        private void HandleItemViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_itemView)) { return; }

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\EnforcerItemsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "EnforcerItemsView" }, _itemView);

                window.FindView("StimTargetBox", out TextInputView stimTargetInput);
                window.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                window.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                window.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                window.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);
                window.FindView("DuckAbsorbsItemPercentageBox", out TextInputView duckInput);
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
                if (duckInput != null)
                    duckInput.Text = $"{DuckAbsorbsItemPercentage}";
                if (bodyDevInput != null)
                    bodyDevInput.Text = $"{BodyDevAbsorbsItemPercentage}";
                if (strengthInput != null)
                    strengthInput.Text = $"{StrengthAbsorbsItemPercentage}";
            }
            else if (_itemWindow == null || (_itemWindow != null && !_itemWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "EnforcerItemsView" }, _itemView, out var container);
                _itemWindow = container;

                container.FindView("StimTargetBox", out TextInputView stimTargetInput);
                container.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                container.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                container.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                container.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);
                container.FindView("DuckAbsorbsItemPercentageBox", out TextInputView duckInput);
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
                if (duckInput != null)
                    duckInput.Text = $"{DuckAbsorbsItemPercentage}";
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
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\EnforcerProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "EnforcerProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "EnforcerProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }
        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_perkView)) { return; }

                _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\EnforcerPerksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Perks", XmlViewName = "EnforcerPerksView" }, _perkView);

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
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "EnforcerPerksView" }, _perkView, out var container);
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

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            if (Game.IsZoning)
                return;

            base.OnUpdate(deltaTime);

            var window = SettingsController.FindValidWindow(_windows);

            if (window != null && window.IsValid)
            {
                window.FindView("SingleTauntDelayBox", out TextInputView singleInput);
                window.FindView("MongoDelayBox", out TextInputView mongoInput);
                window.FindView("AbsorbsDelayBox", out TextInputView absorbsInput);
                window.FindView("ChallengerDelayBox", out TextInputView challengerInput);
                window.FindView("RageDelayBox", out TextInputView rageInput);
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
                window.FindView("DuckAbsorbsItemPercentageBox", out TextInputView duckInput);
                window.FindView("BodyDevAbsorbsItemPercentageBox", out TextInputView bodyDevInput);
                window.FindView("StrengthAbsorbsItemPercentageBox", out TextInputView strengthInput);
                window.FindView("BioRegrowthPercentageBox", out TextInputView bioRegrowthPercentageInput);
                window.FindView("BioRegrowthDelayBox", out TextInputView bioRegrowthDelayInput);

                if (bioCocoonInput != null && !string.IsNullOrEmpty(bioCocoonInput.Text))
                    if (int.TryParse(bioCocoonInput.Text, out int bioCocoonValue))
                        if (Config.CharSettings[Game.ClientInst].BioCocoonPercentage != bioCocoonValue)
                            Config.CharSettings[Game.ClientInst].BioCocoonPercentage = bioCocoonValue;

                if (singleInput != null && !string.IsNullOrEmpty(singleInput.Text))
                    if (int.TryParse(singleInput.Text, out int singleValue))
                        if (Config.CharSettings[Game.ClientInst].SingleTauntDelay != singleValue)
                            Config.CharSettings[Game.ClientInst].SingleTauntDelay = singleValue;

                if (mongoInput != null && !string.IsNullOrEmpty(mongoInput.Text))
                    if (int.TryParse(mongoInput.Text, out int mongoValue))
                        if (Config.CharSettings[Game.ClientInst].MongoDelay != mongoValue)
                            Config.CharSettings[Game.ClientInst].MongoDelay = mongoValue;

                if (absorbsInput != null && !string.IsNullOrEmpty(absorbsInput.Text))
                    if (int.TryParse(absorbsInput.Text, out int absorbsValue))
                        if (Config.CharSettings[Game.ClientInst].CycleAbsorbsDelay != absorbsValue)
                            Config.CharSettings[Game.ClientInst].CycleAbsorbsDelay = absorbsValue;

                if (challengerInput != null && !string.IsNullOrEmpty(challengerInput.Text))
                    if (int.TryParse(challengerInput.Text, out int challengerValue))
                        if (Config.CharSettings[Game.ClientInst].CycleChallengerDelay != challengerValue)
                            Config.CharSettings[Game.ClientInst].CycleChallengerDelay = challengerValue;

                if (rageInput != null && !string.IsNullOrEmpty(rageInput.Text))
                    if (int.TryParse(rageInput.Text, out int rageValue))
                        if (Config.CharSettings[Game.ClientInst].CycleRageDelay != rageValue)
                            Config.CharSettings[Game.ClientInst].CycleRageDelay = rageValue;

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

                if (duckInput != null && !string.IsNullOrEmpty(duckInput.Text))
                    if (int.TryParse(duckInput.Text, out int duckValue))
                        if (Config.CharSettings[Game.ClientInst].DuckAbsorbsItemPercentage != duckValue)
                            Config.CharSettings[Game.ClientInst].DuckAbsorbsItemPercentage = duckValue;

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

                if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                {
                    buffView.Tag = SettingsController.settingsWindow;
                    buffView.Clicked = HandleBuffViewClick;
                }

                if (SettingsController.settingsWindow.FindView("TauntsView", out Button tauntView))
                {
                    tauntView.Tag = SettingsController.settingsWindow;
                    tauntView.Clicked = HandleTauntViewClick;
                }

                if (SettingsController.settingsWindow.FindView("ProcsView", out Button procView))
                {
                    procView.Tag = SettingsController.settingsWindow;
                    procView.Clicked = HandleProcViewClick;
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
            }
        }


        #region LE Procs

        private bool InspireRage(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.InspireRage != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool RagingBlow(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.RagingBlow != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool ShieldOfTheOgre(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.Shieldoftheogre != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool TearLigaments(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.TearLigaments != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool VileRage(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.VileRage != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool VortexOfHate(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.VortexofHate != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool AirOfHatred(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.Airofhatred != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool BustKneecaps(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.BustKneecaps != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool IgnorePain(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.IgnorePain != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool InspireIre(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.InspireIre != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool ShrugOffHits(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.ShrugOffHits != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool ViolationBuffer(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.ViolationBuffer != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Perks

        private bool TrollForm(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("TrollForm") || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            return CyclePerks(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Buffs

        private bool DamageChange(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, NanoLine.DamageChangeBuffs, ref actionTarget);
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (SingleTauntsSelection.Area == (SingleTauntsSelection)_settings["SingleTauntsSelection"].AsInt32()
                && Time.NormalTime > _singleTaunt + SingleTauntDelay)
            {
                SimpleChar mob = DynelManager.NPCs
                    .Where(c => c.IsAttacking && c.FightingTarget != null
                        && c.FightingTarget?.Profession != Profession.Enforcer
                        && c.IsInLineOfSight
                        && !debuffAreaTargetsToIgnore.Contains(c.Name)
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && c.FightingTarget?.Identity != DynelManager.LocalPlayer.Identity
                        && c.Name != "Alien Heavy Patroller"
                        && AttackingTeam(c))
                    .OrderBy(c => c.MaxHealth)
                    .FirstOrDefault();

                if (mob != null)
                {
                    _singleTaunt = Time.NormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = mob;
                    return true;
                }
                else if (fightingTarget != null)
                {
                    _singleTaunt = Time.NormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = fightingTarget;
                    return true;
                }
            }

            if (SingleTauntsSelection.Target == (SingleTauntsSelection)_settings["SingleTauntsSelection"].AsInt32() 
                && Time.NormalTime > _singleTaunt + SingleTauntDelay)
            {
                if (fightingTarget != null)
                {
                    _singleTaunt = Time.NormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = fightingTarget;
                    return true;
                }
            }

            return false;
        }

        private bool Melee1HEBuffWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Edged1H);
        }

        private bool Melee1HBBuffWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Blunt1H);
        }

        private bool Melee2HEBuffWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Edged2H);
        }

        private bool Melee2HBBuffWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Blunt2H);
        }

        private bool MeleePierceBuffWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Piercing);
        }

        private bool MeleeEnergyBuffWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.MeleeEnergy);
        }

        private bool CycleChallenger(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("CycleChallenger") && Time.NormalTime > _challenger + CycleChallengerDelay
                && (fightingTarget != null || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 0))
            {
                if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

                _challenger = Time.NormalTime;
                return true;
            }

            return false;
        }

        private bool CycleRage(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("CycleRage") && Time.NormalTime > _rage + CycleRageDelay
                && (fightingTarget != null || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 0))
            {
                if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

                _rage = Time.NormalTime;
                return true;
            }

            if (!DynelManager.LocalPlayer.Buffs.Contains(NanoLine.Root) || !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.Snare)) { return false; }

            return true;
        }

        private bool CycleAbsorbs(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Any(Buff => Buff.Id == RelevantNanos.BioCocoon)) { return false; }

            if (IsSettingEnabled("CycleAbsorbs") && Time.NormalTime > _absorbs + CycleAbsorbsDelay
                && (fightingTarget != null || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 0))
            {
                if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

                _absorbs = Time.NormalTime;
                return true;
            }

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool TauntProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("TauntProc")) { return false; }

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool Mongo(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !IsSettingEnabled("Mongo") || !CanCast(spell)) { return false; }

            if (DynelManager.NPCs.Any(c => c.Health > 0
                    && c.Name == "Alien Heavy Patroller"
                    && c.Position.DistanceFrom(DynelManager.LocalPlayer.Position) <= 18f)) { return false; }

            if (Time.NormalTime > _mongo + MongoDelay
     && fightingTarget != null)
            {
                _mongo = Time.NormalTime;
                return true;
            }

            //if (Time.NormalTime > _mongo + MongoDelay
            //    && (fightingTarget != null || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 0))
            //{
            //    _mongo = Time.NormalTime;
            //    return DynelManager.NPCs.Any(c => c.Health > 0
            //        && c.FightingTarget?.IsPet == false
            //        && c.Position.DistanceFrom(DynelManager.LocalPlayer.Position) <= 15f);
            //}

            return false;
        }

        #endregion

        #region Team Buffs

        protected bool AbsorbACBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AbsorbACBuff")) { return false; }

            return GenericTeamBuff(spell, ref actionTarget);
        }

        protected bool TargetedHpBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("TargetedHpBuff")) { return false; }

            return GenericTeamBuff(spell, ref actionTarget);
        }

        protected bool InitiativeBuffs(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("InitiativeBuffs"))
                return TeamBuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Melee);

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }


        protected bool StrengthBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (StrengthBuffSelection.None == (StrengthBuffSelection)_settings["StrengthBuffSelection"].AsInt32()) { return false; }

           if (StrengthBuffSelection.Team == (StrengthBuffSelection)_settings["StrengthBuffSelection"].AsInt32())
                return TeamBuff(spell, spell.Nanoline, ref actionTarget);

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        protected bool DamageShields(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("DamageShields")) { return false; }

            return GenericTeamBuff(spell, ref actionTarget);
        }

        #endregion

        #region Misc

        private static class RelevantNanos
        {
            public static readonly int[] SingleTargetTaunt = { 275014, 223123, 223121, 223119, 223117, 223115, 100209, 100210, 100212, 100211, 100213 };
            public static readonly int[] Melee1HB = { 202846, 202844, 202842, 29630, 202840, 29644 };
            public static readonly int[] Melee2HB = { 202856, 202854, 202852, 29630, 202850, 29644, 202848 };
            public static readonly int[] Melee1HE = { 202818, 202816, 202793, 202791, 202774, 202739, 202776 };
            public static readonly int[] Melee2HE = { 202838, 202836, 202834, 202832, 202830, 202828, 202826 };
            public static readonly int[] MeleePierce = { 202858, 202860, 202862, 202864, 202866, 202868, 202870 };
            public static readonly int[] MeleeEnergy = { 203215, 203207, 203209, 203211, 203213 };
            public static readonly int[] TargetedHpBuff = { 273629, 95708, 95700, 95701, 95702, 95704, 95706, 95707 };
            public static readonly int[] FortifyBuffs = { 273320, 270350, 117686, 117688, 117682, 117687, 117685, 117684, 117683, 117680, 117681 };
            public static readonly int[] AbsorbACBuff = { 270350, 117686, 117688, 117682, 117687, 117685, 117684, 117683, 117680, 117681 };
            public static readonly Spell[] TargetedDamageShields = Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder().Where(spell => spell.Id != ICE_BURN).ToArray();
            public const int MONGO_KRAKEN = 273322;
            public const int MONGO_DEMOLISH = 270786;
            public const int FOCUSED_ANGER = 29641;
            public const int IMPROVED_ESSENCE_OF_BEHEMOTH = 273629;
            public const int CORUSCATING_SCREEN = 55751;
            public const int ICE_BURN = 269460;
            public const int ProdigiousStrength = 29652;
            public const int BioCocoon = 209802;
        }

        public enum ProcType1Selection
        {
            VortexofHate, RagingBlow, Shieldoftheogre, InspireRage, Airofhatred, TearLigaments, VileRage
        }

        public enum ProcType2Selection
        {
            ViolationBuffer, InspireIre, ShrugOffHits, BustKneecaps, IgnorePain
        }
        public enum SingleTauntsSelection
        {
            None, Target, Area
        }

        public enum StrengthBuffSelection
        {
            None, Self, Team
        }
        #endregion
    }
}