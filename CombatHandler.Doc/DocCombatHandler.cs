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
using System.Windows.Input;

namespace CombatHandler.Doctor
{
    class DocCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        //private static bool ToggleDebuffing = false;

        private static Window _buffWindow;
        private static Window _debuffWindow;
        private static Window _healingWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _healingView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;

        //private static bool _asyncToggle = false;

        private static double _ncuUpdateTime;

        public DocCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
            //IPCChannel.RegisterCallback((int)IPCOpcode.GlobalDebuffing, OnGlobalDebuffingMessage);

            Config.CharSettings[Game.ClientInst].HealPercentageChangedEvent += HealPercentage_Changed;
            Config.CharSettings[Game.ClientInst].CompleteHealPercentageChangedEvent += CompleteHealPercentage_Changed;
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
            Config.CharSettings[Game.ClientInst].BattleGroupHeal1PercentageChangedEvent += BattleGroupHeal1Percentage_Changed;
            Config.CharSettings[Game.ClientInst].BattleGroupHeal2PercentageChangedEvent += BattleGroupHeal2Percentage_Changed;
            Config.CharSettings[Game.ClientInst].BattleGroupHeal3PercentageChangedEvent += BattleGroupHeal3Percentage_Changed;
            Config.CharSettings[Game.ClientInst].BattleGroupHeal4PercentageChangedEvent += BattleGroupHeal4Percentage_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("GlobalBuffing", true);
            _settings.AddVariable("GlobalComposites", true);
            //_settings.AddVariable("GlobalDebuffs", true);

            _settings.AddVariable("SharpObjects", false);
            _settings.AddVariable("Grenades", false);

            _settings.AddVariable("TOTWBooks", true);

            _settings.AddVariable("StimTargetSelection", (int)StimTargetSelection.None);

            _settings.AddVariable("Kits", true);

            _settings.AddVariable("TreatmentBuffSelection", (int)TreatmentBuffSelection.Self);
            _settings.AddVariable("StrengthBuffSelection", (int)StrengthBuffSelection.Self);

            _settings.AddVariable("InitDebuffSelection", (int)InitDebuffSelection.None);

            _settings.AddVariable("HealSelection", (int)HealSelection.SingleTeam);

            _settings.AddVariable("InitBuffSelection", (int)InitBuffSelection.Team);
            _settings.AddVariable("DOTA", (int)DOTADebuffTargetSelection.None);
            _settings.AddVariable("DOTB", (int)DOTBDebuffTargetSelection.None);
            _settings.AddVariable("DOTC", (int)DOTCDebuffTargetSelection.None);

            _settings.AddVariable("Nuking", false);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.DangerousCulture);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.MassiveVitaePlan);

            _settings.AddVariable("NanoResistTeam", true);
            _settings.AddVariable("PistolTeam", true);
            _settings.AddVariable("HealDeltaBuff", true);

            _settings.AddVariable("ShortHpSelection", (int)ShortHpSelection.None);
            _settings.AddVariable("ShortHOT", false);

            //_settings.AddVariable("CH", true);

            _settings.AddVariable("LockCH", false);

            RegisterSettingsWindow("Doctor Handler", "DocSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcDoctorDangerousCulture, DangerousCulture, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorAntiseptic, Antiseptic, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorMuscleMemory, MuscleMemory, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorBloodTransfusion, BloodTransfusion, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorRestrictiveBandaging, RestrictiveBandaging, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcDoctorMassiveVitaePlan, MassiveVitaePlan, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorAnatomicBlight, AnatomicBlight, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorHealingCare, HealingCare, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorPathogen, Pathogen, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorAnesthetic, Anesthetic, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorAstringent, Astringent, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorInflammation, Inflammation, CombatActionPriority.Low);

            //Perks
            RegisterPerkProcessor(PerkHash.BattlegroupHeal1, BattleGroupHeal1);
            RegisterPerkProcessor(PerkHash.BattlegroupHeal2, BattleGroupHeal2);
            RegisterPerkProcessor(PerkHash.BattlegroupHeal3, BattleGroupHeal3);
            RegisterPerkProcessor(PerkHash.BattlegroupHeal4, BattleGroupHeal4);


            //Healing
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CompleteHealingLine).OrderByStackingOrder(), CompleteHealing, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.Heals, Healing, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.TeamHeals, TeamHealing, CombatActionPriority.High);

            //RegisterSpellProcessor(RelevantNanos.AlphaAndOmega, LockCH, CombatActionPriority.High);

            //Hots
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealOverTime).OrderByStackingOrder(), ShortHOT);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.HPBuffs, MaxHealth);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceBuffs).OrderByStackingOrder(), NanoResistance);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FirstAidAndTreatmentBuff).OrderByStackingOrder(), TreatmentBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.StrengthBuff).OrderByStackingOrder(), StrengthBuff);

            //Team Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolTeam);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealDeltaBuff).OrderByStackingOrder(), HealDeltaBuff);

            RegisterSpellProcessor(RelevantNanos.ImprovedLC, ImprovedLifeChanneler);
            RegisterSpellProcessor(RelevantNanos.IndividualShortMaxHealths, ShortMaxHealth);

            //Debuffs
            RegisterSpellProcessor(RelevantNanos.InitDebuffs, InitDebuff, CombatActionPriority.Medium);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Nuke).OrderByStackingOrder(), SingleTargetNuke, CombatActionPriority.Low);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineA).OrderByStackingOrder(), DOTADebuffTarget, CombatActionPriority.Medium);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineB).OrderByStackingOrder(), DOTBDebuffTarget, CombatActionPriority.Medium);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTStrainC).OrderByStackingOrder(), DOTCDebuffTarget, CombatActionPriority.Medium);

            //Items
            RegisterItemProcessor(RelevantItems.Books, TOTWHeal);

            PluginDirectory = pluginDir;

            HealPercentage = Config.CharSettings[Game.ClientInst].HealPercentage;
            CompleteHealPercentage = Config.CharSettings[Game.ClientInst].CompleteHealPercentage;
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
            BattleGroupHeal1Percentage = Config.CharSettings[Game.ClientInst].BattleGroupHeal1Percentage;
            BattleGroupHeal2Percentage = Config.CharSettings[Game.ClientInst].BattleGroupHeal2Percentage;
            BattleGroupHeal3Percentage = Config.CharSettings[Game.ClientInst].BattleGroupHeal3Percentage;
            BattleGroupHeal4Percentage = Config.CharSettings[Game.ClientInst].BattleGroupHeal4Percentage;
        }

        public Window[] _windows => new Window[] { _buffWindow, _debuffWindow, _healingWindow, _procWindow, _itemWindow, _perkWindow };

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
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_buffView)) { return; }

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\DocBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "DocBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "DocBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }
        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_perkView)) { return; }

                _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\DocPerksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Perks", XmlViewName = "DocPerksView" }, _perkView);

                window.FindView("SphereDelayBox", out TextInputView sphereInput);
                window.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                window.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                window.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                window.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                window.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);
                window.FindView("BattleGroupHeal1PercentageBox", out TextInputView bgh1Input);
                window.FindView("BattleGroupHeal2PercentageBox", out TextInputView bgh2Input);
                window.FindView("BattleGroupHeal3PercentageBox", out TextInputView bgh3Input);
                window.FindView("BattleGroupHeal4PercentageBox", out TextInputView bgh4Input);

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
                if (bgh1Input != null)
                    bgh1Input.Text = $"{BattleGroupHeal1Percentage}";
                if (bgh2Input != null)
                    bgh2Input.Text = $"{BattleGroupHeal2Percentage}";
                if (bgh3Input != null)
                    bgh3Input.Text = $"{BattleGroupHeal3Percentage}";
                if (bgh4Input != null)
                    bgh4Input.Text = $"{BattleGroupHeal4Percentage}";
            }
            else if (_perkWindow == null || (_perkWindow != null && !_perkWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "DocPerksView" }, _perkView, out var container);
                _perkWindow = container;

                container.FindView("SphereDelayBox", out TextInputView sphereInput);
                container.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                container.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                container.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                container.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                container.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);
                container.FindView("BattleGroupHeal1PercentageBox", out TextInputView bgh1Input);
                container.FindView("BattleGroupHeal2PercentageBox", out TextInputView bgh2Input);
                container.FindView("BattleGroupHeal3PercentageBox", out TextInputView bgh3Input);
                container.FindView("BattleGroupHeal4PercentageBox", out TextInputView bgh4Input);

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
                if (bgh1Input != null)
                    bgh1Input.Text = $"{BattleGroupHeal1Percentage}";
                if (bgh2Input != null)
                    bgh2Input.Text = $"{BattleGroupHeal2Percentage}";
                if (bgh3Input != null)
                    bgh3Input.Text = $"{BattleGroupHeal3Percentage}";
                if (bgh4Input != null)
                    bgh4Input.Text = $"{BattleGroupHeal4Percentage}";
            }
        }
        private void HandleHealingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Do we need this?
                if (window.Views.Contains(_healingView)) { return; }

                _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\DocHealingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Healing", XmlViewName = "DocHealingView" }, _healingView);

                window.FindView("HealPercentageBox", out TextInputView healInput);
                window.FindView("CompleteHealPercentageBox", out TextInputView completeHealInput);

                if (healInput != null)
                    healInput.Text = $"{HealPercentage}";

                if (completeHealInput != null)
                    completeHealInput.Text = $"{CompleteHealPercentage}";
            }
            else if (_healingWindow == null || (_healingWindow != null && !_healingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_healingWindow, PluginDir, new WindowOptions() { Name = "Healing", XmlViewName = "DocHealingView" }, _healingView, out var container);
                _healingWindow = container;

                container.FindView("HealPercentageBox", out TextInputView healInput);
                container.FindView("CompleteHealPercentageBox", out TextInputView completeHealInput);

                if (healInput != null)
                    healInput.Text = $"{HealPercentage}";

                if (completeHealInput != null)
                    completeHealInput.Text = $"{CompleteHealPercentage}";
            }
        }

        private void HandleDebuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_debuffView)) { return; }

                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\DocDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "DocDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "DocDebuffsView" }, _debuffView, out var container);
                _debuffWindow = container;
            }
        }
        private void HandleItemViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_itemView)) { return; }

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\DocItemsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "DocItemsView" }, _itemView);

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
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "DocItemsView" }, _itemView, out var container);
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
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\DocProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "DocProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "DocProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            if (Game.IsZoning)
                return;

            base.OnUpdate(deltaTime);

            if (Time.NormalTime > _ncuUpdateTime + 0.5f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }

            var window = SettingsController.FindValidWindow(_windows);

            if (window != null && window.IsValid)
            {
                window.FindView("HealPercentageBox", out TextInputView healInput);
                window.FindView("CompleteHealPercentageBox", out TextInputView completeHealInput);
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
                window.FindView("BattleGroupHeal1PercentageBox", out TextInputView bg1Input);
                window.FindView("BattleGroupHeal2PercentageBox", out TextInputView bg2Input);
                window.FindView("BattleGroupHeal3PercentageBox", out TextInputView bg3Input);
                window.FindView("BattleGroupHeal4PercentageBox", out TextInputView bg4Input);

                if (healInput != null && !string.IsNullOrEmpty(healInput.Text))
                    if (int.TryParse(healInput.Text, out int healValue))
                        if (Config.CharSettings[Game.ClientInst].HealPercentage != healValue)
                            Config.CharSettings[Game.ClientInst].HealPercentage = healValue;

                if (completeHealInput != null && !string.IsNullOrEmpty(completeHealInput.Text))
                    if (int.TryParse(completeHealInput.Text, out int completeHealValue))
                        if (Config.CharSettings[Game.ClientInst].CompleteHealPercentage != completeHealValue)
                            Config.CharSettings[Game.ClientInst].CompleteHealPercentage = completeHealValue;

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

                if (bg1Input != null && !string.IsNullOrEmpty(bg1Input.Text))
                    if (int.TryParse(bg1Input.Text, out int bg1Value))
                        if (Config.CharSettings[Game.ClientInst].BattleGroupHeal1Percentage != bg1Value)
                            Config.CharSettings[Game.ClientInst].BattleGroupHeal1Percentage = bg1Value;

                if (bg2Input != null && !string.IsNullOrEmpty(bg2Input.Text))
                    if (int.TryParse(bg2Input.Text, out int bg2Value))
                        if (Config.CharSettings[Game.ClientInst].BattleGroupHeal2Percentage != bg2Value)
                            Config.CharSettings[Game.ClientInst].BattleGroupHeal2Percentage = bg2Value;

                if (bg3Input != null && !string.IsNullOrEmpty(bg3Input.Text))
                    if (int.TryParse(bg3Input.Text, out int bg3Value))
                        if (Config.CharSettings[Game.ClientInst].BattleGroupHeal3Percentage != bg3Value)
                            Config.CharSettings[Game.ClientInst].BattleGroupHeal3Percentage = bg3Value;

                if (bg4Input != null && !string.IsNullOrEmpty(bg4Input.Text))
                    if (int.TryParse(bg4Input.Text, out int bg4Value))
                        if (Config.CharSettings[Game.ClientInst].BattleGroupHeal4Percentage != bg4Value)
                            Config.CharSettings[Game.ClientInst].BattleGroupHeal4Percentage = bg4Value;
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

                if (SettingsController.settingsWindow.FindView("HealingView", out Button healingView))
                {
                    healingView.Tag = SettingsController.settingsWindow;
                    healingView.Clicked = HandleHealingViewClick;
                }

                if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                {
                    buffView.Tag = SettingsController.settingsWindow;
                    buffView.Clicked = HandleBuffViewClick;
                }

                if (SettingsController.settingsWindow.FindView("DebuffsView", out Button debuffView))
                {
                    debuffView.Tag = SettingsController.settingsWindow;
                    debuffView.Clicked = HandleDebuffViewClick;
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

        private bool Antiseptic(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.Antiseptic != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool BloodTransfusion(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.BloodTransfusion != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DangerousCulture(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.DangerousCulture != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool MuscleMemory(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.MuscleMemory != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool RestrictiveBandaging(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.RestrictiveBandaging != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool AnatomicBlight(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.AnatomicBlight != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool Anesthetic(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Anesthetic != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool Astringent(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Astringent != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool HealingCare(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.HealingCare != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool Inflammation(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Inflammation != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool MassiveVitaePlan(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.MassiveVitaePlan != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool Pathogen(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Pathogen != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }


        #endregion

        #region Healing


        private bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("LockCH") || CompleteHealPercentage == 0) { return false; }

            return FindMemberWithHealthBelow(CompleteHealPercentage, spell, ref actionTarget);
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HealPercentage == 0) { return false; }

            if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                if (Team.IsInTeam)
                {
                    List<SimpleChar> dyingTeamMember = DynelManager.Characters
                        .Where(c => Team.Members
                            .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
                                .Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                             && c.HealthPercent <= 85 && c.HealthPercent >= 50)
                        .ToList();

                    if (dyingTeamMember.Count >= 4) { return false; }
                }

                return FindMemberWithHealthBelow(HealPercentage, spell, ref actionTarget);
            }

            if (HealSelection.SingleArea == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                return FindPlayerWithHealthBelow(HealPercentage, spell, ref actionTarget);
            }

            return false;
        }

        private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HealPercentage == 0) { return false; }

            if (HealSelection.Team == (HealSelection)_settings["HealSelection"].AsInt32())
                return FindMemberWithHealthBelow(HealPercentage, spell, ref actionTarget);

            if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                if (Spell.List.Any(c => c.Id == 275011)) { return false; }

                if (Team.IsInTeam)
                {
                    List<SimpleChar> dyingTeamMember = DynelManager.Characters
                        .Where(c => Team.Members
                            .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
                                .Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                                && c.HealthPercent <= 85 && c.HealthPercent >= 50)
                        .ToList();

                    if (dyingTeamMember.Count >= 3)
                    {
                        return CanCast(spell);
                    }
                }
            }

            return false;
        }

        private bool ShortHOT(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("ShortHOT")|| !InCombat()) { return false; }

            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Buffs

        private bool MaxHealth(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (InitBuffSelection.Team == (InitBuffSelection)_settings["InitBuffSelection"].AsInt32())
                return GenericTeamBuff(spell, ref actionTarget);

            if (InitBuffSelection.None == (InitBuffSelection)_settings["InitBuffSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool NanoResistance(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("NanoResistTeam"))
                return GenericTeamBuff(spell, ref actionTarget);

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool TreatmentBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (TreatmentBuffSelection.Team == (TreatmentBuffSelection)_settings["TreatmentBuffSelection"].AsInt32())
                return GenericTeamBuff(spell, ref actionTarget);

            if (TreatmentBuffSelection.None == (TreatmentBuffSelection)_settings["TreatmentBuffSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool StrengthBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (StrengthBuffSelection.Team == (StrengthBuffSelection)_settings["StrengthBuffSelection"].AsInt32())
                return GenericTeamBuff(spell, ref actionTarget);

            if (StrengthBuffSelection.None == (StrengthBuffSelection)_settings["StrengthBuffSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        protected bool PistolTeam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam && IsSettingEnabled("PistolTeam"))
                return TeamBuffExclusionWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);

            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        protected bool HealDeltaBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("HealDeltaBuff"))
                return GenericTeamBuff(spell, ref actionTarget);

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool ImprovedLifeChanneler(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HealSelection.ImprovedLifeChanneler == (HealSelection)_settings["HealSelection"].AsInt32())
                return FindMemberWithHealthBelow(HealPercentage, spell, ref actionTarget);

            if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                if (Team.IsInTeam)
                {
                    List<SimpleChar> dyingTeamMember = DynelManager.Characters
                        .Where(c => Team.Members
                            .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
                                .Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                             && c.HealthPercent <= 85 && c.HealthPercent >= 50)
                        .ToList();

                    if (dyingTeamMember.Count >= 4)
                    {
                        return CanCast(spell);
                    }
                }
            }

            return Buff(spell, NanoLine.DoctorShortHPBuffs, ref actionTarget);
        }

        private bool ShortMaxHealth(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ShortHpSelection.Team == (ShortHpSelection)_settings["ShortHpSelection"].AsInt32())
                return GenericCombatTeamBuff(spell, fightingTarget, ref actionTarget);

            if (ShortHpSelection.None == (ShortHpSelection)_settings["ShortHpSelection"].AsInt32()) { return false; }

            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Debuffs

        private bool InitDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            if (InitDebuffSelection.Area == (InitDebuffSelection)_settings["InitDebuffSelection"].AsInt32())
                return AreaDebuff(spell, ref actionTarget);

            if (InitDebuffSelection.Target == (InitDebuffSelection)_settings["InitDebuffSelection"].AsInt32()
                && fightingTarget != null)
            {
                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                if (fightingTarget.Buffs.Find(spell.Id, out Buff buff) && buff.RemainingTime > 10) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (InitDebuffSelection.Boss == (InitDebuffSelection)_settings["InitDebuffSelection"].AsInt32()
                && fightingTarget != null)
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                if (fightingTarget.Buffs.Find(spell.Id, out Buff buff) && buff.RemainingTime > 10) { return false; }

                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.NanoPercent < 40) { return false; }

            return ToggledTargetDebuff("Nuking", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool DOTADebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.NanoPercent < 40) { return false; }

            if (DOTADebuffTargetSelection.None == (DOTADebuffTargetSelection)_settings["DOTA"].AsInt32()) { return false; }

            if (DOTADebuffTargetSelection.Area == (DOTADebuffTargetSelection)_settings["DOTA"].AsInt32())
                return AreaDebuff(spell, ref actionTarget);

            if (DOTADebuffTargetSelection.Target == (DOTADebuffTargetSelection)_settings["DOTA"].AsInt32()
                && fightingTarget != null)
            {
                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (DOTADebuffTargetSelection.Boss == (DOTADebuffTargetSelection)_settings["DOTA"].AsInt32()
                 && fightingTarget != null)
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;

        }

        private bool DOTBDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.NanoPercent < 40) { return false; }

            if (DOTBDebuffTargetSelection.None == (DOTBDebuffTargetSelection)_settings["DOTB"].AsInt32()) { return false; }

            if (DOTBDebuffTargetSelection.Area == (DOTBDebuffTargetSelection)_settings["DOTB"].AsInt32())
                return AreaDebuff(spell, ref actionTarget);

            if (DOTBDebuffTargetSelection.Target == (DOTBDebuffTargetSelection)_settings["DOTB"].AsInt32()
                && fightingTarget != null)
            {
                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (DOTBDebuffTargetSelection.Boss == (DOTBDebuffTargetSelection)_settings["DOTB"].AsInt32()
                 && fightingTarget != null)
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool DOTCDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.NanoPercent < 40) { return false; }

            if (DOTCDebuffTargetSelection.None == (DOTCDebuffTargetSelection)_settings["DOTC"].AsInt32()) { return false; }

            if (DOTCDebuffTargetSelection.Area == (DOTCDebuffTargetSelection)_settings["DOTC"].AsInt32())
                return AreaDebuff(spell, ref actionTarget);

            if (DOTCDebuffTargetSelection.Target == (DOTCDebuffTargetSelection)_settings["DOTC"].AsInt32()
                && fightingTarget != null)
            {
                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (DOTCDebuffTargetSelection.Boss == (DOTCDebuffTargetSelection)_settings["DOTC"].AsInt32()
                 && fightingTarget != null)
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }

        #endregion

        #region Items

        //private bool TOTWHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{

        //    if (HealPercentage == 0) { return false; }

        //    if (IsSettingEnabled("TOTWBooks"))

        //    if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BiologicalMetamorphosis)) { return false; }

        //    if (DynelManager.LocalPlayer.HealthPercent < 60)) 


        //                    { return false; }

        //}

        private bool TOTWHeal(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("TOTWBooks")) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BiologicalMetamorphosis)) { return false; }

            if (Team.IsInTeam)
            {
                SimpleChar teamMember = DynelManager.Players
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.HealthPercent < 60
                        && c.IsInLineOfSight
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && c.Health > 0)
                    .OrderBy(c => c.HealthPercent)
                    .OrderBy(c => c.Profession == Profession.Doctor)
                    .OrderBy(c => c.Profession == Profession.Enforcer)
                    .OrderBy(c => c.Profession == Profession.Soldier)
                    .FirstOrDefault();

                if (teamMember != null)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = teamMember;
                    return true;
                }

                return false;
            }


            if (DynelManager.LocalPlayer.HealthPercent < 60)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        #endregion

        #region Misc

        public enum InitBuffSelection
                {
                    None, Self, Team
                }

                public enum TreatmentBuffSelection
                {
                    None, Self, Team
                }
                public enum StrengthBuffSelection
                {
                    None, Self, Team
                }
                public enum HealSelection
                {
                    None, SingleTeam, SingleArea, Team, ImprovedLifeChanneler
                }
                public enum InitDebuffSelection
                {
                    None, Target, Area, Boss
                }

                public enum DOTADebuffTargetSelection
                {
                    None, Target, Area, Boss
                }

                public enum DOTBDebuffTargetSelection
                {
                    None, Target, Area, Boss
                }
                public enum DOTCDebuffTargetSelection
                {
                    None, Target, Area, Boss
                }
                public enum ShortHpSelection
                {
                    None, Self, Team
                }

                public enum ProcType1Selection
                {
                    DangerousCulture, Antiseptic, MuscleMemory, BloodTransfusion, RestrictiveBandaging
                }

                public enum ProcType2Selection
                {
                    MassiveVitaePlan, AnatomicBlight, HealingCare, Pathogen, Anesthetic, Astringent, Inflammation
                }

                private static class RelevantNanos
                {
                    //public static int[] IndividualShortHoTs = new[] { 43852, 43868, 43870, 43872, 43873, 43871, 42396, 43869, 43867, 43877, 43876, 43875, 43879,
                    //    42399, 43882, 43874, 43880, 42401 };

                    public const int ImprovedLC = 275011;
                    public static readonly Spell[] IndividualShortMaxHealths = Spell.GetSpellsForNanoline(NanoLine.DoctorShortHPBuffs).OrderByStackingOrder()
                        .Where(spell => spell.Id != ImprovedLC).ToArray();

                    public const int TiredLimbs = 99578;
                    public static readonly Spell[] InitDebuffs = Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder()
                        .Where(spell => spell.Id != TiredLimbs).ToArray();

                    public static int[] HPBuffs = new[] { 95709, 28662, 95720, 95712, 95710, 95711, 28649, 95713, 28660, 95715, 95714, 95718, 95716, 95717, 95719, 42397 };

                    public const int AlphaAndOmega = 42409;
                    public static int[] Heals = new[] { 223299, 223297, 223295, 223293, 223291, 223289, 223287, 223285, 223281, 43878, 43881, 43886, 43885,
                        43887, 43890, 43884, 43808, 43888, 43889, 43883, 43811, 43809, 43810, 28645, 43816, 43817, 43825, 43815,
                        43814, 43821, 43820, 28648, 43812, 43824, 43822, 43819, 43818, 43823, 28677, 43813, 43826, 43838, 43835,
                        28672, 43836, 28676, 43827, 43834, 28681, 43837, 43833, 43830, 43828, 28654, 43831, 43829, 43832, 28665 };
                    public static int[] TeamHeals = new[] { 273312, 273315, 270349, 43891, 223291, 43892, 43893, 43894, 43895, 43896, 43897, 43898, 43899,
                        43900, 43901, 43903, 43902, 42404, 43905, 43904, 42395, 43907, 43908, 43906, 42398, 43910, 43909, 42402,
                        43911, 43913, 42405, 43912, 43914, 43915, 27804, 43916, 43917, 42403, 42408 };
                }


                private static class RelevantItems
                {
                        public static readonly int[] Books = { 206242, 305514 };
                }

                #endregion
    }
}