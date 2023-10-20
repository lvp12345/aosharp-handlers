using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System.Linq;


namespace CombatHandler.Soldier
{
    public class SoldCombathandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleRez = false;

        private static Window _buffWindow;
        private static Window _healingWindow;
        private static Window _tauntWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;

        private static View _buffView;
        private static View _healingView;
        private static View _tauntView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;

        private static double _singleTaunt;
        private static double _timedTaunt;

        private static double _ncuUpdateTime;

        public SoldCombathandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalRez, OnGlobalRezMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.ClearBuffs, OnClearBuffs);
            IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

            Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentageChangedEvent += BioCocoonPercentage_Changed;
            Config.CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelayChangedEvent += SingleTauntDelay_Changed;
            Config.CharSettings[DynelManager.LocalPlayer.Name].TimedTauntDelayChangedEvent += TimedTauntDelay_Changed;
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

            _settings.AddVariable("DeTaunt", false);

            _settings.AddVariable("EncaseInStone", false);

            _settings.AddVariable("SharpObjects", false);
            _settings.AddVariable("Grenades", false);

            _settings.AddVariable("ScorpioTauntTool", false);

            _settings.AddVariable("StimTargetSelection", (int)StimTargetSelection.Self);

            _settings.AddVariable("Kits", true);

            _settings.AddVariable("ShadowlandReflect", true);
            _settings.AddVariable("TeamArmorBuff", false);
            _settings.AddVariable("Evades", false);
            _settings.AddVariable("InitBuff", false);

            _settings.AddVariable("LEHealthDrain", false);

            _settings.AddVariable("AAO", false);
            _settings.AddVariable("PistolTeam", false);
            _settings.AddVariable("CompHeavyArt", false);
            _settings.AddVariable("RiotControl", false);

            _settings.AddVariable("SingleTauntsSelection", (int)SingleTauntsSelection.None);
            _settings.AddVariable("TimedTauntsSelection", (int)TimedTauntsSelection.None);

            _settings.AddVariable("RKReflectSelection", (int)RKReflectSelection.None);

            _settings.AddVariable("NotumGrenades", false);

            _settings.AddVariable("LegShot", false);
            _settings.AddVariable("DamagePerk", false);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.FuriousAmmunition);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.GrazeJugularVein);

            RegisterSettingsWindow("Soldier Handler", "SoldierSettingsView.xml");

            //DeTaunt
            RegisterSpellProcessor(RelevantNanos.DeTaunt, DeTaunt);

            //Heals
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DrainHeal).OrderByStackingOrder(), LEDrainHeal);

            //Taunts
            RegisterSpellProcessor(RelevantNanos.TimedTauntBuffs, TimedTargetTaunt, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.SingleTauntBuffs, SingleTargetTaunt, CombatActionPriority.High);

            //Taunt Tools
            RegisterItemProcessor(244655, 244655, TauntTool);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ReflectShield).Where(c => c.Name.Contains("Mirror")).OrderByStackingOrder(), AMS);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ReflectShield).Where(c => !c.Name.Contains("Mirror")).OrderByStackingOrder(), RKReflects);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadowlandReflectBase).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonCombatBuff(spell, ref actionTarget, fightingTarget, "ShadowlandReflect"));

            RegisterSpellProcessor(RelevantNanos.Phalanx, Phalanx);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HPBuff).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                            => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SiphonBox683).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonCombatBuff(spell, ref actionTarget, fightingTarget, "NotumGrenades"));
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                            => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierFullAutoBuff).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                            => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TotalFocus).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                            => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierShotgunBuff).OrderByStackingOrder(), Shotgun);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HeavyWeaponsBuffs).OrderByStackingOrder(), HeavyWeapon);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(),
            (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                => NonComabtTeamBuff(buffSpell, fightingTarget, ref actionTarget, "TeamArmorBuff"));

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(),
            (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                => NonComabtTeamBuff(buffSpell, fightingTarget, ref actionTarget, "InitBuff"));

            RegisterSpellProcessor(RelevantNanos.ArBuffs, AssaultRifle);
            RegisterSpellProcessor(RelevantNanos.CompositeHeavyArtillery, HeavyCompBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierDamageBase).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                            => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AAOBuffs).OrderByStackingOrder(), AAO);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolTeam);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.BurstBuff).OrderByStackingOrder(), RiotControl);

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.Precognition, Evades);

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcSoldierFuriousAmmunition, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcSoldierTargetAcquired, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcSoldierReconditioned, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcSoldierConcussiveShot, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcSoldierEmergencyBandages, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcSoldierSuccessfulTargeting, LEProc1, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcSoldierFuseBodyArmor, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcSoldierOnTheDouble, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcSoldierGrazeJugularVein, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcSoldierGearAssaultAbsorption, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcSoldierDeepSixInitiative, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcSoldierShootArtery, LEProc2, CombatActionPriority.Low);

            PluginDirectory = pluginDir;

            BioCocoonPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage;
            SingleTauntDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelay;
            TimedTauntDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].TimedTauntDelay;
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
        public Window[] _windows => new Window[] { _buffWindow, _tauntWindow, _procWindow, _itemWindow, _perkWindow };

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


                if (window.Views.Contains(_buffView)) { return; }

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\SoldierBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "SoldierBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "SoldierBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }
        private void HandleHealingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\SoldierHealingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Healing", XmlViewName = "SoldierHealingView" }, _healingView);

                window.FindView("HealPercentageBox", out TextInputView healInput);
                window.FindView("HealthDrainPercentageBox", out TextInputView healthDrainInput);

                if (healInput != null)
                    healInput.Text = $"{HealPercentage}";

                if (healthDrainInput != null)
                    healthDrainInput.Text = $"{HealthDrainPercentage}";
            }
            else if (_healingWindow == null || (_healingWindow != null && !_healingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_healingWindow, PluginDir, new WindowOptions() { Name = "Healing", XmlViewName = "SoldierHealingView" }, _healingView, out var container);
                _healingWindow = container;

                container.FindView("HealPercentageBox", out TextInputView healInput);
                container.FindView("HealthDrainPercentageBox", out TextInputView healthDrainInput);

                if (healInput != null)
                    healInput.Text = $"{HealPercentage}";

                if (healthDrainInput != null)
                    healthDrainInput.Text = $"{HealthDrainPercentage}";
            }
        }
        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_perkView)) { return; }

                _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\SoldierPerksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Perks", XmlViewName = "SoldierPerksView" }, _perkView);

                window.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);
                window.FindView("SphereDelayBox", out TextInputView sphereInput);
                window.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                window.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                window.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                window.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                window.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);

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
            }
            else if (_perkWindow == null || (_perkWindow != null && !_perkWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "SoldierPerksView" }, _perkView, out var container);
                _perkWindow = container;

                container.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);
                container.FindView("SphereDelayBox", out TextInputView sphereInput);
                container.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                container.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                container.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                container.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                container.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);

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
            }
        }

        private void HandleTauntViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                _tauntView = View.CreateFromXml(PluginDirectory + "\\UI\\SoldierTauntsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Taunts", XmlViewName = "SoldierTauntsView" }, _tauntView);

                window.FindView("SingleTauntDelayBox", out TextInputView singleInput);
                window.FindView("TimedTauntDelayBox", out TextInputView timedInput);

                if (singleInput != null)
                    singleInput.Text = $"{SingleTauntDelay}";
                if (timedInput != null)
                    timedInput.Text = $"{TimedTauntDelay}";
            }
            else if (_tauntWindow == null || (_tauntWindow != null && !_tauntWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_tauntWindow, PluginDir, new WindowOptions() { Name = "Taunts", XmlViewName = "SoldierTauntsView" }, _tauntView, out var container);
                _tauntWindow = container;

                container.FindView("SingleTauntDelayBox", out TextInputView singleInput);
                container.FindView("TimedTauntDelayBox", out TextInputView timedInput);

                if (singleInput != null)
                    singleInput.Text = $"{SingleTauntDelay}";
                if (timedInput != null)
                    timedInput.Text = $"{TimedTauntDelay}";
            }
        }

        private void HandleItemViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_itemView)) { return; }

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\SoldierItemsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "SoldierItemsView" }, _itemView);

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
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "SoldierItemsView" }, _itemView, out var container);
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

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\SoldierProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "SoldierProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "SoldierProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 0.6)
                return;

            base.OnUpdate(deltaTime);

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
                window.FindView("SingleTauntDelayBox", out TextInputView singleInput);
                window.FindView("TimedTauntDelayBox", out TextInputView timedInput);
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

                if (bioCocoonInput != null && !string.IsNullOrEmpty(bioCocoonInput.Text))
                    if (int.TryParse(bioCocoonInput.Text, out int bioCocoonValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage != bioCocoonValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage = bioCocoonValue;

                if (singleInput != null && !string.IsNullOrEmpty(singleInput.Text))
                    if (int.TryParse(singleInput.Text, out int singleValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelay != singleValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelay = singleValue;

                if (timedInput != null && !string.IsNullOrEmpty(timedInput.Text))
                    if (int.TryParse(timedInput.Text, out int timedValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].TimedTauntDelay != timedValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].TimedTauntDelay = timedValue;

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

        #region Perks

        #endregion

        #region DeTaunt

        private bool DeTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.NanoPercent < 30) { return false; }

            if (!CanCast(spell) || !IsSettingEnabled("DeTaunt")) { return false; }
            if (!Team.IsInTeam) { return false; }

            SimpleChar target = DynelManager.NPCs
                    .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name)
                        && c.FightingTarget != null
                        && c.Health > 0
                        && c.IsInLineOfSight
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && SpellChecksOther(spell, spell.Nanoline, c))
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = target;
                return true;
            }

            return false;
        }

        #endregion

        #region Taunts

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

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

            if (SingleTauntsSelection.Boss == (SingleTauntsSelection)_settings["SingleTauntsSelection"].AsInt32()
                && Time.NormalTime > _singleTaunt + SingleTauntDelay)
            {
                if (fightingTarget != null && fightingTarget?.MaxHealth > 1000000)
                {
                    _singleTaunt = Time.NormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = fightingTarget;
                    return true;
                }
            }

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

            return false;
        }

        private bool TimedTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (TimedTauntsSelection.Target == (TimedTauntsSelection)_settings["TimedTauntsSelection"].AsInt32()
                && Time.NormalTime > _timedTaunt + TimedTauntDelay)
            {
                if (fightingTarget != null)
                {
                    _timedTaunt = Time.NormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = fightingTarget;
                    return true;
                }
            }

            if (TimedTauntsSelection.Boss == (TimedTauntsSelection)_settings["TimedTauntsSelection"].AsInt32()
                && Time.NormalTime > _timedTaunt + TimedTauntDelay)
            {
                if (fightingTarget != null && fightingTarget?.MaxHealth > 1000000)
                {
                    _timedTaunt = Time.NormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = fightingTarget;
                    return true;
                }
            }

            if (TimedTauntsSelection.Area == (TimedTauntsSelection)_settings["TimedTauntsSelection"].AsInt32()
                && Time.NormalTime > _timedTaunt + TimedTauntDelay)
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
                    _timedTaunt = Time.NormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = mob;
                    return true;
                }
                else if (fightingTarget != null)
                {
                    _timedTaunt = Time.NormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = fightingTarget;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Heals

        private bool LEDrainHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (DynelManager.LocalPlayer.FightingTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent <= 30) { return true; }

            return false;
        }

        #endregion

        #region Buffs

        private bool AAO(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AAO")) { return false; }
            {
                if (Team.IsInTeam)
                {
                    SimpleChar target = DynelManager.Players
                        .Where(c => c.IsInLineOfSight
                            && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                            && c.Health > 0
                            && !c.Buffs.Contains(NanoLine.AAOBuffs) && !c.Buffs.Contains(NanoLine.AdventurerMorphBuff)
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

        private bool PistolTeam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam && IsSettingEnabled("PistolTeam"))
                return TeamBuffExclusionWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);

            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        private bool RiotControl(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("RiotControl")) { return false; }
            {
                if (Team.IsInTeam)
                {
                    SimpleChar target = DynelManager.Players
                        .Where(c => c.IsInLineOfSight
                            && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                            && c.Health > 0
                            && c.SpecialAttacks.Contains(SpecialAttack.Burst)
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

        private bool HeavyCompBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("CompHeavyArt"))
            {
                if (Team.IsInTeam)
                {
                    SimpleChar target = DynelManager.Players
                        .Where(c => c.IsInLineOfSight
                            && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                            && c.Health > 0
                            && !c.Buffs.Contains(NanoLine.FixerSuppressorBuff) && !c.Buffs.Contains(NanoLine.AssaultRifleBuffs)
                            && HeavyCompWeaponChecks(c)
                            && SpellChecksOther(spell, spell.Nanoline, c))
                        .FirstOrDefault();

                    if (target != null)
                    {
                        if (target.Identity == DynelManager.LocalPlayer.Identity
                            && GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(CharacterWieldedWeapon.AssaultRifle)) { return false; }

                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = target;
                        return true;
                    }
                }
            }

            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Smg)
                                || BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Shotgun)
                                || BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade);
        }

        private bool HeavyWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.HeavyWeapons);
        }

        private bool Shotgun(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Shotgun);
        }

        private bool AssaultRifle(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.AssaultRifle);
        }

        private bool RKReflects(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (RKReflectSelection.RubiKa == (RKReflectSelection)_settings["RKReflectSelection"].AsInt32())
                return NonCombatBuff(spell, ref actionTarget, fightingTarget);

            if (RKReflectSelection.RubiKaTeam == (RKReflectSelection)_settings["RKReflectSelection"].AsInt32())
                return NonComabtTeamBuff(spell, fightingTarget, ref actionTarget);

            return false;
        }

        private bool Phalanx(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("TeamArmorBuff")) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(162357)) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }


        private bool AMS(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (fightingtarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent <= 85) { return true; }

            return false;
        }

        #endregion

        #region Team Buffs
        private bool Evades(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (IsSettingEnabled("Evades"))
                return NonComabtTeamBuff(spell, fightingTarget, ref actionTarget);

            return false;
        }

        #endregion

        #region Misc

        private bool HeavyCompWeaponChecks(SimpleChar _target)
        {
            return GetWieldedWeapons(_target).HasFlag(CharacterWieldedWeapon.AssaultRifle)
                                || GetWieldedWeapons(_target).HasFlag(CharacterWieldedWeapon.Smg)
                                || GetWieldedWeapons(_target).HasFlag(CharacterWieldedWeapon.Shotgun)
                                || (GetWieldedWeapons(_target).HasFlag(CharacterWieldedWeapon.Grenade) && _target.Profession != Profession.Engineer);
        }

        public enum RKReflectSelection
        {
            None, RubiKa, RubiKaTeam
        }

        public enum SingleTauntsSelection
        {
            None, Target, Boss, Area
        }

        public enum TimedTauntsSelection
        {
            None, Target, Boss, Area
        }

        public enum ProcType1Selection
        {
            FuriousAmmunition = 1229865293,
            TargetAcquired = 1196573778,
            Reconditioned = 1398096452,
            ConcussiveShot = 1095188813,
            EmergencyBandages = 1163018573,
            SuccessfulTargeting = 1129730888
        }

        public enum ProcType2Selection
        {
            FuseBodyArmor = 1381190981,
            OnTheDouble = 1179992397,
            GrazeJugularVein = 1381256527,
            GearAssaultAbsorption = 1128617037,
            DeepSixInitiative = 1162691137,
            ShootArtery = 1397899609
        }

        private static class RelevantNanos
        {
            public static readonly int[] SingleTauntBuffs = { 223209, 223207, 223205, 223203, 223201, 29242, 100207,
            29218, 100205, 100206, 100208, 29228};
            public static readonly int[] TimedTauntBuffs = { 229104, 229102, 229100, 229098, 229096, 229094, 229092,
            229090};
            public static readonly int[] DeTaunt = { 223221, 223219, 223217, 223215, 223213, 223211 };
            public const int CompositeHeavyArtillery = 269482;
            public const int Phalanx = 29245;
            public const int Precognition = 29247;
            public static readonly int[] ArBuffs = { 275027, 203119, 29220, 203121 };
        }

        private static class RelevantItems
        {
            public const int DreadlochEnduranceBooster = 267168;
            public const int DreadlochEnduranceBoosterNanomageEdition = 267167;
        }

        #endregion
    }
}
