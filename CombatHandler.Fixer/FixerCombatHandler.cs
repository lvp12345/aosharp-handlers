using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System.Linq;

namespace CombatHandler.Fixer
{
    public class FixerCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleRez = false;

        private static Window _buffWindow;
        private static Window _debuffWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;

        private double _lastBackArmorCheckTime = Time.NormalTime;

        private static double _ncuUpdateTime;

        public FixerCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalRez, OnGlobalRezMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.ClearBuffs, OnClearBuffs);
            IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

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

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("GlobalBuffing", true);
            _settings.AddVariable("GlobalComposites", true);
            _settings.AddVariable("GlobalRez", true);

            _settings.AddVariable("EncaseInStone", false);

            _settings.AddVariable("SharpObjects", true);
            _settings.AddVariable("Grenades", true);
            _settings.AddVariable("BulletsSelection", (int)BulletsSelection.None);

            _settings.AddVariable("StimTargetSelection", (int)StimTargetSelection.Self);

            _settings.AddVariable("Kits", true);

            _settings.AddVariable("ShortHOT", false);
            _settings.AddVariable("LongHOTSelection", (int)LongHOTSelection.None);
            _settings.AddVariable("SLMap", false);

            _settings.AddVariable("RunspeedSelection", (int)RunspeedSelection.None);
            _settings.AddVariable("ArmorSelection", (int)ArmorSelection.None);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.LucksCalamity);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.BootlegRemedies);

            _settings.AddVariable("EvasionDebuff", false);

            _settings.AddVariable("AOESnare", false);

            RegisterSettingsWindow("Fixer Handler", "FixerSettingsView.xml");

            //Perks
            RegisterPerkProcessor(PerkHash.EvasiveStance, EvasiveStance, CombatActionPriority.High);

            //Root/Snare
            RegisterSpellProcessor(RelevantNanos.SpinNanoweb, AOESnare, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.IntenseAgglutinativeNanoweb, Snare, CombatActionPriority.High);

            //Debuffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EvasionDebuffs).OrderByStackingOrder(), EvasionDecrease);

            //Items
            RegisterItemProcessor(RelevantItems.ClusterBullets, Cluster);
            RegisterItemProcessor(RelevantItems.HomingPermorphaBullets, Permorpha);

            //Hots
            RegisterSpellProcessor(RelevantNanos.LongHOT,
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "LongHOTSelection"));

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealOverTime).OrderByStackingOrder(), ShortHOT);

            RegisterSpellProcessor(RelevantNanos.GreaterPreservationMatrix,
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                            => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));

            //Runspeed
            RegisterSpellProcessor(RelevantNanos.RKTargetRunspeed, RKTarget);
            RegisterSpellProcessor(RelevantNanos.RKTeamRunspeed, RKTeam);
            RegisterSpellProcessor(RelevantNanos.ShadowlandsRunspeed, SLRunspeed);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonComabtTeamBuff(spell, fightingTarget, ref actionTarget));
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FixerDodgeBuffLine).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                            => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FixerSuppressorBuff).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                            => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));

            RegisterSpellProcessor(RelevantNanos.NCU, NCU);

            //Spawn Armor
            RegisterSpellProcessor(RelevantNanos.Grid, Grid);
            RegisterSpellProcessor(RelevantNanos.ShadowwebSpinner, ShadowwebSpinner);

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcFixerLucksCalamity, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerDirtyTricks, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerEscapeTheSystem, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerIntenseMetabolism, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerFishInABarrel, LEProc1, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcFixerBootlegRemedies, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerSlipThemAMickey, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerBendingTheRules, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerBackyardBandages, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerFightingChance, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerContaminatedBullets, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerUndergroundSutures, LEProc2, CombatActionPriority.Low);

            PluginDirectory = pluginDir;

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
        }
        public Window[] _windows => new Window[] { _buffWindow, _debuffWindow, _procWindow, _itemWindow, _perkWindow };

        #region Callbacks

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

        #endregion

        #region Handles

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //Cannot stop Multi-Tabs. Easy fix would be correct naming of views to reference against WindowOptions - options.Name
                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\FixerBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "FixerBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "FixerBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }
        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_perkView)) { return; }

                _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\FixerPerksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Perks", XmlViewName = "FixerPerksView" }, _perkView);

                window.FindView("SphereDelayBox", out TextInputView sphereInput);
                window.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                window.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                window.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                window.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                window.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);

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
            }
            else if (_perkWindow == null || (_perkWindow != null && !_perkWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "FixerPerksView" }, _perkView, out var container);
                _perkWindow = container;

                container.FindView("SphereDelayBox", out TextInputView sphereInput);
                container.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                container.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                container.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                container.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                container.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);

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
            }
        }
        private void HandleDebuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //Cannot stop Multi-Tabs. Easy fix would be correct naming of views to reference against WindowOptions - options.Name
                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\FixerDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "FixerDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "FixerDebuffsView" }, _debuffView, out var container);
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

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\FixerItemsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "FixerItemsView" }, _itemView);

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
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "FixerItemsView" }, _itemView, out var container);
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

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\FixerProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "FixerProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "FixerProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 2.4)
                return;

            base.OnUpdate(deltaTime);
            EquipBackArmor();

            if (Time.NormalTime > _ncuUpdateTime + 1.0f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }

            #region UI

            var window = SettingsController.FindValidWindow(_windows);

            if (window != null && window.IsValid)
            {
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
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
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

        #region Buffs

        private bool NCU(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, NanoLine.FixerNCUBuff, ref actionTarget);
        }

        protected bool Buff(Spell spell, NanoLine nanoline, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.FightingTarget != null || RelevantGenericNanos.ShrinkingGrowingflesh.Contains(spell.Id)) { return false; }

            if (SpellChecksPlayer(spell, nanoline))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        #endregion

        #region Debuffs

        private bool EvasionDecrease(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledTargetDebuff("EvasionDebuff", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Spawn Armor

        private bool Grid(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)
                || ArmorSelection.Grid != (ArmorSelection)_settings["ArmorSelection"].AsInt32()) { return false; }

            return !Inventory.Items.Any(x => RelevantItems.Grid.Contains(x.HighId));
        }

        private bool ShadowwebSpinner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)
                || ArmorSelection.ShadowwebSpinner != (ArmorSelection)_settings["ArmorSelection"].AsInt32()) { return false; }

            return !Inventory.Items.Any(x => RelevantItems.ShadowwebSpinner.Contains(x.HighId));
        }

        #endregion

        #region Hots

        private bool ShortHOT(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("ShortHOT") || !InCombat()) { return false; }

            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Runspeed

        private bool RKTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum() || RunspeedSelection.Rk != (RunspeedSelection)_settings["RunspeedSelection"].AsInt32()) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.ShadowlandsRunspeed))
            {
                CancelBuffs(RelevantNanos.ShadowlandsRunspeed);
            }

            if (Team.IsInTeam)
            {
                SimpleChar target = DynelManager.Players
                   .Where(c => c.IsInLineOfSight
                       && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                       && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                       && c.Health > 0
                       && !(c.Buffs.Contains(NanoLine.MajorEvasionBuffs) || c.Buffs.Contains(NanoLine.RunspeedBuffs)))
                   .FirstOrDefault();


                if (target != null)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = target;
                    return true;
                }
            }

            if (!DynelManager.LocalPlayer.Buffs.Contains(NanoLine.MajorEvasionBuffs)
                || !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.RunspeedBuffs))
            {
                return NonCombatBuff(spell, ref actionTarget, fightingTarget);
            }

            return false;

        }

        private bool RKTeam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum() || RunspeedSelection.RKTeam != (RunspeedSelection)_settings["RunspeedSelection"].AsInt32()) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.MajorEvasionBuffs)) { return false; }
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.RunspeedBuffs)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.ShadowlandsRunspeed))
            {
                CancelBuffs(RelevantNanos.ShadowlandsRunspeed);
            }
            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        private bool SLRunspeed(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum() || RunspeedSelection.Shadowlands != (RunspeedSelection)_settings["RunspeedSelection"].AsInt32()) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.MajorEvasionBuffs)) { return false; }
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.RunspeedBuffs)) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        #endregion

        #region Snare

        private bool AOESnare(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")
                || !IsSettingEnabled("AOESnare") || !CanCast(spell)) { return false; }

            SimpleChar target = DynelManager.Characters
                    .Where(c => c.IsInLineOfSight
                        && IsMoving(c)
                        && !c.Buffs.Contains(NanoLine.Root)
                        && (c.Name == "Flaming Vengeance"
                            || c.Name == "Hand of the Colonel"
                            || c.Name == "Alien Seeker"))
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .FirstOrDefault();

            if (target != null)
            {
                actionTarget.Target = target;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        private bool Snare(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")
                || !IsSettingEnabled("AOESnare") || !CanCast(spell)) { return false; }

            SimpleChar target = DynelManager.Characters
                    .Where(c => c.IsInLineOfSight
                        && c.IsMoving
                        && !c.Buffs.Contains(NanoLine.Root)
                        && c.Name == "Alien Heavy Patroller")
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .FirstOrDefault();

            if (target != null)
            {
                actionTarget.Target = target;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        #endregion

        #region Items

        private bool Cluster(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BulletsSelection.Cluster != (BulletsSelection)_settings["BulletsSelection"].AsInt32()) { return false; }

            if (fightingtarget == null) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.MGSMG)) { return false; }

            actionTarget.ShouldSetTarget = true;
            return true;
        }

        private bool Permorpha(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BulletsSelection.Permorpha != (BulletsSelection)_settings["BulletsSelection"].AsInt32()) { return false; }

            if (fightingtarget == null) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.MGSMG)) { return false; }

            actionTarget.ShouldSetTarget = true;
            return true;
        }

        #endregion

        #region Misc

        private static bool IsMoving(SimpleChar target)
        {
            if (Playfield.Identity.Instance == 4021)
            {
                return true;
            }

            return target.IsMoving;
        }

        private void EquipBackArmor()
        {
            if (ArmorSelection.Grid == (ArmorSelection)_settings["ArmorSelection"].AsInt32() && !HasBackItemEquipped() && Time.NormalTime - _lastBackArmorCheckTime > 6)
            {
                _lastBackArmorCheckTime = Time.NormalTime;
                Item backArmor = Inventory.Items.FirstOrDefault(x => RelevantItems.Grid.Contains(x.HighId));

                backArmor?.Equip(EquipSlot.Cloth_Back);
            }

            if (ArmorSelection.ShadowwebSpinner == (ArmorSelection)_settings["ArmorSelection"].AsInt32() && !HasBackItemEquipped() && Time.NormalTime - _lastBackArmorCheckTime > 6)
            {
                _lastBackArmorCheckTime = Time.NormalTime;
                Item backArmor = Inventory.Items.FirstOrDefault(x => RelevantItems.ShadowwebSpinner.Contains(x.HighId));

                backArmor?.Equip(EquipSlot.Cloth_Back);
            }
        }

        private bool HasBackItemEquipped()
        {
            return Inventory.Items.Any(itemCandidate => itemCandidate.Slot.Instance == (int)EquipSlot.Cloth_Back);
        }

        private static class RelevantNanos
        {
            public const int GreaterPreservationMatrix = 275679;
            public const int SpinNanoweb = 85216;
            public const int IntenseAgglutinativeNanoweb = 223143;
            
            public static readonly int[] RKTargetRunspeed = { 93132, 93126, 93127, 93128, 93129, 93130, 93131, 93125 };
            public static readonly int[] RKTeamRunspeed = { 162595, 162589, 162603, 162593, 162599, 162591, 162597, 162601 };
            public static readonly int[] ShadowlandsRunspeed = { 223125, 223131, 223129, 215718, 223127 };

            public static readonly int[] Grid = { 155189, 155187, 155188, 155186 };
            public static readonly int[] ShadowwebSpinner = { 273349, 224422, 224420, 224418, 224416, 224414, 224412, 224410, 224408, 224405, 224403 };
            public static readonly int[] NCU = { 275043, 163095, 163094, 163087, 163085, 163083, 163081, 163079, 162995 };
            public static readonly Spell[] LongHOT = Spell.GetSpellsForNanoline(NanoLine.FixerLongHoT).OrderByStackingOrder().Where(spell => spell.Id != GreaterPreservationMatrix).ToArray();
        }

        private static class RelevantItems
        {
            public static readonly int[] Grid = { 155172, 155173, 155174, 155150 };
            public static readonly int[] ShadowwebSpinner = { 273350, 224400, 224399, 224398, 224397, 224396, 224395, 224394, 224393, 224392, 224390 };
            public static readonly int[] ClusterBullets = { 300944, 158952, 158951 };
            public static readonly int[] HomingPermorphaBullets = { 246840, 246839 };
        }

        public enum ProcType1Selection
        {
            LucksCalamity = 1196774482,
            DirtyTricks = 1162630233,
            EscapeTheSystem = 1179208014,
            IntenseMetabolism = 1179538255,
            FishInABarrel = 1230195529
        }

        public enum ProcType2Selection
        {
            BootlegRemedies = 1195725889,
            SlipThemAMickey = 1179075400,
            BendingTheRules = 1146246226,
            BackyardBandages = 1397314637,
            FightingChance = 1112429640,
            ContaminatedBullets = 1145394241,
            UndergroundSutures = 1095259201
        }

        public enum BulletsSelection
        {
            None, Cluster, Permorpha
        }

        public enum ArmorSelection
        {
            None, ShadowwebSpinner, Grid
        }

        public enum RunspeedSelection
        {
            None, Rk, RKTeam, Shadowlands
        }

        public enum LongHOTSelection
        {
            None, Self, Team
        }

        #endregion
    }
}
