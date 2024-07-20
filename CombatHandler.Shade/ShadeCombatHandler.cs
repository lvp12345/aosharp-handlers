using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CombatHandler.Shade
{
    public class ShadeCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleRez = false;

        private static bool _shadeSiphon;

        private static Window _buffWindow;
        private static Window _debuffWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;
        private static Window _healingWindow;
        private static Window _specialAttacksWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;
        private static View _healingView;
        private static View _specialAttacksView;

        private static double _ncuUpdateTime;

        public ShadeCombatHandler(string pluginDir) : base(pluginDir)
        {
            try
            {
                IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalRez, OnGlobalRezMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.ClearBuffs, OnClearBuffs);
                IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

                Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentageChangedEvent += FountainOfLifeHealPercentage_Changed;
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
                Config.CharSettings[DynelManager.LocalPlayer.Name].ShadesCaressPercentageChangedEvent += ShadesCaressPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].ShadeTattooPercentageChangedEvent += ShadeTattooPercentage_Changed;

                _settings.AddVariable("AllPlayers", false);
                _settings["AllPlayers"] = false;

                _settings.AddVariable("Buffing", true);
                _settings.AddVariable("Composites", true);

                _settings.AddVariable("GlobalBuffing", true);
                _settings.AddVariable("GlobalComposites", true);
                _settings.AddVariable("GlobalRez", true);
                _settings.AddVariable("SpamHealthDrain", false);

                _settings.AddVariable("SharpObjects", true);
                _settings.AddVariable("Grenades", true);

                _settings.AddVariable("TauntTool", false);

                _settings.AddVariable("StimTargetSelection", 0);

                _settings.AddVariable("Kits", true);

                _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.BlackenedLegacy);
                _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.Blackheart);

                _settings.AddVariable("Runspeed", 0);

                _settings.AddVariable("AAD", false);

                _settings.AddVariable("SLMap", false);

                _settings.AddVariable("MASelection", (int)MASelection.Sappo);
                _settings.AddVariable("IntelligenceSelection", 0);

                _settings.AddVariable("ShadeProcSelection", 0);

                _settings.AddVariable("InitDebuffProc", false);
                _settings.AddVariable("DamageProc", false);
                _settings.AddVariable("DOTProc", false);
                _settings.AddVariable("StunProc", false);

                _settings.AddVariable("HealthDrain", false);
                _settings.AddVariable("SpiritSiphon", false);

                _settings.AddVariable("ShouldMoveBehindTarget", 0);

                RegisterSettingsWindow("Shade Handler", "ShadeSettingsView.xml");

                //Perks
                RelevantPerks.SpiritPhylactery.ForEach(p => RegisterPerkProcessor(p, SpiritPhylacteryPerk));
                RelevantPerks.TotemicRites.ForEach(p => RegisterPerkProcessor(p, TotemicRitesPerk, CombatActionPriority.Medium));
                RelevantPerks.PiercingMastery.ForEach(p => RegisterPerkProcessor(p, PiercingMasteryPerk, CombatActionPriority.Medium));
                RegisterPerkProcessor(PerkHash.Symbiosis, SymbiosisPerk, CombatActionPriority.High);
                RegisterPerkProcessor(PerkHash.EvasiveStance, EvasiveStance, CombatActionPriority.Low);

                //Spells
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EmergencySneak).OrderByStackingOrder(), SmokeBombNano, CombatActionPriority.High);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NemesisNanoPrograms).OrderByStackingOrder(), ShadesCaress, CombatActionPriority.High);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealthDrain).OrderByStackingOrder(), HealthDrain);
                RegisterSpellProcessor(RelevantNanos.SpiritDrain, SpiritSiphon);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AgilityBuff).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ConcealmentBuff).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MultiwieldBuff).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtsBuff).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadePiercingBuff).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SneakAttackBuffs).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.WeaponEffectAdd_On2).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AADBuffs).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, "AAD"));

                RegisterSpellProcessor(RelevantNanos.ShadeDmgProc,
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericShadeProc(spell, fightingTarget, ref actionTarget, ShadeProcSelection.Damage));
                RegisterSpellProcessor(RelevantNanos.ShadeStunProc,
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericShadeProc(spell, fightingTarget, ref actionTarget, ShadeProcSelection.Stun));
                RegisterSpellProcessor(RelevantNanos.ShadeInitDebuffProc,
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericShadeProc(spell, fightingTarget, ref actionTarget, ShadeProcSelection.InitDebuff));
                RegisterSpellProcessor(RelevantNanos.ShadeDOTProc,
                 (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericShadeProc(spell, fightingTarget, ref actionTarget, ShadeProcSelection.DOT));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RunspeedBuffs).OrderByStackingOrder(), FasterThanYourShadow);

                //Items
                RegisterItemProcessor(RelevantItems.Tattoo, RelevantItems.Tattoo, TattooItem, CombatActionPriority.High);

                int maItem = _settings["MASelection"].AsInt32();
                int intelligenceItem = _settings["IntelligenceSelection"].AsInt32();
                RegisterItemProcessor(maItem, maItem, MAItem);
                RegisterItemProcessor(intelligenceItem, intelligenceItem, IntelligenceItem);

                //LE Proc
                RegisterPerkProcessor(PerkHash.LEProcShadeBlackenedLegacy, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcShadeSiphonBeing, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcShadeShadowedGift, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcShadeDrainEssence, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcShadeElusiveSpirit, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcShadeToxicConfusion, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcShadeSapLife, LEProc1, CombatActionPriority.Low);

                RegisterPerkProcessor(PerkHash.LEProcShadeBlackheart, LEProc2, CombatActionPriority.Low); ;
                RegisterPerkProcessor(PerkHash.LEProcShadeTwistedCaress, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcShadeConcealedSurprise, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcShadeMisdirection, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcShadeDeviousSpirit, LEProc2, CombatActionPriority.Low);


                PluginDirectory = pluginDir;

                Healing.FountainOfLifeHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentage;
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
                ShadesCaressPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].ShadesCaressPercentage;
                ShadeTattooPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].ShadeTattooPercentage;
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
        public Window[] _windows => new Window[] { _buffWindow, _healingWindow, _debuffWindow, _specialAttacksWindow, _procWindow, _itemWindow, _perkWindow };

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
                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\ShadeBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "ShadeBuffsView" }, _buffView);
                window.FindView("ShadesCaressPercentageBox", out TextInputView shadesCaressInput);

                if (shadesCaressInput != null)
                {
                    shadesCaressInput.Text = $"{ShadesCaressPercentage}";
                }

            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "ShadeBuffsView" }, _buffView, out var container);
                _buffWindow = container;
                container.FindView("ShadesCaressPercentageBox", out TextInputView shadesCaressInput);

                if (shadesCaressInput != null)
                {
                    shadesCaressInput.Text = $"{ShadesCaressPercentage}";
                }
            }
        }
        private void HandleHealingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_healingView)) { return; }

                _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\ShadeHealingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Healing", XmlViewName = "ShadeHealingView" }, _healingView);

                window.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);

                if (FountainOfLifeInput != null)
                {
                    FountainOfLifeInput.Text = $"{Healing.FountainOfLifeHealPercentage}";
                }
            }
            else if (_healingWindow == null || (_healingWindow != null && !_healingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_healingWindow, PluginDir, new WindowOptions() { Name = "Healing", XmlViewName = "ShadeHealingView" }, _healingView, out var container);
                _healingWindow = container;

                container.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);

                if (FountainOfLifeInput != null)
                {
                    FountainOfLifeInput.Text = $"{Healing.FountainOfLifeHealPercentage}";
                }
            }
        }
        private void HandleDebuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\ShadeDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "ShadeDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "ShadeDebuffsView" }, _debuffView, out var container);
                _debuffWindow = container;
            }
        }
        private void HandleSpecialAttacksViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_specialAttacksView)) { return; }

                _specialAttacksView = View.CreateFromXml(PluginDirectory + "\\UI\\ShadeSpecialAttacksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "SpecialAttacks", XmlViewName = "ShadeSpecialAttacksView" }, _specialAttacksView);
            }
            else if (_specialAttacksWindow == null || (_specialAttacksWindow != null && !_specialAttacksWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_specialAttacksWindow, PluginDir, new WindowOptions() { Name = "SpecialAttacks", XmlViewName = "ShadeSpecialAttacksView" }, _specialAttacksView, out var container);
                _specialAttacksWindow = container;
            }
        }
        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_perkView)) { return; }

                _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\ShadePerksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Perks", XmlViewName = "ShadePerksView" }, _perkView);

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
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "ShadePerksView" }, _perkView, out var container);
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

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\ShadeItemsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "ShadeItemsView" }, _itemView);

                window.FindView("StimTargetBox", out TextInputView stimTargetInput);
                window.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                window.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                window.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                window.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);
                window.FindView("BodyDevAbsorbsItemPercentageBox", out TextInputView bodyDevInput);
                window.FindView("StrengthAbsorbsItemPercentageBox", out TextInputView strengthInput);
                window.FindView("ShadeTattooPercentageBox", out TextInputView shadeTattooInput);

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
                if (shadeTattooInput != null)
                {
                    shadeTattooInput.Text = $"{ShadeTattooPercentage}";
                }
            }
            else if (_itemWindow == null || (_itemWindow != null && !_itemWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "ShadeItemsView" }, _itemView, out var container);
                _itemWindow = container;

                container.FindView("StimTargetBox", out TextInputView stimTargetInput);
                container.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
                container.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
                container.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                container.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);
                container.FindView("BodyDevAbsorbsItemPercentageBox", out TextInputView bodyDevInput);
                container.FindView("StrengthAbsorbsItemPercentageBox", out TextInputView strengthInput);
                container.FindView("ShadeTattooPercentageBox", out TextInputView shadeTattooInput);

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
                if (shadeTattooInput != null)
                {
                    shadeTattooInput.Text = $"{ShadeTattooPercentage}";
                }
            }
        }
        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\ShadeProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "ShadeProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "ShadeProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }
        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 2.3)
                return;

            if (Time.NormalTime > _ncuUpdateTime + 1.0f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }

            CancelBuffs();

            GetBehindAndPoke(_settings["ShouldMoveBehindTarget"].AsInt32());

            #region UI

            var window = SettingsController.FindValidWindow(_windows);

            if (window != null && window.IsValid)
            {
                window.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);
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
                window.FindView("ShadesCaressPercentageBox", out TextInputView shadesCaressInput);
                window.FindView("ShadeTattooPercentageBox", out TextInputView shadeTattooInput);

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

                if (shadesCaressInput != null && !string.IsNullOrEmpty(shadesCaressInput.Text))
                {
                    if (int.TryParse(shadesCaressInput.Text, out int shadesCaressValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].ShadesCaressPercentage != shadesCaressValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].ShadesCaressPercentage = shadesCaressValue;
                        }
                    }
                }

                if (shadeTattooInput != null && !string.IsNullOrEmpty(shadeTattooInput.Text))
                {
                    if (int.TryParse(shadeTattooInput.Text, out int shadeTattooValue))
                    {
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].ShadeTattooPercentage != shadeTattooValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].ShadeTattooPercentage = shadeTattooValue;
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

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                {
                    buffView.Tag = SettingsController.settingsWindow;
                    buffView.Clicked = HandleBuffViewClick;
                }

                if (SettingsController.settingsWindow.FindView("HealingView", out Button healingView))
                {
                    healingView.Tag = SettingsController.settingsWindow;
                    healingView.Clicked = HandleHealingViewClick;
                }

                if (SettingsController.settingsWindow.FindView("DebuffsView", out Button debuffView))
                {
                    debuffView.Tag = SettingsController.settingsWindow;
                    debuffView.Clicked = HandleDebuffViewClick;
                }

                if (SettingsController.settingsWindow.FindView("SpecialAttacksView", out Button specialAttacksView))
                {
                    specialAttacksView.Tag = SettingsController.settingsWindow;
                    specialAttacksView.Clicked = HandleSpecialAttacksViewClick;
                }

                if (SettingsController.settingsWindow.FindView("ProcsView", out Button procView))
                {
                    procView.Tag = SettingsController.settingsWindow;
                    procView.Clicked = HandleProcViewClick;
                }

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

            base.OnUpdate(deltaTime);
        }

        #region Items
        private bool MAItem(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            item.Id = _settings["MASelection"].AsInt32();

            if (item.Id == 0) { return false; }
            if (fightingtarget == null) { return false; }
            if (Item.HasPendingUse) { return false; }
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.MartialArts)) { return false; }

            return true;
        }
        private bool IntelligenceItem(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            item.Id = _settings["IntelligenceSelection"].AsInt32();

            if (item.Id == 0) { return false; }
            if (fightingtarget == null) { return false; }
            if (Item.HasPendingUse) { return false; }
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Intelligence)) { return false; }

            return true;
        }
        private bool TattooItem(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (fightingtarget == null) { return false; }
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)) { return false; }
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BiologicalMetamorphosis)) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent <= ShadeTattooPercentage && fightingtarget?.HealthPercent > 5)
            {
                return true;
            }
            return false;
        }

        #endregion

        #region Perks

        private bool PiercingMasteryPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            if (_actionQueue.Any(x => x.CombatAction is PerkAction action && RelevantPerks.TotemicRites.Contains(action.Hash))) { return false; }
  
            if (!(PerkAction.Find(PerkHash.Stab, out PerkAction stab) && PerkAction.Find(PerkHash.DoubleStab, out PerkAction doubleStab)))
                return true;

            if (perkAction.Hash == PerkHash.Perforate)
            {
                if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (action == stab || action == doubleStab))) { return false; }
            }

            if (!(PerkAction.Find(PerkHash.Perforate, out PerkAction perforate) && PerkAction.Find(PerkHash.Lacerate, out PerkAction lacerate))) { return true; }

            if (perkAction.Hash == PerkHash.Impale)
            {
                if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (action == stab || action == doubleStab || action == perforate || action == lacerate))) { return false; }
            }

            return true;
        }

        private bool SpiritPhylacteryPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (RelevantPerks.TotemicRites.Contains(action.Hash)
            || RelevantPerks.PiercingMastery.Contains(action.Hash)))) { return false; }

            return true;
        }

        private bool SymbiosisPerk(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || PerkAction.List.Any(p => p.Hash == PerkHash.RitualOfDevotion && !p.IsAvailable)) { return false; }

            if (PerkAction.List.Any(a => a.IsExecuting)) { return false; }

            return true;
        }

        private bool TotemicRitesPerk(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (RelevantPerks.SpiritPhylactery.Contains(action.Hash)
            || RelevantPerks.PiercingMastery.Contains(action.Hash)))) { return false; }

            return true;
        }

        #endregion

        #region Procs

        #endregion

        #region Buffs
        private bool FasterThanYourShadow(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var setting = _settings["Runspeed"].AsInt32();

            if (setting == 0) { return false; }
            if (IsInsideInnerSanctum()) { return false; }

            if (setting != 0)
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.RK_RUN_BUFFS)) { CancelBuffs(RelevantNanos.RK_RUN_BUFFS); }
            }

            switch (_settings["Runspeed"].AsInt32())
            {
                case 1://Self
                    return NonCombatBuff(spell, ref actionTarget, fightingTarget, null);
                case 2://Team
                    return NonComabtTeamBuff(spell, fightingTarget, ref actionTarget);
                default:
                    return false;
            }
        }
    
        private bool SmokeBombNano(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Buffing"].AsBool()) { return false; }
            if (DynelManager.LocalPlayer.HealthPercent > 30) { return false; }
            actionTarget.ShouldSetTarget = false;
            return true;
        }
        private bool SpiritSiphon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["SpiritSiphon"].AsBool()) { return false; }

            if (fightingTarget == null && _shadeSiphon)
            {
                _shadeSiphon = false;
            }

            if (!DynelManager.LocalPlayer.IsAttacking) { return false; }

            if (DynelManager.LocalPlayer.Nano < spell.Cost) { return false; }

            if (fightingTarget != null && fightingTarget.HealthPercent <= 21)
            {
                if (!_shadeSiphon)
                {
                    _shadeSiphon = true;
                    spell.Cast(fightingTarget);
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Debuffs
        private bool ShadesCaress(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Buffing"].AsBool()) { return false; }

            if (!DynelManager.LocalPlayer.IsAttacking || fightingTarget == null
                 || !CanCast(spell)) { return false; }

            if (fightingTarget.HealthPercent < 5) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                var teamMembersLowHp = DynelManager.Characters
                    .Where(c => Team.Members.Any(t => t.Identity.Instance == c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 50)
                    .ToList();

                if (teamMembersLowHp.Count >= 3)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = fightingTarget;
                    return true;
                }
            }

            if (DynelManager.LocalPlayer.HealthPercent <= ShadesCaressPercentage && fightingTarget.HealthPercent > 5) { return true; }

            return false;
        }
        private bool HealthDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            if (_settings["SpamHealthDrain"].AsBool())
            {
                if (!CanCast(spell)) { return false; }
                if (!spell.IsReady) { return false; }
                if (Spell.HasPendingCast) { return false; }
                if (fightingTarget == null) { return false; }

                return true;
            }

            if (!_settings["HealthDrain"].AsBool()) { return false; }

            if (fightingTarget == null) { return false; }

            return ToggledTargetDebuff("HealthDrain", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool GenericShadeProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, ShadeProcSelection procType)
        {
            ShadeProcSelection currentSetting = (ShadeProcSelection)_settings["ShadeProcSelection"].AsInt32();

            if (currentSetting != procType)
            {
                return false;
            }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }
        #endregion

        #region Misc
        private void CancelBuffs()
        {
            if (ShadeProcSelection.Damage != (ShadeProcSelection)_settings["ShadeProcSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ShadeDmgProc);
            }
            if (ShadeProcSelection.InitDebuff != (ShadeProcSelection)_settings["ShadeProcSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ShadeInitDebuffProc);
            }
            if (ShadeProcSelection.DOT != (ShadeProcSelection)_settings["ShadeProcSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ShadeDOTProc);
            }
            if (ShadeProcSelection.Stun != (ShadeProcSelection)_settings["ShadeProcSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ShadeStunProc);
            }
        }
        private class RelevantItems
        {
            public const int Sappo = 267525;
            public const int Tattoo = 269511;
        }
        private class RelevantNanos
        {
            public const int ShadesCaress = 266300;
            public const int CompositeAttribute = 223372;
            public const int CompositeNano = 223380;
            public const int CompositeMelee = 223360;
            public const int CompositeMeleeSpec = 215264;
            public const int SpiritDrain = 297342;
            public static readonly int[] FasterThanYourShadow = { 272371 };
            public static readonly int[] EVASION_BUFFS = { 275844, 29247, 28903, 28878, 28872, 218070, 218068, 218066,
            218064, 218062, 218060, 272371, 270808, 30745, 302188, 29272, 270802, 28603, 223125, 223131, 223129, 215718,
            223127, 272416, 272415, 272414, 272413, 272412};
            public static readonly int[] RK_RUN_BUFFS = { 93132, 93126, 93127, 93128, 93129, 93130, 93131, 93125 };

            public static readonly int[] ShadeDmgProc = { 224167, 224165, 224163, 210371, 210369, 210367, 210365, 210363, 210351, 210359, 210357, 210533, 210353, };
            public static readonly int[] ShadeStunProc = { 224171, 224169, 210380, 210378, 210376, };
            public static readonly int[] ShadeInitDebuffProc = { 224177, 210407, 210401, };
            public static readonly int[] ShadeDOTProc = { 224161, 224159, 210395, 210393, 210391, 210389, 210387, };
        }
        private class RelevantPerks
        {
            public static readonly List<PerkHash> TotemicRites = new List<PerkHash>
            {
                PerkHash.RitualOfDevotion,
                PerkHash.DevourVigor,
                PerkHash.RitualOfZeal,
                PerkHash.DevourEssence,
                PerkHash.RitualOfSpirit,
                PerkHash.DevourVitality,
                PerkHash.RitualOfBlood
            };
            public static readonly List<PerkHash> PiercingMastery = new List<PerkHash>
            {
                PerkHash.Stab,
                PerkHash.DoubleStab,
                PerkHash.Perforate,
                PerkHash.Lacerate,
                PerkHash.Impale,
                PerkHash.Gore,
                PerkHash.Hecatomb
            };
            public static readonly List<PerkHash> SpiritPhylactery = new List<PerkHash>
            {
                PerkHash.CaptureVigor,
                PerkHash.UnsealedBlight,
                PerkHash.CaptureEssence,
                PerkHash.UnsealedPestilence,
                PerkHash.CaptureSpirit,
                PerkHash.UnsealedContagion,
                PerkHash.CaptureVitality
            };
        }
        public enum MASelection
        {
            None, Sappo = 267525, Shen = 201281, FlowerofLife = 204326, BrightBlueCloudlessSky = 204328, BlessedwithThunder = 204327, BirdofPrey = 204329,
            AttackoftheSnake = 204330, AngelofNight = 204331
        }
        
        public enum ShadeProcSelection
        {
            None, Damage, Stun, InitDebuff, DOT
        }
        public enum ProcType1Selection
        {
            BlackenedLegacy = 1111706695,
            SiphonBeing = 1397768775,
            ShadowedGift = 1396787014,
            DrainEssence = 1146242387,
            ElusiveSpirit = 1163219794,
            ToxicConfusion = 1330529877,
            SapLife = 1397771337
        }
        public enum ProcType2Selection
        {
            Blackheart = 1129007173,
            TwistedCaress = 1347179342,
            ConcealedSurprise = 1213354838,
            Misdirection = 1396984146,
            DeviousSpirit = 1146508112
        }
        #endregion
    }
}
