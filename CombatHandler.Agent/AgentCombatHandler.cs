using AOSharp.Common.GameData;
using AOSharp.Core;
using CombatHandler.Generic;

namespace CombatHandler.Agent
{
    public class AgentCombatHandler : GenericCombatHandler
    {
        public AgentCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("UseDotStrainA", false);
            settings.AddVariable("UseEvasionDebuff", false);
            settings.AddVariable("LEProcSelection", (int)LEProcSelection.LASER_AIM);
            settings.AddVariable("ProcSelection", (int)ProcSelection.DOT);

            RegisterSettingsWindow("Agent Handler", "AgentSettingsView.xml");

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
            RegisterSpellProcessor(RelevantNanos.DetauntProcs, DetauntProc);
            RegisterSpellProcessor(RelevantNanos.DotProcs, DotProc);

            //Team Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(), TeamBuff);
            RegisterSpellProcessor(RelevantNanos.TeamCritBuffs, TeamBuff);

            //Debuffs/DoTs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTAgentStrainA).OrderByStackingOrder(), DotStrainA);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EvasionDebuffs_Agent), EvasionDebuff);

        }
        
        private bool RifleBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.AssassinsAimedShot, out Buff buff))
            {
                return false;
            }
            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool LaserAim(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(LEProcSelection.LASER_AIM != (LEProcSelection)settings["LEProcSelection"].AsInt32())
            {
                return false;
            }
            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool NotumChargedRounds(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (LEProcSelection.NOTUM_CHARGED_ROUNDS != (LEProcSelection)settings["LEProcSelection"].AsInt32())
            {
                return false;
            }
            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DetauntProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        { 
            if(ProcSelection.DETAUNT != (ProcSelection)settings["ProcSelection"].AsInt32())
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DotProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcSelection.DOT != (ProcSelection)settings["ProcSelection"].AsInt32())
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

            return true;
        }

        private bool DotStrainA(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("UseDotStrainA", spell, fightingTarget, ref actionTarget);
        }

        private bool EvasionDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("UseEvasionDebuff", spell, fightingTarget, ref actionTarget);
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            if (ProcSelection.DETAUNT != (ProcSelection)settings["ProcSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.DetauntProcs);
            }
            if (ProcSelection.DOT != (ProcSelection)settings["ProcSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.DotProcs);
            }
        }

        private static class RelevantNanos
        {
            public static int[] DetauntProcs = { 226437, 226435, 226433, 226431, 226429, 226427 };
            public static int[] DotProcs = { 226425, 226423, 226421, 226419, 226417, 226415, 226413, 226410 };
            public static int[] TeamCritBuffs = { 160791, 160789, 160787 };
            public static int AssassinsAimedShot = 275007;
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
