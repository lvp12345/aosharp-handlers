using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Combat;
using AOSharp.Core.Inventory;
using CombatHandler.Generic;

namespace Desu
{
    public class MACombatHandler : GenericCombatHandler
    {
        public MACombatHandler() : base()
        {
            //Perks
            RegisterPerkProcessor(PerkHash.Moonmist, Moonmist);
            RegisterPerkProcessor(PerkHash.Dragonfire, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.ChiConductor, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Incapacitate, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.TremorHand, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.FleshQuiver, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Obliterate, Obliterate);
            RegisterPerkProcessor(PerkHash.Bore, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Crave, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.NanoFeast, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.BotConfinement, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.RedDawn, RedDawnPerk, CombatActionPriority.High);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.SingleTargetHealing).OrderByStackingOrder(), SingleTargetHeal, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.TeamHealing).OrderByStackingOrder(), TeamHeal, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.FistsOfTheWinterFlame, ConditionProcessor);

            //Items
            RegisterItemProcessor(RelevantItems.TheWizdomOfHuzzum, RelevantItems.TheWizdomOfHuzzum, MartialArtsTeamHealAttack);

        }

        private bool MartialArtsTeamHealAttack(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (fightingtarget == null)
                return false;

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item)))
                return false;

            int healingReceived = item.LowId == RelevantItems.TreeOfEnlightenment ? 290 : 1200;

            if (DynelManager.LocalPlayer.MissingHealth < healingReceived * 2)
                return false;

            return true;
        }

        protected override bool ShouldUseSpecialAttack(SpecialAttack specialAttack)
        {
            return specialAttack != SpecialAttack.Dimach;
        }

        private bool ConditionProcessor(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            actiontarget.ShouldSetTarget = false;
            return fightingtarget != null && fightingtarget.HealthPercent > 50;
        }

        private bool RedDawnPerk(Perk perk, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return DynelManager.LocalPlayer.MissingHealth > 2000;
        }

        private bool SingleTargetHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.MissingHealth > 800) //TODO: Some kind of healing check to calc an optimal missing health value
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        private bool TeamHeal(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            actiontarget.ShouldSetTarget = false;
            return DynelManager.LocalPlayer.MissingHealth > 1500;
        }

        private bool Moonmist(Perk perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || fightingTarget.HealthPercent < 90)
                return false;

            return true;
        }

        private bool GenericDamagePerk(Perk perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || fightingTarget.HealthPercent < 5)
                return false;

            return true;
        }

        private bool Obliterate(Perk perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || fightingTarget.HealthPercent > 15)
                return false;

            return true;
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
        }

        private static class RelevantItems
        {
            public const int TheWizdomOfHuzzum = 303056;
            public const int TreeOfEnlightenment = 204607;
        }
    }
}
