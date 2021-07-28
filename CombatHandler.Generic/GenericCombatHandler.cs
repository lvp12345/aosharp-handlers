using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AOSharp.Character;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using Character.State;
using static CombatHandler.Generic.PerkCondtionProcessors;

namespace CombatHandler.Generic
{
    public class GenericCombatHandler : AOSharp.Core.Combat.CombatHandler
    {
        private const float PostZonePetCheckBuffer = 5;
        private double _lastPetSyncTime = Time.NormalTime;
        protected double _lastZonedTime = Time.NormalTime;
        protected double _lastCombatTime = double.MinValue;
        public int EvadeCycleTimeoutSeconds = 180;
        private string pluginDir = "";
        protected Settings settings;

        public GenericCombatHandler(string pluginDir)
        {
            this.pluginDir = pluginDir;
            Game.TeleportEnded += TeleportEnded;
            settings = new Settings("CombatHandler");


            //Chat.RegisterCommand("grid", UseGridCanCommand);

            RegisterPerkProcessors();
            RegisterPerkProcessor(PerkHash.Limber, Limber, CombatActionPriority.High);
            RegisterPerkProcessor(PerkHash.DanceOfFools, DanceOfFools, CombatActionPriority.High);

            RegisterItemProcessor(RelevantItems.FlurryOfBlowsLow, RelevantItems.FlurryOfBlowsLow, DamageItem);
            RegisterItemProcessor(RelevantItems.FlurryOfBlowsHigh, RelevantItems.FlurryOfBlowsHigh, DamageItem);
            RegisterItemProcessor(RelevantItems.StrengthOfTheImmortal, RelevantItems.StrengthOfTheImmortal, DamageItem);
            RegisterItemProcessor(RelevantItems.MightOfTheRevenant, RelevantItems.MightOfTheRevenant, DamageItem);
            RegisterItemProcessor(RelevantItems.BarrowStrength, RelevantItems.BarrowStrength, DamageItem);
            RegisterItemProcessor(RelevantItems.MeteoriteSpikes, RelevantItems.MeteoriteSpikes, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.LavaCapsule, RelevantItems.LavaCapsule, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.HSR1, RelevantItems.HSR2, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.KizzermoleGumboil, RelevantItems.KizzermoleGumboil, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.SteamingHotCupOfEnhancedCoffee, RelevantItems.SteamingHotCupOfEnhancedCoffee, Coffee);
            RegisterItemProcessor(RelevantItems.GnuffsEternalRiftCrystal, RelevantItems.GnuffsEternalRiftCrystal, DamageItem);
            RegisterItemProcessor(RelevantItems.UponAWaveOfSummerLow, RelevantItems.UponAWaveOfSummerHigh, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.BlessedWithThunderLow, RelevantItems.BlessedWithThunderHigh, TargetedDamageItem);

            RegisterItemProcessor(RelevantItems.DreadlochEnduranceBooster, RelevantItems.DreadlochEnduranceBooster, EnduranceBooster, CombatActionPriority.High);
            RegisterItemProcessor(RelevantItems.DreadlochEnduranceBoosterNanomageEdition, RelevantItems.DreadlochEnduranceBoosterNanomageEdition, EnduranceBooster, CombatActionPriority.High);
            RegisterItemProcessor(RelevantItems.HealthAndNanoStimLow, RelevantItems.HealthAndNanoStimHigh, HealthAndNanoStim, CombatActionPriority.High);
            RegisterItemProcessor(RelevantItems.RezCan, RelevantItems.RezCan, UseRezCan);
            RegisterItemProcessor(RelevantItems.FlurryOfBlows25, RelevantItems.FlurryOfBlows200, UseFlurry);
            RegisterItemProcessor(RelevantItems.FlowerOfLifeLow, RelevantItems.FlowerOfLifeHigh, FlowerOfLife);
            RegisterItemProcessor(RelevantItems.ExperienceStim, RelevantItems.ExperienceStim, ExperienceStim);

            RegisterItemProcessor(RelevantItems.PremSitKit, RelevantItems.PremSitKit, SitKit);
            RegisterItemProcessor(RelevantItems.SitKit1, RelevantItems.SitKit100, SitKit);
            RegisterItemProcessor(RelevantItems.SitKit100, RelevantItems.SitKit200, SitKit);
            RegisterItemProcessor(RelevantItems.SitKit200, RelevantItems.SitKit300, SitKit);

            RegisterItemProcessor(RelevantItems.FreeStim1, RelevantItems.FreeStim50, UseFreeStim);
            RegisterItemProcessor(RelevantItems.FreeStim50, RelevantItems.FreeStim100, UseFreeStim);
            RegisterItemProcessor(RelevantItems.FreeStim100, RelevantItems.FreeStim200, UseFreeStim);
            RegisterItemProcessor(RelevantItems.FreeStim200, RelevantItems.FreeStim200, UseFreeStim);
            RegisterItemProcessor(RelevantItems.FreeStim200, RelevantItems.FreeStim300, UseFreeStim);


            RegisterItemProcessor(RelevantItems.AmmoBoxArrows, RelevantItems.AmmoBoxArrows, AmmoBoxArrows);
            RegisterItemProcessor(RelevantItems.AmmoBoxBullets, RelevantItems.AmmoBoxBullets, AmmoBoxBullets);
            RegisterItemProcessor(RelevantItems.AmmoBoxEnergy, RelevantItems.AmmoBoxEnergy, AmmoBoxEnergy);
            RegisterItemProcessor(RelevantItems.AmmoBoxShotgun, RelevantItems.AmmoBoxShotgun, AmmoBoxShotgun);

            RegisterSpellProcessor(RelevantNanos.CompositeNano, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeAttribute, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeUtility, GenericBuff);

            if (CharacterState.GetWeaponType(DynelManager.LocalPlayer.Identity) == CharacterWeaponType.MELEE)
            {
                //We are melee
                RegisterSpellProcessor(RelevantNanos.CompositeMartial, GenericBuffExcludeInnerSanctum);
                RegisterSpellProcessor(RelevantNanos.CompositeMelee, GenericBuff);
                RegisterSpellProcessor(RelevantNanos.CompositePhysicalSpecial, GenericBuff);
            }


            if (CharacterState.GetWeaponType(DynelManager.LocalPlayer.Identity) == CharacterWeaponType.RANGED)
            {
                //We are ranged
                RegisterSpellProcessor(RelevantNanos.CompositeRanged, GenericBuff);
                RegisterSpellProcessor(RelevantNanos.CompositeRangedSpecial, GenericBuff);
            }

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

            Game.TeleportEnded += OnZoned;
        }

        protected bool HealOverTimeTeamBuff(string toggleName, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledTeamBuff(toggleName, spell, fightingTarget,  target => HasBuffNanoLine(NanoLine.HealOverTime, target) || HasBuffNanoLine(NanoLine.MongoBuff, target), ref actionTarget);
        }
        
        protected void RegisterPerkProcessors()
        {
            PerkAction.List.ForEach(perkAction => RegisterPerkAction(perkAction));
        }

        private void RegisterPerkAction(PerkAction perkAction)
        {
            GenericPerkConditionProcessor perkConditionProcessor = PerkCondtionProcessors.GetPerkConditionProcessor(perkAction);

            if(perkConditionProcessor != null)
            {
                RegisterPerkProcessor(perkAction.Hash, ToPerkConditionProcessor(perkConditionProcessor));
            }
        }

        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, pluginDir + "\\UI\\" + xmlName, settings);
        }

        protected bool TauntTool(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !IsSettingEnabled("UseTauntTool"))
            {
                return false;
            }

            if (TauntTools.CanUseTauntTool())
            {
                actionTarget.Target = fightingTarget;
                actionTarget.ShouldSetTarget = true;
            }

            return false;
        }

        private bool SitKit(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Treatment))
                return false;

            if (DynelManager.LocalPlayer.HealthPercent > 65 && DynelManager.LocalPlayer.NanoPercent > 65)
                return false;

            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = true;
            return true;
        }


        private bool ExperienceStim(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.FirstAid))
                return false;

            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = false;
            return true;

        }

        protected bool LEProc(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perk.Name)
                {
                    return false;
                }
            }
            return true;
        }

        protected bool GenericBuffExcludeInnerSanctum(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum())
            {
                return false;
            }
            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool CombatBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !CanCast(spell))
            {
                return false;
            }

            if (DynelManager.LocalPlayer.Buffs.Find(spell.Nanoline, out Buff buff))
            {
                //Don't cast if weaker than existing
                if (spell.StackingOrder < buff.StackingOrder)
                {
                    return false;
                }

                //Don't cast if greater than 10% time remaining
                if (spell.Nanoline == buff.Nanoline && buff.RemainingTime / buff.TotalTime > 0.1)
                {

                    return false;
                }

                if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU))
                {
                    return false;
                }
            }
            else
            {
                if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU)
                {
                    return false;
                }
            }
            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = DynelManager.LocalPlayer;
            return true;
        }

        protected bool GenericBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell) || spell.Name.Contains("Veteran"))
            {
                return false;
            }

            if (DynelManager.LocalPlayer.Buffs.Find(spell.Nanoline, out Buff buff))
            {
                //Don't cast if weaker than existing
                if (spell.StackingOrder < buff.StackingOrder)
                {
                    return false;
                }

                //Don't cast if greater than 10% time remaining
                if (spell.Nanoline == buff.Nanoline && buff.RemainingTime / buff.TotalTime > 0.1)
                {

                    return false;
                }

                if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU))
                {
                    return false;
                }
            }
            else
            {
                if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU)
                {
                    return false;
                }
            }
            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = DynelManager.LocalPlayer;
            return true;
        }

        protected bool ToggledDebuffTarget(string settingName, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            // Check if we are fighting and if debuffing is enabled
            if (fightingTarget == null || !IsSettingEnabled(settingName))
            {
                return false;
            }

            return !fightingTarget.Buffs.Any(buff => ShouldRefreshBuff(spell, buff));
        }

        protected bool ToggledDebuffTarget(string settingName, Spell spell, SimpleChar fightingTarget, NanoLine debuffNanoLine, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            // Check if we are fighting and if debuffing is enabled
            if (fightingTarget == null || !IsSettingEnabled(settingName))
            {
                return false;
            }

            return !fightingTarget.Buffs.Any(buff => buff.Nanoline == debuffNanoLine);

        }

        protected bool ShouldRefreshBuff(Spell spell, Buff buff)
        {
            if(buff.Nanoline != spell.Nanoline)
            {
                return false;
            }

            return buff.StackingOrder > spell.StackingOrder || buff.StackingOrder == spell.StackingOrder && buff.RemainingTime / buff.TotalTime > 0.1;
        }

        protected bool ToggledTeamBuff(string settingName, Spell spell, SimpleChar fightingTarget, Func<SimpleChar, bool> hasBuffCheck, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled(settingName))
            {
                return false;
            }
            return TeamBuff(spell, fightingTarget, ref actionTarget, hasBuffCheck);
        }

        protected bool TeamBuffExcludeInnerSanctum(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(IsInsideInnerSanctum())
            {
                return false;
            }
            return TeamBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool TeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return TeamBuff(spell, fightingTarget, ref actionTarget, null);
        }

        private bool SpecialAttackTeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, Func<SpecialAttacks, bool> specialAttackCheck)
        {
            if (fightingTarget != null || !CanCast(spell))
            {
                return false;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => HasNCU(spell, c))
                    .Where(c => !HasBuff(spell, c))
                    .Where(c => specialAttackCheck == null || specialAttackCheck.Invoke(CharacterState.GetSpecialAttacks(c.Identity)))
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected bool TeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, Func<SimpleChar, bool> hasBuffCheck)
        {
            if (fightingTarget != null || !CanCast(spell))
            {
                return false;
            }

            if(hasBuffCheck == null)
            {
                hasBuffCheck = c => HasBuff(spell, c);
            }

            if (!hasBuffCheck.Invoke(DynelManager.LocalPlayer) && HasNCU(spell, DynelManager.LocalPlayer))
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => HasNCU(spell, c))
                    .Where(c => !hasBuffCheck.Invoke(c))
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected bool TeamBuffInitDoc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, Func<SimpleChar, bool> hasBuffCheck)
        {
            if (fightingTarget != null || !CanCast(spell))
            {
                return false;
            }

            if (hasBuffCheck == null)
            {
                hasBuffCheck = c => HasBuff(spell, c);
            }

            if (!hasBuffCheck.Invoke(DynelManager.LocalPlayer) && HasNCU(spell, DynelManager.LocalPlayer))
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => HasNCU(spell, c))
                    .Where(c => !hasBuffCheck.Invoke(c))
                    .Where(c => c.Profession == Profession.Doctor || c.Profession == Profession.NanoTechnician)
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected bool TeamBuffInitEngi(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, Func<SimpleChar, bool> hasBuffCheck, CharacterWeaponType supportedWeaponType)
        {
            if (fightingTarget != null || !CanCast(spell))
            {
                return false;
            }

            if (hasBuffCheck == null)
            {
                hasBuffCheck = c => HasBuff(spell, c);
            }

            if (!hasBuffCheck.Invoke(DynelManager.LocalPlayer) && HasNCU(spell, DynelManager.LocalPlayer))
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => HasNCU(spell, c))
                    .Where(c => !hasBuffCheck.Invoke(c))
                    .Where(c => c.Profession != Profession.Doctor && c.Profession != Profession.NanoTechnician)
                    .Where(c => CharacterState.GetWeaponType(c.Identity) == supportedWeaponType)
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }
        protected bool TeamBuffInitSol(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, Func<SimpleChar, bool> hasBuffCheck)
        {
            if (fightingTarget != null || !CanCast(spell))
            {
                return false;
            }

            if (hasBuffCheck == null)
            {
                hasBuffCheck = c => HasBuff(spell, c);
            }

            if (!hasBuffCheck.Invoke(DynelManager.LocalPlayer) && HasNCU(spell, DynelManager.LocalPlayer))
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => HasNCU(spell, c))
                    .Where(c => !hasBuffCheck.Invoke(c))
                    .Where(c => c.Profession != Profession.Doctor && c.Profession != Profession.NanoTechnician)
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected bool TeamBuffOverAllSelf(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, Func<SimpleChar, bool> hasBuffCheck, CharacterWeaponType supportedWeaponType)
        {
            if (fightingTarget != null || !CanCast(spell))
            {
                return false;
            }

            if (hasBuffCheck == null)
            {
                hasBuffCheck = c => HasBuff(spell, c);
            }

            if (!hasBuffCheck.Invoke(DynelManager.LocalPlayer) && HasNCU(spell, DynelManager.LocalPlayer) && CharacterState.GetWeaponType(DynelManager.LocalPlayer.Identity) == supportedWeaponType)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        protected bool TeamBuffOverAll(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, Func<SimpleChar, bool> hasBuffCheck, CharacterWeaponType supportedWeaponType)
        {
            if (fightingTarget != null || !CanCast(spell))
            {
                return false;
            }

            if (hasBuffCheck == null)
            {
                hasBuffCheck = c => HasBuff(spell, c);
            }

            if (!hasBuffCheck.Invoke(DynelManager.LocalPlayer) && HasNCU(spell, DynelManager.LocalPlayer) && CharacterState.GetWeaponType(DynelManager.LocalPlayer.Identity) == supportedWeaponType)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => HasNCU(spell, c))
                    .Where(c => !hasBuffCheck.Invoke(c))
                    .Where(c => supportedWeaponType == CharacterWeaponType.UNAVAILABLE || CharacterState.GetWeaponType(c.Identity) == supportedWeaponType)
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected bool TeamBuffWeaponCheck(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, Func<SimpleChar, bool> hasBuffCheck, CharacterWieldedWeapon supportedWeaponType, CharacterWieldedWeapon supportedWeaponType2, CharacterWieldedWeapon supportedWeaponType3, CharacterWieldedWeapon supportedWeaponType4)
        {
            if (fightingTarget != null || !CanCast(spell))
            {
                return false;
            }

            if (hasBuffCheck == null)
            {
                hasBuffCheck = c => HasBuff(spell, c);
            }

            if (!hasBuffCheck.Invoke(DynelManager.LocalPlayer) && HasNCU(spell, DynelManager.LocalPlayer) && (CharacterState.GetWieldedWeapon(DynelManager.LocalPlayer) == supportedWeaponType || CharacterState.GetWieldedWeapon(DynelManager.LocalPlayer) == supportedWeaponType2 || CharacterState.GetWieldedWeapon(DynelManager.LocalPlayer) == supportedWeaponType3 || CharacterState.GetWieldedWeapon(DynelManager.LocalPlayer) == supportedWeaponType4))
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => HasNCU(spell, c))
                    .Where(c => !hasBuffCheck.Invoke(c))
                    .Where(c => CharacterState.GetWieldedWeaponOther(c) == supportedWeaponType || CharacterState.GetWieldedWeaponOther(c) == supportedWeaponType2 || CharacterState.GetWieldedWeaponOther(c) == supportedWeaponType3 || CharacterState.GetWieldedWeaponOther(c) == supportedWeaponType4)
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected bool RangedTeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return TeamBuffOverAll(spell, fightingTarget, ref actionTarget, null, CharacterWeaponType.RANGED);
        }

        protected bool MeleeTeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return TeamBuffOverAll(spell, fightingTarget, ref actionTarget, null, CharacterWeaponType.MELEE);
        }

        protected bool RangedTeamBuffSelf(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return TeamBuffOverAllSelf(spell, fightingTarget, ref actionTarget, null, CharacterWeaponType.RANGED);
        }

        protected bool MeleeTeamBuffSelf(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return TeamBuffOverAllSelf(spell, fightingTarget, ref actionTarget, null, CharacterWeaponType.MELEE);
        }

        protected bool PistolTeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return TeamBuffWeaponCheck(spell, fightingTarget, ref actionTarget, hasBuffCheck: target => HasBuffNanoLine(NanoLine.PistolBuff, target), CharacterWieldedWeapon.Pistol, CharacterWieldedWeapon.PistolAndAssaultRifle, CharacterWieldedWeapon.PistolAndShotgun, CharacterWieldedWeapon.Bandaid);
        }

        protected bool BurstTeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return SpecialAttackTeamBuff(spell, fightingTarget, ref actionTarget, specialAttacks => specialAttacks.HasBurst);
        }

        protected bool CanCast(Spell spell)
        {
            return spell.Cost < DynelManager.LocalPlayer.Nano;
        }

        protected bool IsSettingEnabled(string settingName)
        {
            return settings[settingName].AsBool();
        }

        protected bool HasNCU(Spell spell, SimpleChar target)
        {
            return CharacterState.GetRemainingNCU(target.Identity) > spell.NCU;
        }

        protected bool HasBuff(Spell spell, SimpleChar target)
        {
            return target.Buffs.Any(buff => ShouldRefreshBuff(spell, buff));
        }

        protected bool HasBuffNanoLine(NanoLine nanoLine, SimpleChar target)
        {
            return target.Buffs.Contains(nanoLine);
        }

        private void TeleportEnded(object sender, EventArgs e)
        {
            _lastCombatTime = double.MinValue;
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            if (DynelManager.LocalPlayer.IsAttacking || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 0)
            {
                _lastCombatTime = Time.NormalTime;
            }
        }

        protected void CancelHostileAuras(int[] auras)
        {
            if (Time.NormalTime - _lastCombatTime > 5)
            {
                CancelBuffs(auras);
            }
        }

        private bool FlowerOfLife(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (fightingtarget == null)
                return false;

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item)))
                return false;

            int approximateHealing = item.QualityLevel * 10;

            return DynelManager.LocalPlayer.MissingHealth > approximateHealing;
        }

        private bool HealthAndNanoStim(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            //if (fightingtarget == null)
            //    return false;

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item)) || DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) >= 8)
                return false;

            if (DynelManager.LocalPlayer.Buffs.Contains(280470) || DynelManager.LocalPlayer.Buffs.Contains(258231))
                return false;

            actiontarget.ShouldSetTarget = true;
            actiontarget.Target = DynelManager.LocalPlayer;

            //int approximateHealing = item.QualityLevel * 12;

            return DynelManager.LocalPlayer.HealthPercent < 80 || DynelManager.LocalPlayer.NanoPercent < 80/* || DynelManager.LocalPlayer.MissingHealth > (approximateHealing * 2) || DynelManager.LocalPlayer.MissingNano > (approximateHealing * 2)*/;
        }

        private bool AmmoBoxBullets(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            Item bulletsammo = Inventory.Items
                .Where(c => c.Name.Contains("Bullets"))
                .Where(c => !c.Name.Contains("Crate"))
                .FirstOrDefault();

            actiontarget.ShouldSetTarget = false;

            return bulletsammo == null && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStatWeaponSmithing());
        }

        private bool AmmoBoxEnergy(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            Item energyammo = Inventory.Items
                .Where(c => c.Name.Contains("Energy"))
                .Where(c => !c.Name.Contains("Crate"))
                .FirstOrDefault();

            actiontarget.ShouldSetTarget = false;

            return energyammo == null && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStatWeaponSmithing());
        }

        private bool AmmoBoxShotgun(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            Item shotgunammo = Inventory.Items
                .Where(c => c.Name.Contains("Shotgun"))
                .Where(c => !c.Name.Contains("Crate"))
                .FirstOrDefault();

            actiontarget.ShouldSetTarget = false;

            return shotgunammo == null && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStatWeaponSmithing());
        }

        private bool AmmoBoxArrows(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            Item arrowammo = Inventory.Items
                .Where(c => c.Name.Contains("Arrows"))
                .Where(c => !c.Name.Contains("Crate"))
                .FirstOrDefault();

            actiontarget.ShouldSetTarget = false;

            return arrowammo == null && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStatWeaponSmithing());
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
                    .OrderByDescending(c => c.GetStat(Stat.NumFightingOpponents))
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    actiontarget.Target = dyingTeamMember;
                    return true;
                }
            }

            return false;
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
            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0)
                return false;

            // don't use if we have another major absorb running
            // we could check remaining absorb stat to be slightly more effective
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon))
                return false;

            return true;
        }

        protected virtual bool TargetedDamageItem(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = true;
            return DamageItem(item, fightingTarget, ref actionTarget);
        }

        protected virtual bool DamageItem(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return !DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item)) && fightingTarget != null;
        }

        protected virtual bool Coffee(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(NanoLine.FoodandDrinkBuffs))
                return DamageItem(item, fightingTarget, ref actionTarget);

            return false;
        }

        private bool Limber(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.DanceOfFools, out Buff dof) && dof.RemainingTime > 12.5f)
                return false;

            // stop cycling if we haven't fought anything for over 10 minutes
            if (Time.NormalTime - _lastCombatTime > EvadeCycleTimeoutSeconds)
                return false;

            return true;
        }

        private bool DanceOfFools(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.Limber, out Buff limber) || limber.RemainingTime > 12.5f)
                return false;

            // stop cycling if we haven't fought anything for over 10 minutes
            if (Time.NormalTime - _lastCombatTime > EvadeCycleTimeoutSeconds)
                return false;

            return true;
        }

        protected static void CancelBuffs(int[] buffsToCancel)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs)
            {
                if (buffsToCancel.Contains(buff.Identity.Instance))
                    buff.Remove();
            }
        }

        protected bool ToggledDebuffOthersInCombat(string toggleName, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled(toggleName))
            {
                return false;
            }

            SimpleChar debuffTarget = DynelManager.Characters
                .Where(c => !debuffTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
                .Where(c => !c.IsPlayer).Where(c => !c.IsPet) //Is not player or a pet
                .Where(c => c.IsAttacking) //Is in combat
                .Where(c => !c.Buffs.Contains(301844)) // doesn't have ubt in ncu
                .Where(c => c.IsValid).Where(c => c.IsInLineOfSight).Where(c => c.IsInAttackRange()) //Is in range for debuff (we assume weapon range == debuff range)
                .Where(c => NeedsDebuffRefresh(spell, c)) //Needs debuff refreshed
                .FirstOrDefault();

            if (debuffTarget != null)
            {
                actionTarget = (debuffTarget, true);
                return true;
            }

            return false;
        }

        private bool NeedsDebuffRefresh(Spell spell, SimpleChar target)
        {
            if (target == null)
            {
                return false;
            }
            //Check the remaining time on debuffs. On the enemy target
            return !target.Buffs.Where(buff => buff.Name == spell.Name)
                .Where(buff => buff.RemainingTime > 1)
                .Any();
        }

        protected bool IsInsideInnerSanctum()
        {
            return DynelManager.LocalPlayer.Buffs.Any(buff => buff.Identity.Instance == RelevantNanos.InnerSanctumDebuff);
        }

        protected bool NoShellPetSpawner(PetType petType, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanSpawnPets(petType))
            {
                return false;
            }

            actionTarget.ShouldSetTarget = false;
            return true;
        }

        protected bool PetSpawner(Dictionary<int, PetSpellData> petData, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!petData.ContainsKey(spell.Identity.Instance))
            {
                return false;
            }

            PetType petType = petData[spell.Identity.Instance].PetType;

            //Ignore spell if we already have the shell in our inventory
            if (Inventory.Find(petData[spell.Identity.Instance].ShellId, out Item shell))
            {
                return false;
            }

            return NoShellPetSpawner(petType, spell, fightingTarget, ref actionTarget);
        }

        protected virtual bool PetSpawnerItem(Dictionary<int, PetSpellData> petData, Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("SpawnPets"))
                return false;

            if (!CanLookupPetsAfterZone())
                return false;

            if (!petData.Values.Any(x => (x.ShellId == item.LowId || x.ShellId == item.HighId) && !DynelManager.LocalPlayer.Pets.Any(p => p.Type == x.PetType)))
                return false;

            actionTarget.ShouldSetTarget = false;
            return true;
        }

        protected bool CanSpawnPets(PetType petType)
        {
            if (!IsSettingEnabled("SpawnPets") || !CanLookupPetsAfterZone() || PetAlreadySpawned(petType))
            {
                return false;
            }

            return true;
        }

        private bool PetAlreadySpawned(PetType petType)
        {
            return DynelManager.LocalPlayer.Pets.Any(x => (x.Type == PetType.Unknown || x.Type == petType));
        }

        protected Pet FindPetThat(Func<Pet, bool> Filter)
        {
            return DynelManager.LocalPlayer.Pets
                .Where(pet => pet.Character != null && pet.Character.Buffs != null)
                .Where(pet => pet.Type == PetType.Support || pet.Type == PetType.Attack || pet.Type == PetType.Heal)
                .Where(pet => Filter.Invoke(pet))
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
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone())
            {
                return false;
            }

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
            if(pet != null)
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

        private PerkConditionProcessor ToPerkConditionProcessor(GenericPerkConditionProcessor genericPerkConditionProcessor)
        {
            return (PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) => genericPerkConditionProcessor(perkAction, fightingTarget, ref actionTarget);
        }

        private bool UseRezCan(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            actiontarget.ShouldSetTarget = false;

            return !DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStatFirstAid());
        }

        private bool UseFlurry(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            actiontarget.ShouldSetTarget = false;

            return !DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStatAggDef()) && DynelManager.LocalPlayer.IsAttacking;
        }

        private bool UseFreeStim(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            actiontarget.ShouldSetTarget = true;

            return (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.Root) || DynelManager.LocalPlayer.Buffs.Contains(NanoLine.Snare)) && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item));

        }

        private void OnZoned(object s, EventArgs e)
        {
            _lastZonedTime = Time.NormalTime;
        }

        private Stat GetSkillLockStatFirstAid()
        {
            return Stat.FirstAid;
        }

        private Stat GetSkillLockStatAggDef()
        {
            return Stat.AggDef;
        }

        private Stat GetSkillLockStatWeaponSmithing()
        {
            return Stat.WeaponSmithing;
        }

        // This will eventually be done dynamically but for now I will implement
        // it statically so we can have it functional
        private Stat GetSkillLockStat(Item item)
        {
            switch (item.HighId)
            {
                case RelevantItems.UponAWaveOfSummerLow:
                case RelevantItems.UponAWaveOfSummerHigh:
                    return Stat.Riposte;
                case RelevantItems.FlowerOfLifeLow:
                case RelevantItems.FlowerOfLifeHigh:
                case RelevantItems.BlessedWithThunderLow:
                case RelevantItems.BlessedWithThunderHigh:
                    return Stat.MartialArts;
                case RelevantItems.HealthAndNanoStimLow:
                case RelevantItems.HealthAndNanoStimHigh:
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
                case RelevantItems.FreeStim200:
                    return Stat.FirstAid;
                default:
                    throw new Exception($"No skill lock stat defined for item id {item.HighId}");
            }
        }

        private static class RelevantItems
        {
            public const int FlurryOfBlowsLow = 85907;
            public const int FlurryOfBlowsHigh = 85908;
            public const int StrengthOfTheImmortal = 305478;
            public const int MightOfTheRevenant = 206013;
            public const int BarrowStrength = 204653;
            public const int LavaCapsule = 245990;
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
            public const int HealthAndNanoStimLow = 291043;
            public const int HealthAndNanoStimHigh = 291044;
            public const int RezCan = 301070;
            public const int ExperienceStim = 288769;
            public const int PremSitKit = 297274;
            public const int SitKit1 = 291082;
            public const int SitKit100 = 291083;
            public const int SitKit200 = 291084;
            public const int SitKit300 = 293296;
            public const int FreeStim1 = 204103;
            public const int FreeStim50 = 204104;
            public const int FreeStim100 = 204105;
            public const int FreeStim200 = 204106;
            public const int FreeStim300 = 204107;
            public const int AmmoBoxEnergy = 303138;
            public const int AmmoBoxShotgun = 303141;
            public const int AmmoBoxBullets = 303137;
            public const int AmmoBoxArrows = 303136;
            public const int FlurryOfBlows25 = 85907;
            public const int FlurryOfBlows200 = 85908;
        };

        private static class RelevantNanos
        {
            public const int FountainOfLife = 302907;
            public const int DanceOfFools = 210159;
            public const int Limber = 210158;
            public const int CompositeAttribute = 223372;
            public const int CompositeNano = 223380;
            public const int CompositeUtility = 287046;
            public const int CompositeMartial = 302158;
            public const int CompositeMelee = 223360;
            public const int CompositePhysicalSpecial = 215264;
            public const int CompositeRanged = 223348;
            public const int CompositeRangedSpecial = 223364;
            public const int InnerSanctumDebuff = 206387;
            public const int AdvAAOMorph = 85070;
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

        protected static HashSet<string> debuffTargetsToIgnore = new HashSet<string>
        {
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
                    "Essence Fragment"
        };

        private static readonly List<string> AoeRootSnareSpamTargets = new List<string>() { "Flaming Vengeance", "Hand of the Colonel" };

        protected bool IsAoeRootSnareSpamTarget(SimpleChar target)
        {
            return AoeRootSnareSpamTargets.Contains(target.Name);
        }
    }
}