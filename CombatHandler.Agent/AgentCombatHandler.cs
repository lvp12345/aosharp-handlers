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

namespace CombatHandler.Agent
{
    public class AgentCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static int AgentHealPercentage;
        private static int AgentCompleteHealPercentage;

        private double _lastSwitchedHealTime = 0;

        private static Window _buffWindow;
        private static Window _debuffWindow;
        private static Window _procWindow;
        private static Window _falseProfWindow;
        private static Window _healingWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _procView;
        private static View _falseProfView;
        private static View _healingView;

        private static double _ncuUpdateTime;

        public AgentCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            Config.CharSettings[Game.ClientInst].AgentHealPercentageChangedEvent += AgentHealPercentage_Changed;
            Config.CharSettings[Game.ClientInst].AgentCompleteHealPercentageChangedEvent += AgentCompleteHealPercentage_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("DotStrainA", false);

            _settings.AddVariable("CritTeam", false);

            _settings.AddVariable("InitDebuffSelection", (int)InitDebuffSelection.None);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.GrimReaper);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.LaserAim);

            _settings.AddVariable("HealSelection", (int)HealSelection.None);

            _settings.AddVariable("EvasionDebuff", false);

            _settings.AddVariable("CH", false);
            _settings.AddVariable("Damage", false);
            _settings.AddVariable("Detaunt", false);

            _settings.AddVariable("Concentration", false);

            _settings.AddVariable("FalseProfSelection", (int)FalseProfSelection.None);

            RegisterSettingsWindow("Agent Handler", "AgentSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcAgentGrimReaper, GrimReaper, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAgentDisableCuffs, DisableCuffs, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAgentNoEscape, NoEscape, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAgentIntenseMetabolism, IntenseMetabolism, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAgentMinorNanobotEnhance, MinorNanobotEnhance, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcAgentNotumChargedRounds, NotumChargedRounds, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAgentLaserAim, LaserAim, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAgentNanoEnhancedTargeting, NanoEnhancedTargeting, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAgentPlasteelPiercingRounds, PlasteelPiercingRounds, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAgentCellKiller, CellKiller, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAgentImprovedFocus, ImprovedFocus, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAgentBrokenAnkle, BrokenAnkle, CombatActionPriority.Low);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AgilityBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AimedShotBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ConcealmentBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalIncreaseBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ExecutionerBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RifleBuffs).OrderByStackingOrder(), RifleBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AgentProcBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ConcentrationCriticalLine).OrderByStackingOrder(), Concentration, CombatActionPriority.Medium);

            RegisterSpellProcessor(RelevantNanos.HEALS, Healing, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.CH, CompleteHealing, CombatActionPriority.High);

            //False Profs
            RegisterSpellProcessor(RelevantNanos.FalseProfDoc, FalseProfDoctor);
            RegisterSpellProcessor(RelevantNanos.FalseProfAdv, FalseProfAdventurer);
            RegisterSpellProcessor(RelevantNanos.FalseProfCrat, FalseProfBeauracrat);
            RegisterSpellProcessor(RelevantNanos.FalseProfEnf, FalseProfEnforcer);
            RegisterSpellProcessor(RelevantNanos.FalseProfEng, FalseProfEngineer);
            RegisterSpellProcessor(RelevantNanos.FalseProfFixer, FalseProfFixer);
            RegisterSpellProcessor(RelevantNanos.FalseProfMa, FalseProfMartialArtist);
            RegisterSpellProcessor(RelevantNanos.FalseProfMp, FalseProfMetaphysicist);
            RegisterSpellProcessor(RelevantNanos.FalseProfNt, FalseProfNanoTechnician);
            RegisterSpellProcessor(RelevantNanos.FalseProfSol, FalseProfSoldier);
            RegisterSpellProcessor(RelevantNanos.FalseProfTrader, FalseProfTrader);

            RegisterSpellProcessor(RelevantNanos.DetauntProcs, DetauntProc);
            RegisterSpellProcessor(RelevantNanos.DotProcs, DamageProc);

            //Team Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(), TeamBuff);
            RegisterSpellProcessor(RelevantNanos.TeamCritBuffs, TeamCrit);

            //Debuffs/DoTs
            RegisterSpellProcessor(RelevantNanos.InitDebuffs, InitDebuffs, CombatActionPriority.Low);
            //RegisterSpellProcessor(RelevantNanos.InitDebuffs, InitDebuffTarget, CombatActionPriority.Low);
            //RegisterSpellProcessor(RelevantNanos.InitDebuffs, OSInitDebuff, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTAgentStrainA).OrderByStackingOrder(), DotStrainA);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EvasionDebuffs_Agent), EvasionDebuff);

            PluginDirectory = pluginDir;

            AgentHealPercentage = Config.CharSettings[Game.ClientInst].AgentHealPercentage;
            AgentCompleteHealPercentage = Config.CharSettings[Game.ClientInst].AgentCompleteHealPercentage;
        }

        public Window[] _windows => new Window[] { _buffWindow, _debuffWindow, _healingWindow, _procWindow };

        public static void AgentHealPercentage_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].AgentHealPercentage = e;
            AgentHealPercentage = e;
            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }

        public static void AgentCompleteHealPercentage_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].AgentCompleteHealPercentage = e;
            AgentCompleteHealPercentage = e;
            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }
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


        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\AgentProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "AgentProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "AgentProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        private void HandleFalseProfViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_falseProfView)) { return; }

                _falseProfView = View.CreateFromXml(PluginDirectory + "\\UI\\AgentFalseProfsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "False Professions", XmlViewName = "AgentFalseProfsView" }, _falseProfView);
            }
            else if (_falseProfWindow == null || (_falseProfWindow != null && !_falseProfWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_falseProfWindow, PluginDir, new WindowOptions() { Name = "False Professions", XmlViewName = "AgentFalseProfsView" }, _falseProfView, out var container);
                _falseProfWindow = container;
            }
        }

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_buffView)) { return; }

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\AgentBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "AgentBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "AgentBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }

        private void HandleDebuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\AgentDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "AgentDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "AgentDebuffsView" }, _debuffView, out var container);
                _debuffWindow = container;
            }
        }

        private void HandleHealingViewClick(object s, ButtonBase button)
        {

            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_healingView)) { return; }

                _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\AgentHealingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Healing", XmlViewName = "AgentHealingView" }, _healingView);

                window.FindView("HealPercentageBox", out TextInputView healInput);
                window.FindView("CompleteHealPercentageBox", out TextInputView completeHealInput);

                if (healInput != null)
                {
                    healInput.Text = $"{AgentHealPercentage}";
                }

                if (completeHealInput != null)
                {
                    completeHealInput.Text = $"{AgentCompleteHealPercentage}";
                }
            }
            else if (_healingWindow == null || (_healingWindow != null && !_healingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_healingWindow, PluginDir, new WindowOptions() { Name = "Healing", XmlViewName = "AgentHealingView" }, _healingView, out var container);
                _healingWindow = container;

                container.FindView("HealPercentageBox", out TextInputView healInput);
                container.FindView("CompleteHealPercentageBox", out TextInputView completeHealInput);

                if (healInput != null)
                {
                    healInput.Text = $"{AgentHealPercentage}";
                }

                if (completeHealInput != null)
                {
                    completeHealInput.Text = $"{AgentCompleteHealPercentage}";
                }
            }
        }


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

            var window = SettingsController.FindValidWindow(_windows);

            if (window != null && window.IsValid)
            {
                window.FindView("HealPercentageBox", out TextInputView healInput);
                window.FindView("CompleteHealPercentageBox", out TextInputView completeHealInput);

                if (healInput != null && !string.IsNullOrEmpty(healInput.Text))
                {
                    if (int.TryParse(healInput.Text, out int healValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].AgentHealPercentage != healValue)
                        {
                            Config.CharSettings[Game.ClientInst].AgentHealPercentage = healValue;
                        }
                    }
                }
                if (completeHealInput != null && !string.IsNullOrEmpty(completeHealInput.Text))
                {
                    if (int.TryParse(completeHealInput.Text, out int completeHealValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].AgentCompleteHealPercentage != completeHealValue)
                        {
                            Config.CharSettings[Game.ClientInst].AgentCompleteHealPercentage = completeHealValue;
                        }
                    }
                }
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                if (SettingsController.settingsWindow.FindView("HealingView", out Button healingView))
                {
                    healingView.Tag = SettingsController.settingsWindow;
                    healingView.Clicked = HandleHealingViewClick;
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

                if (SettingsController.settingsWindow.FindView("ProcView", out Button procView))
                {
                    procView.Tag = SettingsController.settingsWindow;
                    procView.Clicked = HandleProcViewClick;
                }

                if (SettingsController.settingsWindow.FindView("FalseProfsView", out Button falseProfView))
                {
                    falseProfView.Tag = SettingsController.settingsWindow;
                    falseProfView.Clicked = HandleFalseProfViewClick;
                }
            }

            if (CanLookupPetsAfterZone())
            {
                SynchronizePetCombatStateWithOwner();
                AssignTargetToHealPet();
            }

            if (IsSettingEnabled("Damage") && !IsSettingEnabled("Detaunt"))
            {
                CancelBuffs(RelevantNanos.DetauntProcs);
            }
            if (IsSettingEnabled("Detaunt") && !IsSettingEnabled("Damage"))
            {
                CancelBuffs(RelevantNanos.DotProcs);
            }
        }

        #region Perks


        private bool DisableCuffs(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.DisableCuffs != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool GrimReaper(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.GrimReaper != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool IntenseMetabolism(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.IntenseMetabolism != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool MinorNanobotEnhance(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.MinorNanobotEnhance != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool NoEscape(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.NoEscape != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool BrokenAnkle(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.BrokenAnkle != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool CellKiller(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.CellKiller != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool ImprovedFocus(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.ImprovedFocus != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool LaserAim(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.LaserAim != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool NanoEnhancedTargeting(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.NanoEnhancedTargeting != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool NotumChargedRounds(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.NotumChargedRounds != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool PlasteelPiercingRounds(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.PlasteelPiercingRounds != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Healing

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

        private SimpleChar GetTargetToHeal()
        {
            if (DynelManager.LocalPlayer.HealthPercent < 90)
            {
                return DynelManager.LocalPlayer;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent < 90)
                    .OrderByDescending(c => c.HealthPercent)
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    return dyingTeamMember;
                }
            }

            Pet dyingPet = DynelManager.LocalPlayer.Pets
                 .Where(pet => pet.Type == PetType.Attack || pet.Type == PetType.Social)
                 .Where(pet => pet.Character.HealthPercent < 90)
                 .OrderByDescending(pet => pet.Character.HealthPercent)
                 .FirstOrDefault();

            if (dyingPet != null)
            {
                return dyingPet.Character;
            }

            return null;
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (!CanCast(spell) || AgentHealPercentage == 0) { return false; }

            if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                return FindMemberWithHealthBelow(AgentHealPercentage, ref actionTarget);
            }
            else if (HealSelection.SingleOS == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                return FindPlayerWithHealthBelow(AgentHealPercentage, ref actionTarget);
            }

            return false;
        }

        private bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (!IsSettingEnabled("CH") || AgentCompleteHealPercentage == 0) { return false; }

            return FindMemberWithHealthBelow(AgentCompleteHealPercentage, ref actionTarget);
        }

        #endregion

        #region Instanced Logic

        //private bool FalseProfDoc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (!IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal")) { return false; }

        //    return GenericBuff(spell, fightingTarget, ref actionTarget);
        //}

        private bool FalseProfEnforcer(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Enforcer != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfEngineer(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Engineer != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfNanoTechnician(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.NanoTechnician != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfTrader(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Trader != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfBeauracrat(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Beauracrat != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfMartialArtist(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.MartialArtist != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfFixer(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Fixer != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfAdventurer(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Adventurer != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfDoctor(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Doctor != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfSoldier(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Soldier != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool FalseProfMetaphysicist(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Metaphysicist != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool RifleBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.AssassinsAimedShot, out Buff AAS)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.SteadyNerves, out Buff SN)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool TeamCrit(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam)
            {
                if (!IsSettingEnabled("CritTeam")) { return false; }

                return TeamBuff(spell, fightingTarget, ref actionTarget);
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }


        private bool DetauntProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Detaunt")) { return false; }

            if (!IsSettingEnabled("Detaunt") && !IsSettingEnabled("Damage") || (IsSettingEnabled("Detaunt") && IsSettingEnabled("Damage"))) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DamageProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Damage")) { return false; }

            if (!IsSettingEnabled("Detaunt") && !IsSettingEnabled("Damage") || (IsSettingEnabled("Detaunt") && IsSettingEnabled("Damage"))) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool InitDebuffs(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (!CanCast(spell)) { return false; }

            if (InitDebuffSelection.OS == (InitDebuffSelection)_settings["InitDebuffSelection"].AsInt32())
            {
                if (DynelManager.NPCs
                    .Where(c => !debuffOSTargetsToIgnore.Contains(c.Name)
                        && c.FightingTarget != null && !c.Buffs.Contains(301844) && c.IsInLineOfSight
                        && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && SpellChecksOther(spell, spell.Nanoline, c))
                    .Any())
                {
                    actionTarget.Target = DynelManager.Characters
                        .Where(c => !debuffOSTargetsToIgnore.Contains(c.Name)
                            && c.FightingTarget != null && !c.Buffs.Contains(301844) && c.IsInLineOfSight
                            && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                            && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                            && SpellChecksOther(spell, spell.Nanoline, c))
                        .FirstOrDefault();

                    if (actionTarget.Target != null)
                    {
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }

                return false;
            }

            if (InitDebuffSelection.Target == (InitDebuffSelection)_settings["InitDebuffSelection"].AsInt32()
                && fightingTarget != null)
            {
                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return DebuffTarget(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool Concentration(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Concentration") || !CanCast(spell) || fightingTarget == null) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DotStrainA(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("DotStrainA", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool EvasionDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("EvasionDebuff", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Misc

        public enum FalseProfSelection
        {
            None, Metaphysicist, Soldier, Enforcer, Engineer, Doctor, Fixer, Beauracrat, MartialArtist, NanoTechnician, Trader, Adventurer
        }
        public enum HealSelection
        {
            None, SingleTeam, SingleOS
        }
        public enum InitDebuffSelection
        {
            None, Target, OS
        }
        public enum ProcType1Selection
        {
            GrimReaper, DisableCuffs, NoEscape, IntenseMetabolism, MinorNanobotEnhance
        }

        public enum ProcType2Selection
        {
            NotumChargedRounds, LaserAim, NanoEnhancedTargeting, PlasteelPiercingRounds, CellKiller, ImprovedFocus, BrokenAnkle
        }

        private static class RelevantNanos
        {
            public static int[] DetauntProcs = { 226437, 226435, 226433, 226431, 226429, 226427 };
            public static int[] FalseProfDoc = { 117210, 117221, 32033 };
            public static int[] FalseProfEng = { 117213, 117224, 32034 };
            public static int[] FalseProfSol = { 117216, 117227, 32038 };
            public static int[] FalseProfCrat = { 117209, 117220, 32032 };
            public static int[] FalseProfTrader = { 117211, 117222, 32040 };
            public static int[] FalseProfAdv = { 117214, 117225, 32030 };
            public static int[] FalseProfMp = { 117210, 117221, 32033 };
            public static int[] FalseProfFixer = { 117212, 117223, 32039 };
            public static int[] FalseProfEnf = { 117217, 117228, 32041 };
            public static int[] FalseProfMa = { 117215, 117226, 32035 };
            public static int[] FalseProfNt = { 117207, 117218, 32037 };
            public static int[] DotProcs = { 226425, 226423, 226421, 226419, 226417, 226415, 226413, 226410 };
            public static int[] TeamCritBuffs = { 160791, 160789, 160787 };
            public static int AssassinsAimedShot = 275007;
            public static int SteadyNerves = 160795;
            public static int CH = 28650;
            public static int TeamCH = 42409; //Add logic later
            public const int TiredLimbs = 99578;
            public static readonly Spell[] InitDebuffs = Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder().Where(spell => spell.Id != TiredLimbs).ToArray();
            public static int[] HEALS = new[] { 223299, 223297, 223295, 223293, 223291, 223289, 223287, 223285, 223281, 43878, 43881, 43886, 43885,
                43887, 43890, 43884, 43808, 43888, 43889, 43883, 43811, 43809, 43810, 28645, 43816, 43817, 43825, 43815,
                43814, 43821, 43820, 28648, 43812, 43824, 43822, 43819, 43818, 43823, 28677, 43813, 43826, 43838, 43835,
                28672, 43836, 28676, 43827, 43834, 28681, 43837, 43833, 43830, 43828, 28654, 43831, 43829, 43832, 28665 };
        }

        #endregion
    }
}
