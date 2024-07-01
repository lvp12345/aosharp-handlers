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

            var dyingTeamMembersCount = Team.Members
                   .Count(m => m.Character != null
                            && m.Character.Health > 0
                            && m.Character.HealthPercent <= TeamHealPercentage);

            if (dyingTeamMembersCount >= 2)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        public static bool CompleteTeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (CompleteTeamHealPercentage == 0) { return false; }

            if (!Team.IsInTeam) { return false; }

            var dyingTeamMembersCount = Team.Members
                   .Count(m => m.Character != null
                            && m.Character.Health > 0
                            && m.Character.HealthPercent <= CompleteTeamHealPercentage);

            if (dyingTeamMembersCount >= 2)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        #endregion

        #endregion

       public static bool FindMemberForTargetHeal(int healthPercentThreshold, Spell spell, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!GenericCombatHandler.CanCast(spell)) { return false; }

            if (Team.IsInTeam)
            {
                var teamMember = Team.Members.Where(t => t.Character != null && t.Character.IsInLineOfSight && t.Character.IsAlive &&
                t.Character.HealthPercent <= healthPercentThreshold && spell.IsInRange(t.Character)) .OrderBy(t => t.Character.HealthPercent)
                .FirstOrDefault();

                if (teamMember != null)
                {
                    actionTarget.Target = teamMember.Character;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }
            return false;
        }

        public static bool FindPlayerWithHealthBelow(int healthPercentThreshold, Spell spell, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!GenericCombatHandler.CanCast(spell)) { return false; }

            var player = DynelManager.Players
                .Where(c => c.Health > 0 && c.HealthPercent <= healthPercentThreshold && c.IsInLineOfSight && spell.IsInRange(c))
                .OrderBy(c => c.HealthPercent).FirstOrDefault();

            if (player != null)
            {
                actionTarget.Target = player;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }
    }
}
