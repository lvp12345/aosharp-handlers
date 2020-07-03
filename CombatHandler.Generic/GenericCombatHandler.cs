using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Combat;

namespace CombatHandler.Generic
{
    public class GenericCombatHandler : AOSharp.Core.Combat.CombatHandler
    {
        public GenericCombatHandler() : base()
        {
            RegisterPerkProcessor(PerkHash.Bore, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.Crave, TargetedDamagePerk);

            RegisterPerkProcessor(PerkHash.NanoFeast, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.BotConfinement, TargetedDamagePerk);

            //Fuzz
            //Fire Frenzy

            //Bluntness
            //Break

            //Collapser
            //Implode

            //Initial strike

            //Tick
            //Assume target

            // Opportunity knocks


            // xp stim?
            // health and nano recharger
            // first aid stim
            // free movement
            // flurry
            // barrow strength/motr (include 220 totw one)
            // absorb shoulders (include 220 totw one)
            // endurance booster
            // kizzer
            // lava cap
            // totw rings?
            // coffee
            // reani cloaks
            // deflection shield
            // SoM stims
            // rod of dismissal / staff of cleansing
            // alb rings
            // special arrows
            // MA attacks



            switch (DynelManager.LocalPlayer.Breed)
            {
                case Breed.Solitus:
                    break;
                case Breed.Opifex:
                    //Opening
                    //Derivate
                    //Blinded by delights
                    //Dizzying Heights
                    break;
                case Breed.Nanomage:
                    break;
                case Breed.Atrox:
                    break;
            }
        }

        protected virtual bool TargetedDamagePerk(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;
            return DamagePerk(perk, fightingTarget, out _);
        }

        protected virtual bool DamagePerk(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = null;

            if (fightingTarget == null)
                return false;

            if (fightingTarget.Health > 50000)
                return true;

            if (fightingTarget.HealthPercent < 5)
                return false;

            return true;
        }
    }
}
