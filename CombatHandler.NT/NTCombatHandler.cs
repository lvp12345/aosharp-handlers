using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Desu
{
    public class NTCombatHandler : GenericCombatHandler
    {
        public NTCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("UseAIDot", true);
            settings.AddVariable("UseAoeBlind", false);
            RegisterSettingsWindow("Nano-Technician Handler", "NTSettingsView.xml");

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NullitySphereNano).OrderByStackingOrder(), NullitySphere, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.NanobotAegis, NanobotAegis);
            RegisterSpellProcessor(RelevantNanos.IzgimmersWealth, IzgimmersWealth);

            RegisterSpellProcessor(RelevantNanos.NanobotShelter, GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Psy_IntBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDamageMultiplierBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NFRangeBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MatCreaBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), GenericBuffExcludeInnerSanctum);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Fortify).OrderByStackingOrder(), GenericBuff);

            if(Spell.Find(RelevantNanos.SuperiorFleetingImmunity, out Spell immunity))
            {
                RegisterSpellProcessor(immunity, GenericBuff);
            }

            //Team buffs
            RegisterSpellProcessor(RelevantNanos.AbsortAcTargetBuffs, TeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoOverTime_LineA).OrderByStackingOrder(), CheckBeforeCast);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NPCostBuff).OrderByStackingOrder(), CheckBeforeCast);

            //Nukes and DoTs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTNanotechnicianStrainA).OrderByStackingOrder(), AiDotNuke);
            RegisterSpellProcessor(RelevantNanos.Garuk, SingleTargetNuke);
            RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke);

            //Debuffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AAODebuffs).OrderByStackingOrder(), SingleBlind);
            RegisterSpellProcessor(RelevantNanos.AoeBlinds, AoeBlind);
        }

        private bool AoeBlind(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseAoeBlind") || fightingTarget == null)
            {
                return false;
            }

            return !fightingTarget.Buffs.Contains(NanoLine.AAODebuffs);
        }

        private bool SingleBlind(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("UseAoeBlind") || fightingTarget == null)
            {
                return false;
            }

            return !fightingTarget.Buffs.Contains(NanoLine.AAODebuffs);
        }

        private bool NanobotAegis(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = false;
            return DynelManager.LocalPlayer.HealthPercent < 50 && !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.NullitySphereNano);
        }

        private bool CheckBeforeCast(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell))
            {
                return false;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => HasNCU(spell, c))
                    .Where(c => !HasBuff(spell, c))
                    .Where(c => c.Profession != Profession.Keeper)
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }
        private bool NanoHOT(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = true;
            return DynelManager.LocalPlayer.HealthPercent < 50 && !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.NanoOverTime_LineA);
        }

        private bool NullitySphere(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = false;
            return DynelManager.LocalPlayer.HealthPercent < 50 && !DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.NanobotAegis);
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null)
                return false;

            return true;
        }

        private bool IzgimmersWealth(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = false;

            if (fightingTarget == null)
                return false;

            if (DynelManager.LocalPlayer.MissingNano < 20000 && DynelManager.LocalPlayer.NanoPercent > 5)
                return false;

            return true;
        }

        private bool AiDotNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseAIDot"))
                return false;

            if (fightingTarget == null)
                return false;

            if (fightingTarget.Health < 80000)
                return false;

            if (fightingTarget.Buffs.Find(spell.Identity.Instance, out Buff buff) && buff.RemainingTime > 5)
                return false;

            return true;
        }

        private static class RelevantNanos
        {
            public const int NanobotAegis = 302074;
            public const int IzgimmersWealth = 275024;
            public const int IzgimmersUltimatum = 218168;
            public const int Garuk = 275692;
            public const int SuperiorFleetingImmunity = 273386;
            public static readonly Spell[] AbsortAcTargetBuffs = Spell.GetSpellsForNanoline(NanoLine.AbsorbACBuff).OrderByStackingOrder().Where(spell => spell.Identity.Instance != SuperiorFleetingImmunity).ToArray();
            public static readonly int[] AoeBlinds = { 83959, 83960, 83961, 83962, 83963, 83964 };
            public static readonly int[] SingleTargetNukes = { 218168, 218164, 218162, 218160, 218158, 218156, 218154, 218152, 218150, 
                218148, 218146, 218144, 218142, 218140, 218138, 218136, 269473, 218134, 201935, 202262, 201933, 218132, 28618, 218124, 218130, 
                218122, 218120, 218128, 218118, 218126, 45226, 45192, 28619, 45230, 28623, 28604, 28616, 218116, 28597, 45210, 45236, 45197, 
                45233, 45247, 45199, 45235, 45234, 218114, 45258, 45217, 28600, 45198, 28613, 45919, 45195, 45225, 45260, 45891, 45254, 45890, 
                45213, 218112, 45215, 45915, 218104, 45252, 45214, 45251, 45929, 45220, 45920, 45222, 218102, 28598, 45911, 45237, 45216, 
                218110, 45913, 45901, 45212, 45206, 45912, 45883, 45245, 45140, 45904, 45218, 28626, 218108, 45261, 218100, 45909, 45203, 
                45228, 45903, 45200, 45939, 28592, 45242, 218098, 218106, 45885, 45926, 45241, 44538, 45908, 45250, 45934, 45138, 45932, 
                28632, 45205, 28609, 45209, 45246, 45935, 45921, 45227, 45207, 45942, 45191, 45924, 218096, 28610, 45914, 45208, 45893, 
                28621, 45211, 45916, 45933, 218094, 45240, 45259, 45941, 45910, 45253, 28614, 218092, 45221, 45204, 28634, 45196, 45886, 
                45201, 45928, 45193, 45323, 45244, 45889, 45895, 28605, 45219, 45223, 45938, 28628, 45232, 45248, 45898, 45202, 45923, 
                45229, 45907, 45139, 45887, 45231, 45882, 28627, 45936, 45194, 28639, 45243, 45931, 28630, 45137, 28607, 45257, 45880, 
                45256, 45249, 45888, 45255, 45881, 42543, 45927, 45902, 42540, 42541, 45899, 45905, 28611, 45897, 28601, 42542, 28608, 
                45918, 42539, 45892, 45930, 45879, 45896, 28612 };

            //Buffs
            public static readonly int[] NanobotShelter = { 273388, 263265 };
            public static readonly int CompositeAttribute = 223372;
            public static readonly int CompositeNano = 223380;
        }
    }
}
