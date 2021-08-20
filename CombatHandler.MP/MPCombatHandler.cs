using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Desu
{
    public class MPCombatHandler : GenericCombatHandler
    {
        private double _lastSwitchedHealTime = 0;
        private double _lastSwitchedMezzTime = 0;

        public MPCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("SpawnPets", true);
            settings.AddVariable("BuffPets", true);
            settings.AddVariable("BuffInterruptChance", false);
            settings.AddVariable("UseDamageDebuffs", true);
            settings.AddVariable("UseNanoResistanceDebuff", true);
            settings.AddVariable("UseNanoShutdownDebuff", true);
            settings.AddVariable("UseDamageDebuffsOnOthers", false);
            settings.AddVariable("UseNukes", true);
            settings.AddVariable("NanoBuffsSelection", (int)NanoBuffsSelection.SL);
            settings.AddVariable("SummonedWeaponSelection", (int)SummonedWeaponSelection.DISABLED);
            RegisterSettingsWindow("MP Handler", "MPSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistAnticipatedEvasion, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistSuppressFury, LEProc, CombatActionPriority.Low);

            //Self buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), GenericBuffExcludeInnerSanctum);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtistBowBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Psy_IntBuff).OrderByStackingOrder(), GenericBuff);

            //Team buffs
            RegisterSpellProcessor(RelevantNanos.MPCompositeNano, MPCompositeNanoBuff);
            RegisterSpellProcessor(RelevantNanos.MatMetBuffs, MPNanoBuff);
            RegisterSpellProcessor(RelevantNanos.BioMetBuffs, MPNanoBuff);
            RegisterSpellProcessor(RelevantNanos.PsyModBuffs, MPNanoBuff);
            RegisterSpellProcessor(RelevantNanos.SenImpBuffs, MPNanoBuff);
            RegisterSpellProcessor(RelevantNanos.MatCreBuffs, MPNanoBuff);
            RegisterSpellProcessor(RelevantNanos.MatLocBuffs, MPNanoBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InterruptModifier).OrderByStackingOrder(), InterruptModifierBuff);
            RegisterSpellProcessor(RelevantNanos.CostBuffs, GenericBuff);

            //Debuffs
            RegisterSpellProcessor(RelevantNanos.WarmUpfNukes, WarmUpNuke);
            RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPDamageDebuffLineA).OrderByStackingOrder(), DamageDebuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPDamageDebuffLineB).OrderByStackingOrder(), DamageDebuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MetaPhysicistDamageDebuff).OrderByStackingOrder(), DamageDebuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MetaPhysicistDamageDebuff).OrderByStackingOrder(), MPDebuffOthersInCombat);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceDebuff_LineA).OrderByStackingOrder(), NanoResistanceDebuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoShutdownDebuff).OrderByStackingOrder(), NanoShutdownDebuff);

            //Pets
            RegisterSpellProcessor(GetAttackPetsWithSLPetsFirst(), AttackPetSpawner);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SupportPets).OrderByStackingOrder(), SupportPetSpawner);
            RegisterSpellProcessor(RelevantNanos.HealPets, HealPetSpawner);

            //Pet Buffs
            RegisterSpellProcessor(RelevantNanos.InstillDamageBuffs, InstillDamageBuff);
            RegisterSpellProcessor(RelevantNanos.ChantBuffs, ChantBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MesmerizationConstructEmpowerment).OrderByStackingOrder(), MezzPetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealingConstructEmpowerment).OrderByStackingOrder(), HealPetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AggressiveConstructEmpowerment).OrderByStackingOrder(), AttackPetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPAttackPetDamageType).OrderByStackingOrder(), DamageTypePetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDamageOverTimeResistNanos).OrderByStackingOrder(), NanoResistancePetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDefensiveNanos).OrderByStackingOrder(), DefensivePetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetHealDelta843).OrderByStackingOrder(), HealDeltaPetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetShortTermDamageBuffs).OrderByStackingOrder(), ShortTermDamagePetBuff);
            RegisterSpellProcessor(RelevantNanos.CostBuffs, CostPetBuff);

            RegisterPerkProcessor(PerkHash.ChannelRage, ChannelRage);
        }

        private bool ChannelRage(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone())
            {
                return false;
            }

            Pet petToPerk = FindPetThat(CanPerkChannelRage);
            if (petToPerk != null)
            {
                actionTarget.Target = petToPerk.Character;
                actionTarget.ShouldSetTarget = true;
                return true;
            }
            return false;
        }

        private bool CanPerkChannelRage(Pet pet)
        {
            if(pet.Type != PetType.Attack)
            {
                return false;
            }
            return !pet.Character.Buffs.Any(buff => buff.Nanoline == NanoLine.ChannelRage);
        }

        private bool NanoShutdownDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("UseNanoShutdownDebuff", spell, fightingTarget, ref actionTarget);
        }

        private bool NanoResistanceDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("UseNanoResistanceDebuff", spell, fightingTarget, ref actionTarget);
        }

        private bool DamageDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("UseDamageDebuffs", spell, fightingTarget, ref actionTarget);
        }

        private bool WarmUpNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) {
            return ToggledDebuffTarget("UseNukes", spell, fightingTarget, ref actionTarget);
        }

        private bool MPCompositeNanoBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(GetNanoBuffsSelection() != NanoBuffsSelection.SL)
            {
                return false;
            }
            return GenericBuff(spell, fightingTarget,  ref actionTarget);
        }

        private bool MPNanoBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (GetNanoBuffsSelection() != NanoBuffsSelection.RK)
            {
                return false;
            }
            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool HasAnyMPNanoLineBuff(SimpleChar target)
        {
            return target.Buffs.Contains(NanoLine.MatMetBuff) || target.Buffs.Contains(NanoLine.BioMetBuff)
                || target.Buffs.Contains(NanoLine.PsyModBuff) || target.Buffs.Contains(NanoLine.SenseImpBuff)
                || target.Buffs.Contains(NanoLine.MatCreaBuff) || target.Buffs.Contains(NanoLine.MatLocBuff);
        }

        private bool InterruptModifierBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledBuff("BuffInterruptChance", spell, fightingTarget, ref actionTarget);
        }

        private bool MPDebuffOthersInCombat(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) 
        {
            return ToggledDebuffOthersInCombat("UseDamageDebuffsOnOthers", spell, fightingTarget, ref actionTarget);
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !IsSettingEnabled("UseNukes"))
            {
                return false;
            }

            return true;
        }

        private bool MezzPetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !IsSettingEnabled("BuffPets"))
            {
                return false;
            }

            return !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.MesmerizationConstructEmpowerment);
        }

        private bool HealPetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !IsSettingEnabled("BuffPets"))
            {
                return false;
            }

            return !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.HealingConstructEmpowerment);
        }

        private bool AttackPetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !IsSettingEnabled("BuffPets"))
            {
                return false;
            }

            return !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.AggressiveConstructEmpowerment);
        }

        private bool DamageTypePetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.MPAttackPetDamageType, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }
        

        private bool ChantBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.MPPetInitiativeBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool InstillDamageBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.MPPetDamageBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool HealDeltaPetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetHealDelta843, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool DefensivePetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetDefensiveNanos, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool NanoResistancePetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetDamageOverTimeResistNanos, PetType.Attack, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.PetDamageOverTimeResistNanos, PetType.Heal, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.PetDamageOverTimeResistNanos, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool ShortTermDamagePetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetShortTermDamageBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool CostPetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.NPCostBuff, PetType.Heal, spell, fightingTarget, ref actionTarget);
        }

        private bool AttackPetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return NoShellPetSpawner(PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool SupportPetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return NoShellPetSpawner(PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool HealPetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return NoShellPetSpawner(PetType.Heal, spell, fightingTarget, ref actionTarget);
        }

        private Spell[] GetAttackPetsWithSLPetsFirst()
        {
            List<Spell> attackPetsWithoutSL = Spell.GetSpellsForNanoline(NanoLine.AttackPets).Where(spell => !RelevantNanos.SLAttackPets.Contains(spell.Identity.Instance)).OrderByStackingOrder().ToList();
            List<Spell> attackPets = RelevantNanos.SLAttackPets.Select(FindSpell).Where(spell => spell != null).ToList();
            attackPets.AddRange(attackPetsWithoutSL);
            return attackPets.ToArray();
        }

        private Spell FindSpell(int spellHash)
        {
            if (Spell.Find(spellHash, out Spell spell))
            {
                return spell;
            }
            return null;
        }

        private SimpleChar GetTargetToHeal()
        {
           if(DynelManager.LocalPlayer.HealthPercent < 90)
            {
                return DynelManager.LocalPlayer;
            }

           if(DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent < 90)
                    .OrderByDescending(c => c.HealthPercent)
                    .FirstOrDefault();
                if(dyingTeamMember != null)
                {
                    return dyingTeamMember;
                }
            }

            Pet dyingPet = DynelManager.LocalPlayer.Pets
                 .Where(pet => pet.Type == PetType.Attack || pet.Type == PetType.Social)
                 .Where(pet => pet.Character.HealthPercent < 90)
                 .OrderByDescending(pet => pet.Character.HealthPercent)
                 .FirstOrDefault();
            if(dyingPet != null)
            {
                return dyingPet.Character;
            }

            return null;
        }

        private SimpleChar GetTargetToMezz()
        {
            return DynelManager.Characters
                .Where(c => !debuffTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
                .Where(c => DynelManager.LocalPlayer.FightingTarget.Identity != c.Identity)
                .Where(c => !c.IsPlayer).Where(c => !c.IsPet) //Is not player of a pet
                .Where(c => c.IsAttacking) //Is in combat
                .Where(c => c.IsValid).Where(c => c.IsInLineOfSight).Where(c => c.IsInAttackRange()) //Is in range for debuff (we assume weapon range == debuff range)
                .FirstOrDefault();
        }

        private void AssignTargetToHealPet()
        {
            if (Time.NormalTime - _lastSwitchedHealTime > 5)
            {
                SimpleChar dyingTarget = GetTargetToHeal();
                if (dyingTarget != null)
                {
                    Pet healPet = DynelManager.LocalPlayer.Pets.Where(pet => pet.Type == PetType.Heal).FirstOrDefault();
                    if (healPet != null)
                    {
                        healPet.Heal(dyingTarget.Identity);
                        _lastSwitchedHealTime = Time.NormalTime;
                    }
                }
            }
        }

        private void AssignTargetToMezzPet()
        {
            if (DynelManager.LocalPlayer.IsAttacking && Time.NormalTime - _lastSwitchedMezzTime > 9)
            {
                SimpleChar targetToMezz = GetTargetToMezz();
                if (targetToMezz != null)
                {
                    Pet mezzPet = DynelManager.LocalPlayer.Pets.Where(pet => pet.Type == PetType.Support).FirstOrDefault();
                    if (mezzPet != null)
                    {
                        mezzPet.Attack(targetToMezz.Identity);
                        _lastSwitchedMezzTime = Time.NormalTime;
                    }
                }
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (CanLookupPetsAfterZone())
            {
                SynchronizePetCombatStateWithOwner();
                AssignTargetToHealPet();
                AssignTargetToMezzPet();
            }
            base.OnUpdate(deltaTime);

            if (GetNanoBuffsSelection() == NanoBuffsSelection.RK) 
            {
                CancelBuffs(RelevantNanos.MPCompositeNano);
            } 
            else
            {
                CancelBuffs(RelevantNanos.MatMetBuffs);
                CancelBuffs(RelevantNanos.BioMetBuffs);
                CancelBuffs(RelevantNanos.PsyModBuffs);
                CancelBuffs(RelevantNanos.SenImpBuffs);
                CancelBuffs(RelevantNanos.MatCreBuffs);
                CancelBuffs(RelevantNanos.MatLocBuffs);
            }
        }

        private static class RelevantNanos
        {
            public static readonly int[] CostBuffs = { 95409, 29307, 95411, 95408, 95410 };
            public static readonly int[] HealPets = { 225902, 125746, 125739, 125740, 125741, 125742, 125743, 125744, 125745, 125738 }; //Belamorte has a higher stacking order than Moritficant
            public static readonly int[] SLAttackPets = { 254859, 225900, 254859, 225900, 225898, 225896, 225894 };
            public static readonly int[] MPCompositeNano = { 220343, 220341, 220339, 220337, 220335, 220333, 220331 };
            public static readonly int[] WarmUpfNukes = { 270355, 125761, 29297, 125762, 29298, 29114 };
            public static readonly int[] SingleTargetNukes = { 267878, 125763, 125760, 125765, 125764 };
            public static readonly int[] InstillDamageBuffs = { 285101, 116814, 116817, 116812, 116816, 116821, 116815, 116813 };
            public static readonly int[] ChantBuffs = { 116819, 116818, 116811, 116820 };
            public static readonly int[] MatMetBuffs = Spell.GetSpellsForNanoline(NanoLine.MatMetBuff).OrderByStackingOrder().Select(spell => spell.Identity.Instance).ToArray();
            public static readonly int[] BioMetBuffs = Spell.GetSpellsForNanoline(NanoLine.BioMetBuff).OrderByStackingOrder().Select(spell => spell.Identity.Instance).ToArray();
            public static readonly int[] PsyModBuffs = Spell.GetSpellsForNanoline(NanoLine.PsyModBuff).OrderByStackingOrder().Select(spell => spell.Identity.Instance).ToArray();
            public static readonly int[] SenImpBuffs = { 29304, 151757, 29315, 151764 }; //Composites count as SenseImp buffs. Have to be excluded
            public static readonly int[] MatCreBuffs = Spell.GetSpellsForNanoline(NanoLine.MatCreaBuff).OrderByStackingOrder().Select(spell => spell.Identity.Instance).ToArray();
            public static readonly int[] MatLocBuffs = Spell.GetSpellsForNanoline(NanoLine.MatLocBuff).OrderByStackingOrder().Select(spell => spell.Identity.Instance).ToArray();

            public static readonly string[] TwoHandedNames = { "Azure Cobra of Orma", "Wixel's Notum Python", "Asp of Semol", "Viper Staff" };
            public static readonly string[] OneHandedNames = { "Asp of Titaniush", "Gold Acantophis", "Bitis Striker", "Coplan's Hand Taipan", "The Crotalus" };
            public static readonly string[] ShieldNames = { "Shield of Zset", "Shield of Esa", "Shield of Asmodian", "Mocham's Guard", "Death Ward", "Belthior's Flame Ward", "Wave Breaker", "Living Shield of Evernan", "Solar Guard", "Notum Defender", "Vital Buckler" };
        }

        private enum NanoBuffsSelection
        {
            SL = 0,
            RK = 1
        }

        private enum SummonedWeaponSelection
        {
            DISABLED = 0,
            TWO_HANDED = 1,
            ONE_HANDED_PLUS_SHIELD = 2,
            ONE_HANDED_PLUS_ONE_HANDED = 3,
            ONE_HANDED = 4,
            SHIELD = 5
        }

        private NanoBuffsSelection GetNanoBuffsSelection()
        {
            return (NanoBuffsSelection)settings["NanoBuffsSelection"].AsInt32();
        }

        private SummonedWeaponSelection GetSummonedWeaponSelection()
        {
            return (SummonedWeaponSelection)settings["SummonedWeaponSelection"].AsInt32();
        }
    }
}
