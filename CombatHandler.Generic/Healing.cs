using AOSharp.Core;
using AOSharp.Core.UI;
using System.Linq;

namespace CombatHandler.Generic
{
    public class Healing
    {
        public static int TargetHealPercentage = 0;
        public static int CompleteHealPercentage = 0;
        public static int FountainOfLifeHealPercentage = 0;
        public static int TeamHealPercentage = 0;
        public static int CompleteTeamHealPercentage = 0;

        #region Healing

        #region target

        public static bool TargetHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (TargetHealPercentage == 0) { return false; }

            if (Team.IsInTeam)
            {
                return FindMemberForTargetHeal(TargetHealPercentage, spell, ref actionTarget);
            }
            else
            {
                if (DynelManager.LocalPlayer.HealthPercent <= TargetHealPercentage)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = DynelManager.LocalPlayer;
                    return true;
                }
            }

            return false;
        }

        public static bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (CompleteHealPercentage == 0) { return false; }

            if (Team.IsInTeam)
            {
                return FindMemberForTargetHeal(CompleteHealPercentage, spell, ref actionTarget);
            }
            else
            {
                if (DynelManager.LocalPlayer.HealthPercent <= CompleteHealPercentage)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = DynelManager.LocalPlayer;
                    return true;
                }
            }

            return false;
        }

        public static bool FountainOfLife(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FountainOfLifeHealPercentage == 0) { return false; }

            if (Team.IsInTeam)
            {
                return FindMemberForTargetHeal(FountainOfLifeHealPercentage, spell, ref actionTarget);
            }
            else
            {
                if (DynelManager.LocalPlayer.HealthPercent <= FountainOfLifeHealPercentage)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = DynelManager.LocalPlayer;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Team

        public static bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (TeamHealPercentage == 0) { return false; }

            if (!Team.IsInTeam) { return false; }

            var teamIndex = Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex;

            var count = DynelManager.Characters.Count(c =>
                Team.Members.Any(m => m.TeamIndex == teamIndex && m.Identity.Instance == c.Identity.Instance)
                && c.HealthPercent <= TeamHealPercentage && c.Health > 0);

            if (count >= 2)
            {
                actionTarget.ShouldSetTarget = false;
                return true;
            }

            return false;
        }

        public static bool CompleteTeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (CompleteTeamHealPercentage == 0) { return false; }

            if (!Team.IsInTeam) { return false; }

            var teamIndex = Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex;

            var count = DynelManager.Characters.Count(c =>
                Team.Members.Any(m => m.TeamIndex == teamIndex && m.Identity.Instance == c.Identity.Instance)
                && c.HealthPercent <= CompleteTeamHealPercentage && c.Health > 0);

            if (count >= 2)
            {
                actionTarget.ShouldSetTarget = false;
                return true;
            }

            return false;
        }

        #endregion

        #endregion

       static bool FindMemberForTargetHeal(int healthPercentThreshold, Spell spell, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!GenericCombatHandler.CanCast(spell)) { return false; }

            if (Team.IsInTeam)
            {
                SimpleChar teamMember = DynelManager.Players
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.HealthPercent <= healthPercentThreshold && c.IsInLineOfSight
                        && GenericCombatHandler.InNanoRange(c)
                        && c.Health > 0)
                    .OrderBy(c => c.HealthPercent)
                    .FirstOrDefault();

                if (teamMember != null)
                {
                    actionTarget.Target = teamMember;
                    actionTarget.ShouldSetTarget = true;

                    return true;
                }
            }
            return false;
        }

        static bool FindPlayerWithHealthBelow(int healthPercentThreshold, Spell spell, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!GenericCombatHandler.CanCast(spell)) { return false; }

            SimpleChar player = DynelManager.Players
                .Where(c => c.HealthPercent <= healthPercentThreshold
                    && c.IsInLineOfSight
                    && GenericCombatHandler.InNanoRange(c)
                    && c.Health > 0)
                .OrderBy(c => c.HealthPercent)
                    .FirstOrDefault();

            if (player != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = player;
                return true;
            }

            return false;
        }
    }
}
