using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using CombatHandler.Generic;
using Character.State;

namespace Desu
{
    public class AdvCombatHandler : GenericCombatHandler
    {
        public AdvCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("HealTeammates", true);
            RegisterSettingsWindow("Adventurer Handler", "AdvSettingsView.xml");

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SingleTargetHealing).OrderByStackingOrder(), SingleTargetHeal, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TeamHealing).OrderByStackingOrder(), TeamHeal, CombatActionPriority.High);

            //Buffs

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.General1HEdgedBuff).OrderByStackingOrder(), MeleeTeamBuffSelf);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), RangedTeamBuffSelf);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShieldUpgrades).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MultiwieldBuff).OrderByStackingOrder(), GenericBuff);

            //Items
            //RegisterItemProcessor(RelevantItems.TheWizdomOfHuzzum, RelevantItems.TheWizdomOfHuzzum, MartialArtsTeamHealAttack);

        }

        //private bool ControlledDestructionNoShutdown(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ControlledDestructionBuff))
        //        return false;

        //    if (fightingTarget == null)
        //        return false;

        //    if (DynelManager.LocalPlayer.NanoPercent < 30)
        //        return false;

        //    return true;
        //}

        //private bool ControlledDestructionWithShutdown(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (!IsSettingEnabled("UseShortDamageBuffNanoShutdown"))
        //        return false;

        //    if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ControlledDestructionBuff))
        //        return false;

        //    if (DynelManager.LocalPlayer.HealthPercent < 100)
        //        return false;

        //    if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 1)
        //        return false;

        //    if (fightingTarget == null)
        //        return false;

        //    if (DynelManager.LocalPlayer.NanoPercent < 30)
        //        return false;

        //    return true;
        //}

        //private bool MartialArtsTeamHealAttack(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (fightingtarget == null)
        //        return false;

        //    if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item)))
        //        return false;

        //    int healingReceived = item.LowId == RelevantItems.TreeOfEnlightenment ? 290 : 1200;

        //    if (DynelManager.LocalPlayer.MissingHealth > healingReceived * 2)
        //        return true;

        //    if (IsSettingEnabled("HealTeammates"))
        //    {
        //        SimpleChar dyingTeammate = GetTeammatesInNeedOfHealing(healingReceived * 2)
        //            .FirstOrDefault();

        //        if (dyingTeammate != null)
        //            return true;
        //    }

        //    return false;
        //}

        //protected override bool ShouldUseSpecialAttack(SpecialAttack specialAttack)
        //{
        //    return specialAttack != SpecialAttack.Dimach;
        //}

        //private bool FistsOfTheWinterFlameNano(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        //{
        //    actiontarget.ShouldSetTarget = false;
        //    return fightingtarget != null && fightingtarget.HealthPercent > 50;
        //}

        private bool SingleTargetHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = true;

            if (DynelManager.LocalPlayer.MissingHealth > 800) //TODO: Some kind of healing check to calc an optimal missing health value
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            if (IsSettingEnabled("HealTeammates"))
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

            if (IsSettingEnabled("HealTeammates"))
            {
                SimpleChar dyingTeammate = GetTeammatesInNeedOfHealing(1500)
                    .FirstOrDefault(target => target.Position.DistanceFrom(DynelManager.LocalPlayer.Position) < spell.AttackRange); //TODO: factor in nano range increase

                if (dyingTeammate != null)
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

        //private Stat GetSkillLockStat(Item item)
        //{
        //    switch (item.HighId)
        //    {
        //        case RelevantItems.TheWizdomOfHuzzum:
        //        case RelevantItems.TreeOfEnlightenment:
        //            return Stat.Dimach;
        //        default:
        //            throw new Exception($"No skill lock stat defined for item id {item.HighId}");
        //    }
        //}

        private static class RelevantNanos
        {
            //public const int FistsOfTheWinterFlame = 269470;
            //public const int LimboMastery = 28894;
        }

        private static class RelevantItems
        {
            //public const int TheWizdomOfHuzzum = 303056;
            //public const int TreeOfEnlightenment = 204607;
        }
    }
}
