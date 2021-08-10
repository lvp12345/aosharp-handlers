using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
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
            settings.AddVariable("InitDebuff", false);
            settings.AddVariable("OSInitDebuff", false);

            settings.AddVariable("DotA", false);
            settings.AddVariable("DotB", false);
            settings.AddVariable("DotC", false);

            settings.AddVariable("IndividualHOT", false);
            settings.AddVariable("ShortHPBuff", false);

            settings.AddVariable("CH", true);

            settings.AddVariable("Heal", true);
            settings.AddVariable("OSHeal", false);

            settings.AddVariable("LockCH", false);

            RegisterSettingsWindow("Doctor Handler", "DoctorSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcDoctorAstringent, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorMuscleMemory, LEProc, CombatActionPriority.Low);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CompleteHealingLine).OrderByStackingOrder(), CompleteHealing, CombatActionPriority.High);

            RegisterSpellProcessor(RelevantNanos.HEALS, Healing, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.IMPROVED_LC, TeamHealing, CombatActionPriority.High);

            RegisterSpellProcessor(RelevantNanos.ALPHA_AND_OMEGA, LockCH, CombatActionPriority.High);

            //Team Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealDeltaBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceBuffs).OrderByStackingOrder(), NanoResistanceBuff);
            
            RegisterSpellProcessor(RelevantNanos.HP_BUFFS, TeamHPBuff);
            
            if(HasNano(RelevantNanos.IMPROVED_LC))
            {
                RegisterSpellProcessor(RelevantNanos.IMPROVED_LC, LifeChanneler);
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

            //Debuffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder(), InitDebuffTarget);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder(), OSInitDebuff, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineA).OrderByStackingOrder(), DOTADebuffTarget);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineB).OrderByStackingOrder(), DOTBDebuffTarget);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTStrainC).OrderByStackingOrder(), DOTCDebuffTarget);
        }

        #region Healing

        private bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("CH"))
            {
                return false;
            }

            return FindMemberWithHealthBelow(50, ref actionTarget);
        }

        private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Heal"))
            {
                return false;
            }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam() && HasNano(RelevantNanos.IMPROVED_LC))
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 80)
                    .Where(c => c.HealthPercent >= 50)
                    .ToList();

                if (dyingTeamMember.Count < 4)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal"))
            {
                return false;
            }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam() && IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal") && HasNano(RelevantNanos.IMPROVED_LC))
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 80)
                    .Where(c => c.HealthPercent >= 50)
                    .ToList();

                if (dyingTeamMember.Count >= 4)
                {
                    return false;
                }
                else
                {
                    return FindMemberWithHealthBelow(85, ref actionTarget);
                }
            }
            if (IsSettingEnabled("OSHeal") && !IsSettingEnabled("Heal"))
            {
                return FindPlayerWithHealthBelow(85, ref actionTarget);
            }
            else
            {
                return FindMemberWithHealthBelow(85, ref actionTarget);
            }
        }

        #endregion

        #region Buffs

        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffInitDoc(spell, fightingTarget, ref actionTarget);
        }

        private bool LifeChanneler(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HasBuffNanoLine(NanoLine.DoctorShortHPBuffs, DynelManager.LocalPlayer))
                return false;

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool NanoResistanceBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }



        private bool TeamHOTBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return HealOverTimeBuff("IndividualHOT", spell, fightingTarget, ref actionTarget);
        }

        private bool TeamHPBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HasBuffNanoLine(NanoLine.DoctorHPBuffs, DynelManager.LocalPlayer))
                return false;

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool TeamShortHPBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("ShortHPBuff"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool TeamDeathlessBlessing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SpellChecksPlayer(spell))
                return false;

            return true;
        }

        #endregion

        #region Debuffs

        private bool DocToggledDebuffTarget(String settingName, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SomeoneNeedsHealing())
                return false;

            return ToggledDebuffTarget(settingName, spell, fightingTarget, ref actionTarget);
        }

        private bool InitDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SomeoneNeedsHealing())
                return false;

            return DocToggledDebuffTarget("InitDebuff", spell, fightingTarget, ref actionTarget);
        }

        private bool DOTADebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SomeoneNeedsHealing())
                return false;

            return DocToggledDebuffTarget("DotA", spell, fightingTarget, ref actionTarget);
        }

        private bool DOTBDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SomeoneNeedsHealing())
                return false;

            return DocToggledDebuffTarget("DotB", spell, fightingTarget, ref actionTarget);
        }

        private bool DOTCDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SomeoneNeedsHealing())
                return false;

            return DocToggledDebuffTarget("DotC", spell, fightingTarget, ref actionTarget);
        }

        private bool OSInitDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SomeoneNeedsHealing())
                return false;

            return ToggledDebuffOthersInCombat("OSInitDebuff", spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Misc

        private bool LockCH(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("LockCH"))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
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
            //public static int[] RK_HEALS = new[] 
            //{ 43885, 43887, 43890, 43884 , 43808 , 43888 , 43889 ,43883, 43811, 43809, 43810, 28645, 43816, 43817, 43825, 43815, 
            //    43814, 43821, 43820, 28648, 43812, 43824, 43822, 43819, 43818, 43823, 28677, 43813, 43826, 43838, 43835, 
            //    28672, 43836, 28676, 43827, 43834, 28681, 43837, 43833, 43830, 43828, 28654, 43831, 43829, 43832, 28665 };
            //SL Heals have a broken stacking order and cannot be ordered dynamically. Need to hardcode order.
            public static int[] HEALS = new[] { 223299, 223297, 223295, 223293, 223291, 223289, 223287, 223285, 223281, 43878, 43881, 43886, 43885,
                43887, 43890, 43884, 43808, 43888, 43889, 43883, 43811, 43809, 43810, 28645, 43816, 43817, 43825, 43815,
                43814, 43821, 43820, 28648, 43812, 43824, 43822, 43819, 43818, 43823, 28677, 43813, 43826, 43838, 43835,
                28672, 43836, 28676, 43827, 43834, 28681, 43837, 43833, 43830, 43828, 28654, 43831, 43829, 43832, 28665 };
        }

        #endregion
    }
}