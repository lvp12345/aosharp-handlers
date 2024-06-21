using AOSharp.Common.GameData;
using AOSharp.Core;
using System.Collections.Generic;
using System.Linq;
using static CombatHandler.Generic.GenericCombatHandler;

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
                //Chat.WriteLine("Attempt to register custom perk processor without defintion. Perk name: " + perkAction.Name);
                case PerkType.Custom:
                    if (!CustomProcessor.ContainsKey(perkHash)) { return null; }
                    return CustomProcessor[perkHash];
                case PerkType.SelfBuff:
                    return BuffPerk;
                case PerkType.SelfHeal:
                    return SelfHealPerk;
                case PerkType.SelfNano:
                    return SelfNanoPerk;
                case PerkType.TeamHeal:
                    return TeamHealPerk;
                case PerkType.TeamNano:
                    return TeamNanoPerk;
                case PerkType.TargetDamage:
                    return TargetedDamagePerk;
                case PerkType.advy_morph_perks:
                    return ADVYDamageBuffPerk;
                case PerkType.DamageBuff:
                    return DamageBuffPerk;
                case PerkType.CombatBuff:
                    return CombatBuffPerk;
                case PerkType.Pet_Buff:
                    return PetBuffPerk;
                case PerkType.Combat_PetBuff:
                    return PetCombatBuffPerk;
                case PerkType.Pet_Heal:
                    return PetHealPerk;
                case PerkType.LEProc:
                case PerkType.NanoShutdown_TraderDebuff_Cleanse:
                    return NanoShutdown_TraderDebuff_CleansePerk;
                case PerkType.Trader_Debuff_Cleanse:
                    return Trader_Debuff_CleansePerk;
                case PerkType.AAO_Dots_Cleanse:
                    return AAO_Dots_CleansePerk;
                case PerkType.Root_Snare_Cleanse:
                    return Root_Snare_CleansePerk;
                case PerkType.AOO_Trader_DOts_Init_Cleanse:
                    return AOO_Trader_DOts_Init_CleansePerk;
                case PerkType.Snare_Cleanse:
                    return Snare_CleansePerk;
                case PerkType.Root_Cleanse:
                    return Root_CleansePerk;
                case PerkType.Dot_Cleanse:
                    return Dot_CleansePerk;
                case PerkType.NanoShutdown_Cleanse:
                    return NanoShutdown_CleansePerk;
                case PerkType.Cleanse:
                case PerkType.Disabled:
                case PerkType.Unknown:
                    return null;
                default:
                    //Chat.WriteLine("Attempt to register unknown perk type for perk name: " + perkAction.Name);
                    return null;
            }
        }

        private static Dictionary<PerkHash, GenericPerkConditionProcessor> CustomProcessor = new Dictionary<PerkHash, GenericPerkConditionProcessor>()
        {
            {PerkHash.InstallExplosiveDevice, InstallExplosiveDevice },
            {PerkHash.InstallNotumDepletionDevice, InstallNotumDepletionDevice },
        };

        private static bool InstallExplosiveDevice(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            return ShouldInstallPrimedDevice(fightingTarget, RelevantEffects.ThermalPrimerBuff);
        }

        private static bool InstallNotumDepletionDevice(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            return ShouldInstallPrimedDevice(fightingTarget, RelevantEffects.SuppressivePrimerBuff);
        }

        private static bool ShouldInstallPrimedDevice(SimpleChar fightingTarget, int primerBuffId)
        {
            if (fightingTarget == null) { return false; }

            if (fightingTarget.Buffs.Find(primerBuffId, out Buff primerBuff))
                if (primerBuff.RemainingTime > 10) //Only install device if it will trigger before primer expires
                {
                    return true;
                }

            return false;
        }

        public static bool ADVYDamageBuffPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Profession != Profession.Adventurer) { return false; }
            if (fightingTarget == null) { return false; }
           
            if (!AdvyMorphs.Any(buffId => DynelManager.LocalPlayer.Buffs.Contains(buffId))) { return false; }

            return true;
        }

        public static bool DamageBuffPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            return true;
        }

        public static bool LeadershipPerk(PerkAction perkAction)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name) { return false; }
            }

            return perkAction.IsAvailable;
        }
        public static bool GovernancePerk(PerkAction perkAction)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name) { return false; }
            }

            if (PerkAction.Find("Leadership", out PerkAction _leadership))
            {
                if (_leadership?.IsAvailable == false && _leadership?.IsExecuting == false)
                    return perkAction.IsAvailable;
            }

            return false;
        }
        public static bool TheDirectorPerk(PerkAction perkAction)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name) { return false; }
            }

            if (PerkAction.Find("Leadership", out PerkAction _leadership) && PerkAction.Find("Governance", out PerkAction _governance))
            {
                if (_leadership?.IsAvailable == false && _leadership?.IsExecuting == false && _governance?.IsAvailable == false && _governance?.IsExecuting == false)
                    return perkAction.IsAvailable;
            }

            return false;
        }

        public static bool VolunteerPerk(PerkAction perkAction, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name) { return false; }
            }

            if (Time.NormalTime < _timer + 363f) { return false; }

            _timer = Time.NormalTime;
            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = true;
            return perkAction.IsAvailable;
        }

        public static bool BuffPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsPlayerFlyingOrFalling()) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Any(buff => buff.Name == perkAction.Name))
            {
                return false;
            }

            actionTarget = (DynelManager.LocalPlayer, true);
            return true;
        }

        public static bool SelfHealPerk(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SelfHealPerkPercentage == 0) { return false; }
            if (IsPlayerFlyingOrFalling()) { return false; }
            if (!perk.IsAvailable) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent <= SelfHealPerkPercentage)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = false;
                return true;
            }

            return false;
        }

        //public static bool TargetHealPerk(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (IsPlayerFlyingOrFalling()) { return false; }

        //    if (TargetHealPerkPercentage == 0) { return false; }

        //    if (!perk.IsAvailable) { return false; }

        //    if (Team.IsInTeam)
        //    {
        //        SimpleChar teamMember = DynelManager.Players
        //            .Where(c => Team.Members.Any(t => t.Identity.Instance == c.Identity.Instance)
        //                && c.HealthPercent <= TargetHealPerkPercentage && c.IsInLineOfSight
        //                && spell.IsInRange(c)
        //                && c.IsAlive)
        //            .OrderBy(c => c.HealthPercent)
        //            .FirstOrDefault();

        //        if (teamMember != null)
        //        {
        //            actionTarget.Target = teamMember;
        //            actionTarget.ShouldSetTarget = true;

        //            return true;
        //        }
        //    }
        //    else
        //    {
        //        if (DynelManager.LocalPlayer.HealthPercent <= TargetHealPerkPercentage)
        //        {
        //            actionTarget.ShouldSetTarget = true;
        //            actionTarget.Target = DynelManager.LocalPlayer;
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        public static bool TeamHealPerk(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsPlayerFlyingOrFalling()) { return false; }

            if (TeamHealPerkPercentage == 0) { return false; }

            if (!perk.IsAvailable) { return false; }

            if (Team.IsInTeam)
            {
                var teamMember = DynelManager.Players
                    .Where(c => Team.Members.Any(t => t.Identity.Instance == c.Identity.Instance)
                        && c.HealthPercent <= TeamHealPerkPercentage && c.IsInLineOfSight
                        && perk.IsInRange(c)
                        && c.IsAlive)
                    .OrderBy(c => c.HealthPercent)
                    .FirstOrDefault();

                if (teamMember != null)
                {
                    actionTarget.Target = teamMember;
                    actionTarget.ShouldSetTarget = true;

                    return true;
                }
            }
            else
            {
                if (DynelManager.LocalPlayer.HealthPercent <= TeamHealPerkPercentage)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = DynelManager.LocalPlayer;
                    return true;
                }
            }

            return false;
        }

        public static bool SelfNanoPerk(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsPlayerFlyingOrFalling()) { return false; }

            if (!perk.IsAvailable) { return false; }

            if (DynelManager.LocalPlayer.NanoPercent <= SelfNanoPerkPercentage)
            {
                return CombatBuffPerk(perk, fightingTarget, ref actionTarget);
            }
            return false;
        }

        public static bool TeamNanoPerk(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsPlayerFlyingOrFalling()) { return false; }

            if (Team.IsInTeam)
            {
                var _person = DynelManager.Players
                    .Where(c => c.Health > 0
                        && Team.Members.Any(t => t.Identity.Instance == c.Identity.Instance)
                        && c.NanoPercent <= TeamNanoPerkPercentage)
                    .FirstOrDefault();

                if (_person != null)
                {
                    actionTarget.ShouldSetTarget = false;
                    actionTarget.Target = _person;
                    return true;
                }
            }

            if (DynelManager.LocalPlayer.NanoPercent <= TeamNanoPerkPercentage)
            {
                return CombatBuffPerk(perk, fightingTarget, ref actionTarget);
            }

            return false;
        }

        public static bool CombatBuffPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perkAction.Name) { return false; }
            }

            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        public static bool PetBuffPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsPlayerFlyingOrFalling()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null
                    || pet.Character.Health == 0
                    || pet.Type != PetType.Attack) continue;

                if (!pet.Character.Buffs.Any(buff => buff.Nanoline == NanoLine.ChannelRage))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        public static bool PetCombatBuffPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (CanPerkPet(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        public static bool PetHealPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (pet.Character.HealthPercent <= 90)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        public static bool TargetedDamagePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            actionTarget.ShouldSetTarget = true;
            return DamagePerk(perkAction, fightingTarget, ref actionTarget);
        }

        public static bool DamagePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || (fightingTarget.MaxHealth < 1000000 && fightingTarget.HealthPercent < 5)) { return false; }

            if (perkAction.Name == "Unhallowed Wrath" || perkAction.Name == "Spectator Wrath" || perkAction.Name == "Righteous Wrath" || perkAction.Name == "Righteous Fury Item")
            {
                if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Skill2hEdged)) { return false; }
            }
            return true;
        }

        public static bool NanoShutdown_TraderDebuff_CleansePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            var target = DynelManager.Players
                .Where(c => c.Buffs.Contains(NanoLine.TraderSkillTransferTargetDebuff_Deprive)
            || c.Buffs.Contains(NanoLine.TraderSkillTransferTargetDebuff_Ransack)
            || c.Buffs.Contains(NanoLine.NanoShutdownDebuff))
            .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        public static bool Trader_Debuff_CleansePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            var target = DynelManager.Players
            .Where(c => c.Buffs.Contains(NanoLine.TraderSkillTransferTargetDebuff_Deprive)
            || c.Buffs.Contains(NanoLine.TraderSkillTransferTargetDebuff_Ransack))
            .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        public static bool AAO_Dots_CleansePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            var target = DynelManager.Players
            .Where(c => c.Buffs.Contains(NanoLine.AAODebuffs)
            || c.Buffs.Contains(NanoLine.TraderAAODrain)
            || c.Buffs.Contains(NanoLine.DOT_LineA)
            || c.Buffs.Contains(NanoLine.DOT_LineB)
            || c.Buffs.Contains(NanoLine.DOTNanotechnicianStrainA)
            || c.Buffs.Contains(NanoLine.DOTAgentStrainA)
            || c.Buffs.Contains(NanoLine.DOTNanotechnicianStrainB)
            || c.Buffs.Contains(NanoLine.DOTStrainC)
            || c.Buffs.Contains(NanoLine.PainLanceDoT)
            || c.Buffs.Contains(NanoLine.MINIDoT))
            .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        public static bool Root_Snare_CleansePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            var target = DynelManager.Players
            .Where(c => c.Buffs.Contains(NanoLine.Root)
            || c.Buffs.Contains(NanoLine.Snare))
            .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        public static bool AOO_Trader_DOts_Init_CleansePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            var target = DynelManager.Players
            .Where(c => c.Buffs.Contains(NanoLine.AAODebuffs)
            || c.Buffs.Contains(NanoLine.TraderAAODrain)
            || c.Buffs.Contains(NanoLine.TraderSkillTransferTargetDebuff_Deprive)
            || c.Buffs.Contains(NanoLine.TraderSkillTransferTargetDebuff_Ransack)
            || c.Buffs.Contains(NanoLine.DOT_LineA)
            || c.Buffs.Contains(NanoLine.DOT_LineB)
            || c.Buffs.Contains(NanoLine.DOTNanotechnicianStrainA)
            || c.Buffs.Contains(NanoLine.DOTAgentStrainA)
            || c.Buffs.Contains(NanoLine.DOTNanotechnicianStrainB)
            || c.Buffs.Contains(NanoLine.DOTStrainC)
            || c.Buffs.Contains(NanoLine.PainLanceDoT)
            || c.Buffs.Contains(NanoLine.MINIDoT)
            || c.Buffs.Contains(NanoLine.InitiativeDebuffs))
            .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        public static bool SelfAOO_Trader_DOts_Init_CleansePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            var target = DynelManager.Players
            .Where(c => c.Buffs.Contains(NanoLine.AAODebuffs)
            || c.Buffs.Contains(NanoLine.TraderAAODrain)
            || c.Buffs.Contains(NanoLine.TraderSkillTransferTargetDebuff_Deprive)
            || c.Buffs.Contains(NanoLine.TraderSkillTransferTargetDebuff_Ransack)
            || c.Buffs.Contains(NanoLine.DOT_LineA)
            || c.Buffs.Contains(NanoLine.DOT_LineB)
            || c.Buffs.Contains(NanoLine.DOTNanotechnicianStrainA)
            || c.Buffs.Contains(NanoLine.DOTAgentStrainA)
            || c.Buffs.Contains(NanoLine.DOTNanotechnicianStrainB)
            || c.Buffs.Contains(NanoLine.DOTStrainC)
            || c.Buffs.Contains(NanoLine.PainLanceDoT)
            || c.Buffs.Contains(NanoLine.MINIDoT)
            || c.Buffs.Contains(NanoLine.InitiativeDebuffs))
            .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = false;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        public static bool Snare_CleansePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            var target = DynelManager.Players
            .Where(c => c.Buffs.Contains(NanoLine.Snare))
            .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        public static bool Root_CleansePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            var target = DynelManager.Players
            .Where(c => c.Buffs.Contains(NanoLine.Root))
            .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        public static bool Dot_CleansePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            var target = DynelManager.Players
            .Where(c => c.Buffs.Contains(NanoLine.DOT_LineA)
            || c.Buffs.Contains(NanoLine.DOT_LineB)
            || c.Buffs.Contains(NanoLine.DOTNanotechnicianStrainA)
            || c.Buffs.Contains(NanoLine.DOTAgentStrainA)
            || c.Buffs.Contains(NanoLine.DOTNanotechnicianStrainB)
            || c.Buffs.Contains(NanoLine.DOTStrainC)
            || c.Buffs.Contains(NanoLine.PainLanceDoT)
            || c.Buffs.Contains(NanoLine.MINIDoT))
            .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        public static bool NanoShutdown_CleansePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            var target = DynelManager.Players
            .Where(c => c.Buffs.Contains(NanoLine.NanoShutdownDebuff))
            .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        public delegate bool GenericPerkConditionProcessor(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget);

        public static bool CanPerkPet(Pet pet)
        {
            return pet.Type == PetType.Attack;
        }

        private static class RelevantEffects
        {
            public const int ThermalPrimerBuff = 209835;
            public const int SuppressivePrimerBuff = 209834;
        }
    }
}
