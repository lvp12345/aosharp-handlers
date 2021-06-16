using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Desu
{
    class EnfCombatHandler : GenericCombatHandler
    {
        public EnfCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("UseSingleTaunt", false);
            settings.AddVariable("UseAOETaunt", true);
            settings.AddVariable("UseTauntTool", true);
            settings.AddVariable("IsOST", false);
            settings.AddVariable("SpamMongo", false);
            RegisterSettingsWindow("Enforcer Handler", "EnforcerSettingsView.xml");

            //-------------LE procs-------------
            RegisterPerkProcessor(PerkHash.LEProcEnforcerVortexOfHate, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerInspireIre, LEProc, CombatActionPriority.Low);

            //Spells (Im not sure the spell lines are up to date to support the full line of SL mongos)
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HPBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MongoBuff).OrderByStackingOrder(), AoeTaunt);
            RegisterSpellProcessor(RelevantNanos.SingleTargetTaunt, SingleTargetTaunt, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageChangeBuffs).OrderByStackingOrder(), DamageChangeBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AbsorbACBuff).OrderByStackingOrder(), Fortify);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EnforcerTauntProcs).OrderByStackingOrder(), GenericBuff);

            //Team buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), MeleeTeamBuff);
            RegisterSpellProcessor(RelevantNanos.TargetedDamageShields, TeamBuff);
            RegisterSpellProcessor(RelevantNanos.TargetedHpBuff, TeamBuff);
            RegisterSpellProcessor(RelevantNanos.FOCUSED_ANGER, TeamBuff);

            if (TauntTools.CanUseTauntTool())
            {
                Item tauntTool = TauntTools.GetBestTauntTool();
                RegisterItemProcessor(tauntTool.LowId, tauntTool.HighId, TauntTool);
            }
        }

        private bool DamageChangeBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(HasBuffNanoLine(NanoLine.DamageChangeBuffs, DynelManager.LocalPlayer))
            {
                return false;
            }
            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseSingleTaunt") || fightingTarget == null)
            {
                return false;
            }

            //If our target has a different target than us we need to make sure we taunt
            if (IsNotFightingMe(fightingTarget))
            {
                return true;
            }

            if (DynelManager.LocalPlayer.NanoPercent < 30)
            {
                return false;
            }

            return true;
        }

        private bool Fortify(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(DynelManager.LocalPlayer.Buffs.Any(Buff => Buff.Identity.Instance == RelevantNanos.BIO_COCOON_BUFF))
            {
                return false;
            }

            if (IsSettingEnabled("IsOST") && DynelManager.LocalPlayer.NanoPercent > 30)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool AoeTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(IsSettingEnabled("IsOST") || IsSettingEnabled("SpamMongo"))
            {
                return true;
            }

            if (!IsSettingEnabled("UseAOETaunt"))
            {
                return false;
            }

            if (fightingTarget == null)
            {
                return false;
            }

            //If our target has a different target than us we need to make sure we taunt
            if (fightingTarget.FightingTarget != null && (fightingTarget.FightingTarget.Identity != DynelManager.LocalPlayer.Identity))
            {
                return true;
            }

            //If there is a target in range, that is not fighting us, we need to make sure we taunt
            if(DynelManager.Characters.Where(ShouldBeTaunted).Any(IsNotFightingMe))
            {
                return true;
            }

            //Check if we still have the mongo hot 
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if ((buff.Name == spell.Name && buff.RemainingTime > 5))
                {
                    return false;
                }
            }

            //Make sure we have plenty of nano for spamming mongo
            if (DynelManager.LocalPlayer.NanoPercent < 30)
            {
                return false;
            }

            return true;
        }

        private bool ShouldBeTaunted(SimpleChar target)
        {
            return !target.IsPlayer && !target.IsPet && target.IsValid && target.IsInLineOfSight;
        }

        private bool IsNotFightingMe(SimpleChar target)
        {
            return target.IsAttacking && target.FightingTarget.Identity != DynelManager.LocalPlayer.Identity;
        }

        private static class RelevantNanos
        {
            public static readonly int[] SingleTargetTaunt = { 275014, 223123, 223121, 223119, 223117, 223115, 100209, 100210, 100212, 100211, 100213 };
            public static readonly int[] TargetedHpBuff = { 273629, 95708, 95700, 95701, 95702, 95704, 95706, 95707 };
            public static readonly Spell[] TargetedDamageShields = Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder().Where(spell => spell.Identity.Instance != ICE_BURN).ToArray();
            public const int MONGO_KRAKEN = 273322;
            public const int MONGO_DEMOLISH = 270786;
            public const int FOCUSED_ANGER = 29641;
            public const int IMPROVED_ESSENCE_OF_BEHEMOTH = 273629;
            public const int CORUSCATING_SCREEN = 55751;
            public const int ICE_BURN = 269460;
            public const int BIO_COCOON_BUFF = 209802;
        }
    }
}