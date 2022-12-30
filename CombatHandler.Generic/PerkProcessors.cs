using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Collections.Specialized.BitVector32;

namespace CombatHandler.Generic
{
    public class PerkCondtionProcessors
    {
        private static double _timer = 0f;

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
            {PerkHash.WitOfTheAtrox, SelfBuffPerk },
            {PerkHash.EvasiveStance, EvasiveStance },
        };

        private static bool EvasiveStance(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.HealthPercent >= 75) { return false; }

            return SelfBuffPerk(perk, fightingTarget, ref actionTarget);
        }

        private static bool QuickShot(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PerkAction.Find("Double Shot", out PerkAction doubleShot) && !doubleShot.IsAvailable) { return false; }

            return DamagePerk(perk, fightingTarget, ref actionTarget);
        }

        public static bool DamageBuffPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking || fightingTarget == null) { return false; }

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
            if (!DynelManager.LocalPlayer.IsAttacking) { return false; }

            if (fightingTarget.Buffs.Find(primerBuffId, out Buff primerBuff))
                if (primerBuff.RemainingTime > 10) //Only install device if it will trigger before primer expires
                {
                    return true;
                }

            return false;
        }

        private static bool StarfallPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PerkAction.Find(PerkHash.Combust, out PerkAction combust) && !combust.IsAvailable) { return false; }

            return TargetedDamagePerk(perkAction, fightingTarget, ref actionTarget);
        }

        private static bool Moonmist(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || (fightingTarget.HealthPercent < 90 && DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) < 2)) { return false; }

            actionTarget.ShouldSetTarget = false;
            return true;
        }

        public static bool BattleGroupHealPerk1(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.HealthPercent <= 40)
                    .ToList();

                if (dyingTeamMember.Count >= 1)
                    return perkAction.IsAvailable;
            }

            if (DynelManager.LocalPlayer.HealthPercent > 40) { return false; }

            return perkAction.IsAvailable;
        }

        public static bool BattleGroupHealPerk2(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.HealthPercent <= 40)
                    .ToList();

                if (dyingTeamMember.Count >= 1)
                {
                    PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Team);
                    PerkAction.Find("Battlegroup Heal 2", out PerkAction _bgHeal2Team);

                    if (_bgHeal1Team?.IsAvailable == false && _bgHeal2Team?.IsAvailable == false) 
                        return perkAction.IsAvailable;
                }
            }

            if (DynelManager.LocalPlayer.HealthPercent > 40) { return false; }

            if (PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Self))
            {
                if (_bgHeal1Self?.IsAvailable == false)
                {
                    PerkAction.Find("Battlegroup Heal 2", out PerkAction _bgHeal2Self);

                    if (_bgHeal2Self?.IsAvailable == false) 
                        return perkAction.IsAvailable;
                }
            }

            return false;
        }

        public static bool BattleGroupHealPerk3(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.HealthPercent <= 40)
                    .ToList();

                if (dyingTeamMember.Count >= 1)
                {
                    PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Team);
                    PerkAction.Find("Battlegroup Heal 2", out PerkAction _bgHeal2Team);

                    if (_bgHeal1Team?.IsAvailable == false && _bgHeal2Team?.IsAvailable == false)
                        return perkAction.IsAvailable;
                }
            }

            if (DynelManager.LocalPlayer.HealthPercent > 40) { return false; }

            if (PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Self) && PerkAction.Find("Battlegroup Heal 2", out PerkAction _bgHeal2Self))
            {
                if (_bgHeal1Self?.IsAvailable == false && _bgHeal2Self?.IsAvailable == false)
                    return perkAction.IsAvailable;
            }

            return false;
        }

        public static bool BattleGroupHealPerk4(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.HealthPercent <= 40)
                    .ToList();

                if (dyingTeamMember.Count >= 1)
                {
                    PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Team);
                    PerkAction.Find("Battlegroup Heal 2", out PerkAction _bgHeal2Team);
                    PerkAction.Find("Battlegroup Heal 3", out PerkAction _bgHeal3Team);

                    if (_bgHeal1Team?.IsAvailable == false && _bgHeal2Team?.IsAvailable == false
                        && _bgHeal3Team?.IsAvailable == false)
                        return perkAction.IsAvailable;
                }
            }

            if (DynelManager.LocalPlayer.HealthPercent > 40) { return false; }

            if (PerkAction.Find("Battlegroup Heal 1", out PerkAction _bgHeal1Self) && PerkAction.Find("Battlegroup Heal 2", out PerkAction _bgHeal2Self)
                && PerkAction.Find("Battlegroup Heal 3", out PerkAction _bgHeal3Self))
            {
                if (_bgHeal1Self?.IsAvailable == false && !_bgHeal2Self?.IsAvailable == false
                    && _bgHeal3Self?.IsAvailable == false)
                return perkAction.IsAvailable;
            }

            return false;
        }

        public static bool SelfAbsorbPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!perkAction.IsAvailable) { return false; }

            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name) { return false; }

                if (buff.Name == "Endurance Skin" || buff.Name == "Flesh of the Believer" 
                    || buff.Name == "Skin of the Believer" || buff.Name == "Assault Screen") { return false; }
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

            if (DynelManager.LocalPlayer.HealthPercent >= 70) { return false; }

            return true;
        }

        public static bool LeadershipPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!perkAction.IsAvailable) { return false; }

            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name) { return false; }
            }

            return perkAction.IsAvailable;
        }
        public static bool GovernancePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!perkAction.IsAvailable) { return false; }

            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name) { return false; }
            }

            if (PerkAction.Find("Leadership", out PerkAction _leadership))
            {
                if (_leadership?.IsAvailable == false)
                    return perkAction.IsAvailable;
            }

            return false;
        }
        public static bool TheDirectorPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!perkAction.IsAvailable) { return false; }

            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name) { return false; }
            }

            if (PerkAction.Find("Leadership", out PerkAction _leadership) && PerkAction.Find("Governance", out PerkAction _governance))
            {
                if (_leadership?.IsAvailable == false && _governance?.IsAvailable == false)
                    return perkAction.IsAvailable;
            }

            return false;
        }

        public static bool VolunteerPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!perkAction.IsAvailable) { return false; }

            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name) { return false; }
            }

            if (Time.NormalTime < _timer + 363f) { return false; }

            _timer = Time.NormalTime;
            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        public static bool SelfBuffPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!perkAction.IsAvailable) { return false; }

            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name) { return false; }
            }

            if (!DynelManager.LocalPlayer.IsAttacking && 
                (perkAction.Name == "Bio Shield" || perkAction.Name == "Wit of the Atrox" 
                || perkAction.Name == "Dodge the Blame" || perkAction.Name == "Devotional Armor")
                || perkAction.Name == "Limber" || perkAction.Name == "Dance of Fools"
                || perkAction.Name == "Leg Shot") { return false; }

            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        public static bool HealPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => c.Health > 0
                        && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.HealthPercent <= 75)
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    actionTarget.Target = dyingTeamMember;
                    return true;
                }
            }

            if (DynelManager.LocalPlayer.HealthPercent <= 70)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        public static bool NanoPerk(PerkAction perkAction, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => c.Health > 0
                        && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.NanoPercent <= 75)
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    actionTarget.Target = dyingTeamMember;
                    return true;
                }
            }

            if (DynelManager.LocalPlayer.NanoPercent <= 70)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
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
                if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Skill2hEdged)) { return false; }

            if (fightingTarget == null || fightingTarget.HealthPercent < 5) { return false; }

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
