using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System.Linq;
using System;
using AOSharp.Core.IPC;
using CombatHandler.Generic;

namespace CombatHandler.Enf
{
    class EnfCombatHandler : GenericCombatHandler
    { 
        private static string PluginDirectory;

        private static int EnfTauntDelayArea;
        private static int EnfCycleAbsorbsDelay;
        private static int EnfCycleRageDelay;
        private static int EnfCycleChallengerDelay;
        private static int EnfTauntDelaySingle;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleDebuffing = false;

        private static Window _buffWindow;
        private static Window _tauntWindow;
        private static Window _procWindow;
        private static Window _itemWindow;

        private static View _buffView;
        private static View _tauntView;
        private static View _procView;
        private static View _itemView;     

        private static double _absorbs;
        private static double _challenger;
        private static double _rage;
        private static double _areaTaunt;
        private static double _singleTaunt;

        private static double _ncuUpdateTime;

        public EnfCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
            //IPCChannel.RegisterCallback((int)IPCOpcode.GlobalDebuffing, OnGlobalDebuffingMessage);

            Config.CharSettings[Game.ClientInst].EnfTauntDelaySingleChangedEvent += EnfTauntDelaySingle_Changed;
            Config.CharSettings[Game.ClientInst].EnfTauntDelayAreaChangedEvent += EnfTauntDelayArea_Changed;
            Config.CharSettings[Game.ClientInst].EnfCycleAbsorbsDelayChangedEvent += EnfCycleAbsorbsDelay_Changed;
            Config.CharSettings[Game.ClientInst].EnfCycleChallengerDelayChangedEvent += EnfCycleChallengerDelay_Changed;
            Config.CharSettings[Game.ClientInst].EnfCycleRageDelayChangedEvent += EnfCycleRageDelay_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("GlobalBuffing", true);
            _settings.AddVariable("GlobalComposites", true);
            //_settings.AddVariable("GlobalDebuffs", true);

            _settings.AddVariable("Kits", true);
            _settings.AddVariable("Stims", true);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.RagingBlow);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.ViolationBuffer);

            _settings.AddVariable("SingleTauntsSelection", (int)SingleTauntsSelection.None);

            _settings.AddVariable("AreaTaunt", false);
            _settings.AddVariable("CycleAbsorbs", false);
            _settings.AddVariable("CycleChallenger", false);
            _settings.AddVariable("CycleRage", false);
            _settings.AddVariable("TauntProc", false);

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
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HPBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MongoBuff).OrderByStackingOrder(), AreaTaunt, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.SingleTargetTaunt, SingleTargetTaunt, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageChangeBuffs).OrderByStackingOrder(), DamageChange);
            RegisterSpellProcessor(RelevantNanos.FortifyBuffs, CycleAbsorbs, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Rage).OrderByStackingOrder(), CycleRage, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Challenger).OrderByStackingOrder(), CycleChallenger, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EnforcerTauntProcs).OrderByStackingOrder(), TauntProc);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(RelevantNanos.Melee1HB, Melee1HBBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.Melee1HE, Melee1HEBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.Melee2HE, Melee2HEBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.Melee2HB, Melee2HBBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.MeleePierce, MeleePierceBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.MeleeEnergy, MeleeEnergyBuffWeapon);

            //Team buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), Melee);
            RegisterSpellProcessor(RelevantNanos.TargetedDamageShields, GenericTeamBuff);
            RegisterSpellProcessor(RelevantNanos.TargetedHpBuff, GenericTeamBuff);
            RegisterSpellProcessor(RelevantNanos.FOCUSED_ANGER, GenericTeamBuff);

            RegisterItemProcessor(244655, 244655, TauntTool);

            //if (TauntTools.CanUseTauntTool())
            //{
            //    Item tauntTool = TauntTools.GetBestTauntTool();
            //    RegisterItemProcessor(tauntTool.LowId, tauntTool.HighId, TauntTool);
            //}

            PluginDirectory = pluginDir;

            EnfTauntDelayArea = Config.CharSettings[Game.ClientInst].EnfTauntDelayArea;
            EnfTauntDelaySingle = Config.CharSettings[Game.ClientInst].EnfTauntDelaySingle;
            EnfCycleAbsorbsDelay = Config.CharSettings[Game.ClientInst].EnfCycleAbsorbsDelay;
            EnfCycleChallengerDelay = Config.CharSettings[Game.ClientInst].EnfCycleChallengerDelay;
            EnfCycleRageDelay = Config.CharSettings[Game.ClientInst].EnfCycleRageDelay;
        }

        public Window[] _windows => new Window[] { _buffWindow, _tauntWindow, _procWindow, _itemWindow };

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

                window.FindView("DelayAbsorbsBox", out TextInputView absorbsInput);
                window.FindView("DelayChallengerBox", out TextInputView challengerInput);
                window.FindView("DelayRageBox", out TextInputView rageInput);

                if (absorbsInput != null)
                {
                    absorbsInput.Text = $"{EnfCycleAbsorbsDelay}";
                }
                if (challengerInput != null)
                {
                    challengerInput.Text = $"{EnfCycleChallengerDelay}";
                }
                if (rageInput != null)
                {
                    rageInput.Text = $"{EnfCycleRageDelay}";
                }
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "EnforcerBuffsView" }, _buffView, out var container);
                _buffWindow = container;

                container.FindView("DelayAbsorbsBox", out TextInputView absorbsInput);
                container.FindView("DelayChallengerBox", out TextInputView challengerInput);
                container.FindView("DelayRageBox", out TextInputView rageInput);

                if (absorbsInput != null)
                {
                    absorbsInput.Text = $"{EnfCycleAbsorbsDelay}";
                }
                if (challengerInput != null)
                {
                    challengerInput.Text = $"{EnfCycleChallengerDelay}";
                }
                if (rageInput != null)
                {
                    rageInput.Text = $"{EnfCycleRageDelay}";
                }
            }
        }

        private void HandleTauntViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                _tauntView = View.CreateFromXml(PluginDirectory + "\\UI\\EnforcerTauntsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Taunts", XmlViewName = "EnforcerTauntsView" }, _tauntView);

                window.FindView("DelaySingleBox", out TextInputView singleInput);
                window.FindView("DelayAreaBox", out TextInputView areaInput);

                if (singleInput != null)
                {
                    singleInput.Text = $"{EnfTauntDelaySingle}";
                }
                if (areaInput != null)
                {
                    areaInput.Text = $"{EnfTauntDelayArea}";
                }
            }
            else if (_tauntWindow == null || (_tauntWindow != null && !_tauntWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_tauntWindow, PluginDir, new WindowOptions() { Name = "Taunts", XmlViewName = "EnforcerTauntsView" }, _tauntView, out var container);
                _tauntWindow = container;

                container.FindView("DelaySingleBox", out TextInputView singleInput);
                container.FindView("DelayAreaBox", out TextInputView areaInput);

                if (singleInput != null)
                {
                    singleInput.Text = $"{EnfTauntDelaySingle}";
                }
                if (areaInput != null)
                {
                    areaInput.Text = $"{EnfTauntDelayArea}";
                }
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
            }
            else if (_itemWindow == null || (_itemWindow != null && !_itemWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "EnforcerItemsView" }, _itemView, out var container);
                _itemWindow = container;
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

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            if (Game.IsZoning)
                return;

            base.OnUpdate(deltaTime);

            var window = SettingsController.FindValidWindow(_windows);

            if (window != null && window.IsValid)
            {
                window.FindView("DelaySingleBox", out TextInputView singleInput);
                window.FindView("DelayAreaBox", out TextInputView areaInput);
                window.FindView("DelayAbsorbsBox", out TextInputView absorbsInput);
                window.FindView("DelayChallengerBox", out TextInputView challengerInput);
                window.FindView("DelayRageBox", out TextInputView rageInput);

                if (singleInput != null && !string.IsNullOrEmpty(singleInput.Text))
                {
                    if (int.TryParse(singleInput.Text, out int singleValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].EnfTauntDelaySingle != singleValue)
                        {
                            Config.CharSettings[Game.ClientInst].EnfTauntDelaySingle = singleValue;
                        }
                    }
                }
                if (areaInput != null && !string.IsNullOrEmpty(areaInput.Text))
                {
                    if (int.TryParse(areaInput.Text, out int aoeValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].EnfTauntDelayArea != aoeValue)
                        {
                            Config.CharSettings[Game.ClientInst].EnfTauntDelayArea = aoeValue;
                        }
                    }
                }
                if (absorbsInput != null && !string.IsNullOrEmpty(absorbsInput.Text))
                {
                    if (int.TryParse(absorbsInput.Text, out int absorbsValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].EnfCycleAbsorbsDelay != absorbsValue)
                        {
                            Config.CharSettings[Game.ClientInst].EnfCycleAbsorbsDelay = absorbsValue;
                        }
                    }
                }
                if (challengerInput != null && !string.IsNullOrEmpty(challengerInput.Text))
                {
                    if (int.TryParse(challengerInput.Text, out int challengerValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].EnfCycleChallengerDelay != challengerValue)
                        {
                            Config.CharSettings[Game.ClientInst].EnfCycleChallengerDelay = challengerValue;
                        }
                    }
                }
                if (rageInput != null && !string.IsNullOrEmpty(rageInput.Text))
                {
                    if (int.TryParse(rageInput.Text, out int rageValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].EnfCycleRageDelay != rageValue)
                        {
                            Config.CharSettings[Game.ClientInst].EnfCycleRageDelay = rageValue;
                        }
                    }
                }
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

                if (SettingsController.settingsWindow.FindView("ItemsView", out Button itemView))
                {
                    itemView.Tag = SettingsController.settingsWindow;
                    itemView.Clicked = HandleItemViewClick;
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
            return Buff(spell, NanoLine.DamageChangeBuffs, fightingTarget, ref actionTarget);
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (SingleTauntsSelection.Area == (SingleTauntsSelection)_settings["SingleTauntsSelection"].AsInt32() 
                && Time.NormalTime > _singleTaunt + EnfTauntDelaySingle)
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
            }

            if (SingleTauntsSelection.Target == (SingleTauntsSelection)_settings["SingleTauntsSelection"].AsInt32() 
                && Time.NormalTime > _singleTaunt + EnfTauntDelaySingle)
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
            if (IsSettingEnabled("CycleChallenger") && Time.NormalTime > _challenger + EnfCycleChallengerDelay
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
            if (IsSettingEnabled("CycleRage") && Time.NormalTime > _rage + EnfCycleRageDelay
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

            if (IsSettingEnabled("CycleAbsorbs") && Time.NormalTime > _absorbs + EnfCycleAbsorbsDelay
                && (fightingTarget != null || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 0))
            {
                if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

                _absorbs = Time.NormalTime;
                return true;
            }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool TauntProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("TauntProc")) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool AreaTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !IsSettingEnabled("AreaTaunt") || !CanCast(spell)) { return false; }

            if (DynelManager.NPCs.Any(c => c.Health > 0
                    && c.Name == "Alien Heavy Patroller"
                    && c.Position.DistanceFrom(DynelManager.LocalPlayer.Position) <= 18f)) { return false; }

            if (Time.NormalTime > _areaTaunt + EnfTauntDelayArea
                && (fightingTarget != null || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 0))
            {
                _areaTaunt = Time.NormalTime;
                return DynelManager.NPCs.Any(c => c.Health > 0
                    && c.FightingTarget?.IsPet == false
                    && c.Position.DistanceFrom(DynelManager.LocalPlayer.Position) <= 15f);
            }

            return false;
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
            public static readonly Spell[] TargetedDamageShields = Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder().Where(spell => spell.Id != ICE_BURN).ToArray();
            public const int MONGO_KRAKEN = 273322;
            public const int MONGO_DEMOLISH = 270786;
            public const int FOCUSED_ANGER = 29641;
            public const int IMPROVED_ESSENCE_OF_BEHEMOTH = 273629;
            public const int CORUSCATING_SCREEN = 55751;
            public const int ICE_BURN = 269460;
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

        public static void EnfTauntDelaySingle_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].EnfTauntDelaySingle = e;
            EnfTauntDelaySingle = e;
            Config.Save();
        }

        public static void EnfTauntDelayArea_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].EnfTauntDelayArea = e;
            EnfTauntDelayArea = e;
            Config.Save();
        }
        public static void EnfCycleAbsorbsDelay_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].EnfCycleAbsorbsDelay = e;
            EnfCycleAbsorbsDelay = e;
            Config.Save();
        }
        public static void EnfCycleChallengerDelay_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].EnfCycleChallengerDelay = e;
            EnfCycleChallengerDelay = e;
            Config.Save();
        }
        public static void EnfCycleRageDelay_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].EnfCycleRageDelay = e;
            EnfCycleRageDelay = e;
            Config.Save();
        }

        #endregion
    }
}