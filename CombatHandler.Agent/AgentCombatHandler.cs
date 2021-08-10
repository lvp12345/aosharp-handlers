using AOSharp.Common.GameData;
using AOSharp.Core;
using CombatHandler.Generic;
using Character.State;
using AOSharp.Core.UI;
using System.Linq;

namespace CombatHandler.Agent
{
    public class AgentCombatHandler : GenericCombatHandler
    {
        public AgentCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("DotStrainA", false);
            settings.AddVariable("EvasionDebuff", false);
            settings.AddVariable("LazerAim", false);
            settings.AddVariable("NotumChargedRounds", false);
            settings.AddVariable("Damage", false);
            settings.AddVariable("Detaunt", false);
            settings.AddVariable("Healing", false);

            settings.AddVariable("LEProcSelection", (int)LEProcSelection.LASER_AIM);
            settings.AddVariable("ProcSelection", (int)ProcSelection.DOT);

            RegisterSettingsWindow("Agent Handler", "AgentSettingsView.xml");

            //Chat.WriteLine("" + DynelManager.LocalPlayer.GetStat(Stat.EquippedWeapons));

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcAgentGrimReaper, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcAgentLaserAim, LaserAim);
            RegisterPerkProcessor(PerkHash.LEProcAgentNotumChargedRounds, NotumChargedRounds);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AgilityBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AimedShotBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ConcealmentBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalIncreaseBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ExecutionerBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RifleBuffs).OrderByStackingOrder(), RifleBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AgentProcBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ConcentrationCriticalLine).OrderByStackingOrder(), Concentration);

            RegisterSpellProcessor(RelevantNanos.SL_HEALS, Healing, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.FalseProfDoc, FalseProfDoc);

            RegisterSpellProcessor(RelevantNanos.DetauntProcs, DetauntProc);
            RegisterSpellProcessor(RelevantNanos.DotProcs, DotProc);

            //Team Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(), DamageTeamBuff);
            RegisterSpellProcessor(RelevantNanos.TeamCritBuffs, TeamBuffCrit);

            //Debuffs/DoTs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTAgentStrainA).OrderByStackingOrder(), DotStrainA);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EvasionDebuffs_Agent), EvasionDebuff);

        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Healing"))
            {
                return false;
            }

            return FindMemberWithHealthBelow(90, ref actionTarget);
        }

        private bool FalseProfDoc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Healing"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool RifleBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.AssassinsAimedShot, out Buff AAS))
            {
                return false;
            }

            if (DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.SteadyNerves, out Buff SN))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool LaserAim(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("LazerAim"))
            {
                return false;
            }

            if (IsSettingEnabled("NotumChargedRounds") && IsSettingEnabled("LazerAim"))
            {
                return false;
            }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool NotumChargedRounds(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("NotumChargedRounds"))
            {
                return false;
            }

            if (IsSettingEnabled("NotumChargedRounds") && IsSettingEnabled("LazerAim"))
            {
                return false;
            }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DetauntProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Detaunt"))
            {
                return false;
            }

            if (!IsSettingEnabled("Detaunt") && !IsSettingEnabled("Damage") || (IsSettingEnabled("Detaunt") && IsSettingEnabled("Damage")))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DotProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Damage"))
            {
                return false;
            }

            if (!IsSettingEnabled("Detaunt") && !IsSettingEnabled("Damage") || (IsSettingEnabled("Detaunt") && IsSettingEnabled("Damage")))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool Concentration(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(fightingTarget == null)
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool TeamBuffCrit(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool DamageTeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DotStrainA(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("DotStrainA", spell, fightingTarget, ref actionTarget);
        }

        private bool EvasionDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("EvasionDebuff", spell, fightingTarget, ref actionTarget);
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

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            if (IsSettingEnabled("Damage") && !IsSettingEnabled("Detaunt"))
            {
                CancelBuffs(RelevantNanos.DetauntProcs);
            }
            if (IsSettingEnabled("Detaunt") && !IsSettingEnabled("Damage"))
            {
                CancelBuffs(RelevantNanos.DotProcs);
            }
        }

        private static class RelevantNanos
        {
            public static int[] DetauntProcs = { 226437, 226435, 226433, 226431, 226429, 226427 };
            public static int[] FalseProfDoc = { 117210, 117221, 32033 };
            public static int[] DotProcs = { 226425, 226423, 226421, 226419, 226417, 226415, 226413, 226410 };
            public static int[] TeamCritBuffs = { 160791, 160789, 160787 };
            public static int AssassinsAimedShot = 275007;
            public static int SteadyNerves = 160795;
            public static int[] RK_HEALS = new[]
            { 43885, 43887, 43890, 43884 , 43808 , 43888 , 43889 ,43883, 43811, 43809, 43810, 28645, 43816, 43817, 43825, 43815,
                43814, 43821, 43820, 28648, 43812, 43824, 43822, 43819, 43818, 43823, 28677, 43813, 43826, 43838, 43835,
                28672, 43836, 28676, 43827, 43834, 28681, 43837, 43833, 43830, 43828, 28654, 43831, 43829, 43832, 28665 };
            public static int[] SL_HEALS = new[] { 223299, 223297, 223295, 223293, 223291, 223289, 223287, 223285, 223281, 43878, 43881, 43886 };
        }
    }

    public enum LEProcSelection
    {
        LASER_AIM, NOTUM_CHARGED_ROUNDS
    }

    public enum ProcSelection
    {
        DETAUNT, DOT
    }
}
