using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using CombatHandler.Generic;
using Character.State;
using AOSharp.Core.UI;

namespace Desu
{
    public class SoldCombathandler : GenericCombatHandler
    {
        public SoldCombathandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("UseSingleTaunt", false);
            settings.AddVariable("UseTauntTool", false);

            RegisterSettingsWindow("Soldier Handler", "SoldierSettingsView.xml");

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcSoldierGrazeJugularVein, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcSoldierFuriousAmmunition, LEProc);

            // Leave in to get the ID of SMG and Shotgun
            Chat.WriteLine("" + DynelManager.LocalPlayer.GetStat(Stat.EquippedWeapons));

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ReflectShield).OrderByStackingOrder(), AugmentedMirrorShieldMKV);
            RegisterSpellProcessor(RelevantNanos.SolDrainHeal, SolDrainHeal);
            RegisterSpellProcessor(RelevantNanos.TauntBuffs, SingleTargetTaunt);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(), TeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HPBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadowlandReflectBase).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierFullAutoBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierShotgunBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TotalFocus).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(RelevantNanos.ArBuffs, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.HeavyComp, HeavyCompBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierDamageBase).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AAOBuffs).OrderByStackingOrder(), TeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.BurstBuff).OrderByStackingOrder(), BurstBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);

            // Needs work for 2nd tanking Abmouth and Ayjous
            if (TauntTools.CanUseTauntTool()) 
            {
                Item tauntTool = TauntTools.GetBestTauntTool();
                RegisterItemProcessor(tauntTool.LowId, tauntTool.HighId, TauntTool);
            }
        }

        #region Instanced Logic

        private bool HeavyCompBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Smg, CharacterWieldedWeapon.AssaultRifle, CharacterWieldedWeapon.PistolAndAssaultRifle, CharacterWieldedWeapon.Bandaid);
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseSingleTaunt") || !DynelManager.LocalPlayer.IsAttacking || fightingTarget == null)
                return false;

            return true;
        }

        private bool SolDrainHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking || fightingTarget == null)
                return false;

            if (DynelManager.LocalPlayer.HealthPercent <= 40)
                return true;

            return false;
        }

        private bool AugmentedMirrorShieldMKV(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking || fightingtarget == null)
                return false;

            if (DynelManager.LocalPlayer.HealthPercent <= 75)
                return true;

            return false;
        }

        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffInitSol(spell, fightingTarget, ref actionTarget);
        }

        protected bool TeamBuffAAONoMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Misc

        private static class RelevantNanos
        {
            public static readonly int[] SolDrainHeal = { 29241, 301897 };
            public static readonly int[] TauntBuffs = { 223209, 223207, 223205, 223203, 223201, 29242, 100207,
            29218, 100205, 100206, 100208, 29228};
            public const int HeavyComp = 269482;
            public static readonly int[] ArBuffs = { 275027, 203119, 29220, 203121 };
        }

        private static class RelevantItems
        {
            public const int DreadlochEnduranceBooster = 267168;
            public const int DreadlochEnduranceBoosterNanomageEdition = 267167;
        }

        #endregion
    }
}
