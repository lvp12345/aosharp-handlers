using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI.Options;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.Unmanaged.Imports;
using CombatHandler;
using AOSharp.Common.GameData.UI;

namespace Desu
{
    public class KeeperCombatHandler : GenericCombatHandler
    {
        public static string PluginDirectory;

        public static Window buffWindow;
        public KeeperCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("NanoAura", false);
            settings.AddVariable("HealAura", false);
            settings.AddVariable("ReflectAura", false);
            settings.AddVariable("AAOAura", false);
            settings.AddVariable("DamageAura", false);
            settings.AddVariable("DerootAura", false);
            settings.AddVariable("ReaperAura", false);
            settings.AddVariable("SancAura", false);
            settings.AddVariable("SpamAntifear", false);

            RegisterSettingsWindow("Keeper Handler", "KeeperSettingsView.xml");

            RegisterSettingsWindow("Buffs", "KeeperBuffsView.xml");


            RegisterPerkProcessors();

            //Chat.WriteLine("" + DynelManager.LocalPlayer.GetStat(Stat.EquippedWeapons));

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

        private void BuffView(object s, ButtonBase button)
        {
            buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\KeeperBuffsView.xml",
                windowSize: new Rect(0, 0, 240, 345),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            buffWindow.Show(true);
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (IsSettingEnabled("HealAura") && IsSettingEnabled("NanoAura"))
            {
                settings["HealAura"] = false;
                settings["NanoAura"] = false;

                Chat.WriteLine($"Can only have one Aura active.");
            }

            if (IsSettingEnabled("ReflectAura") && IsSettingEnabled("AAOAura"))
            {
                settings["ReflectAura"] = false;
                settings["AAOAura"] = false;

                Chat.WriteLine($"Can only have one Aura active.");
            }

            if (IsSettingEnabled("DamageAura") && IsSettingEnabled("DerootAura"))
            {
                settings["DamageAura"] = false;
                settings["DerootAura"] = false;

                Chat.WriteLine($"Can only have one Aura active.");
            }

            if (IsSettingEnabled("SancAura") && IsSettingEnabled("ReaperAura"))
            {
                settings["SancAura"] = false;
                settings["ReaperAura"] = false;

                Chat.WriteLine($"Can only have one Aura active.");
            }


            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                {
                    buffView.Tag = SettingsController.settingsWindow;
                    buffView.Clicked = BuffView;
                }
            }

            base.OnUpdate(deltaTime);

            CancelBuffs(IsSettingEnabled("NanoAura") ? RelevantNanos.HealAuras : RelevantNanos.NanoAuras);
            CancelBuffs(IsSettingEnabled("ReflectAura") ? RelevantNanos.AAOAuras : RelevantNanos.ReflectAuras);
            CancelBuffs(IsSettingEnabled("DerootAura") ? RelevantNanos.DamageAuras : RelevantNanos.DerootAuras);
        }

        private bool AntifearSpam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("SpamAntifear")) { return true; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool ReaperAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("SancAura")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool SanctifierAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("ReaperAura")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool VengeanceAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("DerootAura")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool EnervateAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("DamageAura")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool ImminenceAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("ReflectAura")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool BarrierAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("AAOAura")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool HpAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("NanoAura")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool NpAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("HealAura"))
            {
                return false;
            }

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
                .Where(s => s.Name.Contains("Barrier of")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            public static readonly int[] DamageAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Absorb_Reflect_AMSBuff)
                .Where(s => s.Name.Contains("Imminence of")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            public static readonly int[] AAOAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Damage_SnareReductionBuff)
                .Where(s => s.Name.Contains("Vengeance")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            public static readonly int[] DerootAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Damage_SnareReductionBuff)
                .Where(s => s.Name.Contains("Enervate")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            public static readonly int[] ReaperAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperProcBuff)
                .Where(s => s.Name.Contains("Reaper")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            public static readonly int[] SancAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperProcBuff)
                .Where(s => s.Name.Contains("Sanctifier")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
        }
    }
}
