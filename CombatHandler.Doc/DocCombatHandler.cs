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
using System.Windows.Input;
using static CombatHandler.Doctor.DocCombatHandler;

namespace CombatHandler.Doctor
{
    class DocCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static int DocHealPercentage;
        private static int DocCompleteHealPercentage;

        private static Window _buffWindow;
        private static Window _debuffWindow;
        private static Window _healingWindow;
        private static Window _procWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _healingView;
        private static View _procView;

        private static bool _asyncToggle = false;

        private static double _ncuUpdateTime;

        public DocCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            Config.CharSettings[Game.ClientInst].DocHealPercentageChangedEvent += DocHealPercentage_Changed;
            Config.CharSettings[Game.ClientInst].DocCompleteHealPercentageChangedEvent += DocCompleteHealPercentage_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("GlobalBuffing", true);
            _settings.AddVariable("GlobalComposites", true);
            //_settings.AddVariable("GlobalDebuffs", true);

            _settings.AddVariable("InitBuffSelection", (int)InitBuffSelection.Self);
            _settings.AddVariable("InitDebuffSelection", (int)InitDebuffSelection.None);
            _settings.AddVariable("HealSelection", (int)HealSelection.None);

            _settings.AddVariable("DOTA", false);
            _settings.AddVariable("DOTB", false);
            _settings.AddVariable("DOTC", false);

            _settings.AddVariable("Nuking", false);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.DangerousCulture);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.MassiveVitaePlan);

            _settings.AddVariable("NanoResistTeam", false);

            _settings.AddVariable("ShortHpSelection", (int)ShortHpSelection.None);
            _settings.AddVariable("ShortHOTSelection", (int)ShortHOTSelection.None);

            _settings.AddVariable("CH", true);

            _settings.AddVariable("LockCH", false);

            RegisterSettingsWindow("Doctor Handler", "DocSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcDoctorDangerousCulture, DangerousCulture, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorAntiseptic, Antiseptic, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorMuscleMemory, MuscleMemory, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorBloodTransfusion, BloodTransfusion, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorRestrictiveBandaging, RestrictiveBandaging, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcDoctorMassiveVitaePlan, MassiveVitaePlan, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorAnatomicBlight, AnatomicBlight, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorHealingCare, HealingCare, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorPathogen, Pathogen, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorAnesthetic, Anesthetic, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorAstringent, Astringent, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorInflammation, Inflammation, CombatActionPriority.Low);

            //Healing
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CompleteHealingLine).OrderByStackingOrder(), CompleteHealing, CombatActionPriority.High);

            RegisterSpellProcessor(RelevantNanos.Heals, Healing, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.TeamHeals, TeamHealing, CombatActionPriority.High);

            RegisterSpellProcessor(RelevantNanos.AlphaAndOmega, LockCH, CombatActionPriority.High);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.HPBuffs, MaxHealth);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), Pistol);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealDeltaBuff).OrderByStackingOrder(), GenericTeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceBuffs).OrderByStackingOrder(), NanoResistance);

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.TeamDeathlessBlessing, TeamDeathlessBlessing);
            RegisterSpellProcessor(RelevantNanos.IndividualShortHOTs, ShortHOT);

            RegisterSpellProcessor(RelevantNanos.ImprovedLC, ImprovedLifeChanneler);
            RegisterSpellProcessor(RelevantNanos.IndividualShortMaxHealths, ShortMaxHealth);

            //Debuffs
            RegisterSpellProcessor(RelevantNanos.InitDebuffs, InitDebuff, CombatActionPriority.Medium);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Nuke).OrderByStackingOrder(), SingleTargetNuke, CombatActionPriority.Low);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineA).OrderByStackingOrder(), DOTADebuffTarget, CombatActionPriority.Medium);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineB).OrderByStackingOrder(), DOTBDebuffTarget, CombatActionPriority.Medium);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTStrainC).OrderByStackingOrder(), DOTCDebuffTarget, CombatActionPriority.Medium);

            PluginDirectory = pluginDir;

            DocHealPercentage = Config.CharSettings[Game.ClientInst].DocHealPercentage;
            DocCompleteHealPercentage = Config.CharSettings[Game.ClientInst].DocCompleteHealPercentage;
        }

        public Window[] _windows => new Window[] { _buffWindow, _debuffWindow, _healingWindow, _procWindow };

        #region Callbacks

        public static void OnRemainingNCUMessage(int sender, IPCMessage msg)
        {
            if (Game.IsZoning)
                return;

            RemainingNCUMessage ncuMessage = (RemainingNCUMessage)msg;
            SettingsController.RemainingNCU[ncuMessage.Character] = ncuMessage.RemainingNCU;
        }
        private void OnGlobalBuffingMessage(int sender, IPCMessage msg)
        {
            GlobalBuffingMessage buffMsg = (GlobalBuffingMessage)msg;

            if (DynelManager.LocalPlayer.Identity.Instance == sender) { return; }

            _settings[$"Buffing"] = buffMsg.Switch;
            _settings[$"GlobalBuffing"] = buffMsg.Switch;
        }
        private void OnGlobalCompositesMessage(int sender, IPCMessage msg)
        {
            GlobalCompositesMessage compMsg = (GlobalCompositesMessage)msg;

            if (DynelManager.LocalPlayer.Identity.Instance == sender) { return; }

            _settings[$"Composites"] = compMsg.Switch;
            _settings[$"GlobalComposites"] = compMsg.Switch;
        }

        //private void OnGlobalDebuffingMessage(int sender, IPCMessage msg)
        //{
        //    GlobalDebuffingMessage debuffMsg = (GlobalDebuffingMessage)msg;

        //    _settings[$"Debuffing"] = debuffMsg.Switch;
        //    _settings[$"Debuffing"] = debuffMsg.Switch;
        //}

        #endregion

        #region Handles
        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_buffView)) { return; }

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\DocBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "DocBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "DocBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }

        private void HandleHealingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Do we need this?
                if (window.Views.Contains(_healingView)) { return; }

                _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\DocHealingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Healing", XmlViewName = "DocHealingView" }, _healingView);

                window.FindView("HealPercentageBox", out TextInputView healInput);
                window.FindView("CompleteHealPercentageBox", out TextInputView completeHealInput);

                if (healInput != null)
                {
                    healInput.Text = $"{DocHealPercentage}";
                }

                if (completeHealInput != null)
                {
                    completeHealInput.Text = $"{DocCompleteHealPercentage}";
                }
            }
            else if (_healingWindow == null || (_healingWindow != null && !_healingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_healingWindow, PluginDir, new WindowOptions() { Name = "Healing", XmlViewName = "DocHealingView" }, _healingView, out var container);
                _healingWindow = container;

                container.FindView("HealPercentageBox", out TextInputView healInput);
                container.FindView("CompleteHealPercentageBox", out TextInputView completeHealInput);

                if (healInput != null)
                {
                    healInput.Text = $"{DocHealPercentage}";
                }

                if (completeHealInput != null)
                {
                    completeHealInput.Text = $"{DocCompleteHealPercentage}";
                }
            }
        }

        private void HandleDebuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_debuffView)) { return; }

                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\DocDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "DocDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "DocDebuffsView" }, _debuffView, out var container);
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

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\DocProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "DocProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "DocProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            if (Game.IsZoning)
                return;

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
                        if (Config.CharSettings[Game.ClientInst].DocHealPercentage != healValue)
                        {
                            Config.CharSettings[Game.ClientInst].DocHealPercentage = healValue;
                        }
                    }
                }
                if (completeHealInput != null && !string.IsNullOrEmpty(completeHealInput.Text))
                {
                    if (int.TryParse(completeHealInput.Text, out int completeHealValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].DocCompleteHealPercentage != completeHealValue)
                        {
                            Config.CharSettings[Game.ClientInst].DocCompleteHealPercentage = completeHealValue;
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

                if (SettingsController.settingsWindow.FindView("ProcsView", out Button procView))
                {
                    procView.Tag = SettingsController.settingsWindow;
                    procView.Clicked = HandleProcViewClick;
                }

                if (SettingsController.settingsWindow.FindView("DebuffsView", out Button debuffView))
                {
                    debuffView.Tag = SettingsController.settingsWindow;
                    debuffView.Clicked = HandleDebuffViewClick;
                }
            }
        }

        #region LE Procs

        private bool Antiseptic(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.Antiseptic != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool BloodTransfusion(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.BloodTransfusion != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DangerousCulture(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.DangerousCulture != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool MuscleMemory(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.MuscleMemory != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool RestrictiveBandaging(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.RestrictiveBandaging != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool AnatomicBlight(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.AnatomicBlight != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool Anesthetic(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Anesthetic != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool Astringent(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Astringent != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool HealingCare(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.HealingCare != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool Inflammation(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Inflammation != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool MassiveVitaePlan(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.MassiveVitaePlan != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool Pathogen(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Pathogen != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }


        #endregion

        #region Healing

        private bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("CH") || DocCompleteHealPercentage == 0) { return false; }

            return FindMemberWithHealthBelow(DocCompleteHealPercentage, spell, ref actionTarget);
        }

        private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DocHealPercentage == 0) { return false; }

            if (HealSelection.Team == (HealSelection)_settings["HealSelection"].AsInt32())
                return FindMemberWithHealthBelow(DocHealPercentage, spell, ref actionTarget);

            if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                if (Spell.List.Any(c => c.Id == 275011)) { return false; }

                if (Team.IsInTeam)
                {
                    List<SimpleChar> dyingTeamMember = DynelManager.Characters
                        .Where(c => Team.Members
                            .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
                                .Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                                && c.HealthPercent <= 85 && c.HealthPercent >= 50)
                        .ToList();

                    if (dyingTeamMember.Count >= 4) 
                    { 
                        return true;
                    }
                }
            }

            return false;
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DocHealPercentage == 0) { return false; }

            if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                if (Team.IsInTeam)
                {
                    List<SimpleChar> dyingTeamMember = DynelManager.Characters
                        .Where(c => Team.Members
                            .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
                                .Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                             && c.HealthPercent <= 85 && c.HealthPercent >= 50)
                        .ToList();

                    if (dyingTeamMember.Count >= 4) { return false; }
                }

                return FindMemberWithHealthBelow(DocHealPercentage, spell, ref actionTarget);
            }

            if (HealSelection.SingleOS == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                return FindPlayerWithHealthBelow(DocHealPercentage, spell, ref actionTarget);
            }

            return false;
        }

        #endregion

        #region Buffs

        private bool MaxHealth(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, NanoLine.DoctorHPBuffs, fightingTarget, ref actionTarget);
        }

        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (InitBuffSelection.Team == (InitBuffSelection)_settings["InitBuffSelection"].AsInt32())
                return TeamBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);

            if (InitBuffSelection.None == (InitBuffSelection)_settings["InitBuffSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool ImprovedLifeChanneler(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DocHealPercentage == 0) { return false; }

            if (HealSelection.ImprovedLifeChanneler == (HealSelection)_settings["HealSelection"].AsInt32())
                return FindMemberWithHealthBelow(DocHealPercentage, spell, ref actionTarget);

            if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                if (Team.IsInTeam)
                {
                    List<SimpleChar> dyingTeamMember = DynelManager.Characters
                        .Where(c => Team.Members
                            .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
                                .Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                             && c.HealthPercent <= 85 && c.HealthPercent >= 50)
                        .ToList();

                    if (dyingTeamMember.Count >= 4) 
                    {
                        return true;
                    }
                }
            }

            return Buff(spell, NanoLine.DoctorShortHPBuffs, fightingTarget, ref actionTarget);
        }

        //Add raido options
        private bool NanoResistance(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("NanoResistTeam")) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        // Template for all
        private bool ShortHOT(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //We use this logic because it has radio options
            //If not we use GenericCombatBuff as the processor condition

            if (ShortHOTSelection.Team == (ShortHOTSelection)_settings["ShortHOTSelection"].AsInt32())
                return CombatTeamBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);

            if (ShortHOTSelection.None == (ShortHOTSelection)_settings["ShortHOTSelection"].AsInt32()) { return false; }

            //We allow here for our own input of NanoLine in ref to Supazooted
            //NanoLine.TraderTeamSkillWranglerBuff
            //Different to the spell.NanoLine
            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool ShortMaxHealth(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ShortHpSelection.Team == (ShortHpSelection)_settings["ShortHpSelection"].AsInt32())
                return CombatTeamBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);

            if (ShortHpSelection.None == (ShortHpSelection)_settings["ShortHpSelection"].AsInt32()) { return false; }

            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool TeamDeathlessBlessing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ShortHOTSelection.TeamDeathlessBlessing != (ShortHOTSelection)_settings["ShortHOTSelection"].AsInt32()) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.IndividualShortHoTs))
                CancelBuffs(RelevantNanos.IndividualShortHoTs);

            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Debuffs

        private bool InitDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (InitDebuffSelection.OS == (InitDebuffSelection)_settings["InitDebuffSelection"].AsInt32())
                return OSDebuff(spell, ref actionTarget);

            if (InitDebuffSelection.Target == (InitDebuffSelection)_settings["InitDebuffSelection"].AsInt32()
                && fightingTarget != null)
            {
                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledTargetDebuff("Nuking", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool DOTADebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledTargetDebuff("DOTA", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool DOTBDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledTargetDebuff("DOTB", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool DOTCDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledTargetDebuff("DOTC", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Misc

        private bool LockCH(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("LockCH"))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }
        public enum InitBuffSelection
        {
            None, Self, Team
        }
        public enum HealSelection
        {
            None, SingleTeam, SingleOS, Team, ImprovedLifeChanneler
        }
        public enum InitDebuffSelection
        {
            None, Target, OS
        }
        public enum ShortHpSelection
        {
            None, Self, Team
        }
        public enum ShortHOTSelection
        {
            None, Self, Team, TeamDeathlessBlessing
        }
        public enum ProcType1Selection
        {
            DangerousCulture, Antiseptic, MuscleMemory, BloodTransfusion, RestrictiveBandaging
        }

        public enum ProcType2Selection
        {
            MassiveVitaePlan, AnatomicBlight, HealingCare, Pathogen, Anesthetic, Astringent, Inflammation
        }

        private static class RelevantNanos
        {
            public const int TeamDeathlessBlessing = 269455;
            public static readonly Spell[] IndividualShortHOTs = Spell.GetSpellsForNanoline(NanoLine.HealOverTime).OrderByStackingOrder()
                .Where(spell => spell.Id != TeamDeathlessBlessing).ToArray();
            public static int[] IndividualShortHoTs = new[] { 43852, 43868, 43870, 43872, 43873, 43871, 42396, 43869, 43867, 43877, 43876, 43875, 43879,
                42399, 43882, 43874, 43880, 42401 };

            public const int ImprovedLC = 275011;
            public static readonly Spell[] IndividualShortMaxHealths = Spell.GetSpellsForNanoline(NanoLine.DoctorShortHPBuffs).OrderByStackingOrder()
                .Where(spell => spell.Id != ImprovedLC).ToArray();

            public const int TiredLimbs = 99578;
            public static readonly Spell[] InitDebuffs = Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder()
                .Where(spell => spell.Id != TiredLimbs).ToArray();

            public static int[] HPBuffs = new[] { 95709, 28662, 95720, 95712, 95710, 95711, 28649, 95713, 28660, 95715, 95714, 95718, 95716, 95717, 95719, 42397 };

            public const int AlphaAndOmega = 42409;
            public static int[] Heals = new[] { 223299, 223297, 223295, 223293, 223291, 223289, 223287, 223285, 223281, 43878, 43881, 43886, 43885,
                43887, 43890, 43884, 43808, 43888, 43889, 43883, 43811, 43809, 43810, 28645, 43816, 43817, 43825, 43815,
                43814, 43821, 43820, 28648, 43812, 43824, 43822, 43819, 43818, 43823, 28677, 43813, 43826, 43838, 43835,
                28672, 43836, 28676, 43827, 43834, 28681, 43837, 43833, 43830, 43828, 28654, 43831, 43829, 43832, 28665 };
            public static int[] TeamHeals = new[] { 273312, 273315, 270349, 43891, 223291, 43892, 43893, 43894, 43895, 43896, 43897, 43898, 43899,
                43900, 43901, 43903, 43902, 42404, 43905, 43904, 42395, 43907, 43908, 43906, 42398, 43910, 43909, 42402,
                43911, 43913, 42405, 43912, 43914, 43915, 27804, 43916, 43917, 42403, 42408 };
        }

        public static void DocHealPercentage_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].DocHealPercentage = e;
            DocHealPercentage = e;
            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }

        public static void DocCompleteHealPercentage_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].DocCompleteHealPercentage = e;
            DocCompleteHealPercentage = e;
            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }

        #endregion
    }
}