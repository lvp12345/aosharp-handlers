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

namespace CombatHandler.NanoTechnician
{
    public class NTCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static int NTNanoAegisPercentage;
        private static int NTNullitySpherePercentage;
        private static int NTIzgimmersWealthPercentage;
        private static int NTCycleAbsorbsDelay;

        private static double _absorbs;

        private static Window _buffWindow;
        private static Window _procWindow;
        private static Window _debuffWindow;
        private static Window _nukeWindow;

        private static View _procView;
        private static View _buffView;
        private static View _debuffView;
        private static View _nukeView;

        private static double _ncuUpdateTime;

        public NTCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            Config.CharSettings[Game.ClientInst].NTNanoAegisPercentageChangedEvent += NTNanoAegisPercentage_Changed;
            Config.CharSettings[Game.ClientInst].NTNullitySpherePercentageChangedEvent += NTNullitySpherePercentage_Changed;
            Config.CharSettings[Game.ClientInst].NTIzgimmersWealthPercentageChangedEvent += NTIzgimmersWealthPercentage_Changed;
            Config.CharSettings[Game.ClientInst].NTCycleAbsorbsDelayChangedEvent += NTCycleAbsorbsDelay_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("CycleAbsorbs", false);

            _settings.AddVariable("AIDot", false);

            _settings.AddVariable("Pierce", false);
            _settings.AddVariable("FlimFocus", false);

            _settings.AddVariable("NanoHOTTeam", false);
            _settings.AddVariable("CostTeam", false);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.ThermalReprieve);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.OptimizedLibrary);

            _settings.AddVariable("BlindSelection", (int)BlindSelection.None);

            _settings.AddVariable("AOESelection", (int)AOESelection.None);

            RegisterSettingsWindow("Nano-Technician Handler", "NTSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianThermalReprieve, ThermalReprieve, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianHarvestEnergy, HarvestEnergy, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianLayeredAmnesty, LayeredAmnesty, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianSourceTap, SourceTap, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianCircularLogic, CircularLogic, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianOptimizedLibrary, OptimizedLibrary, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianAcceleratedReality, AcceleratedReality, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianLoopingService, LoopingService, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianPoweredNanoFortress, PoweredNanoFortress, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianIncreaseMomentum, IncreaseMomentum, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianUnstableLibrary, UnstableLibrary, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.FlimFocus, FlimFocus, CombatActionPriority.Low);


            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NullitySphereNano).OrderByStackingOrder(), NullitySphere, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.NanobotAegis, NanobotAegis, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.IzgimmersWealth, IzgimmersWealth, CombatActionPriority.High);

            RegisterSpellProcessor(RelevantNanos.NanobotShelter, NanoBotShelter);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Psy_IntBuff).OrderByStackingOrder(), PsyInt);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDamageMultiplierBuffs).OrderByStackingOrder(), NanoDamage);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NFRangeBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MatCreaBuff).OrderByStackingOrder(), MatCrea);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), GenericBuffExcludeInnerSanctum);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Fortify).OrderByStackingOrder(), Fortify);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoOverTime_LineA).OrderByStackingOrder(), NanoHOT);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NPCostBuff).OrderByStackingOrder(), Cost);

            //if (Spell.Find(RelevantNanos.SuperiorFleetingImmunity, out Spell immunity))
            //{
            //    RegisterSpellProcessor(immunity, GenericBuff);
            //}

            //Team buffs
            //RegisterSpellProcessor(RelevantNanos.AbsortAcTargetBuffs, GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AbsorbACBuff).OrderByStackingOrder(), CycleAbsorbs);

            //Nukes and DoTs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTNanotechnicianStrainA).OrderByStackingOrder(), AiDotNuke, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.Garuk, SingleTargetNuke, CombatActionPriority.Medium);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTNanotechnicianStrainB).OrderByStackingOrder(), PierceNuke, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.AOENukes, AOENuke);
            RegisterSpellProcessor(RelevantNanos.VolcanicEruption, VolcanicEruption);

            //Debuffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AAODebuffs).OrderByStackingOrder(), SingleBlind);
            RegisterSpellProcessor(RelevantNanos.AOEBlinds, AOEBlind);

            PluginDirectory = pluginDir;

            NTNanoAegisPercentage = Config.CharSettings[Game.ClientInst].NTNanoAegisPercentage;
            NTNullitySpherePercentage = Config.CharSettings[Game.ClientInst].NTNullitySpherePercentage;
            NTIzgimmersWealthPercentage = Config.CharSettings[Game.ClientInst].NTIzgimmersWealthPercentage;
            NTCycleAbsorbsDelay = Config.CharSettings[Game.ClientInst].NTCycleAbsorbsDelay;
        }
        public Window[] _windows => new Window[] { _buffWindow, _procWindow, _debuffWindow, _nukeWindow };

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

        #region Handles

        private void HandleDebuffsViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_debuffView)) { return; }

                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\NTDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "NTDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "NTDebuffsView" }, _debuffView, out var container);
                _debuffWindow = container;
            }
        }

        private void HandleNukesViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_nukeView)) { return; }

                _nukeView = View.CreateFromXml(PluginDirectory + "\\UI\\NTNukesView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Nukes", XmlViewName = "NTNukesView" }, _nukeView);
            }
            else if (_nukeWindow == null || (_nukeWindow != null && !_nukeWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_nukeWindow, PluginDir, new WindowOptions() { Name = "Nukes", XmlViewName = "NTNukesView" }, _nukeView, out var container);
                _nukeWindow = container;
            }
        }
        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_buffView)) { return; }

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\NTBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "NTBuffsView" }, _buffView);

                window.FindView("DelayAbsorbsBox", out TextInputView absorbsInput);

                if (absorbsInput != null)
                {
                    absorbsInput.Text = $"{NTCycleAbsorbsDelay}";
                }
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "NTBuffsView" }, _buffView, out var container);
                _buffWindow = container;

                container.FindView("DelayAbsorbsBox", out TextInputView absorbsInput);

                if (absorbsInput != null)
                {
                    absorbsInput.Text = $"{NTCycleAbsorbsDelay}";
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

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\NtProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "NtProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "NtProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
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
                window.FindView("DelayAbsorbsBox", out TextInputView absorbsInput);

                if (absorbsInput != null && !string.IsNullOrEmpty(absorbsInput.Text))
                {
                    if (int.TryParse(absorbsInput.Text, out int absorbsValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].NTCycleAbsorbsDelay != absorbsValue)
                        {
                            Config.CharSettings[Game.ClientInst].NTCycleAbsorbsDelay = absorbsValue;
                        }
                    }
                }
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("NTNanoAegisPercentageBox", out TextInputView nanoAegisInput);
                SettingsController.settingsWindow.FindView("NTNullitySpherePercentageBox", out TextInputView nullSphereInput);
                SettingsController.settingsWindow.FindView("NTIzgimmersWealthPercentageBox", out TextInputView izWealthInput);

                if (nanoAegisInput != null && !string.IsNullOrEmpty(nanoAegisInput.Text))
                {
                    if (int.TryParse(nanoAegisInput.Text, out int nanoAegisValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].NTNanoAegisPercentage != nanoAegisValue)
                        {
                            Config.CharSettings[Game.ClientInst].NTNanoAegisPercentage = nanoAegisValue;
                        }
                    }
                }
                if (nullSphereInput != null && !string.IsNullOrEmpty(nullSphereInput.Text))
                {
                    if (int.TryParse(nullSphereInput.Text, out int nullSphereValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].NTNullitySpherePercentage != nullSphereValue)
                        {
                            Config.CharSettings[Game.ClientInst].NTNullitySpherePercentage = nullSphereValue;
                        }
                    }
                }
                if (izWealthInput != null && !string.IsNullOrEmpty(izWealthInput.Text))
                {
                    if (int.TryParse(izWealthInput.Text, out int izWealthValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].NTIzgimmersWealthPercentage != izWealthValue)
                        {
                            Config.CharSettings[Game.ClientInst].NTIzgimmersWealthPercentage = izWealthValue;
                        }
                    }
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

                if (SettingsController.settingsWindow.FindView("DebuffsView", out Button debuffView))
                {
                    debuffView.Tag = SettingsController.settingsWindow;
                    debuffView.Clicked = HandleDebuffsViewClick;
                }

                if (SettingsController.settingsWindow.FindView("NukesView", out Button nukeView))
                {
                    nukeView.Tag = SettingsController.settingsWindow;
                    nukeView.Clicked = HandleNukesViewClick;
                }
            }
        }

        #region LE Procs

        private bool CircularLogic(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.CircularLogic != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool HarvestEnergy(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.HarvestEnergy != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool LayeredAmnesty(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.LayeredAmnesty != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool SourceTap(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.SourceTap != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool ThermalReprieve(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.ThermalReprieve != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool AcceleratedReality(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.AcceleratedReality != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool IncreaseMomentum(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.IncreaseMomentum != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool UnstableLibrary(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.UnstableLibrary != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool LoopingService(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.LoopingService != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool PoweredNanoFortress(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.PoweredNanoFortress != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool OptimizedLibrary(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.OptimizedLibrary != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Nukes

        private bool VolcanicEruption(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AOESelection.VE != (AOESelection)_settings["AOESelection"].AsInt32()
                || fightingTarget == null || !CanCast(spell)) { return false; }

            return true;
        }

        private bool PierceNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Pierce") || fightingTarget == null || !CanCast(spell)) { return false; }

            return true;
        }

        private bool AOENuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AOESelection.Normal != (AOESelection)_settings["AOESelection"].AsInt32()
                || fightingTarget == null || !CanCast(spell)) { return false; }

            return true;
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AOESelection.VE == (AOESelection)_settings["AOESelection"].AsInt32()
                || AOESelection.Normal == (AOESelection)_settings["AOESelection"].AsInt32()
                || fightingTarget == null) { return false; }

            //Task.Factory.StartNew(
            //    async () =>
            //    {
            //        DynelManager.LocalPlayer.SetStat(Stat.AggDef, 100);
            //        await Task.Delay(444);
            //        DynelManager.LocalPlayer.SetStat(Stat.AggDef, -100);
            //    });

            //if (DynelManager.LocalPlayer.GetStat(Stat.AggDef) == 100)
            //{
            //    return true;
            //}

            return true;
        }

        private bool AiDotNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AIDot") || fightingTarget == null || !CanCast(spell)) { return false; }

            if (fightingTarget.Health < 80000) { return false; }

            if (fightingTarget.Buffs.Find(spell.Id, out Buff buff) && buff.RemainingTime > 5) { return false; }

            return true;
        }

        #endregion

        #region Blinds

        private bool AOEBlind(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BlindSelection.AOE != (BlindSelection)_settings["BlindSelection"].AsInt32()
                || fightingTarget == null || !CanCast(spell)) { return false; }

            return !fightingTarget.Buffs.Contains(NanoLine.AAODebuffs);
        }

        private bool SingleBlind(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BlindSelection.Target != (BlindSelection)_settings["BlindSelection"].AsInt32()
                || fightingTarget == null || !CanCast(spell)) { return false; }

            return !fightingTarget.Buffs.Contains(NanoLine.AAODebuffs);
        }

        #endregion

        #region Perks

        private bool FlimFocus(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("FlimFocus")) { return false; }

            return CyclePerks(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Buffs

        private bool NanoDamage(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool Fortify(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool PsyInt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool MatCrea(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool NanoBotShelter(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool NanoHOT(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("NanoHOTTeam"))
                if (Team.IsInTeam)
                    return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool Cost(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("CostTeam"))
                if (Team.IsInTeam)
                    return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool NanobotAegis(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            return DynelManager.LocalPlayer.HealthPercent <= NTNanoAegisPercentage && !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.NullitySphereNano);
        }

        private bool NullitySphere(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            return DynelManager.LocalPlayer.HealthPercent <= NTNullitySpherePercentage && !DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.NanobotAegis);
        }

        private bool CycleAbsorbs(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Any(Buff => Buff.Id == RelevantNanos.BioCocoon)) { return false; }

            if (IsSettingEnabled("CycleAbsorbs") && Time.NormalTime > _absorbs + NTCycleAbsorbsDelay
                && (fightingTarget != null || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 0))
            {
                if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

                _absorbs = Time.NormalTime;
                return true;
            }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool IzgimmersWealth(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || fightingTarget == null || !CanCast(spell)) { return false; }

            return DynelManager.LocalPlayer.NanoPercent <= NTIzgimmersWealthPercentage;
        }

        #endregion

        #region Misc

        public static void NTNanoAegisPercentage_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].NTNanoAegisPercentage = e;
            NTNanoAegisPercentage = e;
            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }

        public static void NTNullitySpherePercentage_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].NTNullitySpherePercentage = e;
            NTNullitySpherePercentage = e;
            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }

        public static void NTIzgimmersWealthPercentage_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].NTIzgimmersWealthPercentage = e;
            NTIzgimmersWealthPercentage = e;
            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }

        public static void NTCycleAbsorbsDelay_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].NTCycleAbsorbsDelay = e;
            NTCycleAbsorbsDelay = e;
            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }

        public enum ProcType1Selection
        {
            ThermalReprieve, HarvestEnergy, LayeredAmnesty, SourceTap, CircularLogic
        }

        public enum ProcType2Selection
        {
            OptimizedLibrary, AcceleratedReality, LoopingService, PoweredNanoFortress, IncreaseMomentum, UnstableLibrary
        }
        public enum BlindSelection
        {
            None, Target, AOE
        }

        public enum AOESelection
        {
            None, Normal, VE
        }
        private static class RelevantNanos
        {
            public const int NanobotAegis = 302074;
            public const int IzgimmersWealth = 275024;
            public const int IzgimmersUltimatum = 218168;
            public const int Garuk = 275692;
            public const int PierceReflect = 266287;
            public const int VolcanicEruption = 28638;
            public const int BioCocoon = 209802;
            public static readonly int[] AOENukes = { 266293, 28638,
                266297, 28637, 28594, 45922, 45906, 45884, 28635, 266298, 28593, 45925, 45940, 45900,28629,
                45917, 45937, 28599, 45894, 45943, 28633, 28631 };
            public const int SuperiorFleetingImmunity = 273386;
            public static readonly Spell[] AbsortAcTargetBuffs = Spell.GetSpellsForNanoline(NanoLine.AbsorbACBuff).OrderByStackingOrder().Where(spell => spell.Id != SuperiorFleetingImmunity).ToArray();
            public static readonly int[] AOEBlinds = { 83959, 83960, 83961, 83962, 83963, 83964 };
            public static readonly int[] SingleTargetNukes = { 218168, 218164, 218162, 218160, 218158, 218156, 218154, 218152, 218150, 
                218148, 218146, 218144, 218142, 218140, 218138, 218136, 269473, 218134, 201935, 202262, 201933, 218132, 28618, 218124, 218130, 
                218122, 218120, 218128, 218118, 218126, 45226, 45192, 28619, 45230, 28623, 28604, 28616, 218116, 28597, 45210, 45236, 45197, 
                45233, 45247, 45199, 45235, 45234, 218114, 45258, 45217, 28600, 45198, 28613, 45919, 45195, 45225, 45260, 45891, 45254, 45890, 
                45213, 218112, 45215, 45915, 218104, 45252, 45214, 45251, 45929, 45220, 45920, 45222, 218102, 28598, 45911, 45237, 45216, 
                218110, 45913, 45901, 45212, 45206, 45912, 45883, 45245, 45140, 45904, 45218, 28626, 218108, 45261, 218100, 45909, 45203, 
                45228, 45903, 45200, 45939, 28592, 45242, 218098, 218106, 45885, 45926, 45241, 44538, 45908, 45250, 45934, 45138, 45932, 
                28632, 45205, 28609, 45209, 45246, 45935, 45921, 45227, 45207, 45942, 45191, 45924, 218096, 28610, 45914, 45208, 45893, 
                28621, 45211, 45916, 45933, 218094, 45240, 45259, 45941, 45910, 45253, 28614, 218092, 45221, 45204, 28634, 45196, 45886, 
                45201, 45928, 45193, 45323, 45244, 45889, 45895, 28605, 45219, 45223, 45938, 28628, 45232, 45248, 45898, 45202, 45923, 
                45229, 45907, 45139, 45887, 45231, 45882, 28627, 45936, 45194, 28639, 45243, 45931, 28630, 45137, 28607, 45257, 45880, 
                45256, 45249, 45888, 45255, 45881, 42543, 45927, 45902, 42540, 42541, 45899, 45905, 28611, 45897, 28601, 42542, 28608, 
                45918, 42539, 45892, 45930, 45879, 45896, 28612 };
            public static readonly int[] NanobotShelter = { 273388, 263265 };
            public static readonly int CompositeAttribute = 223372;
            public static readonly int CompositeNano = 223380;
        }

        #endregion
    }
}
