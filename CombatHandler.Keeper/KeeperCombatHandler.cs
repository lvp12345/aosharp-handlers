using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;
using System;
using System.Linq;

namespace Desu
{
    public class KeeperCombatHandler : GenericCombatHandler
    {
        private Menu _menu;
        public KeeperCombatHandler()
        {

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
            RegisterSpellProcessor(RelevantNanos.CourageOfTheJust, CourageOfTheJust);

            _menu = new Menu("CombatHandler.Keeper", "CombatHandler.Keeper");
            //_menu.AddItem(new MenuBool("UseAIDot", "Use AI DoT", true));
            OptionPanel.AddMenu(_menu);
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
        //TODO: Rework
        private bool CourageOfTheJust(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking)
                return false;

            if (DynelManager.LocalPlayer.Buffs.Find(spell.Identity.Instance, out Buff buff) && buff.RemainingTime > 5)
                return false;

            return false;
        }

        private static class RelevantNanos
        {
            public const int CourageOfTheJust = 279379;
        }
    }
}
