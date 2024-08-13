using AOSharp.Core;
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
        public static int DragonHealingPercentage = 0;
        #region Healing

        #region target

        public static bool TargetHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (TargetHealPercentage == 0) { return false; }

            if (GenericCombatHandler._settings["AllPlayers"].AsBool())
            {
                return FindPlayerWithHealthBelow(TargetHealPercentage, spell, ref actionTarget);
            }

            if (Team.IsInTeam)
            {
                return FindMemberForTargetHeal(TargetHealPercentage, spell, ref actionTarget);
            }
            else
            {
                if (DynelManager.LocalPlayer.HealthPercent > TargetHealPercentage) { return false; }

                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }
        }
        public static bool TargetHealingAsTeam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (TeamHealPercentage == 0) { return false; }

            if (GenericCombatHandler._settings["AllPlayers"].AsBool())
            {
                return FindPlayerWithHealthBelow(TeamHealPercentage, spell, ref actionTarget);
            }

            if (Team.IsInTeam)
            {
                return FindMemberForTargetHeal(TeamHealPercentage, spell, ref actionTarget);
            }
            else
            {
                if (DynelManager.LocalPlayer.HealthPercent > TeamHealPercentage) { return false; }

                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }
        }

        public static bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (CompleteHealPercentage == 0) { return false; }

            if (GenericCombatHandler._settings["AllPlayers"].AsBool())
            {
                return FindPlayerWithHealthBelow(CompleteHealPercentage, spell, ref actionTarget);
            }

            if (Team.IsInTeam)
            {
                return FindMemberForTargetHeal(CompleteHealPercentage, spell, ref actionTarget);
            }
            else
            {
                if (DynelManager.LocalPlayer.HealthPercent > CompleteHealPercentage) { return false; }

                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }
        }

        public static bool FountainOfLife(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FountainOfLifeHealPercentage == 0) { return false; }

            if (GenericCombatHandler._settings["AllPlayers"].AsBool())
            {
                return FindPlayerWithHealthBelow(FountainOfLifeHealPercentage, spell, ref actionTarget);
            }

            if (Team.IsInTeam)
            {
                return FindMemberForTargetHeal(FountainOfLifeHealPercentage, spell, ref actionTarget);
            }
            else
            {
                if (DynelManager.LocalPlayer.HealthPercent > FountainOfLifeHealPercentage) { return false; }

                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;

            }
        }

        #endregion

        #region Team

        public static bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (TeamHealPercentage == 0) { return false; }

            if (!Team.IsInTeam) { return false; }

            var dyingTeamMembersCount = Team.Members
                   .Count(m => m.Character != null
                            && m.Character.Health > 0
                            && m.Character.HealthPercent <= TeamHealPercentage);

            if (dyingTeamMembersCount < 2) { return false; }

            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        public static bool CompleteTeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (CompleteTeamHealPercentage == 0) { return false; }

            if (!Team.IsInTeam) { return false; }

            var dyingTeamMembersCount = Team.Members
                   .Count(m => m.Character != null
                            && m.Character.Health > 0
                            && m.Character.HealthPercent <= CompleteTeamHealPercentage);

            if (dyingTeamMembersCount < 2) { return false; }

            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        #endregion

        #endregion

        public static bool FindMemberForTargetHeal(int healthPercentThreshold, Spell spell, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!GenericCombatHandler.CanCast(spell)) { return false; }

            if (!Team.IsInTeam) { return false; }

            var teamMember = Team.Members.Where(t => t?.Character != null && t.Character.IsInLineOfSight && t.Character.Health > 0 &&
            t.Character.HealthPercent <= healthPercentThreshold && spell.IsInRange(t?.Character)).OrderBy(t => t.Character.HealthPercent)
            .FirstOrDefault();

            if (teamMember == null) { return false; }

            actionTarget.Target = teamMember.Character;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        public static bool FindPlayerWithHealthBelow(int healthPercentThreshold, Spell spell, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!GenericCombatHandler.CanCast(spell)) { return false; }

            var player = DynelManager.Players
                .Where(c => c != null && c.Health > 0 && c.HealthPercent <= healthPercentThreshold && c.IsInLineOfSight && spell.IsInRange(c))
                .OrderBy(c => c.HealthPercent).FirstOrDefault();

            if (player == null) { return false; }

            actionTarget.Target = player;
            actionTarget.ShouldSetTarget = true;
            return true;
        }
    }
}
