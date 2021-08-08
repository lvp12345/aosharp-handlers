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
        private double _lastTrimTime = 0;
        private const float DelayBetweenTrims = 1;
        private const float DelayBetweenDiverTrims = 305;
        private bool attackPetTrimmedAggressive = false;
        private Dictionary<PetType, bool> petTrimmedAggDef = new Dictionary<PetType, bool>();
        private Dictionary<PetType, double> _lastPetTrimDivertTime = new Dictionary<PetType, double>()
        {
            { PetType.Attack, 0 },
            { PetType.Support, 0 }
        };

        public CratCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("BuffCrit", false);
            settings.AddVariable("BuffAAOAAD", false);
            settings.AddVariable("BuffNanoResist", false);
            settings.AddVariable("DebuffCrit", false);
            settings.AddVariable("DebuffNanoResist", false);
            settings.AddVariable("DebuffNanoDrain", false);

            settings.AddVariable("SpawnPets", true);
            settings.AddVariable("BuffPets", true);
            settings.AddVariable("MalaiseTarget", true);
            settings.AddVariable("LEInitDebuffs", true);
            settings.AddVariable("MalaiseOnOthers", false);
            settings.AddVariable("DivertTrimmer", true);
            settings.AddVariable("TauntTrimmer", true);
            settings.AddVariable("AggDefTrimmer", true);
            settings.AddVariable("UseNukes", true);
            settings.AddVariable("UseAoeRoot", false);
            settings.AddVariable("Calm12Man", false);
            RegisterSettingsWindow("Bureaucrat Handler", "BureaucratSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcBureaucratWrongWindow, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratFormsInTriplicate, LEProc, CombatActionPriority.Low);

            //Debuffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder(), CratDebuffOthersInCombat, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder(), MalaiseTargetDebuff);
            RegisterSpellProcessor(RelevantNanos.GeneralRadACDebuff, LEInitTargetDebuff);
            RegisterSpellProcessor(RelevantNanos.GeneralProjACDebuff, LEInitTargetDebuff);
            RegisterSpellProcessor(RelevantNanos.AoeRoots, AoeRoot, CombatActionPriority.High);

            //Debuff Aura
            RegisterSpellProcessor(RelevantNanos.NanoPointsDebuffAuras, DebuffNanoDrainAura);
            RegisterSpellProcessor(RelevantNanos.NanoResDebuffAuras, DebuffNanoResistAura);
            RegisterSpellProcessor(RelevantNanos.CritDebuffAuras, DebuffCritAura);

            //Spells
            RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke, CombatActionPriority.Low);
            RegisterSpellProcessor(RelevantNanos.WorkplaceDepression, WorkplaceDepressionTargetDebuff);
            RegisterSpellProcessor(RelevantNanos.LastMinNegotiations, Calm12Man);

            //Buff Aura
            RegisterSpellProcessor(RelevantNanos.AadBuffAuras, BuffAAOAADAura);
            RegisterSpellProcessor(RelevantNanos.CritBuffAuras, BuffCritAura);
            RegisterSpellProcessor(RelevantNanos.NanoResBuffAuras, BuffNanoResistAura);

            //Team Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDeltaBuffs).OrderByStackingOrder(), CheckBeforeCast);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolTeamBuff);
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

            //Pet Trimmers
            ResetTrimmers();
            RegisterItemProcessor(RelevantTrimmers.PositiveAggressiveDefensive, RelevantTrimmers.PositiveAggressiveDefensive, PetAggDefTrimmer);
            RegisterItemProcessor(RelevantTrimmers.IncreaseAggressivenessHigh, RelevantTrimmers.IncreaseAggressivenessHigh, PetAggressiveTrimmer);
            RegisterItemProcessor(RelevantTrimmers.DivertEnergyToOffense, RelevantTrimmers.DivertEnergyToOffense, PetDivertTrimmer);

            Game.TeleportEnded += OnZoned;
        }
        private bool BuffCritAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("BuffNanoResist") || IsSettingEnabled("BuffAAOAAD"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool BuffNanoResistAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("BuffCrit") || IsSettingEnabled("BuffAAOAAD"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool BuffAAOAADAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("BuffNanoResist") || IsSettingEnabled("BuffCrit"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DebuffCritAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("DebuffNanoResist") || IsSettingEnabled("DebuffNanoDrain"))
            {
                return false;
            }

            return CombatBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DebuffNanoResistAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("DebuffCrit") || IsSettingEnabled("DebuffNanoDrain"))
            {
                return false;
            }

            return CombatBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DebuffNanoDrainAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("DebuffNanoResist") || IsSettingEnabled("DebuffCrit"))
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

        private bool CheckBeforeCast(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell))
            {
                return false;
            }

            if (!SpellChecksPlayer(spell))
                return false;

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => SpellChecksOther(spell, c))
                    .Where(c => c.Profession != Profession.Keeper)
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

            SimpleChar target = DynelManager.Characters.Where(IsAoeRootSnareSpamTarget).Where(DoesNotHaveAoeRootRunning).FirstOrDefault();

            if (IsSettingEnabled("UseAoeRoot") && target != null)
            {
                return false;
            }

            return NeedsDebuffRefresh(spell, fightingTarget);
        }

        private bool MalaiseTargetDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(!IsSettingEnabled("MalaiseTarget"))
            {
                return false;
            }

            SimpleChar target = DynelManager.Characters.Where(IsAoeRootSnareSpamTarget).Where(DoesNotHaveAoeRootRunning).FirstOrDefault();

            if (IsSettingEnabled("UseAoeRoot") && target != null)
            {
                return false;
            }

            return NeedsDebuffRefresh(spell, fightingTarget);
        }

        private bool Calm12Man(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            List<SimpleChar> targets = DynelManager.NPCs
                .Where(x => x.IsAlive)
                .Where(x => x.Name == "Right Hand of Madness" || x.Name == "Deranged Xan")
                .Where(x => x.DistanceFrom(DynelManager.LocalPlayer) < 30f)
                .Where(x => !x.Buffs.Contains(267535) || !x.Buffs.Contains(267536))
                .ToList();

            if (!IsSettingEnabled("Calm12Man"))
            {
                return false;
            }

            actionTarget.Target = targets.FirstOrDefault();
            actionTarget.ShouldSetTarget = true;

            return true;
        }

        private bool LEInitTargetDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("LEInitDebuffs"))
            {
                return false;
            }

            SimpleChar target = DynelManager.Characters.Where(IsAoeRootSnareSpamTarget).Where(DoesNotHaveAoeRootRunning).FirstOrDefault();

            if (IsSettingEnabled("UseAoeRoot") && target != null)
            {
                return false;
            }

            List<SimpleChar> targets = DynelManager.NPCs
                .Where(x => x.IsAlive)
                .Where(x => x.IsAttacking)
                .Where(x => Team.Members.Any(c => x.FightingTarget.Identity == c.Character.Identity))
                .ToList();

            if (targets.Count >= 2)
                return false;

            return NeedsDebuffRefresh(spell, fightingTarget);
        }

        private bool CratDebuffOthersInCombat(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) 
        {
            SimpleChar target = DynelManager.Characters.Where(IsAoeRootSnareSpamTarget).Where(DoesNotHaveAoeRootRunning).FirstOrDefault();

            if (IsSettingEnabled("UseAoeRoot") && target != null)
            {
                return false;
            }

            return ToggledDebuffOthersInCombat("MalaiseOnOthers", spell, fightingTarget, ref actionTarget);
        }

        private bool NeedsDebuffRefresh(Spell spell, SimpleChar target)
        {
            if (target == null)
            {
                return false;
            }

            if (spell.Nanoline == NanoLine.InitiativeDebuffs && target.Buffs.Contains(301844))
            {
                return false;
            }

            //Check the remaining time on debuffs. On the enemy target
            return !target.Buffs.Where(buff => buff.Name == spell.Name)
                .Where(buff => buff.RemainingTime > 1)
                .Any();
        }

        protected bool PetDivertTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("DivertTrimmer") || !CanLookupPetsAfterZone() || !CanTrim())
            {
                return false;
            }

            Pet petToTrim = FindPetThat(CanDivertTrim);
            if (petToTrim != null)
            {
                actiontarget.Target = petToTrim.Character;
                actiontarget.ShouldSetTarget = true;
                _lastPetTrimDivertTime[petToTrim.Type] = Time.NormalTime;
                _lastTrimTime = Time.NormalTime;
                return true;
            }
            return false;
        }

        protected bool PetAggDefTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("AggDefTrimmer") || !CanLookupPetsAfterZone() || !CanTrim())
            {
                return false;
            }

            Pet petToTrim = FindPetThat(CanAggDefTrim);
            if (petToTrim != null)
            {
                actiontarget.Target = petToTrim.Character;
                actiontarget.ShouldSetTarget = true;
                petTrimmedAggDef[petToTrim.Type] = true;
                _lastTrimTime = Time.NormalTime;
                return true;
            }
            return false;
        }

        protected bool PetAggressiveTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("TauntTrimmer") || !CanLookupPetsAfterZone() || !CanTrim())
            {
                return false;
            }

            Pet petToTrim = FindPetThat(CanTauntTrim);
            if (petToTrim != null)
            {
                actiontarget = (petToTrim.Character, true);
                attackPetTrimmedAggressive = true;
                _lastTrimTime = Time.NormalTime;
                return true;
            }
            return false;
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            SimpleChar target = DynelManager.Characters.Where(IsAoeRootSnareSpamTarget).Where(DoesNotHaveAoeRootRunning).FirstOrDefault();

            Spell workplace = Spell.List.Where(x => x.Name == "Workplace Depression").FirstOrDefault();

            if (fightingTarget == null || !IsSettingEnabled("UseNukes"))
            {
                return false;
            }

            if (IsSettingEnabled("UseAoeRoot") && target != null)
            {
                return false;
            }

            if (workplace != null && (!fightingTarget.Buffs.Contains(301842) || !fightingTarget.Buffs.Contains(222687)))
            {
                return false;
            }

            if (IsSettingEnabled("MalaiseTarget"))
            {
                SimpleChar targets = DynelManager.NPCs
                    .Where(x => x.IsAlive)
                    .Where(x => x.IsAttacking)
                    .Where(x => Team.Members.Any(c => x.FightingTarget.Identity == c.Character.Identity))
                    .Where(x => !x.Buffs.Contains(NanoLine.InitiativeDebuffs))
                    .FirstOrDefault();

                if (targets != null)
                    return false;
            }

            if (IsSettingEnabled("MalaiseOnOthers"))
            {
                List<SimpleChar> targets = DynelManager.NPCs
                    .Where(x => x.IsAlive)
                    .Where(x => x.IsAttacking)
                    .Where(x => Team.Members.Any(c => x.FightingTarget.Identity == c.Character.Identity))
                    .Where(x => !x.Buffs.Contains(NanoLine.InitiativeDebuffs))
                    .ToList();

                if (targets.Count >= 1)
                    return false;
            }

            if (IsSettingEnabled("LEInitDebuffs"))
            {
                List<SimpleChar> targetss = DynelManager.NPCs
                    .Where(x => x.IsAlive)
                    .Where(x => x.IsAttacking)
                    .Where(x => Team.Members.Any(c => x.FightingTarget.Identity == c.Character.Identity))
                    .Where(x => !x.Buffs.Contains(NanoLine.GeneralRadiationACDebuff) && !x.Buffs.Contains(NanoLine.GeneralProjectileACDebuff))
                    .ToList();

                if (targetss.Count >= 1)
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

        protected bool CanTrim()
        {
            return _lastTrimTime + DelayBetweenTrims < Time.NormalTime;
        }
        protected bool CanDivertTrim(Pet pet)
        {
            return _lastPetTrimDivertTime[pet.Type] + DelayBetweenDiverTrims < Time.NormalTime;
        }

        protected bool CanAggDefTrim(Pet pet)
        {
            return !petTrimmedAggDef[pet.Type];
        }

        protected bool CanTauntTrim(Pet pet)
        {
            return pet.Type == PetType.Attack && !attackPetTrimmedAggressive;
        }

        private void ResetTrimmers()
        {
            attackPetTrimmedAggressive = false;
            petTrimmedAggDef[PetType.Attack] = false;
            petTrimmedAggDef[PetType.Support] = false;
        }

        private void OnZoned(object s, EventArgs e)
        {

            ResetTrimmers();
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
            if (!IsSettingEnabled("BuffAAOAAD"))
            {
                CancelBuffs(RelevantNanos.AadBuffAuras);
            }
            if (!IsSettingEnabled("BuffCrit"))
            {
                CancelBuffs(RelevantNanos.CritBuffAuras);
            }
            if (!IsSettingEnabled("BuffNanoResist"))
            {
                CancelBuffs(RelevantNanos.NanoResBuffAuras);
            }
        }

        private void CancelDebuffAurasIfNeeded()
        {
            CancelHostileAuras(RelevantNanos.CritDebuffAuras);
            CancelHostileAuras(RelevantNanos.NanoPointsDebuffAuras);
            CancelHostileAuras(RelevantNanos.NanoResDebuffAuras);

            if (!IsSettingEnabled("DebuffCrit"))
            {
                CancelBuffs(RelevantNanos.CritDebuffAuras);
            }
            if (!IsSettingEnabled("DebuffNanoDrain"))
            {
                CancelBuffs(RelevantNanos.NanoPointsDebuffAuras);
            }
            if (!IsSettingEnabled("DebuffNanoResist"))
            {
                CancelBuffs(RelevantNanos.NanoResDebuffAuras);
            }
        }

        private static class RelevantNanos
        {
            public const int WorkplaceDepression = 273631;
            public const int DroidDamageMatrix = 267916;
            public const int DroidPressureMatrix = 302247;
            public const int CorporateStrategy = 267611;
            public const int LastMinNegotiations = 267535;
            public static readonly int[] SingleTargetNukes = { 273307, WorkplaceDepression, 270250, 78400, 30082, 78394, 78395, 82000, 78396, 78397, 30091, 78399, 81996, 30083, 81997, 30068, 81998, 78398, 81999, 29618 };
            public static readonly int[] AoeRoots = { 224129, 224127, 224125, 224123, 224121, 224119, 82166, 82164, 82163, 82161, 82160, 82159, 82158, 82157, 82156 };
            public static readonly int[] AoeRootDebuffs = { 82137, 244634, 244633, 244630, 244631, 244632, 82138, 82139, 244629, 82140, 82141, 82142, 82143, 82144, 82145 };
            public static readonly int[] AadBuffAuras = { 270783, 155807, 155806, 155805, 155809, 155808 };
            public static readonly int[] CritBuffAuras = { 157503, 157499 };
            public static readonly int[] NanoResBuffAuras = { 157504, 157500, 157501, 157502 };
            public static readonly int[] NanoPointsDebuffAuras = { 275826, 157524, 157534, 157533, 157532, 157531 };
            public static readonly int[] CritDebuffAuras = { 157530, 157529, 157528 };
            public static readonly int[] NanoResDebuffAuras = { 157527, 157526, 157525, 157535 };
            public static readonly int[] GeneralRadACDebuff = { 302143, 302142 };
            public static readonly int[] GeneralProjACDebuff = { 302150, 302152 };

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

        private static class RelevantTrimmers
        {
            public const int IncreaseAggressivenessLow = 154940;
            public const int IncreaseAggressivenessHigh = 154940;
            public const int DivertEnergyToOffense = 88378;
            public const int PositiveAggressiveDefensive = 88384;
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
