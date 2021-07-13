using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CombatHandler.Generic
{
    public class PerkCondtionProcessors
    {
        public static GenericPerkConditionProcessor GetPerkConditionProcessor(PerkAction perkAction)
        {
            PerkHash perkHash = perkAction.Hash;
            PerkType perkType = PerkTypes.GetPerkType(perkHash);

            switch (perkType)
            {
                case PerkType.CUSTOM:
                    if (!CUSTOM_PROCESSORS.ContainsKey(perkHash))
                    {
                        Chat.WriteLine("Attempt to register custom perk processor without defintion. Perk name: " + perkAction.Name);
                        return null;
                    }
                    return CUSTOM_PROCESSORS[perkHash];
                case PerkType.TARGETED_DAMAGE:
                    return TargetedDamagePerk;
                case PerkType.TEAM_HEAL:
                    return TeamHealPerk;
                case PerkType.SELF_HEAL:
                    return SelfHealPerk;
                case PerkType.SELF_BUFF:
                    return SelfBuffPerk;
                case PerkType.DAMAGE_BUFF:
                    return DamageBuffPerk;
                case PerkType.SELF_NANO_HEAL:
                case PerkType.TEAM_NANO_HEAL:
                case PerkType.CLEANSE:
                case PerkType.PET_BUFF:
                case PerkType.PET_HEAL:
                case PerkType.DISABLED:
                case PerkType.LE_PROC:
                    return null;
                default:
                    Chat.WriteLine("Attempt to register unknown perk type for perk name: " + perkAction.Name);
                    return null;
            }
        }

        private static Dictionary<PerkHash, GenericPerkConditionProcessor> CUSTOM_PROCESSORS = new Dictionary<PerkHash, GenericPerkConditionProcessor>()
        {
            {PerkHash.Moonmist, Moonmist },
            {PerkHash.DazzleWithLights, StarfallPerk },
            {PerkHash.InstallExplosiveDevice, InstallExplosiveDevice },
            {PerkHash.InstallNotumDepletionDevice, InstallNotumDepletionDevice },
            {PerkHash.QuickShot, QuickShot },
            //{PerkHash.UnhallowedWrath, UnhallowedWrath }
        };

        private static bool QuickShot(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PerkAction.Find("Double Shot", out PerkAction doubleShot) && !doubleShot.IsAvailable)
                return false;

            return DamagePerk(perk, fightingTarget, ref actionTarget);
        }

        //private static bool UnhallowedWrath(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (PerkAction.Find("Unhallowed Wrath", out PerkAction unhallowedwrath) && !unhallowedwrath.IsAvailable)
        //        return false;

        //    return DamagePerk(perk, fightingTarget, ref actionTarget);
        //}

        public static bool DamageBuffPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking || fightingTarget == null)
            {
                return false;
            }
            return true;
        }

        private static bool InstallExplosiveDevice(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ShouldInstallPrimedDevice(fightingTarget, RelevantEffects.ThermalPrimerBuff);
        }

        private static bool InstallNotumDepletionDevice(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ShouldInstallPrimedDevice(fightingTarget, RelevantEffects.SuppressivePrimerBuff);
        }

        private static bool ShouldInstallPrimedDevice(SimpleChar fightingTarget, int primerBuffId)
        {
            if (!DynelManager.LocalPlayer.IsAttacking)
            {
                return false;
            }

            if (fightingTarget.Buffs.Find(primerBuffId, out Buff primerBuff))
            {
                if (primerBuff.RemainingTime > 10) //Only install device if it will trigger before primer expires
                {
                    return true;
                }
            }

            return false;
        }

        private static bool StarfallPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PerkAction.Find(PerkHash.Combust, out PerkAction combust) && !combust.IsAvailable)
                return false;

            return TargetedDamagePerk(perkAction, fightingTarget, ref actionTarget);
        }

        private static bool Moonmist(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = false;

            if (fightingTarget == null || (fightingTarget.HealthPercent < 90 && DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) < 2))
                return false;

            return true;
        }

        public static bool SelfBuffPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name)
                {
                    //Chat.WriteLine(buff.Name+" "+perk.Name);
                    return false;
                }
            }
            return true;
        }

        public static bool SelfHealPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking)
                return false;

            // Prioritize keeping ourself alive
            if (DynelManager.LocalPlayer.HealthPercent <= 35)//We should consider making this a slider value in the options
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }
            return false;
        }

        public static bool TeamHealPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            if (!DynelManager.LocalPlayer.IsAttacking)
                return false;

            // Prioritize keeping ourself alive
            if (DynelManager.LocalPlayer.HealthPercent <= 60)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 60)
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

        public bool SelfNanoHeal(PerkAction perkAction, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (fightingtarget == null)
            {
                return false;
            }

            return DynelManager.LocalPlayer.NanoPercent < 50;
        }

        public static bool TargetedDamagePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = true;
            return DamagePerk(perkAction, fightingTarget, ref actionTarget);
        }

        public static bool DamagePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (perkAction.Name == "Unhallowed Wrath" || perkAction.Name == "Spectator Wrath" || perkAction.Name == "Righteous Wrath")
            {
                if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat()))
                {
                    return false;
                }
            }

            if (fightingTarget == null)
                return false;

            if (fightingTarget.Health > 50000)
                return true;

            if (fightingTarget.HealthPercent < 5)
                return false;

            return true;
        }

        public delegate bool GenericPerkConditionProcessor(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget);

        private static Stat GetSkillLockStat()
        {
            return Stat.Skill2hEdged;
        }

        private static class RelevantEffects
        {
            public const int ThermalPrimerBuff = 209835;
            public const int SuppressivePrimerBuff = 209834;
        }
    }
}
