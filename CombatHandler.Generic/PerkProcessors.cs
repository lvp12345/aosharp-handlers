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
                case PerkType.HEAL:
                    return HealPerk;
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
            {PerkHash.LegShot, LegShot },
            {PerkHash.BattlegroupHeal1, BattleGroupHealPerk1 },
            {PerkHash.BattlegroupHeal2, BattleGroupHealPerk2 },
            {PerkHash.BattlegroupHeal3, BattleGroupHealPerk3 },
            {PerkHash.BattlegroupHeal4, BattleGroupHealPerk4 },
            {PerkHash.WitOfTheAtrox, WitOfTheAtrox },
            {PerkHash.BioCocoon, BioCocoon },
            {PerkHash.EvasiveStance, EvasiveStance },
        };

        private static bool WitOfTheAtrox(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return SelfBuffPerk(perk, fightingTarget, ref actionTarget);
        }

        private static bool BioCocoon(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return SelfAbsorbPerk(perk, fightingTarget, ref actionTarget);
        }

        private static bool EvasiveStance(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.HealthPercent >= 75)
                return false;

            return SelfBuffPerk(perk, fightingTarget, ref actionTarget);
        }

        private static bool LegShot(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PerkAction.Find("Leg Shot", out PerkAction legshot) && !legshot.IsAvailable)
                return false;

            return LegShotPerk(perk, fightingTarget, ref actionTarget);
        }

        private static bool QuickShot(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PerkAction.Find("Double Shot", out PerkAction doubleShot) && !doubleShot.IsAvailable)
                return false;

            return DamagePerk(perk, fightingTarget, ref actionTarget);
        }

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

        public static bool BattleGroupHealPerk1(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!Team.IsInCombat())
                return false;

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 100)
                    .ToList();

                List<SimpleChar> dyingTeamMemberindivid = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 35)
                    .Where(c => c.Profession == Profession.Enforcer || c.Profession == Profession.Doctor)
                    .ToList();

                if (dyingTeamMember.Count >= 4 || dyingTeamMemberindivid.Count >= 1)
                {
                    PerkAction.Find("Battlegroup Heal 1", out PerkAction battlegroupheal1);

                    if (battlegroupheal1.IsAvailable)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool BattleGroupHealPerk2(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!Team.IsInCombat())
                return false;

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 100)
                    .ToList();

                List<SimpleChar> dyingTeamMemberindivid = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 35)
                    .Where(c => c.Profession == Profession.Enforcer || c.Profession == Profession.Doctor)
                    .ToList();

                if (dyingTeamMember.Count >= 4 || dyingTeamMemberindivid.Count >= 1)
                {
                    PerkAction.Find("Battlegroup Heal 1", out PerkAction battlegroupheal1);
                    PerkAction.Find("Battlegroup Heal 2", out PerkAction battlegroupheal2);

                    if (battlegroupheal2.IsAvailable && !battlegroupheal1.IsAvailable)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool BattleGroupHealPerk3(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!Team.IsInCombat())
                return false;

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 100)
                    .ToList();

                List<SimpleChar> dyingTeamMemberindivid = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 35)
                    .Where(c => c.Profession == Profession.Enforcer || c.Profession == Profession.Doctor)
                    .ToList();

                if (dyingTeamMember.Count >= 4 || dyingTeamMemberindivid.Count >= 1)
                {
                    PerkAction.Find("Battlegroup Heal 1", out PerkAction battlegroupheal1);
                    PerkAction.Find("Battlegroup Heal 2", out PerkAction battlegroupheal2);
                    PerkAction.Find("Battlegroup Heal 3", out PerkAction battlegroupheal3);

                    if (battlegroupheal3.IsAvailable && !battlegroupheal2.IsAvailable && !battlegroupheal1.IsAvailable)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool BattleGroupHealPerk4(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!Team.IsInCombat())
                return false;

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 40)
                    .ToList();

                List<SimpleChar> dyingTeamMemberindivid = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 35)
                    .Where(c => c.Profession == Profession.Enforcer || c.Profession == Profession.Doctor)
                    .ToList();

                if (dyingTeamMember.Count >= 4 || dyingTeamMemberindivid.Count >= 1)
                {
                    PerkAction.Find("Battlegroup Heal 1", out PerkAction battlegroupheal1);
                    PerkAction.Find("Battlegroup Heal 2", out PerkAction battlegroupheal2);
                    PerkAction.Find("Battlegroup Heal 3", out PerkAction battlegroupheal3);
                    PerkAction.Find("Battlegroup Heal 4", out PerkAction battlegroupheal4);

                    if (battlegroupheal4.IsAvailable && !battlegroupheal3.IsAvailable && !battlegroupheal2.IsAvailable && !battlegroupheal1.IsAvailable)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool SelfAbsorbPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0)
                return false;

            if (!perkAction.IsAvailable)
                return false;

            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name)
                {
                    return false;
                }
                if (buff.Name == "Endurance Skin" || buff.Name == "Flesh of the Believer" || buff.Name == "Skin of the Believer" || buff.Name == "Assault Screen")
                    return false;
            }

            if (Inventory.Find(267168, 267168, out Item enduranceabsorbenf))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength))
                {
                    return false;
                }
            }
            if (Inventory.Find(267167, 267167, out Item enduranceabsorbnanomage))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength))
                {
                    return false;
                }
            }
            if (Inventory.Find(305476, 305476, out Item absorbdesflesh))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BodyDevelopment))
                {
                    return false;
                }
            }
            if (Inventory.Find(204698, 204698, out Item absorbwithflesh))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BodyDevelopment))
                {
                    return false;
                }
            }
            if (Inventory.Find(156576, 156576, out Item absorbassaultclass))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.DuckExp))
                {
                    return false;
                }
            }

            if (DynelManager.LocalPlayer.HealthPercent >= 65)
                return false;

            return true;
        }

        public static bool SelfBuffPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!perkAction.IsAvailable)
                return false;

            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name)
                {
                    //Chat.WriteLine(buff.Name+" "+perk.Name);
                    return false;
                }
            }

            if (!DynelManager.LocalPlayer.IsAttacking && 
                (perkAction.Name == "Bio Shield" || perkAction.Name == "Wit of the Atrox" 
                || perkAction.Name == "Dodge the Blame" || perkAction.Name == "Devotional Armor"))
                return false;


            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        public static bool HealPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0)
                return false;

            // Prioritize keeping ourself alive
            if (DynelManager.LocalPlayer.HealthPercent <= 70)
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
                    .Where(c => c.HealthPercent <= 70)
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

            return DynelManager.LocalPlayer.NanoPercent < 75;
        }

        public static bool TargetedDamagePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = true;
            return DamagePerk(perkAction, fightingTarget, ref actionTarget);
        }

        public static bool LegShotPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null)
                return false;

            if (fightingTarget.MaxHealth >= 1000000)
            {
                if (fightingTarget.Name == "Technological Officer Darwelsi")
                    return false;

                if (fightingTarget.Name == "Collector")
                    return false;
            }

            if (fightingTarget.HealthPercent < 5)
                return false;

            return true;
        }

        public static bool DamagePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (perkAction.Name == "Unhallowed Wrath" || perkAction.Name == "Spectator Wrath" || perkAction.Name == "Righteous Wrath")
            {
                if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Skill2hEdged))
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

        private static class RelevantEffects
        {
            public const int ThermalPrimerBuff = 209835;
            public const int SuppressivePrimerBuff = 209834;
        }
    }
}
