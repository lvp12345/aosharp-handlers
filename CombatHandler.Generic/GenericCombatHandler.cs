using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using MultiboxHelper;

using static CombatHandler.Generic.PerkCondtionProcessors;

namespace CombatHandler.Generic
{
    public class GenericCombatHandler : AOSharp.Core.Combat.CombatHandler
    {
        private const float PostZonePetCheckBuffer = 5;
        public int EvadeCycleTimeoutSeconds = 180;

        private double _lastPetSyncTime = Time.NormalTime;
        protected double _lastZonedTime = Time.NormalTime;
        protected double _lastCombatTime = double.MinValue;

        private string pluginDir;

        protected AOSharp.Core.Settings settings;

        protected static HashSet<string> debuffTargetsToIgnore = new HashSet<string>
        {
                    "Immortal Guardian",
                    "Mature Abyss Orchid",
                    "Abyss Orchid Sprout",
                    "Tower of Astodan",
                    "Unicorn Commander Labbe",
                    "Altar of Torture",
                    "Altar of Purification",
                    "Calan-Cur",
                    "Spirit of Judgement",
                    "Wandering Spirit",
                    "Altar of Torture",
                    "Altar of Purification",
                    "Unicorn Coordinator Magnum Blaine",
                    "Xan Spirit",
                    "Watchful Spirit",
                    "Amesha Vizaresh",
                    "Guardian Spirit of Purification",
                    "Tibor 'Rocketman' Nagy",
                    "One Who Obeys Precepts",
                    "The Retainer Of Ergo",
                    "Green Tower",
                    "Blue Tower",
                    "Alien Cocoon",
                    "Outzone Supplier",
                    "Hollow Island Weed",
                    "Sheila Marlene",
                    "Unicorn Advance Sentry",
                    "Unicorn Technician",
                    "Basic Tools Merchant",
                    "Container Supplier",
                    "Basic Quality Pharmacist",
                    "Basic Quality Armorer",
                    "Basic Quality Weaponsdealer",
                    "Tailor",
                    "Unicorn Commander Rufus",
                    "Ergo, Inferno Guardian of Shadows",
                    "Unicorn Trooper",
                    "Unicorn Squadleader",
                    "Rookie Alien Hunter",
                    "Unicorn Service Tower Alpha",
                    "Unicorn Service Tower Delta",
                    "Unicorn Service Tower Gamma",
                    "Sean Powell",
                    "Xan Spirit",
                    "Unicorn Guard",
                    "Essence Fragment",
                    "Scalding Flames",
                    "Guide",
                    "Guard",
                    "Awakened Xan"
        };

        public GenericCombatHandler(string pluginDir)
        {
            this.pluginDir = pluginDir;
            Game.TeleportEnded += TeleportEnded;

            settings = new AOSharp.Core.Settings("CombatHandler");

            RegisterPerkProcessors();
            RegisterPerkProcessor(PerkHash.Limber, Limber, CombatActionPriority.High);
            RegisterPerkProcessor(PerkHash.DanceOfFools, DanceOfFools, CombatActionPriority.High);

            RegisterSpellProcessor(RelevantNanos.FountainOfLife, FountainOfLife);
            RegisterItemProcessor(RelevantItems.FlowerOfLifeLow, RelevantItems.FlowerOfLifeHigh, FlowerOfLife);

            RegisterItemProcessor(RelevantItems.ReflectGraft, RelevantItems.ReflectGraft, ReflectGraft);

            RegisterItemProcessor(RelevantItems.SteamingHotCupOfEnhancedCoffee, RelevantItems.SteamingHotCupOfEnhancedCoffee, Coffee);

            RegisterItemProcessor(RelevantItems.FlurryOfBlowsLow, RelevantItems.FlurryOfBlowsHigh, DamageItem);

            RegisterItemProcessor(RelevantItems.StrengthOfTheImmortal, RelevantItems.StrengthOfTheImmortal, DamageItem);
            RegisterItemProcessor(RelevantItems.MightOfTheRevenant, RelevantItems.MightOfTheRevenant, DamageItem);
            RegisterItemProcessor(RelevantItems.BarrowStrength, RelevantItems.BarrowStrength, DamageItem);

            RegisterItemProcessor(RelevantItems.GnuffsEternalRiftCrystal, RelevantItems.GnuffsEternalRiftCrystal, DamageItem);

            RegisterItemProcessor(RelevantItems.DreadlochEnduranceBooster, RelevantItems.DreadlochEnduranceBooster, EnduranceBooster, CombatActionPriority.High);
            RegisterItemProcessor(RelevantItems.DreadlochEnduranceBoosterNanomageEdition, RelevantItems.DreadlochEnduranceBoosterNanomageEdition, EnduranceBooster, CombatActionPriority.High);
            RegisterItemProcessor(RelevantItems.WitheredFlesh, RelevantItems.WitheredFlesh, WithFlesh, CombatActionPriority.High);
            RegisterItemProcessor(RelevantItems.DesecratedFlesh, RelevantItems.DesecratedFlesh, DescFlesh, CombatActionPriority.High);
            RegisterItemProcessor(RelevantItems.AssaultClassTank, RelevantItems.AssaultClassTank, AssaultClass, CombatActionPriority.High);

            //RegisterItemProcessor(new int[] { RelevantItems.FlurryOfBlowsLow, RelevantItems.StrengthOfTheImmortal,
            //RelevantItems.MightOfTheRevenant, RelevantItems.BarrowStrength, RelevantItems.GnuffsEternalRiftCrystal }, DamageItem, CombatActionPriority.High);

            //RegisterItemProcessor(new int[] { RelevantItems.MeteoriteSpikes, RelevantItems.LavaCapsule,
            //RelevantItems.HSR1, RelevantItems.KizzermoleGumboil, RelevantItems.UponAWaveOfSummerLow, 
            //RelevantItems.BlessedWithThunderLow }, TargetedDamageItem);

            RegisterItemProcessor(RelevantItems.MeteoriteSpikes, RelevantItems.MeteoriteSpikes, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.LavaCapsule, RelevantItems.LavaCapsule, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.HSR1, RelevantItems.HSR2, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.KizzermoleGumboil, RelevantItems.KizzermoleGumboil, TargetedDamageItem);

            RegisterItemProcessor(RelevantItems.UponAWaveOfSummerLow, RelevantItems.UponAWaveOfSummerHigh, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.BlessedWithThunderLow, RelevantItems.BlessedWithThunderHigh, TargetedDamageItem);

            //RegisterItemProcessor(RelevantItems.HealthAndNanoStim1, RelevantItems.HealthAndNanoStim200, HealthAndNanoStim, CombatActionPriority.High);
            //RegisterItemProcessor(RelevantItems.HealthAndNanoStim200, RelevantItems.HealthAndNanoStim400, HealthAndNanoStim, CombatActionPriority.High);

            RegisterItemProcessor(RelevantItems.RezCan, RelevantItems.RezCan, UseRezCan);
            RegisterItemProcessor(RelevantItems.ExperienceStim, RelevantItems.ExperienceStim, ExperienceStim);

            RegisterItemProcessor(new int[] { RelevantItems.HealthAndNanoStim1, RelevantItems.HealthAndNanoStim200, 
            RelevantItems.HealthAndNanoStim400, }, HealthAndNanoStim, CombatActionPriority.High);

            RegisterItemProcessor(new int[] { RelevantItems.PremSitKit, RelevantItems.AreteSitKit, RelevantItems.SitKit1,
            RelevantItems.SitKit100, RelevantItems.SitKit200, RelevantItems.SitKit300, RelevantItems.SitKit400 }, SitKit);

            RegisterItemProcessor(new int[] { RelevantItems.FreeStim1, RelevantItems.FreeStim50, RelevantItems.FreeStim100,
            RelevantItems.FreeStim300 }, FreeStim);

            //RegisterItemProcessor(RelevantItems.PremSitKit, RelevantItems.PremSitKit, SitKit);
            //RegisterItemProcessor(RelevantItems.AreteSitKit, RelevantItems.AreteSitKit, SitKit);
            //RegisterItemProcessor(RelevantItems.SitKit1, RelevantItems.SitKit100, SitKit);
            //RegisterItemProcessor(RelevantItems.SitKit100, RelevantItems.SitKit200, SitKit);
            //RegisterItemProcessor(RelevantItems.SitKit200, RelevantItems.SitKit300, SitKit);
            //RegisterItemProcessor(RelevantItems.SitKit300, RelevantItems.SitKit400, SitKit);

            //RegisterItemProcessor(RelevantItems.FreeStim1, RelevantItems.FreeStim50, FreeStim);
            //RegisterItemProcessor(RelevantItems.FreeStim50, RelevantItems.FreeStim100, FreeStim);
            //RegisterItemProcessor(RelevantItems.FreeStim100, RelevantItems.FreeStim200, FreeStim);
            //RegisterItemProcessor(RelevantItems.FreeStim200, RelevantItems.FreeStim300, FreeStim);


            RegisterItemProcessor(RelevantItems.AmmoBoxArrows, RelevantItems.AmmoBoxArrows, AmmoBoxArrows);
            RegisterItemProcessor(RelevantItems.AmmoBoxBullets, RelevantItems.AmmoBoxBullets, AmmoBoxBullets);
            RegisterItemProcessor(RelevantItems.AmmoBoxEnergy, RelevantItems.AmmoBoxEnergy, AmmoBoxEnergy);
            RegisterItemProcessor(RelevantItems.AmmoBoxShotgun, RelevantItems.AmmoBoxShotgun, AmmoBoxShotgun);

            RegisterSpellProcessor(RelevantNanos.CompositeNano, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeAttribute, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeUtility, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeMartialProwess, GenericBuff);

            RegisterSpellProcessor(RelevantNanos.InsightIntoSL, GenericBuff);

            if (GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(CharacterWieldedWeapon.Melee))
            {
                //We are melee
                RegisterSpellProcessor(RelevantNanos.CompositeMartial, GenericBuffExcludeInnerSanctum);
                RegisterSpellProcessor(RelevantNanos.CompositeMelee, GenericBuff);
                RegisterSpellProcessor(RelevantNanos.CompositePhysicalSpecial, GenericBuff);
            }


            if (GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(CharacterWieldedWeapon.Ranged))
            {
                //We are ranged
                RegisterSpellProcessor(RelevantNanos.CompositeRanged, GenericBuff);
                RegisterSpellProcessor(RelevantNanos.CompositeRangedSpecial, GenericBuff);
            }

            switch (DynelManager.LocalPlayer.Breed)
            {
                case Breed.Solitus:
                    break;
                case Breed.Opifex:
                    break;
                case Breed.Nanomage:
                    break;
                case Breed.Atrox:
                    break;
            }

            Game.TeleportEnded += OnZoned;
        }

        private void OnZoned(object s, EventArgs e)
        {
            _lastZonedTime = Time.NormalTime;
        }

        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, pluginDir + "\\UI\\" + xmlName, settings);
        }

        protected override void OnUpdate(float deltaTime)
        {
            SettingsController.CleanUp();

            base.OnUpdate(deltaTime);

            if (DynelManager.LocalPlayer.IsAttacking || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 0)
            {
                _lastCombatTime = Time.NormalTime;
            }
        }


        #region Perks

        private PerkConditionProcessor ToPerkConditionProcessor(GenericPerkConditionProcessor genericPerkConditionProcessor)
        {
            return (PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) => genericPerkConditionProcessor(perkAction, fightingTarget, ref actionTarget);
        }

        protected void RegisterPerkProcessors()
        {
            PerkAction.List.ForEach(perkAction => RegisterPerkAction(perkAction));
        }

        private void RegisterPerkAction(PerkAction perkAction)
        {
            GenericPerkConditionProcessor perkConditionProcessor = PerkCondtionProcessors.GetPerkConditionProcessor(perkAction);

            if (perkConditionProcessor != null)
            {
                RegisterPerkProcessor(perkAction.Hash, ToPerkConditionProcessor(perkConditionProcessor));
            }
        }

        protected bool LEProc(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perk.Name) { return false; }
            }
            return true;
        }

        private bool Limber(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking) { return false; }

            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.DanceOfFools, out Buff dof) && dof.RemainingTime > 12.5f) { return false; }

            // stop cycling if we haven't fought anything for over 10 minutes
            if (Time.NormalTime - _lastCombatTime > EvadeCycleTimeoutSeconds) { return false; }

            return true;
        }

        private bool DanceOfFools(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking) { return false; }

            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.Limber, out Buff limber) || limber.RemainingTime > 12.5f) { return false; }

            // stop cycling if we haven't fought anything for over 10 minutes
            if (Time.NormalTime - _lastCombatTime > EvadeCycleTimeoutSeconds) { return false; }

            return true;
        }

        #endregion

        #region Instanced Logic

        protected bool HealOverTimeBuff(string toggleName, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledBuff(toggleName, spell, fightingTarget, ref actionTarget);
        }

        protected bool BuffInitDoc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => SpellChecksOther(spell, c))
                    .Where(c => c.Profession == Profession.Doctor || c.Profession == Profession.NanoTechnician)
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }
            else
            {
                if (SpellChecksPlayer(spell))
                {
                    actionTarget.Target = DynelManager.LocalPlayer;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected bool BuffInitEngi(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => GetWieldedWeapons(c).HasFlag(CharacterWieldedWeapon.Ranged))
                    .Where(c => SpellChecksOther(spell, c))
                    .Where(c => c.Profession != Profession.Doctor && c.Profession != Profession.NanoTechnician)
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }
            else
            {
                if (SpellChecksPlayer(spell))
                {
                    actionTarget.Target = DynelManager.LocalPlayer;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        private bool FountainOfLife(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            // Prioritize keeping ourself alive
            if (DynelManager.LocalPlayer.HealthPercent <= 30)
            {
                actiontarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent < 30)
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    actiontarget.Target = dyingTeamMember;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Logic

        protected bool GenericBuffExcludeInnerSanctum(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.MajorEvasionBuffs)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool CombatBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !CanCast(spell)) { return false; }

            if (SpellChecksPlayer(spell))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        protected bool ToggledDebuffTarget(string settingName, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            // Check if we are fighting and if debuffing is enabled
            if (fightingTarget == null || !IsSettingEnabled(settingName)) { return false; }

            if (SpellChecksOther(spell, fightingTarget))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = fightingTarget;
                return true;
            }

            return false;
            //return !fightingTarget.Buffs.Any(buff => ShouldRefreshBuff(spell, buff));
        }

        protected bool ToggledDebuffOthersInCombat(string toggleName, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled(toggleName) || !CanCast(spell)) { return false; }

            SimpleChar debuffTarget = DynelManager.NPCs
                .Where(c => !debuffTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
                .Where(c => c.FightingTarget != null) //Is in combat
                .Where(c => !c.Buffs.Contains(301844)) // doesn't have ubt in ncu
                .Where(c => c.IsInLineOfSight)
                .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f) //Is in range for debuff (we assume weapon range == debuff range)
                .Where(c => SpellChecksOther(spell, c)) //Needs debuff refreshed
                .OrderBy(c => c.MaxHealth)
                .FirstOrDefault();

            if (debuffTarget != null)
            {
                actionTarget = (debuffTarget, true);
                return true;
            }

            return false;
        }

        protected virtual bool TargetedDamageItem(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = true;
            return DamageItem(item, fightingTarget, ref actionTarget);
        }

        protected virtual bool ReflectGraft(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return !DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item)) && !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ReflectShield);
        }

        protected virtual bool DamageItem(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return !DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item)) && fightingTarget != null && fightingTarget.IsInAttackRange();
        }

        protected bool ToggledBuff(string settingName, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled(settingName)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool TeamBuffExcludeInnerSanctum(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            return TeamBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool TeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell) || spell.Name.Contains("Veteran")) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => SpellChecksOther(spell, c))
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }
            else
            {
                if (SpellChecksPlayer(spell))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = DynelManager.LocalPlayer;
                    return true;
                }
            }

            return false;
        }

        protected bool GenericBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell) || spell.Name.Contains("Veteran")) { return false; }

            if (SpellChecksPlayer(spell))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        protected bool TeamBuffNoNTWeaponType(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, CharacterWieldedWeapon supportedWeaponType)
        {
            if (fightingTarget != null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => SpellChecksOther(spell, c))
                    .Where(c => c.Profession != Profession.NanoTechnician)
                    // Combines both together
                    .Where(c => GetWieldedWeapons(c).HasFlag(supportedWeaponType))
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    if (teamMemberWithoutBuff.Buffs.Contains(NanoLine.FixerSuppressorBuff) &&
                        (spell.Nanoline == NanoLine.FixerSuppressorBuff || spell.Nanoline == NanoLine.AssaultRifleBuffs)) { return false; }

                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected bool TeamBuffWeaponType(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, CharacterWieldedWeapon supportedWeaponType)
        {
            if (fightingTarget != null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => SpellChecksOther(spell, c))
                    // Combines both together
                    .Where(c => GetWieldedWeapons(c).HasFlag(supportedWeaponType))
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    if (teamMemberWithoutBuff.Buffs.Contains(NanoLine.FixerSuppressorBuff) &&
                        (spell.Nanoline == NanoLine.FixerSuppressorBuff || spell.Nanoline == NanoLine.AssaultRifleBuffs)) { return false; }

                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected bool BuffWeaponType(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, CharacterWieldedWeapon supportedWeaponType)
        {
            if (fightingTarget != null || !CanCast(spell)) { return false; }

            if (SpellChecksPlayer(spell) && GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(supportedWeaponType))
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        public bool IsNotFightingMe(SimpleChar target)
        {
            return target.IsAttacking && target.FightingTarget.Identity != DynelManager.LocalPlayer.Identity;
        }

        // expression body method / inline method   
        public static CharacterWieldedWeapon GetWieldedWeapons(SimpleChar local) => (CharacterWieldedWeapon)local.GetStat(Stat.EquippedWeapons);

        protected bool RangedBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) => BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Ranged);

        protected bool MeleeBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) => BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Melee);

        protected bool PistolSelfBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
                return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        protected bool PistolMasteryBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam)
            {
                return TeamBuffNoNTWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
            }
            else
                return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }
        #endregion

        #region Items

        private bool UseRezCan(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.FirstAid)) { return false; }

            actiontarget.ShouldSetTarget = false;
            return true;
        }

        private bool FreeStim(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(NanoLine.Root) && !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.Snare)
                && !DynelManager.LocalPlayer.Buffs.Contains(258231)) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.FirstAid)) { return false; }

            actiontarget.ShouldSetTarget = true;
            return true;

        }

        protected bool TauntTool(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !IsSettingEnabled("UseTauntTool")) { return false; }

            if (TauntTools.CanUseTauntTool())
            {
                actionTarget.Target = fightingTarget;
                actionTarget.ShouldSetTarget = true;
            }

            return false;
        }

        private bool SitKit(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Treatment)) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent >= 66 && DynelManager.LocalPlayer.NanoPercent >= 66) { return false; }

            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        private bool ExperienceStim(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.FirstAid)) { return false; }

            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = false;
            return true;

        }

        private bool FlowerOfLife(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (fightingtarget == null) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item))) { return false; }

            int approximateHealing = item.QualityLevel * 10;

            return DynelManager.LocalPlayer.MissingHealth > approximateHealing;
        }

        private bool HealthAndNanoStim(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.FirstAid)) { return false; }

            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) >= 8) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(280470) || DynelManager.LocalPlayer.Buffs.Contains(258231)) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent >= 80 && DynelManager.LocalPlayer.NanoPercent >= 80) { return false; }

            actiontarget.ShouldSetTarget = true;
            actiontarget.Target = DynelManager.LocalPlayer;

            return true;
        }

        private bool AmmoBoxBullets(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            Item bulletsammo = Inventory.Items
                .Where(c => c.Name.Contains("Bullets"))
                .Where(c => !c.Name.Contains("Crate"))
                .FirstOrDefault();

            actiontarget.ShouldSetTarget = false;

            return bulletsammo == null && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.WeaponSmithing);
        }

        private bool AmmoBoxEnergy(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            Item energyammo = Inventory.Items
                .Where(c => c.Name.Contains("Energy"))
                .Where(c => !c.Name.Contains("Crate"))
                .FirstOrDefault();

            actiontarget.ShouldSetTarget = false;

            return energyammo == null && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.WeaponSmithing);
        }

        private bool AmmoBoxShotgun(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            Item shotgunammo = Inventory.Items
                .Where(c => c.Name.Contains("Shotgun"))
                .Where(c => !c.Name.Contains("Crate"))
                .FirstOrDefault();

            actiontarget.ShouldSetTarget = false;

            return shotgunammo == null && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.WeaponSmithing);
        }

        private bool AmmoBoxArrows(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            Item arrowammo = Inventory.Items
                .Where(c => c.Name.Contains("Arrows"))
                .Where(c => !c.Name.Contains("Crate"))
                .FirstOrDefault();

            actiontarget.ShouldSetTarget = false;

            return arrowammo == null && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.WeaponSmithing);
        }

        private bool EnduranceBooster(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (Inventory.Find(305476, 305476, out Item absorbdesflesh))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BodyDevelopment)) { return false; }
            }
            if (Inventory.Find(204698, 204698, out Item absorbwithflesh))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BodyDevelopment)) { return false; }
            }
            if (Inventory.Find(156576, 156576, out Item absorbassaultclass))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.DuckExp)) { return false; }
            }

            // don't use if skill is locked (we will add this dynamically later)
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength)) { return false; }

            // don't use if we're above 40%
            if (DynelManager.LocalPlayer.HealthPercent >= 65) { return false; }

            // don't use if nothing is fighting us
            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            // don't use if we have another major absorb running
            // we could check remaining absorb stat to be slightly more effective
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)) { return false; }

            return true;
        }

        private bool AssaultClass(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            // don't use if skill is locked (we will add this dynamically later)
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.DuckExp)) { return false; }

            // don't use if we're above 40%
            if (DynelManager.LocalPlayer.HealthPercent >= 65) { return false; }

            // don't use if nothing is fighting us
            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            // don't use if we have another major absorb running
            // we could check remaining absorb stat to be slightly more effective
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)) { return false; }

            return true;
        }

        private bool DescFlesh(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (Inventory.Find(267168, 267168, out Item enduranceabsorbenf))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength)) { return false; }
            }
            if (Inventory.Find(267167, 267167, out Item enduranceabsorbnanomage))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength)) { return false; }
            }
            if (Inventory.Find(156576, 156576, out Item absorbassaultclass))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.DuckExp)) { return false; }
            }

            // don't use if skill is locked (we will add this dynamically later)
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BodyDevelopment)) { return false; }

            // don't use if we're above 40%
            if (DynelManager.LocalPlayer.HealthPercent >= 65) { return false; }

            // don't use if nothing is fighting us
            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            // don't use if we have another major absorb running
            // we could check remaining absorb stat to be slightly more effective
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)) { return false; }

            return true;
        }

        private bool WithFlesh(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            Inventory.Find(305476, 305476, out Item absorbdesflesh);

            if (Inventory.Find(267168, 267168, out Item enduranceabsorbenf))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength)) { return false; }
            }
            if (Inventory.Find(267167, 267167, out Item enduranceabsorbnanomage))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength)) { return false; }
            }
            if (Inventory.Find(156576, 156576, out Item absorbassaultclass))
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.DuckExp)) { return false; }
            }

            // don't use if skill is locked (we will add this dynamically later)
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BodyDevelopment)) { return false; }

            // don't use if we're above 40%
            if (DynelManager.LocalPlayer.HealthPercent >= 65) { return false; }

            // don't use if nothing is fighting us
            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            // don't use if we have another major absorb running
            // we could check remaining absorb stat to be slightly more effective
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)) { return false; }

            if (absorbdesflesh != null) { return false; }

            return true;
        }

        protected virtual bool Coffee(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(NanoLine.FoodandDrinkBuffs))
                return DamageItem(item, fightingTarget, ref actionTarget);

            return false;
        }
        #endregion

        #region Pets

        protected bool NoShellPetSpawner(PetType petType, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanSpawnPets(petType)) { return false; }

            actionTarget.ShouldSetTarget = false;
            return true;
        }

        protected bool PetSpawner(Dictionary<int, PetSpellData> petData, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!petData.ContainsKey(spell.Identity.Instance)) { return false; }

            PetType petType = petData[spell.Identity.Instance].PetType;

            //Ignore spell if we already have the shell in our inventory
            if (Inventory.Find(petData[spell.Identity.Instance].ShellId, out Item shell)) { return false; }

            return NoShellPetSpawner(petType, spell, fightingTarget, ref actionTarget);
        }

        protected virtual bool PetSpawnerItem(Dictionary<int, PetSpellData> petData, Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("SpawnPets")) { return false; }

            if (!CanLookupPetsAfterZone()) { return false; }

            if (!petData.Values.Any(x => (x.ShellId == item.LowId || x.ShellId == item.HighId) && !DynelManager.LocalPlayer.Pets.Any(p => p.Type == x.PetType))) { return false; }

            actionTarget.ShouldSetTarget = false;
            return true;
        }

        protected bool CanSpawnPets(PetType petType)
        {
            if (!IsSettingEnabled("SpawnPets") || !CanLookupPetsAfterZone() || PetAlreadySpawned(petType)) { return false; }

            return true;
        }

        private bool PetAlreadySpawned(PetType petType)
        {
            return DynelManager.LocalPlayer.Pets.Any(x => (x.Type == PetType.Unknown || x.Type == petType));
        }

        protected Pet FindAttackPetThat(Func<Pet, bool> Filter)
        {
            return DynelManager.LocalPlayer.Pets
                .Where(pet => pet.Character != null && pet.Character.Buffs != null)
                .Where(pet => pet.Type == PetType.Attack)
                .Where(pet => Filter.Invoke(pet))
                .FirstOrDefault();
        }

        protected Pet FindSupportPetThat(Func<Pet, bool> Filter)
        {
            return DynelManager.LocalPlayer.Pets
                .Where(pet => pet.Character != null && pet.Character.Buffs != null)
                .Where(pet => pet.Type == PetType.Support)
                .Where(pet => Filter.Invoke(pet))
                .FirstOrDefault();
        }

        protected Pet FindPetThat(Func<Pet, bool> Filter)
        {
            return DynelManager.LocalPlayer.Pets
                .Where(pet => pet.Character != null && pet.Character.Buffs != null)
                .Where(pet => pet.Type == PetType.Support || pet.Type == PetType.Attack || pet.Type == PetType.Heal)
                .Where(pet => Filter.Invoke(pet))
                .FirstOrDefault();
        }

        protected Pet FindPets()
        {
            return DynelManager.LocalPlayer.Pets
                .Where(pet => pet.Character != null && pet.Character.Buffs != null)
                .Where(pet => pet.Type == PetType.Support || pet.Type == PetType.Attack || pet.Type == PetType.Heal)
                .FirstOrDefault();
        }
        protected Pet FindPetsWithoutBuff(int[] buff)
        {
            return DynelManager.LocalPlayer.Pets
                .Where(pet => pet.Character != null && pet.Character.Buffs != null)
                .Where(pet => pet.Type == PetType.Support || pet.Type == PetType.Attack || pet.Type == PetType.Heal)
                .Where(pet => !pet.Character.Buffs.Contains(buff))
                .FirstOrDefault();
        }

        protected Pet FindPetNeedsHeal(int percent)
        {
            return DynelManager.LocalPlayer.Pets
                .Where(pet => pet.Character != null && pet.Character.Buffs != null)
                .Where(pet => pet.Type == PetType.Support || pet.Type == PetType.Attack || pet.Type == PetType.Heal)
                .Where(pet => pet.Character.HealthPercent <= percent)
                .FirstOrDefault();
        }

        protected bool CanLookupPetsAfterZone()
        {
            return Time.NormalTime > _lastZonedTime + PostZonePetCheckBuffer;
        }

        protected bool PetTargetBuff(NanoLine buffNanoLine, PetType petType, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            Pet petToBuff = FindPetThat(pet => pet.Type == petType && !pet.Character.Buffs.Contains(buffNanoLine));

            if (petToBuff != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = petToBuff.Character;
                return true;
            }

            return false;
        }

        protected void SynchronizePetCombatStateWithOwner()
        {
            if (CanLookupPetsAfterZone() && Time.NormalTime - _lastPetSyncTime > 1)
            {
                SynchronizePetCombatState(FindPetThat(pet => pet.Type == PetType.Attack));
                SynchronizePetCombatState(FindPetThat(pet => pet.Type == PetType.Support));

                _lastPetSyncTime = Time.NormalTime;
            }
        }

        private void SynchronizePetCombatState(Pet pet)
        {
            if (pet != null)
            {
                if (DynelManager.LocalPlayer.IsAttacking)
                {
                    if (!pet.Character.IsAttacking)
                    {
                        pet.Attack(DynelManager.LocalPlayer.FightingTarget.Identity);
                    }
                }
                else
                {
                    if (pet.Character.IsAttacking)
                    {
                        pet.Follow();
                    }
                }
            }
        }

        #endregion

        #region Checks

        protected bool HasBuffNanoLine(NanoLine nanoLine, SimpleChar target)
        {
            return target.Buffs.Contains(nanoLine);
        }

        protected bool CheckNotProfsBeforeCast(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //if (DynelManager.LocalPlayer.Profession == Profession.NanoTechnician && !IsSettingEnabled("CostTeam") && !IsSettingEnabled("NanoHoT")) { return false; }

            if (!CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.Profession != Profession.Keeper)
                    .Where(c => c.Profession != Profession.Engineer)
                    .Where(c => SpellChecksOther(spell, c))
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null && teamMemberWithoutBuff.Profession != Profession.Keeper)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected bool SomeoneNeedsHealing()
        {
            if (DynelManager.LocalPlayer.HealthPercent <= 85)
            {
                return true;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 85)
                    .OrderBy(c => c.Profession == Profession.Doctor)
                    .OrderBy(c => c.Profession == Profession.Enforcer)
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    return true;
                }
            }

            return false;
        }

        protected bool FindMemberWithHealthBelow(int healthPercentTreshold, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.HealthPercent <= healthPercentTreshold)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= healthPercentTreshold)
                    .OrderBy(c => c.Profession == Profession.Doctor)
                    .OrderBy(c => c.Profession == Profession.Enforcer)
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    actionTarget.Target = dyingTeamMember;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected bool FindPlayerWithHealthBelow(int healthPercentTreshold, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.HealthPercent <= healthPercentTreshold)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            SimpleChar dyingTeamMember = DynelManager.Characters
                .Where(c => c.IsPlayer)
                .Where(c => c.HealthPercent <= healthPercentTreshold)
                .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f)
                .OrderBy(c => c.Profession == Profession.Doctor)
                .OrderBy(c => c.Profession == Profession.Enforcer)
                .FirstOrDefault();

            if (dyingTeamMember != null)
            {
                actionTarget.Target = dyingTeamMember;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        protected bool HasNano(int nanoId)
        {
            return Spell.Find(nanoId, out Spell spell);
        }

        protected bool SpellChecksOther(Spell spell, SimpleChar fightingTarget)
        {
            if (DynelManager.LocalPlayer.Nano < spell.Cost) { return false; }

            if (Playfield.ModelIdentity.Instance == 152) { return false; }

            if (fightingTarget.IsPlayer && !MultiboxHelper.SettingsController.IsCharacterRegistered(fightingTarget.Identity)) { return false; }

            if (fightingTarget.IsPlayer && !HasNCU(spell, fightingTarget)) { return false; }

            if (fightingTarget.Buffs.Find(spell.Nanoline, out Buff buff))
            {
                //Don't cast if weaker than existing
                if (spell.StackingOrder < buff.StackingOrder) { return false; }

                //Don't cast if greater than 10% time remaining
                if (spell.Nanoline == buff.Nanoline && buff.RemainingTime / buff.TotalTime > 0.1) { return false; }
            }

            return true;
        }

        protected bool SpellChecksPlayer(Spell spell)
        {
            if (DynelManager.LocalPlayer.Nano < spell.Cost) { return false; }

            if (Playfield.ModelIdentity.Instance == 152) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Find(spell.Nanoline, out Buff buff))
            {
                //Don't cast if weaker than existing
                if (spell.StackingOrder < buff.StackingOrder) { return false; }

                //Don't cast if greater than 10% time remaining
                if (spell.Nanoline == buff.Nanoline && buff.RemainingTime / buff.TotalTime > 0.1) { return false; }

                if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU)) { return false; }
            }

            if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU) { return false; }

            return true;
        }

        protected bool CanCast(Spell spell)
        {
            return spell.Cost < DynelManager.LocalPlayer.Nano;
        }

        public static void CancelBuffs(int[] buffsToCancel)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs)
            {
                if (buffsToCancel.Contains(buff.Identity.Instance))
                    buff.Remove();
            }
        }

        protected bool IsSettingEnabled(string settingName)
        {
            return settings[settingName].AsBool();
        }

        protected bool HasNCU(Spell spell, SimpleChar target)
        {
            return MultiboxHelper.SettingsController.GetRemainingNCU(target.Identity) > spell.NCU;
        }

        private void TeleportEnded(object sender, EventArgs e)
        {
            _lastCombatTime = double.MinValue;
        }

        protected void CancelHostileAuras(int[] auras)
        {
            if (Time.NormalTime - _lastCombatTime > 5)
            {
                CancelBuffs(auras);
            }
        }

        protected bool IsInsideInnerSanctum()
        {
            return DynelManager.LocalPlayer.Buffs.Any(buff => buff.Identity.Instance == RelevantNanos.InnerSanctumDebuff);
        }

        #endregion

        #region Misc

        [Flags]
        public enum CharacterWieldedWeapon
        {
            Fists = 0x0,            // 0x00000000000000000000b Fists / invalid
            MartialArts = 0x01,             // 0x00000000000000000001b martialarts / fists
            Melee = 0x02,             // 0x00000000000000000010b
            Ranged = 0x04,            // 0x00000000000000000100b
            Bow = 0x08,               // 0x00000000000000001000b
            Smg = 0x10,               // 0x00000000000000010000b
            Edged1H = 0x20,           // 0x00000000000000100000b
            Blunt1H = 0x40,           // 0x00000000000001000000b
            Edged2H = 0x80,           // 0x00000000000010000000b
            Blunt2H = 0x100,          // 0x00000000000100000000b
            Piercing = 0x200,         // 0x00000000001000000000b
            Pistol = 0x400,           // 0x00000000010000000000b
            AssaultRifle = 0x800,     // 0x00000000100000000000b
            Rifle = 0x1000,           // 0x00000001000000000000b
            Shotgun = 0x2000,         // 0x00000010000000000000b
            Grenade = 0x8000,     // 0x00000100000000000000b // 0x00001000000000000000b grenade / martial arts
            MeleeEnergy = 0x4000,     // 0x00001000000000000000b // 0x00000100000000000000b
            RangedEnergy = 0x10000,   // 0x00010000000000000000b
            Grenade2 = 0x20000,        // 0x00100000000000000000b
            HeavyWeapons = 0x40000,   // 0x01000000000000000000b
        }

        // This will eventually be done dynamically but for now I will implement
        // it statically so we can have it functional
        private Stat GetSkillLockStat(Item item)
        {
            switch (item.HighId)
            {
                case RelevantItems.ReflectGraft:
                    return Stat.SpaceTime;
                case RelevantItems.UponAWaveOfSummerLow:
                case RelevantItems.UponAWaveOfSummerHigh:
                    return Stat.Riposte;
                case RelevantItems.FlowerOfLifeLow:
                case RelevantItems.FlowerOfLifeHigh:
                case RelevantItems.BlessedWithThunderLow:
                case RelevantItems.BlessedWithThunderHigh:
                    return Stat.MartialArts;
                case RelevantItems.HealthAndNanoStim1:
                case RelevantItems.HealthAndNanoStim200:
                case RelevantItems.HealthAndNanoStim400:
                    return Stat.FirstAid;
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
                case RelevantItems.GnuffsEternalRiftCrystal:
                    return Stat.MapNavigation;
                case RelevantItems.HSR1:
                case RelevantItems.HSR2:
                    return Stat.Grenade;
                default:
                    throw new Exception($"No skill lock stat defined for item id {item.HighId}");
            }
        }


        private static class RelevantItems
        {
            public const int ReflectGraft = 95225;
            public const int FlurryOfBlowsLow = 85907;
            public const int FlurryOfBlowsHigh = 85908;
            public const int StrengthOfTheImmortal = 305478;
            public const int MightOfTheRevenant = 206013;
            public const int BarrowStrength = 204653;
            public const int LavaCapsule = 245990;
            public const int WitheredFlesh = 204698;
            public const int DesecratedFlesh = 305476;
            public const int AssaultClassTank = 156576;
            public const int HSR1 = 164780;
            public const int HSR2 = 164781;
            public const int KizzermoleGumboil = 245323;
            public const int SteamingHotCupOfEnhancedCoffee = 157296;
            public const int DreadlochEnduranceBooster = 267168;
            public const int DreadlochEnduranceBoosterNanomageEdition = 267167;
            public const int MeteoriteSpikes = 244204;
            public const int FlowerOfLifeLow = 70614;
            public const int FlowerOfLifeHigh = 204326;
            public const int UponAWaveOfSummerLow = 205405;
            public const int UponAWaveOfSummerHigh = 205406;
            public const int BlessedWithThunderLow = 70612;
            public const int BlessedWithThunderHigh = 204327;
            public const int GnuffsEternalRiftCrystal = 303179;
            public const int RezCan = 301070;
            public const int ExperienceStim = 288769;
            public const int PremSitKit = 297274;
            public const int AreteSitKit = 292256;
            public const int SitKit1 = 291082;
            public const int SitKit100 = 291083;
            public const int SitKit200 = 291084;
            public const int SitKit300 = 293296;
            public const int SitKit400 = 293297;
            public const int FreeStim1 = 204103;
            public const int FreeStim50 = 204104;
            public const int FreeStim100 = 204105;
            public const int FreeStim200 = 204106;
            public const int FreeStim300 = 204107;
            public const int HealthAndNanoStim1 = 291043;
            public const int HealthAndNanoStim200 = 291044;
            public const int HealthAndNanoStim400 = 291045;
            public const int AmmoBoxEnergy = 303138;
            public const int AmmoBoxShotgun = 303141;
            public const int AmmoBoxBullets = 303137;
            public const int AmmoBoxArrows = 303136;
        };

        private static class RelevantNanos
        {
            public const int FountainOfLife = 302907;
            public const int DanceOfFools = 210159;
            public const int Limber = 210158;
            public const int CompositeAttribute = 223372;
            public const int CompositeNano = 223380;
            public const int CompositeUtility = 287046;
            public const int CompositeMartialProwess = 302158;
            public const int CompositeMartial = 302158;
            public const int CompositeMelee = 223360;
            public const int CompositePhysicalSpecial = 215264;
            public const int CompositeRanged = 223348;
            public const int CompositeRangedSpecial = 223364;
            public const int InnerSanctumDebuff = 206387;
            public const int InsightIntoSL = 268610;
        }

        public class PetSpellData
        {
            public int ShellId;
            public int ShellId2;
            public PetType PetType;

            public PetSpellData(int shellId, PetType petType)
            {
                ShellId = shellId;
                PetType = petType;
            }
            public PetSpellData(int shellId, int shellId2, PetType petType)
            {
                ShellId = shellId;
                ShellId2 = shellId2;
                PetType = petType;
            }
        }
        #endregion
    }
}