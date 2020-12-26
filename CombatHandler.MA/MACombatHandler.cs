using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Combat;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;

namespace Desu
{
    public class MACombatHandler : GenericCombatHandler
    {
        private Menu _menu;

        private bool HealTeammates => _menu != null && _menu.GetBool("HealTeammates");
        private bool UseShortDamageBuffWithNanoShutdown => _menu != null && _menu.GetBool("AllowShortDamageBuffWithNanoShutdown");

        public MACombatHandler() : base()
        {
            _menu = new Menu("CombatHandler.MA", "CombatHandler.MA");
            _menu.AddItem(new MenuBool("HealTeammates", "Heal teammates", true));
            _menu.AddItem(new MenuBool("AllowShortDamageBuffWithNanoShutdown", "Allow use of controlled destruction buffs that shutdown nanoskills", false));
            OptionPanel.AddMenu(_menu);

            //Perks
            RegisterPerkProcessor(PerkHash.Moonmist, Moonmist);
            RegisterPerkProcessor(PerkHash.Dragonfire, DamagePerk);
            RegisterPerkProcessor(PerkHash.ChiConductor, DamagePerk);
            RegisterPerkProcessor(PerkHash.Incapacitate, DamagePerk);
            RegisterPerkProcessor(PerkHash.TremorHand, DamagePerk);
            RegisterPerkProcessor(PerkHash.FleshQuiver, DamagePerk);
            RegisterPerkProcessor(PerkHash.Obliterate, DamagePerk);
            RegisterPerkProcessor(PerkHash.RedDawn, RedDawnPerk, CombatActionPriority.High);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SingleTargetHealing).OrderByStackingOrder(), SingleTargetHeal, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TeamHealing).OrderByStackingOrder(), TeamHeal, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledDestructionBuff).Where(s => s.StackingOrder >= 19).OrderByStackingOrder(), ControlledDestructionNoShutdown);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledDestructionBuff).Where(s => s.StackingOrder < 19).OrderByStackingOrder(), ControlledDestructionWithShutdown);
            RegisterSpellProcessor(RelevantNanos.FistsOfTheWinterFlame, FistsOfTheWinterFlameNano);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.CompositeAttribute, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeNano, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeUtility, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositePhysical, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeMartialProwess, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.LimboMastery, GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalIncreaseBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.BrawlBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledRageBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RunspeedBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.StrengthBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtsBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).Where(s => s.Identity.Instance != 28879).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RiposteBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistBuff).OrderByStackingOrder(), GenericBuff);

            //Items
            RegisterItemProcessor(RelevantItems.TheWizdomOfHuzzum, RelevantItems.TheWizdomOfHuzzum, MartialArtsTeamHealAttack);

        }

        private bool ControlledDestructionNoShutdown(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ControlledDestructionBuff))
                return false;

            if (fightingTarget == null)
                return false;

            if (DynelManager.LocalPlayer.NanoPercent < 30)
                return false;

            return true;
        }

        private bool ControlledDestructionWithShutdown(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!UseShortDamageBuffWithNanoShutdown)
                return false;

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ControlledDestructionBuff))
                return false;

            if (DynelManager.LocalPlayer.HealthPercent < 100)
                return false;

            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 1)
                return false;

            if (fightingTarget == null)
                return false;

            if (DynelManager.LocalPlayer.NanoPercent < 30)
                return false;

            return true;
        }

        private bool MartialArtsTeamHealAttack(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingtarget == null)
                return false;

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item)))
                return false;

            int healingReceived = item.LowId == RelevantItems.TreeOfEnlightenment ? 290 : 1200;

            if (DynelManager.LocalPlayer.MissingHealth > healingReceived * 2)
                return true;

            if (HealTeammates)
            {
                SimpleChar dyingTeammate = GetTeammatesInNeedOfHealing(healingReceived * 2)
                    .FirstOrDefault();

                if (dyingTeammate != null)
                    return true;
            }

            return false;
        }

        protected override bool ShouldUseSpecialAttack(SpecialAttack specialAttack)
        {
            return specialAttack != SpecialAttack.Dimach;
        }

        private bool FistsOfTheWinterFlameNano(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            actiontarget.ShouldSetTarget = false;
            return fightingtarget != null && fightingtarget.HealthPercent > 50;
        }

        private bool RedDawnPerk(PerkAction perkAction, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return DynelManager.LocalPlayer.MissingHealth > 2000;
        }

        private bool SingleTargetHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = true;

            if (DynelManager.LocalPlayer.MissingHealth > 800) //TODO: Some kind of healing check to calc an optimal missing health value
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            if (HealTeammates)
            {
                SimpleChar dyingTeammate = GetTeammatesInNeedOfHealing(800)
                    .Where(target => target.IsInLineOfSight)
                    .FirstOrDefault(target => target.Position.DistanceFrom(DynelManager.LocalPlayer.Position) < 20); //TODO: make this dynamic when possible

                if (dyingTeammate != null)
                {
                    actionTarget.Target = dyingTeammate;
                    return true;
                }
            }

            return false;
        }

        private bool TeamHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            actiontarget.ShouldSetTarget = false;

            if (DynelManager.LocalPlayer.MissingHealth > 1500) //TODO: Some kind of healing check to calc an optimal missing health value
                return true;

            if (HealTeammates)
            {
                SimpleChar dyingTeammate = GetTeammatesInNeedOfHealing(1500)
                    .FirstOrDefault(target => target.Position.DistanceFrom(DynelManager.LocalPlayer.Position) < spell.AttackRange); //TODO: factor in nano range increase

                if (dyingTeammate != null)
                    return true;
            }

            return false;
        }

        private bool Moonmist(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = false;

            if (fightingTarget == null || (fightingTarget.HealthPercent < 90 && DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) < 2))
                return false;

            return true;
        }

        private bool Obliterate(Perk perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || fightingTarget.HealthPercent > 15)
                return false;

            return true;
        }

        private bool TeamNeedsHealing(int missingHealthThreshold, float castRange, out SimpleChar target)
        {
            target = null;

            if (!DynelManager.LocalPlayer.IsInTeam())
                return false;

            SimpleChar dyingTeamMember = DynelManager.Characters
                .Where(c => c.IsAlive)
                .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                .Where(c => c.MissingHealth >= missingHealthThreshold)
                .OrderByDescending(c => c.GetStat(Stat.NumFightingOpponents))
                .FirstOrDefault();

            if (dyingTeamMember != null)
            {
                target = dyingTeamMember;
                return true;
            }

            return false;
        }

        private IEnumerable<SimpleChar> GetTeammatesInNeedOfHealing(int missingHealthThreshold)
        {
            return DynelManager.Characters
                .Where(c => c.IsAlive)
                .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                .Where(c => c.MissingHealth >= missingHealthThreshold)
                .OrderByDescending(c => c.GetStat(Stat.NumFightingOpponents));
        }

        private Stat GetSkillLockStat(Item item)
        {
            switch (item.HighId)
            {
                case RelevantItems.TheWizdomOfHuzzum:
                case RelevantItems.TreeOfEnlightenment:
                    return Stat.Dimach;
                default:
                    throw new Exception($"No skill lock stat defined for item id {item.HighId}");
            }
        }

        private static class RelevantNanos
        {
            public const int FistsOfTheWinterFlame = 269470;
            public const int CompositeAttribute = 223372;
            public const int CompositeNano = 223380;
            public const int CompositeUtility = 287046;
            public const int CompositePhysical = 215264;
            public const int CompositeMartialProwess = 302158;
            public const int LimboMastery = 28894;
        }

        private static class RelevantItems
        {
            public const int TheWizdomOfHuzzum = 303056;
            public const int TreeOfEnlightenment = 204607;
        }
    }
}
