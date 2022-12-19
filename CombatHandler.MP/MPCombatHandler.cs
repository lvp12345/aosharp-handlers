using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System.Linq;
using System;
using AOSharp.Common.GameData.UI;
using AOSharp.Core.IPC;
using System.Threading.Tasks;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System.Threading;
using SmokeLounge.AOtomation.Messaging.Messages;
using System.Collections.Generic;
using AOSharp.Core.Inventory;
using CombatHandler.Generic;

namespace CombatHandler.Metaphysicist
{
    public class MPCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static Window _buffWindow;
        private static Window _debuffWindow;
        private static Window _petWindow;
        private static Window _procWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _petView;
        private static View _procView;

        private double _lastSwitchedHealTime = 0;
        private double _lastSwitchedMezzTime = 0;

        private static double _ncuUpdateTime;

        public MPCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("SyncPets", true);
            _settings.AddVariable("SpawnPets", true);
            _settings.AddVariable("BuffPets", true);
            _settings.AddVariable("MezzPet", false);
            _settings.AddVariable("WarpPets", false);

            _settings.AddVariable("PetProcSelection", (int)PetProcSelection.None);
            _settings.AddVariable("CompositeNanoSkillsBuffSelection", (int)CompositeNanoSkillsBuffSelection.None);
            _settings.AddVariable("CostBuffSelection", (int)CostBuffSelection.Self);
            _settings.AddVariable("DamageDebuffSelection", (int)DamageDebuffSelection.None);

            _settings.AddVariable("Cost", false);
            _settings.AddVariable("CostTeam", false);

            _settings.AddVariable("InterruptChance", false);

            _settings.AddVariable("NanoResistanceDebuff", false);
            _settings.AddVariable("NanoShutdownDebuff", false);

            _settings.AddVariable("CompositesNanoSkills", false);
            _settings.AddVariable("CompositesNanoSkillsTeam", false);

            //LE Proc
            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.AnticipatedEvasion);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.DiffuseRage);

            _settings.AddVariable("Replenish", false);

            //_settings.AddVariable("MatterCrea", false);
            //_settings.AddVariable("PyschoModi", false);
            //_settings.AddVariable("TimeSpace", false);
            //_settings.AddVariable("SenseImprov", false);
            //_settings.AddVariable("BioMet", false);
            //_settings.AddVariable("MattMet", false);

            //settings.AddVariable("CostTeam", false);

            _settings.AddVariable("Nukes", false);

            //settings.AddVariable("NanoBuffsSelection", (int)NanoBuffsSelection.SL);
            //settings.AddVariable("SummonedWeaponSelection", (int)SummonedWeaponSelection.DISABLED);

            RegisterSettingsWindow("MP Handler", "MPSettingsView.xml");

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistNanobotContingentArrest, NanobotContingentArrest, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistAnticipatedEvasion, AnticipatedEvasion, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistThoughtfulMeans, ThoughtfulMeans, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistRegainFocus, RegainFocus, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistEconomicNanobotUse, EconomicNanobotUse, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistSuperEgoStrike, SuperEgoStrike, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistSuppressFury, SuppressFury, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistEgoStrike, EgoStrike, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistMindWail, MindWail, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistSowDoubt, SowDoubt, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistSowDespair, SowDespair, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistDiffuseRage, DiffuseRage, CombatActionPriority.Low);

            //Self buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), GenericTeamBuffExclusion);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtistBowBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Psy_IntBuff).OrderByStackingOrder(), GenericBuff);

            //Team buffs
            RegisterSpellProcessor(RelevantNanos.MPCompositeNano, CompositeNanoBuff);


            RegisterSpellProcessor(RelevantNanos.PetWarp, PetWarp);
            RegisterSpellProcessor(RelevantNanos.MatMetBuffs, MattMet);
            RegisterSpellProcessor(RelevantNanos.BioMetBuffs, BioMet);
            RegisterSpellProcessor(RelevantNanos.PsyModBuffs, PsyMod);
            RegisterSpellProcessor(RelevantNanos.SenImpBuffs, SenImp);
            RegisterSpellProcessor(RelevantNanos.MatCreBuffs, MatCre);
            RegisterSpellProcessor(RelevantNanos.MatLocBuffs, MatLoc);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InterruptModifier).OrderByStackingOrder(), InterruptModifier);
            RegisterSpellProcessor(RelevantNanos.CostBuffs, Cost);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), Pistol);

            //Debuffs
            RegisterSpellProcessor(RelevantNanos.WarmUpfNukes, WarmUpNuke, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPDamageDebuffLineA).OrderByStackingOrder(), DamageDebuff, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPDamageDebuffLineB).OrderByStackingOrder(), DamageDebuff, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MetaPhysicistDamageDebuff).OrderByStackingOrder(), DamageDebuff, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceDebuff_LineA).OrderByStackingOrder(), NanoResistanceDebuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoShutdownDebuff).OrderByStackingOrder(), NanoShutdownDebuff);

            //Pets
            RegisterSpellProcessor(GetAttackPetsWithSLPetsFirst(), AttackPetSpawner);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SupportPets).OrderByStackingOrder(), SupportPetSpawner);
            RegisterSpellProcessor(RelevantNanos.HealPets, HealPetSpawner);

            //Pet Buffs
            RegisterSpellProcessor(RelevantNanos.PetCleanse, PetCleanse);
            RegisterSpellProcessor(RelevantNanos.MastersBidding, MastersBidding);
            RegisterSpellProcessor(RelevantNanos.InducedApathy, InducedApathy);

            RegisterSpellProcessor(RelevantNanos.EvadeBuff, EvasionPet);
            RegisterSpellProcessor(RelevantNanos.InstillDamageBuffs, InstillDamage);
            RegisterSpellProcessor(RelevantNanos.ChantBuffs, Chant);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MesmerizationConstructEmpowerment).OrderByStackingOrder(), MezzPetSeed);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealingConstructEmpowerment).OrderByStackingOrder(), HealPetSeed);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AggressiveConstructEmpowerment).OrderByStackingOrder(), AttackPetSeed);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPAttackPetDamageType).OrderByStackingOrder(), DamageTypePet);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDamageOverTimeResistNanos).OrderByStackingOrder(), NanoResistancePet);
            RegisterSpellProcessor(RelevantNanos.PetDefensive, DefensivePet);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetHealDelta843).OrderByStackingOrder(), HealDeltaPet);
            RegisterSpellProcessor(RelevantNanos.PetShortTermDamage, ShortTermDamagePet);
            RegisterSpellProcessor(RelevantNanos.CostBuffs, CostPet);

            RegisterPerkProcessor(PerkHash.ChannelRage, ChannelRage);

            PluginDirectory = pluginDir;
        }

        public Window[] _windows => new Window[] { _petWindow, _buffWindow, _debuffWindow };

        #region Callbacks

        public static void OnRemainingNCUMessage(int sender, IPCMessage msg)
        {
            try
            {
                if (Game.IsZoning)
                    return;

                RemainingNCUMessage ncuMessage = (RemainingNCUMessage)msg;
                SettingsController.RemainingNCU[ncuMessage.Character] = ncuMessage.RemainingNCU;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e);
            }
        }

        #endregion

        #region Handles
        private void HandlePetViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_petView)) { return; }

                _petView = View.CreateFromXml(PluginDirectory + "\\UI\\MPPetsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Pets", XmlViewName = "MPPetsView" }, _petView);
            }
            else if (_petWindow == null || (_petWindow != null && !_petWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_petWindow, PluginDir, new WindowOptions() { Name = "Pets", XmlViewName = "MPPetsView" }, _petView, out var container);
                _petWindow = container;
            }
        }

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_buffView)) { return; }

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\MPBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "MPBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "MPBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }

        private void HandleDebuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_debuffView)) { return; }

                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\MPDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "MPDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "MPDebuffsView" }, _debuffView, out var container);
                _debuffWindow = container;
            }
        }

        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\MPProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "MPProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "MPProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            if (Time.NormalTime > _ncuUpdateTime + 0.5f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }

            if (_settings["Replenish"].AsBool() && (_settings["CompositesNanoSkills"].AsBool() || _settings["CompositesNanoSkillsTeam"].AsBool()))
            {
                _settings["CompositesNanoSkills"] = false;
                _settings["CompositesNanoSkillsTeam"] = false;
                _settings["Replenish"] = false;

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
                if (SettingsController.settingsWindow.FindView("PetsView", out Button petView))
                {
                    petView.Tag = SettingsController.settingsWindow;
                    petView.Clicked = HandlePetViewClick;
                }

                if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                {
                    buffView.Tag = SettingsController.settingsWindow;
                    buffView.Clicked = HandleBuffViewClick;
                }

                if (SettingsController.settingsWindow.FindView("DebuffsView", out Button debuffView))
                {
                    debuffView.Tag = SettingsController.settingsWindow;
                    debuffView.Clicked = HandleDebuffViewClick;
                }

                if (SettingsController.settingsWindow.FindView("ProcsView", out Button procView))
                {
                    procView.Tag = SettingsController.settingsWindow;
                    procView.Clicked = HandleProcViewClick;
                }
            }
        }

        #region LE Procs


        private bool AnticipatedEvasion(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.AnticipatedEvasion != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool EconomicNanobotUse(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.EconomicNanobotUse != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool NanobotContingentArrest(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.NanobotContingentArrest != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool RegainFocus(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.RegainFocus != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool ThoughtfulMeans(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.ThoughtfulMeans != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool DiffuseRage(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.DiffuseRage != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool EgoStrike(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.EgoStrike != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool MindWail(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.MindWail != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool SowDespair(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.SowDespair != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool SowDoubt(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.SowDoubt != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool SuperEgoStrike(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.SuperEgoStrike != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool SuppressFury(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.SuppressFury != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Nukes
        private bool WarmUpNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (fightingTarget == null || !IsSettingEnabled("Nukes") || !CanCast(spell)) { return false; }

            Spell singleNuke = Spell.List.FirstOrDefault(x => RelevantNanos.SingleTargetNukes.Contains(x.Id));

            if (singleNuke != null)
            {
                if (fightingTarget.Buffs.Contains(NanoLine.MetaphysicistMindDamageNanoDebuffs)) { return false; }
            }

            return true;
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (fightingTarget == null || !IsSettingEnabled("Nukes") || !CanCast(spell)) { return false; }

            Spell warmupNuke = Spell.List.FirstOrDefault(x => RelevantNanos.WarmUpfNukes.Contains(x.Id));

            if (warmupNuke != null)
            {
                if (!fightingTarget.Buffs.Contains(NanoLine.MetaphysicistMindDamageNanoDebuffs)) { return false; }
            }

            return true;
        }

        #endregion

        #region Buffs
        private bool Cost(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (CostBuffSelection.Team == (CostBuffSelection)_settings["CostBuffSelection"].AsInt32())
                return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool CompositeNanoBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (CompositeNanoSkillsBuffSelection.Team == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
                return GenericTeamBuff(spell, fightingTarget, ref actionTarget);

            if (CompositeNanoSkillsBuffSelection.None == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool MatCre(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Replenish") && CompositeNanoSkillsBuffSelection.None == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
                return GenericNanoSkillsBuff(spell, fightingTarget, ref actionTarget);

            return false;
        }

        private bool PsyMod(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Replenish") && CompositeNanoSkillsBuffSelection.None == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
                return GenericNanoSkillsBuff(spell, fightingTarget, ref actionTarget);

            return false;
        }

        private bool MatLoc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Replenish") && CompositeNanoSkillsBuffSelection.None == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
                return GenericNanoSkillsBuff(spell, fightingTarget, ref actionTarget);

            return false;
        }

        private bool SenImp(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Replenish") && CompositeNanoSkillsBuffSelection.None == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
                return GenericNanoSkillsBuff(spell, fightingTarget, ref actionTarget);

            return false;
        }

        private bool BioMet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Replenish") && CompositeNanoSkillsBuffSelection.None == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
                return GenericNanoSkillsBuff(spell, fightingTarget, ref actionTarget);

            return false;
        }

        private bool MattMet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("Replenish") && CompositeNanoSkillsBuffSelection.None == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
                return GenericNanoSkillsBuff(spell, fightingTarget, ref actionTarget);

            return false;
        }

        private bool InterruptModifier(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("InterruptChance")) { return false; }

            return GenericTeamBuff(spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Debufs

        private bool NanoShutdownDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledTargetDebuff("NanoShutdownDebuff", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool NanoResistanceDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledTargetDebuff("NanoResistanceDebuff", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool DamageDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DamageDebuffSelection.OS == (DamageDebuffSelection)_settings["DamageDebuffSelection"].AsInt32())
                return OSDebuff(spell, ref actionTarget);

            if (DamageDebuffSelection.Target == (DamageDebuffSelection)_settings["DamageDebuffSelection"].AsInt32()
                && fightingTarget != null)
            {
                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }

        #endregion

        #region Pets

        #region Warp

        protected bool PetWarp(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("WarpPets") || !CanCast(spell) || !CanLookupPetsAfterZone()) { return false; }

            return DynelManager.LocalPlayer.Pets.Any(c => c.Character == null);
        }

        #endregion

        #region Perks

        private bool ChannelRage(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (CanPerkChannelRage(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Buffs
        private bool MezzPetSeed(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.MesmerizationConstructEmpowerment);
        }

        private bool HealPetSeed(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.HealingConstructEmpowerment);
        }

        private bool AttackPetSeed(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.AggressiveConstructEmpowerment);
        }

        private bool DamageTypePet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.MPAttackPetDamageType, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool EvasionPet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.MajorEvasionBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.MajorEvasionBuffs, PetType.Heal, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.MajorEvasionBuffs, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool Chant(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.MPPetInitiativeBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool InstillDamage(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.MPPetDamageBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool HealDeltaPet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetHealDelta843, PetType.Attack, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.PetHealDelta843, PetType.Heal, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.PetHealDelta843, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool DefensivePet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetDefensiveNanos, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool NanoResistancePet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetDamageOverTimeResistNanos, PetType.Attack, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.PetDamageOverTimeResistNanos, PetType.Heal, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.PetDamageOverTimeResistNanos, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool ShortTermDamagePet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetShortTermDamageBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool CostPet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.NPCostBuff, PetType.Heal, spell, fightingTarget, ref actionTarget) ||
                PetTargetBuff(NanoLine.NPCostBuff, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        protected bool InducedApathy(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            if (PetProcSelection.InducedApathy != (PetProcSelection)_settings["PetProcSelection"].AsInt32()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (!pet.Character.Buffs.Contains(NanoLine.SiphonBox683)
                    && pet.Type == PetType.Attack)
                {
                    if (spell.IsReady)
                        spell.Cast(pet.Character, true);

                    //Not working for some reason

                    //actionTarget.Target = pet.Character;
                    //actionTarget.ShouldSetTarget = true;

                    //return true;
                }
            }

            return false;
        }

        protected bool MastersBidding(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            if (PetProcSelection.MastersBidding != (PetProcSelection)_settings["PetProcSelection"].AsInt32()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (!pet.Character.Buffs.Contains(NanoLine.SiphonBox683)
                    && pet.Type == PetType.Attack)
                {
                    if (spell.IsReady)
                        spell.Cast(pet.Character, true);

                    //Not working for some reason

                    //actionTarget.Target = pet.Character;
                    //actionTarget.ShouldSetTarget = true;

                    //return true;
                }
            }

            return false;
        }

        #endregion

        #endregion

        #region Misc

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

        private bool CanPerkChannelRage(Pet pet)
        {
            if (!pet.Character.IsAlive) { return false; }

            if (pet.Type != PetType.Attack) { return false; }

            return !pet.Character.Buffs.Any(buff => buff.Nanoline == NanoLine.ChannelRage);
        }

        private Spell[] GetAttackPetsWithSLPetsFirst()
        {
            List<Spell> attackPetsWithoutSL = Spell.GetSpellsForNanoline(NanoLine.AttackPets).Where(spell => !RelevantNanos.SLAttackPets.Contains(spell.Id)).OrderByStackingOrder().ToList();
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
                .Where(c => !debuffOSTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
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
            public const int InducedApathy = 301888;
            public const int EvadeBuff = 29272;
            public const int PetWarp = 209488;
            public static readonly int[] CostBuffs = { 95409, 29307, 95411, 95408, 95410 };
            public static readonly int[] HealPets = { 225902, 125746, 125739, 125740, 125741, 125742, 125743, 125744, 125745, 125738 }; //Belamorte has a higher stacking order than Moritficant
            public static readonly int[] SLAttackPets = { 254859, 225900, 254859, 225900, 225898, 225896, 225894 };
            public static readonly int[] MPCompositeNano = { 220343, 220341, 220339, 220337, 220335, 220333, 220331 };
            public static readonly int[] WarmUpfNukes = { 270355, 125761, 29297, 125762, 29298, 29114 };
            public static readonly int[] PetDefensive = { 267601, 267600, 267599 };
            public static readonly int[] PetCleanse = { 269870, 269869 };

            public static readonly int[] PetShortTermDamage = { 267598, 205193, 151827, 205189, 205187, 151828, 205185, 151824, 205183,
            151830, 205191, 151826, 205195, 151825, 205197, 151831 };

            public static readonly int[] SingleTargetNukes = { 267878, 125763, 125760, 125765, 125764 };
            public static readonly int[] InstillDamageBuffs = { 270800, 285101, 116814, 116817, 116812, 116816, 116821, 116815, 116813 };
            public static readonly int[] ChantBuffs = { 116819, 116818, 116811, 116820 };
            public static readonly int[] MatMetBuffs = Spell.GetSpellsForNanoline(NanoLine.MatMetBuff).OrderByStackingOrder().Select(spell => spell.Id).ToArray();
            public static readonly int[] BioMetBuffs = Spell.GetSpellsForNanoline(NanoLine.BioMetBuff).OrderByStackingOrder().Select(spell => spell.Id).ToArray();
            public static readonly int[] PsyModBuffs = Spell.GetSpellsForNanoline(NanoLine.PsyModBuff).OrderByStackingOrder().Select(spell => spell.Id).ToArray();
            public static readonly int[] SenImpBuffs = { 29304, 151757, 29315, 151764 }; //Composites count as SenseImp buffs. Have to be excluded
            public static readonly int[] MatCreBuffs = Spell.GetSpellsForNanoline(NanoLine.MatCreaBuff).OrderByStackingOrder().Select(spell => spell.Id).ToArray();
            public static readonly int[] MatLocBuffs = Spell.GetSpellsForNanoline(NanoLine.MatLocBuff).OrderByStackingOrder().Select(spell => spell.Id).ToArray();

            //public static readonly string[] TwoHandedNames = { "Azure Cobra of Orma", "Wixel's Notum Python", "Asp of Semol", "Viper Staff" };
            //public static readonly string[] OneHandedNames = { "Asp of Titaniush", "Gold Acantophis", "Bitis Striker", "Coplan's Hand Taipan", "The Crotalus" };
            //public static readonly string[] ShieldNames = { "Shield of Zset", "Shield of Esa", "Shield of Asmodian", "Mocham's Guard", "Death Ward", "Belthior's Flame Ward", "Wave Breaker", "Living Shield of Evernan", "Solar Guard", "Notum Defender", "Vital Buckler" };
        }

        public enum PetProcSelection
        {
            None, InducedApathy, MastersBidding
        }
        public enum CompositeNanoSkillsBuffSelection
        {
            None, Self, Team
        }
        public enum DamageDebuffSelection
        {
            None, Target, OS
        }
        
        public enum CostBuffSelection
        {
            Self, Team
        }
        public enum ProcType1Selection
        {
            NanobotContingentArrest, AnticipatedEvasion, ThoughtfulMeans, RegainFocus, EconomicNanobotUse
        }

        public enum ProcType2Selection
        {
            SuperEgoStrike, SuppressFury, EgoStrike, MindWail, SowDoubt, SowDespair, DiffuseRage
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

        #endregion
    }
}
