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

        private static Window _buffWindow;
        private static Window _tauntWindow;
        private static Window _procWindow;

        private static View _buffView;
        private static View _tauntView;
        private static View _procView;

        private static double _absorbs;
        private static double _areaTaunt;
        private static double _singleTaunt;

        private static int EnfTauntDelayArea;
        private static int EnfCycleAbsorbsDelay;
        private static int EnfTauntDelaySingle;

        private static double _ncuUpdateTime;

        public EnfCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            Config.CharSettings[Game.ClientInst].EnfTauntDelaySingleChangedEvent += EnfTauntDelaySingle_Changed;
            Config.CharSettings[Game.ClientInst].EnfTauntDelayAreaChangedEvent += EnfTauntDelayArea_Changed;
            Config.CharSettings[Game.ClientInst].EnfCycleAbsorbsDelayChangedEvent += EnfCycleAbsorbsDelay_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.RagingBlow);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.ViolationBuffer);

            _settings.AddVariable("SingleTauntsSelection", (int)SingleTauntsSelection.None);

            _settings.AddVariable("AreaTaunt", false);
            _settings.AddVariable("CycleAbsorbs", false);
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
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageChangeBuffs).OrderByStackingOrder(), DamageChangeBuff);
            RegisterSpellProcessor(RelevantNanos.FortifyBuffs, CycleAbsorbs, CombatActionPriority.High);
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
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), MeleeBuff);
            RegisterSpellProcessor(RelevantNanos.TargetedDamageShields, TeamBuff);
            RegisterSpellProcessor(RelevantNanos.TargetedHpBuff, TeamBuff);
            RegisterSpellProcessor(RelevantNanos.FOCUSED_ANGER, GenericBuff);

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
        }

        public Window[] _windows => new Window[] { _buffWindow, _tauntWindow, _procWindow };

        #region Events

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\EnforcerBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "EnforcerBuffsView" }, _buffView);

                window.FindView("DelayAbsorbsBox", out TextInputView absorbsInput);

                if (absorbsInput != null)
                {
                    absorbsInput.Text = $"{EnfCycleAbsorbsDelay}";
                }
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "EnforcerBuffsView" }, _buffView, out var container);
                _buffWindow = container;

                container.FindView("DelayAbsorbsBox", out TextInputView absorbsInput);

                if (absorbsInput != null)
                {
                    absorbsInput.Text = $"{EnfCycleAbsorbsDelay}";
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

        #region Callbacks
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

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            var window = SettingsController.FindValidWindow(_windows);

            if (window != null && window.IsValid)
            {
                window.FindView("DelaySingleBox", out TextInputView singleInput);
                window.FindView("DelayAreaBox", out TextInputView areaInput);
                window.FindView("DelayAbsorbsBox", out TextInputView absorbsInput);

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
            }
        }


        #region Perks

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

        #region Logic

        private bool DamageChangeBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HasBuffNanoLine(NanoLine.DamageChangeBuffs, DynelManager.LocalPlayer)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (SingleTauntsSelection.OS == (SingleTauntsSelection)_settings["SingleTauntsSelection"].AsInt32() 
                && Time.NormalTime > _singleTaunt + EnfTauntDelaySingle)
            {
                SimpleChar mob = DynelManager.NPCs
                    .Where(c => c.IsAttacking && c.FightingTarget != null
                        && c.IsInLineOfSight
                        && !debuffOSTargetsToIgnore.Contains(c.Name)
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && IsNotFightingMe(c)
                        && IsAttackingUs(c))
                    .OrderBy(c => c.MaxHealth)
                        //&& (c.FightingTarget.Profession != Profession.Enforcer
                        //        && c.FightingTarget.Profession != Profession.Soldier
                        //        && c.FightingTarget.Profession != Profession.MartialArtist))
                    .FirstOrDefault();

                if (mob != null)
                {
                    _singleTaunt = Time.NormalTime;
                    actionTarget.Target = mob;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            if (SingleTauntsSelection.Target == (SingleTauntsSelection)_settings["SingleTauntsSelection"].AsInt32() 
                && Time.NormalTime > _singleTaunt + EnfTauntDelaySingle)
            {
                if (fightingTarget != null)
                {
                    _singleTaunt = Time.NormalTime;
                    actionTarget.Target = fightingTarget;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        private bool TrollForm(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("TrollForm")) { return false; }

            return TrollFormPerk(perk, fightingTarget, ref actionTarget);
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

        private bool CycleAbsorbs(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Any(Buff => Buff.Id == RelevantNanos.BioCocoon)) { return false; }

            if (IsSettingEnabled("CycleAbsorbs") && Time.NormalTime > _absorbs + EnfCycleAbsorbsDelay)
            {
                _absorbs = Time.NormalTime;
                return true;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool TauntProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell) || !IsSettingEnabled("TauntProc")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool AreaTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (!IsSettingEnabled("AreaTaunt")) { return false; }

            if (EnfTauntDelayArea == 0)
            {
                return true;
            }

            if (Time.NormalTime > _areaTaunt + EnfTauntDelayArea
                && fightingTarget != null)
            {
                _areaTaunt = Time.NormalTime;
                return true;
            }

            return false;
        }

        #endregion

        #region Misc

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
            None, Target, OS
        }

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

        public static void EnfTauntDelaySingle_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].EnfTauntDelaySingle = e;
            EnfTauntDelaySingle = e;
            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }

        public static void EnfTauntDelayArea_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].EnfTauntDelayArea = e;
            EnfTauntDelayArea = e;
            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }
        public static void EnfCycleAbsorbsDelay_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].EnfCycleAbsorbsDelay = e;
            EnfCycleAbsorbsDelay = e;
            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }

        #endregion
    }
}