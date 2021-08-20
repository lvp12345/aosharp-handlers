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

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SingleTargetHealing).OrderByStackingOrder(), Healing, CombatActionPriority.High);
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
            if (!IsSettingEnabled("Heal"))
            {
                return false;
            }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 80)
                    .Where(c => c.HealthPercent >= 50)
                    .ToList();

                if (dyingTeamMember.Count < 4)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal"))
            {
                return false;
            }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam() && IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal"))
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 80)
                    .Where(c => c.HealthPercent >= 50)
                    .ToList();

                if (dyingTeamMember.Count >= 4)
                {
                    return false;
                }
                else
                {
                    return FindMemberWithHealthBelow(85, ref actionTarget);
                }
            }
            if (IsSettingEnabled("OSHeal") && !IsSettingEnabled("Heal"))
            {
                return FindPlayerWithHealthBelow(85, ref actionTarget);
            }
            else
            {
                return FindMemberWithHealthBelow(85, ref actionTarget);
            }
        }

        #endregion

        #region Misc
        private static class RelevantNanos
        {

        }

        private static class RelevantItems
        {

        }

        #endregion
    }
}
