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
    public class CratCombatHandler : GenericCombatHandler
    {
        private double _lastTrimTime = 0;
        private const float DelayBetweenTrims = 1;
        private const float DelayBetweenDiverTrims = 305;
        private bool attackPetTrimmedAggressive = false;

        public static Window buffWindow;
        public static Window debuffWindow;
        public static Window petWindow;
        public static Window aidingWindow;

        public static string PluginDirectory;

        private Dictionary<PetType, bool> petTrimmedAggDef = new Dictionary<PetType, bool>();
        private Dictionary<PetType, double> _lastPetTrimDivertTime = new Dictionary<PetType, double>()
        {
            { PetType.Attack, 0 },
            { PetType.Support, 0 }
        };

        public CratCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("BuffCrit", false);
            settings.AddVariable("BuffAAOAAD", true);
            settings.AddVariable("BuffNanoResist", false);
            settings.AddVariable("DebuffCrit", false);
            settings.AddVariable("DebuffNanoResist", false);
            settings.AddVariable("DebuffNanoDrain", false);

            settings.AddVariable("NanoDeltaTeam", false);

            settings.AddVariable("SpawnPets", true);
            settings.AddVariable("BuffPets", true);

            settings.AddVariable("MastersBidding", false);

            settings.AddVariable("MalaiseTarget", true);
            settings.AddVariable("LEInitDebuffs", true);
            settings.AddVariable("OSMalaise", false);

            settings.AddVariable("DivertTrimmer", false);
            settings.AddVariable("TauntTrimmer", false);
            settings.AddVariable("AggDefTrimmer", false);

            settings.AddVariable("Nukes", false);
            settings.AddVariable("AoeRoot", false);

            settings.AddVariable("AoECalm", false);
            settings.AddVariable("SLCalm", false);
            settings.AddVariable("RKCalm", false);
            settings.AddVariable("Calm12Man", false);
            settings.AddVariable("CalmSector7", false);

            RegisterSettingsWindow("Bureaucrat Handler", "BureaucratSettingsView.xml");

            RegisterSettingsWindow("Pets", "BureaucratPetsView.xml");
            RegisterSettingsWindow("Aiding", "BureaucratAidingView.xml");
            RegisterSettingsWindow("Buffs", "BureaucratBuffsView.xml");
            RegisterSettingsWindow("Debuffs", "BureaucratDebuffsView.xml");

            //LE Procs
            //RegisterPerkProcessor(PerkHash.LEProcBureaucratPleaseHold, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratWrongWindow, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratFormsInTriplicate, LEProc, CombatActionPriority.Low);

            //Debuffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder(), CratDebuffOthersInCombat, CombatActionPriority.Medium);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder(), MalaiseTargetDebuff, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.GeneralRadACDebuff, LEInitTargetDebuff);
            RegisterSpellProcessor(RelevantNanos.GeneralProjACDebuff, LEInitTargetDebuff);
            RegisterSpellProcessor(RelevantNanos.AoeRoots, AoeRoot, CombatActionPriority.High);

            RegisterSpellProcessor(RelevantNanos.ShadowlandsCalms, SLCalmDebuff, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.AoECalms, AoECalmDebuff, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.RkCalms, RKCalmDebuff, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.LastMinNegotiations, Calm12Man, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.RkCalms, CalmSector7, CombatActionPriority.High);

            //Debuff Aura
            RegisterSpellProcessor(RelevantNanos.NanoPointsDebuffAuras, DebuffNanoDrainAura);
            RegisterSpellProcessor(RelevantNanos.NanoResDebuffAuras, DebuffNanoResistAura);
            RegisterSpellProcessor(RelevantNanos.CritDebuffAuras, DebuffCritAura);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke, CombatActionPriority.Low);
            RegisterSpellProcessor(RelevantNanos.WorkplaceDepression, WorkplaceDepressionTargetDebuff, CombatActionPriority.Low);
            RegisterSpellProcessor(RelevantNanos.PistolBuffsSelf, PistolSelfBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Psy_IntBuff).OrderByStackingOrder(), GenericBuff);

            //Buff Aura
            RegisterSpellProcessor(RelevantNanos.AadBuffAuras, BuffAAOAADAura);
            RegisterSpellProcessor(RelevantNanos.CritBuffAuras, BuffCritAura);
            RegisterSpellProcessor(RelevantNanos.NanoResBuffAuras, BuffNanoResistAura);

            //Team Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDeltaBuffs).OrderByStackingOrder(), NanoDelta);
            RegisterSpellProcessor(RelevantNanos.PistolBuffs, PistolMasteryBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalDecreaseBuff).OrderByStackingOrder(), TeamBuff);

            //Pet Buffs
            if (Spell.Find(RelevantNanos.CorporateStrategy, out Spell spell))
            {
                RegisterSpellProcessor(RelevantNanos.CorporateStrategy, CorporateStrategy);
            }
            else
            {
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetShortTermDamageBuffs).OrderByStackingOrder(), PetTargetBuff);
            }

            RegisterSpellProcessor(RelevantNanos.PetCleanse, PetCleanse);
            RegisterSpellProcessor(RelevantNanos.MastersBidding, MastersBidding);
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

            //Pet Perks
            RegisterPerkProcessor(PerkHash.Puppeteer, Puppeteer);

            Game.TeleportEnded += OnZoned;

            PluginDirectory = pluginDir;
        }

        private void PetView(object s, ButtonBase button)
        {
            if (buffWindow != null && buffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Pets", buffWindow);
            }
            else if (debuffWindow != null && debuffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Pets", debuffWindow);
            }
            else if (aidingWindow != null && aidingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Pets", aidingWindow);
            }
            else
            {
                petWindow = Window.CreateFromXml("Pets", PluginDirectory + "\\UI\\BureaucratPetsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                petWindow.Show(true);
            }
        }

        private void BuffView(object s, ButtonBase button)
        {
            if (petWindow != null && petWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", petWindow);
            }
            else if (debuffWindow != null && debuffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", debuffWindow);
            }
            else if (aidingWindow != null && aidingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", aidingWindow);
            }
            else
            {
                buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\BureaucratBuffsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                buffWindow.Show(true);
            }
        }

        private void DebuffView(object s, ButtonBase button)
        {
            if (petWindow != null && petWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Debuffs", petWindow);
            }
            else if (buffWindow != null && buffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Debuffs", buffWindow);
            }
            else if (aidingWindow != null && aidingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Debuffs", aidingWindow);
            }
            else
            {
                debuffWindow = Window.CreateFromXml("Debuffs", PluginDirectory + "\\UI\\BureaucratDebuffsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                debuffWindow.Show(true);
            }
        }

        private void AidingView(object s, ButtonBase button)
        {
            if (petWindow != null && petWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Aiding", petWindow);
            }
            else if (buffWindow != null && buffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Aiding", buffWindow);
            }
            else if (debuffWindow != null && debuffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Aiding", debuffWindow);
            }
            else
            {
                aidingWindow = Window.CreateFromXml("Aiding", PluginDirectory + "\\UI\\BureaucratAidingView.xml",
                    windowSize: new Rect(0, 0, 340, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                aidingWindow.Show(true);
            }
        }

        private void OnZoned(object s, EventArgs e)
        {
            ResetTrimmers();
        }

        protected override void OnUpdate(float deltaTime)
        {
            SynchronizePetCombatStateWithOwner();

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                if (SettingsController.settingsWindow.FindView("AidingView", out Button helpView))
                {
                    helpView.Tag = SettingsController.settingsWindow;
                    helpView.Clicked = AidingView;
                }

                if (SettingsController.settingsWindow.FindView("PetsView", out Button petView))
                {
                    petView.Tag = SettingsController.settingsWindow;
                    petView.Clicked = PetView;
                }

                if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                {
                    buffView.Tag = SettingsController.settingsWindow;
                    buffView.Clicked = BuffView;
                }

                if (SettingsController.settingsWindow.FindView("DebuffsView", out Button debuffView))
                {
                    debuffView.Tag = SettingsController.settingsWindow;
                    debuffView.Clicked = DebuffView;
                }
            }

            base.OnUpdate(deltaTime);

            CancelDebuffAurasIfNeeded();
            CancelBuffAurasIfNeeded();
        }


        #region Buffs

        private bool NanoDelta(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam)
            {
                if (IsSettingEnabled("NanoDeltaTeam"))
                {
                    return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);
                }
                else
                {
                    return GenericBuff(spell, fightingTarget, ref actionTarget);
                }
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        public bool PetCleanse(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanLookupPetsAfterZone()) { return false; }

            List<Pet> pets = DynelManager.LocalPlayer.Pets
                .Where(x => x.Character.Buffs.Contains(NanoLine.Root) || x.Character.Buffs.Contains(NanoLine.Snare)
                || x.Character.Buffs.Contains(NanoLine.Mezz))
                .ToList();

            if (pets?.Count > 1)
            {
                return true;
            }

            return false;
        }

        protected bool MastersBidding(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            if (!IsSettingEnabled("MastersBidding")) { return false; }

            Pet petToBuff = FindPetThat(pet => !pet.Character.Buffs.Contains(NanoLine.SiphonBox683)
                && (pet.Character.GetStat(Stat.NPCFamily) != 98)
                && (pet.Type == PetType.Attack || pet.Type == PetType.Support));

            if (petToBuff != null)
            {
                spell.Cast(petToBuff.Character, true);
            }

            return false;
        }

        public bool Puppeteer(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.FightingTarget == null || fightingTarget == null) { return false; }

            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            Pet petToPerk = FindPetThat(CanPerkPuppeteer);
            if (petToPerk != null)
            {
                actionTarget.Target = petToPerk.Character;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }
        private bool BuffCritAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("BuffCrit") || IsSettingEnabled("BuffAAOAAD") || !IsSettingEnabled("BuffAAOAAD")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool BuffNanoResistAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("BuffCrit") || IsSettingEnabled("BuffAAOAAD") || !IsSettingEnabled("BuffNanoResist")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool BuffAAOAADAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("BuffNanoResist") || IsSettingEnabled("BuffCrit") || !IsSettingEnabled("BuffAAOAAD")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DebuffCritAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("DebuffNanoResist") || IsSettingEnabled("DebuffNanoDrain") || !IsSettingEnabled("DebuffCrit")) { return false; }

            return CombatBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DebuffNanoResistAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("DebuffCrit") || IsSettingEnabled("DebuffNanoDrain") || !IsSettingEnabled("DebuffNanoResist")) { return false; }

            return CombatBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DebuffNanoDrainAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("DebuffNanoResist") || IsSettingEnabled("DebuffCrit") || !IsSettingEnabled("DebuffNanoDrain")) { return false; }

            return CombatBuff(spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Debuffs

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //Spell workplace = Spell.List.Where(x => x.Name == "Workplace Depression").FirstOrDefault();

            if (fightingTarget == null || !CanCast(spell)) { return false; }

            if (!IsSettingEnabled("Nukes")) { return false; }

            //if (IsSettingEnabled("Calm12Man"))
            //{
            //    List<SimpleChar> target = DynelManager.NPCs
            //        .Where(x => x.IsAlive)
            //        .Where(x => x.Name == "Right Hand of Madness" || x.Name == "Deranged Xan")
            //        .Where(x => x.DistanceFrom(DynelManager.LocalPlayer) < 20f)
            //        .Where(x => !x.Buffs.Contains(267535) || !x.Buffs.Contains(267536))
            //        .ToList();

            //    if (target != null)
            //        return false;
            //}

            //if (IsSettingEnabled("AoeRoot"))
            //{
            //    SimpleChar target = DynelManager.NPCs
            //        .Where(c => c.Name == "Flaming Vengeance" ||
            //        c.Name == "Hand of the Colonel")
            //        .Where(c => DoesNotHaveAoeRootRunning(c))
            //        .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f)
            //        .FirstOrDefault();

            //    if (target != null)
            //        return false;
            //}

            //if (IsSettingEnabled("MalaiseTarget"))
            //{
            //    SimpleChar target = DynelManager.NPCs
            //        .Where(x => x.IsAlive)
            //        .Where(x => x.Identity == fightingTarget.Identity)
            //        .Where(c => !debuffTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
            //        .Where(x => x.DistanceFrom(DynelManager.LocalPlayer) < 30f)
            //        .Where(x => !x.Buffs.Contains(NanoLine.InitiativeDebuffs))
            //        .FirstOrDefault();

            //    if (target != null)
            //        return false;
            //}

            //if (IsSettingEnabled("OSMalaise"))
            //{
            //    List<SimpleChar> target = DynelManager.NPCs
            //        .Where(x => x.IsAlive)
            //        .Where(c => !debuffTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
            //        .Where(x => x.FightingTarget != null)
            //        .Where(x => x.DistanceFrom(DynelManager.LocalPlayer) < 30f)
            //        .Where(x => !x.Buffs.Contains(NanoLine.InitiativeDebuffs))
            //        .ToList();

            //    if (target.Count >= 1)
            //        return false;
            //}

            //if (IsSettingEnabled("LEInitDebuffs"))
            //{
            //    SimpleChar target = DynelManager.NPCs
            //        .Where(x => x.IsAlive)
            //        .Where(x => x.Identity == fightingTarget.Identity)
            //        .Where(c => !debuffTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
            //        .Where(x => x.DistanceFrom(DynelManager.LocalPlayer) < 30f)
            //        .Where(x => !x.Buffs.Contains(NanoLine.GeneralRadiationACDebuff) && !x.Buffs.Contains(NanoLine.GeneralProjectileACDebuff))
            //        .FirstOrDefault();

            //    if (target != null)
            //        return false;
            //}

            if (Spell.Find(273631, out Spell workplace))
            {
                if (!fightingTarget.Buffs.Contains(273632) && !fightingTarget.Buffs.Contains(301842) &&
                    ((fightingTarget.HealthPercent >= 40 && fightingTarget.MaxHealth < 1000000)
                    || fightingTarget.MaxHealth > 1000000)) { return false; }
            }

            //if (workplace != null)
            //{
            //    if (!fightingTarget.Buffs.Contains(273632) && !fightingTarget.Buffs.Contains(301842) && 
            //        ((fightingTarget.HealthPercent >= 40 && fightingTarget.MaxHealth < 1000000) 
            //        || fightingTarget.MaxHealth > 1000000))
            //        return false;
            //}

            return true;
        }

        private bool WorkplaceDepressionTargetDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !CanCast(spell)) { return false; }

            if (!IsSettingEnabled("Nukes")) { return false; }

            //if (IsSettingEnabled("Calm12Man"))
            //{
            //    List<SimpleChar> target = DynelManager.NPCs
            //        .Where(x => x.IsAlive)
            //        .Where(x => x.Name == "Right Hand of Madness" || x.Name == "Deranged Xan")
            //        .Where(x => x.DistanceFrom(DynelManager.LocalPlayer) < 20f)
            //        .Where(x => !x.Buffs.Contains(267535) || !x.Buffs.Contains(267536))
            //        .ToList();

            //    if (target != null)
            //        return false;
            //}

            //if (IsSettingEnabled("AoeRoot"))
            //{
            //    SimpleChar target = DynelManager.NPCs
            //        .Where(c => c.Name == "Flaming Vengeance" ||
            //        c.Name == "Hand of the Colonel")
            //        .Where(c => DoesNotHaveAoeRootRunning(c))
            //        .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f)
            //        .FirstOrDefault();

            //    if (target != null)
            //        return false;
            //}

            //if (IsSettingEnabled("MalaiseTarget"))
            //{
            //    SimpleChar target = DynelManager.NPCs
            //        .Where(x => x.IsAlive)
            //        .Where(x => x.Identity == fightingTarget.Identity)
            //        .Where(x => x.DistanceFrom(DynelManager.LocalPlayer) < 30f)
            //        .Where(x => !x.Buffs.Contains(NanoLine.InitiativeDebuffs))
            //        .FirstOrDefault();

            //    if (target != null)
            //        return false;
            //}

            //if (IsSettingEnabled("OSMalaise"))
            //{
            //    List<SimpleChar> target = DynelManager.NPCs
            //        .Where(x => x.IsAlive)
            //        .Where(c => !debuffTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
            //        .Where(x => x.FightingTarget != null)
            //        .Where(x => x.DistanceFrom(DynelManager.LocalPlayer) < 30f)
            //        .Where(x => !x.Buffs.Contains(NanoLine.InitiativeDebuffs))
            //        .ToList();

            //    if (target.Count >= 1)
            //        return false;
            //}

            //if (IsSettingEnabled("LEInitDebuffs"))
            //{
            //    SimpleChar target = DynelManager.NPCs
            //        .Where(x => x.IsAlive)
            //        .Where(x => x.Identity == fightingTarget.Identity)
            //        .Where(x => x.DistanceFrom(DynelManager.LocalPlayer) < 30f)
            //        .Where(x => !x.Buffs.Contains(NanoLine.GeneralRadiationACDebuff) && !x.Buffs.Contains(NanoLine.GeneralProjectileACDebuff))
            //        .FirstOrDefault();

            //    if (target != null)
            //        return false;
            //}

            if (fightingTarget.Buffs.Contains(273632) || fightingTarget.Buffs.Contains(301842)) { return false; }

            if (fightingTarget.HealthPercent < 40 && fightingTarget.MaxHealth < 1000000) { return false; }

            return true;
        }

        private bool CalmSector7(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell)) { return false; }

            if (!IsSettingEnabled("CalmSector7")) { return false; }

            SimpleChar target = DynelManager.NPCs
                .Where(c => !debuffTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
                .Where(c => c.IsInLineOfSight)
                .Where(x => x.Name == "Kyr'Ozch Guardian")
                .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f) //Is in range for debuff (we assume weapon range == debuff range)
                .Where(c => !c.Buffs.Contains(NanoLine.Mezz))
                .Where(c => c.MaxHealth < 1000000)
                .FirstOrDefault();

            if (target != null)
            {
                actionTarget = (target, true);
                return true;
            }

            return false;
        }

        private bool RKCalmDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell)) { return false; }

            if (!IsSettingEnabled("RKCalm")) { return false; }

            SimpleChar target = DynelManager.NPCs
                .Where(c => !debuffTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
                .Where(c => c.IsInLineOfSight)
                .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f) //Is in range for debuff (we assume weapon range == debuff range)
                .Where(c => !c.Buffs.Contains(NanoLine.Mezz))
                .Where(c => c.MaxHealth < 1000000)
                .FirstOrDefault();

            if (target != null)
            {
                actionTarget = (target, true);
                return true;
            }

            return false;
        }

        private bool SLCalmDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell)) { return false; }

            if (!IsSettingEnabled("SLCalm")) { return false; }

            SimpleChar target = DynelManager.NPCs
                .Where(c => !debuffTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
                .Where(c => c.IsInLineOfSight)
                .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f) //Is in range for debuff (we assume weapon range == debuff range)
                .Where(c => !c.Buffs.Contains(NanoLine.Mezz))
                .Where(c => c.MaxHealth < 1000000)
                .FirstOrDefault();

            if (target != null)
            {
                actionTarget = (target, true);
                return true;
            }

            return false;
        }

        private bool AoECalmDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell)) { return false; }

            if (!IsSettingEnabled("AoECalm")) { return false; }

            SimpleChar target = DynelManager.NPCs
                .Where(c => !debuffTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
                .Where(c => c.IsInLineOfSight)
                .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f) //Is in range for debuff (we assume weapon range == debuff range)
                .Where(c => !c.Buffs.Contains(NanoLine.AOEMezz))
                .Where(c => c.MaxHealth < 1000000)
                .FirstOrDefault();

            if (target != null)
            {
                actionTarget = (target, true);
                return true;
            }

            return false;
        }

        private bool MalaiseTargetDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !CanCast(spell)) { return false; }

            if (!IsSettingEnabled("MalaiseTarget")) { return false; }

            //if (IsSettingEnabled("Calm12Man"))
            //{
            //    List<SimpleChar> target = DynelManager.NPCs
            //        .Where(x => x.IsAlive)
            //        .Where(x => x.Name == "Right Hand of Madness" || x.Name == "Deranged Xan")
            //        .Where(x => x.DistanceFrom(DynelManager.LocalPlayer) < 20f)
            //        .Where(x => !x.Buffs.Contains(267535) || !x.Buffs.Contains(267536))
            //        .ToList();

            //    if (target != null)
            //        return false;
            //}

            //if (IsSettingEnabled("AoeRoot"))
            //{
            //    SimpleChar target = DynelManager.NPCs
            //        .Where(c => c.Name == "Flaming Vengeance" ||
            //        c.Name == "Hand of the Colonel")
            //        .Where(c => DoesNotHaveAoeRootRunning(c))
            //        .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f)
            //        .FirstOrDefault();

            //    if (target != null)
            //        return false;
            //}

            //if (!SpellChecksOther(spell, fightingTarget)) { return false; }

            return SpellChecksOther(spell, spell.Nanoline, fightingTarget);
        }

        private bool Calm12Man(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Calm12Man") || !CanCast(spell)) { return false; }

            List<SimpleChar> targets = DynelManager.NPCs
                .Where(x => x.IsAlive)
                .Where(x => x.Name == "Right Hand of Madness" || x.Name == "Deranged Xan")
                .Where(x => x.DistanceFrom(DynelManager.LocalPlayer) < 20f)
                .Where(x => !x.Buffs.Contains(267535) || !x.Buffs.Contains(267536))
                .ToList();

            if (targets.Count >= 1)
            {
                actionTarget.Target = targets.FirstOrDefault();
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        private bool LEInitTargetDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !CanCast(spell)) { return false; }

            if (!IsSettingEnabled("LEInitDebuffs")) { return false; }

            //if (IsSettingEnabled("Calm12Man"))
            //{
            //    List<SimpleChar> target = DynelManager.NPCs
            //        .Where(x => x.IsAlive)
            //        .Where(x => x.Name == "Right Hand of Madness" || x.Name == "Deranged Xan")
            //        .Where(x => x.DistanceFrom(DynelManager.LocalPlayer) < 20f)
            //        .Where(x => !x.Buffs.Contains(267535) || !x.Buffs.Contains(267536))
            //        .ToList();

            //    if (target != null)
            //        return false;
            //}

            //if (IsSettingEnabled("AoeRoot"))
            //{
            //    SimpleChar target = DynelManager.NPCs
            //        .Where(c => c.Name == "Flaming Vengeance" ||
            //        c.Name == "Hand of the Colonel")
            //        .Where(c => DoesNotHaveAoeRootRunning(c))
            //        .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f)
            //        .FirstOrDefault();

            //    if (target != null)
            //        return false;
            //}

            //if (!SpellChecksOther(spell, fightingTarget)) { return false; }

            return SpellChecksOther(spell, spell.Nanoline, fightingTarget);
        }

        private bool CratDebuffOthersInCombat(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //if (IsSettingEnabled("Calm12Man"))
            //{
            //    List<SimpleChar> target = DynelManager.NPCs
            //        .Where(x => x.IsAlive)
            //        .Where(x => x.Name == "Right Hand of Madness" || x.Name == "Deranged Xan")
            //        .Where(x => x.DistanceFrom(DynelManager.LocalPlayer) < 20f)
            //        .Where(x => !x.Buffs.Contains(267535) || !x.Buffs.Contains(267536))
            //        .ToList();

            //    if (target != null)
            //        return false;
            //}

            //if (IsSettingEnabled("AoeRoot"))
            //{
            //    SimpleChar target = DynelManager.NPCs
            //        .Where(c => c.Name == "Flaming Vengeance" ||
            //        c.Name == "Hand of the Colonel")
            //        .Where(c => DoesNotHaveAoeRootRunning(c))
            //        .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f)
            //        .FirstOrDefault();

            //    if (target != null)
            //        return false;
            //}

            return ToggledDebuffOthersInCombat("OSMalaise", spell, fightingTarget, ref actionTarget);
        }

        private bool AoeRoot(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AoeRoot") || !CanCast(spell)) { return false; }

            SimpleChar target = DynelManager.NPCs
                .Where(c => c.Name == "Flaming Vengeance" ||
                c.Name == "Hand of the Colonel")
                .Where(c => DoesNotHaveAoeRootRunning(c))
                .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f)
                .FirstOrDefault();

            if (target != null)
            {
                actionTarget.Target = target;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        #endregion

        #region Logic

        private bool CanPerkPuppeteer(Pet pet)
        {
            if (pet.Type != PetType.Attack) { return false; }

            return true;
        }

        protected bool FindSpellNanoLineFallbackToId(Spell spell, Buff[] buffs, out Buff buff)
        {
            if (buffs.Find(spell.Nanoline, out Buff buffFromNanoLine))
            {
                buff = buffFromNanoLine;
                return true;
            }
            int spellId = spell.Identity.Instance;
            if (RelevantNanos.PetNanoToBuff.ContainsKey(spellId))
            {
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

        private bool DoesNotHaveAoeRootRunning(SimpleChar target)
        {
            return !target.Buffs.Any(IsAoeRoot);
        }

        private bool IsAoeRoot(Buff buff)
        {
            return RelevantNanos.AoeRootDebuffs.Any(id => id == buff.Identity.Instance);
        }

        #endregion

        #region Pets

        private bool RobotNeedsBuff(Spell spell, Pet pet)
        {
            if (pet.Type != PetType.Attack) { return false; }

            if (FindSpellNanoLineFallbackToId(spell, pet.Character.Buffs, out Buff buff))
            {
                //Don't cast if weaker than existing
                if (spell.StackingOrder < buff.StackingOrder) { return false; }

                //Don't cast if greater than 10% time remaining
                if (buff.RemainingTime / buff.TotalTime > 0.1) { return false; }
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

        protected bool PetDivertTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("DivertTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

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
            if (!IsSettingEnabled("AggDefTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

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
            if (!IsSettingEnabled("TauntTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

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

        protected bool DroidMatrixBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            Pet petToBuff = FindPetThat(pet => RobotNeedsBuff(spell, pet));
            if (petToBuff != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = petToBuff.Character;
                return true;
            }

            return false;
        }

        #endregion

        #region Misc

        private static class RelevantNanos
        {
            public const int WorkplaceDepression = 273631;
            public const int DroidDamageMatrix = 267916;
            public const int DroidPressureMatrix = 302247;
            public const int CorporateStrategy = 267611;
            public const int LastMinNegotiations = 267535;
            public const int SkilledGunSlinger = 263251;
            public const int GreaterGunSlinger = 263250;
            public const int MastersBidding = 268171;

            public static readonly int[] PistolBuffsSelf = { 263250, 263251 };
            public static readonly Spell[] PistolBuffs = Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder().Where(spell => spell.Identity.Instance != GreaterGunSlinger && spell.Identity.Instance != SkilledGunSlinger).ToArray();
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
            public static readonly int[] PetCleanse = { 269870, 269869 };

            public static readonly int[] ShadowlandsCalms = { 224143, 224141, 224139, 224149, 224147, 224145,
            224137, 224135, 224133, 224131, 219020 };
            public static readonly int[] RkCalms = { 155577, 100428, 100429, 100430, 100431, 100432,
            30093, 30056, 30065 };
            public static readonly int[] AoECalms = { 100422, 100424, 100426 };

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

        #endregion
    }
}
