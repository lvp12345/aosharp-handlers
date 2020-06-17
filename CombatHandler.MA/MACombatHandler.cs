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
    public class MACombatHandler : CombatHandler
    {
        private const int DOF_BUFF = 210159;
        private const int LIMBER_BUFF = 210158;

        public MACombatHandler() : base()
        {
            //Perks
            RegisterPerkProcessor(PerkHash.Limber, Limber);
            RegisterPerkProcessor(PerkHash.DanceOfFools, DanceOfFools);      
            RegisterPerkProcessor(PerkHash.Moonmist, Moonmist);
            RegisterPerkProcessor(PerkHash.Dragonfire, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.ChiConductor, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Incapacitate, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.TremorHand, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.FleshQuiver, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Obliterate, Obliterate);
            RegisterPerkProcessor(PerkHash.Bore, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Crave, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.NanoFeast, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.BotConfinement, GenericDamagePerk);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.SingleTargetHealing).OrderByStackingOrder(), SingleTargetHeal);
        }

        private bool SingleTargetHeal(Spell spell, SimpleChar fightingTarget, out SimpleChar target)
        {
            if (DynelManager.LocalPlayer.MissingHealth > 2000) //TODO: Some kind of healing check to calc an optimal missing health value
            {
                target = DynelManager.LocalPlayer;
                return true;
            }

            target = null;
            return false;
        }

        private bool Limber(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;

            Buff dof;
            if (DynelManager.LocalPlayer.Buffs.Find(DOF_BUFF, out dof) && dof.RemainingTime > 12.5f)
                return false;

            return true;
        }

        private bool DanceOfFools(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;

            Buff limber;
            if (!DynelManager.LocalPlayer.Buffs.Find(LIMBER_BUFF, out limber) || limber.RemainingTime > 12.5f)
                return false;

            return true;
        }

        private bool Moonmist(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;

            if (fightingTarget == null || fightingTarget.HealthPercent < 90)
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

        private bool Obliterate(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;

            if (fightingTarget == null || fightingTarget.HealthPercent > 15)
                return false;

            return true;
        }
    }
}
