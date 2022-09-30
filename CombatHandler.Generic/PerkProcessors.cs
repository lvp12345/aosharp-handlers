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
                case PerkType.NANO_HEAL:
                    return NanoPerk;
                case PerkType.SELF_BUFF:
                    return SelfBuffPerk;
                case PerkType.DAMAGE_BUFF:
                    return DamageBuffPerk;
                case PerkType.CLEANSE:
                case PerkType.PET_BUFF:
                case PerkType.PET_HEAL:
                case PerkType.DISABLED:
                case PerkType.LE_PROC:
                case PerkType.OTHER:
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
            {PerkHash.BattlegroupHeal1, BattleGroupHealPerk1 },
            {PerkHash.BattlegroupHeal2, BattleGroupHealPerk2 },
            {PerkHash.BattlegroupHeal3, BattleGroupHealPerk3 },
            {PerkHash.BattlegroupHeal4, BattleGroupHealPerk4 },
            {PerkHash.WitOfTheAtrox, WitOfTheAtrox },
            {PerkHash.EvasiveStance, EvasiveStance },
        };

        private static bool WitOfTheAtrox(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return SelfBuffPerk(perk, fightingTarget, ref actionTarget);
        }

        private static bool EvasiveStance(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.HealthPercent >= 75)
                return false;

            return SelfBuffPerk(perk, fightingTarget, ref actionTarget);
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
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                //if (!Team.IsInCombat()) { return false; }

                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.HealthPercent <= 40)
                    .ToList();

                if (dyingTeamMember.Count >= 1)
                {
                    PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Team);

                    if (_bgHeal1Team?.IsAvailable == true) { return true; }
                }
            }

            if (/*DynelManager.LocalPlayer.FightingTarget == null || */DynelManager.LocalPlayer.HealthPercent > 40) { return false; }

            if (PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Self))
            {
                if (_bgHeal1Self?.IsAvailable == true) { return true; }
            }

            return false;
        }

        public static bool BattleGroupHealPerk2(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                //if (!Team.IsInCombat()) { return false; }

                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.HealthPercent <= 40)
                    .ToList();

                if (dyingTeamMember.Count >= 1)
                {
                    PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Team);
                    PerkAction.Find("Battlegroup Heal 2", out PerkAction _bgHeal2Team);

                    if (!_bgHeal1Team?.IsAvailable == true && _bgHeal2Team?.IsAvailable == true) { return true; }
                }
            }

            if (/*DynelManager.LocalPlayer.FightingTarget == null || */DynelManager.LocalPlayer.HealthPercent > 40) { return false; }

            if (PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Self))
            {
                if (!_bgHeal1Self?.IsAvailable == true)
                {
                    PerkAction.Find("Battlegroup Heal 2", out PerkAction _bgHeal2Self);

                    if (_bgHeal2Self?.IsAvailable == true) { return true; }
                }
            }

            return false;
        }

        public static bool BattleGroupHealPerk3(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                //if (!Team.IsInCombat()) { return false; }

                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.HealthPercent <= 40)
                    .ToList();

                if (dyingTeamMember.Count >= 1)
                {
                    PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Team);
                    PerkAction.Find("Battlegroup Heal 2", out PerkAction _bgHeal2Team);
                    PerkAction.Find("Battlegroup Heal 3", out PerkAction _bgHeal3Team);

                    if (!_bgHeal1Team?.IsAvailable == true && !_bgHeal2Team?.IsAvailable == true
                        && _bgHeal3Team?.IsAvailable == true) { return true; }
                }
            }

            if (/*DynelManager.LocalPlayer.FightingTarget == null || */DynelManager.LocalPlayer.HealthPercent > 40) { return false; }

            if (PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Self) && PerkAction.Find("Battlegroup Heal 2", out PerkAction _bgHeal2Self))
            {
                if (!_bgHeal1Self?.IsAvailable == true && !_bgHeal2Self?.IsAvailable == true)
                {
                    PerkAction.Find("Battlegroup Heal 3", out PerkAction _bgHeal3Self);

                    if (_bgHeal3Self?.IsAvailable == true) { return true; }
                }
            }

            return false;
        }

        public static bool BattleGroupHealPerk4(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                //if (!Team.IsInCombat()) { return false; }

                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.HealthPercent <= 40)
                    .ToList();

                if (dyingTeamMember.Count >= 1)
                {
                    PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Team);
                    PerkAction.Find("Battlegroup Heal 2", out PerkAction _bgHeal2Team);
                    PerkAction.Find("Battlegroup Heal 3", out PerkAction _bgHeal3Team);
                    PerkAction.Find("Battlegroup Heal 4", out PerkAction _bgHeal4Team);

                    if (!_bgHeal1Team?.IsAvailable == true && !_bgHeal2Team?.IsAvailable == true
                        && !_bgHeal3Team?.IsAvailable == true && _bgHeal4Team?.IsAvailable == true) { return true; }
                }
            }

            if (/*DynelManager.LocalPlayer.FightingTarget == null || */DynelManager.LocalPlayer.HealthPercent > 40) { return false; }

            if (PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Self) && PerkAction.Find("Battlegroup Heal 2", out PerkAction _bgHeal2Self)
                && PerkAction.Find("Battlegroup Heal 3", out PerkAction _bgHeal3Self))
            {
                if (!_bgHeal1Self?.IsAvailable == true && !_bgHeal2Self?.IsAvailable == true
                    && !_bgHeal3Self?.IsAvailable == true)
                {
                    PerkAction.Find("Battlegroup Heal 4", out PerkAction _bgHeal4Self);

                    if (_bgHeal4Self?.IsAvailable == true) { return true; }
                }
            }

            return false;
        }

        public static bool SelfAbsorbPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!perkAction.IsAvailable) { return false; }

            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name)
                {
                    return false;
                }
                if (buff.Name == "Endurance Skin" || buff.Name == "Flesh of the Believer" || buff.Name == "Skin of the Believer" || buff.Name == "Assault Screen")
                    return false;
            }

            //if (Inventory.Find(267168, 267168, out Item enduranceabsorbenf))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength))
            //    {
            //        return false;
            //    }
            //}
            //if (Inventory.Find(267167, 267167, out Item enduranceabsorbnanomage))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength))
            //    {
            //        return false;
            //    }
            //}
            //if (Inventory.Find(305476, 305476, out Item absorbdesflesh))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BodyDevelopment))
            //    {
            //        return false;
            //    }
            //}
            //if (Inventory.Find(204698, 204698, out Item absorbwithflesh))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BodyDevelopment))
            //    {
            //        return false;
            //    }
            //}
            //if (Inventory.Find(156576, 156576, out Item absorbassaultclass))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.DuckExp))
            //    {
            //        return false;
            //    }
            //}

            if (DynelManager.LocalPlayer.HealthPercent >= 70)
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

        public static bool NanoPerk(PerkAction perkAction, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0)
                return false;

            // Prioritize keeping ourself alive
            if (DynelManager.LocalPlayer.NanoPercent <= 70)
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
                    .Where(c => c.NanoPercent <= 75)
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    actionTarget.Target = dyingTeamMember;
                    return true;
                }
            }

            return false;
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
