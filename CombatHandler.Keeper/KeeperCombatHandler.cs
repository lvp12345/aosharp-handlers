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

namespace CombatHandler.Keeper
{
    public class KeeperCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleDebuffing = false;

        private static Window _buffWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;

        private static View _buffView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;

        private static double _ncuUpdateTime;

        public KeeperCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
            //IPCChannel.RegisterCallback((int)IPCOpcode.GlobalDebuffing, OnGlobalDebuffingMessage);

            Config.CharSettings[Game.ClientInst].BioCocoonPercentageChangedEvent += BioCocoonPercentage_Changed;
            Config.CharSettings[Game.ClientInst].StimTargetNameChangedEvent += StimTargetName_Changed;
            Config.CharSettings[Game.ClientInst].StimHealthPercentageChangedEvent += StimHealthPercentage_Changed;
            Config.CharSettings[Game.ClientInst].StimNanoPercentageChangedEvent += StimNanoPercentage_Changed;
            Config.CharSettings[Game.ClientInst].KitHealthPercentageChangedEvent += KitHealthPercentage_Changed;
            Config.CharSettings[Game.ClientInst].KitNanoPercentageChangedEvent += KitNanoPercentage_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("GlobalBuffing", true);
            _settings.AddVariable("GlobalComposites", true);
            //_settings.AddVariable("GlobalDebuffs", true);

            _settings.AddVariable("Kits", true);
            _settings.AddVariable("Stims", true);

            _settings.AddVariable("RecastAntiFear", false);

            //LE Proc
            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.RighteousSmite);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.HonorRestored);

            //Auras
            _settings.AddVariable("AuraSet1Selection", (int)AuraSet1Selection.Heal);
            _settings.AddVariable("AuraSet2Selection", (int)AuraSet2Selection.Damage);
            _settings.AddVariable("AuraSet3Selection", (int)AuraSet3Selection.AAO);
            _settings.AddVariable("AuraSet4Selection", (int)AuraSet4Selection.Sanc);

            RegisterSettingsWindow("Keeper Handler", "KeeperSettingsView.xml");

            RegisterPerkProcessors();


            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcKeeperRighteousSmite, RighteousSmite, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperSymbioticBypass, SymbioticBypass, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperVirtuousReaper, VirtuousReaper, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperIgnoreTheUnrepentant, IgnoreTheUnrepentant, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperPureStrike, PureStrike, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperEschewTheFaithless, EschewTheFaithless, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperRighteousStrike, RighteousStrike, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcKeeperHonorRestored, HonorRestored, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperAmbientPurification, AmbientPurification, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperBenevolentBarrier, BenevolentBarrier, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperSubjugation, Subjugation, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcKeeperFaithfulReconstruction, FaithfulReconstruction, CombatActionPriority.Low);

            //Anti-Fear
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.KeeperFearImmunity).OrderByStackingOrder(), RecastAntiFear);

            //Perks
            RegisterPerkProcessor(PerkHash.BioCocoon, BioCocoon);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Fortify).OrderByStackingOrder().OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine._2HEdgedBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Fury).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.KeeperDeflect_RiposteBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.KeeperEvade_Dodge_DuckBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.KeeperStr_Stam_AgiBuff).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(RelevantNanos.HealAuras, HpAura);
            RegisterSpellProcessor(RelevantNanos.NanoAuras, NpAura);
            RegisterSpellProcessor(RelevantNanos.ReflectAuras, BarrierAura);
            RegisterSpellProcessor(RelevantNanos.AAOAuras, ImminenceAura);
            RegisterSpellProcessor(RelevantNanos.DerootAuras, EnervateAura);
            RegisterSpellProcessor(RelevantNanos.DamageAuras, VengeanceAura);
            RegisterSpellProcessor(RelevantNanos.SancAuras, SanctifierAura);
            RegisterSpellProcessor(RelevantNanos.ReaperAuras, ReaperAura);

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.PunisherOfTheWicked, GenericTeamBuff);

            PluginDirectory = pluginDir;

            BioCocoonPercentage = Config.CharSettings[Game.ClientInst].BioCocoonPercentage;
            StimTargetName = Config.CharSettings[Game.ClientInst].StimTargetName;
            StimHealthPercentage = Config.CharSettings[Game.ClientInst].StimHealthPercentage;
            StimNanoPercentage = Config.CharSettings[Game.ClientInst].StimNanoPercentage;
            KitHealthPercentage = Config.CharSettings[Game.ClientInst].KitHealthPercentage;
            KitNanoPercentage = Config.CharSettings[Game.ClientInst].KitNanoPercentage;
        }
        public Window[] _windows => new Window[] { _buffWindow, _procWindow, _itemWindow, _perkWindow };

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
        private void HandleItemViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_itemView)) { return; }

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\KeeperItemsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "KeeperItemsView" }, _itemView);

                window.FindView("StimTargetBox", out TextInputView stimTargetInput);
                window.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                window.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                window.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                window.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);

                if (stimTargetInput != null)
                    stimTargetInput.Text = $"{StimTargetName}";
                if (stimHealthInput != null)
                    stimHealthInput.Text = $"{StimHealthPercentage}";
                if (stimNanoInput != null)
                    stimNanoInput.Text = $"{StimNanoPercentage}";
                if (kitHealthInput != null)
                    kitHealthInput.Text = $"{KitHealthPercentage}";
                if (kitNanoInput != null)
                    kitNanoInput.Text = $"{KitNanoPercentage}";
            }
            else if (_itemWindow == null || (_itemWindow != null && !_itemWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "KeeperItemsView" }, _itemView, out var container);
                _itemWindow = container;

                container.FindView("StimTargetBox", out TextInputView stimTargetInput);
                container.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                container.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                container.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                container.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);

                if (stimTargetInput != null)
                    stimTargetInput.Text = $"{StimTargetName}";
                if (stimHealthInput != null)
                    stimHealthInput.Text = $"{StimHealthPercentage}";
                if (stimNanoInput != null)
                    stimNanoInput.Text = $"{StimNanoPercentage}";
                if (kitHealthInput != null)
                    kitHealthInput.Text = $"{KitHealthPercentage}";
                if (kitNanoInput != null)
                    kitNanoInput.Text = $"{KitNanoPercentage}";
            }
        }
        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\KeeperProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "KeeperProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "KeeperProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }
        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_perkView)) { return; }

                _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\KeeperPerksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Perks", XmlViewName = "KeeperPerksView" }, _perkView);

                window.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);

                if (bioCocoonInput != null)
                    bioCocoonInput.Text = $"{BioCocoonPercentage}";
            }
            else if (_perkWindow == null || (_perkWindow != null && !_perkWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "KeeperPerksView" }, _perkView, out var container);
                _perkWindow = container;

                container.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);

                if (bioCocoonInput != null)
                    bioCocoonInput.Text = $"{BioCocoonPercentage}";
            }
        }

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_buffView)) { return; }

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\KeeperBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "KeeperBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "KeeperBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            if (Game.IsZoning)
                return;

            base.OnUpdate(deltaTime);

            var window = SettingsController.FindValidWindow(_windows);

            if (window != null && window.IsValid)
            {
                window.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);
                window.FindView("StimTargetBox", out TextInputView stimTargetInput);
                window.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                window.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                window.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                window.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);

                if (bioCocoonInput != null && !string.IsNullOrEmpty(bioCocoonInput.Text))
                    if (int.TryParse(bioCocoonInput.Text, out int bioCocoonValue))
                        if (Config.CharSettings[Game.ClientInst].BioCocoonPercentage != bioCocoonValue)
                            Config.CharSettings[Game.ClientInst].BioCocoonPercentage = bioCocoonValue;

                if (stimTargetInput != null)
                    if (Config.CharSettings[Game.ClientInst].StimTargetName != stimTargetInput.Text)
                        Config.CharSettings[Game.ClientInst].StimTargetName = stimTargetInput.Text;

                if (stimHealthInput != null && !string.IsNullOrEmpty(stimHealthInput.Text))
                    if (int.TryParse(stimHealthInput.Text, out int stimHealthValue))
                        if (Config.CharSettings[Game.ClientInst].StimHealthPercentage != stimHealthValue)
                            Config.CharSettings[Game.ClientInst].StimHealthPercentage = stimHealthValue;

                if (stimNanoInput != null && !string.IsNullOrEmpty(stimNanoInput.Text))
                    if (int.TryParse(stimNanoInput.Text, out int stimNanoValue))
                        if (Config.CharSettings[Game.ClientInst].StimNanoPercentage != stimNanoValue)
                            Config.CharSettings[Game.ClientInst].StimNanoPercentage = stimNanoValue;

                if (kitHealthInput != null && !string.IsNullOrEmpty(kitHealthInput.Text))
                    if (int.TryParse(kitHealthInput.Text, out int kitHealthValue))
                        if (Config.CharSettings[Game.ClientInst].KitHealthPercentage != kitHealthValue)
                            Config.CharSettings[Game.ClientInst].KitHealthPercentage = kitHealthValue;

                if (kitNanoInput != null && !string.IsNullOrEmpty(kitNanoInput.Text))
                    if (int.TryParse(kitNanoInput.Text, out int kitNanoValue))
                        if (Config.CharSettings[Game.ClientInst].KitNanoPercentage != kitNanoValue)
                            Config.CharSettings[Game.ClientInst].KitNanoPercentage = kitNanoValue;
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

                CancelAuras();
            }
        }

        #region LE Procs

        private bool EschewTheFaithless(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.EschewTheFaithless != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool IgnoreTheUnrepentant(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.IgnoreTheUnrepentant != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool PureStrike(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.PureStrike != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool RighteousSmite(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.RighteousSmite != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool RighteousStrike(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.RighteousStrike != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool SymbioticBypass(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.SymbioticBypass != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool VirtuousReaper(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.VirtuousReaper != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool AmbientPurification(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.AmbientPurification != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool BenevolentBarrier(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.BenevolentBarrier != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool FaithfulReconstruction(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.FaithfulReconstruction != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool HonorRestored(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.HonorRestored != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool Subjugation(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Subjugation != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Buffs

        #region Auras

        private bool RecastAntiFear(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("RecastAntiFear")) { return true; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool ReaperAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet4Selection.Reaper != (AuraSet4Selection)_settings["AuraSet4Selection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool SanctifierAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet4Selection.Sanc != (AuraSet4Selection)_settings["AuraSet4Selection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool VengeanceAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet2Selection.Damage != (AuraSet2Selection)_settings["AuraSet2Selection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool EnervateAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet2Selection.DeRoot != (AuraSet2Selection)_settings["AuraSet2Selection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool ImminenceAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet3Selection.AAO != (AuraSet3Selection)_settings["AuraSet3Selection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool BarrierAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet3Selection.Reflect != (AuraSet3Selection)_settings["AuraSet3Selection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool HpAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet1Selection.Heal != (AuraSet1Selection)_settings["AuraSet1Selection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool NpAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AuraSet1Selection.Nano != (AuraSet1Selection)_settings["AuraSet1Selection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #endregion

        #region Misc

        private void CancelAuras()
        {
            if (AuraSet1Selection.Heal != (AuraSet1Selection)_settings["AuraSet1Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.HealAuras);
            }
            if (AuraSet1Selection.Nano != (AuraSet1Selection)_settings["AuraSet1Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.NanoAuras);
            }
            if (AuraSet2Selection.Damage != (AuraSet2Selection)_settings["AuraSet2Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.DamageAuras);
            }
            if (AuraSet2Selection.DeRoot != (AuraSet2Selection)_settings["AuraSet2Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.DerootAuras);
            }
            if (AuraSet3Selection.AAO != (AuraSet3Selection)_settings["AuraSet3Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.AAOAuras);
            }
            if (AuraSet3Selection.Reflect != (AuraSet3Selection)_settings["AuraSet3Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ReflectAuras);
            }
            if (AuraSet4Selection.Sanc != (AuraSet4Selection)_settings["AuraSet4Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.SancAuras);
            }
            if (AuraSet4Selection.Reaper != (AuraSet4Selection)_settings["AuraSet4Selection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ReaperAuras);
            }
        }

        protected override bool ShouldUseSpecialAttack(SpecialAttack specialAttack)
        {
            return specialAttack != SpecialAttack.Dimach;
        }

        private static class RelevantNanos
        {
            public const int CourageOfTheJust = 279380;
            public const int CompositeAttribute = 223372;
            public const int CompositeNano = 223380;
            public const int CompositeUtility = 287046;
            public const int CompositePhysical = 215264;
            public const int CompositeMartialProwess = 302158;
            public const int CompositeMelee = 223360;
            public const int PunisherOfTheWicked = 301602;

            public static int[] HealAuras = new[] { 273362, 223024, 210536, 210528 };
            public static int[] NanoAuras = new[] { 224073, 210597, 210589 };

            public static readonly int[] ReflectAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Absorb_Reflect_AMSBuff)
                .Where(s => s.Name.Contains("Barrier of")).OrderByStackingOrder().Select(s => s.Id).ToArray();
            public static readonly int[] DamageAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Damage_SnareReductionBuff)
                .Where(s => s.Name.Contains("Vengeance")).OrderByStackingOrder().Select(s => s.Id).ToArray();
            public static readonly int[] AAOAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Absorb_Reflect_AMSBuff)
                .Where(s => s.Name.Contains("Imminence of")).OrderByStackingOrder().Select(s => s.Id).ToArray();
            public static readonly int[] DerootAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperAura_Damage_SnareReductionBuff)
                .Where(s => s.Name.Contains("Enervate")).OrderByStackingOrder().Select(s => s.Id).ToArray();
            public static readonly int[] ReaperAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperProcBuff)
                .Where(s => s.Name.Contains("Reaper")).OrderByStackingOrder().Select(s => s.Id).ToArray();
            public static readonly int[] SancAuras = Spell.GetSpellsForNanoline(NanoLine.KeeperProcBuff)
                .Where(s => s.Name.Contains("Sanctifier")).OrderByStackingOrder().Select(s => s.Id).ToArray();
        }
        public enum AuraSet1Selection
        {
            Heal, Nano
        }
        public enum AuraSet2Selection
        {
            Damage, DeRoot
        }
        public enum AuraSet3Selection
        {
            AAO, Reflect
        }
        public enum AuraSet4Selection
        {
            Sanc, Reaper
        }

        public enum ProcType1Selection
        {
            RighteousSmite, SymbioticBypass, VirtuousReaper, IgnoreTheUnrepentant, PureStrike, EschewTheFaithless, RighteousStrike
        }

        public enum ProcType2Selection
        {
            HonorRestored, AmbientPurification, BenevolentBarrier, Subjugation, FaithfulReconstruction
        }

        #endregion
    }
}
