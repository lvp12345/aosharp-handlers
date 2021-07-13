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
    public class CratCombatHandler : GenericCombatHandler
    {
        private static readonly List<string> AoeRootTargets = new List<string>() { "Flaming Vengeance", "Hand of the Colonel" };

        public CratCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("SpawnPets", true);
            settings.AddVariable("BuffPets", true);
            settings.AddVariable("UseMalaise", true);
            settings.AddVariable("UseLEInitDebuffs", true);
            settings.AddVariable("UseMalaiseOnOthers", false);
            settings.AddVariable("UseNukes", true);
            settings.AddVariable("UseAoeRoot", false);
            settings.AddVariable("BuffAuraSelection", (int)BuffAuraType.AAD);
            settings.AddVariable("DebuffAuraSelection", (int)DebuffAuraType.NONE);
            RegisterSettingsWindow("Bureaucrat Handler", "BureaucratSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcBureaucratWrongWindow, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratFormsInTriplicate, LEProc, CombatActionPriority.Low);

            //Debuffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder(), CratDebuffOthersInCombat, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder(), MalaiseTargetDebuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.GeneralRadiationACDebuff).OrderByStackingOrder(), LEInitTargetDebuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.GeneralProjectileACDebuff).OrderByStackingOrder(), LEInitTargetDebuff);
            RegisterSpellProcessor(RelevantNanos.AoeRoots, AoeRoot, CombatActionPriority.High);

            //Debuff Aura
            RegisterSpellProcessor(RelevantNanos.DeadMenWalking, NanoPointsDebuffAura);
            RegisterSpellProcessor(RelevantNanos.NanoPointsDebuffAuras, NanoPointsDebuffAura);

            RegisterSpellProcessor(RelevantNanos.DeadMenWalking, NanoResDebuffAura);
            RegisterSpellProcessor(RelevantNanos.NanoResDebuffAuras, NanoResDebuffAura);
            
            RegisterSpellProcessor(RelevantNanos.DeadMenWalking, CritDebuffAura);
            RegisterSpellProcessor(RelevantNanos.CritDebuffAuras, CritDebuffAura);

            //Spells
            RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke, CombatActionPriority.Low);
            RegisterSpellProcessor(RelevantNanos.WorkplaceDepression, WorkplaceDepressionTargetDebuff);

            //Buff Aura
            RegisterSpellProcessor(RelevantNanos.AadBuffAuras, AadBuffAura);
            RegisterSpellProcessor(RelevantNanos.CritBuffAuras, CritBuffAura);
            RegisterSpellProcessor(RelevantNanos.NanoResBuffAuras, NanoResBuffAura);

            //Team Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDeltaBuffs).OrderByStackingOrder(), TeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), RangedTeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalDecreaseBuff).OrderByStackingOrder(), TeamBuff);

            //Pet Buffs
            if(Spell.Find(RelevantNanos.CorporateStrategy, out Spell spell))
            {
                RegisterSpellProcessor(RelevantNanos.CorporateStrategy, CorporateStrategy);
            }
            else
            {
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetShortTermDamageBuffs).OrderByStackingOrder(), PetTargetBuff);
            }
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDamageOverTimeResistNanos).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDefensiveNanos).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetTauntBuff).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(RelevantNanos.DroidDamageMatrix, DroidMatrixBuff);
            RegisterSpellProcessor(RelevantNanos.DroidPressureMatrix, DroidMatrixBuff);

            //Pet Spawners
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SupportPets).OrderByStackingOrder(), CarloSpawner);
            RegisterSpellProcessor(RelevantNanos.Pets.Select(x => x.Key).ToArray(), RobotSpawner);

            //Pet Shells
            foreach (int shellId in RelevantNanos.Pets.Values.Select(x => x.ShellId))
            {
                RegisterItemProcessor(shellId, shellId, RobotSpawnerItem);
            }
        }

        private bool NanoPointsDebuffAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return DebuffAura(DebuffAuraType.NANO_PTS, spell, fightingTarget, ref actionTarget);
        }

        private bool CritDebuffAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return DebuffAura(DebuffAuraType.CRIT, spell, fightingTarget, ref actionTarget);
        }

        private bool NanoResDebuffAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return DebuffAura(DebuffAuraType.NANO_RES, spell, fightingTarget, ref actionTarget);
        }

        private bool AadBuffAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffAura(BuffAuraType.AAD, spell, fightingTarget, ref actionTarget);
        }

        private bool CritBuffAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffAura(BuffAuraType.CRIT, spell, fightingTarget, ref actionTarget);
        }

        private bool NanoResBuffAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffAura(BuffAuraType.NANO_RES, spell, fightingTarget, ref actionTarget);
        }

        private bool BuffAura(BuffAuraType auraType, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsBuffAuraSelected(auraType))
            {
            return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DebuffAura(DebuffAuraType auraType, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !IsDebuffAuraSelected(auraType))
            {
                return false;
            }

            return CombatBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool AoeRoot(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseAoeRoot"))
            {
                return false;
            }

            SimpleChar target = DynelManager.Characters.Where(IsAoeRootSnareSpamTarget).Where(DoesNotHaveAoeRootRunning).FirstOrDefault();
            if (target != null)
            {
                actionTarget.Target = target;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        private bool DoesNotHaveAoeRootRunning(SimpleChar target)
        {
            return !target.Buffs.Any(IsAoeRoot);
        }

        private bool IsAoeRoot(Buff buff)
        {
            return RelevantNanos.AoeRootDebuffs.Any(id => id == buff.Identity.Instance);
        }

        private bool WorkplaceDepressionTargetDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseNukes"))
            {
                return false;
            }

            return NeedsDebuffRefresh(spell, fightingTarget);
        }

        private bool MalaiseTargetDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(!IsSettingEnabled("UseMalaise"))
            {
                return false;
            }

            return NeedsDebuffRefresh(spell, fightingTarget);
        }

        private bool LEInitTargetDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseLEInitDebuffs"))
            {
                return false;
            }

            return NeedsDebuffRefresh(spell, fightingTarget);
        }

        private bool CratDebuffOthersInCombat(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) 
        {
            return ToggledDebuffOthersInCombat("UseMalaiseOnOthers", spell, fightingTarget, ref actionTarget);
        }

        private bool NeedsDebuffRefresh(Spell spell, SimpleChar target)
        {
            if(target == null)
            {
                return false;
            }
            //Check the remaining time on debuffs. On the enemy target
            return !target.Buffs.Where(buff => buff.Name == spell.Name)
                .Where(buff => buff.RemainingTime > 1)
                .Any();
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !IsSettingEnabled("UseNukes"))
            {
                return false;
            }

            return true;
        }

        protected bool DroidMatrixBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone())
            {
                return false;
            }

            Pet petToBuff = FindPetThat(pet => RobotNeedsBuff(spell, pet));
            if (petToBuff != null) {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = petToBuff.Character;
                return true;
            }

            return false;
        }

        private bool RobotNeedsBuff(Spell spell, Pet pet)
        {
            if(pet.Type != PetType.Attack)
            {
                return false;
            }

            if (FindSpellNanoLineFallbackToId(spell, pet.Character.Buffs, out Buff buff))
            {
                //Don't cast if weaker than existing
                if (spell.StackingOrder < buff.StackingOrder)
                {
                    return false;
                }

                //Don't cast if greater than 10% time remaining
                if (buff.RemainingTime / buff.TotalTime > 0.1)
                {
                    return false; ;
                }
            }

            return true;
        }

        private bool CorporateStrategy(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetShortTermDamageBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool PetTargetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(spell.Nanoline, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        protected bool FindSpellNanoLineFallbackToId(Spell spell, Buff[] buffs, out Buff buff)
        {
            if (buffs.Find(spell.Nanoline, out Buff buffFromNanoLine))
            {
                buff = buffFromNanoLine;
                return true;
            }
            int spellId = spell.Identity.Instance;
            if (RelevantNanos.PetNanoToBuff.ContainsKey(spellId)) { 
                int buffId = RelevantNanos.PetNanoToBuff[spellId];
                if (buffs.Find(buffId, out Buff buffFromId))
                {
                    buff = buffFromId;
                    return true;
                }
            }
            buff = null;
            return false;
        }

        protected virtual bool RobotSpawnerItem(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetSpawnerItem(RelevantNanos.Pets, item, fightingTarget, ref actionTarget);
        }

        protected bool CarloSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return NoShellPetSpawner(PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        protected bool RobotSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetSpawner(RelevantNanos.Pets, spell, fightingTarget, ref actionTarget);
        }

        protected override void OnUpdate(float deltaTime)
        {
            SynchronizePetCombatStateWithOwner();

            base.OnUpdate(deltaTime);

            CancelDebuffAurasIfNeeded();
            CancelBuffAurasIfNeeded();
        }

        private void CancelBuffAurasIfNeeded()
        {
            if (!IsBuffAuraSelected(BuffAuraType.AAD))
            {
                CancelBuffs(RelevantNanos.AadBuffAuras);
            }
            if (!IsBuffAuraSelected(BuffAuraType.CRIT))
            {
                CancelBuffs(RelevantNanos.CritBuffAuras);
            }
            if (!IsBuffAuraSelected(BuffAuraType.NANO_RES))
            {
                CancelBuffs(RelevantNanos.NanoResBuffAuras);
            }
        }

        private void CancelDebuffAurasIfNeeded()
        {
            CancelHostileAuras(RelevantNanos.DeadMenWalking);
            CancelHostileAuras(RelevantNanos.CritDebuffAuras);
            CancelHostileAuras(RelevantNanos.NanoPointsDebuffAuras);
            CancelHostileAuras(RelevantNanos.NanoResDebuffAuras);

            if(IsDebuffAuraSelected(DebuffAuraType.NONE))
            {
                CancelBuffs(RelevantNanos.DeadMenWalking);
            }
            if (!IsDebuffAuraSelected(DebuffAuraType.CRIT))
            {
                CancelBuffs(RelevantNanos.CritDebuffAuras);
            }
            if (!IsDebuffAuraSelected(DebuffAuraType.NANO_PTS))
            {
                CancelBuffs(RelevantNanos.NanoPointsDebuffAuras);
            }
            if (!IsDebuffAuraSelected(DebuffAuraType.NANO_RES))
            {
                CancelBuffs(RelevantNanos.NanoResDebuffAuras);
            }
        }

        private bool IsBuffAuraSelected(BuffAuraType auraType)
        {
            return auraType.Equals((BuffAuraType)settings["BuffAuraSelection"].AsInt32());
        }

        private bool IsDebuffAuraSelected(DebuffAuraType auraType)
        {
            return auraType.Equals((DebuffAuraType)settings["DebuffAuraSelection"].AsInt32());
        }

        private static class RelevantNanos
        {
            public const int WorkplaceDepression = 273631;
            public const int DroidDamageMatrix = 267916;
            public const int DroidPressureMatrix = 302247;
            public const int CorporateStrategy = 267611;
            public static readonly int[] SingleTargetNukes = { 273307, WorkplaceDepression, 270250, 78400, 30082, 78394, 78395, 82000, 78396, 78397, 30091, 78399, 81996, 30083, 81997, 30068, 81998, 78398, 81999, 29618 };
            public static readonly int[] DeadMenWalking = { 275826 };
            public static readonly int[] AoeRoots = { 224129, 224127, 224125, 224123, 224121, 224119, 82166, 82164, 82163, 82161, 82160, 82159, 82158, 82157, 82156 };
            public static readonly int[] AoeRootDebuffs = { 82137, 244634, 244633, 244630, 244631, 244632, 82138, 82139, 244629, 82140, 82141, 82142, 82143, 82144, 82145 };
            public static readonly int[] AadBuffAuras = { 270783, 155807, 155806, 155805, 155809, 155808 };
            public static readonly int[] CritBuffAuras = { 157503, 157499 };
            public static readonly int[] NanoResBuffAuras = { 157504, 157500, 157501, 157502 };
            public static readonly int[] NanoPointsDebuffAuras = { 157524, 157534, 157533, 157532, 157531 };
            public static readonly int[] CritDebuffAuras = { 157530, 157529, 157528 };
            public static readonly int[] NanoResDebuffAuras = { 157527, 157526, 157525, 157535 };

            public static Dictionary<int, int> PetNanoToBuff = new Dictionary<int, int>
            {
                {DroidDamageMatrix, 285696},
                {DroidPressureMatrix, 302246},
                {CorporateStrategy, 285695}
            };

            public static Dictionary<int, PetSpellData> Pets = new Dictionary<int, PetSpellData>
            {
                { 273300, new PetSpellData(273301, PetType.Attack) },
                { 235386, new PetSpellData(239828, PetType.Attack) },
                { 46391, new PetSpellData(96213, PetType.Attack) }
            };
        }

        private enum BuffAuraType
        {
            AAD = 0,
            CRIT = 1,
            NANO_RES = 2
        }

        private enum DebuffAuraType
        {
            NONE = 0,
            NANO_PTS = 1,
            CRIT = 2,
            NANO_RES = 3
        }
    }
}
