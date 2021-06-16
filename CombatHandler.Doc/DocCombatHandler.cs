using AOSharp.Common.GameData;
using AOSharp.Core;
using Character.State;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Desu
{
    class DocCombatHandler : GenericCombatHandler
    {
        public DocCombatHandler(String pluginDir) : base(pluginDir)
        {
            settings.AddVariable("UseInitDebuff", false);
            settings.AddVariable("UseInitDebuffOnOthers", false);
            settings.AddVariable("UseDotA", false);
            settings.AddVariable("UseDotB", false);
            settings.AddVariable("UseDotC", false);
            settings.AddVariable("UseCH", true);
            settings.AddVariable("UseHOT", true);
            settings.AddVariable("UseSLHeal", true);
            settings.AddVariable("UseRKHeal", false);
            settings.AddVariable("UseHPBuff", true);
            settings.AddVariable("UseShortHPBuff", true);
            settings.AddVariable("SpamCH", false);
            RegisterSettingsWindow("Doctor Handler", "DoctorSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcDoctorAstringent, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorMuscleMemory, LEProc, CombatActionPriority.Low);

            RegisterSpellProcessor(RelevantNanos.ALPHA_AND_OMEGA, SpamCH, CombatActionPriority.High);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CompleteHealingLine).OrderByStackingOrder(), CompleteHealing, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.SL_HEALS, ShadowlandsHeal, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.RK_HEALS, RubiKaHeal, CombatActionPriority.High);

            //Team Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealDeltaBuff).OrderByStackingOrder(), TeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), TeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceBuffs).OrderByStackingOrder(), NanoResistanceBuff);
            
            RegisterSpellProcessor(RelevantNanos.HP_BUFFS, TeamHPBuff);
            
            if(HasNano(RelevantNanos.IMPROVED_LC))
            {
                RegisterSpellProcessor(RelevantNanos.IMPROVED_LC, TeamShortHPBuff);
            } 
            else
            {
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DoctorShortHPBuffs).OrderByStackingOrder(), TeamShortHPBuff);
            }

            if(HasNano(RelevantNanos.TEAM_DEATHLESS_BLESSING))
            {
                RegisterSpellProcessor(RelevantNanos.TEAM_DEATHLESS_BLESSING, TeamDeathlessBlessing);
            }
            else
            {
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealOverTime).OrderByStackingOrder(), TeamHOTBuff);
            }

            //This needs work 
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder(), InitDebuffTarget);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder(), InitDebuffOthers, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineA).OrderByStackingOrder(), DOTADebuffTarget);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineB).OrderByStackingOrder(), DOTBDebuffTarget);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTStrainC).OrderByStackingOrder(), DOTCDebuffTarget);
        }

        private bool InitDebuffOthers(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffOthersInCombat("UseInitDebuffOnOthers", spell, fightingTarget, ref actionTarget);
        }

        private bool NanoResistanceBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return TeamBuff(spell, fightingTarget, ref actionTarget, hasBuffCheck: target => HasBuffNanoLine(NanoLine.NanoResistanceBuffs, target) || HasBuffNanoLine(NanoLine.Rage, target), CharacterWeaponType.UNAVAILABLE);
        }

        private bool SpamCH(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(IsSettingEnabled("SpamCH"))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }
            return false;
        }

        private bool TeamHOTBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return HealOverTimeTeamBuff("UseHOT", spell, fightingTarget, ref actionTarget);
        }

        private bool TeamHPBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseHPBuff") || HasBuffNanoLine(NanoLine.DoctorHPBuffs, DynelManager.LocalPlayer))
            {
                return false;
            }
            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool TeamShortHPBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseShortHPBuff") || HasBuffNanoLine(NanoLine.DoctorShortHPBuffs, DynelManager.LocalPlayer))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool InitDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return DocToggledDebuffTarget("UseInitDebuff", spell, fightingTarget, ref actionTarget);
        }

        private bool DOTADebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return DocToggledDebuffTarget("UseDotA", spell, fightingTarget, ref actionTarget);
        }

        private bool DOTBDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return DocToggledDebuffTarget("UseDotB", spell, fightingTarget, ref actionTarget);
        }

        private bool DOTCDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return DocToggledDebuffTarget("UseDotC", spell, fightingTarget, ref actionTarget);
        }

        private bool TeamDeathlessBlessing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(fightingTarget == null || !IsSettingEnabled("UseHOT") || DynelManager.LocalPlayer.Buffs.Contains(NanoLine.HealOverTime))
            {
                return false;
            }

            return true;
        }

        private bool DocToggledDebuffTarget(String settingName, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //Check if you are low hp dont debuff
            if (DynelManager.LocalPlayer.HealthPercent <= 30)
            {
                return false;
            }

            //Check if we're in a team and someone is low hp , dont debuff
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent < 30)
                    .OrderByDescending(c => c.GetStat(Stat.NumFightingOpponents))
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    return false;
                }
            }

            return ToggledDebuffTarget(settingName, spell, fightingTarget, ref actionTarget);
        }

        private bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(!IsSettingEnabled("UseCH"))
            {
                return false;
            }
            return FindMemberWithHealthBelow(50, ref actionTarget);
        }

        private bool ShadowlandsHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(!IsSettingEnabled("UseSLHeal"))
            {
                return false;
            }
            return FindMemberWithHealthBelow(80, ref actionTarget);
        }

        private bool RubiKaHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseRKHeal"))
            {
                return false;
            }
            return FindMemberWithHealthBelow(80, ref actionTarget);
        }

        private bool FindMemberWithHealthBelow(int healthPercentTreshold, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            // Prioritize keeping ourself alive
            if (DynelManager.LocalPlayer.HealthPercent <= healthPercentTreshold)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= healthPercentTreshold)
                    .OrderByDescending(c => c.GetStat(Stat.NumFightingOpponents))
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    actionTarget.Target = dyingTeamMember;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }
            return false;
        }

        private bool HasNano(int nanoId)
        {
            return Spell.Find(nanoId, out Spell spell);
        }


        private static class RelevantNanos
        {
            public const int TEAM_DEATHLESS_BLESSING = 269455;
            public const int OMNI_MED = 95709;
            public const int IMPROVED_LC = 275011;
            public const int IMPROVED_CH = 270747;
            public const int BODILY_INV = 223299;
            public const int UBT = 99577;
            public const int UBT_MONSTER = 301844;
            public const int UBT_HUMAN = 301843;
            public const int CONTINUOUS_RECONSTRUCTION = 222824;
            public const int IMPROVED_INSTINCTIVE_CONTROL = 222856;
            public const int IMPROVED_NANO_REPULSOR = 222823;
            public const int ALPHA_AND_OMEGA = 42409;
            //HP buffs don't have a nano line and cannot be ordered dynamically. Need to hardcode list and order.
            public static int[] HP_BUFFS = new[] { 95709, 28662, 95720, 95712, 95710, 95711, 28649, 95713, 28660, 95715, 95714, 95718, 95716, 95717, 95719, 42397 };
            //RK Heals don't have a nano line and cannot be ordered dynamically. Need to hardcode list and order.
            public static int[] RK_HEALS = new[] 
            { 43885, 43887, 43890, 43884 , 43808 , 43888 , 43889 ,43883, 43811, 43809, 43810, 28645, 43816, 43817, 43825, 43815, 
                43814, 43821, 43820, 28648, 43812, 43824, 43822, 43819, 43818, 43823, 28677, 43813, 43826, 43838, 43835, 
                28672, 43836, 28676, 43827, 43834, 28681, 43837, 43833, 43830, 43828, 28654, 43831, 43829, 43832, 28665 };
            //SL Heals have a broken stacking order and cannot be ordered dynamically. Need to hardcode order.
            public static int[] SL_HEALS = new[] { 223299, 223297, 223295, 223293, 223291, 223289, 223287, 223285, 223281, 43878, 43881, 43886 };
        }
    }
}