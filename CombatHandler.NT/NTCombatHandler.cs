using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using static SmokeLounge.AOtomation.Messaging.Messages.N3Messages.FollowTargetMessage;

namespace CombatHandler.NanoTechnician
{
    public class NTCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleRez = false;

        private static double _absorbs;

        private static Window _buffWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _debuffWindow;
        private static Window _calmingWindow;
        private static Window _nukeWindow;
        private static Window _perkWindow;

        private static View _procView;
        private static View _itemView;
        private static View _buffView;
        private static View _debuffView;
        private static View _calmView;
        private static View _nukeView;
        private static View _perkView;

        private static SimpleChar _drainTarget;

        private static double _ncuUpdateTime;

        public NTCombatHandler(string pluginDir) : base(pluginDir)
        {
            try
            {
                IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalRez, OnGlobalRezMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.ClearBuffs, OnClearBuffs);
                IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleAbsorbsDelayChangedEvent += CycleAbsorbsDelay_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].NanoAegisPercentageChangedEvent += NanoAegisPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].NullitySpherePercentageChangedEvent += NullitySpherePercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].IzgimmersWealthPercentageChangedEvent += IzgimmersWealthPercentage_Changed;
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

                _settings.AddVariable("Buffing", true);
                _settings.AddVariable("Composites", true);

                _settings.AddVariable("GlobalBuffing", true);
                _settings.AddVariable("GlobalComposites", true);
                _settings.AddVariable("GlobalRez", true);

                _settings.AddVariable("DeTaunt", false);

                _settings.AddVariable("Illusionist", false);
                _settings.AddVariable("NotumGraft", false);
                _settings.AddVariable("SharpObjects", true);
                _settings.AddVariable("Grenades", true);

                _settings.AddVariable("StimTargetSelection", (int)StimTargetSelection.Self);

                _settings.AddVariable("Kits", true);

                _settings.AddVariable("CycleAbsorbs", false);

                _settings.AddVariable("DOTA", (int)DOTADebuffTargetSelection.None);

                _settings.AddVariable("Pierce", false);
                _settings.AddVariable("FlimFocus", false);

                _settings.AddVariable("NanoHOTTeam", false);
                _settings.AddVariable("CostTeam", false);
                _settings.AddVariable("AbsorbACBuff", false);
                _settings.AddVariable("Evades", false);
                _settings.AddVariable("NFRangeBuff", false);

                _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.ThermalReprieve);
                _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.OptimizedLibrary);

                _settings.AddVariable("BlindSelection", (int)BlindSelection.None);
                _settings.AddVariable("HaloSelection", (int)HaloSelection.None);
                _settings.AddVariable("LickOfThePestSelection", (int)LickOfThePestSelection.None);
                _settings.AddVariable("NanoResistSelection", (int)NanoResistSelection.None);
                _settings.AddVariable("HackedBlindSelection", (int)HackedBlindSelection.None);

                _settings.AddVariable("CalmingSelection", (int)CalmingSelection.Calm);
                _settings.AddVariable("ModeSelection", (int)ModeSelection.None);

                _settings.AddVariable("AOESelection", (int)AOESelection.None);

                RegisterSettingsWindow("Nano-Technician Handler", "NTSettingsView.xml");

                //DeTaunt
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DeTaunt).OrderByStackingOrder(), DeTaunt, CombatActionPriority.High);

                //Calms
                RegisterSpellProcessor(RelevantNanos.Stun, Stun, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.Calm, Calm, CombatActionPriority.High);

                //Debuffs
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AAODebuffs).OrderByStackingOrder(), SingleBlind, CombatActionPriority.High);

                RegisterSpellProcessor(RelevantNanos.AOEBlinds, AOEBlind, CombatActionPriority.High);

                RegisterSpellProcessor(RelevantNanos.HaloNanoDebuff, HaloNanoDebuff, CombatActionPriority.High);

                RegisterSpellProcessor(RelevantNanos.LickofthePest,
                   (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "LickOfThePestSelection"), CombatActionPriority.High);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceDebuff_LineA).OrderByStackingOrder(),
                   (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "NanoResistSelection"), CombatActionPriority.High);

                RegisterSpellProcessor(RelevantNanos.HackedBlind,
                   (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "HackedBlindSelection"), CombatActionPriority.High);

                //Dots
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTNanotechnicianStrainA).OrderByStackingOrder(), DOTADebuffTarget, CombatActionPriority.High);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTNanotechnicianStrainB).OrderByStackingOrder(), PierceNuke, CombatActionPriority.Medium);

                //Nukes
                //RegisterSpellProcessor(RelevantNanos.Garuk, SingleTargetNuke);
                //RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke);

                RegisterSpellProcessor(RelevantNanos.RKAOENukes,
                    (Spell aoeNuke, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => AOENuke(aoeNuke, fightingTarget, ref actionTarget, AOESelection.RK));

                RegisterSpellProcessor(RelevantNanos.SLAOENukes,
                     (Spell aoeNuke, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => AOENuke(aoeNuke, fightingTarget, ref actionTarget, AOESelection.SL));

                RegisterSpellProcessor(new[] { RelevantNanos.VolcanicEruption },
                     (Spell aoeNuke, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => AOENuke(aoeNuke, fightingTarget, ref actionTarget, AOESelection.VE));

                //Perks
                RegisterPerkProcessor(PerkHash.FlimFocus, FlimFocus, CombatActionPriority.High);

                //Buffs
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NullitySphereNano).OrderByStackingOrder(), NullitySphere, CombatActionPriority.Medium);
                RegisterSpellProcessor(RelevantNanos.NanobotAegis, NanobotAegis, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.IzgimmersWealth, IzgimmersWealth, CombatActionPriority.High);


                RegisterSpellProcessor(RelevantNanos.NanobotShelter,
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Psy_IntBuff).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDamageMultiplierBuffs).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NFRangeBuff).OrderByStackingOrder(), NFRangeBuff);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MatCreaBuff).OrderByStackingOrder(), MatCreaBuff);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Fortify).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoOverTime_LineA).OrderByStackingOrder(), NanoHOT);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NPCostBuff).OrderByStackingOrder(), Cost);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AbsorbACBuff).OrderByStackingOrder(), CycleAbsorbs);

                //Team Buffs
                RegisterSpellProcessor(RelevantNanos.AbsorbACBuff,
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                        => NonComabtTeamBuff(spell, fightingTarget, ref actionTarget, "AbsorbACBuff"));
                RegisterSpellProcessor(RelevantNanos.DarkMovement, Evades);

                //Items
                RegisterItemProcessor(new int[] { RelevantItems.NotumGraft, RelevantItems.NotumSplice }, NotumItem);
                RegisterItemProcessor(RelevantItems.WilloftheIllusionist, RelevantItems.WilloftheIllusionist, Illusionist);

                //LE Procs
                RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianThermalReprieve, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianHarvestEnergy, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianLayeredAmnesty, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianSourceTap, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianCircularLogic, LEProc1, CombatActionPriority.Low);

                RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianOptimizedLibrary, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianAcceleratedReality, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianLoopingService, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianPoweredNanoFortress, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianIncreaseMomentum, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcNanoTechnicianUnstableLibrary, LEProc2, CombatActionPriority.Low);

                PluginDirectory = pluginDir;

                CycleAbsorbsDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].CycleAbsorbsDelay;
                NanoAegisPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].NanoAegisPercentage;
                NullitySpherePercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].NullitySpherePercentage;
                IzgimmersWealthPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].IzgimmersWealthPercentage;
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
        public Window[] _windows => new Window[] { _calmingWindow, _buffWindow, _procWindow, _debuffWindow, _nukeWindow, _itemWindow, _perkWindow };

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

        private void HandleDebuffsViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_debuffView)) { return; }

                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\NTDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "NTDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "NTDebuffsView" }, _debuffView, out var container);
                _debuffWindow = container;
            }
        }

        private void HandleCalmingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_calmView)) { return; }

                _calmView = View.CreateFromXml(PluginDirectory + "\\UI\\NTCalmingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Calming", XmlViewName = "NTCalmingView" }, _calmView);
            }
            else if (_calmingWindow == null || (_calmingWindow != null && !_calmingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_calmingWindow, PluginDir, new WindowOptions() { Name = "Calming", XmlViewName = "NTCalmingView" }, _calmView, out var container);
                _calmingWindow = container;
            }
        }

        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_perkView)) { return; }

                _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\NTPerksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Perks", XmlViewName = "NTPerksView" }, _perkView);

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
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "NTPerksView" }, _perkView, out var container);
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
        private void HandleNukesViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_nukeView)) { return; }

                _nukeView = View.CreateFromXml(PluginDirectory + "\\UI\\NTNukesView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Nukes", XmlViewName = "NTNukesView" }, _nukeView);
            }
            else if (_nukeWindow == null || (_nukeWindow != null && !_nukeWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_nukeWindow, PluginDir, new WindowOptions() { Name = "Nukes", XmlViewName = "NTNukesView" }, _nukeView, out var container);
                _nukeWindow = container;
            }
        }
        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_buffView)) { return; }

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\NTBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "NTBuffsView" }, _buffView);

                window.FindView("AbsorbsDelayBox", out TextInputView absorbsInput);
                window.FindView("NanoAegisPercentageBox", out TextInputView nanoAegisInput);
                window.FindView("NullitySpherePercentageBox", out TextInputView nullSphereInput);
                window.FindView("IzgimmersWealthPercentageBox", out TextInputView izWealthInput);

                if (absorbsInput != null)
                    absorbsInput.Text = $"{CycleAbsorbsDelay}";

                if (nanoAegisInput != null)
                    nanoAegisInput.Text = $"{NanoAegisPercentage}";

                if (nullSphereInput != null)
                    nullSphereInput.Text = $"{NullitySpherePercentage}";

                if (izWealthInput != null)
                    izWealthInput.Text = $"{IzgimmersWealthPercentage}";
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "NTBuffsView" }, _buffView, out var container);
                _buffWindow = container;

                container.FindView("AbsorbsDelayBox", out TextInputView absorbsInput);
                container.FindView("NanoAegisPercentageBox", out TextInputView nanoAegisInput);
                container.FindView("NullitySpherePercentageBox", out TextInputView nullSphereInput);
                container.FindView("IzgimmersWealthPercentageBox", out TextInputView izWealthInput);

                if (absorbsInput != null)
                    absorbsInput.Text = $"{CycleAbsorbsDelay}";

                if (nanoAegisInput != null)
                    nanoAegisInput.Text = $"{NanoAegisPercentage}";

                if (nullSphereInput != null)
                    nullSphereInput.Text = $"{NullitySpherePercentage}";

                if (izWealthInput != null)
                    izWealthInput.Text = $"{IzgimmersWealthPercentage}";
            }
        }
        private void HandleItemViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                if (window.Views.Contains(_itemView)) { return; }

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\NTItemsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "NTItemsView" }, _itemView);

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
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "NTItemsView" }, _itemView, out var container);
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

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\NTProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "NTProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "NTProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            try
            {
                if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 3.0)
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
                    window.FindView("AbsorbsDelayBox", out TextInputView absorbsInput);
                    window.FindView("NanoAegisPercentageBox", out TextInputView nanoAegisInput);
                    window.FindView("NullitySpherePercentageBox", out TextInputView nullSphereInput);
                    window.FindView("IzgimmersWealthPercentageBox", out TextInputView izWealthInput);
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

                    if (absorbsInput != null && !string.IsNullOrEmpty(absorbsInput.Text))
                        if (int.TryParse(absorbsInput.Text, out int absorbsValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleAbsorbsDelay != absorbsValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleAbsorbsDelay = absorbsValue;

                    if (nanoAegisInput != null && !string.IsNullOrEmpty(nanoAegisInput.Text))
                        if (int.TryParse(nanoAegisInput.Text, out int nanoAegisValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].NanoAegisPercentage != nanoAegisValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].NanoAegisPercentage = nanoAegisValue;

                    if (nullSphereInput != null && !string.IsNullOrEmpty(nullSphereInput.Text))
                        if (int.TryParse(nullSphereInput.Text, out int nullSphereValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].NullitySpherePercentage != nullSphereValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].NullitySpherePercentage = nullSphereValue;

                    if (izWealthInput != null && !string.IsNullOrEmpty(izWealthInput.Text))
                        if (int.TryParse(izWealthInput.Text, out int izWealthValue))
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].IzgimmersWealthPercentage != izWealthValue)
                                Config.CharSettings[DynelManager.LocalPlayer.Name].IzgimmersWealthPercentage = izWealthValue;
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

                    if (SettingsController.settingsWindow.FindView("NukesView", out Button nukeView))
                    {
                        nukeView.Tag = SettingsController.settingsWindow;
                        nukeView.Clicked = HandleNukesViewClick;
                    }

                    if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                    {
                        buffView.Tag = SettingsController.settingsWindow;
                        buffView.Clicked = HandleBuffViewClick;
                    }

                    if (SettingsController.settingsWindow.FindView("DebuffsView", out Button debuffView))
                    {
                        debuffView.Tag = SettingsController.settingsWindow;
                        debuffView.Clicked = HandleDebuffsViewClick;
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

        #region Nukes

        private bool AOENuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, AOESelection aoeType)
        {
            var aoeSelection = (AOESelection)_settings["AOESelection"].AsInt32();

            if (fightingTarget == null || !CanCast(spell) || aoeSelection != aoeType)
                return false;

            return true;
        }

        private bool PierceNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Pierce") || fightingTarget == null || !CanCast(spell)) { return false; }

            return true;
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            if (AOESelection.None != (AOESelection)_settings["AOESelection"].AsInt32()
                    || fightingTarget == null) { return false; }

            return true;
        }

        private bool DOTADebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.NanoPercent < 40) { return false; }

            if (DOTADebuffTargetSelection.None == (DOTADebuffTargetSelection)_settings["DOTA"].AsInt32()) { return false; }

            if (DOTADebuffTargetSelection.Target == (DOTADebuffTargetSelection)_settings["DOTA"].AsInt32()
                && fightingTarget != null)
            {
                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (DOTADebuffTargetSelection.Boss == (DOTADebuffTargetSelection)_settings["DOTA"].AsInt32()
                 && fightingTarget != null)
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;

        }

        #endregion

        #region Calms

        private bool Stun(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (CalmingSelection.Stun != (CalmingSelection)_settings["CalmingSelection"].AsInt32()) { return false; }

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
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
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

        private bool Calm(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (CalmingSelection.Calm != (CalmingSelection)_settings["CalmingSelection"].AsInt32()) { return false; }

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
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
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

        #region Blinds

        private bool AOEBlind(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BlindSelection.AOE != (BlindSelection)_settings["BlindSelection"].AsInt32()
                || fightingTarget == null || !CanCast(spell)) { return false; }

            return !fightingTarget.Buffs.Contains(NanoLine.AAODebuffs);
        }

        private bool SingleBlind(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (BlindSelection.Target != (BlindSelection)_settings["BlindSelection"].AsInt32()
                || fightingTarget == null || !CanCast(spell)) { return false; }

            return !fightingTarget.Buffs.Contains(NanoLine.AAODebuffs);
        }

        #endregion

        #region Debuffs

        private bool HaloNanoDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HaloSelection.None == (HaloSelection)_settings["HaloSelection"].AsInt32()) { return false; }

            if (fightingTarget != null && fightingTarget.Buffs.Contains(NanoLine.HaloNanoDebuff)) { return false; }

            if (HaloSelection.Target != (HaloSelection)_settings["HaloSelection"].AsInt32()
                || fightingTarget == null || !CanCast(spell)) { return false; }

            if (HaloSelection.Boss != (HaloSelection)_settings["HaloSelection"].AsInt32())
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

            return true;

        }

        #endregion

        #region Perks

        private bool FlimFocus(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("FlimFocus")) { return false; }

            return CyclePerks(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region DeTaunt

        private bool DeTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell) || !IsSettingEnabled("DeTaunt")) { return false; }
            if (!Team.IsInTeam) { return false; }

            SimpleChar target = DynelManager.NPCs
                .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name)
                    && c.FightingTarget != null
                    && c.Health > 0
                    && c.IsInLineOfSight
                    && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                    && SpellChecksOther(spell, spell.Nanoline, c))
                .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = target;
                return true;
            }

            return false;
        }

        #endregion

        #region Buffs

        private bool MatCreaBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //if (RelevantIgnoreNanos.CompNanoSkills.Contains(spell.Id)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantIgnoreNanos.CompNanoSkills)) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        private bool NanoHOT(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("NanoHOTTeam"))
                return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        private bool Cost(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("CostTeam"))
                return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        private bool NanobotAegis(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            return DynelManager.LocalPlayer.HealthPercent <= NanoAegisPercentage && !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.NullitySphereNano);
        }

        private bool NullitySphere(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            return DynelManager.LocalPlayer.HealthPercent <= NullitySpherePercentage && !DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.NanobotAegis);
        }

        private bool CycleAbsorbs(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Any(Buff => Buff.Id == RelevantNanos.BioCocoon)) { return false; }

            if (IsSettingEnabled("CycleAbsorbs") && Time.NormalTime > _absorbs + CycleAbsorbsDelay
                && (fightingTarget != null || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 0))
            {
                if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

                _absorbs = Time.NormalTime;
                return true;
            }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        private bool IzgimmersWealth(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || fightingTarget == null || !CanCast(spell)) { return false; }

            return DynelManager.LocalPlayer.NanoPercent <= IzgimmersWealthPercentage;
        }

        #endregion

        #region Team Buffs

        //private bool AbsorbACBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (IsSettingEnabled("AbsorbACBuff"))
        //        return GenericTeamBuff(spell, ref actionTarget);

        //    return false;
        //}

        private bool Evades(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (IsSettingEnabled("Evades"))
                return NonComabtTeamBuff(spell, fightingTarget, ref actionTarget);

            return false;
        }

        private bool NFRangeBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("NFRangeBuff"))
                return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        #endregion

        #region Items

        private bool NotumItem(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("NotumGraft")) { return false; }
            if (Item.HasPendingUse || DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.MaxNanoEnergy)
            || DynelManager.LocalPlayer.NanoPercent >= 75) { return false; }

            return item != null;
        }

        private bool Illusionist(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Illusionist")) { return false; }
            if (fightingtarget == null) { return false; }
            if (fightingtarget?.MaxHealth < 1000000) { return false; }
            if (DynelManager.LocalPlayer.Buffs.Contains(274736) || Item.HasPendingUse) { return false; }

            return item != null;
        }


        #endregion

        #region Misc

        public enum ProcType1Selection
        {
            ThermalReprieve = 1347900245,
            HarvestEnergy = 1128877135,
            LayeredAmnesty = 1397248588,
            SourceTap = 1364218711,
            CircularLogic = 1346851668
        }

        public enum ProcType2Selection
        {
            OptimizedLibrary = 1330465858,
            AcceleratedReality = 1163086928,
            LoopingService = 1431522377,
            PoweredNanoFortress = 1414677832,
            IncreaseMomentum = 1229147471,
            UnstableLibrary = 1146311237
        }

        public enum BlindSelection
        {
            None, Target, AOE
        }
        public enum HaloSelection
        {
            None, Target, Area, Boss
        }
        public enum LickOfThePestSelection
        {
            None, Target, Area, Boss
        }
        public enum NanoResistSelection
        {
            None, Target, Area, Boss
        }
        public enum HackedBlindSelection
        {
            None, Target, Area, Boss
        }
        public enum AOESelection
        {
            None, RK, SL, VE
        }

        public enum DOTADebuffTargetSelection
        {
            None, Target, Boss
        }

        public enum CalmingSelection
        {
            Stun, Calm
        }

        public enum ModeSelection
        {
            None, All, Adds
        }

        private static class RelevantNanos
        {
            public const int NanobotAegis = 302074;
            public const int IzgimmersWealth = 275024;
            public const int IzgimmersUltimatum = 218168;
            public const int Garuk = 275692;
            public const int PierceReflect = 266287;
            
            public const int DarkMovement = 28603;
            public const int BioCocoon = 209802;

            public static readonly int[] RKAOENukes = { 28620, 28638, 28637, 28594, 45922, 45906, 45884, 28635, 28593, 45925, 45940, 
            45900, 28629, 45917, 45937, 28599, 45894, 45943, 28633, 28631};
            public static readonly int[] SLAOENukes = { 266293, 266294, 266295, 266296, 266297, 266298 };
            public const int VolcanicEruption = 28638;

            public static readonly int[] HackedBlind = { 253384, 253382, 253380 };
            public static readonly int[] HaloNanoDebuff = { 45239, 45238, 45224 };
            public const int Stun = 28625;
            public static readonly int[] Calm = { 259364, 259365, 259362, 259363, 259366, 259367, 100443, 100441, 259335, 259336, 100442, 100440 };
            public static int LickofthePest = 201937;


            public const int SuperiorFleetingImmunity = 273386;
            public static readonly int[] AbsorbACBuff = { 270356, 117676, 117675, 117677, 117678, 117679 };
            public static readonly int[] AOEBlinds = { 83959, 83960, 83961, 83962, 83963, 83964 };
            public static readonly int[] SingleTargetNukes = { 218168, 218164, 218162, 218160, 218158, 218156, 218154, 218152, 218150,
                218148, 218146, 218144, 218142, 218140, 218138, 218136, 269473, 218134, 201935, 202262, 201933, 218132, 28618, 218124, 218130,
                218122, 218120, 218128, 218118, 218126, 45226, 45192, 28619, 45230, 28623, 28604, 28616, 218116, 28597, 45210, 45236, 45197,
                45233, 45247, 45199, 45235, 45234, 218114, 45258, 45217, 28600, 45198, 28613, 45919, 45195, 45225, 45260, 45891, 45254, 45890,
                45213, 218112, 45215, 45915, 218104, 45252, 45214, 45251, 45929, 45220, 45920, 45222, 218102, 28598, 45911, 45237, 45216,
                218110, 45913, 45901, 45212, 45206, 45912, 45883, 45245, 45140, 45904, 45218, 28626, 218108, 45261, 218100, 45909, 45203,
                45228, 45903, 45200, 45939, 28592, 45242, 218098, 218106, 45885, 45926, 45241, 44538, 45908, 45250, 45934, 45138, 45932,
                28632, 45205, 28609, 45209, 45246, 45935, 45921, 45227, 45207, 45942, 45191, 45924, 218096, 28610, 45914, 45208, 45893,
                28621, 45211, 45916, 45933, 218094, 45240, 45259, 45941, 45910, 45253, 28614, 218092, 45221, 45204, 28634, 45196, 45886,
                45201, 45928, 45193, 45323, 45244, 45889, 45895, 28605, 45219, 45223, 45938, 28628, 45232, 45248, 45898, 45202, 45923,
                45229, 45907, 45139, 45887, 45231, 45882, 28627, 45936, 45194, 28639, 45243, 45931, 28630, 45137, 28607, 45257, 45880,
                45256, 45249, 45888, 45255, 45881, 42543, 45927, 45902, 42540, 42541, 45899, 45905, 28611, 45897, 28601, 42542, 28608,
                45918, 42539, 45892, 45930, 45879, 45896, 28612 };
            public static readonly int[] NanobotShelter = { 273388, 263265 };
            public static readonly int CompositeAttribute = 223372;
            public static readonly int CompositeNano = 223380;


        }

        private static class RelevantIgnoreNanos
        {
            public static int[] CompNanoSkills = new[] { 220331, 220333, 220335, 220337, 292299, 220339, 220341, 220343 };

        }

        private static class RelevantItems
        {
            public const int NotumSplice = 204649;
            public const int NotumGraft = 305513;

            public const int WilloftheIllusionist = 274717;

        }

        #endregion
    }
}
