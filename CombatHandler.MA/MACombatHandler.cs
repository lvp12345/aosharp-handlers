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

namespace CombatHandler.MartialArtist
{
    public class MACombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static int MAHealPercentage;

        private Window _buffWindow;
        private Window _tauntWindow;
        private Window _healingWindow;
        private Window _procWindow;

        private View _buffView;
        private View _tauntView;
        private View _healingView;
        private View _procView;

        private static double _singleTauntTick;
        private static double _singleTaunt;

        private static double _ncuUpdateTime;

        public MACombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            Config.CharSettings[Game.ClientInst].MAHealPercentageChangedEvent += MAHealPercentage_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.AbsoluteFist);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.SelfReconstruction);

            _settings.AddVariable("HealSelection", (int)HealSelection.None);

            _settings.AddVariable("DamageTypeSelection", (int)DamageTypeSelection.Melee);
            _settings.AddVariable("SingleTauntSelection", (int)SingleTauntSelection.None);
            _settings.AddVariable("InitBuffSelection", (int)InitBuffSelection.Self);
            _settings.AddVariable("EvadesSelection", (int)EvadesSelection.Self);

            _settings.AddVariable("Zazen", false);

            _settings.AddVariable("ShortDamage", false);

            RegisterSettingsWindow("Martial-Artist Handler", "MASettingsView.xml");

            //LE Procs Type 1
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistAbsoluteFist, AbsoluteFist, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistStrengthenKi, StrengthenKi, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistDisruptKi, DisruptKi, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistSmashingFist, SmashingFist, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistStrengthenSpirit, StrengthenSpirit, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistStingingFist, StingingFist, CombatActionPriority.Low);
            //LE Procs Type 2
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistSelfReconstruction, SelfReconstruction, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistDebilitatingStrike, DebilitatingStrike, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistHealingMeditation, HealingMeditation, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistAttackLigaments, AttackLigaments, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistMedicinalRemedy, MedicinalRemedy, CombatActionPriority.Low);

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.ReduceInertia, GenericTeamBuffExclusion);
            RegisterSpellProcessor(RelevantNanos.TeamCritBuffs, GenericTeamBuff);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SingleTargetHealing).OrderByStackingOrder(), Healing, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TeamHealing).OrderByStackingOrder(), TeamHealing, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledDestructionBuff).Where(s => s.StackingOrder >= 19).OrderByStackingOrder(), ControlledDestructionNoShutdown);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledDestructionBuff).Where(s => s.StackingOrder < 19).OrderByStackingOrder(), ControlledDestructionWithShutdown);
            RegisterSpellProcessor(RelevantNanos.FistsOfTheWinterFlame, FistsOfTheWinterFlameNano);
            RegisterSpellProcessor(RelevantNanos.Taunts, SingleTargetTaunt, CombatActionPriority.High);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.LimboMastery, GenericTeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.BrawlBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledRageBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RunspeedBuffs).OrderByStackingOrder(), RunSpeed);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.StrengthBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtsBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).Where(s => s.Id != 28879).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RiposteBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtistZazenStance).OrderByStackingOrder(), ZazenStance);

            RegisterSpellProcessor(RelevantNanos.DamageTypeMelee, DamageTypeMelee);
            RegisterSpellProcessor(RelevantNanos.DamageTypeFire, DamageTypeFire);
            RegisterSpellProcessor(RelevantNanos.DamageTypeEnergy, DamageTypeEnergy);
            RegisterSpellProcessor(RelevantNanos.DamageTypeChemical, DamageTypeChemical);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalIncreaseBuff)
                .Where(c => !RelevantNanos.TeamCritBuffs.Contains(c.Id)).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), GenericBuff);


            //Items
            RegisterItemProcessor(RelevantItems.TheWizdomOfHuzzum, RelevantItems.TheWizdomOfHuzzum, MartialArtsTeamHealAttack);
            RegisterItemProcessor(RelevantItems.TouchOfSaiFung, RelevantItems.TouchOfSaiFung, TouchOfSaiFung);
            RegisterItemProcessor(RelevantItems.Sappo, RelevantItems.Sappo, Sappo);

            PluginDirectory = pluginDir;

            MAHealPercentage = Config.CharSettings[Game.ClientInst].MAHealPercentage;
        }

        public Window[] _windows => new Window[] { _healingWindow, _buffWindow, _tauntWindow, _procWindow };

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
        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\MAProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "MAProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "MAProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //if (window.Views.Contains(_buffView)) { return; }

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\MABuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "MABuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "MABuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }

        private void HandleHealingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //if (window.Views.Contains(_healingView)) { return; }

                _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\MAHealingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Healing", XmlViewName = "MAHealingView" }, _healingView);

                window.FindView("HealPercentageBox", out TextInputView healInput);

                if (healInput != null)
                {
                    healInput.Text = $"{MAHealPercentage}";
                }
            }
            else if (_healingWindow == null || (_healingWindow != null && !_healingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_healingWindow, PluginDir, new WindowOptions() { Name = "Healing", XmlViewName = "MAHealingView" }, _healingView, out var container);
                _healingWindow = container;

                container.FindView("HealPercentageBox", out TextInputView healInput);

                if (healInput != null)
                {
                    healInput.Text = $"{MAHealPercentage}";
                }
            }
        }

        private void HandleTauntViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //if (window.Views.Contains(_tauntView)) { return; }

                _tauntView = View.CreateFromXml(PluginDirectory + "\\UI\\MATauntsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Taunts", XmlViewName = "MATauntsView" }, _tauntView);
            }
            else if (_tauntWindow == null || (_tauntWindow != null && !_tauntWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_tauntWindow, PluginDir, new WindowOptions() { Name = "Taunts", XmlViewName = "MATauntsView" }, _tauntView, out var container);
                _tauntWindow = container;
            }
        }

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            var window = SettingsController.FindValidWindow(_windows);

            if (window != null && window.IsValid)
            {
                window.FindView("HealPercentageBox", out TextInputView healInput);

                if (healInput != null && !string.IsNullOrEmpty(healInput.Text))
                {
                    if (int.TryParse(healInput.Text, out int healValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].MAHealPercentage != healValue)
                        {
                            Config.CharSettings[Game.ClientInst].MAHealPercentage = healValue;
                        }
                    }
                }
            }

            if (Time.NormalTime > _ncuUpdateTime + 0.5f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
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

                if (SettingsController.settingsWindow.FindView("TauntsView", out Button tauntView))
                {
                    tauntView.Tag = SettingsController.settingsWindow;
                    tauntView.Clicked = HandleTauntViewClick;
                }

                if (SettingsController.settingsWindow.FindView("ProcsView", out Button procView))
                {
                    procView.Tag = SettingsController.settingsWindow;
                    procView.Clicked = HandleProcViewClick;
                }
            }
        }

        #region LE Procs

        private bool AbsoluteFist(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.AbsoluteFist != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DisruptKi(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.DisruptKi != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool SmashingFist(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.SmashingFist != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool StingingFist(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.StingingFist != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool StrengthenKi(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.StrengthenKi != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool StrengthenSpirit(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.StrengthenSpirit != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool AttackLigaments(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.AttackLigaments != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DebilitatingStrike(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.DebilitatingStrike != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool HealingMeditation(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.HealingMeditation != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool MedicinalRemedy(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.MedicinalRemedy != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool SelfReconstruction(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.SelfReconstruction != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Healing

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                return FindMemberWithHealthBelow(MAHealPercentage, spell, ref actionTarget);
            }
            else if (HealSelection.SingleOS == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                return FindPlayerWithHealthBelow(MAHealPercentage, spell, ref actionTarget);
            }

            return false;
        }

        private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (HealSelection.SingleTeam != (HealSelection)_settings["HealSelection"].AsInt32()) { return false; }

            return FindMemberWithHealthBelow(MAHealPercentage, spell, ref actionTarget);
        }

        #endregion

        #region Items

        private bool Sappo(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingtarget == null) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.MartialArts)) { return false; }

            return true;
        }

        private bool TouchOfSaiFung(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingtarget == null) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Dimach)) { return false; }

            return true;
        }

        private bool MartialArtsTeamHealAttack(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingtarget == null) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Dimach)) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent <= 85) { return false; }

            return true;
        }

        #endregion

        #region Buffs

        private bool ZazenStance(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell) || !IsSettingEnabled("Zazen")) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool DamageTypeEnergy(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DamageTypeSelection.Energy != (DamageTypeSelection)_settings["DamageTypeSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool DamageTypeFire(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DamageTypeSelection.Fire != (DamageTypeSelection)_settings["DamageTypeSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool DamageTypeMelee(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DamageTypeSelection.Melee != (DamageTypeSelection)_settings["DamageTypeSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool DamageTypeChemical(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DamageTypeSelection.Chemical != (DamageTypeSelection)_settings["DamageTypeSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (SingleTauntSelection.OS == (SingleTauntSelection)_settings["SingleTauntSelection"].AsInt32()
/*                && Time.NormalTime > _singleTaunt + EnfTauntDelaySingle*/)
            {
                SimpleChar mob = DynelManager.NPCs
                    .Where(c => c.IsAttacking && c.FightingTarget != null
                        && c.IsInLineOfSight
                        && !debuffOSTargetsToIgnore.Contains(c.Name)
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && !FightingMe(c)
                        && AttackingTeam(c))
                    .OrderBy(c => c.MaxHealth)
                    //&& (c.FightingTarget.Profession != Profession.Enforcer
                    //        && c.FightingTarget.Profession != Profession.Soldier
                    //        && c.FightingTarget.Profession != Profession.MartialArtist))
                    .FirstOrDefault();

                if (mob != null)
                {
                    //_singleTaunt = Time.NormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = mob;
                    return true;
                }
            }

            if (SingleTauntSelection.Target == (SingleTauntSelection)_settings["SingleTauntSelection"].AsInt32()
/*                && Time.NormalTime > _singleTaunt + EnfTauntDelaySingle*/)
            {
                if (fightingTarget != null)
                {
                    //_singleTaunt = Time.NormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = fightingTarget;
                    return true;
                }
            }

            return false;
        }
        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (InitBuffSelection.Team == (InitBuffSelection)_settings["InitBuffSelection"].AsInt32())
            {
                if (Team.IsInTeam)
                {
                    SimpleChar target = DynelManager.Players
                        .Where(c => c.IsInLineOfSight
                            && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                            && c.Health > 0
                            && c.Profession != Profession.NanoTechnician
                            && SpellChecksOther(spell, spell.Nanoline, c))
                        .FirstOrDefault();

                    if (target != null)
                    {
                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = target;
                        return true;
                    }
                }
            }

            if (InitBuffSelection.None == (InitBuffSelection)_settings["InitBuffSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        protected bool Evades(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || IsInsideInnerSanctum()) { return false; }

            if (EvadesSelection.Team == (EvadesSelection)_settings["EvadesSelection"].AsInt32())
                return GenericTeamBuff(spell, fightingTarget, ref actionTarget);

            if (EvadesSelection.None == (EvadesSelection)_settings["EvadesSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        protected bool RunSpeed(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, NanoLine.MajorEvasionBuffs, fightingTarget, ref actionTarget);
        }

        private bool ControlledDestructionNoShutdown(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || DynelManager.LocalPlayer.NanoPercent < 30) { return false; }

            return CombatBuff(spell, NanoLine.ControlledDestructionBuff, fightingTarget, ref actionTarget);
        }

        private bool ControlledDestructionWithShutdown(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !IsSettingEnabled("ShortDamage")
                || DynelManager.LocalPlayer.NanoPercent < 30 || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 1
                || DynelManager.LocalPlayer.HealthPercent < 100) { return false; }

            return CombatBuff(spell, NanoLine.ControlledDestructionBuff, fightingTarget, ref actionTarget);
        }

        private bool FistsOfTheWinterFlameNano(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || fightingTarget?.HealthPercent <= 50) { return false; }

            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Misc

        protected override bool ShouldUseSpecialAttack(SpecialAttack specialAttack)
        {
            return specialAttack != SpecialAttack.Dimach;
        }

        private static class RelevantNanos
        {
            public const int FistsOfTheWinterFlame = 269470;
            public const int LimboMastery = 28894;
            public const int ReduceInertia = 28903;
            public static int[] TeamCritBuffs = { 160574, 160575, 160576 };
            public static int[] Taunts = { 301936, 100214, 100216, 100215, 100217, 28866 };
            public static int[] DamageTypeMelee = { 270798, 28892 };
            public static int[] DamageTypeFire = { 81827, 81824, 28876 };
            public static int[] DamageTypeEnergy = { 81825, 81823, 81826, 81829 };
            public static int[] DamageTypeChemical = { 81822, 81830 };
        }

        private static class RelevantItems
        {
            public const int TheWizdomOfHuzzum = 303056;
            public const int TouchOfSaiFung = 275018;
            public const int Sappo = 267525;
            public const int TreeOfEnlightenment = 204607;
        }

        public static void MAHealPercentage_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].MAHealPercentage = e;
            MAHealPercentage = e;
            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }

        public enum HealSelection
        {
            None, SingleTeam, SingleOS
        }
        public enum SingleTauntSelection
        {
            None, Target, OS
        }

        public enum ProcType1Selection
        {
            AbsoluteFist, StrengthenKi, DisruptKi, SmashingFist, StrengthenSpirit, StingingFist
        }

        public enum ProcType2Selection
        {
            SelfReconstruction, DebilitatingStrike, HealingMeditation, AttackLigaments, MedicinalRemedy
        }
        public enum DamageTypeSelection
        {
            Melee, Fire, Energy, Chemical
        }
        public enum EvadesSelection
        {
            None, Self, Team
        }
        public enum InitBuffSelection
        {
            None, Self, Team
        }

        #endregion
    }
}
