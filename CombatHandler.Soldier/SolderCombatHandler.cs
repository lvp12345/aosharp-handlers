using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;
using Character.State;
using System;
using AOSharp.Core.UI;

namespace Desu
{
    public class SoldCombathandler : GenericCombatHandler
    {
        public SoldCombathandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("UseSingleTaunt", false);
            settings.AddVariable("UseTauntTool", true);
            RegisterSettingsWindow("Soldier Handler", "SoldierSettingsView.xml");

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcSoldierGrazeJugularVein, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcSoldierFuriousAmmunition, LEProc);

            //Chat.WriteLine("" + DynelManager.LocalPlayer.GetStat(Stat.EquippedWeapons));

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ReflectShield).OrderByStackingOrder(), AugmentedMirrorShieldMKV);
            RegisterSpellProcessor(RelevantNanos.AdrenalineRush, AdrenalineRush);
            RegisterSpellProcessor(RelevantNanos.Distinctvictim, SingleTargetTaunt);//TODO: Generate soldier taunt line to support lower ql taunt use
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HPBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadowlandReflectBase).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierFullAutoBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierShotgunBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TotalFocus).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AssaultRifleBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierDamageBase).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AAOBuffs).OrderByStackingOrder(), TeamBuffAAONoMorph);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolTeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.BurstBuff).OrderByStackingOrder(), RangedTeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);

            //Items
            RegisterItemProcessor(RelevantItems.DreadlochEnduranceBoosterNanomageEdition, RelevantItems.DreadlochEnduranceBoosterNanomageEdition, EnduranceBooster, CombatActionPriority.High);

            if (TauntTools.CanUseTauntTool()) 
            {
                Item tauntTool = TauntTools.GetBestTauntTool();
                RegisterItemProcessor(tauntTool.LowId, tauntTool.HighId, TauntTool);
            }
        }

        private bool EnduranceBooster(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            // don't use if skill is locked (we will add this dynamically later)
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength))
                return false;

            // don't use if we're above 40%
            if (DynelManager.LocalPlayer.HealthPercent > 40)
                return false;

            // don't use if nothing is fighting us
            //if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0)
            //    return false;

            // don't use if we have another major absorb running
            // we could check remaining absorb stat to be slightly more effective
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon))
                return false;

            return true;
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseSingleTaunt") || !DynelManager.LocalPlayer.IsAttacking || fightingTarget == null || DynelManager.LocalPlayer.Nano < spell.Cost)
                return false;

            return true;
        }

        private bool AdrenalineRush(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //if (!DynelManager.LocalPlayer.IsAttacking || fightingtarget == null)
            //    return false;

            if (DynelManager.LocalPlayer.HealthPercent <= 30)
                return true;

            return false;
        }

        private bool AugmentedMirrorShieldMKV(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking || fightingtarget == null)
                return false;

            if (DynelManager.LocalPlayer.HealthPercent <= 75 && spell.IsReady)
                return true;

            return false;
        }

        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return TeamBuffInitSol(spell, fightingTarget, ref actionTarget, hasBuffCheck: target => HasBuffNanoLine(NanoLine.InitiativeBuffs, target));
        }

        protected bool TeamBuffAAONoMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return TeamBuff(spell, fightingTarget, ref actionTarget, hasBuffCheck: target => HasBuffNanoLine(NanoLine.AAOBuffs, target) || HasBuffNanoLine(NanoLine.Polymorph, target));
        }

        private static class RelevantNanos
        {
            public const int AdrenalineRush = 301897;
            public const int Distinctvictim = 223205;
        }

        private static class RelevantItems
        {
            public const int DreadlochEnduranceBooster = 267168;
            public const int DreadlochEnduranceBoosterNanomageEdition = 267167;
        }
    }
}
