using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using static CombatHandler.Generic.PerkCondtionProcessors;

namespace CombatHandler.Doctor
{
    class DocCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleRez = false;

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

        private static double _ncuUpdateTime;

        public DocCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalRez, OnGlobalRezMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.ClearBuffs, OnClearBuffs);
            IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

            Config.CharSettings[DynelManager.LocalPlayer.Name].HealPercentageChangedEvent += HealPercentage_Changed;
            Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentageChangedEvent += CompleteHealPercentage_Changed;
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
            Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal1PercentageChangedEvent += BattleGroupHeal1Percentage_Changed;
            Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal2PercentageChangedEvent += BattleGroupHeal2Percentage_Changed;
            Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal3PercentageChangedEvent += BattleGroupHeal3Percentage_Changed;
            Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal4PercentageChangedEvent += BattleGroupHeal4Percentage_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("GlobalBuffing", true);
            _settings.AddVariable("GlobalComposites", true);
            _settings.AddVariable("GlobalRez", true);

            _settings.AddVariable("SharpObjects", false);
            _settings.AddVariable("Grenades", false);

            _settings.AddVariable("TOTWBooks", true);

            _settings.AddVariable("StimTargetSelection", (int)StimTargetSelection.None);

            _settings.AddVariable("Kits", true);

            _settings.AddVariable("TreatmentBuffSelection", (int)TreatmentBuffSelection.Self);
            _settings.AddVariable("StrengthBuffSelection", (int)StrengthBuffSelection.Self);

            _settings.AddVariable("InitDebuffSelection", (int)InitDebuffSelection.None);

            _settings.AddVariable("NanoTransmission", false);

            _settings.AddVariable("HealSelection", (int)HealSelection.SingleTeam);

            _settings.AddVariable("InitBuffSelection", (int)InitBuffSelection.Team);

            _settings.AddVariable("NukingSelection", (int)NukingSelection.Boss);

            _settings.AddVariable("DOTA", (int)DOTADebuffTargetSelection.Boss);
            _settings.AddVariable("DOTB", (int)DOTBDebuffTargetSelection.Boss);
            _settings.AddVariable("DOTC", (int)DOTCDebuffTargetSelection.Boss);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.DangerousCulture);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.MassiveVitaePlan);

            _settings.AddVariable("PistolTeam", true);

            _settings.AddVariable("NanoResistSelection", (int)NanoResistSelection.None);
            _settings.AddVariable("HealDeltaBuffSelection", (int)HealDeltaBuffSelection.None);

            _settings.AddVariable("ShortHpSelection", (int)ShortHpSelection.None);
            _settings.AddVariable("ShortHOT", false);

            _settings.AddVariable("CH", true);
            _settings.AddVariable("LockCH", false);

            RegisterSettingsWindow("Doctor Handler", "DocSettingsView.xml");

            //Healing
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CompleteHealingLine).OrderByStackingOrder(),
                CompleteHealing, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.AlphaAndOmega, LockCH, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.Heals, Healing, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.TeamHeals, TeamHealing, CombatActionPriority.High);

            //Perks
            RegisterPerkProcessor(PerkHash.BattlegroupHeal1, BattleGroupHeal1);
            RegisterPerkProcessor(PerkHash.BattlegroupHeal2, BattleGroupHeal2);
            RegisterPerkProcessor(PerkHash.BattlegroupHeal3, BattleGroupHeal3);
            RegisterPerkProcessor(PerkHash.BattlegroupHeal4, BattleGroupHeal4);

            RegisterPerkProcessor(PerkHash.NanoTransmission, NanoTransmission);
            RegisterPerkProcessor(PerkHash.CloseCall, CloseCall);

            //Hots
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealOverTime).OrderByStackingOrder(), ShortHOT);

            RegisterSpellProcessor(RelevantNanos.ImprovedLC, ImprovedLifeChanneler);
            RegisterSpellProcessor(RelevantNanos.IndividualShortMaxHealths, ShortMaxHealth);

            //Debuffs
            RegisterSpellProcessor(RelevantNanos.InitDebuffs,
                 (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) 
                 => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "InitDebuffSelection"),
                 CombatActionPriority.Medium);

            //Nukes
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Nuke).OrderByStackingOrder(), SingleTargetNuke, CombatActionPriority.Low);

            //Dots
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineA).OrderByStackingOrder(),
                (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) 
                => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "DOTA"),
                CombatActionPriority.Medium
            );

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineB).OrderByStackingOrder(),
                (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) 
                => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "DOTB"),
                CombatActionPriority.Medium
            );

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTStrainC).OrderByStackingOrder(),
                (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) 
                => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "DOTC"),
                CombatActionPriority.Medium
            );

            //Items
            RegisterItemProcessor(new int[] { RelevantItems.SacredTextoftheImmortalOne, RelevantItems.TeachingsoftheImmortalOne }, TOTWHeal);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.HPBuffs, MaxHealth);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolTeam);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "InitBuffSelection"));

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceBuffs).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) 
                => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "NanoResistSelection"));

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealDeltaBuff).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "HealDeltaBuffSelection"));

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FirstAidAndTreatmentBuff).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) 
                => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "TreatmentBuffSelection"));

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.StrengthBuff).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "StrengthBuffSelection"));

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcDoctorDangerousCulture, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorAntiseptic, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorMuscleMemory, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorBloodTransfusion, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorRestrictiveBandaging, LEProc1, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcDoctorMassiveVitaePlan, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorAnatomicBlight, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorHealingCare, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorPathogen, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorAnesthetic, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorAstringent, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorInflammation, LEProc2, CombatActionPriority.Low);

            PluginDirectory = pluginDir;

            HealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].HealPercentage;
            CompleteHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentage;
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
            BattleGroupHeal1Percentage = Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal1Percentage;
            BattleGroupHeal2Percentage = Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal2Percentage;
            BattleGroupHeal3Percentage = Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal3Percentage;
            BattleGroupHeal4Percentage = Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal4Percentage;
        }

        public Window[] _windows => new Window[] { _buffWindow, _debuffWindow, _healingWindow, _procWindow, _itemWindow, _perkWindow };

        #region Callbacks

        public static void OnRemainingNCUMessage(int sender, IPCMessage msg)
        {
            RemainingNCUMessage ncuMessage = (RemainingNCUMessage)msg;
            SettingsController.RemainingNCU[ncuMessage.Character] = ncuMessage.RemainingNCU;

            //Chat.WriteLine("Received RemainingNCUMessage");
        }
        private void OnGlobalBuffingMessage(int sender, IPCMessage msg)
        {
            GlobalBuffingMessage buffMsg = (GlobalBuffingMessage)msg;

            if (DynelManager.LocalPlayer.Identity.Instance == sender) { return; }

            _settings[$"Buffing"] = buffMsg.Switch;
            _settings[$"GlobalBuffing"] = buffMsg.Switch;

            //Chat.WriteLine("Received GlobalBuffingMessage");
        }
        private void OnGlobalCompositesMessage(int sender, IPCMessage msg)
        {
            GlobalCompositesMessage compMsg = (GlobalCompositesMessage)msg;

            if (DynelManager.LocalPlayer.Identity.Instance == sender) { return; }

            _settings[$"Composites"] = compMsg.Switch;
            _settings[$"GlobalComposites"] = compMsg.Switch;

            //Chat.WriteLine("Received GlobalCompositesMessage");
        }

        private void OnGlobalRezMessage(int sender, IPCMessage msg)
        {
            GlobalRezMessage rezMsg = (GlobalRezMessage)msg;

            if (DynelManager.LocalPlayer.Identity.Instance == sender) { return; }

            _settings[$"GlobalRez"] = rezMsg.Switch;
            _settings[$"GlobalRez"] = rezMsg.Switch;

            //Chat.WriteLine("Received GlobalRezMessage");

        }

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
            if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 2.0)
                return;

            base.OnUpdate(deltaTime);

            if (Time.NormalTime > _ncuUpdateTime + 1.0f)
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
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].HealPercentage != healValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].HealPercentage = healValue;

                if (completeHealInput != null && !string.IsNullOrEmpty(completeHealInput.Text))
                    if (int.TryParse(completeHealInput.Text, out int completeHealValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentage != completeHealValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentage = completeHealValue;

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

                if (bg1Input != null && !string.IsNullOrEmpty(bg1Input.Text))
                    if (int.TryParse(bg1Input.Text, out int bg1Value))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal1Percentage != bg1Value)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal1Percentage = bg1Value;

                if (bg2Input != null && !string.IsNullOrEmpty(bg2Input.Text))
                    if (int.TryParse(bg2Input.Text, out int bg2Value))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal2Percentage != bg2Value)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal2Percentage = bg2Value;

                if (bg3Input != null && !string.IsNullOrEmpty(bg3Input.Text))
                    if (int.TryParse(bg3Input.Text, out int bg3Value))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal3Percentage != bg3Value)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal3Percentage = bg3Value;

                if (bg4Input != null && !string.IsNullOrEmpty(bg4Input.Text))
                    if (int.TryParse(bg4Input.Text, out int bg4Value))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal4Percentage != bg4Value)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal4Percentage = bg4Value;
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

        #region LE Procs

        protected bool LEProc1(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (perk.Hash != ((PerkHash)_settings["ProcType1Selection"].AsInt32()))
                return false;

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        protected bool LEProc2(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (perk.Hash != ((PerkHash)_settings["ProcType2Selection"].AsInt32()))
                return false;

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Healing


        private bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("CH") || CompleteHealPercentage == 0) { return false; }

            if (IsSettingEnabled("LockCH")) { return false; }

            return FindMemberWithHealthBelow(CompleteHealPercentage, spell, ref actionTarget);
        }

        private bool LockCH(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("LockCH"))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        //private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (HealPercentage == 0) { return false; }

        //    if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
        //    {
        //        if (Team.IsInTeam)
        //        {
        //            List<SimpleChar> dyingTeamMember = DynelManager.Characters
        //                .Where(c => Team.Members
        //                    .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
        //                        .Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
        //                     && c.HealthPercent <= 90 && c.HealthPercent >= 30)
        //                .ToList();

        //            if (dyingTeamMember.Count >= 4) { return false; }
        //        }

        //        return FindMemberWithHealthBelow(HealPercentage, spell, ref actionTarget);
        //    }

        //    if (HealSelection.SingleArea == (HealSelection)_settings["HealSelection"].AsInt32())
        //    {
        //        return FindPlayerWithHealthBelow(HealPercentage, spell, ref actionTarget);
        //    }

        //    return false;
        //}

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HealPercentage == 0)
            {
                return false;
            }

            int healSelection = _settings["HealSelection"].AsInt32();

            if (HealSelection.SingleTeam == (HealSelection)healSelection)
            {
                if (Team.IsInTeam)
                {
                    int teamIndex = Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex;
                    int count = 0;

                    foreach (SimpleChar c in DynelManager.Characters)
                    {
                        if (Team.Members.Any(m => m.TeamIndex == teamIndex && m.Identity.Instance == c.Identity.Instance)
                            && c.HealthPercent <= 90 && c.HealthPercent >= 30)
                        {
                            count++;
                            if (count >= 4)
                            {
                                return false;
                            }
                        }
                    }
                }

                return FindMemberWithHealthBelow(HealPercentage, spell, ref actionTarget);
            }

            if (HealSelection.SingleArea == (HealSelection)healSelection)
            {
                return FindPlayerWithHealthBelow(HealPercentage, spell, ref actionTarget);
            }

            return false;
        }

        //private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (HealPercentage == 0) { return false; }

        //    if (HealSelection.Team == (HealSelection)_settings["HealSelection"].AsInt32())
        //        return FindMemberWithHealthBelow(HealPercentage, spell, ref actionTarget);

        //    if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
        //    {
        //        if (Spell.List.Any(c => c.Id == 275011)) { return false; }

        //        if (Team.IsInTeam)
        //        {
        //            List<SimpleChar> dyingTeamMember = DynelManager.Characters
        //                .Where(c => Team.Members
        //                    .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
        //                        .Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
        //                        && c.HealthPercent <= 90 && c.HealthPercent >= 30)
        //                .ToList();

        //            if (dyingTeamMember.Count >= 1)
        //            {
        //                return CanCast(spell);
        //            }
        //        }
        //    }

        //    return false;
        //}

        private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HealPercentage == 0)
            {
                return false;
            }

            int healSelection = _settings["HealSelection"].AsInt32();

            if (HealSelection.Team == (HealSelection)healSelection)
            {
                return FindMemberWithHealthBelow(HealPercentage, spell, ref actionTarget);
            }

            if (HealSelection.SingleTeam == (HealSelection)healSelection)
            {
                if (Spell.List.Any(c => c.Id == 275011))
                {
                    return false;
                }

                if (Team.IsInTeam)
                {
                    int teamIndex = Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex;

                    foreach (SimpleChar c in DynelManager.Characters)
                    {
                        if (Team.Members.Any(m => m.TeamIndex == teamIndex && m.Identity.Instance == c.Identity.Instance)
                            && c.HealthPercent <= 90 && c.HealthPercent >= 30)
                        {
                            return CanCast(spell);
                        }
                    }
                }
            }

            return false;
        }


        private bool ShortHOT(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("ShortHOT") || !InCombat()) { return false; }

            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Buffs

        private bool MaxHealth(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool PistolTeam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam && IsSettingEnabled("PistolTeam"))
                return TeamBuffExclusionWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);

            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        private bool ShortMaxHealth(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ShortHpSelection.Team == (ShortHpSelection)_settings["ShortHpSelection"].AsInt32())
                return GenericCombatTeamBuff(spell, fightingTarget, ref actionTarget);

            if (ShortHpSelection.None == (ShortHpSelection)_settings["ShortHpSelection"].AsInt32()) { return false; }

            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
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

        #endregion

        #region Nuke

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.NanoPercent < 40) { return false; }

            if (NukingSelection.None == (NukingSelection)_settings["NukingSelection"].AsInt32()) { return false; }

            if (NukingSelection.Target == (NukingSelection)_settings["NukingSelection"].AsInt32())
            {
                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (NukingSelection.Boss == (NukingSelection)_settings["NukingSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }
            return false;
        }

        #endregion

        #region Perks

        private bool NanoTransmission(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("NanoTransmission")) { return false; }

            return CombatBuffPerk(perk, fightingTarget, ref actionTarget);
        }

        private bool CloseCall(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null)
            {
                return false;
            }

            if (Team.IsInTeam)
            {
                SimpleChar teamMember = null;
                foreach (SimpleChar c in DynelManager.Players)
                {
                    if (Team.Members.Any(t => t.Identity.Instance == c.Identity.Instance)
                        && c.HealthPercent < 50
                        && c.IsInLineOfSight
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 20f
                        && c.Health > 0)
                    {
                        if (teamMember == null
                            || c.HealthPercent < teamMember.HealthPercent
                            || (c.Profession == Profession.Doctor && teamMember.Profession != Profession.Doctor)
                            || (c.Profession == Profession.Enforcer && teamMember.Profession != Profession.Doctor && teamMember.Profession != Profession.Enforcer)
                            || (c.Profession == Profession.Soldier && teamMember.Profession != Profession.Doctor && teamMember.Profession != Profession.Enforcer && teamMember.Profession != Profession.Soldier))
                        {
                            teamMember = c;
                        }
                    }
                }

                if (teamMember != null)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = teamMember;
                    return true;
                }

                return false;
            }

            if (DynelManager.LocalPlayer.HealthPercent < 50)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }


        #endregion

        #region Items

        private bool TOTWHeal(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("TOTWBooks") || Item.HasPendingUse || DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BiologicalMetamorphosis))
            {
                return false;
            }

            if (Team.IsInTeam)
            {
                SimpleChar teamMember = null;
                foreach (SimpleChar c in DynelManager.Players)
                {
                    if (Team.Members.Any(t => t.Identity.Instance == c.Identity.Instance)
                        && c.HealthPercent < 25
                        && c.IsInLineOfSight
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 20f
                        && c.Health > 0)
                    {
                        if (teamMember == null
                            || c.HealthPercent < teamMember.HealthPercent
                            || (c.Profession == Profession.Doctor && teamMember.Profession != Profession.Doctor)
                            || (c.Profession == Profession.Enforcer && teamMember.Profession != Profession.Doctor && teamMember.Profession != Profession.Enforcer)
                            || (c.Profession == Profession.Soldier && teamMember.Profession != Profession.Doctor && teamMember.Profession != Profession.Enforcer && teamMember.Profession != Profession.Soldier))
                        {
                            teamMember = c;
                        }
                    }
                }

                if (teamMember != null)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = teamMember;
                    return true;
                }

                return false;
            }

            if (DynelManager.LocalPlayer.HealthPercent < 25)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }


        #endregion

        #region Misc

        public enum HealSelection
        {
            None, SingleTeam, SingleArea, Team, ImprovedLifeChanneler
        }

        public enum NukingSelection
        {
            None, Target, Boss
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
        public enum InitDebuffSelection
        {
            None, Target, Area, Boss
        }
        public enum ShortHpSelection
        {
            None, Self, Team
        }

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
        public enum NanoResistSelection
        {
            None, Self, Team
        }
        public enum HealDeltaBuffSelection
        {
            None, Self, Team
        }

        public enum ProcType1Selection
        {
            DangerousCulture = 1111774273,
            Antiseptic = 1096177490,
            MuscleMemory = 1313229889,
            BloodTransfusion = 1145979733,
            RestrictiveBandaging = 1414547023
        }

        public enum ProcType2Selection
        {
            MassiveVitaePlan = 1229931077,
            AnatomicBlight = 1414025031,
            HealingCare = 1498502234,
            Pathogen = 1128874835,
            Anesthetic = 1263289936,
            Astringent = 1296908628,
            Inflammation = 1381188174
        }

        private static class RelevantNanos
        {

            public const int ImprovedLC = 275011;

            public static readonly Spell[] IndividualShortMaxHealths = Spell.GetSpellsForNanoline(NanoLine.DoctorShortHPBuffs).OrderByStackingOrder()
                .Where(spell => spell.Id != ImprovedLC).ToArray();

            public const int TiredLimbs = 99578;

            public static readonly Spell[] InitDebuffs = Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder()
                .Where(spell => spell.Id != TiredLimbs).ToArray();

            public static int[] HPBuffs = new[] { 95709, 28662, 95720, 95712, 95710, 95711, 28649, 95713, 28660, 95715, 95714, 95718, 95716, 95717, 95719, 42397 };

            public const int AlphaAndOmega = 42409;
            public static int[] Heals = new[]
            { 223299, 223297, 223295, 223293, 223291, 223289, 223287, 223285, 223281, 43878, 43881, 43886, 43885,
                        43887, 43890, 43884, 43808, 43888, 43889, 43883, 43811, 43809, 43810, 28645, 43816, 43817, 43825, 43815,
                        43814, 43821, 43820, 28648, 43812, 43824, 43822, 43819, 43818, 43823, 28677, 43813, 43826, 43838, 43835,
                        28672, 43836, 28676, 43827, 43834, 28681, 43837, 43833, 43830, 43828, 28654, 43831, 43829, 43832, 28665
            };
            public static int[] TeamHeals = new[]
            { 273312, 273315, 270349, 43891, 223291, 43892, 43893, 43894, 43895, 43896, 43897, 43898, 43899,
                        43900, 43901, 43903, 43902, 42404, 43905, 43904, 42395, 43907, 43908, 43906, 42398, 43910, 43909, 42402,
                        43911, 43913, 42405, 43912, 43914, 43915, 27804, 43916, 43917, 42403, 42408
            };
        }


        private static class RelevantItems
        {
            public const int SacredTextoftheImmortalOne = 305514;
            public const int TeachingsoftheImmortalOne = 206242;
        }

        #endregion
    }
}