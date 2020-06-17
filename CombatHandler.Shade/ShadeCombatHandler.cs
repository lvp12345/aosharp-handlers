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

        public ShadeCombatHandler() : base()
        {
            //Perks
            RegisterPerkProcessor(PerkHash.Limber, Limber);
            RegisterPerkProcessor(PerkHash.DanceOfFools, DanceOfFools);

            RegisterPerkProcessor(PerkHash.Blur, GenericDamagePerk);

            RegisterPerkProcessor(PerkHash.CaptureVigor, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.UnsealedBlight, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.CaptureEssence, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.UnsealedPestilence, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.CaptureSpirit, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.UnsealedContagion, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.CaptureVitality, GenericDamagePerk);

            RegisterPerkProcessor(PerkHash.Perforate, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Lacerate, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Impale, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Gore, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Hecatomb, GenericDamagePerk);

            RegisterPerkProcessor(PerkHash.RitualOfDevotion, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.DevourVigor, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.RitualOfZeal, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.DevourEssence, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.RitualOfSpirit, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.DevourVitality, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.RitualOfBlood, GenericDamagePerk);

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

        private bool GenericDamagePerk(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;

            if (fightingTarget == null || fightingTarget.HealthPercent < 5)
                return false;

            return true;
        }
    }
}
