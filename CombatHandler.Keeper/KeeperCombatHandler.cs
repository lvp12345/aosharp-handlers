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

namespace CombatHandler.Keeper
{
    public class KeeperCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static Window _buffWindow;
        private static Window _procWindow;

        private static View _buffView;
        private static View _procView;

        private static double _ncuUpdateTime;

        public KeeperCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("RecastAntiFear", false);

            //LE Proc
            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.RighteousSmite);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.HonorRestored);

            //Auras
            _settings.AddVariable("AuraSet1Selection", (int)AuraSet1Selection.Heal);
            _settings.AddVariable("AuraSet2Selection", (int)AuraSet2Selection.Damage);
            _settings.AddVariable("AuraSet3Selection", (int)AuraSet3Selection.AAO);
            _settings.AddVariable("AuraSet4Selection", (int)AuraSet4Selection.Sanc);

            RegisterSettingsWindow("Keeper Handler", "KeeperSettingsView.xml");

            RegisterPerkProcessors();


            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcKeeperRighteousSmite, RighteousSmite, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperSymbioticBypass, SymbioticBypass, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperVirtuousReaper, VirtuousReaper, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperIgnoreTheUnrepentant, IgnoreTheUnrepentant, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperPureStrike, PureStrike, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperEschewTheFaithless, EschewTheFaithless, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperRighteousStrike, RighteousStrike, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcKeeperHonorRestored, HonorRestored, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperAmbientPurification, AmbientPurification, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperBenevolentBarrier, BenevolentBarrier, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperSubjugation, Subjugation, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperFaithfulReconstruction, FaithfulReconstruction, CombatActionPriority.Low);

            //Anti-Fear
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.KeeperFearImmunity).OrderByStackingOrder(), RecastAntiFear);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Fortify).OrderByStackingOrder().OrderByStackingOrder(), Buff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine._2HEdgedBuff).OrderByStackingOrder(), Buff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Fury).OrderByStackingOrder(), Buff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.KeeperDeflect_RiposteBuff).OrderByStackingOrder(), Buff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), Buff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.KeeperEvade_Dodge_DuckBuff).OrderByStackingOrder(), Buff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.KeeperStr_Stam_AgiBuff).OrderByStackingOrder(), Buff);

            RegisterSpellProcessor(RelevantNanos.HealAuras, HpAura);
            RegisterSpellProcessor(RelevantNanos.NanoAuras, NpAura);
            RegisterSpellProcessor(RelevantNanos.ReflectAuras, BarrierAura);
            RegisterSpellProcessor(RelevantNanos.AAOAuras, ImminenceAura);
            RegisterSpellProcessor(RelevantNanos.DerootAuras, EnervateAura);
            RegisterSpellProcessor(RelevantNanos.DamageAuras, VengeanceAura);
            RegisterSpellProcessor(RelevantNanos.SancAuras, SanctifierAura);
            RegisterSpellProcessor(RelevantNanos.ReaperAuras, ReaperAura);

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.PunisherOfTheWicked, Buff);

            PluginDirectory = pluginDir;
        }
        public Window[] _windows => new Window[] { _buffWindow, _procWindow };

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

        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\KeeperProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "KeeperProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "KeeperProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }


        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //Cannot stop Multi-Tabs. Easy fix would be correct naming of views to reference against WindowOptions - options.Name
                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\KeeperBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "KeeperBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "KeeperBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }

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

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
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

                CancelAuras();
            }
        }

        #region Perks


        private bool EschewTheFaithless(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.EschewTheFaithless != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool IgnoreTheUnrepentant(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.IgnoreTheUnrepentant != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool PureStrike(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.PureStrike != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool RighteousSmite(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.RighteousSmite != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool RighteousStrike(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.RighteousStrike != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool SymbioticBypass(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.SymbioticBypass != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool VirtuousReaper(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.VirtuousReaper != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool AmbientPurification(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.AmbientPurification != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool BenevolentBarrier(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.BenevolentBarrier != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool FaithfulReconstruction(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.FaithfulReconstruction != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool HonorRestored(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.HonorRestored != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool Subjugation(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Subjugation != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Auras

        private bool RecastAntiFear(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("RecastAntiFear")) { return true; }

            return Buff(spell, fightingTarget, ref actionTarget);
        }

        private bool ReaperAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet4Selection.Reaper != (AuraSet4Selection)_settings["AuraSet4Selection"].AsInt32()) { return false; }

            return Buff(spell, fightingTarget, ref actionTarget);
        }

        private bool SanctifierAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet4Selection.Sanc != (AuraSet4Selection)_settings["AuraSet4Selection"].AsInt32()) { return false; }

            return Buff(spell, fightingTarget, ref actionTarget);
        }

        private bool VengeanceAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet2Selection.Damage != (AuraSet2Selection)_settings["AuraSet2Selection"].AsInt32()) { return false; }

            return Buff(spell, fightingTarget, ref actionTarget);
        }

        private bool EnervateAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet2Selection.DeRoot != (AuraSet2Selection)_settings["AuraSet2Selection"].AsInt32()) { return false; }

            return Buff(spell, fightingTarget, ref actionTarget);
        }

        private bool ImminenceAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet3Selection.AAO != (AuraSet3Selection)_settings["AuraSet3Selection"].AsInt32()) { return false; }

            return Buff(spell, fightingTarget, ref actionTarget);
        }

        private bool BarrierAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet3Selection.Reflect != (AuraSet3Selection)_settings["AuraSet3Selection"].AsInt32()) { return false; }

            return Buff(spell, fightingTarget, ref actionTarget);
        }

        private bool HpAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet1Selection.Heal != (AuraSet1Selection)_settings["AuraSet1Selection"].AsInt32()) { return false; }

            return Buff(spell, fightingTarget, ref actionTarget);
        }

        private bool NpAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet1Selection.Nano != (AuraSet1Selection)_settings["AuraSet1Selection"].AsInt32()) { return false; }

            return Buff(spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Misc

        private void CancelAuras()
        {
            if (AuraSet1Selection.Heal != (AuraSet1Selection)_settings["AuraSet1Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.HealAuras);
            }
            if (AuraSet1Selection.Nano != (AuraSet1Selection)_settings["AuraSet1Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.NanoAuras);
            }
            if (AuraSet2Selection.Damage != (AuraSet2Selection)_settings["AuraSet2Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.DamageAuras);
            }
            if (AuraSet2Selection.DeRoot != (AuraSet2Selection)_settings["AuraSet2Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.DerootAuras);
            }
            if (AuraSet3Selection.AAO != (AuraSet3Selection)_settings["AuraSet3Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.AAOAuras);
            }
            if (AuraSet3Selection.Reflect != (AuraSet3Selection)_settings["AuraSet3Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ReflectAuras);
            }
            if (AuraSet4Selection.Sanc != (AuraSet4Selection)_settings["AuraSet4Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.SancAuras);
            }
            if (AuraSet4Selection.Reaper != (AuraSet4Selection)_settings["AuraSet4Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ReaperAuras);
            }
        }

        protected override bool ShouldUseSpecialAttack(SpecialAttack specialAttack)
        {
            return specialAttack != SpecialAttack.Dimach;
        }

        private static class RelevantNanos
        {
            public const int CourageOfTheJust = 279380;
            public const int CompositeAttribute = 223372;
            public const int CompositeNano = 223380;
            public const int CompositeUtility = 287046;
            public const int CompositePhysical = 215264;
            public const int CompositeMartialProwess = 302158;
            public const int CompositeMelee = 223360;
            public const int PunisherOfTheWicked = 301602;

            public static int[] HealAuras = new[] { 273362, 223024, 210536, 210528 };
            public static int[] NanoAuras = new[] { 224073, 210597, 210589 };

            public static readonly int[] ReflectAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Absorb_Reflect_AMSBuff)
                .Where(s => s.Name.Contains("Barrier of")).OrderByStackingOrder().Select(s => s.Id).ToArray();
            public static readonly int[] DamageAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Damage_SnareReductionBuff)
                .Where(s => s.Name.Contains("Vengeance")).OrderByStackingOrder().Select(s => s.Id).ToArray();
            public static readonly int[] AAOAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Absorb_Reflect_AMSBuff)
                .Where(s => s.Name.Contains("Imminence of")).OrderByStackingOrder().Select(s => s.Id).ToArray();
            public static readonly int[] DerootAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Damage_SnareReductionBuff)
                .Where(s => s.Name.Contains("Enervate")).OrderByStackingOrder().Select(s => s.Id).ToArray();
            public static readonly int[] ReaperAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperProcBuff)
                .Where(s => s.Name.Contains("Reaper")).OrderByStackingOrder().Select(s => s.Id).ToArray();
            public static readonly int[] SancAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperProcBuff)
                .Where(s => s.Name.Contains("Sanctifier")).OrderByStackingOrder().Select(s => s.Id).ToArray();
        }
        public enum AuraSet1Selection
        {
            Heal, Nano
        }
        public enum AuraSet2Selection
        {
            Damage, DeRoot
        }
        public enum AuraSet3Selection
        {
            AAO, Reflect
        }
        public enum AuraSet4Selection
        {
            Sanc, Reaper
        }

        public enum ProcType1Selection
        {
            RighteousSmite, SymbioticBypass, VirtuousReaper, IgnoreTheUnrepentant, PureStrike, EschewTheFaithless, RighteousStrike
        }

        public enum ProcType2Selection
        {
            HonorRestored, AmbientPurification, BenevolentBarrier, Subjugation, FaithfulReconstruction
        }

        #endregion
    }
}
