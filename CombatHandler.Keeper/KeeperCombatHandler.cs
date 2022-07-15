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

        private static View _buffView;

        private static double _ncuUpdateTime;

        public KeeperCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("SpamAntifear", false);

            //Auras
            _settings.AddVariable("AuraSet1Selection", (int)AuraSet1Selection.Heal);
            _settings.AddVariable("AuraSet2Selection", (int)AuraSet2Selection.Damage);
            _settings.AddVariable("AuraSet3Selection", (int)AuraSet3Selection.AAO);
            _settings.AddVariable("AuraSet4Selection", (int)AuraSet4Selection.Sanc);

            RegisterSettingsWindow("Keeper Handler", "KeeperSettingsView.xml");

            RegisterPerkProcessors();

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcKeeperHonorRestored, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperRighteousSmite, LEProc, CombatActionPriority.Low);

            //12man Antifear spam
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.KeeperFearImmunity).OrderByStackingOrder(), AntifearSpam);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Fortify).OrderByStackingOrder().OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine._2HEdgedBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Fury).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.KeeperDeflect_RiposteBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.KeeperEvade_Dodge_DuckBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.KeeperStr_Stam_AgiBuff).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(RelevantNanos.HealAuras, HpAura);
            RegisterSpellProcessor(RelevantNanos.NanoAuras, NpAura);
            RegisterSpellProcessor(RelevantNanos.ReflectAuras, BarrierAura);
            RegisterSpellProcessor(RelevantNanos.AAOAuras, ImminenceAura);
            RegisterSpellProcessor(RelevantNanos.DerootAuras, EnervateAura);
            RegisterSpellProcessor(RelevantNanos.DamageAuras, VengeanceAura);
            RegisterSpellProcessor(RelevantNanos.SancAuras, SanctifierAura);
            RegisterSpellProcessor(RelevantNanos.ReaperAuras, ReaperAura);

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.PunisherOfTheWicked, GenericBuff);

            PluginDirectory = pluginDir;
        }
        public Window[] _windows => new Window[] { _buffWindow };

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

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
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

            //CancelBuffs(IsSettingEnabled("NanoAura") ? RelevantNanos.HealAuras : RelevantNanos.NanoAuras);
            //CancelBuffs(IsSettingEnabled("ReflectAura") ? RelevantNanos.AAOAuras : RelevantNanos.ReflectAuras);
            //CancelBuffs(IsSettingEnabled("DerootAura") ? RelevantNanos.DamageAuras : RelevantNanos.DerootAuras);
        }

        private bool AntifearSpam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("SpamAntifear")) { return true; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool ReaperAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet4Selection.Reaper != (AuraSet4Selection)_settings["AuraSet4Selection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool SanctifierAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet4Selection.Sanc != (AuraSet4Selection)_settings["AuraSet4Selection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool VengeanceAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet2Selection.Damage != (AuraSet2Selection)_settings["AuraSet2Selection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool EnervateAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet2Selection.DeRoot != (AuraSet2Selection)_settings["AuraSet2Selection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool ImminenceAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet3Selection.AAO != (AuraSet3Selection)_settings["AuraSet3Selection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool BarrierAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet3Selection.Reflect != (AuraSet3Selection)_settings["AuraSet3Selection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool HpAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet1Selection.Heal != (AuraSet1Selection)_settings["AuraSet1Selection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool NpAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet1Selection.Nano != (AuraSet1Selection)_settings["AuraSet1Selection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
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
            public static readonly int[] DamageAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Absorb_Reflect_AMSBuff)
                .Where(s => s.Name.Contains("Imminence of")).OrderByStackingOrder().Select(s => s.Id).ToArray();
            public static readonly int[] AAOAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Damage_SnareReductionBuff)
                .Where(s => s.Name.Contains("Vengeance")).OrderByStackingOrder().Select(s => s.Id).ToArray();
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
    }
}
