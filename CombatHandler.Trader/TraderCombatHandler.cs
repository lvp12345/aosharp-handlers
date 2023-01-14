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

namespace CombatHandler.Trader
{
    public class TraderCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleDebuffing = false;

        private static Window _buffWindow;
        private static Window _debuffWindow;
        private static Window _healingWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _healingView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;

        private static SimpleChar _drainTarget;

        private static double _drainTick;
        private static double _ncuUpdateTime;

        private static bool _purpleReady = false;

        public TraderCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
            //IPCChannel.RegisterCallback((int)IPCOpcode.GlobalDebuffing, OnGlobalDebuffingMessage);

            Config.CharSettings[Game.ClientInst].HealPercentageChangedEvent += HealPercentage_Changed;
            Config.CharSettings[Game.ClientInst].HealthDrainPercentageChangedEvent += HealthDrainPercentage_Changed;
            Config.CharSettings[Game.ClientInst].StimTargetNameChangedEvent += StimTargetName_Changed;
            Config.CharSettings[Game.ClientInst].StimHealthPercentageChangedEvent += StimHealthPercentage_Changed;
            Config.CharSettings[Game.ClientInst].StimNanoPercentageChangedEvent += StimNanoPercentage_Changed;
            Config.CharSettings[Game.ClientInst].KitHealthPercentageChangedEvent += KitHealthPercentage_Changed;
            Config.CharSettings[Game.ClientInst].KitNanoPercentageChangedEvent += KitNanoPercentage_Changed;
            Config.CharSettings[Game.ClientInst].CycleSpherePerkDelayChangedEvent += CycleSpherePerkDelay_Changed;
            Config.CharSettings[Game.ClientInst].CycleWitOfTheAtroxPerkDelayChangedEvent += CycleWitOfTheAtroxPerkDelay_Changed;
            Config.CharSettings[Game.ClientInst].SelfHealPerkPercentageChangedEvent += SelfHealPerkPercentage_Changed;
            Config.CharSettings[Game.ClientInst].SelfNanoPerkPercentageChangedEvent += SelfNanoPerkPercentage_Changed;
            Config.CharSettings[Game.ClientInst].TeamHealPerkPercentageChangedEvent += TeamHealPerkPercentage_Changed;
            Config.CharSettings[Game.ClientInst].TeamNanoPerkPercentageChangedEvent += TeamNanoPerkPercentage_Changed;
            Config.CharSettings[Game.ClientInst].BodyDevAbsorbsItemPercentageChangedEvent += BodyDevAbsorbsItemPercentage_Changed;
            Config.CharSettings[Game.ClientInst].StrengthAbsorbsItemPercentageChangedEvent += StrengthAbsorbsItemPercentage_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("GlobalBuffing", true);
            _settings.AddVariable("GlobalComposites", true);
            //_settings.AddVariable("GlobalDebuffs", true);

            _settings.AddVariable("SharpObjects", true);
            _settings.AddVariable("Grenades", true);

            _settings.AddVariable("StimTargetSelection", (int)StimTargetSelection.Self);

            _settings.AddVariable("Kits", true);

            _settings.AddVariable("DamageDrain", true);
            _settings.AddVariable("HealthDrain", false);

            _settings.AddVariable("EvadesSelection", (int)EvadesSelection.None);

            //LE Proc
            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.DebtCollection);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.UnopenedLetter);

            _settings.AddVariable("NanoHeal", false);

            _settings.AddVariable("LegShot", false);

            _settings.AddVariable("PerkSelection", (int)PerkSelection.Sacrifice);

            _settings.AddVariable("HealSelection", (int)HealSelection.None);

            _settings.AddVariable("DepriveSelection", (int)DepriveSelection.Target);
            _settings.AddVariable("RansackSelection", (int)RansackSelection.Target);
            _settings.AddVariable("NanoDrainSelection", (int)NanoDrainSelection.None);
            _settings.AddVariable("ACDrainSelection", (int)ACDrainSelection.None);
            _settings.AddVariable("NanoResistSelection", (int)NanoResistSelection.None);
            _settings.AddVariable("DamageDrainSelection", (int)DamageDrainSelection.None);
            _settings.AddVariable("AADDrainSelection", (int)AADDrainSelection.None);
            _settings.AddVariable("AAODrainSelection", (int)AAODrainSelection.None);
            _settings.AddVariable("GrandTheftHumiditySelection", (int)GrandTheftHumiditySelection.Target);
            _settings.AddVariable("MyEnemySelection", (int)MyEnemySelection.Target);

            RegisterSettingsWindow("Trader Handler", "TraderSettingsView.xml");

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcTraderDebtCollection, DebtCollection, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcTraderAccumulatedInterest, AccumulatedInterest, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcTraderExchangeProduct, ExchangeProduct, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcTraderUnforgivenDebts, UnforgivenDebts, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcTraderUnexpectedBonus, UnexpectedBonus, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcTraderRebate, Rebate, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcTraderUnopenedLetter, UnopenedLetter, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcTraderRigidLiquidation, RigidLiquidation, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcTraderDepleteAssets, DepleteAssets, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcTraderEscrow, Escrow, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcTraderRefinanceLoans, RefinanceLoans, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcTraderPaymentPlan, PaymentPlan, CombatActionPriority.Low);

            //Perks
            RegisterPerkProcessor(PerkHash.LegShot, LegShot);
            RegisterPerkProcessor(PerkHash.Sacrifice, Sacrifice);
            RegisterPerkProcessor(PerkHash.PurpleHeart, PurpleHeart);

            //Heals
            RegisterSpellProcessor(RelevantNanos.Heal, Healing);
            RegisterSpellProcessor(RelevantNanos.TeamHeal, Healing);
            RegisterSpellProcessor(RelevantNanos.HealthDrain, HealthDrain);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DrainHeal).OrderByStackingOrder(), LEHeal);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDrain_LineA).OrderByStackingOrder(), RKNanoDrain);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SLNanopointDrain).OrderByStackingOrder(), SLNanoDrain);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.ImprovedQuantumUncertanity, ImprovedQuantumUncertanity);
            RegisterSpellProcessor(RelevantNanos.UnstoppableKiller, GlobalGenericBuff);
            RegisterSpellProcessor(RelevantNanos.UmbralWranglerPremium, GlobalGenericBuff);

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.QuantumUncertanity, Evades);

            //Team Nano heal (Rouse Outfit nanoline)
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoPointHeals).OrderByStackingOrder(), NanoHeal, CombatActionPriority.Medium);

            //Debuffs
            RegisterSpellProcessor(RelevantNanos.GrandThefts, GrandTheftHumidity, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.MyEnemiesEnemyIsMyFriend, MyEnemy);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderAADDrain).OrderByStackingOrder(), AADDrain, CombatActionPriority.Medium);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderAAODrain).OrderByStackingOrder(), AAODrain, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.DivestDamage, DamageDrain, CombatActionPriority.Medium);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceDebuff_LineA).OrderByStackingOrder(), NanoResistA, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DebuffNanoACHeavy).OrderByStackingOrder(), NanoResistB, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Deprive).OrderByStackingOrder(), DepriveDrain, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Ransack).OrderByStackingOrder(), RansackDrain, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderDebuffACNanos).OrderByStackingOrder(), ACDrain, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderACTransferTargetDebuff_Draw).OrderByStackingOrder(), ACDrain, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderACTransferTargetDebuff_Siphon).OrderByStackingOrder(), ACDrain, CombatActionPriority.Low);

            PluginDirectory = pluginDir;

            HealPercentage = Config.CharSettings[Game.ClientInst].HealPercentage;
            HealthDrainPercentage = Config.CharSettings[Game.ClientInst].HealthDrainPercentage;
            StimTargetName = Config.CharSettings[Game.ClientInst].StimTargetName;
            StimHealthPercentage = Config.CharSettings[Game.ClientInst].StimHealthPercentage;
            StimNanoPercentage = Config.CharSettings[Game.ClientInst].StimNanoPercentage;
            KitHealthPercentage = Config.CharSettings[Game.ClientInst].KitHealthPercentage;
            KitNanoPercentage = Config.CharSettings[Game.ClientInst].KitNanoPercentage;
            CycleSpherePerkDelay = Config.CharSettings[Game.ClientInst].CycleSpherePerkDelay;
            CycleWitOfTheAtroxPerkDelay = Config.CharSettings[Game.ClientInst].CycleWitOfTheAtroxPerkDelay;
            SelfHealPerkPercentage = Config.CharSettings[Game.ClientInst].SelfHealPerkPercentage;
            SelfNanoPerkPercentage = Config.CharSettings[Game.ClientInst].SelfNanoPerkPercentage;
            TeamHealPerkPercentage = Config.CharSettings[Game.ClientInst].TeamHealPerkPercentage;
            TeamNanoPerkPercentage = Config.CharSettings[Game.ClientInst].TeamNanoPerkPercentage;
            BodyDevAbsorbsItemPercentage = Config.CharSettings[Game.ClientInst].BodyDevAbsorbsItemPercentage;
            StrengthAbsorbsItemPercentage = Config.CharSettings[Game.ClientInst].StrengthAbsorbsItemPercentage;
        }
        public Window[] _windows => new Window[] { _healingWindow, _buffWindow, _debuffWindow, _procWindow, _itemWindow, _perkWindow };

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

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\TraderBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "TraderBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "TraderBuffsView" }, _buffView, out var container);
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

                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\TraderDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "TraderDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "TraderDebuffsView" }, _debuffView, out var container);
                _debuffWindow = container;
            }
        }

        private void HandleHealingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //Cannot stop Multi-Tabs. Easy fix would be correct naming of views to reference against WindowOptions - options.Name
                _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\TraderHealingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Healing", XmlViewName = "TraderHealingView" }, _healingView);

                window.FindView("HealPercentageBox", out TextInputView healInput);
                window.FindView("HealthDrainPercentageBox", out TextInputView healthDrainInput);

                if (healInput != null)
                    healInput.Text = $"{HealPercentage}";

                if (healthDrainInput != null)
                    healthDrainInput.Text = $"{HealthDrainPercentage}";
            }
            else if (_healingWindow == null || (_healingWindow != null && !_healingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_healingWindow, PluginDir, new WindowOptions() { Name = "Healing", XmlViewName = "TraderHealingView" }, _healingView, out var container);
                _healingWindow = container;

                container.FindView("HealPercentageBox", out TextInputView healInput);
                container.FindView("HealthDrainPercentageBox", out TextInputView healthDrainInput);

                if (healInput != null)
                    healInput.Text = $"{HealPercentage}";

                if (healthDrainInput != null)
                    healthDrainInput.Text = $"{HealthDrainPercentage}";
            }
        }
        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_perkView)) { return; }

                _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\TraderPerksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Perks", XmlViewName = "TraderPerksView" }, _perkView);

                window.FindView("SphereDelayBox", out TextInputView sphereInput);
                window.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                window.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                window.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                window.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                window.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);

                if (sphereInput != null)
                    sphereInput.Text = $"{CycleSpherePerkDelay}";
                if (witOfTheAtroxInput != null)
                    witOfTheAtroxInput.Text = $"{CycleWitOfTheAtroxPerkDelay}";
                if (selfHealInput != null)
                    selfHealInput.Text = $"{SelfHealPerkPercentage}";
                if (selfNanoInput != null)
                    selfNanoInput.Text = $"{SelfNanoPerkPercentage}";
                if (teamHealInput != null)
                    teamHealInput.Text = $"{TeamHealPerkPercentage}";
                if (teamNanoInput != null)
                    teamNanoInput.Text = $"{TeamNanoPerkPercentage}";
            }
            else if (_perkWindow == null || (_perkWindow != null && !_perkWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "TraderPerksView" }, _perkView, out var container);
                _perkWindow = container;

                container.FindView("SphereDelayBox", out TextInputView sphereInput);
                container.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                container.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                container.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                container.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                container.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);

                if (sphereInput != null)
                    sphereInput.Text = $"{CycleSpherePerkDelay}";
                if (witOfTheAtroxInput != null)
                    witOfTheAtroxInput.Text = $"{CycleWitOfTheAtroxPerkDelay}";
                if (selfHealInput != null)
                    selfHealInput.Text = $"{SelfHealPerkPercentage}";
                if (selfNanoInput != null)
                    selfNanoInput.Text = $"{SelfNanoPerkPercentage}";
                if (teamHealInput != null)
                    teamHealInput.Text = $"{TeamHealPerkPercentage}";
                if (teamNanoInput != null)
                    teamNanoInput.Text = $"{TeamNanoPerkPercentage}";
            }
        }
        private void HandleItemViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_itemView)) { return; }

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\TraderItemsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "TraderItemsView" }, _itemView);

                window.FindView("StimTargetBox", out TextInputView stimTargetInput);
                window.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                window.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                window.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                window.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);
                window.FindView("BodyDevAbsorbsItemPercentageBox", out TextInputView bodyDevInput);
                window.FindView("StrengthAbsorbsItemPercentageBox", out TextInputView strengthInput);

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
                if (bodyDevInput != null)
                    bodyDevInput.Text = $"{BodyDevAbsorbsItemPercentage}";
                if (strengthInput != null)
                    strengthInput.Text = $"{StrengthAbsorbsItemPercentage}";
            }
            else if (_itemWindow == null || (_itemWindow != null && !_itemWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "TraderItemsView" }, _itemView, out var container);
                _itemWindow = container;

                container.FindView("StimTargetBox", out TextInputView stimTargetInput);
                container.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                container.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                container.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                container.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);
                container.FindView("BodyDevAbsorbsItemPercentageBox", out TextInputView bodyDevInput);
                container.FindView("StrengthAbsorbsItemPercentageBox", out TextInputView strengthInput);

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
                if (bodyDevInput != null)
                    bodyDevInput.Text = $"{BodyDevAbsorbsItemPercentage}";
                if (strengthInput != null)
                    strengthInput.Text = $"{StrengthAbsorbsItemPercentage}";
            }
        }
        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\TraderProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "TraderProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "TraderProcsView" }, _procView, out var container);
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

            #region Settings

            var window = SettingsController.FindValidWindow(_windows);

            if (window != null && window.IsValid)
            {
                window.FindView("HealPercentageBox", out TextInputView healInput);
                window.FindView("HealthDrainPercentageBox", out TextInputView healthDrainInput);
                window.FindView("StimTargetBox", out TextInputView stimTargetInput);
                window.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                window.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                window.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                window.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);
                window.FindView("SphereDelayBox", out TextInputView sphereInput);
                window.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                window.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                window.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                window.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                window.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);
                window.FindView("BodyDevAbsorbsItemPercentageBox", out TextInputView bodyDevInput);
                window.FindView("StrengthAbsorbsItemPercentageBox", out TextInputView strengthInput);

                if (healInput != null && !string.IsNullOrEmpty(healInput.Text))
                    if (int.TryParse(healInput.Text, out int healValue))
                        if (Config.CharSettings[Game.ClientInst].HealPercentage != healValue)
                            Config.CharSettings[Game.ClientInst].HealPercentage = healValue;

                if (healthDrainInput != null && !string.IsNullOrEmpty(healthDrainInput.Text))
                    if (int.TryParse(healthDrainInput.Text, out int heallthDrainValue))
                        if (Config.CharSettings[Game.ClientInst].HealthDrainPercentage != heallthDrainValue)
                            Config.CharSettings[Game.ClientInst].HealthDrainPercentage = heallthDrainValue;

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

                if (sphereInput != null && !string.IsNullOrEmpty(sphereInput.Text))
                    if (int.TryParse(sphereInput.Text, out int sphereValue))
                        if (Config.CharSettings[Game.ClientInst].CycleSpherePerkDelay != sphereValue)
                            Config.CharSettings[Game.ClientInst].CycleSpherePerkDelay = sphereValue;

                if (witOfTheAtroxInput != null && !string.IsNullOrEmpty(witOfTheAtroxInput.Text))
                    if (int.TryParse(witOfTheAtroxInput.Text, out int witOfTheAtroxValue))
                        if (Config.CharSettings[Game.ClientInst].CycleWitOfTheAtroxPerkDelay != witOfTheAtroxValue)
                            Config.CharSettings[Game.ClientInst].CycleWitOfTheAtroxPerkDelay = witOfTheAtroxValue;

                if (selfHealInput != null && !string.IsNullOrEmpty(selfHealInput.Text))
                    if (int.TryParse(selfHealInput.Text, out int selfHealValue))
                        if (Config.CharSettings[Game.ClientInst].SelfHealPerkPercentage != selfHealValue)
                            Config.CharSettings[Game.ClientInst].SelfHealPerkPercentage = selfHealValue;

                if (selfNanoInput != null && !string.IsNullOrEmpty(selfNanoInput.Text))
                    if (int.TryParse(selfNanoInput.Text, out int selfNanoValue))
                        if (Config.CharSettings[Game.ClientInst].SelfNanoPerkPercentage != selfNanoValue)
                            Config.CharSettings[Game.ClientInst].SelfNanoPerkPercentage = selfNanoValue;

                if (teamHealInput != null && !string.IsNullOrEmpty(teamHealInput.Text))
                    if (int.TryParse(teamHealInput.Text, out int teamHealValue))
                        if (Config.CharSettings[Game.ClientInst].TeamHealPerkPercentage != teamHealValue)
                            Config.CharSettings[Game.ClientInst].TeamHealPerkPercentage = teamHealValue;

                if (teamNanoInput != null && !string.IsNullOrEmpty(teamNanoInput.Text))
                    if (int.TryParse(teamNanoInput.Text, out int teamNanoValue))
                        if (Config.CharSettings[Game.ClientInst].TeamNanoPerkPercentage != teamNanoValue)
                            Config.CharSettings[Game.ClientInst].TeamNanoPerkPercentage = teamNanoValue;

                if (bodyDevInput != null && !string.IsNullOrEmpty(bodyDevInput.Text))
                    if (int.TryParse(bodyDevInput.Text, out int bodyDevValue))
                        if (Config.CharSettings[Game.ClientInst].BodyDevAbsorbsItemPercentage != bodyDevValue)
                            Config.CharSettings[Game.ClientInst].BodyDevAbsorbsItemPercentage = bodyDevValue;

                if (strengthInput != null && !string.IsNullOrEmpty(strengthInput.Text))
                    if (int.TryParse(strengthInput.Text, out int strengthValue))
                        if (Config.CharSettings[Game.ClientInst].StrengthAbsorbsItemPercentage != strengthValue)
                            Config.CharSettings[Game.ClientInst].StrengthAbsorbsItemPercentage = strengthValue;
            }

            #endregion

            if ((RansackSelection.Area == (RansackSelection)_settings["RansackSelection"].AsInt32()
                || DepriveSelection.Area == (DepriveSelection)_settings["DepriveSelection"].AsInt32()
                || DamageDrainSelection.Area == (DamageDrainSelection)_settings["DamageDrainSelection"].AsInt32()
                || ACDrainSelection.Area == (ACDrainSelection)_settings["ACDrainSelection"].AsInt32()
                || AAODrainSelection.Area == (AAODrainSelection)_settings["AAODrainSelection"].AsInt32()
                || AADDrainSelection.Area == (AADDrainSelection)_settings["AADDrainSelection"].AsInt32())
                && Time.NormalTime > _drainTick + 1)
            {
                _drainTarget = DynelManager.NPCs
                    .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name)
                        && c.IsInLineOfSight
                        && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f)
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .FirstOrDefault();

                _drainTick = Time.NormalTime;
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

        private bool AccumulatedInterest(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.AccumulatedInterest != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DebtCollection(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.DebtCollection != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool ExchangeProduct(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.ExchangeProduct != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool Rebate(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.Rebate != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool UnexpectedBonus(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.UnexpectedBonus != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool UnforgivenDebts(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.UnforgivenDebts != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DepleteAssets(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.DepleteAssets != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool Escrow(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Escrow != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool RefinanceLoans(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.RefinanceLoans != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool RigidLiquidation(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.RigidLiquidation != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool UnopenedLetter(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.UnopenedLetter != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool PaymentPlan(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.PaymentPlan != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Perks

        private bool Sacrifice(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PerkSelection.Sacrifice != (PerkSelection)_settings["PerkSelection"].AsInt32()
                || fightingTarget == null) { return false; }

            return Volunteer(perk, fightingTarget, ref actionTarget);
        }

        private bool PurpleHeart(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PerkSelection.PurpleHeart != (PerkSelection)_settings["PerkSelection"].AsInt32()
                || fightingTarget == null) { return false; }

            return Volunteer(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Healing

        private bool LEHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return FindMemberWithHealthBelow(60, spell, ref actionTarget);
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HealPercentage == 0) { return false; }

            if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
                return FindMemberWithHealthBelow(HealPercentage, spell, ref actionTarget);

            if (HealSelection.SingleArea == (HealSelection)_settings["HealSelection"].AsInt32())
                return FindPlayerWithHealthBelow(HealPercentage, spell, ref actionTarget);

            if (HealSelection.Team == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                if (DynelManager.LocalPlayer.IsInTeam())
                {
                    List<SimpleChar> dyingTeamMember = DynelManager.Characters
                        .Where(c => Team.Members
                            .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
                                .Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                        .Where(c => c.HealthPercent <= 85 && c.HealthPercent >= 50)
                        .ToList();

                    if (dyingTeamMember.Count >= 4) { return false; }
                }

                return FindMemberWithHealthBelow(HealPercentage, spell, ref actionTarget);
            }

            return false;
        }

        private bool HealthDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || HealthDrainPercentage == 0 || !IsSettingEnabled("HealthDrain")) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent <= HealthDrainPercentage)
            {
                if (SpellChecksOther(spell, spell.Nanoline, fightingTarget))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = fightingTarget;
                    return true;
                }
            }

            return false;
        }

        private bool NanoHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("NanoHeal")) { return false; }

            if (DynelManager.NPCs.Any(c => c.Health > 0
                && AttackingTeam(c)))
                return Buff(spell, spell.Nanoline, ref actionTarget);

            return false;
        }

        #endregion

        #region Buffs

        //protected bool UmbralWrangle(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (!IsSettingEnabled("UmbralWrangle")) { return false; }

        //    return Buff(spell, NanoLine.TraderTeamSkillWranglerBuff, fightingTarget, ref actionTarget);
        //}

        protected bool ImprovedQuantumUncertanity(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        protected bool Evades(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (EvadesSelection.Team == (EvadesSelection)_settings["EvadesSelection"].AsInt32())
                return GenericTeamBuff(spell, ref actionTarget);

            if (EvadesSelection.Self == (EvadesSelection)_settings["EvadesSelection"].AsInt32())
                return Buff(spell, spell.Nanoline, ref actionTarget);

            return false;
        }

        #endregion

        #region Debuffs

        private bool SLNanoDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (NanoDrainSelection.Shadowlands != (NanoDrainSelection)_settings["NanoDrainSelection"].AsInt32()
                || fightingTarget?.MaxHealth < 1000000) { return false; }

            return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool RKNanoDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (NanoDrainSelection.RubiKa != (NanoDrainSelection)_settings["NanoDrainSelection"].AsInt32()
                || fightingTarget?.MaxHealth < 1000000) { return false; }

            return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool MyEnemy(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MyEnemySelection.Target == (MyEnemySelection)_settings["MyEnemySelection"].AsInt32())
                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);

            if (MyEnemySelection.Boss == (MyEnemySelection)_settings["MyEnemySelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;

        }

        private bool GrandTheftHumidity(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (GrandTheftHumiditySelection.Target == (GrandTheftHumiditySelection)_settings["GrandTheftHumiditySelection"].AsInt32())
                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);

            if (GrandTheftHumiditySelection.Boss == (GrandTheftHumiditySelection)_settings["GrandTheftHumiditySelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool RansackDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (RansackSelection.Target == (RansackSelection)_settings["RansackSelection"].AsInt32())
                return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);

            if (RansackSelection.Boss == (RansackSelection)_settings["RansackSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);
            }

            if (!IsSettingEnabled("Buffing") || !CanCast(spell) || _drainTarget == null) { return false; }

            if (RansackSelection.Area == (RansackSelection)_settings["RansackSelection"].AsInt32())
            {
                if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Ransack, out Buff buff))
                {
                    if (spell.StackingOrder <= buff.StackingOrder)
                    {
                        if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU)) { return false; }

                        if (buff.RemainingTime > 25) { return false; }

                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = _drainTarget;
                        return true;
                    }

                    return false;
                }

                if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU) { return false; }

                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _drainTarget;
                return true;
            }

            return false;
        }

        private bool DepriveDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DepriveSelection.Target == (DepriveSelection)_settings["DepriveSelection"].AsInt32())
                return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);

            if (DepriveSelection.Boss == (DepriveSelection)_settings["DepriveSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);
            }

            if (!IsSettingEnabled("Buffing") || !CanCast(spell) || _drainTarget == null) { return false; }

            if (DepriveSelection.Area == (DepriveSelection)_settings["DepriveSelection"].AsInt32())
            {
                if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Deprive, out Buff buff))
                {
                    if (spell.StackingOrder <= buff.StackingOrder)
                    {
                        if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU)) { return false; }

                        if (buff.RemainingTime > 25) { return false; }

                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = _drainTarget;
                        return true;
                    }

                    return false;
                }

                if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU) { return false; }

                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _drainTarget;
                return true;
            }

            return false;
        }

        private bool DamageDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DamageDrainSelection.Target == (DamageDrainSelection)_settings["DamageDrainSelection"].AsInt32())
                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);

            if (DamageDrainSelection.Boss == (DamageDrainSelection)_settings["DamageDrainSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (!IsSettingEnabled("Buffing") || !CanCast(spell) || _drainTarget == null) { return false; }

            if (DamageDrainSelection.Area == (DamageDrainSelection)_settings["DamageDrainSelection"].AsInt32())
            {
                if (DynelManager.LocalPlayer.Buffs.Find(spell.Nanoline, out Buff buff))
                {
                    if (spell.StackingOrder <= buff.StackingOrder)
                    {
                        if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU)) { return false; }

                        if (buff.RemainingTime > 15) { return false; }

                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = _drainTarget;
                        return true;
                    }

                    return false;
                }

                if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU) { return false; }

                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _drainTarget;
                return true;
            }

            return false;
        }

        private bool AAODrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AAODrainSelection.Target == (AAODrainSelection)_settings["AAODrainSelection"].AsInt32())
                return TargetDebuff(spell, NanoLine.TraderNanoTheft1, fightingTarget, ref actionTarget);

            if (AAODrainSelection.Boss == (AAODrainSelection)_settings["AAODrainSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, NanoLine.TraderNanoTheft1, fightingTarget, ref actionTarget);
            }

            if (!IsSettingEnabled("Buffing") || !CanCast(spell) || _drainTarget == null) { return false; }

            if (AAODrainSelection.Area == (AAODrainSelection)_settings["AAODrainSelection"].AsInt32())
            {
                if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderNanoTheft1, out Buff buff))
                {
                    if (spell.StackingOrder <= buff.StackingOrder)
                    {
                        if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU)) { return false; }

                        if (buff.RemainingTime > 20) { return false; }

                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = _drainTarget;
                        return true;
                    }

                    return false;
                }

                if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU) { return false; }

                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _drainTarget;
                return true;
            }

            return false;
        }

        private bool AADDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (AADDrainSelection.Target == (AADDrainSelection)_settings["AADDrainSelection"].AsInt32())
                return TargetDebuff(spell, NanoLine.TraderNanoTheft2, fightingTarget, ref actionTarget);

            if (AADDrainSelection.Boss == (AADDrainSelection)_settings["AADDrainSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, NanoLine.TraderNanoTheft2, fightingTarget, ref actionTarget);
            }

            if (!IsSettingEnabled("Buffing") || !CanCast(spell) || _drainTarget == null) { return false; }

            if (AADDrainSelection.Area == (AADDrainSelection)_settings["AADDrainSelection"].AsInt32())
            {
                if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderNanoTheft2, out Buff buff))
                {
                    if (spell.StackingOrder <= buff.StackingOrder)
                    {
                        if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU)) { return false; }

                        if (buff.RemainingTime > 20) { return false; }

                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = _drainTarget;
                        return true;
                    }

                    return false;
                }

                if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU) { return false; }

                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _drainTarget;
                return true;
            }

            return false;
        }
        private bool NanoResistA(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (NanoResistSelection.Target == (NanoResistSelection)_settings["NanoResistSelection"].AsInt32())
                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);

            if (NanoResistSelection.Boss == (NanoResistSelection)_settings["NanoResistSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }
        private bool NanoResistB(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (NanoResistSelection.Target == (NanoResistSelection)_settings["NanoResistSelection"].AsInt32())
                if (fightingTarget?.Buffs.Contains(NanoLine.NanoResistanceDebuff_LineA) == true)
                    return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);

            if (NanoResistSelection.Boss == (NanoResistSelection)_settings["NanoResistSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                if (fightingTarget?.Buffs.Contains(NanoLine.NanoResistanceDebuff_LineA) == true)
                    return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }
        private bool ACDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ACDrainSelection.Target == (ACDrainSelection)_settings["ACDrainSelection"].AsInt32())
                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);

            if (ACDrainSelection.Boss == (ACDrainSelection)_settings["ACDrainSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (!IsSettingEnabled("Buffing") || !CanCast(spell) || _drainTarget == null) { return false; }

            if (ACDrainSelection.Area == (ACDrainSelection)_settings["ACDrainSelection"].AsInt32())
            {
                if (DynelManager.LocalPlayer.Buffs.Find(spell.Nanoline, out Buff buff))
                {
                    if (spell.StackingOrder <= buff.StackingOrder)
                    {
                        if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU)) { return false; }

                        if (buff.RemainingTime > 20) { return false; }

                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = _drainTarget;
                        return true;
                    }

                    return false;
                }

                if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU) { return false; }

                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _drainTarget;
                return true;
            }

            return false;
        }

        #endregion

        #region Misc

        private static class RelevantNanos
        {
            public const int QuantumUncertanity = 30745;
            public const int ImprovedQuantumUncertanity = 270808;
            public const int UnstoppableKiller = 275846;
            public const int DivestDamage = 273407;
            public const int UmbralWranglerPremium = 235291;
            public const int MyEnemiesEnemyIsMyFriend = 270714;
            public static int[] GrandThefts = { 269842, 280050 };
            public static int[] HealthDrain = { 270357, 77195, 76478, 76475, 76487, 76481,
                76484, 76491, 76494, 76499, 76571, 76503, 76651, 76614, 76656,
                76653, 76679, 76681, 76684, 76686, 76691, 76688, 76717, 76715,
                76720, 76722, 76724, 76727, 76729, 76732, 76742};
            public static int[] Heal = { 273410, 252155, 121496, 121500, 121501, 121499,
                121502, 121495, 121492, 121506, 121494, 121493, 121504, 121498, 121503,
                76653, 76679, 76681, 76684, 76686, 76691, 76688, 76717, 76715,
                121497, 121505};
            public static int[] TeamHeal = { 118245, 118230, 118232, 118231, 118235, 118233,
                118234, 118238, 118236, 118237, 118241, 118239, 118240, 118243, 118244,
                118242, 43374};
        }

        public enum PerkSelection
        {
            Sacrifice, PurpleHeart
        }
        public enum HealSelection
        {
            None, SingleTeam, SingleArea, Team
        }
        public enum NanoDrainSelection
        {
            None, RubiKa, Shadowlands
        }
        public enum AADDrainSelection
        {
            None, Target, Area, Boss
        }
        public enum AAODrainSelection
        {
            None, Target, Area, Boss
        }
        public enum ACDrainSelection
        {
            None, Target, Area, Boss
        }
        public enum NanoResistSelection
        {
            None, Target, Boss
        }
        public enum DamageDrainSelection
        {
            None, Target, Area, Boss
        }
        public enum RansackSelection
        {
            None, Target, Area, Boss
        }
        public enum DepriveSelection
        {
            None, Target, Area, Boss
        }
        public enum GrandTheftHumiditySelection
        {
            None, Target, Boss
        }
        public enum MyEnemySelection
        {
            None, Target, Boss
        }
        public enum ProcType1Selection
        {
            DebtCollection, AccumulatedInterest, ExchangeProduct, UnforgivenDebts, UnexpectedBonus, Rebate
        }

        public enum EvadesSelection
        {
            None, Self, Team
        }

        public enum ProcType2Selection
        {
            UnopenedLetter, RigidLiquidation, DepleteAssets, Escrow, RefinanceLoans, PaymentPlan
        }

        #endregion
    }
}
