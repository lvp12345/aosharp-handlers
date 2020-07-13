using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Combat;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;

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

            RegisterItemProcessor(RelevantItems.FlurryOfBlowsLow, RelevantItems.FlurryOfBlowsLow, DamageItem);
            RegisterItemProcessor(RelevantItems.FlurryOfBlowsHigh, RelevantItems.FlurryOfBlowsHigh, DamageItem);
            RegisterItemProcessor(RelevantItems.StrengthOfTheImmortal, RelevantItems.StrengthOfTheImmortal, DamageItem);
            RegisterItemProcessor(RelevantItems.MightOfTheRevenant, RelevantItems.MightOfTheRevenant, DamageItem);
            RegisterItemProcessor(RelevantItems.BarrowStrength, RelevantItems.BarrowStrength, DamageItem);
            RegisterItemProcessor(RelevantItems.MeteoriteSpikes, RelevantItems.MeteoriteSpikes, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.LavaCapsule, RelevantItems.LavaCapsule, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.KizzermoleGumboil, RelevantItems.KizzermoleGumboil, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.SteamingHotCupOfEnhancedCoffee, RelevantItems.SteamingHotCupOfEnhancedCoffee, Coffee);

            RegisterItemProcessor(RelevantItems.DreadlochEnduranceBooster, RelevantItems.DreadlochEnduranceBooster, EnduranceBooster, CombatActionPriority.High);
            RegisterItemProcessor(RelevantItems.DreadlochEnduranceBoosterNanomageEdition, RelevantItems.DreadlochEnduranceBoosterNanomageEdition, EnduranceBooster, CombatActionPriority.High);
            // xp stim?
            // health and nano recharger
            // first aid stim
            // free movement
            // absorb shoulders (include 220 totw one)
            // endurance booster
            // totw rings?
            // reani cloaks
            // deflection shield
            // SoM stims
            // rod of dismissal / staff of cleansing
            // alb rings
            // special arrows
            // MA attacks

            RegisterSpellProcessor(RelevantNanos.FountainOfLife, FountainOfLife);


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

        private bool FountainOfLife(Spell spell, SimpleChar fightingtarget, out SimpleChar target)
        {
            // Prioritize keeping ourself alive
            if (DynelManager.LocalPlayer.HealthPercent <= 30)
            {
                target = DynelManager.LocalPlayer;
                return true;
            }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent < 30)
                    .OrderByDescending(c => c.GetStat(Stat.IsFightingMe))
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    target = dyingTeamMember;
                    return true;
                }
            }

            target = null;
            return false;
        }

        private bool EnduranceBooster(Item item, SimpleChar fightingtarget, out SimpleChar target)
        {
            target = null;

            // don't use if skill is locked (we will add this dynamically later)
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength))
                return false;

            // don't use if we're above 40%
            if (DynelManager.LocalPlayer.HealthPercent > 40)
                return false;

            // don't use if nothing is fighting us
            if (DynelManager.LocalPlayer.GetStat(Stat.IsFightingMe) == 0)
                return false;

            // don't use if we have another major absorb running
            // we could check remaining absorb stat to be slightly more effective
            if (DynelManager.LocalPlayer.Buffs.Contains(Nanoline.BioCocoon))
                return false;

            return true;
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

        protected virtual bool TargetedDamageItem(Item item, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;
            return DamageItem(item, fightingTarget, out _);
        }

        protected virtual bool DamageItem(Item item, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = null;

            if (fightingTarget == null)
                return false;

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item)))
                return false;

            return true;
        }

        protected virtual bool Coffee(Item item, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = null;

            if (!DynelManager.LocalPlayer.Buffs.Contains(Nanoline.FoodandDrinkBuffs))
                return DamageItem(item, fightingTarget, out _);

            return false;
        }

        // This will eventually be done dynamically but for now I will implement
        // it statically so we can have it functional
        private Stat GetSkillLockStat(Item item)
        {
            switch (item.HighId)
            {
                case RelevantItems.FlurryOfBlowsLow:
                case RelevantItems.FlurryOfBlowsHigh:
                    return Stat.AggDef;
                case RelevantItems.StrengthOfTheImmortal:
                case RelevantItems.MightOfTheRevenant:
                case RelevantItems.BarrowStrength:
                    return Stat.Strength;
                case RelevantItems.MeteoriteSpikes:
                case RelevantItems.LavaCapsule:
                case RelevantItems.KizzermoleGumboil:
                    return Stat.SharpObject;
                case RelevantItems.SteamingHotCupOfEnhancedCoffee:
                    return Stat.RunSpeed;
                default:
                    throw new Exception($"No skill lock stat defined for item id {item.HighId}");
            }
        }

        private static class RelevantItems
        {
            public const int FlurryOfBlowsLow = 85907;
            public const int FlurryOfBlowsHigh = 85908;
            public const int StrengthOfTheImmortal = -1;
            public const int MightOfTheRevenant = 206013;
            public const int BarrowStrength = 204653;
            public const int LavaCapsule = 245990;
            public const int KizzermoleGumboil = 245323;
            public const int SteamingHotCupOfEnhancedCoffee = 157296;
            public const int DreadlochEnduranceBooster = 267168;
            public const int DreadlochEnduranceBoosterNanomageEdition = 267167;
            public const int MeteoriteSpikes = 244204;
        }

        private static class RelevantNanos
        {
            public const int FountainOfLife = 302907;
        }
    }
}
