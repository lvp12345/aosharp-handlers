using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Linq;
using Healing = CombatHandler.Generic.Healing;

namespace CombatHandler.Trader
{
    public class TraderCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleRez = false;

        private static Window _buffWindow;
        private static Window _debuffWindow;
        private static Window _healingWindow;
        private static Window _procWindow;
        private static Window _mezzWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;
        private static Window _petWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _healingView;
        private static View _procView;
        private static View _mezzView;
        private static View _itemView;
        private static View _perkView;
        private static View _petView;

        private static SimpleChar _drainTarget;

        private static double _drainTick;
        private static double _ncuUpdateTime;

        public TraderCombatHandler(string pluginDir) : base(pluginDir)
        {
            try
            {
                IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalRez, OnGlobalRezMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.ClearBuffs, OnClearBuffs);
                IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

                Config.CharSettings[DynelManager.LocalPlayer.Name].TargetHealPercentageChangedEvent += TargetHealPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentageChangedEvent += FountainOfLifeHealPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPercentageChangedEvent += TeamHealPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].HealthDrainPercentageChangedEvent += HealthDrainPercentage_Changed;

                Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetNameChangedEvent += StimTargetName_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentageChangedEvent += StimHealthPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentageChangedEvent += StimNanoPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentageChangedEvent += KitHealthPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentageChangedEvent += KitNanoPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelayChangedEvent += CycleSpherePerkDelay_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelayChangedEvent += CycleWitOfTheAtroxPerkDelay_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentageChangedEvent += SelfHealPerkPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentageChangedEvent += SelfNanoPerkPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentageChangedEvent += TeamHealPerkPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentageChangedEvent += TeamNanoPerkPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentageChangedEvent += BodyDevAbsorbsItemPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentageChangedEvent += StrengthAbsorbsItemPercentage_Changed;

                _settings.AddVariable("AllPlayers", false);
                _settings["AllPlayers"] = false;

                _settings.AddVariable("Buffing", true);
                _settings.AddVariable("Composites", true);

                _settings.AddVariable("GlobalBuffing", true);
                _settings.AddVariable("GlobalComposites", true);
                _settings.AddVariable("GlobalRez", true);

                _settings.AddVariable("SharpObjects", true);
                _settings.AddVariable("Grenades", true);

                _settings.AddVariable("TauntTool", false);

                _settings.AddVariable("StimTargetSelection", (int)StimTargetSelection.None);

                _settings.AddVariable("Kits", true);

                _settings.AddVariable("SpawnPets", true);

                _settings.AddVariable("HealthDrain", false);
                _settings.AddVariable("LEDrainHeal", false);

                _settings.AddVariable("Evades", false);
                _settings.AddVariable("UmbralWrangler", false);
                _settings.AddVariable("SLMap", false);

                _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.DebtCollection);
                _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.UnopenedLetter);

                _settings.AddVariable("PetSelection", (int)PetSelection.Boss);

                _settings.AddVariable("LegShot", false);

                _settings.AddVariable("PerkSelection", (int)PerkSelection.None);

                _settings.AddVariable("HealSelection", (int)HealSelection.None);

                _settings.AddVariable("DepriveSelection", (int)DepriveSelection.Target);
                _settings.AddVariable("RansackSelection", (int)RansackSelection.Target);

                _settings.AddVariable("RKNanoDrainSelection", (int)RKNanoDrainSelection.None);
                _settings.AddVariable("SLNanoDrainSelection", (int)SLNanoDrainSelection.None);

                _settings.AddVariable("ACDrainSelection", (int)ACDrainSelection.None);
                _settings.AddVariable("NanoResistSelection", (int)NanoResistSelection.None);

                _settings.AddVariable("MPDamageDebuffLineASelection", (int)MPDamageDebuffLineASelection.None);
                _settings.AddVariable("TraderShutdownSkillDebuffSelection", (int)TraderShutdownSkillDebuffSelection.None);

                _settings.AddVariable("AADDrainSelection", (int)AADDrainSelection.None);
                _settings.AddVariable("AAODrainSelection", (int)AAODrainSelection.None);
                _settings.AddVariable("GrandTheftHumiditySelection", (int)GrandTheftHumiditySelection.Target);
                _settings.AddVariable("MyEnemySelection", (int)MyEnemySelection.Target);
                _settings.AddVariable("NanoHealSelection", (int)NanoHealSelection.Combat);

                _settings.AddVariable("ModeSelection", (int)ModeSelection.None);

                _settings.AddVariable("Root", false);

                RegisterSettingsWindow("Trader Handler", "TraderSettingsView.xml");

                //Pets
                RegisterSpellProcessor(RelevantNanos.DecisionbyCommittee, PetSpawner, CombatActionPriority.High);

                //Root/Snare
                RegisterSpellProcessor(RelevantNanos.FlowofTime, Root, CombatActionPriority.High);

                //Mezz
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Mezz).OrderByStackingOrder(), Mezz, CombatActionPriority.High);

                //Heals

                //health
                RegisterSpellProcessor(RelevantNanos.Heals, Healing.TargetHealing, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantGenericNanos.FountainOfLife, Healing.FountainOfLife, CombatActionPriority.High);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TeamHealing).OrderByStackingOrder(), Healing.TeamHealing, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.HealthDrain, HealthDrain, CombatActionPriority.High);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DrainHeal).OrderByStackingOrder(), LEDrainHeal, CombatActionPriority.High);

                //nano
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDrain_LineA).OrderByStackingOrder(), RKNanoDrain);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SLNanopointDrain).OrderByStackingOrder(), SLNanoDrain);

                //Team Nano heal (Rouse Outfit nanoline)
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoPointHeals).OrderByStackingOrder(), NanoHeal, CombatActionPriority.Medium);

                //Debuffs
                RegisterSpellProcessor(RelevantNanos.GrandThefts, GrandTheftHumidity, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.EnemiesEnemy, MyEnemy);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderAADDrain).OrderByStackingOrder(), AADDrain, CombatActionPriority.Medium);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderAAODrain).OrderByStackingOrder(), AAODrain, CombatActionPriority.Medium);

                RegisterSpellProcessor(RelevantNanos.DivestDamage, MPDamageDebuffLineA, CombatActionPriority.Medium);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderShutdownSkillDebuff).OrderByStackingOrder(), TraderShutdownSkillDebuff, CombatActionPriority.Medium);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceDebuff_LineA).OrderByStackingOrder(), NanoResistA, CombatActionPriority.High);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DebuffNanoACHeavy).OrderByStackingOrder(), NanoResistB, CombatActionPriority.High);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Deprive).OrderByStackingOrder(), DepriveDrain, CombatActionPriority.High);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Ransack).OrderByStackingOrder(), RansackDrain, CombatActionPriority.High);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderDebuffACNanos).OrderByStackingOrder(), ACDrain, CombatActionPriority.Medium);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderACTransferTargetDebuff_Draw).OrderByStackingOrder(), ACDrain, CombatActionPriority.Low);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderACTransferTargetDebuff_Siphon).OrderByStackingOrder(), ACDrain, CombatActionPriority.Low);

                //Buffs
                RegisterSpellProcessor(RelevantNanos.ImprovedQuantumUncertanity, ImprovedQuantumUncertanity);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuff_LineC).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));

                //Team Buffs
                RegisterSpellProcessor(RelevantNanos.QuantumUncertanity, Evades);
                RegisterSpellProcessor(RelevantNanos.UmbralWrangler, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                        => NonCombatBuff(spell, ref actionTarget, fightingTarget, "UmbralWrangler"));

                //Perks
                RegisterPerkProcessor(PerkHash.Sacrifice, Sacrifice);
                RegisterPerkProcessor(PerkHash.PurpleHeart, PurpleHeart);

                //LE Proc
                RegisterPerkProcessor(PerkHash.LEProcTraderDebtCollection, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcTraderAccumulatedInterest, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcTraderExchangeProduct, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcTraderUnforgivenDebts, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcTraderUnexpectedBonus, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcTraderRebate, LEProc1, CombatActionPriority.Low);

                RegisterPerkProcessor(PerkHash.LEProcTraderUnopenedLetter, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcTraderRigidLiquidation, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcTraderDepleteAssets, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcTraderEscrow, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcTraderRefinanceLoans, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcTraderPaymentPlan, LEProc2, CombatActionPriority.Low);

                PluginDirectory = pluginDir;

                Healing.TargetHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].TargetHealPercentage;
                Healing.FountainOfLifeHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentage;
                Healing.TeamHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPercentage;
                HealthDrainPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].HealthDrainPercentage;

                StimTargetName = Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetName;
                StimHealthPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentage;
                StimNanoPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentage;
                KitHealthPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage;
                KitNanoPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage;
                CycleSpherePerkDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelay;
                CycleWitOfTheAtroxPerkDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelay;
                SelfHealPerkPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentage;
                SelfNanoPerkPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentage;
                TeamHealPerkPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentage;
                TeamNanoPerkPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentage;
                BodyDevAbsorbsItemPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentage;
                StrengthAbsorbsItemPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentage;
            }
            catch (Exception ex)
            {
                var errorMessage = "An error occurred on line " + GetLineNumber(ex) + ": " + ex.Message;

                if (errorMessage != previousErrorMessage)
                {
                    Chat.WriteLine(errorMessage);
                    Chat.WriteLine("Stack Trace: " + ex.StackTrace);
                    previousErrorMessage = errorMessage;
                }
            }
        }
        public Window[] _windows => new Window[] { _mezzWindow, _healingWindow, _buffWindow, _debuffWindow, _procWindow, _itemWindow, _perkWindow };

        #region Callbacks

        public static void OnRemainingNCUMessage(int sender, IPCMessage msg)
        {
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
        private void OnGlobalRezMessage(int sender, IPCMessage msg)
        {
            GlobalRezMessage rezMsg = (GlobalRezMessage)msg;

            if (DynelManager.LocalPlayer.Identity.Instance == sender) { return; }

            _settings[$"GlobalRez"] = rezMsg.Switch;
            _settings[$"GlobalRez"] = rezMsg.Switch;

        }

        #endregion

        #region Handles

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
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
        private void HandleMezzViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_mezzView)) { return; }

                _mezzView = View.CreateFromXml(PluginDirectory + "\\UI\\TraderMezzView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Mezz", XmlViewName = "TraderMezzView" }, _mezzView);
            }
            else if (_mezzWindow == null || (_mezzWindow != null && !_mezzWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_mezzWindow, PluginDir, new WindowOptions() { Name = "Mezz", XmlViewName = "TraderMezzView" }, _mezzView, out var container);
                _mezzWindow = container;
            }
        }
        private void HandleDebuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
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
        private void HandlePetViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_petView)) { return; }

                _petView = View.CreateFromXml(PluginDirectory + "\\UI\\TraderPetsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Pets", XmlViewName = "TraderPetsView" }, _petView);
            }
            else if (_petWindow == null || (_petWindow != null && !_petWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_petWindow, PluginDir, new WindowOptions() { Name = "Pets", XmlViewName = "TraderPetsView" }, _petView, out var container);
                _petWindow = container;
            }
        }
        private void HandleHealingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\TraderHealingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Healing", XmlViewName = "TraderHealingView" }, _healingView);

                window.FindView("TargetHealPercentageBox", out TextInputView TargetHealInput);
                window.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);
                window.FindView("TeamHealPercentageBox", out TextInputView TeamHealInput);
                window.FindView("HealthDrainPercentageBox", out TextInputView healthDrainInput);

                if (TargetHealInput != null)
                {
                    TargetHealInput.Text = $"{Healing.TargetHealPercentage}";
                }

                if (FountainOfLifeInput != null)
                {
                    FountainOfLifeInput.Text = $"{Healing.FountainOfLifeHealPercentage}";
                }

                if (TeamHealInput != null)
                {
                    TeamHealInput.Text = $"{Healing.TeamHealPercentage}";
                }

                if (healthDrainInput != null)
                {
                    healthDrainInput.Text = $"{HealthDrainPercentage}";
                }
            }
            else if (_healingWindow == null || (_healingWindow != null && !_healingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_healingWindow, PluginDir, new WindowOptions() { Name = "Healing", XmlViewName = "TraderHealingView" }, _healingView, out var container);
                _healingWindow = container;

                container.FindView("TargetHealPercentageBox", out TextInputView TargetHealInput);
                container.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);
                container.FindView("TeamHealPercentageBox", out TextInputView TeamHealInput);
                container.FindView("HealthDrainPercentageBox", out TextInputView healthDrainInput);

                if (TargetHealInput != null)
                {
                    TargetHealInput.Text = $"{Healing.TargetHealPercentage}";
                }

                if (FountainOfLifeInput != null)
                {
                    FountainOfLifeInput.Text = $"{Healing.FountainOfLifeHealPercentage}";
                }

                if (TeamHealInput != null)
                {
                    TeamHealInput.Text = $"{Healing.TeamHealPercentage}";
                }

                if (healthDrainInput != null)
                {
                    healthDrainInput.Text = $"{HealthDrainPercentage}";
                }
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

                window.FindView("SelfHealPerkPercentageBox", out TextInputView selfHealInput);
                window.FindView("SelfNanoPerkPercentageBox", out TextInputView selfNanoInput);
                window.FindView("TeamHealPerkPercentageBox", out TextInputView teamHealInput);
                window.FindView("TeamNanoPerkPercentageBox", out TextInputView teamNanoInput);

                if (sphereInput != null)
                {
                    sphereInput.Text = $"{CycleSpherePerkDelay}";
                }
                if (witOfTheAtroxInput != null)
                {
                    witOfTheAtroxInput.Text = $"{CycleWitOfTheAtroxPerkDelay}";
                }
                if (selfHealInput != null)
                {
                    selfHealInput.Text = $"{SelfHealPerkPercentage}";
                }
                if (selfNanoInput != null)
                {
                    selfNanoInput.Text = $"{SelfNanoPerkPercentage}";
                }
                if (teamHealInput != null)
                {
                    teamHealInput.Text = $"{TeamHealPerkPercentage}";
                }
                if (teamNanoInput != null)
                {
                    teamNanoInput.Text = $"{TeamNanoPerkPercentage}";
                }
            }
            else if (_perkWindow == null || (_perkWindow != null && !_perkWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "TraderPerksView" }, _perkView, out var container);
                _perkWindow = container;

                container.FindView("SphereDelayBox", out TextInputView sphereInput);
                container.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);

                container.FindView("SelfHealPerkPercentageBox", out TextInputView selfHealInput);
                container.FindView("SelfNanoPerkPercentageBox", out TextInputView selfNanoInput);
                container.FindView("TeamHealPerkPercentageBox", out TextInputView teamHealInput);
                container.FindView("TeamNanoPerkPercentageBox", out TextInputView teamNanoInput);

                if (sphereInput != null)
                {
                    sphereInput.Text = $"{CycleSpherePerkDelay}";
                }
                if (witOfTheAtroxInput != null)
                {
                    witOfTheAtroxInput.Text = $"{CycleWitOfTheAtroxPerkDelay}";
                }
                if (selfHealInput != null)
                {
                    selfHealInput.Text = $"{SelfHealPerkPercentage}";
                }
                if (selfNanoInput != null)
                {
                    selfNanoInput.Text = $"{SelfNanoPerkPercentage}";
                }
                if (teamHealInput != null)
                {
                    teamHealInput.Text = $"{TeamHealPerkPercentage}";
                }
                if (teamNanoInput != null)
                {
                    teamNanoInput.Text = $"{TeamNanoPerkPercentage}";
                }
            }
        }
        private void HandleItemViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
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
                {
                    stimTargetInput.Text = $"{StimTargetName}";
                }
                if (stimHealthInput != null)
                {
                    stimHealthInput.Text = $"{StimHealthPercentage}";
                }
                if (stimNanoInput != null)
                {
                    stimNanoInput.Text = $"{StimNanoPercentage}";
                }
                if (kitHealthInput != null)
                {
                    kitHealthInput.Text = $"{KitHealthPercentage}";
                }
                if (kitNanoInput != null)
                {
                    kitNanoInput.Text = $"{KitNanoPercentage}";
                }
                if (bodyDevInput != null)
                {
                    bodyDevInput.Text = $"{BodyDevAbsorbsItemPercentage}";
                }
                if (strengthInput != null)
                {
                    strengthInput.Text = $"{StrengthAbsorbsItemPercentage}";
                }
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
                {
                    stimTargetInput.Text = $"{StimTargetName}";
                }
                if (stimHealthInput != null)
                {
                    stimHealthInput.Text = $"{StimHealthPercentage}";
                }
                if (stimNanoInput != null)
                {
                    stimNanoInput.Text = $"{StimNanoPercentage}";
                }
                if (kitHealthInput != null)
                {
                    kitHealthInput.Text = $"{KitHealthPercentage}";
                }
                if (kitNanoInput != null)
                {
                    kitNanoInput.Text = $"{KitNanoPercentage}";
                }
                if (bodyDevInput != null)
                {
                    bodyDevInput.Text = $"{BodyDevAbsorbsItemPercentage}";
                }
                if (strengthInput != null)
                {
                    strengthInput.Text = $"{StrengthAbsorbsItemPercentage}";
                }
            }
        }
        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
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
            try
            {
                if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 2.1)
                {
                    return;
                }

                base.OnUpdate(deltaTime);

                if (Time.NormalTime > _ncuUpdateTime + 1.0f)
                {
                    RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                    IPCChannel.Broadcast(ncuMessage);

                    OnRemainingNCUMessage(0, ncuMessage);

                    _ncuUpdateTime = Time.NormalTime;
                }

                SyncPetCombat();

                #region UI

                var window = SettingsController.FindValidWindow(_windows);

                if (window != null && window.IsValid)
                {
                    window.FindView("TargetHealPercentageBox", out TextInputView TargetHealInput);
                    window.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);
                    window.FindView("TeamHealPercentageBox", out TextInputView TeamHealInput);
                    window.FindView("HealthDrainPercentageBox", out TextInputView healthDrainInput);

                    window.FindView("StimTargetBox", out TextInputView stimTargetInput);
                    window.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                    window.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                    window.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                    window.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);
                    window.FindView("SphereDelayBox", out TextInputView sphereInput);
                    window.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);

                    window.FindView("SelfHealPerkPercentageBox", out TextInputView selfHealInput);
                    window.FindView("SelfNanoPerkPercentageBox", out TextInputView selfNanoInput);
                    window.FindView("TeamHealPerkPercentageBox", out TextInputView teamHealInput);
                    window.FindView("TeamNanoPerkPercentageBox", out TextInputView teamNanoInput);

                    window.FindView("BodyDevAbsorbsItemPercentageBox", out TextInputView bodyDevInput);
                    window.FindView("StrengthAbsorbsItemPercentageBox", out TextInputView strengthInput);

                    if (TargetHealInput != null && !string.IsNullOrEmpty(TargetHealInput.Text))
                    {
                        if (int.TryParse(TargetHealInput.Text, out int healValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].TargetHealPercentage != healValue)
                            {

                                Config.CharSettings[DynelManager.LocalPlayer.Name].TargetHealPercentage = healValue;
                            }
                        }
                    }

                    if (FountainOfLifeInput != null && !string.IsNullOrEmpty(FountainOfLifeInput.Text))
                    {
                        if (int.TryParse(FountainOfLifeInput.Text, out int Value))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentage != Value)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentage = Value;
                            }
                        }
                    }

                    if (TeamHealInput != null && !string.IsNullOrEmpty(TeamHealInput.Text))
                    {
                        if (int.TryParse(TeamHealInput.Text, out int Value))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPercentage != Value)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPercentage = Value;
                            }
                        }
                    }

                    if (healthDrainInput != null && !string.IsNullOrEmpty(healthDrainInput.Text))
                    {
                        if (int.TryParse(healthDrainInput.Text, out int heallthDrainValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].HealthDrainPercentage != heallthDrainValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].HealthDrainPercentage = heallthDrainValue;
                        }
                    }

                    if (stimTargetInput != null)
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetName != stimTargetInput.Text)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetName = stimTargetInput.Text;
                        }
                    }

                    if (stimHealthInput != null && !string.IsNullOrEmpty(stimHealthInput.Text))
                    {
                        if (int.TryParse(stimHealthInput.Text, out int stimHealthValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentage != stimHealthValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentage = stimHealthValue;
                            }
                        }
                    }

                    if (stimNanoInput != null && !string.IsNullOrEmpty(stimNanoInput.Text))
                    {
                        if (int.TryParse(stimNanoInput.Text, out int stimNanoValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentage != stimNanoValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentage = stimNanoValue;
                            }
                        }
                    }

                    if (kitHealthInput != null && !string.IsNullOrEmpty(kitHealthInput.Text))
                    {
                        if (int.TryParse(kitHealthInput.Text, out int kitHealthValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage != kitHealthValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage = kitHealthValue;
                            }
                        }
                    }

                    if (kitNanoInput != null && !string.IsNullOrEmpty(kitNanoInput.Text))
                    {
                        if (int.TryParse(kitNanoInput.Text, out int kitNanoValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage != kitNanoValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage = kitNanoValue;
                            }
                        }
                    }

                    if (sphereInput != null && !string.IsNullOrEmpty(sphereInput.Text))
                    {
                        if (int.TryParse(sphereInput.Text, out int sphereValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelay != sphereValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelay = sphereValue;
                            }
                        }
                    }

                    if (witOfTheAtroxInput != null && !string.IsNullOrEmpty(witOfTheAtroxInput.Text))
                    {
                        if (int.TryParse(witOfTheAtroxInput.Text, out int witOfTheAtroxValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelay != witOfTheAtroxValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelay = witOfTheAtroxValue;
                            }
                        }
                    }

                    if (selfHealInput != null && !string.IsNullOrEmpty(selfHealInput.Text))
                    {
                        if (int.TryParse(selfHealInput.Text, out int selfHealValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentage != selfHealValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentage = selfHealValue;
                            }
                        }
                    }

                    if (selfNanoInput != null && !string.IsNullOrEmpty(selfNanoInput.Text))
                    {
                        if (int.TryParse(selfNanoInput.Text, out int selfNanoValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentage != selfNanoValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentage = selfNanoValue;
                            }
                        }
                    }

                    if (teamHealInput != null && !string.IsNullOrEmpty(teamHealInput.Text))
                    {
                        if (int.TryParse(teamHealInput.Text, out int teamHealValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentage != teamHealValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentage = teamHealValue;
                            }
                        }
                    }

                    if (teamNanoInput != null && !string.IsNullOrEmpty(teamNanoInput.Text))
                    {
                        if (int.TryParse(teamNanoInput.Text, out int teamNanoValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentage != teamNanoValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentage = teamNanoValue;
                            }
                        }
                    }

                    if (bodyDevInput != null && !string.IsNullOrEmpty(bodyDevInput.Text))
                    {
                        if (int.TryParse(bodyDevInput.Text, out int bodyDevValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentage != bodyDevValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentage = bodyDevValue;
                            }
                        }
                    }

                    if (strengthInput != null && !string.IsNullOrEmpty(strengthInput.Text))
                    {
                        if (int.TryParse(strengthInput.Text, out int strengthValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentage != strengthValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentage = strengthValue;
                            }
                        }
                    }
                }

                if ((RansackSelection.Area == (RansackSelection)_settings["RansackSelection"].AsInt32()
                    || DepriveSelection.Area == (DepriveSelection)_settings["DepriveSelection"].AsInt32()
                    || MPDamageDebuffLineASelection.Area == (MPDamageDebuffLineASelection)_settings["MPDamageDebuffLineASelection"].AsInt32()
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

                    if (SettingsController.settingsWindow.FindView("PetsView", out Button petView))
                    {
                        petView.Tag = SettingsController.settingsWindow;
                        petView.Clicked = HandlePetViewClick;
                    }

                    if (SettingsController.settingsWindow.FindView("HealingView", out Button healingView))
                    {
                        healingView.Tag = SettingsController.settingsWindow;
                        healingView.Clicked = HandleHealingViewClick;
                    }

                    if (SettingsController.settingsWindow.FindView("MezzView", out Button mezzView))
                    {
                        mezzView.Tag = SettingsController.settingsWindow;
                        mezzView.Clicked = HandleMezzViewClick;
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

                    #endregion

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

                    #region Global Resurrection

                    if (!_settings["GlobalRez"].AsBool() && ToggleRez)
                    {
                        IPCChannel.Broadcast(new GlobalRezMessage()
                        {

                            Switch = false
                        });

                        ToggleRez = false;
                        _settings["GlobalRez"] = false;
                    }
                    if (_settings["GlobalRez"].AsBool() && !ToggleRez)
                    {
                        IPCChannel.Broadcast(new GlobalRezMessage()
                        {
                            Switch = true
                        });

                        ToggleRez = true;
                        _settings["GlobalRez"] = true;
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                var errorMessage = "An error occurred on line " + GetLineNumber(ex) + ": " + ex.Message;

                if (errorMessage != previousErrorMessage)
                {
                    Chat.WriteLine(errorMessage);
                    Chat.WriteLine("Stack Trace: " + ex.StackTrace);
                    previousErrorMessage = errorMessage;
                }
            }
        }

        #region Perks

        private bool Sacrifice(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PerkSelection.Sacrifice != (PerkSelection)_settings["PerkSelection"].AsInt32()
                || PerkSelection.None == (PerkSelection)_settings["PerkSelection"].AsInt32()
                || fightingTarget == null) { return false; }

            return Volunteer(perk, fightingTarget, ref actionTarget);
        }

        private bool PurpleHeart(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PerkSelection.PurpleHeart != (PerkSelection)_settings["PerkSelection"].AsInt32()
                || PerkSelection.None == (PerkSelection)_settings["PerkSelection"].AsInt32()
                || fightingTarget == null) { return false; }

            return Volunteer(perk, fightingTarget, ref actionTarget);
        }

        protected bool Volunteer(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!perk.IsAvailable) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Where(c => c.Name.ToLower().Contains(perk.Name.ToLower())).Any()) { return false; }

            return PerkCondtionProcessors.VolunteerPerk(perk, ref actionTarget);
        }

        #endregion

        #region Healing

        private bool LEDrainHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.FightingTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent <= 40) { return true; }

            return false;
        }

        private bool HealthDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }
            if (HealthDrainPercentage == 0) { return false; }

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

        #endregion

        #region Buffs

        private bool ImprovedQuantumUncertanity(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        #endregion

        #region Team Buffs

        private bool NanoHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (NanoHealSelection.Combat == (NanoHealSelection)_settings["NanoHealSelection"].AsInt32())
            {
                if (InCombat())
                {
                    return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
                }
            }

            return false;
        }

        private bool Evades(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (Team.IsInTeam && IsSettingEnabled("Evades"))
            {
                return NonComabtTeamBuff(spell, fightingTarget, ref actionTarget);
            }
            return false;
        }

        #endregion

        #region Debuffs

        private bool SLNanoDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            SLNanoDrainSelection selection = (SLNanoDrainSelection)_settings["SLNanoDrainSelection"].AsInt32();

            if (NeedsReload()) { return false; }

            switch (selection)
            {
                case SLNanoDrainSelection.Target:
                case SLNanoDrainSelection.Boss:
                    if (selection == SLNanoDrainSelection.Boss && (fightingTarget == null || fightingTarget.MaxHealth < 1000000))
                    {

                        return false;
                    }

                    return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);

                default:
                    return false;
            }
        }

        private bool RKNanoDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            RKNanoDrainSelection selection = (RKNanoDrainSelection)_settings["RKNanoDrainSelection"].AsInt32();

            if (NeedsReload()) { return false; }

            switch (selection)
            {
                case RKNanoDrainSelection.Target:
                case RKNanoDrainSelection.Boss:
                    if (selection == RKNanoDrainSelection.Boss && (fightingTarget == null || fightingTarget.MaxHealth < 1000000))
                    {
                        return false;
                    }

                    return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);

                default:
                    return false;
            }
        }

        private bool MyEnemy(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (NeedsReload()) { return false; }

            if (MyEnemySelection.Target == (MyEnemySelection)_settings["MyEnemySelection"].AsInt32())
            {
                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (MyEnemySelection.Boss == (MyEnemySelection)_settings["MyEnemySelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;

        }

        private bool GrandTheftHumidity(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            string[] namesToCheck = new string[] { "Vergil Aeneid", "Abmouth Supremus", "Aztur the Immortal" };

            if (NeedsReload()) { return false; }

            if (namesToCheck.Any(name => fightingTarget?.Name.Contains(name) ?? false))
            {
                return false;
            }

            if (GrandTheftHumiditySelection.Target == (GrandTheftHumiditySelection)_settings["GrandTheftHumiditySelection"].AsInt32())
            {
                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (GrandTheftHumiditySelection.Boss == (GrandTheftHumiditySelection)_settings["GrandTheftHumiditySelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool RansackDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            RansackSelection ransackSelection = (RansackSelection)_settings["RansackSelection"].AsInt32();

            if (NeedsReload()) { return false; }

            switch (ransackSelection)
            {
                case RansackSelection.Target:
                    return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);

                case RansackSelection.Boss:
                    if (fightingTarget?.MaxHealth < 1000000)
                    {
                        if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Ransack, out Buff bossDebuff) && bossDebuff.RemainingTime > 25)
                        {
                            return false;
                        }

                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = fightingTarget;
                        return true;
                    }
                    else
                    {
                        return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);
                    }

                case RansackSelection.Area:
                    if (!IsSettingEnabled("Buffing") || !CanCast(spell) || _drainTarget == null)
                    {
                        return false;
                    }

                    if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Ransack, out Buff buff))
                    {
                        if (spell.StackingOrder <= buff.StackingOrder)
                        {
                            if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU))
                            {
                                return false;
                            }

                            if (buff.RemainingTime > 25)
                            {
                                return false;
                            }

                            actionTarget.ShouldSetTarget = true;
                            actionTarget.Target = _drainTarget;
                            return true;
                        }

                        return false;
                    }

                    if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU)
                    {
                        return false;
                    }

                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = _drainTarget;
                    return true;

                default:
                    return false;
            }
        }

        private bool DepriveDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            DepriveSelection depriveSelection = (DepriveSelection)_settings["DepriveSelection"].AsInt32();

            if (NeedsReload()) { return false; }

            switch (depriveSelection)
            {
                case DepriveSelection.Target:
                    return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);

                case DepriveSelection.Boss:
                    if (fightingTarget?.MaxHealth < 1000000)
                    {
                        if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Deprive, out Buff bossDebuff) && bossDebuff.RemainingTime > 25)
                        {
                            return false;
                        }

                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = fightingTarget;
                        return true;
                    }
                    else
                    {
                        return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);
                    }

                case DepriveSelection.Area:
                    if (!IsSettingEnabled("Buffing") || !CanCast(spell) || _drainTarget == null)
                    {
                        return false;
                    }

                    if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Deprive, out Buff buff))
                    {
                        if (spell.StackingOrder <= buff.StackingOrder)
                        {
                            if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU))
                            {
                                return false;
                            }

                            if (buff.RemainingTime > 25)
                            {
                                return false;
                            }

                            actionTarget.ShouldSetTarget = true;
                            actionTarget.Target = _drainTarget;
                            return true;
                        }

                        return false;
                    }

                    if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU)
                    {
                        return false;
                    }

                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = _drainTarget;
                    return true;

                default:
                    return false;
            }
        }


        private bool MPDamageDebuffLineA(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            MPDamageDebuffLineASelection selection = (MPDamageDebuffLineASelection)_settings["MPDamageDebuffLineASelection"].AsInt32();

            if (NeedsReload()) { return false; }

            switch (selection)
            {
                case MPDamageDebuffLineASelection.Target:
                //case MPDamageDebuffLineASelection.Boss:
                //    if (selection == MPDamageDebuffLineASelection.Boss && (fightingTarget == null || fightingTarget.MaxHealth < 1000000))
                //        return false;

                //    return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);

                case MPDamageDebuffLineASelection.Boss:
                    if (fightingTarget?.MaxHealth < 1000000)
                    {
                        if (DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.DivestDamageTransfer, out Buff bossDebuff) && bossDebuff.RemainingTime > 25)
                        {
                            return false;
                        }

                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = fightingTarget;
                        return true;
                    }
                    else
                    {
                        return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
                    }

                case MPDamageDebuffLineASelection.Area:
                    if (!IsSettingEnabled("Buffing") || !CanCast(spell) || _drainTarget == null)
                    {
                        return false;
                    }

                    if (DynelManager.LocalPlayer.Buffs.Find(spell.Nanoline, out Buff buff))
                    {
                        if (spell.StackingOrder <= buff.StackingOrder)
                        {
                            if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU) || buff.RemainingTime > 15)
                            {
                                return false;
                            }

                            actionTarget.ShouldSetTarget = true;
                            actionTarget.Target = _drainTarget;
                            return true;
                        }

                        return false;
                    }

                    if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU)
                    {
                        return false;
                    }

                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = _drainTarget;
                    return true;

                default:
                    return false;
            }
        }

        private bool TraderShutdownSkillDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            TraderShutdownSkillDebuffSelection selection = (TraderShutdownSkillDebuffSelection)_settings["TraderShutdownSkillDebuffSelection"].AsInt32();

            if (NeedsReload()) { return false; }

            switch (selection)
            {
                case TraderShutdownSkillDebuffSelection.Target:
                case TraderShutdownSkillDebuffSelection.Boss:
                    if (selection == TraderShutdownSkillDebuffSelection.Boss && (fightingTarget == null || fightingTarget.MaxHealth < 1000000))
                    {
                        return false;
                    }

                    return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);

                case TraderShutdownSkillDebuffSelection.Area:
                    if (!IsSettingEnabled("Buffing") || !CanCast(spell) || _drainTarget == null)
                    {
                        return false;
                    }

                    if (DynelManager.LocalPlayer.Buffs.Find(spell.Nanoline, out Buff buff))
                    {
                        if (spell.StackingOrder <= buff.StackingOrder)
                        {
                            if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU) || buff.RemainingTime > 15)
                            {
                                return false;
                            }

                            actionTarget.ShouldSetTarget = true;
                            actionTarget.Target = _drainTarget;
                            return true;
                        }

                        return false;
                    }

                    if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU)
                    {
                        return false;
                    }

                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = _drainTarget;
                    return true;

                default:
                    return false;
            }
        }

        private bool AAODrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (NeedsReload()) { return false; }

            if (AAODrainSelection.Target == (AAODrainSelection)_settings["AAODrainSelection"].AsInt32())
            {
                return TargetDebuff(spell, NanoLine.TraderNanoTheft1, fightingTarget, ref actionTarget);
            }

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
            if (NeedsReload()) { return false; }

            if (AADDrainSelection.Target == (AADDrainSelection)_settings["AADDrainSelection"].AsInt32())
            {
                return TargetDebuff(spell, NanoLine.TraderNanoTheft2, fightingTarget, ref actionTarget);
            }

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
            if (NeedsReload()) { return false; }

            if (NanoResistSelection.Target == (NanoResistSelection)_settings["NanoResistSelection"].AsInt32())
            {
                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }


            if (NanoResistSelection.Boss == (NanoResistSelection)_settings["NanoResistSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }
        private bool NanoResistB(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (NeedsReload()) { return false; }

            if (NanoResistSelection.Target == (NanoResistSelection)_settings["NanoResistSelection"].AsInt32())
            {
                if (fightingTarget?.Buffs.Contains(NanoLine.NanoResistanceDebuff_LineA) == true)
                {
                    return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
                }
            }

            if (NanoResistSelection.Boss == (NanoResistSelection)_settings["NanoResistSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                if (fightingTarget?.Buffs.Contains(NanoLine.NanoResistanceDebuff_LineA) == true)
                {
                    return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
                }
            }

            return false;
        }
        private bool ACDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (NeedsReload()) { return false; }

            if (ACDrainSelection.Target == (ACDrainSelection)_settings["ACDrainSelection"].AsInt32())
            {
                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

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

        #region Mezz

        private bool Mezz(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (ModeSelection.None == (ModeSelection)_settings["ModeSelection"].AsInt32()) { return false; }

            if (ModeSelection.All == (ModeSelection)_settings["ModeSelection"].AsInt32())
            {
                SimpleChar target = DynelManager.NPCs
                    .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name)
                        && c.Health > 0
                        && c.IsInLineOfSight
                        && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                        && InNanoRange(c)
                        && c.MaxHealth < 1000000)
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .ThenBy(c => c.Health)
                    .FirstOrDefault();

                if (target != null)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = target;
                    return true;
                }
            }

            if (ModeSelection.Adds == (ModeSelection)_settings["ModeSelection"].AsInt32())
            {
                SimpleChar target = DynelManager.NPCs
                    .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name)
                        && c.Health > 0
                        && c.IsInLineOfSight
                        && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                        && InNanoRange(c)
                        && c.MaxHealth < 1000000
                        && c.FightingTarget != null
                        && !AttackingMob(c)
                        && AttackingTeam(c))
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .ThenBy(c => c.Health)
                    .FirstOrDefault();

                if (target != null)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = target;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Roots
        private bool Root(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !IsSettingEnabled("Root") || !CanCast(spell)) { return false; }

            SimpleChar target = DynelManager.Characters
                    .Where(c => c.IsInLineOfSight
                        && IsMoving(c)
                        && !c.Buffs.Contains(NanoLine.Root)
                        && (c.Name == "Flaming Vengeance"
                            || c.Name == "Hand of the Colonel"
                            || c.Name == "Alien Seeker"))
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
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

        #region Pets

        private bool PetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if ((PetSelection)_settings["PetSelection"].AsInt32() == PetSelection.None) { return false; }
            if (!CanCast(spell)) { return false; }
            if (fightingTarget == null) { return false; }

            if ((PetSelection)_settings["PetSelection"].AsInt32() == PetSelection.Target)
            {
                return NoShellPetSpawner(PetType.Attack, spell, fightingTarget, ref actionTarget);
            }

            if ((PetSelection)_settings["PetSelection"].AsInt32() == PetSelection.Boss)
            {
                if (fightingTarget.MaxHealth < 1000000) { return false; }

                return NoShellPetSpawner(PetType.Attack, spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        protected void SyncPetCombat()
        {
            foreach (Pet _pet in DynelManager.LocalPlayer.Pets)
            {
                SyncPetCombat(_pet);
            }
        }

        private void SyncPetCombat(Pet pet)
        {
            var target = DynelManager.LocalPlayer.FightingTarget;

            if (!DynelManager.LocalPlayer.IsAttacking && pet?.Character.IsAttacking == true)
            {
                pet?.Follow();
            }

            if (target != null)
            {
                if (pet?.Character.IsAttacking == false)
                {
                    pet?.Attack(target.Identity);
                }
                else
                {
                    if (pet?.Character.FightingTarget.Identity != target.Identity)
                    {
                        pet?.Attack(target.Identity);
                    }
                }
            }
        }

        #endregion

        #region Misc
        private static bool IsMoving(SimpleChar target)
        {
            if (Playfield.Identity.Instance == 4021)
            {
                return true;
            }

            return target.IsMoving;
        }

        private static class RelevantNanos
        {
            public const int QuantumUncertanity = 30745;
            public const int ImprovedQuantumUncertanity = 270808;
            public const int UnstoppableKiller = 275846;
            public const int DivestDamage = 273407;
            public static int[] EnemiesEnemy = { 270714, 263293 };
            public const int FlowofTime = 30719;
            public const int DecisionbyCommittee = 258659;
            public const int DivestDamageTransfer = 273408;
            public static int[] UmbralWrangler = { 235291, 235289, 235287, 235283, 235281, 235279, 235277, 235275, 235273, 235271 };
            public static int[] GrandThefts = { 269842, 280050 };
            public static int[] HealthDrain = { 270357, 77195, 76478, 76475, 76487, 76481,
                76484, 76491, 76494, 76499, 76571, 76503, 76651, 76614, 76656,
                76653, 76679, 76681, 76684, 76686, 76691, 76688, 76717, 76715,
                76720, 76722, 76724, 76727, 76729, 76732, 76742};
            public static int[] Heals = { 273410, 252155, 121496, 121500, 121501, 121499,
                121502, 121495, 121492, 121506, 121494, 121493, 121504, 121498, 121503,
                76653, 76679, 76681, 76684, 76686, 76691, 76688, 76717, 76715,
                121497, 121505};
            public static int[] TeamHeals = { 118245, 118230, 118232, 118231, 118235, 118233,
                118234, 118238, 118236, 118237, 118241, 118239, 118240, 118243, 118244,
                118242, 43374};
        }

        public enum PerkSelection
        {
            None, Sacrifice, PurpleHeart
        }
        public enum HealSelection
        {
            None, SingleTeam, SingleArea, Team
        }
        public enum NanoHealSelection
        {
            None, Combat
        }
        public enum RKNanoDrainSelection
        {
            None, Target, Boss
        }

        public enum SLNanoDrainSelection
        {
            None, Target, Boss
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
        public enum MPDamageDebuffLineASelection
        {
            None, Target, Area, Boss
        }
        public enum TraderShutdownSkillDebuffSelection
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
        public enum ModeSelection
        {
            None, All, Adds
        }
        public enum ProcType1Selection
        {
            DebtCollection = 1431327317,
            AccumulatedInterest = 1163084626,
            ExchangeProduct = 1096108366,
            UnforgivenDebts = 1380338753,
            UnexpectedBonus = 1430602318,
            Rebate = 1313882454
        }

        public enum ProcType2Selection
        {
            UnopenedLetter = 1195724622,
            RigidLiquidation = 1145458777,
            DepleteAssets = 1162039378,
            Escrow = 1179599938,
            RefinanceLoans = 1145456965,
            PaymentPlan = 1348030540
        }

        public enum PetSelection
        {
            None, Target, Boss
        }

        #endregion
    }
}
