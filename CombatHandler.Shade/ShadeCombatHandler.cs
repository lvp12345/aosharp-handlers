using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using CombatHandler.Generic;

namespace Desu
{
    public class ShadeCombatHandler : GenericCombatHandler
    {
        private const int MissingHealthCombatAbortPercentage = 30;

        public ShadeCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("UseDrainNanoForDps", true);
            settings.AddVariable("UseSpiritSiphon", true);
            settings.AddVariable("UseFasterThanYourShadow", false);
            settings.AddVariable("ProcSelection", 0);
            RegisterSettingsWindow("Shade Handler", "ShadeSettingsView.xml");

            RegisterPerkProcessor(PerkHash.LEProcShadeSiphonBeing, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcShadeBlackheart, LEProc);

            //Perks
            RelevantPerks.SpiritPhylactery.ForEach(p => RegisterPerkProcessor(p, SpiritPhylacteryPerk));
            RelevantPerks.TotemicRites.ForEach(p => RegisterPerkProcessor(p, TotemicRitesPerk));
            RelevantPerks.PiercingMastery.ForEach(p => RegisterPerkProcessor(p, PiercingMasteryPerk));

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EmergencySneak).OrderByStackingOrder(), SmokeBombNano, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NemesisNanoPrograms).OrderByStackingOrder(), ShadesCaressNano, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealthDrain).OrderByStackingOrder(), HealthDrainNano);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SpiritDrain).OrderByStackingOrder(), SpiritSiphonNano);

            //Items
            RegisterItemProcessor(RelevantItems.Tattoo, RelevantItems.Tattoo, TattooItem, CombatActionPriority.High);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AgilityBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ConcealmentBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MultiwieldBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtsBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadePiercingBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SneakAttackBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.WeaponEffectAdd_On2).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(RelevantNanos.ShadeDmgProc, DamageProc);
            RegisterSpellProcessor(RelevantNanos.ShadeStunProc, StunProc);
            RegisterSpellProcessor(RelevantNanos.ShadeInitDebuffProc, InitDebuffProc);
            RegisterSpellProcessor(RelevantNanos.ShadeDotProc, DotProc);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RunspeedBuffs).OrderByStackingOrder(), FasterThanYourShadow);
        }

        private bool DamageProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return ShadeProc(ProcType.DAMAGE, spell, fightingtarget, ref actiontarget);
        }

        private bool StunProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return ShadeProc(ProcType.STUN, spell, fightingtarget, ref actiontarget);
        }

        private bool InitDebuffProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return ShadeProc(ProcType.INIT_DEBUFF, spell, fightingtarget, ref actiontarget);
        }

        private bool DotProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return ShadeProc(ProcType.DOT, spell, fightingtarget, ref actiontarget);
        }

        private bool ShadeProc(ProcType procType, Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsProcSelected(procType))
            {
                return false;
            }

            return GenericBuff(spell, fightingtarget, ref actiontarget);
        }

        private bool IsProcSelected(ProcType procType)
        {
            return procType == (ProcType)settings["ProcSelection"].AsInt32();
        }

        private bool ShadesCaressNano(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking || fightingtarget == null)
                return false;

            if (DynelManager.LocalPlayer.HealthPercent <= 50 && fightingtarget.HealthPercent > 5)
                return true;

            return false;
        }

        private bool FasterThanYourShadow(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(IsInsideInnerSanctum())
            {
                return false;
            }
            return ToggledTeamBuff("UseFasterThanYourShadow", spell, fightingTarget, target => HasBuffNanoLine(NanoLine.RunspeedBuffs, target) || HasGsfNanoLine(target), ref actionTarget);
        }

        private bool HasGsfNanoLine(SimpleChar target)
        {
            return target.Buffs.Any(buff => buff.Name.Contains("Grid"));
        }

        private bool TattooItem(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            // don't use if BM is locked (we will add this dynamically later)
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BiologicalMetamorphosis))
                return false;

            // don't use if we're above 40%
            if (DynelManager.LocalPlayer.HealthPercent > 40)
                return false;

            // don't use if nothing is fighting us
            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0)
                return false;

            // don't use if we have another major absorb (example: nanomage booster) running
            // we could check remaining absorb stat to be slightly more effective
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon))
                return false;

            // don't use if our fighting target has caress running
            if (fightingtarget.Buffs.Contains(275242))
                return false;

            return true;
        }

        private bool SmokeBombNano(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = false;

            if (DynelManager.LocalPlayer.HealthPercent <= MissingHealthCombatAbortPercentage)
                return true;

            return false;
        }

        private bool SpiritSiphonNano(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseSpiritSiphon"))
                return false;

            if (DynelManager.LocalPlayer.Nano < spell.Cost)
                return false;

            if (!DynelManager.LocalPlayer.IsAttacking)
                return false;

            return true;
        }

        private bool HealthDrainNano(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Nano < spell.Cost)
                return false;

            if (!DynelManager.LocalPlayer.IsAttacking)
                return false;

            // if we have caress, save enough nano to use it
            if (Spell.Find(RelevantNanos.ShadesCaress, out Spell caress))
            {
                if (DynelManager.LocalPlayer.Nano - spell.Cost < caress.Cost)
                    return false;
            }

            // only use it for dps if we have plenty of nano
            if (IsSettingEnabled("UseDrainNanoForDps") && DynelManager.LocalPlayer.NanoPercent > 80)
                return true;

            // otherwise save it for if our health starts to drop
            if (DynelManager.LocalPlayer.HealthPercent >= 85)
                return false;

            return true;
        }

        private bool PiercingMasteryPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null)
                return false;

            //Don't PM if there are TR/SP chains in progress
            if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (RelevantPerks.TotemicRites.Contains(action.Hash) || RelevantPerks.SpiritPhylactery.Contains(action.Hash))))
                return false;

            if (!(PerkAction.Find(PerkHash.Stab, out PerkAction stab) && PerkAction.Find(PerkHash.DoubleStab, out PerkAction doubleStab)))
                return true;

            if (perkAction.Hash == PerkHash.Perforate)
            {
                if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (action == stab || action == doubleStab)))
                    return false;
            }

            if (!(PerkAction.Find(PerkHash.Stab, out PerkAction perforate) && PerkAction.Find(PerkHash.DoubleStab, out PerkAction lacerate)))
                return true;

            if (perkAction.Hash == PerkHash.Impale)
            {
                if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (action == stab || action == doubleStab || action == perforate || action == lacerate)))
                    return false;
            }

            return true;
        }

        private bool SpiritPhylacteryPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null)
                return false;

            //Don't SP if there are TR/PM chains in progress
            if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (RelevantPerks.TotemicRites.Contains(action.Hash) || RelevantPerks.PiercingMastery.Contains(action.Hash))))
                return false;

            return true;
        }

        private bool TotemicRitesPerk(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null)
                return false;

            //Don't TR if there are SP/PM chains in progress
            if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (RelevantPerks.SpiritPhylactery.Contains(action.Hash) || RelevantPerks.PiercingMastery.Contains(action.Hash))))
                return false;

            return true;
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            if (!IsProcSelected(ProcType.DAMAGE))
            {
                CancelBuffs(RelevantNanos.ShadeDmgProc);
            }
            if (!IsProcSelected(ProcType.STUN))
            {
                CancelBuffs(RelevantNanos.ShadeStunProc);
            }
            if (!IsProcSelected(ProcType.INIT_DEBUFF))
            {
                CancelBuffs(RelevantNanos.ShadeInitDebuffProc);
            }
            if (!IsProcSelected(ProcType.DOT))
            {
                CancelBuffs(RelevantNanos.ShadeDotProc);
            }
        }

        private enum ProcType
        {
            DAMAGE = 0,
            STUN = 1,
            INIT_DEBUFF = 2,
            DOT = 3
        }

        private class RelevantItems {
            public const int Tattoo = 269511;
        }

        private class RelevantNanos
        {
            public const int ShadesCaress = 266300;
            public const int CompositeAttribute = 223372;
            public const int CompositeNano = 223380;
            public const int CompositeMelee = 223360;
            public const int CompositeMeleeSpec = 215264;
            public static readonly int[] ShadeDmgProc = { 224167, 224165, 224163, 210371, 210369, 210367, 210365, 210363, 210361, 210359, 210357, 210355, 210353 };
            public static readonly int[] ShadeStunProc = { 224171, 224169, 210380, 210378, 210376 };
            public static readonly int[] ShadeInitDebuffProc = { 224177, 210407, 210401 };
            public static readonly int[] ShadeDotProc = { 224161, 224159, 210395, 210393, 210391, 210389, 210387 };
        }

        private class RelevantPerks
        {
            public static readonly List<PerkHash> TotemicRites = new List<PerkHash>
            {
                PerkHash.RitualOfDevotion,
                PerkHash.DevourVigor,
                PerkHash.RitualOfZeal,
                PerkHash.DevourEssence,
                PerkHash.RitualOfSpirit,
                PerkHash.DevourVitality,
                PerkHash.RitualOfBlood
            };

            public static readonly List<PerkHash> PiercingMastery = new List<PerkHash>
            {
                PerkHash.Stab,
                PerkHash.DoubleStab,
                PerkHash.Perforate,
                PerkHash.Lacerate,
                PerkHash.Impale,
                PerkHash.Gore,
                PerkHash.Hecatomb
            };

            public static readonly List<PerkHash> SpiritPhylactery = new List<PerkHash>
            {
                PerkHash.CaptureVigor,
                PerkHash.UnsealedBlight,
                PerkHash.CaptureEssence,
                PerkHash.UnsealedPestilence,
                PerkHash.CaptureSpirit,
                PerkHash.UnsealedContagion,
                PerkHash.CaptureVitality
            };
        }
    }
}
