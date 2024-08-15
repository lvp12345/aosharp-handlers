using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
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
            try
            {
                IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalRez, OnGlobalRezMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.ClearBuffs, OnClearBuffs);
                IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

                Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentageChangedEvent += FountainOfLifeHealPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentageChangedEvent += BioCocoonPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelayChangedEvent += SingleTauntDelay_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].TimedTauntDelayChangedEvent += TimedTauntDelay_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetNameChangedEvent += StimTargetName_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].AMSPercentageChangedEvent += AMSPercentage_Changed;
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

                _settings.AddVariable("AllPlayers", false);
                _settings["AllPlayers"] = false;

                _settings.AddVariable("Buffing", true);
                _settings.AddVariable("Composites", true);

                _settings.AddVariable("GlobalBuffing", true);
                _settings.AddVariable("GlobalComposites", true);
                _settings.AddVariable("GlobalRez", true);

                _settings.AddVariable("DeTaunt", false);

                _settings.AddVariable("EncaseInStone", false);

                _settings.AddVariable("SharpObjects", false);
                _settings.AddVariable("Grenades", false);

                _settings.AddVariable("TauntTool", false);

                _settings.AddVariable("StimTargetSelection", 1);

                _settings.AddVariable("Kits", true);

                _settings.AddVariable("ShadowlandReflect", true);
                _settings.AddVariable("TeamArmorBuff", false);
                _settings.AddVariable("Evades", false);
                _settings.AddVariable("InitBuff", false);

                _settings.AddVariable("LEHealthDrain", false);

                _settings.AddVariable("AOEPerks", false);

                _settings.AddVariable("AAO", false);
                _settings.AddVariable("PistolTeam", false);
                _settings.AddVariable("CompHeavyArt", false);
                _settings.AddVariable("RiotControl", false);
                _settings.AddVariable("SLMap", false);

                _settings.AddVariable("SingleTauntsSelection", 2);
                _settings.AddVariable("TimedTauntsSelection", 2);

                _settings.AddVariable("RKReflectSelection", 0);

                _settings.AddVariable("NotumGrenades", false);
                _settings.AddVariable("MajorEvasionBuffs", false);

                _settings.AddVariable("LegShot", false);

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
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, "MajorEvasionBuffs"));
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
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.BurstBuff).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                             => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(29251, TeamRiotControl);

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

                Healing.FountainOfLifeHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentage;
                BioCocoonPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage;
                SingleTauntDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelay;
                TimedTauntDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].TimedTauntDelay;
                StimTargetName = Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetName;
                AMSPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].AMSPercentage;
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
        public Window[] _windows => new Window[] { _buffWindow, _healingWindow, _tauntWindow, _procWindow, _itemWindow, _perkWindow };

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

                window.FindView("AMSPercentageBox", out TextInputView AMSInput);

                if (AMSInput != null)
                {
                    AMSInput.Text = $"{AMSPercentage}";
                }
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "SoldierBuffsView" }, _buffView, out var container);
                _buffWindow = container;

                container.FindView("AMSPercentageBox", out TextInputView AMSInput);

                if (AMSInput != null)
                {
                    AMSInput.Text = $"{AMSPercentage}";
                }
            }
        }
        private void HandleHealingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_healingView)) { return; }

                _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\SoldierHealingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Healing", XmlViewName = "SoldierHealingView" }, _healingView);

                window.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);

                if (FountainOfLifeInput != null)
                {
                    FountainOfLifeInput.Text = $"{Healing.FountainOfLifeHealPercentage}";
                }
            }
            else if (_healingWindow == null || (_healingWindow != null && !_healingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_healingWindow, PluginDir, new WindowOptions() { Name = "Healing", XmlViewName = "SoldierHealingView" }, _healingView, out var container);
                _healingWindow = container;

                container.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);

                if (FountainOfLifeInput != null)
                {
                    FountainOfLifeInput.Text = $"{Healing.FountainOfLifeHealPercentage}";
                }
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

                window.FindView("SelfHealPerkPercentageBox", out TextInputView selfHealInput);
                window.FindView("SelfNanoPerkPercentageBox", out TextInputView selfNanoInput);
                window.FindView("TeamHealPerkPercentageBox", out TextInputView teamHealInput);
                window.FindView("TeamNanoPerkPercentageBox", out TextInputView teamNanoInput);

                if (bioCocoonInput != null)
                {
                    bioCocoonInput.Text = $"{BioCocoonPercentage}";
                }
                if (sphereInput != null)
                {
                    sphereInput.Text = $"{CycleSpherePerkDelay}";
                }
                if (witOfTheAtroxInput != null)
                {
                    witOfTheAtroxInput.Text = $"{CycleWitOfTheAtroxPerkDelay}";
                }
                if (selfHealInput != null)
                {
                    selfHealInput.Text = $"{SelfHealPerkPercentage}";
                }
                if (selfNanoInput != null)
                {
                    selfNanoInput.Text = $"{SelfNanoPerkPercentage}";
                }
                if (teamHealInput != null)
                {
                    teamHealInput.Text = $"{TeamHealPerkPercentage}";
                }
                if (teamNanoInput != null)
                {
                    teamNanoInput.Text = $"{TeamNanoPerkPercentage}";
                }
            }
            else if (_perkWindow == null || (_perkWindow != null && !_perkWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "SoldierPerksView" }, _perkView, out var container);
                _perkWindow = container;

                container.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);
                container.FindView("SphereDelayBox", out TextInputView sphereInput);
                container.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);

                container.FindView("SelfHealPerkPercentageBox", out TextInputView selfHealInput);
                container.FindView("SelfNanoPerkPercentageBox", out TextInputView selfNanoInput);
                container.FindView("TeamHealPerkPercentageBox", out TextInputView teamHealInput);
                container.FindView("TeamNanoPerkPercentageBox", out TextInputView teamNanoInput);

                if (bioCocoonInput != null)
                {
                    bioCocoonInput.Text = $"{BioCocoonPercentage}";
                }
                if (sphereInput != null)
                {
                    sphereInput.Text = $"{CycleSpherePerkDelay}";
                }
                if (witOfTheAtroxInput != null)
                {
                    witOfTheAtroxInput.Text = $"{CycleWitOfTheAtroxPerkDelay}";
                }
                if (selfHealInput != null)
                {
                    selfHealInput.Text = $"{SelfHealPerkPercentage}";
                }
                if (selfNanoInput != null)
                {
                    selfNanoInput.Text = $"{SelfNanoPerkPercentage}";
                }
                if (teamHealInput != null)
                {
                    teamHealInput.Text = $"{TeamHealPerkPercentage}";
                }
                if (teamNanoInput != null)
                {
                    teamNanoInput.Text = $"{TeamNanoPerkPercentage}";
                }
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
                {
                    singleInput.Text = $"{SingleTauntDelay}";
                }
                if (timedInput != null)
                {
                    timedInput.Text = $"{TimedTauntDelay}";
                }
            }
            else if (_tauntWindow == null || (_tauntWindow != null && !_tauntWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_tauntWindow, PluginDir, new WindowOptions() { Name = "Taunts", XmlViewName = "SoldierTauntsView" }, _tauntView, out var container);
                _tauntWindow = container;

                container.FindView("SingleTauntDelayBox", out TextInputView singleInput);
                container.FindView("TimedTauntDelayBox", out TextInputView timedInput);

                if (singleInput != null)
                {
                    singleInput.Text = $"{SingleTauntDelay}";
                }
                if (timedInput != null)
                {
                    timedInput.Text = $"{TimedTauntDelay}";
                }
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
                {
                    stimTargetInput.Text = $"{StimTargetName}";
                }
                if (stimHealthInput != null)
                {
                    stimHealthInput.Text = $"{StimHealthPercentage}";
                }
                if (stimNanoInput != null)
                {
                    stimNanoInput.Text = $"{StimNanoPercentage}";
                }
                if (kitHealthInput != null)
                {
                    kitHealthInput.Text = $"{KitHealthPercentage}";
                }
                if (kitNanoInput != null)
                {
                    kitNanoInput.Text = $"{KitNanoPercentage}";
                }
                if (bodyDevInput != null)
                {
                    bodyDevInput.Text = $"{BodyDevAbsorbsItemPercentage}";
                }
                if (strengthInput != null)
                {
                    strengthInput.Text = $"{StrengthAbsorbsItemPercentage}";
                }
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
                {
                    stimTargetInput.Text = $"{StimTargetName}";
                }
                if (stimHealthInput != null)
                {
                    stimHealthInput.Text = $"{StimHealthPercentage}";
                }
                if (stimNanoInput != null)
                {
                    stimNanoInput.Text = $"{StimNanoPercentage}";
                }
                if (kitHealthInput != null)
                {
                    kitHealthInput.Text = $"{KitHealthPercentage}";
                }
                if (kitNanoInput != null)
                {
                    kitNanoInput.Text = $"{KitNanoPercentage}";
                }
                if (bodyDevInput != null)
                {
                    bodyDevInput.Text = $"{BodyDevAbsorbsItemPercentage}";
                }
                if (strengthInput != null)
                {
                    strengthInput.Text = $"{StrengthAbsorbsItemPercentage}";
                }
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
            if (Game.IsZoning || Time.AONormalTime < _lastZonedTime + 0.6)
            {
                return;
            }

            if (Time.AONormalTime > _ncuUpdateTime + 1.0f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.AONormalTime;
            }

            #region UI

            var window = SettingsController.FindValidWindow(_windows);

            if (window != null && window.IsValid)
            {
                window.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);
                window.FindView("SingleTauntDelayBox", out TextInputView singleInput);
                window.FindView("TimedTauntDelayBox", out TextInputView timedInput);
                window.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);
                window.FindView("AMSPercentageBox", out TextInputView AMSInput);
                window.FindView("StimTargetBox", out TextInputView stimTargetInput);
                window.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                window.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                window.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                window.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);
                window.FindView("SphereDelayBox", out TextInputView sphereInput);
                window.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);

                window.FindView("SelfHealPerkPercentageBox", out TextInputView selfHealInput);
                window.FindView("SelfNanoPerkPercentageBox", out TextInputView selfNanoInput);
                window.FindView("TeamHealPerkPercentageBox", out TextInputView teamHealInput);
                window.FindView("TeamNanoPerkPercentageBox", out TextInputView teamNanoInput);

                window.FindView("BodyDevAbsorbsItemPercentageBox", out TextInputView bodyDevInput);
                window.FindView("StrengthAbsorbsItemPercentageBox", out TextInputView strengthInput);

                if (AMSInput != null && !string.IsNullOrEmpty(AMSInput.Text))
                {
                    if (int.TryParse(AMSInput.Text, out int AMSValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].AMSPercentage != AMSValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].AMSPercentage = AMSValue;
                        }
                    }
                }
                
                if (FountainOfLifeInput != null && !string.IsNullOrEmpty(FountainOfLifeInput.Text))
                {
                    if (int.TryParse(FountainOfLifeInput.Text, out int Value))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentage != Value)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentage = Value;
                        }
                    }
                }

                if (bioCocoonInput != null && !string.IsNullOrEmpty(bioCocoonInput.Text))
                {
                    if (int.TryParse(bioCocoonInput.Text, out int bioCocoonValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage != bioCocoonValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage = bioCocoonValue;
                        }
                    }
                }

                if (singleInput != null && !string.IsNullOrEmpty(singleInput.Text))
                {
                    if (int.TryParse(singleInput.Text, out int singleValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelay != singleValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelay = singleValue;
                        }
                    }
                }

                if (timedInput != null && !string.IsNullOrEmpty(timedInput.Text))
                {
                    if (int.TryParse(timedInput.Text, out int timedValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].TimedTauntDelay != timedValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].TimedTauntDelay = timedValue;
                        }
                    }
                }

                if (stimTargetInput != null)
                {
                    if (Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetName != stimTargetInput.Text)
                    {
                        Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetName = stimTargetInput.Text;
                    }
                }

                if (stimHealthInput != null && !string.IsNullOrEmpty(stimHealthInput.Text))
                {
                    if (int.TryParse(stimHealthInput.Text, out int stimHealthValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentage != stimHealthValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentage = stimHealthValue;
                        }
                    }
                }

                if (stimNanoInput != null && !string.IsNullOrEmpty(stimNanoInput.Text))
                {
                    if (int.TryParse(stimNanoInput.Text, out int stimNanoValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentage != stimNanoValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentage = stimNanoValue;
                        }
                    }
                }

                if (kitHealthInput != null && !string.IsNullOrEmpty(kitHealthInput.Text))
                {
                    if (int.TryParse(kitHealthInput.Text, out int kitHealthValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage != kitHealthValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage = kitHealthValue;
                        }
                    }
                }

                if (kitNanoInput != null && !string.IsNullOrEmpty(kitNanoInput.Text))
                {
                    if (int.TryParse(kitNanoInput.Text, out int kitNanoValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage != kitNanoValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage = kitNanoValue;
                        }
                    }
                }

                if (sphereInput != null && !string.IsNullOrEmpty(sphereInput.Text))
                {
                    if (int.TryParse(sphereInput.Text, out int sphereValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelay != sphereValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelay = sphereValue;
                        }
                    }
                }

                if (witOfTheAtroxInput != null && !string.IsNullOrEmpty(witOfTheAtroxInput.Text))
                {
                    if (int.TryParse(witOfTheAtroxInput.Text, out int witOfTheAtroxValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelay != witOfTheAtroxValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelay = witOfTheAtroxValue;
                        }
                    }
                }

                if (selfHealInput != null && !string.IsNullOrEmpty(selfHealInput.Text))
                {
                    if (int.TryParse(selfHealInput.Text, out int selfHealValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentage != selfHealValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentage = selfHealValue;
                        }
                    }
                }

                if (selfNanoInput != null && !string.IsNullOrEmpty(selfNanoInput.Text))
                {
                    if (int.TryParse(selfNanoInput.Text, out int selfNanoValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentage != selfNanoValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentage = selfNanoValue;
                        }
                    }
                }

                if (teamHealInput != null && !string.IsNullOrEmpty(teamHealInput.Text))
                {
                    if (int.TryParse(teamHealInput.Text, out int teamHealValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentage != teamHealValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentage = teamHealValue;
                        }
                    }
                }

                if (teamNanoInput != null && !string.IsNullOrEmpty(teamNanoInput.Text))
                {
                    if (int.TryParse(teamNanoInput.Text, out int teamNanoValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentage != teamNanoValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentage = teamNanoValue;
                        }
                    }
                }

                if (bodyDevInput != null && !string.IsNullOrEmpty(bodyDevInput.Text))
                {
                    if (int.TryParse(bodyDevInput.Text, out int bodyDevValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentage != bodyDevValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentage = bodyDevValue;
                        }
                    }
                }

                if (strengthInput != null && !string.IsNullOrEmpty(strengthInput.Text))
                {
                    if (int.TryParse(strengthInput.Text, out int strengthValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentage != strengthValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentage = strengthValue;
                        }
                    }
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

            base.OnUpdate(deltaTime);
        }

        #region Perks

        #endregion

        #region DeTaunt

        private bool DeTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["DeTaunt"].AsBool()) { return false; }
            if (!Team.IsInTeam) { return false; }
            if (DynelManager.LocalPlayer.NanoPercent < 30) { return false; }
            if (!CanCast(spell)) { return false; }

            var target = DynelManager.NPCs
                    .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name)
                        && c.FightingTarget != null
                        && c.Health > 0
                        && c.IsInLineOfSight
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && SpellChecksOther(spell, spell.Nanoline, c))
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .FirstOrDefault();

            if (target == null) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = target;
            return true;
        }

        #endregion

        #region Taunts

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var setting = _settings["SingleTauntsSelection"].AsInt32();
            if (setting == 0) { return false; }
            if (DynelManager.LocalPlayer.HealthPercent <= 30) { return false; }
            if (!CanCast(spell)) { return false; }
            if (debuffAreaTargetsToIgnore.Contains(fightingTarget?.Name)) { return false; }
            if (Time.AONormalTime < _singleTaunt + SingleTauntDelay) { return false; }

            switch (setting)
            {
                case 1:
                    if (fightingTarget == null) { return false; }

                    _singleTaunt = Time.AONormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = fightingTarget;
                    return true;

                case 2:
                    var mob = DynelManager.NPCs
                   .Where(c => c != null && c.IsAttacking && c.FightingTarget?.Identity != DynelManager.LocalPlayer.Identity
                       && c.IsInLineOfSight
                       && !debuffAreaTargetsToIgnore.Contains(c.Name)
                       && spell.IsInRange(c)
                       && AttackingTeam(c))
                   .OrderBy(c => c.MaxHealth)
                   .FirstOrDefault();

                    if (mob == null) { return false; }

                    _singleTaunt = Time.AONormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = mob;
                    return true;

                default:
                    return false;

            }
        }

        private bool TimedTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var selection = _settings["TimedTauntsSelection"].AsInt32();

            if (selection == 0) { return false; }
            if (fightingTarget == null) { return false; }
            if (debuffAreaTargetsToIgnore.Contains(fightingTarget?.Name)) { return false; }
            if (!CanCast(spell)) { return false; }
            if ( DynelManager.LocalPlayer.HealthPercent <= 30) { return false; }
            if (Time.AONormalTime < _timedTaunt + TimedTauntDelay) { return false; }

            switch (selection)
            {
                case 1:
                    _timedTaunt = Time.AONormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = fightingTarget;
                    return true;
                case 2:
                    var mob = DynelManager.NPCs
                    .Where(c => c != null && c.IsAttacking && c.FightingTarget?.Identity != DynelManager.LocalPlayer.Identity
                        && c.IsInLineOfSight
                        && !debuffAreaTargetsToIgnore.Contains(c.Name)
                        && spell.IsInRange(c)
                        && AttackingTeam(c))
                    .OrderBy(c => c.MaxHealth)
                    .FirstOrDefault();

                    if (mob == null) { return false; }

                    _timedTaunt = Time.AONormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = mob;
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Heals

        private bool LEDrainHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Buffing"].AsBool()) { return false; }

            if (DynelManager.LocalPlayer.FightingTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent <= 30) { return true; }

            return false;
        }

        #endregion

        #region Buffs

        private bool TeamRiotControl(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["RiotControl"].AsBool()) { return false; }
            if (!Team.IsInTeam) { return false; }

            var teamMember = Team.Members.Where(t => t?.Character != null
            && t.Character.Health > 0
            && t.Character.IsInLineOfSight
            && t.Character.SpecialAttacks.Contains(SpecialAttack.Burst)
            && SpellChecksOther(spell, spell.Nanoline, t.Character)
            && spell.IsInRange(t?.Character)).FirstOrDefault();

            if (teamMember == null) { return false; }

            actionTarget = (teamMember.Character, true);
            return true;

        }

        private bool HeavyCompBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["CompHeavyArt"].AsBool())
            {
                if (Team.IsInTeam) 
                {
                    var target = Team.Members.Where(t => t?.Character != null
                        && t.Character.Health > 0
                        && t.Character.IsInLineOfSight
                        && !t.Character.Buffs.Contains(NanoLine.FixerSuppressorBuff) && !t.Character.Buffs.Contains(NanoLine.AssaultRifleBuffs)
                        && HeavyCompWeaponChecks(t.Character)
                        && SpellChecksOther(spell, spell.Nanoline, t.Character)).FirstOrDefault()?.Character;

                    if (target == null) { return false; }

                    if (target.Identity == DynelManager.LocalPlayer.Identity
                        && GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(CharacterWieldedWeapon.AssaultRifle)) { return false; }

                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = target;
                    return true;
                }
                else
                {
                    return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Smg)
                      || BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Shotgun)
                      || BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade);
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
            switch (_settings["RKReflectSelection"].AsInt32())
            {
                case 1:
                    return NonCombatBuff(spell, ref actionTarget, fightingTarget);
                case 2:
                    return NonComabtTeamBuff(spell, fightingTarget, ref actionTarget);
                default:
                    return false;
            }
        }

        private bool Phalanx(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["TeamArmorBuff"].AsBool()) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(162357)) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }


        private bool AMS(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if   (!_settings["Buffing"].AsBool()) {  return false; }
            if   (!CanCast(spell)) { return false; }

            return DynelManager.LocalPlayer.HealthPercent <= AMSPercentage;
                       
        }

        #endregion

        #region Team Buffs
        private bool Evades(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Evades"].AsBool()) { return false; }
            if (IsInsideInnerSanctum()) { return false; }

            return NonComabtTeamBuff(spell, fightingTarget, ref actionTarget);
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
