using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using CombatHandler;
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

        public static string PluginDirectory;

        public MPCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("SpawnPets", true);
            settings.AddVariable("BuffPets", true);
            settings.AddVariable("MezzPet", false);

            settings.AddVariable("Cost", false);
            settings.AddVariable("CostTeam", false);

            settings.AddVariable("InterruptChance", false);

            settings.AddVariable("DamageDebuffs", false);
            settings.AddVariable("OSDamageDebuffs", false);

            settings.AddVariable("NanoResistanceDebuff", false);
            settings.AddVariable("NanoShutdownDebuff", false);

            settings.AddVariable("Composites", false);
            settings.AddVariable("CompositesTeam", false);

            settings.AddVariable("MatterCrea", false);
            settings.AddVariable("PyschoModi", false);
            settings.AddVariable("TimeSpace", false);
            settings.AddVariable("SenseImprov", false);
            settings.AddVariable("BioMet", false);
            settings.AddVariable("MattMet", false);

            //settings.AddVariable("CostTeam", false);

            settings.AddVariable("Nukes", false);

            //settings.AddVariable("NanoBuffsSelection", (int)NanoBuffsSelection.SL);
            //settings.AddVariable("SummonedWeaponSelection", (int)SummonedWeaponSelection.DISABLED);

            RegisterSettingsWindow("MP Handler", "MPSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistAnticipatedEvasion, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistSuppressFury, LEProc, CombatActionPriority.Low);

            //Self buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), GenericBuffExcludeInnerSanctum);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtistBowBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Psy_IntBuff).OrderByStackingOrder(), GenericBuff);

            //Team buffs
            RegisterSpellProcessor(RelevantNanos.MPCompositeNano, CompositeNanoBuff);

            RegisterSpellProcessor(RelevantNanos.MatMetBuffs, MattMetBuff);
            RegisterSpellProcessor(RelevantNanos.BioMetBuffs, BioMetBuff);
            RegisterSpellProcessor(RelevantNanos.PsyModBuffs, PyschoModiBuff);
            RegisterSpellProcessor(RelevantNanos.SenImpBuffs, SenseImprovBuff);
            RegisterSpellProcessor(RelevantNanos.MatCreBuffs, MatterCreaBuff);
            RegisterSpellProcessor(RelevantNanos.MatLocBuffs, TimeSpaceBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InterruptModifier).OrderByStackingOrder(), InterruptModifierBuff);
            RegisterSpellProcessor(RelevantNanos.CostBuffs, Cost);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolMasteryBuff);

            //Debuffs
            RegisterSpellProcessor(RelevantNanos.WarmUpfNukes, WarmUpNuke, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPDamageDebuffLineA).OrderByStackingOrder(), DamageDebuff, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPDamageDebuffLineB).OrderByStackingOrder(), DamageDebuff, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MetaPhysicistDamageDebuff).OrderByStackingOrder(), DamageDebuff, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MetaPhysicistDamageDebuff).OrderByStackingOrder(), MPDebuffOthersInCombat);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceDebuff_LineA).OrderByStackingOrder(), NanoResistanceDebuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoShutdownDebuff).OrderByStackingOrder(), NanoShutdownDebuff);

            //Pets
            RegisterSpellProcessor(GetAttackPetsWithSLPetsFirst(), AttackPetSpawner);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SupportPets).OrderByStackingOrder(), SupportPetSpawner);
            RegisterSpellProcessor(RelevantNanos.HealPets, HealPetSpawner);

            //Pet Buffs
            RegisterSpellProcessor(RelevantNanos.EvadeBuff, EvasionPetBuff);
            RegisterSpellProcessor(RelevantNanos.InstillDamageBuffs, InstillDamageBuff);
            RegisterSpellProcessor(RelevantNanos.MastersBidding, MastersBidding);
            RegisterSpellProcessor(RelevantNanos.ChantBuffs, ChantBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MesmerizationConstructEmpowerment).OrderByStackingOrder(), MezzPetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealingConstructEmpowerment).OrderByStackingOrder(), HealPetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AggressiveConstructEmpowerment).OrderByStackingOrder(), AttackPetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPAttackPetDamageType).OrderByStackingOrder(), DamageTypePetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDamageOverTimeResistNanos).OrderByStackingOrder(), NanoResistancePetBuff);
            RegisterSpellProcessor(RelevantNanos.PetDefensive, DefensivePetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetHealDelta843).OrderByStackingOrder(), HealDeltaPetBuff);
            RegisterSpellProcessor(RelevantNanos.PetShortTermDamage, ShortTermDamagePetBuff);
            RegisterSpellProcessor(RelevantNanos.CostBuffs, CostPetBuff);

            RegisterPerkProcessor(PerkHash.ChannelRage, ChannelRage);

            PluginDirectory = pluginDir;
        }

        private void PetView(object s, ButtonBase button)
        {
            Window petWindow = Window.CreateFromXml("Pets", PluginDirectory + "\\UI\\MPPetsView.xml",
            windowSize: new Rect(0, 0, 240, 345),
            windowStyle: WindowStyle.Default,
            windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);
            petWindow.Show(true);
        }

        private void DebuffView(object s, ButtonBase button)
        {
            Window debuffWindow = Window.CreateFromXml("Debuffs", PluginDirectory + "\\UI\\MPDebuffsView.xml",
            windowSize: new Rect(0, 0, 240, 345),
            windowStyle: WindowStyle.Default,
            windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);
            debuffWindow.Show(true);
        }

        private void BuffView(object s, ButtonBase button)
        {
            Window buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\MPBuffsView.xml",
            windowSize: new Rect(0, 0, 240, 345),
            windowStyle: WindowStyle.Default,
            windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);
            buffWindow.Show(true);
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (settings["Composites"].AsBool() || settings["CompositesTeam"].AsBool() &&
                (settings["MatterCrea"].AsBool() || settings["PyschoModi"].AsBool()
                || settings["TimeSpace"].AsBool() || settings["SenseImprov"].AsBool()
                || settings["BioMet"].AsBool() || settings["MattMet"].AsBool()))
            {
                settings["Composites"] = false;
                settings["CompositesTeam"] = false;
                settings["MatterCrea"] = false;
                settings["PyschoModi"] = false;
                settings["TimeSpace"] = false;
                settings["SenseImprov"] = false;
                settings["BioMet"] = false;
                settings["MattMet"] = false;

                Chat.WriteLine("Only activate one option.");
            }

            if (CanLookupPetsAfterZone())
            {
                SynchronizePetCombatStateWithOwner();
                AssignTargetToHealPet();
                AssignTargetToMezzPet();
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                if (SettingsController.settingsView != null)
                {
                    if (SettingsController.settingsView.FindChild("PetsView", out Button petView))
                    {
                        petView.Tag = SettingsController.settingsView;
                        petView.Clicked = PetView;
                    }

                    if (SettingsController.settingsView.FindChild("BuffsView", out Button buffView))
                    {
                        buffView.Tag = SettingsController.settingsView;
                        buffView.Clicked = BuffView;
                    }

                    if (SettingsController.settingsView.FindChild("DebuffsView", out Button debuffView))
                    {
                        debuffView.Tag = SettingsController.settingsView;
                        debuffView.Clicked = DebuffView;
                    }
                }
            }

            base.OnUpdate(deltaTime);
        }

        private bool ChannelRage(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            Pet petToPerk = FindPetThat(CanPerkChannelRage);
            if (petToPerk != null)
            {
                actionTarget.Target = petToPerk.Character;
                actionTarget.ShouldSetTarget = true;
                return true;
            }
            return false;
        }
        protected bool MastersBidding(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            Pet petToBuff = FindPetThat(pet => !pet.Character.Buffs.Contains(NanoLine.SiphonBox683)
                && (pet.Character.GetStat(Stat.NPCFamily) != 98)
                && (pet.Type == PetType.Attack || pet.Type == PetType.Support));

            if (petToBuff != null)
            {
                spell.Cast(petToBuff.Character, true);
            }

            return false;
        }

        private bool Cost(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Cost"))
            {
                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            if (IsSettingEnabled("CostTeam"))
            {
                return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }
        private bool CanPerkChannelRage(Pet pet)
        {
            if (!pet.Character.IsAlive) { return false; }

            if (pet.Type != PetType.Attack) { return false; }

            return !pet.Character.Buffs.Any(buff => buff.Nanoline == NanoLine.ChannelRage);
        }

        private bool NanoShutdownDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("NanoShutdownDebuff", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool NanoResistanceDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("NanoResistanceDebuff", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool DamageDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("DamageDebuffs", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool CompositeNanoBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Composites")) 
            {
                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            if (IsSettingEnabled("CompositesTeam"))
            {
                return TeamBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool MatterCreaBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Composites") || IsSettingEnabled("CompositesTeam")) { return false; }

            if (IsSettingEnabled("MatterCrea"))
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.MPCompositeNano))
                    CancelBuffs(RelevantNanos.MPCompositeNano);

                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool PyschoModiBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Composites") || IsSettingEnabled("CompositesTeam")) { return false; }

            if (IsSettingEnabled("PyschoModi"))
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.MPCompositeNano))
                    CancelBuffs(RelevantNanos.MPCompositeNano);

                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool TimeSpaceBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Composites") || IsSettingEnabled("CompositesTeam")) { return false; }

            if (IsSettingEnabled("TimeSpace"))
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.MPCompositeNano))
                    CancelBuffs(RelevantNanos.MPCompositeNano);

                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool SenseImprovBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Composites") || IsSettingEnabled("CompositesTeam")) { return false; }

            if (IsSettingEnabled("SenseImprov"))
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.MPCompositeNano))
                    CancelBuffs(RelevantNanos.MPCompositeNano);

                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool BioMetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Composites") || IsSettingEnabled("CompositesTeam")) { return false; }

            if (IsSettingEnabled("BioMet"))
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.MPCompositeNano))
                    CancelBuffs(RelevantNanos.MPCompositeNano);

                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool MattMetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Composites") || IsSettingEnabled("CompositesTeam")) { return false; }

            if (IsSettingEnabled("MattMet"))
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.MPCompositeNano))
                    CancelBuffs(RelevantNanos.MPCompositeNano);

                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool InterruptModifierBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledBuff("InterruptChance", spell, fightingTarget, ref actionTarget);
        }

        private bool MPDebuffOthersInCombat(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) 
        {
            return ToggledDebuffOthersInCombat("OSDamageDebuffs", spell, fightingTarget, ref actionTarget);
        }

        private bool WarmUpNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !IsSettingEnabled("Nukes") || !CanCast(spell)) { return false; }

            Spell singleNuke = Spell.List.FirstOrDefault(x => RelevantNanos.SingleTargetNukes.Contains(x.Identity.Instance));

            if (singleNuke != null)
            {
                if (fightingTarget.Buffs.Contains(NanoLine.MetaphysicistMindDamageNanoDebuffs)) { return false; }
            }

            return true;
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !IsSettingEnabled("Nukes") || !CanCast(spell)) { return false; }

            Spell warmupNuke = Spell.List.FirstOrDefault(x => RelevantNanos.WarmUpfNukes.Contains(x.Identity.Instance));

            if (warmupNuke != null)
            {
                if (!fightingTarget.Buffs.Contains(NanoLine.MetaphysicistMindDamageNanoDebuffs)) { return false; }
            }

            return true;
        }

        private bool MezzPetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.MesmerizationConstructEmpowerment);
        }

        private bool HealPetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.HealingConstructEmpowerment);
        }

        private bool AttackPetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.AggressiveConstructEmpowerment);
        }

        private bool DamageTypePetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.MPAttackPetDamageType, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool EvasionPetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.MajorEvasionBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.MajorEvasionBuffs, PetType.Heal, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.MajorEvasionBuffs, PetType.Support, spell, fightingTarget, ref actionTarget);
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
            return PetTargetBuff(NanoLine.PetHealDelta843, PetType.Attack, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.PetHealDelta843, PetType.Heal, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.PetHealDelta843, PetType.Support, spell, fightingTarget, ref actionTarget);
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
            return PetTargetBuff(NanoLine.NPCostBuff, PetType.Heal, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.NPCostBuff, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool AttackPetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return NoShellPetSpawner(PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool SupportPetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("MezzPet")) { return false; }

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
           if (DynelManager.LocalPlayer.HealthPercent < 90)
           {
                return DynelManager.LocalPlayer;
           }
           else if (DynelManager.LocalPlayer.IsInTeam())
           {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent < 85)
                    .Where(c => DynelManager.LocalPlayer.DistanceFrom(c) < 30f)
                    .OrderByDescending(c => c.HealthPercent)
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    return dyingTeamMember;
                }
           }
           else
           {
                Pet dyingPet = DynelManager.LocalPlayer.Pets
                     .Where(pet => pet.Type == PetType.Attack || pet.Type == PetType.Social)
                     .Where(pet => pet.Character.HealthPercent < 80)
                     .Where(pet => pet.Character.DistanceFrom(DynelManager.LocalPlayer) < 30f)
                     .OrderByDescending(pet => pet.Character.HealthPercent)
                     .FirstOrDefault();

                if (dyingPet != null)
                {
                    return dyingPet.Character;
                }
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
                .Where(c => c.IsValid).Where(c => c.IsInLineOfSight).Where(c => c.DistanceFrom(DynelManager.LocalPlayer) <= 15f) //Is in range for debuff
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
                        if (healPet.Character.Nano <= 1) { return; }

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
                        if (mezzPet.Character.Nano <= 1) { return; }

                        mezzPet.Attack(targetToMezz.Identity);
                        _lastSwitchedMezzTime = Time.NormalTime;
                    }
                }
            }
        }

        private static class RelevantNanos
        {
            public const int MastersBidding = 268171;
            public const int EvadeBuff = 29272;
            public static readonly int[] CostBuffs = { 95409, 29307, 95411, 95408, 95410 };
            public static readonly int[] HealPets = { 225902, 125746, 125739, 125740, 125741, 125742, 125743, 125744, 125745, 125738 }; //Belamorte has a higher stacking order than Moritficant
            public static readonly int[] SLAttackPets = { 254859, 225900, 254859, 225900, 225898, 225896, 225894 };
            public static readonly int[] MPCompositeNano = { 220343, 220341, 220339, 220337, 220335, 220333, 220331 };
            public static readonly int[] WarmUpfNukes = { 270355, 125761, 29297, 125762, 29298, 29114 };
            public static readonly int[] PetDefensive = { 267601, 267600, 267599 };

            public static readonly int[] PetShortTermDamage = { 267598, 205193, 151827, 205189, 205187, 151828, 205185, 151824, 205183,
            151830, 205191, 151826, 205195, 151825, 205197, 151831 };

            public static readonly int[] SingleTargetNukes = { 267878, 125763, 125760, 125765, 125764 };
            public static readonly int[] InstillDamageBuffs = { 270800, 285101, 116814, 116817, 116812, 116816, 116821, 116815, 116813 };
            public static readonly int[] ChantBuffs = { 116819, 116818, 116811, 116820 };
            public static readonly int[] MatMetBuffs = Spell.GetSpellsForNanoline(NanoLine.MatMetBuff).OrderByStackingOrder().Select(spell => spell.Identity.Instance).ToArray();
            public static readonly int[] BioMetBuffs = Spell.GetSpellsForNanoline(NanoLine.BioMetBuff).OrderByStackingOrder().Select(spell => spell.Identity.Instance).ToArray();
            public static readonly int[] PsyModBuffs = Spell.GetSpellsForNanoline(NanoLine.PsyModBuff).OrderByStackingOrder().Select(spell => spell.Identity.Instance).ToArray();
            public static readonly int[] SenImpBuffs = { 29304, 151757, 29315, 151764 }; //Composites count as SenseImp buffs. Have to be excluded
            public static readonly int[] MatCreBuffs = Spell.GetSpellsForNanoline(NanoLine.MatCreaBuff).OrderByStackingOrder().Select(spell => spell.Identity.Instance).ToArray();
            public static readonly int[] MatLocBuffs = Spell.GetSpellsForNanoline(NanoLine.MatLocBuff).OrderByStackingOrder().Select(spell => spell.Identity.Instance).ToArray();

            //public static readonly string[] TwoHandedNames = { "Azure Cobra of Orma", "Wixel's Notum Python", "Asp of Semol", "Viper Staff" };
            //public static readonly string[] OneHandedNames = { "Asp of Titaniush", "Gold Acantophis", "Bitis Striker", "Coplan's Hand Taipan", "The Crotalus" };
            //public static readonly string[] ShieldNames = { "Shield of Zset", "Shield of Esa", "Shield of Asmodian", "Mocham's Guard", "Death Ward", "Belthior's Flame Ward", "Wave Breaker", "Living Shield of Evernan", "Solar Guard", "Notum Defender", "Vital Buckler" };
        }

        //private enum SummonedWeaponSelection
        //{
        //    DISABLED = 0,
        //    TWO_HANDED = 1,
        //    ONE_HANDED_PLUS_SHIELD = 2,
        //    ONE_HANDED_PLUS_ONE_HANDED = 3,
        //    ONE_HANDED = 4,
        //    SHIELD = 5
        //}

        //private SummonedWeaponSelection GetSummonedWeaponSelection()
        //{
        //    return (SummonedWeaponSelection)settings["SummonedWeaponSelection"].AsInt32();
        //}
    }
}
