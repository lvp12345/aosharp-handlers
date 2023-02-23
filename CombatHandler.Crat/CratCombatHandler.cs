using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace CombatHandler.Bureaucrat
{
    public class CratCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private double _lastTrimTime = 0;
        private const float DelayBetweenTrims = 1;
        private const float DelayBetweenDiverTrims = 305;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        //private static bool ToggleDebuffing = false;

        private bool attackPetTrimmedAggressive = false;

        private static Window _buffWindow;
        private static Window _debuffWindow;
        private static Window _petWindow;
        private static Window _calmingWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _calmView;
        private static View _petView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;

        private readonly Dictionary<PetType, bool> petTrimmedAggDef = new Dictionary<PetType, bool>();
        private readonly Dictionary<PetType, bool> petTrimmedOffDiv = new Dictionary<PetType, bool>();

        private readonly Dictionary<PetType, double> _lastPetTrimDivertOffTime = new Dictionary<PetType, double>()
        {
            { PetType.Attack, 0 },
            { PetType.Support, 0 }
        };

        private static double _ncuUpdateTime;

        public CratCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
            //IPCChannel.RegisterCallback((int)IPCOpcode.GlobalDebuffing, OnGlobalDebuffingMessage);

            Config.CharSettings[Game.ClientInst].CycleXpPerksDelayChangedEvent += CycleXpPerksDelay_Changed;
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

            _settings.AddVariable("Exoneration", false);

            _settings.AddVariable("StimTargetSelection", (int)StimTargetSelection.None);

            _settings.AddVariable("Kits", true);

            _settings.AddVariable("BuffingAuraSelection", (int)BuffingAuraSelection.AAOAAD);
            _settings.AddVariable("DebuffingAuraSelection", (int)DebuffingAuraSelection.None);

            _settings.AddVariable("CalmingSelection", (int)CalmingSelection.SL);
            _settings.AddVariable("ModeSelection", (int)ModeSelection.None);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.FormsinTriplicate);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.WrongWindow);

            _settings.AddVariable("NanoDeltaTeam", false);
            _settings.AddVariable("PistolTeam", false);
            _settings.AddVariable("NeuronalStimulatorTeam", false);
            _settings.AddVariable("CutRedTape", false);

            _settings.AddVariable("SyncPets", true);
            _settings.AddVariable("SpawnPets", true);
            _settings.AddVariable("BuffPets", true);
            _settings.AddVariable("WarpPets", false);

            _settings.AddVariable("MastersBidding", false);

            _settings.AddVariable("InitDebuffSelection", (int)InitDebuffSelection.Boss);
            _settings.AddVariable("RedTapeSelection", (int)RedTapeSelection.Boss);
            _settings.AddVariable("IntensifyStressSelection", (int)IntensifyStressSelection.Boss);

            _settings.AddVariable("DivertTrimmer", true);
            _settings.AddVariable("TauntTrimmer", true);
            _settings.AddVariable("AggDefTrimmer", true);

            _settings.AddVariable("Nuking", true);
            _settings.AddVariable("Root", true);

            _settings.AddVariable("CycleXpPerks", false);

            _settings.AddVariable("Calm12Man", false);
            //_settings.AddVariable("CalmSector7", false);

            RegisterSettingsWindow("Bureaucrat Handler", "BureaucratSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcBureaucratPleaseHold, PleaseHold, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratFormsInTriplicate, FormsinTriplicate, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratSocialServices, SocialServices, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratNextWindowOver, NextWindowOver, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratWaitInThatQueue, WaitInThatQueue, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcBureaucratMobilityEmbargo, MobilityEmbargo, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratWrongWindow, WrongWindow, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratTaxAudit, TaxAudit, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratLostPaperwork, LostPaperwork, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratDeflation, Deflation, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratInflationAdjustment, InflationAdjustment, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcBureaucratPapercut, Papercut, CombatActionPriority.Low);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.PistolBuffsSelf, PistolSelfOnly);

            //Team Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDeltaBuffs).OrderByStackingOrder(), NanoDelta);
            RegisterSpellProcessor(RelevantNanos.PistolBuffs, PistolTeam);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Psy_IntBuff).OrderByStackingOrder(), PsyIntBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalDecreaseBuff).OrderByStackingOrder(), CutRedTape);

            //Buff Aura
            RegisterSpellProcessor(RelevantNanos.AadBuffAuras, BuffAAOAADAura);
            RegisterSpellProcessor(RelevantNanos.CritBuffAuras, BuffCritAura);
            RegisterSpellProcessor(RelevantNanos.NanoResBuffAuras, BuffNanoResistAura);

            //Debuff Aura
            RegisterSpellProcessor(RelevantNanos.NanoPointsDebuffAuras, DebuffNanoDrainAura);
            RegisterSpellProcessor(RelevantNanos.NanoResDebuffAuras, DebuffNanoResistAura);
            RegisterSpellProcessor(RelevantNanos.CritDebuffAuras, DebuffCritAura);

            // Exonerationon/ AOE Root Reducer
            RegisterSpellProcessor(RelevantNanos.CorporateLeadership, RootReducer);

            //Calms
            RegisterSpellProcessor(RelevantNanos.ShadowlandsCalms, SLCalm, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.AOECalms, AOECalm, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.RkCalms, RKCalm, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.LastMinNegotiations, Calm12Man, CombatActionPriority.High);
            //RegisterSpellProcessor(RelevantNanos.RkCalms, CalmSector7, CombatActionPriority.High);

            //Root/Snare
            RegisterSpellProcessor(RelevantNanos.PuissantVoidInertia, Root, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.ShacklesofObedience, Snare, CombatActionPriority.High);

            //Debuffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder(), InitDebuffs, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.GeneralRadACDebuff, InitDebuffs, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.GeneralProjACDebuff, InitDebuffs, CombatActionPriority.Medium);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SkillLockModifierDebuff847).OrderByStackingOrder(), RedTape, CombatActionPriority.Medium);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDeltaDebuff).OrderByStackingOrder(), IntensifyStress, CombatActionPriority.Medium);

            //Nukes
            RegisterSpellProcessor(RelevantNanos.WorkplaceDepression, WorkplaceDepressionTargetDebuff, CombatActionPriority.Low);
            RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke, CombatActionPriority.Low);

            //Perks
            RegisterPerkProcessor(PerkHash.Leadership, Leadership);
            RegisterPerkProcessor(PerkHash.Governance, Governance);
            RegisterPerkProcessor(PerkHash.TheDirector, TheDirector);
            RegisterPerkProcessor(PerkHash.EvasiveStance, EvasiveStance, CombatActionPriority.High);

            //Pet Spawners
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SupportPets).OrderByStackingOrder(), CarloSpawner);
            RegisterSpellProcessor(PetsList.Pets.Where(x => x.Value.PetType == PetType.Attack).Select(x => x.Key).ToArray(), RobotSpawner);

            //Pet Buffs
            if (Spell.Find(RelevantNanos.CorporateStrategy, out Spell spell))
            {
                RegisterSpellProcessor(RelevantNanos.CorporateStrategy, CorporateStrategy);
            }
            else
            {
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetShortTermDamageBuffs).OrderByStackingOrder(), PetBuff);
            }

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDamageOverTimeResistNanos).OrderByStackingOrder(), PetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDefensiveNanos).OrderByStackingOrder(), PetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetTauntBuff).OrderByStackingOrder(), PetBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeMartialProwess, PetBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeMelee, PetBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeMartialProwess, PetSupportTargetBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeMelee, PetSupportTargetBuff);

            RegisterSpellProcessor(RelevantNanos.DroidDamageMatrix, DroidMatrixBuff);
            RegisterSpellProcessor(RelevantNanos.DroidPressureMatrix, DroidMatrixBuff);

            RegisterSpellProcessor(RelevantNanos.MastersBidding, MastersBidding);

            RegisterSpellProcessor(RelevantNanos.PetCleanse, PetCleanse);

            //Pet warp
            RegisterSpellProcessor(RelevantNanos.PetWarp, PetWarp);

            //Pet Trimmers
            RegisterItemProcessor(RelevantTrimmers.PositiveAggressiveDefensive, PetAggDefTrimmer);
            RegisterItemProcessor(RelevantTrimmers.IncreaseAggressiveness, PetAggressiveTrimmer);
            RegisterItemProcessor(RelevantTrimmers.DivertEnergyToOffense, PetDivertOffTrimmer);

            ResetTrimmers();

            //Pet Perks
            

            PluginDirectory = pluginDir;

            CycleXpPerksDelay = Config.CharSettings[Game.ClientInst].CycleXpPerksDelay;
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

        public Window[] _windows => new Window[] { _calmingWindow, _buffWindow, _petWindow, _procWindow, _debuffWindow, _itemWindow, _perkWindow };

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

        //private void OnGlobalDebuffingMessage(int sender, IPCMessage msg)
        //{
        //    GlobalDebuffingMessage debuffMsg = (GlobalDebuffingMessage)msg;

        //    _settings[$"Debuffing"] = debuffMsg.Switch;
        //    _settings[$"Debuffing"] = debuffMsg.Switch;
        //}

        #endregion

        #region Handles

        private void HandlePetViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_petView)) { return; }

                _petView = View.CreateFromXml(PluginDirectory + "\\UI\\BureaucratPetsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Pets", XmlViewName = "BureaucratPetsView" }, _petView);
            }
            else if (_petWindow == null || (_petWindow != null && !_petWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_petWindow, PluginDir, new WindowOptions() { Name = "Pets", XmlViewName = "BureaucratPetsView" }, _petView, out var container);
                _petWindow = container;
            }
        }

        private void HanndleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_buffView)) { return; }

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\BureaucratBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "BureaucratBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "BureaucratBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }

        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_perkView)) { return; }

                _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\BureaucratPerksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Perks", XmlViewName = "BureaucratPerksView" }, _perkView);

                window.FindView("XpPerksDelayBox", out TextInputView xpPerksInput);
                window.FindView("SphereDelayBox", out TextInputView sphereInput);
                window.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                window.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                window.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                window.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                window.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);

                if (xpPerksInput != null)
                    xpPerksInput.Text = $"{CycleXpPerksDelay}";
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
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "BureaucratPerksView" }, _perkView, out var container);
                _perkWindow = container;

                container.FindView("XpPerksDelayBox", out TextInputView xpPerksInput);
                container.FindView("SphereDelayBox", out TextInputView sphereInput);
                container.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                container.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                container.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                container.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                container.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);

                if (xpPerksInput != null)
                    xpPerksInput.Text = $"{CycleXpPerksDelay}";
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

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\BureaucratItemsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "BureaucratItemsView" }, _itemView);

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
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "BureaucratItemsView" }, _itemView, out var container);
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
                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\BureaucratProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "BureaucratProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "BureaucratProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        private void HandleDebuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_debuffView)) { return; }

                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\BureaucratDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "BureaucratDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "BureaucratDebuffsView" }, _debuffView, out var container);
                _debuffWindow = container;
            }
        }

        private void HandleCalmingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_calmView)) { return; }

                _calmView = View.CreateFromXml(PluginDirectory + "\\UI\\BureaucratCalmingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Calming", XmlViewName = "BureaucratCalmingView" }, _calmView);
            }
            else if (_calmingWindow == null || (_calmingWindow != null && !_calmingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_calmingWindow, PluginDir, new WindowOptions() { Name = "Calming", XmlViewName = "BureaucratCalmingView" }, _calmView, out var container);
                _calmingWindow = container;
            }
        }

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            var window = SettingsController.FindValidWindow(_windows);

            if (window != null && window.IsValid)
            {
                window.FindView("XpPerksDelayBox", out TextInputView xpPerksInput);
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

                if (xpPerksInput != null && !string.IsNullOrEmpty(xpPerksInput.Text))
                    if (int.TryParse(xpPerksInput.Text, out int xpPerksValue))
                        if (Config.CharSettings[Game.ClientInst].CycleXpPerksDelay != xpPerksValue)
                            Config.CharSettings[Game.ClientInst].CycleXpPerksDelay = xpPerksValue;

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

            if (Time.NormalTime > _ncuUpdateTime + 0.5f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }

            if (IsSettingEnabled("SyncPets"))
                SynchronizePetCombatStateWithOwner();

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

                if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                {
                    buffView.Tag = SettingsController.settingsWindow;
                    buffView.Clicked = HanndleBuffViewClick;
                }

                if (SettingsController.settingsWindow.FindView("DebuffsView", out Button debuffView))
                {
                    debuffView.Tag = SettingsController.settingsWindow;
                    debuffView.Clicked = HandleDebuffViewClick;
                }

                if (SettingsController.settingsWindow.FindView("CalmingView", out Button calmView))
                {
                    calmView.Tag = SettingsController.settingsWindow;
                    calmView.Clicked = HandleCalmingViewClick;
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

            HandleCancelDebuffAuras();
            HandleCancelBuffAuras();
        }

        #region LE Procs

        private bool FormsinTriplicate(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.FormsinTriplicate != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool NextWindowOver(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.NextWindowOver != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool PleaseHold(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.PleaseHold != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool SocialServices(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.SocialServices != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool WaitInThatQueue(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.WaitInThatQueue != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool Deflation(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Deflation != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool InflationAdjustment(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.InflationAdjustment != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool LostPaperwork(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.LostPaperwork != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool MobilityEmbargo(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.MobilityEmbargo != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool Papercut(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Papercut != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool TaxAudit(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.TaxAudit != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool WrongWindow(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.WrongWindow != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Exoneration



        protected bool RootReducer(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Exoneration")) { return false; }

            SimpleChar target = DynelManager.Players
            .Where(c => c.IsInLineOfSight
            && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
            && c.DistanceFrom(DynelManager.LocalPlayer) < 10f
            && c.Buffs.Contains(NanoLine.Root) 
            || c.Buffs.Contains(NanoLine.Snare) 
            || c.Buffs.Contains(305244) //Pause for Reflection
            && SpellChecksOther(spell, spell.Nanoline, c))
            .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        #endregion

        #region Buffs

        protected bool PistolSelfOnly(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        private bool NanoDelta(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("NanoDeltaTeam"))
                return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        protected bool PistolTeam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam && IsSettingEnabled("PistolTeam"))
                return TeamBuffExclusionWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);

            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        private bool PsyIntBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("NeuronalStimulatorTeam"))
                return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        protected bool CutRedTape(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("CutRedTape"))
                return GenericTeamBuff(spell, ref actionTarget);

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        #endregion

        #region Auras

        private bool BuffAAOAADAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.AAOAAD != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool BuffCritAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.Crit != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool BuffNanoResistAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BuffingAuraSelection.NanoResist != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32()) { return false; }

            return Buff(spell, spell.Nanoline, ref actionTarget);
        }

        private bool DebuffNanoDrainAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DebuffingAuraSelection.MaxNano != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()
                || fightingTarget == null) { return false; }

            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool DebuffNanoResistAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DebuffingAuraSelection.NanoResist != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()
                || fightingTarget == null) { return false; }

            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool DebuffCritAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DebuffingAuraSelection.Crit != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()
                || fightingTarget == null) { return false; }

            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }





        #endregion

        #region Debuffs

        private bool InitDebuffs(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (InitDebuffSelection.None == (InitDebuffSelection)_settings["InitDebuffSelection"].AsInt32()) { return false; }

            if (InitDebuffSelection.Boss == (InitDebuffSelection)_settings["InitDebuffSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (InitDebuffSelection.Area == (InitDebuffSelection)_settings["InitDebuffSelection"].AsInt32())
                return AreaDebuff(spell, ref actionTarget);

            if (InitDebuffSelection.Target == (InitDebuffSelection)_settings["InitDebuffSelection"].AsInt32()
                && fightingTarget != null)
            {
                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool RedTape(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (RedTapeSelection.None == (RedTapeSelection)_settings["RedTapeSelection"].AsInt32()) { return false; }

            if (RedTapeSelection.Boss == (RedTapeSelection)_settings["RedTapeSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (RedTapeSelection.Area == (RedTapeSelection)_settings["RedTapeSelection"].AsInt32())
                return AreaDebuff(spell, ref actionTarget);

            if (RedTapeSelection.Target == (RedTapeSelection)_settings["RedTapeSelection"].AsInt32()
                && fightingTarget != null)
            {
                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool IntensifyStress(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IntensifyStressSelection.None == (IntensifyStressSelection)_settings["IntensifyStressSelection"].AsInt32()) { return false; }

            if (IntensifyStressSelection.Boss == (IntensifyStressSelection)_settings["IntensifyStressSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (IntensifyStressSelection.Target == (IntensifyStressSelection)_settings["IntensifyStressSelection"].AsInt32()
                && fightingTarget != null)
            {
                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }

        #endregion

        #region Nukes

        private bool WorkplaceDepressionTargetDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !CanCast(spell) || !IsSettingEnabled("Nuking")
                || fightingTarget.Buffs.Contains(273632) || fightingTarget.Buffs.Contains(301842)
                || (fightingTarget.HealthPercent < 40 && fightingTarget.MaxHealth < 1000000)) { return false; }

            return true;
        }
        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !CanCast(spell) || !IsSettingEnabled("Nuking")) { return false; }

            if (Spell.Find(273631, out Spell workplace))
            {
                if (!fightingTarget.Buffs.Contains(273632) && !fightingTarget.Buffs.Contains(301842) &&
                    ((fightingTarget.HealthPercent >= 40 && fightingTarget.MaxHealth < 1000000)
                    || fightingTarget.MaxHealth > 1000000)) { return false; }
            }

            return true;
        }

        #endregion

        #region Calms

        private bool SLCalm(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (CalmingSelection.SL != (CalmingSelection)_settings["CalmingSelection"].AsInt32()) { return false; }

            if (ModeSelection.All == (ModeSelection)_settings["ModeSelection"].AsInt32())
            {
                SimpleChar target = DynelManager.NPCs
                    .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name)
                        && c.Health > 0
                        && c.IsInLineOfSight
                        && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && c.MaxHealth < 1000000)
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
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
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && c.MaxHealth < 1000000
                        && c.FightingTarget != null
                        && !AttackingMob(c)
                        && AttackingTeam(c))
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
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

        private bool AOECalm(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (CalmingSelection.AOE != (CalmingSelection)_settings["CalmingSelection"].AsInt32()) { return false; }

            if (ModeSelection.All == (ModeSelection)_settings["ModeSelection"].AsInt32())
            {
                SimpleChar target = DynelManager.NPCs
                    .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name)
                        && c.Health > 0
                        && c.IsInLineOfSight
                        && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && c.MaxHealth < 1000000)
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
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
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && c.MaxHealth < 1000000
                        && c.FightingTarget != null
                        && !AttackingMob(c)
                        && AttackingTeam(c))
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
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

        private bool RKCalm(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (CalmingSelection.RK != (CalmingSelection)_settings["CalmingSelection"].AsInt32()
                || !CanCast(spell) || ModeSelection.None == (ModeSelection)_settings["ModeSelection"].AsInt32()) { return false; }

            if (ModeSelection.All == (ModeSelection)_settings["ModeSelection"].AsInt32())
            {
                SimpleChar target = DynelManager.NPCs
                    .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name)
                        && c.Health > 0
                        && c.IsInLineOfSight
                        && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && c.MaxHealth < 1000000)
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
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
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && c.MaxHealth < 1000000
                        && c.FightingTarget != null
                        && !AttackingMob(c)
                        && AttackingTeam(c))
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
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

        private bool Calm12Man(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !IsSettingEnabled("Calm12Man") || !CanCast(spell)) { return false; }

            List<SimpleChar> targets = DynelManager.NPCs
                .Where(c => c.IsAlive
                    && c.DistanceFrom(DynelManager.LocalPlayer) < 20f
                    && (c.Name == "Right Hand of Madness" || c.Name == "Deranged Xan")
                    && (!c.Buffs.Contains(267535) || !c.Buffs.Contains(267536)))
                .ToList();

            if (targets.Count >= 1)
            {
                actionTarget.Target = targets.FirstOrDefault();
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        //private bool CalmSector7(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (!IsSettingEnabled("Buffing")) { return false; }

        //    if (!CanCast(spell)) { return false; }

        //    if (!IsSettingEnabled("CalmSector7")) { return false; }

        //    SimpleChar target = DynelManager.NPCs
        //        .Where(c => !debuffOSTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
        //        .Where(c => c.IsInLineOfSight)
        //        .Where(c => c.Name == "Kyr'Ozch Guardian")
        //        .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f) //Is in range for debuff (we assume weapon range == debuff range)
        //        .Where(c => !c.Buffs.Contains(NanoLine.Mezz))
        //        .Where(c => c.MaxHealth < 1000000)
        //        .FirstOrDefault();

        //    if (target != null)
        //    {
        //        actionTarget.Target = target;
        //        actionTarget.ShouldSetTarget = true;
        //        return true;
        //    }

        //    return false;
        //}



        #endregion

        #region Root/Snare

        private bool Root(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")
                || !IsSettingEnabled("Root") || !CanCast(spell)) { return false; }

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

        private bool Snare(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")
                || !IsSettingEnabled("Root") || !CanCast(spell)) { return false; }

            SimpleChar target = DynelManager.Characters
                    .Where(c => c.IsInLineOfSight
                        && c.IsMoving
                        && !c.Buffs.Contains(NanoLine.Root)
                        && c.Name == "Alien Heavy Patroller")
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

        #region Spawn Pets

        protected bool CarloSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 0) { return false; }

            return NoShellPetSpawner(PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        protected bool RobotSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 0) { return false; }

            return PetSpawner(PetsList.Pets, spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Buffs

        private bool CorporateStrategy(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetShortTermDamageBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool PetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(spell.Nanoline, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool PetSupportTargetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(spell.Nanoline, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        protected bool DroidMatrixBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (RobotNeedsBuff(spell, pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        protected bool MastersBidding(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone() || !IsSettingEnabled("MastersBidding")) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (!pet.Character.Buffs.Contains(NanoLine.SiphonBox683)
                    && (pet.Type == PetType.Attack || pet.Type == PetType.Support))
                {
                    if (spell.IsReady)
                        spell.Cast(pet.Character, true);
                }
            }

            return false;
        }

        #endregion

        #region Warp

        protected bool PetWarp(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("WarpPets") || !CanCast(spell) || !CanLookupPetsAfterZone()) { return false; }

            return DynelManager.LocalPlayer.Pets.Any(c => c.Character == null);
        }

        #endregion

        #region Trimmers

        protected bool PetAggDefTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AggDefTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (pet.Type == PetType.Attack
                        && CanAggDefTrim(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    petTrimmedAggDef[PetType.Attack] = true;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }
            }

            return false;
        }

        protected bool PetAggressiveTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("TauntTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (pet.Type == PetType.Attack
                        && CanTauntTrim(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    attackPetTrimmedAggressive = true;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }
            }

            return false;
        }

        protected bool PetDivertOffTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("DivertTrimmer") || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null) continue;

                if (pet.Type == PetType.Attack
                        && CanDivertOffTrim(pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    petTrimmedOffDiv[PetType.Attack] = true;
                    _lastPetTrimDivertOffTime[PetType.Attack] = Time.NormalTime;
                    _lastTrimTime = Time.NormalTime;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #endregion

        #region Misc

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

        protected bool FindSpellNanoLineFallbackToId(Spell spell, Buff[] buffs, out Buff buff)
        {
            if (buffs.Find(spell.Nanoline, out Buff buffFromNanoLine))
            {
                buff = buffFromNanoLine;
                return true;
            }
            int spellId = spell.Id;
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

        protected bool CanTrim()
        {
            return _lastTrimTime + DelayBetweenTrims < Time.NormalTime;
        }
        protected bool CanDivertOffTrim(Pet pet)
        {
            return _lastPetTrimDivertOffTime[pet.Type] + DelayBetweenDiverTrims < Time.NormalTime;
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
            petTrimmedOffDiv[PetType.Attack] = false;
            petTrimmedAggDef[PetType.Attack] = false;
        }

        private void HandleCancelBuffAuras()
        {
            if (BuffingAuraSelection.AAOAAD != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.AadBuffAuras);
            }
            if (BuffingAuraSelection.Crit != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.CritBuffAuras);
            }
            if (BuffingAuraSelection.NanoResist != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.NanoResBuffAuras);
            }
        }

        private void HandleCancelDebuffAuras()
        {
            CancelHostileAuras(RelevantNanos.CritDebuffAuras);
            CancelHostileAuras(RelevantNanos.NanoPointsDebuffAuras);
            CancelHostileAuras(RelevantNanos.NanoResDebuffAuras);

            if (DebuffingAuraSelection.Crit != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.CritDebuffAuras);
            }
            if (DebuffingAuraSelection.MaxNano != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.NanoPointsDebuffAuras);
            }
            if (DebuffingAuraSelection.NanoResist != (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.NanoResDebuffAuras);
            }
        }

        private static bool IsMoving(SimpleChar target)
        {
            if (Playfield.Identity.Instance == 4021)
            {
                return true;
            }

            return target.IsMoving;
        }
        private void OnZoned(object s, EventArgs e)
        {
            _lastZonedTime = Time.NormalTime;
            _lastCombatTime = double.MinValue;
            ResetTrimmers();
        }

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
            public const int PuissantVoidInertia = 224129;
            public const int ShacklesofObedience = 82463;
            public const int CompositeMartialProwess = 302158;
            public const int CompositeMelee = 223360;
            public static readonly int[] CorporateLeadership = { 205433, 205435, 205437, 205439 };
            public const int PetWarp = 209488;

            public static readonly int[] PistolBuffsSelf = { 263250, 263251 };
            public static readonly Spell[] PistolBuffs = Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder().Where(spell => spell.Id != GreaterGunSlinger && spell.Id != SkilledGunSlinger).ToArray();
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
            public static readonly int[] AOECalms = { 100422, 100424, 100426 };

            public static Dictionary<int, int> PetNanoToBuff = new Dictionary<int, int>
            {
                {DroidDamageMatrix, 285696},
                {DroidPressureMatrix, 302246},
                {CorporateStrategy, 285695}
            };

        }

        private static class RelevantTrimmers
        {

            public static readonly int[] IncreaseAggressiveness = { 154939, 154940 };
            public static readonly int[] DivertEnergyToOffense = { 88377, 88378 };
            public static readonly int[] PositiveAggressiveDefensive = { 88383, 88384 };
            public static readonly int[] DivertEnergyToHitpoints = { 88381, 88382 };

        }

        public enum InitDebuffSelection
        {
            None, Target, Area, Boss
        }
        public enum RedTapeSelection
        {
            None, Target, Area, Boss
        }

        public enum IntensifyStressSelection
        {
            None, Target, Area, Boss
        }

        public enum ProcType1Selection
        {
            PleaseHold, FormsinTriplicate, SocialServices, NextWindowOver, WaitInThatQueue
        }

        public enum ProcType2Selection
        {
            MobilityEmbargo, WrongWindow, TaxAudit, LostPaperwork, Deflation, InflationAdjustment, Papercut
        }

        public enum BuffingAuraSelection
        {
            AAOAAD, Crit, NanoResist
        }
        public enum DebuffingAuraSelection
        {
            None, NanoResist, Crit, MaxNano
        }
        public enum CalmingSelection
        {
            SL, RK, AOE
        }
        public enum ModeSelection
        {
            None, All, Adds
        }

        #endregion
    }
}
