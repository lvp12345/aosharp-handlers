using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System.Linq;

namespace CombatHandler.Adventurer
{
    public class AdvCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleRez = false;

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
            IPCChannel.RegisterCallback((int)IPCOpcode.GlobalRez, OnGlobalRezMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.ClearBuffs, OnClearBuffs);
            IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

            Config.CharSettings[DynelManager.LocalPlayer.Name].HealPercentageChangedEvent += HealPercentage_Changed;
            Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentageChangedEvent += CompleteHealPercentage_Changed;
            Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentageChangedEvent += BioCocoonPercentage_Changed;
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
            Config.CharSettings[DynelManager.LocalPlayer.Name].BioRegrowthPercentageChangedEvent += BioRegrowthPercentage_Changed;
            Config.CharSettings[DynelManager.LocalPlayer.Name].CycleBioRegrowthPerkDelayChangedEvent += CycleBioRegrowthPerkDelay_Changed;

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("GlobalBuffing", true);
            _settings.AddVariable("GlobalComposites", true);
            _settings.AddVariable("GlobalRez", true);

            _settings.AddVariable("EncaseInStone", false);

            _settings.AddVariable("SharpObjects", true);
            _settings.AddVariable("Grenades", true);

            _settings.AddVariable("StimTargetSelection", (int)StimTargetSelection.Self);

            _settings.AddVariable("Kits", true);

            _settings.AddVariable("HealSelection", (int)HealSelection.None);
            _settings.AddVariable("MorphSelection", (int)MorphSelection.None);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.AesirAbsorption);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.HealingHerbs);

            _settings.AddVariable("ArmorBuff", false);

            _settings.AddVariable("TeamArmorBuffs", false);
            _settings.AddVariable("DamageShields", false);
            _settings.AddVariable("XPBonus", false);
            _settings.AddVariable("RunspeedBuffs", false);
            _settings.AddVariable("TreatmentBuffSelection", (int)TreatmentBuffSelection.None);

            _settings.AddVariable("CH", false);

            RegisterSettingsWindow("Adventurer Handler", "AdvSettingsView.xml");

            //Healing
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CompleteHealingLine).OrderByStackingOrder(), CompleteHealing, CombatActionPriority.High);
            
            RegisterSpellProcessor(RelevantNanos.Heals,
                       (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                       GenericTargetHealing(spell, fightingTarget, ref actionTarget, "HealSelection"),
                       CombatActionPriority.High);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TeamHealing).OrderByStackingOrder(),
                        (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                        GenericTeamHealing(spell, fightingTarget, ref actionTarget, "HealSelection"),
                        CombatActionPriority.High);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine._1HEdgedBuff).OrderByStackingOrder(), Melee);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), Ranged);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShieldUpgrades).OrderByStackingOrder(),
            (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(),
            (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MultiwieldBuff).OrderByStackingOrder(),
            (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
             RegisterSpellProcessor(RelevantNanos.ArmorBuffs, Armor);

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.TargetArmorBuffs,
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonComabtTeamBuff(spell, fightingTarget, ref actionTarget, "TeamArmorBuffs"));
            RegisterSpellProcessor(RelevantNanos.TargetedDamageShields,
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonComabtTeamBuff(spell, fightingTarget, ref actionTarget, "DamageShields"));
            RegisterSpellProcessor(RelevantNanos.LearningbyDoing,
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonCombatBuff(spell, ref actionTarget, fightingTarget, "XPBonus"));
            RegisterSpellProcessor(RelevantNanos.TeamRunSpeedBuffs,
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonComabtTeamBuff(spell, fightingTarget, ref actionTarget, "RunspeedBuff"));

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FirstAidAndTreatmentBuff).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "TreatmentBuffSelection"));

            //Morphs
            RegisterSpellProcessor(RelevantNanos.DragonMorph, DragonMorph);
            RegisterSpellProcessor(RelevantNanos.LeetMorph, LeetMorph);
            RegisterSpellProcessor(RelevantNanos.WolfMorph, WolfMorph);
            RegisterSpellProcessor(RelevantNanos.SaberMorph, SaberMorph);

            //Morph Buffs
            RegisterSpellProcessor(RelevantNanos.DragonScales, DragonScales);
            RegisterSpellProcessor(RelevantNanos.LeetCrit, LeetCrit);
            RegisterSpellProcessor(RelevantNanos.WolfAgility, WolfAgility);
            RegisterSpellProcessor(RelevantNanos.SaberDamage, SaberDamage);

            //LE Procs
            //type1
            RegisterPerkProcessor(PerkHash.LEProcAdventurerAesirAbsorption, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerMacheteFlurry, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerSelfPreservation, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerSkinProtection, LEProc1, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerFerociousHits, LEProc1, CombatActionPriority.Low);

            //type2
            RegisterPerkProcessor(PerkHash.LEProcAdventurerHealingHerbs, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerCombustion, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerCharringBlow, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerRestoreVigor, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerMacheteSlice, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerSoothingHerbs, LEProc2, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerBasicDressing, LEProc2, CombatActionPriority.Low);

            PluginDirectory = pluginDir;

            HealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].HealPercentage;
            CompleteHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentage;
            BioCocoonPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage;
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
            BioRegrowthPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].BioRegrowthPercentage;
            CycleBioRegrowthPerkDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].CycleBioRegrowthPerkDelay;

        }

        public Window[] _windows => new Window[] { _morphWindow, _healingWindow, _procWindow, _buffWindow, _itemWindow, _perkWindow };

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

                window.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);
                window.FindView("SphereDelayBox", out TextInputView sphereInput);
                window.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                window.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                window.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                window.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                window.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);
                window.FindView("BioRegrowthPercentageBox", out TextInputView bioRegrowthPercentageInput);
                window.FindView("BioRegrowthDelayBox", out TextInputView bioRegrowthDelayInput);

                if (bioCocoonInput != null)
                    bioCocoonInput.Text = $"{BioCocoonPercentage}";
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
                if (bioRegrowthPercentageInput != null)
                    bioRegrowthPercentageInput.Text = $"{BioRegrowthPercentage}";
                if (bioRegrowthDelayInput != null)
                    bioRegrowthDelayInput.Text = $"{CycleBioRegrowthPerkDelay}";
            }
            else if (_perkWindow == null || (_perkWindow != null && !_perkWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "AdvPerksView" }, _perkView, out var container);
                _perkWindow = container;

                container.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);
                container.FindView("SphereDelayBox", out TextInputView sphereInput);
                container.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
                container.FindView("SelfHealPercentageBox", out TextInputView selfHealInput);
                container.FindView("SelfNanoPercentageBox", out TextInputView selfNanoInput);
                container.FindView("TeamHealPercentageBox", out TextInputView teamHealInput);
                container.FindView("TeamNanoPercentageBox", out TextInputView teamNanoInput);
                container.FindView("BioRegrowthPercentageBox", out TextInputView bioRegrowthPercentageInput);
                container.FindView("BioRegrowthDelayBox", out TextInputView bioRegrowthDelayInput);

                if (bioCocoonInput != null)
                    bioCocoonInput.Text = $"{BioCocoonPercentage}";
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
                if (bioRegrowthPercentageInput != null)
                    bioRegrowthPercentageInput.Text = $"{BioRegrowthPercentage}";
                if (bioRegrowthDelayInput != null)
                    bioRegrowthDelayInput.Text = $"{CycleBioRegrowthPerkDelay}";
            }
        }

        private void HandleItemViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_itemView)) { return; }

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\AdvItemsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "AdvItemsView" }, _itemView);

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
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "AdvItemsView" }, _itemView, out var container);
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
                if (window.Views.Contains(_morphView)) { return; }

                _morphView = View.CreateFromXml(PluginDirectory + "\\UI\\AdvMorphView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Morphs", XmlViewName = "AdvMorphView" }, _morphView);
            }
            else if (_morphWindow == null || (_morphWindow != null && !_morphWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_morphWindow, PluginDir, new WindowOptions() { Name = "Morphs", XmlViewName = "AdvMorphView" }, _morphView, out var container);
                _morphWindow = container;
            }
        }

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 1.0)
                return;

            base.OnUpdate(deltaTime);

            if (Time.NormalTime > _ncuUpdateTime + 1.0f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }

            #region UI

            var window = SettingsController.FindValidWindow(_windows);

            if (window != null && window.IsValid)
            {
                window.FindView("HealPercentageBox", out TextInputView healInput);
                window.FindView("CompleteHealPercentageBox", out TextInputView completeHealInput);
                window.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);
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
                window.FindView("BioRegrowthPercentageBox", out TextInputView bioRegrowthPercentageInput);
                window.FindView("BioRegrowthDelayBox", out TextInputView bioRegrowthDelayInput);

                if (healInput != null && !string.IsNullOrEmpty(healInput.Text))
                    if (int.TryParse(healInput.Text, out int healValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].HealPercentage != healValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].HealPercentage = healValue;

                if (completeHealInput != null && !string.IsNullOrEmpty(completeHealInput.Text))
                    if (int.TryParse(completeHealInput.Text, out int completeHealValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentage != completeHealValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentage = completeHealValue;

                if (bioCocoonInput != null && !string.IsNullOrEmpty(bioCocoonInput.Text))
                    if (int.TryParse(bioCocoonInput.Text, out int bioCocoonValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage != bioCocoonValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage = bioCocoonValue;

                if (stimTargetInput != null)
                    if (Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetName != stimTargetInput.Text)
                        Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetName = stimTargetInput.Text;

                if (stimHealthInput != null && !string.IsNullOrEmpty(stimHealthInput.Text))
                    if (int.TryParse(stimHealthInput.Text, out int stimHealthValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentage != stimHealthValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentage = stimHealthValue;

                if (stimNanoInput != null && !string.IsNullOrEmpty(stimNanoInput.Text))
                    if (int.TryParse(stimNanoInput.Text, out int stimNanoValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentage != stimNanoValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentage = stimNanoValue;

                if (kitHealthInput != null && !string.IsNullOrEmpty(kitHealthInput.Text))
                    if (int.TryParse(kitHealthInput.Text, out int kitHealthValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage != kitHealthValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage = kitHealthValue;

                if (kitNanoInput != null && !string.IsNullOrEmpty(kitNanoInput.Text))
                    if (int.TryParse(kitNanoInput.Text, out int kitNanoValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage != kitNanoValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage = kitNanoValue;

                if (sphereInput != null && !string.IsNullOrEmpty(sphereInput.Text))
                    if (int.TryParse(sphereInput.Text, out int sphereValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelay != sphereValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelay = sphereValue;

                if (witOfTheAtroxInput != null && !string.IsNullOrEmpty(witOfTheAtroxInput.Text))
                    if (int.TryParse(witOfTheAtroxInput.Text, out int witOfTheAtroxValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelay != witOfTheAtroxValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelay = witOfTheAtroxValue;

                if (selfHealInput != null && !string.IsNullOrEmpty(selfHealInput.Text))
                    if (int.TryParse(selfHealInput.Text, out int selfHealValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentage != selfHealValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentage = selfHealValue;

                if (selfNanoInput != null && !string.IsNullOrEmpty(selfNanoInput.Text))
                    if (int.TryParse(selfNanoInput.Text, out int selfNanoValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentage != selfNanoValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentage = selfNanoValue;

                if (teamHealInput != null && !string.IsNullOrEmpty(teamHealInput.Text))
                    if (int.TryParse(teamHealInput.Text, out int teamHealValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentage != teamHealValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentage = teamHealValue;

                if (teamNanoInput != null && !string.IsNullOrEmpty(teamNanoInput.Text))
                    if (int.TryParse(teamNanoInput.Text, out int teamNanoValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentage != teamNanoValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentage = teamNanoValue;

                if (bodyDevInput != null && !string.IsNullOrEmpty(bodyDevInput.Text))
                    if (int.TryParse(bodyDevInput.Text, out int bodyDevValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentage != bodyDevValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentage = bodyDevValue;

                if (strengthInput != null && !string.IsNullOrEmpty(strengthInput.Text))
                    if (int.TryParse(strengthInput.Text, out int strengthValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentage != strengthValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentage = strengthValue;

                if (bioRegrowthPercentageInput != null && !string.IsNullOrEmpty(bioRegrowthPercentageInput.Text))
                    if (int.TryParse(bioRegrowthPercentageInput.Text, out int bioRegrowthPercentageValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].BioRegrowthPercentage != bioRegrowthPercentageValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].BioRegrowthPercentage = bioRegrowthPercentageValue;

                if (bioRegrowthDelayInput != null && !string.IsNullOrEmpty(bioRegrowthDelayInput.Text))
                    if (int.TryParse(bioRegrowthDelayInput.Text, out int bioRegrowthDelayValue))
                        if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleBioRegrowthPerkDelay != bioRegrowthDelayValue)
                            Config.CharSettings[DynelManager.LocalPlayer.Name].CycleBioRegrowthPerkDelay = bioRegrowthDelayValue;
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

        #region Healing

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

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }
        private bool LeetMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Leet != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }
        private bool WolfMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Wolf != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }
        private bool SaberMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Saber != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        private bool WolfAgility(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Wolf != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.WolfMorph)) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }
        private bool SaberDamage(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Saber != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.SaberMorph)) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }
        private bool LeetCrit(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Leet != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.LeetMorph)) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }
        private bool DragonScales(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (MorphSelection.Dragon != (MorphSelection)_settings["MorphSelection"].AsInt32()) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.DragonMorph)) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        #endregion

        #region Buffs

        protected bool Ranged(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Ranged);
        }

        protected bool Melee(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Melee);
        }

        private bool Armor(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.DragonMorph)) { return false; }

            return NonComabtTeamBuff(spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Misc

        private static class RelevantNanos
        {
            public static int[] Heals = new[] { 223167, 252008, 252006, 136674, 136673, 143908, 82059, 136675, 136676, 82060, 136677,
                136678, 136679, 136682, 82061, 136681, 136680, 136683, 136684, 136685, 82062, 136686, 136689, 82063, 136688, 136687,
                82064, 26695 };

            public static readonly int[] ArmorBuffs = { 74173, 74174, 74175, 74176, 74177, 74178 };
            public static readonly int[] DragonMorph = { 217670, 25994 };
            public static readonly int[] LeetMorph = { 263278, 82834 };
            public static readonly int[] WolfMorph = { 275005, 85062 };
            public static readonly int[] SaberMorph = { 217680, 85070 };
            public static readonly int[] DragonScales = { 302217, 302214 };
            public static readonly int[] WolfAgility = { 302235, 302232 };
            public static readonly int[] LeetCrit = { 302229, 302226 };
            public static readonly int[] SaberDamage = { 302243, 302240 };

            public static int[] TargetArmorBuffs = { 74178, 74177, 74176, 74175, 74174, 74173 };
            public static readonly int[] TargetedDamageShields = { 55812, 55836, 55835, 55833, 55834, 55831, 55832, 55830, 55829, 55828, 55826, 55827,
                55825, 55824, 55823, 55821, 55822, 55819, 55820, 55816, 55818, 55817, 55814, 55815, 55813, 55837 };
            public const int LearningbyDoing = 263277;

            public static readonly int[] TeamRunSpeedBuffs = { 26705, 26237 };

        }

        public enum HealSelection
        {
            None, SingleTeam, SingleArea, Team
        }
        public enum MorphSelection
        {
            None, Dragon, Saber, Wolf, Leet
        }

        public enum TreatmentBuffSelection
        {
            None, Self, Team
        }

        public enum ProcType1Selection
        {
            AesirAbsorption = 1397705028, 
            MacheteFlurry = 1296254540, 
            SelfPreservation = 1145197381, 
            SkinProtection = 1397049667, 
            FerociousHits = 1464618305
        }

        public enum ProcType2Selection
        {
            HealingHerbs = 1212237890, 
            Combustion = 1112822866, 
            CharringBlow = 1296581199, 
            RestoreVigor = 1279608914, 
            MacheteSlice = 1296257868, 
            SoothingHerbs = 1398032450, 
            BasicDressing = 1347635282
        }

        #endregion
    }
}
