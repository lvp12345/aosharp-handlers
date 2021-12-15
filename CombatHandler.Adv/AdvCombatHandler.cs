using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using CombatHandler.Generic;

namespace Desu
{
    public class AdvCombatHandler : GenericCombatHandler
    {
        public AdvCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("Heal", true);
            settings.AddVariable("OSHeal", false);

            //settings.AddVariable("Heal", true); // Morph leet and crit?
            //settings.AddVariable("Heal", true); // Morph sabre and damage?
            //settings.AddVariable("OSHeal", false); // Morph healing?

            RegisterSettingsWindow("Adventurer Handler", "AdvSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcAdventurerMacheteFlurry, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerCombustion, LEProc);

            //Spells
            RegisterSpellProcessor(RelevantNanos.HEALS, Healing, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CompleteHealingLine).OrderByStackingOrder(), CompleteHealing, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TeamHealing).OrderByStackingOrder(), TeamHealing, CombatActionPriority.High);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.General1HEdgedBuff).OrderByStackingOrder(), MeleeBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), RangedBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShieldUpgrades).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MultiwieldBuff).OrderByStackingOrder(), GenericBuff);

            //Items
            //RegisterItemProcessor(RelevantItems.TheWizdomOfHuzzum, RelevantItems.TheWizdomOfHuzzum, MartialArtsTeamHealAttack);

        }

        #region Healing

        private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Heal") || !CanCast(spell)) { return false; }

            return FindMemberWithHealthBelow(85, ref actionTarget);
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal")) { return false; }

            if (!CanCast(spell)) { return false; }

            if (IsSettingEnabled("OSHeal") && !IsSettingEnabled("Heal"))
            {
                return FindPlayerWithHealthBelow(85, ref actionTarget);
            }

            return FindMemberWithHealthBelow(85, ref actionTarget);
        }

        private bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return FindMemberWithHealthBelow(30, ref actionTarget);
        }

        //private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (!IsSettingEnabled("Heal") || !CanCast(spell)) { return false; }

        //    // Try to keep our teammates alive if we're in a team
        //    if (DynelManager.LocalPlayer.IsInTeam())
        //    {
        //        List<SimpleChar> dyingTeamMember = DynelManager.Characters
        //            .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
        //            .Where(c => c.HealthPercent <= 80)
        //            .Where(c => c.HealthPercent >= 50)
        //            .ToList();

        //        if (dyingTeamMember.Count < 4)
        //        {
        //            return false;
        //        }
        //        else
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        //private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (!IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal")) { return false; }

        //    if (!CanCast(spell)) { return false; }

        //    // Try to keep our teammates alive if we're in a team
        //    if (DynelManager.LocalPlayer.IsInTeam() && IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal"))
        //    {
        //        List<SimpleChar> dyingTeamMember = DynelManager.Characters
        //            .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
        //            .Where(c => c.HealthPercent <= 80)
        //            .Where(c => c.HealthPercent >= 50)
        //            .ToList();

        //        if (dyingTeamMember.Count >= 4)
        //        {
        //            return false;
        //        }
        //        else
        //        {
        //            return FindMemberWithHealthBelow(85, ref actionTarget);
        //        }
        //    }
        //    if (IsSettingEnabled("OSHeal") && !IsSettingEnabled("Heal"))
        //    {
        //        return FindPlayerWithHealthBelow(85, ref actionTarget);
        //    }
        //    else
        //    {
        //        return FindMemberWithHealthBelow(85, ref actionTarget);
        //    }
        //}

        #endregion

        #region Misc
        private static class RelevantNanos
        {
            public static int[] HEALS = new[] { 223167, 252008, 252006, 136674, 136673, 143908, 82059, 136675, 136676, 82060, 136677,
                136678, 136679, 136682, 82061, 136681, 136680, 136683, 136684, 136685, 82062, 136686, 136689, 82063, 136688, 136687,
                82064, 26695 };
        }

        private static class RelevantItems
        {

        }

        #endregion
    }
}
