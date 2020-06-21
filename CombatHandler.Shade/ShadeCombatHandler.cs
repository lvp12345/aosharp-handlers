using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Combat;

namespace Desu
{
    public class ShadeCombatHandler : CombatHandler
    {
        private const int DOF_BUFF = 210159;
        private const int LIMBER_BUFF = 210158;

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
            RegisterPerkProcessor(PerkHash.Limber, Limber);
            RegisterPerkProcessor(PerkHash.DanceOfFools, DanceOfFools);

            RegisterPerkProcessor(PerkHash.Blur, GenericDamagePerk);

            SpiritPhylactery.ForEach(p => RegisterPerkProcessor(p, SpiritPhylacteryPerk));
            TotemicRites.ForEach(p => RegisterPerkProcessor(p, TotemicRitesPerk));
            PiercingMastery.ForEach(p => RegisterPerkProcessor(p, PiercingMasteryPerk));

            RegisterPerkProcessor(PerkHash.Bore, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Crave, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.NanoFeast, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.BotConfinement, GenericDamagePerk);

            RegisterPerkProcessor(PerkHash.ChaosRitual, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Diffuse, GenericDamagePerk);
            //Spells

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

        private bool GenericDamagePerk(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;

            if (fightingTarget == null || fightingTarget.HealthPercent < 5)
                return false;

            return true;
        }

        private bool PiercingMasteryPerk(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;

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
            target = fightingTarget;

            if (fightingTarget == null)
                return false;

            //Don't SP if there are TR/PM chains in progress
            if (_actionQueue.Any(x => x.CombatAction is Perk action && (TotemicRites.Contains(action.Hash) || PiercingMastery.Contains(action.Hash))))
                return false;

            return true;
        }

        private bool TotemicRitesPerk(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;

            if (fightingTarget == null)
                return false;

            //Don't TR if there are SP/PM chains in progress
            if (_actionQueue.Any(x => x.CombatAction is Perk action && (SpiritPhylactery.Contains(action.Hash) || PiercingMastery.Contains(action.Hash))))
                return false;

            return true;
        }

        private IEnumerable<PerkHash> GetTotemicRitesPerks()
        {
            yield return PerkHash.RitualOfDevotion;
            yield return PerkHash.DevourVigor;


            RegisterPerkProcessor(PerkHash.RitualOfDevotion, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.DevourVigor, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.RitualOfZeal, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.DevourEssence, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.RitualOfSpirit, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.DevourVitality, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.RitualOfBlood, GenericDamagePerk);
        }


    }
}
