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
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters;

namespace CombatHandler.Adventurer
{
    public class AdvCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleDebuffing = false;

        private static Window _morphWindow;
        private static Window _healingWindow;
        private static Window _buffWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;

        private static View _morphView;
        private static View _healingView;
        private static View _buffView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;

        private static double _ncuUpdateTime;

        public AdvCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
            //IPCChannel.RegisterCallback((int)IPCOpcode.GlobalDebuffing, OnGlobalDebuffingMessage);

            Config.CharSettings[Game.ClientInst].HealPercentageChangedEvent += HealPercentage_Changed;
            Config.CharSettings[Game.ClientInst].CompleteHealPercentageChangedEvent += CompleteHealPercentage_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("GlobalBuffing", true);
            _settings.AddVariable("GlobalComposites", true);
            //_settings.AddVariable("GlobalDebuffs", true);

            _settings.AddVariable("Kits", true);
            _settings.AddVariable("Stims", true);

            _settings.AddVariable("HealSelection", (int)HealSelection.None);
            _settings.AddVariable("MorphSelection", (int)MorphSelection.None);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.AesirAbsorption);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.HealingHerbs);

            _settings.AddVariable("DragonMorph", false);
            _settings.AddVariable("LeetMorph", false);
            _settings.AddVariable("SaberMorph", false);
            _settings.AddVariable("WolfMorph", false);

            _settings.AddVariable("ArmorBuff", false);

            _settings.AddVariable("CH", false);

            RegisterSettingsWindow("Adventurer Handler", "AdvSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcAdventurerAesirAbsorption, AesirAbsorption, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerMacheteFlurry, MacheteFlurry, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerSelfPreservation, SelfPreservation, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerSkinProtection, SkinProtection, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerSoothingHerbs, SoothingHerbs, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerFerociousHits, FerociousHits, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcAdventurerHealingHerbs, HealingHerbs, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerCombustion, Combustion, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerCharringBlow, CharringBlow, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerRestoreVigor, RestoreVigor, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerMacheteSlice, MacheteSlice, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerBasicDressing, BasicDressing, CombatActionPriority.Low);

            //Spells
            RegisterSpellProcessor(RelevantNanos.HEALS, Healing, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CompleteHealingLine).OrderByStackingOrder(), CompleteHealing, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TeamHealing).OrderByStackingOrder(), TeamHealing, CombatActionPriority.High);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.General1HEdgedBuff).OrderByStackingOrder(), Melee);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), Ranged);
            //RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShieldUpgrades).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MultiwieldBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(RelevantNanos.ArmorBuffs, Armor);

            //Morphs
            RegisterSpellProcessor(RelevantNanos.DragonMorph, DragonMorph);
            RegisterSpellProcessor(RelevantNanos.LeetMorph, LeetMorph);
            RegisterSpellProcessor(RelevantNanos.WolfMorph, WolfMorph);
            RegisterSpellProcessor(RelevantNanos.SaberMorph, SaberMorph);

            RegisterSpellProcessor(RelevantNanos.DragonScales, DragonScales);
            RegisterSpellProcessor(RelevantNanos.LeetCrit, LeetCrit);
            RegisterSpellProcessor(RelevantNanos.WolfAgility, WolfAgility);
            RegisterSpellProcessor(RelevantNanos.SaberDamage, SaberDamage);

            PluginDirectory = pluginDir;

            HealPercentage = Config.CharSettings[Game.ClientInst].HealPercentage;
            CompleteHealPercentage = Config.CharSettings[Game.ClientInst].CompleteHealPercentage;

            //Items
            //RegisterItemProcessor(RelevantItems.TheWizdomOfHuzzum, RelevantItems.TheWizdomOfHuzzum, MartialArtsTeamHealAttack);

        }

        public Window[] _windows => new Window[] { _morphWindow, _healingWindow, _procWindow, _buffWindow, _itemWindow, _perkWindow };

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
            GlobalBuffingMessage buffMsg =  (GlobalBuffingMessage)msg;

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

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\AdvBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "AdvBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "AdvBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }
        private void HandleHealingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_healingView)) { return; }

                _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\AdvHealingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Healing", XmlViewName = "AdvHealingView" }, _healingView);

                window.FindView("HealPercentageBox", out TextInputView healInput);
                window.FindView("CompleteHealPercentageBox", out TextInputView completeHealInput);

                if (healInput != null)
                    healInput.Text = $"{HealPercentage}";

                if (completeHealInput != null)
                    completeHealInput.Text = $"{CompleteHealPercentage}";
            }
            else if (_healingWindow == null || (_healingWindow != null && !_healingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_healingWindow, PluginDir, new WindowOptions() { Name = "Healing", XmlViewName = "AdvHealingView" }, _healingView, out var container);
                _healingWindow = container;

                container.FindView("HealPercentageBox", out TextInputView healInput);
                container.FindView("CompleteHealPercentageBox", out TextInputView completeHealInput);

                if (healInput != null)
                    healInput.Text = $"{HealPercentage}";

                if (completeHealInput != null)
                    completeHealInput.Text = $"{CompleteHealPercentage}";
            }
        }
        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_perkView)) { return; }

                _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\AdvPerksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Perks", XmlViewName = "AdvPerksView" }, _perkView);
            }
            else if (_perkWindow == null || (_perkWindow != null && !_perkWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "AdvPerksView" }, _perkView, out var container);
                _perkWindow = container;
            }
        }

        private void HandleItemViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_itemView)) { return; }

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\AdvItemsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "AdvItemsView" }, _itemView);
            }
            else if (_itemWindow == null || (_itemWindow != null && !_itemWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "AdvItemsView" }, _itemView, out var container);
                _itemWindow = container;
            }
        }
        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\AdvProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "AdvProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "AdvProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        private void HandleMorphViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_morphView)) { return; }

                _morphView = View.CreateFromXml(PluginDirectory + "\\UI\\AdvMorphView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "AdvMorphView" }, _morphView);
            }
            else if (_morphWindow == null || (_morphWindow != null && !_morphWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_morphWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "AdvMorphView" }, _morphView, out var container);
                _morphWindow = container;
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
                    if (int.TryParse(healInput.Text, out int healValue))
                        if (Config.CharSettings[Game.ClientInst].HealPercentage != healValue)
                            Config.CharSettings[Game.ClientInst].HealPercentage = healValue;

                if (completeHealInput != null && !string.IsNullOrEmpty(completeHealInput.Text))
                    if (int.TryParse(completeHealInput.Text, out int completeHealValue))
                        if (Config.CharSettings[Game.ClientInst].CompleteHealPercentage != completeHealValue)
                            Config.CharSettings[Game.ClientInst].CompleteHealPercentage = completeHealValue;
            }

            if (MorphSelection.Dragon != (MorphSelection)_settings["MorphSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.DragonMorph);
            }
            if (MorphSelection.Leet != (MorphSelection)_settings["MorphSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.LeetMorph);
            }
            if (MorphSelection.Saber != (MorphSelection)_settings["MorphSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.SaberMorph);
            }
            if (MorphSelection.Wolf != (MorphSelection)_settings["MorphSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.WolfMorph);
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                if (SettingsController.settingsWindow.FindView("ItemsView", out Button itemView))
                {
                    itemView.Tag = SettingsController.settingsWindow;
                    itemView.Clicked = HandleItemViewClick;
                }

                if (SettingsController.settingsWindow.FindView("PerksView", out Button perkView))
                {
                    perkView.Tag = SettingsController.settingsWindow;
                    perkView.Clicked = HandlePerkViewClick;
                }

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

                if (SettingsController.settingsWindow.FindView("MorphView", out Button morphView))
                {
                    morphView.Tag = SettingsController.settingsWindow;
                    morphView.Clicked = HandleMorphViewClick;
                }
                if (SettingsController.settingsWindow.FindView("ProcsView", out Button procView))
                {
                    procView.Tag = SettingsController.settingsWindow;
                    procView.Clicked = HandleProcViewClick;
                }

                #region GlobalBuffing

                if (!_settings["GlobalBuffing"].AsBool() && ToggleBuffing)
                {
                    IPCChannel.Broadcast(new GlobalBuffingMessage()
                    {
                        Switch = false
                    });

                    ToggleBuffing = false;
                    _settings["Buffing"] = false;
                    _settings["GlobalBuffing"] = false;
                }

                if (_settings["GlobalBuffing"].AsBool() && !ToggleBuffing)
                {
                    IPCChannel.Broadcast(new GlobalBuffingMessage()
                    {
                        Switch = true
                    });

                    ToggleBuffing = true;
                    _settings["Buffing"] = true;
                    _settings["GlobalBuffing"] = true;
                }

                #endregion

                #region Global Composites

                if (!_settings["GlobalComposites"].AsBool() && ToggleComposites)
                {
                    IPCChannel.Broadcast(new GlobalCompositesMessage()
                    {
                        Switch = false
                    });

                    ToggleComposites = false;
                    _settings["Composites"] = false;
                    _settings["GlobalComposites"] = false;
                }
                if (_settings["GlobalComposites"].AsBool() && !ToggleComposites)
                {
                    IPCChannel.Broadcast(new GlobalCompositesMessage()
                    {
                        Switch = true
                    });

                    ToggleComposites = true;
                    _settings["Composites"] = true;
                    _settings["GlobalComposites"] = true;
                }

                #endregion

                #region Global Debuffing

                //if (!_settings["GlobalDebuffing"].AsBool() && ToggleDebuffing)
                //{
                //    IPCChannel.Broadcast(new GlobalDebuffingMessage()
                //    {

                //        Switch = false
                //    });

                //    ToggleDebuffing = false;
                //    _settings["GlobalDebuffing"] = false;
                //}
                //if (_settings["GlobalDebuffing"].AsBool() && !ToggleDebuffing)
                //{
                //    IPCChannel.Broadcast(new GlobalDebuffingMessage()
                //    {
                //        Switch = true
                //    });

                //    ToggleDebuffing = true;
                //    _settings["GlobalDebuffing"] = true;
                //}

                #endregion
            }
        }

        #region LE Procs

        private bool AesirAbsorption(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.AesirAbsorption != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool FerociousHits(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.FerociousHits != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool MacheteFlurry(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.MacheteFlurry != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool SelfPreservation(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.SelfPreservation != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool SkinProtection(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.SkinProtection != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool SoothingHerbs(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.SoothingHerbs != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool BasicDressing(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.BasicDressing != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool CharringBlow(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.CharringBlow != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool Combustion(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Combustion != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool HealingHerbs(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.HealingHerbs != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool MacheteSlice(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.MacheteSlice != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool RestoreVigor(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.RestoreVigor != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Healing

        private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell) || HealPercentage == 0) { return false; }

            if (HealSelection.SingleTeam != (HealSelection)_settings["HealSelection"].AsInt32()) { return false; }

            return FindMemberWithHealthBelow(HealPercentage, spell, ref actionTarget);
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell) || HealPercentage == 0) { return false; }

            if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
                return FindMemberWithHealthBelow(HealPercentage, spell, ref actionTarget);
            if (HealSelection.SingleArea == (HealSelection)_settings["HealSelection"].AsInt32())
                return FindPlayerWithHealthBelow(HealPercentage, spell, ref actionTarget);

            return false;
        }

        private bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell) || CompleteHealPercentage == 0) { return false; }

            if (!IsSettingEnabled("CH")) { return false; }

            return FindMemberWithHealthBelow(CompleteHealPercentage, spell, ref actionTarget);
        }

        #endregion

        #region Morphs

        private bool DragonMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Dragon != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool LeetMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Leet != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool WolfMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Wolf != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool SaberMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Saber != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool WolfAgility(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Wolf != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.WolfMorph)) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool SaberDamage(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Saber != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.SaberMorph)) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool LeetCrit(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Leet != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.LeetMorph)) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool DragonScales(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Dragon != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.DragonMorph)) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Buffs

        private bool Armor(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.DragonMorph)) { return false; }

            return GenericTeamBuff(spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Misc

        private static class RelevantNanos
        {
            public static int[] HEALS = new[] { 223167, 252008, 252006, 136674, 136673, 143908, 82059, 136675, 136676, 82060, 136677,
                136678, 136679, 136682, 82061, 136681, 136680, 136683, 136684, 136685, 82062, 136686, 136689, 82063, 136688, 136687,
                82064, 26695 };

            public static readonly int[] ArmorBuffs = { 74173, 74174, 74175 , 74176, 74177, 74178 };
            public static readonly int[] DragonMorph = { 217670, 25994 };
            public static readonly int[] LeetMorph = { 263278, 82834 };
            public static readonly int[] WolfMorph = { 275005, 85062 };
            public static readonly int[] SaberMorph = { 217680, 85070 };
            public static readonly int[] DragonScales = { 302217, 302214 };
            public static readonly int[] WolfAgility = { 302235, 302232 };
            public static readonly int[] LeetCrit = { 302229, 302226 };
            public static readonly int[] SaberDamage = { 302243, 302240 };

        }

        public enum HealSelection
        {
            None, SingleTeam, SingleArea
        }
        public enum MorphSelection
        {
            None, Dragon, Saber, Wolf, Leet
        }

        public enum ProcType1Selection
        {
            AesirAbsorption, MacheteFlurry, SelfPreservation, SkinProtection, FerociousHits
        }

        public enum ProcType2Selection
        {
            HealingHerbs, SoothingHerbs, Combustion, CharringBlow, RestoreVigor, MacheteSlice, BasicDressing
        }

        #endregion
    }
}
