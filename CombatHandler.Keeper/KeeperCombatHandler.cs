using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Desu
{
    public class KeeperCombatHandler : GenericCombatHandler
    {
        private readonly Menu _menu;
        private readonly int[] _barrierAuras;
        private readonly int[] _imminenceAuras;
        private readonly int[] _vengeanceAuras;
        private readonly int[] _enervateAuras;
        private readonly int[] _hpAuras;
        private readonly int[] _npAuras;
        private readonly int[] _allHpAuras;
        private readonly int[] _allNpAuras;

        private bool UseNanoAura => _menu != null && _menu.GetBool("UseNanoAura");

        private bool UseReflectAura => _menu != null && _menu.GetBool("UseReflectAura");
        private bool UseDerootAura => _menu != null && _menu.GetBool("UseDerootAura");

        public KeeperCombatHandler()
        {
            _barrierAuras = Spell.GetSpellsForNanoline(Nanoline.KeeperAura_Absorb_Reflect_AMSBuff).Where(s => s.Name.Contains("Barrier of")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            _imminenceAuras = Spell.GetSpellsForNanoline(Nanoline.KeeperAura_Absorb_Reflect_AMSBuff).Where(s => s.Name.Contains("Imminence of")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            _vengeanceAuras = Spell.GetSpellsForNanoline(Nanoline.KeeperAura_Damage_SnareReductionBuff).Where(s => s.Name.Contains("Vengeance")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            _enervateAuras = Spell.GetSpellsForNanoline(Nanoline.KeeperAura_Damage_SnareReductionBuff).Where(s => s.Name.Contains("Enervate")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            _allHpAuras = Spell.GetSpellsForNanoline(Nanoline.KeeperAura_HPandNPHeal).Where(s => s.Name.Contains("Ambient")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            _allNpAuras = Spell.GetSpellsForNanoline(Nanoline.KeeperAura_HPandNPHeal).Where(s => s.Name.Contains("Tone of")).OrderByStackingOrder().Select(s => s.Identity.Instance).ToArray();
            //Since the new nanos are significantly better than the old ones, we will only consider them
            _hpAuras = new [] { 210528, 210536, 223024, 273362 };
            _npAuras = new[] { 210589, 210597, 224073 };

            _menu = new Menu("CombatHandler.Keeper", "CombatHandler.Keeper");
            _menu.AddItem(new MenuBool("UseNanoAura", "Use nano aura instead of healing", false));
            _menu.AddItem(new MenuBool("UseReflectAura", "Use reflect aura instead of AAO/AAD", false));
            _menu.AddItem(new MenuBool("UseDerootAura", "Use deroot aura instead of damage", false));
            OptionPanel.AddMenu(_menu);

            //DmgPerks
            RegisterPerkProcessor(PerkHash.Insight, DmgBuffPerk);
            RegisterPerkProcessor(PerkHash.BladeWhirlwind, DmgBuffPerk);
            RegisterPerkProcessor(PerkHash.ReinforceSlugs, DmgBuffPerk);

            //Debuffs
            RegisterPerkProcessor(PerkHash.MarkOfSufferance, TeamHealPerk);
            RegisterPerkProcessor(PerkHash.MarkOfTheUnclean, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.MarkOfVengeance, TargetedDamagePerk);

            //Shadow Dmg
            RegisterPerkProcessor(PerkHash.DeepCuts, DamagePerk);
            RegisterPerkProcessor(PerkHash.SeppukuSlash, DamagePerk);
            RegisterPerkProcessor(PerkHash.HonoringTheAncients, DamagePerk);

            //Heal Perks
            RegisterPerkProcessor(PerkHash.BioRejuvenation, TeamHealPerk);
            RegisterPerkProcessor(PerkHash.BioRegrowth, TeamHealPerk);
            RegisterPerkProcessor(PerkHash.LayOnHands, TeamHealPerk);
            RegisterPerkProcessor(PerkHash.LayOnHands, TeamHealPerk);
            RegisterPerkProcessor(PerkHash.BioShield, SelfHealPerk);
            RegisterPerkProcessor(PerkHash.BioCocoon, SelfHealPerk);//TODO: Write independent logic for this

            //Spells
            RegisterSpellProcessor(RelevantNanos.CourageOfTheJust, GenericBuff);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.CompositeAttribute, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeNano, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeUtility, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositePhysical, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeMartialProwess, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeMelee, GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.Fortify).OrderByStackingOrder(), GenericBuff);


            //I'm defining health and nano auras statically since they added new versions with lower stacking order.
            RegisterSpellProcessor(_hpAuras, HpAura);
            RegisterSpellProcessor(_npAuras, NpAura);
            RegisterSpellProcessor(_barrierAuras, BarrierAura);
            RegisterSpellProcessor(_imminenceAuras, ImminenceAura);
            RegisterSpellProcessor(_enervateAuras, EnervateAura);
            RegisterSpellProcessor(_vengeanceAuras, VengeanceAura);
        }

        private bool VengeanceAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (UseDerootAura)
                return false;

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool EnervateAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!UseDerootAura)
                return false;

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool ImminenceAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (UseReflectAura)
                return false;

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool BarrierAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!UseReflectAura)
                return false;

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool HpAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (UseNanoAura)
                return false;

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool NpAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!UseNanoAura)
                return false;

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool SelfHealPerk(Perk perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.HealthPercent <= 30)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }
            return false;
        }

        private bool DmgBuffPerk(Perk perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking || fightingTarget == null)
                return false;
            return true;
        }


        private bool TeamHealPerk(Perk perk, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            // Prioritize keeping ourself alive
            if (DynelManager.LocalPlayer.HealthPercent <= 30)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent < 30)
                    .OrderByDescending(c => c.GetStat(Stat.NumFightingOpponents))
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    actionTarget.Target = dyingTeamMember;
                    return true;
                }
            }

            return false;
        }

        private static void CancelBuffs(int[] buffsToCancel)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs)
            {
                if (buffsToCancel.Contains(buff.Identity.Instance))
                    buff.Remove();
            }
        }

        protected override bool ShouldUseSpecialAttack(SpecialAttack specialAttack)
        {
            return specialAttack != SpecialAttack.Dimach;
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            CancelBuffs(UseNanoAura ? _allHpAuras : _allNpAuras);
            CancelBuffs(UseReflectAura ? _imminenceAuras : _barrierAuras);
            CancelBuffs(UseDerootAura ? _vengeanceAuras : _enervateAuras);
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
        }
    }
}
