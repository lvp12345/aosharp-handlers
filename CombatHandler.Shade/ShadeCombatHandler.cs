using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Combat;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;

namespace Desu
{
    public class ShadeCombatHandler : GenericCombatHandler
    {
        private const int DOF_BUFF = 210159;
        private const int LIMBER_BUFF = 210158;
        private const int ShadesCaress = 266300;
        private const int MissingHealthCombatAbortPercentage = 30;
        private Menu _menu;

        private List<PerkHash> TotemicRites = new List<PerkHash>
        {
            PerkHash.RitualOfDevotion,
            PerkHash.DevourVigor,
            PerkHash.RitualOfZeal,
            PerkHash.DevourEssence,
            PerkHash.RitualOfSpirit,
            PerkHash.DevourVitality,
            PerkHash.RitualOfBlood
        };  

        private List<PerkHash> PiercingMastery = new List<PerkHash>
        {
            PerkHash.Stab,
            PerkHash.DoubleStab,
            PerkHash.Perforate,
            PerkHash.Lacerate,
            PerkHash.Impale,
            PerkHash.Gore,
            PerkHash.Hecatomb
        };

        private List<PerkHash> SpiritPhylactery = new List<PerkHash>
        {
            PerkHash.CaptureVigor,
            PerkHash.UnsealedBlight,
            PerkHash.CaptureEssence,
            PerkHash.UnsealedPestilence,
            PerkHash.CaptureSpirit,
            PerkHash.UnsealedContagion,
            PerkHash.CaptureVitality
        };

        public ShadeCombatHandler() : base()
        {
            //Perks
            RegisterPerkProcessor(PerkHash.Limber, Limber, CombatActionPriority.High);
            RegisterPerkProcessor(PerkHash.DanceOfFools, DanceOfFools, CombatActionPriority.High);

            RegisterPerkProcessor(PerkHash.Blur, TargetedDamagePerk);

            SpiritPhylactery.ForEach(p => RegisterPerkProcessor(p, SpiritPhylacteryPerk));
            TotemicRites.ForEach(p => RegisterPerkProcessor(p, TotemicRitesPerk));
            PiercingMastery.ForEach(p => RegisterPerkProcessor(p, PiercingMasteryPerk));

            RegisterPerkProcessor(PerkHash.ChaosRitual, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.Diffuse, TargetedDamagePerk);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.EmergencySneak).OrderByStackingOrder(), SmokeBombNano, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.NemesisNanoPrograms).OrderByStackingOrder(), ShadesCaressNano, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.HealthDrain).OrderByStackingOrder(), HealthDrainNano);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.SpiritDrain).OrderByStackingOrder(), SpiritSiphonNano);

            _menu = new Menu("CombatHandler.Shade", "CombatHandler.Shade");
            _menu.AddItem(new MenuBool("UseDrainNanoForDps", "Use drain nano for dps", false));
            _menu.AddItem(new MenuBool("UseSpiritSiphon", "Use spirit siphon", false));
            OptionPanel.AddMenu(_menu);
        }

        private bool ShadesCaressNano(Spell spell, SimpleChar fightingtarget, out SimpleChar target)
        {
            target = fightingtarget;

            if (!DynelManager.LocalPlayer.IsAttacking || fightingtarget == null)
                return false;

            if (DynelManager.LocalPlayer.HealthPercent <= 50 && fightingtarget.HealthPercent > 5)
                return true;

            return false;
        }

        private bool SmokeBombNano(Spell spell, SimpleChar fightingtarget, out SimpleChar target)
        {
            target = null;

            if (DynelManager.LocalPlayer.HealthPercent <= MissingHealthCombatAbortPercentage)
            {
                return true;
            }

            return false;
        }

        private bool SpiritSiphonNano(Spell spell, SimpleChar fightingtarget, out SimpleChar target)
        {
            target = fightingtarget;

            if (!_menu.GetBool("UseSpiritSiphon"))
                return false;

            if (DynelManager.LocalPlayer.Nano < spell.Cost)
                return false;

            if (!DynelManager.LocalPlayer.IsAttacking)
                return false;

            return true;
        }

        private bool HealthDrainNano(Spell spell, SimpleChar fightingtarget, out SimpleChar target)
        {
            target = fightingtarget;

            if (DynelManager.LocalPlayer.Nano < spell.Cost)
                return false;

            if (!DynelManager.LocalPlayer.IsAttacking)
                return false;

            // if we have caress, save enough nano to use it
            if (Spell.Find(ShadesCaress, out Spell caress))
            {
                if (DynelManager.LocalPlayer.Nano - spell.Cost < caress.Cost)
                    return false;
            }

            // only use it for dps if we have plenty of nano
            if (_menu.GetBool("UseDrainNanoForDps") && DynelManager.LocalPlayer.NanoPercent > 80)
                return true;

            // otherwise save it for if our health starts to drop
            if (DynelManager.LocalPlayer.HealthPercent >= 85)
                return false;

            return true;
        }

        private bool Limber(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = null;

            Buff dof;
            if (DynelManager.LocalPlayer.Buffs.Find(DOF_BUFF, out dof) && dof.RemainingTime > 12.5f)
                return false;

            return true;
        }

        private bool DanceOfFools(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = null;

            Buff limber;
            if (!DynelManager.LocalPlayer.Buffs.Find(LIMBER_BUFF, out limber) || limber.RemainingTime > 12.5f)
                return false;

            return true;
        }

        private bool PiercingMasteryPerk(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = null;

            if (fightingTarget == null)
                return false;

            //Don't PM if there are TR/SP chains in progress
            if (_actionQueue.Any(x => x.CombatAction is Perk action && (TotemicRites.Contains(action.Hash) || SpiritPhylactery.Contains(action.Hash))))
                return false;

            if (!(Perk.Find(PerkHash.Stab, out Perk stab) && Perk.Find(PerkHash.DoubleStab, out Perk doubleStab)))
                return true;

            if (perk.Hash == PerkHash.Perforate)
            {
                if (_actionQueue.Any(x => x.CombatAction is Perk action && (action == stab || action == doubleStab)))
                    return false;
            }

            if (!(Perk.Find(PerkHash.Stab, out Perk perforate) && Perk.Find(PerkHash.DoubleStab, out Perk lacerate)))
                return true;

            if (perk.Hash == PerkHash.Impale)
            {
                if (_actionQueue.Any(x => x.CombatAction is Perk action && (action == stab || action == doubleStab || action == perforate || action == lacerate)))
                    return false;
            }

            return true;
        }

        private bool SpiritPhylacteryPerk(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = null;

            if (fightingTarget == null)
                return false;

            //Don't SP if there are TR/PM chains in progress
            if (_actionQueue.Any(x => x.CombatAction is Perk action && (TotemicRites.Contains(action.Hash) || PiercingMastery.Contains(action.Hash))))
                return false;

            return true;
        }

        private bool TotemicRitesPerk(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = null;

            if (fightingTarget == null)
                return false;

            //Don't TR if there are SP/PM chains in progress
            if (_actionQueue.Any(x => x.CombatAction is Perk action && (SpiritPhylactery.Contains(action.Hash) || PiercingMastery.Contains(action.Hash))))
                return false;

            return true;
        }

        protected override bool TargetedDamagePerk(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;

            //Don't use if there are SP/PM/TR chains in progress
            if (_actionQueue.Any(x => x.CombatAction is Perk action && (SpiritPhylactery.Contains(action.Hash) || PiercingMastery.Contains(action.Hash) || TotemicRites.Contains(action.Hash))))
                return false;

            return DamagePerk(perk, fightingTarget, out _);
        }

        protected override bool DamagePerk(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = null;

            if (fightingTarget == null)
                return false;

            if (fightingTarget.Health > 50000)
                return true;

            if (fightingTarget.HealthPercent < 5)
                return false;

            //Don't use if there are SP/PM/TR chains in progress
            if (_actionQueue.Any(x => x.CombatAction is Perk action && (SpiritPhylactery.Contains(action.Hash) || PiercingMastery.Contains(action.Hash) || TotemicRites.Contains(action.Hash))))
                return false;

            return true;
        }
    }
}
