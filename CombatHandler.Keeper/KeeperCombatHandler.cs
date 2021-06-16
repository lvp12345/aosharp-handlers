using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI.Options;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.Unmanaged.Imports;

namespace Desu
{
    public class KeeperCombatHandler : GenericCombatHandler
    {
        private readonly int[] _barrierAuras;
        private readonly int[] _imminenceAuras;
        private readonly int[] _vengeanceAuras;
        private readonly int[] _enervateAuras;
        private readonly int[] _hpAuras;
        private readonly int[] _npAuras;
        private readonly int[] _reaperAuras;
        private readonly int[] _sanctifierAuras;

        public KeeperCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("UseNanoAura", false);
            settings.AddVariable("UseReflectAura", false);
            settings.AddVariable("UseDerootAura", false);
            settings.AddVariable("UseReaperAura", false);
            settings.AddVariable("SpamAntifear", false);
            RegisterSettingsWindow("Keeper Handler", "KeeperSettingsView.xml");
            RegisterPerkProcessors();

            _barrierAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Absorb_Reflect_AMSBuff).Where(s => s.Name.Contains("Barrier of")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            _imminenceAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Absorb_Reflect_AMSBuff).Where(s => s.Name.Contains("Imminence of")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            _vengeanceAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Damage_SnareReductionBuff).Where(s => s.Name.Contains("Vengeance")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            _enervateAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Damage_SnareReductionBuff).Where(s => s.Name.Contains("Enervate")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            _reaperAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperProcBuff).Where(s => s.Name.Contains("Reaper")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            _sanctifierAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperProcBuff).Where(s => s.Name.Contains("Sanctifier")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            //Since the new nanos are significantly better than the old ones, we will only consider them
            _hpAuras = new[] { 273362, 223024, 210536, 210528 };
            _npAuras = new[] { 224073, 210597, 210589 };

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

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.PunisherOfTheWicked, TeamBuff);

            //I'm defining health and nano auras statically since they added new versions with lower stacking order.
            RegisterSpellProcessor(_hpAuras, HpAura);
            RegisterSpellProcessor(_npAuras, NpAura);
            RegisterSpellProcessor(_barrierAuras, BarrierAura);
            RegisterSpellProcessor(_imminenceAuras, ImminenceAura);
            RegisterSpellProcessor(_enervateAuras, EnervateAura);
            RegisterSpellProcessor(_vengeanceAuras, VengeanceAura);
            RegisterSpellProcessor(_sanctifierAuras, SanctifierAura);
            RegisterSpellProcessor(_reaperAuras, ReaperAura);
        }

        private Boolean AntifearSpam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(IsSettingEnabled("SpamAntifear"))
            {
                return true;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool ReaperAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseReaperAura"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool SanctifierAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("UseReaperAura"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool VengeanceAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("UseDerootAura"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool EnervateAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseDerootAura"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool ImminenceAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("UseReflectAura"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool BarrierAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseReflectAura"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool HpAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("UseNanoAura"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool NpAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseNanoAura"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        protected override bool ShouldUseSpecialAttack(SpecialAttack specialAttack)
        {
            return specialAttack != SpecialAttack.Dimach;
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            CancelBuffs(IsSettingEnabled("UseNanoAura") ? _hpAuras : _npAuras);
            CancelBuffs(IsSettingEnabled("UseReflectAura") ? _imminenceAuras : _barrierAuras);
            CancelBuffs(IsSettingEnabled("UseDerootAura") ? _vengeanceAuras : _enervateAuras);
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
        }
    }
}
