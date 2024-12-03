using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Linq;
using System.Xml.Linq;

namespace CombatHandler.Agent
{
    public class AgentCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleRez = false;

        public static int[] FPSwitchSetting;
        public static int[] MorhpSpellArray;
        public static bool _syncPets;
        private double _lastHealPetHealTime = 0.0;

        private static Window _buffWindow;
        private static Window _debuffWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _falseProfWindow;
        private static Window _healingWindow;
        private static Window _perkWindow;
        private static Window _mpWindow;
        private static Window _soldWindow;
        private static Window _enfoWindow;
        private static Window _engieWindow;
        private static Window _docWindow;
        private static Window _fixerWindow;
        private static Window _cratWindow;
        private static Window _maWindow;
        private static Window _ntWindow;
        private static Window _tradeWindow;
        private static Window _advyWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _procView;
        private static View _itemView;
        private static View _falseProfView;
        private static View _healingView;
        private static View _perkView;
        private static View _mpView;
        private static View _soldView;
        private static View _enfoView;
        private static View _engieView;
        private static View _docView;
        private static View _fixerView;
        private static View _cratView;
        private static View _maView;
        private static View _ntView;
        private static View _tradeView;
        private static View _advyView;
        private static Window _morphWindow;
        private static View _morphView;
        private static double _ncuUpdateTime;
        private static double _mongo;
        private static double _challenger;
        private static double _singleTaunt;

        int check;

        private static SimpleChar _drainTarget;

        int petColor;

        public AgentCombatHandler(string pluginDir) : base(pluginDir)
        {
            try
            {
                PluginDirectory = pluginDir;

                IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalRez, OnGlobalRezMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.ClearBuffs, OnClearBuffs);
                IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetSyncOn, SyncPetsOnMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetSyncOff, SyncPetsOffMessage);

                Config.CharSettings[DynelManager.LocalPlayer.Name].TargetHealPercentageChangedEvent += TargetHealPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentageChangedEvent += CompleteHealPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPercentageChangedEvent += TeamHealPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteTeamHealPercentageChangedEvent += CompleteTeamHealPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].AMSPercentageChangedEvent += AMSPercentage_Changed;
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
                Config.CharSettings[DynelManager.LocalPlayer.Name].MongoDelayChangedEvent += MongoDelay_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleChallengerDelayChangedEvent += CycleChallengerDelay_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelayChangedEvent += SingleTauntDelay_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].NullitySpherePercentageChangedEvent += NullitySpherePercentage_Changed;

                _settings.AddVariable("AllPlayers", false);
                _settings["AllPlayers"] = false;

                _settings.AddVariable("Buffing", true);
                _settings.AddVariable("Composites", true);
                _settings.AddVariable("GlobalBuffing", true);
                _settings.AddVariable("GlobalComposites", true);
                _settings.AddVariable("GlobalRez", true);

                _settings.AddVariable("EncaseInStone", false);

                _settings.AddVariable("SharpObjects", true);
                _settings.AddVariable("Grenades", true);

                _settings.AddVariable("TauntTool", false);

                _settings.AddVariable("StimTargetSelection", 1);
                _settings.AddVariable("Kits", true);

                _settings.AddVariable("AAO", false); //used for CritTeam

                _settings.AddVariable("DOTASelection", 0);

                _settings.AddVariable("EvasionDebuffSelection", 0);

                _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.GrimReaper);
                _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.LaserAim);

                _settings.AddVariable("SLMap", false);

                _settings.AddVariable("ProcSelection", 1);
                _settings.AddVariable("AgentDamageBuff", 0);

                _settings.AddVariable("Concentration", false);

                _settings.AddVariable("MASelection", 267525);
                _settings.AddVariable("IntelligenceSelection", 0);

                _settings.AddVariable("TheShot", false);

                _settings.AddVariable("FalseProfSelection", 0);
                _settings.AddVariable("Root", false);

                //trader
                _settings.AddVariable("RKNanoDrainSelection", 0);
                _settings.AddVariable("ACDrainSelection", 0);
                _settings.AddVariable("DepriveSelection", 1);
                _settings.AddVariable("RansackSelection", 1);
                _settings.AddVariable("TdrEvades", 0);
                _settings.AddVariable("TraderModeSelection", 0);

                //mp
                _settings.AddVariable("CostBuffSelection", 1);
                _settings.AddVariable("SpawnPets", true);
                _settings.AddVariable("BuffPets", true);
                _settings.AddVariable("WarpPets", false);
                _settings.AddVariable("Nukes", false);
                _settings.AddVariable("NormalNuke", false);
                _settings.AddVariable("DebuffNuke", false);
                _settings.AddVariable("SyncPets", true);

                //doc
                _settings.AddVariable("LockCH", false);
                _settings.AddVariable("TeamHealSelection", 1);
                _settings.AddVariable("PistolTeam", true);
                _settings.AddVariable("NanoResistSelection", 0);
                _settings.AddVariable("HealDeltaBuffSelection", 0);
                _settings.AddVariable("ShortHpSelection", 0);
                _settings.AddVariable("ShortHOT", false);
                _settings.AddVariable("EpsilonPurge", false);
                _settings.AddVariable("InitBuffSelection", 2);
                _settings.AddVariable("StrengthBuffSelection", 1);
                _settings.AddVariable("InitDebuffSelection", 0);

                //Adv
                _settings.AddVariable("MorphSelection", 0);
                _settings.AddVariable("CatDamage", false);
                _settings.AddVariable("AdvArmor", 0);
                _settings.AddVariable("AdvDmgShield", 0);

                //enf
                _settings.AddVariable("MongoSelection", 2);
                _settings.AddVariable("CycleChallenger", false);
                _settings.AddVariable("SingleTauntsSelection", 2);
                _settings.AddVariable("TargetedHpBuff", true);
                _settings.AddVariable("OSTMongo", false);
                _settings.AddVariable("EnfDmgShield", 0);

                //nt
                _settings.AddVariable("AOESelection", 0);
                _settings.AddVariable("NTCost", 0);
                _settings.AddVariable("NFRangeBuff", 0);
                _settings.AddVariable("NanoHOTTeam", 0);
                _settings.AddVariable("BlindSelection", 0);
                _settings.AddVariable("NTEvades", 0);
                _settings.AddVariable("NTFortify", 0);
                _settings.AddVariable("HaloSelection", 0);

                //crat
                _settings.AddVariable("CalmingSelection", 0);
                _settings.AddVariable("CratModeSelection", 0);
                _settings.AddVariable("IntensifyStressSelection", 0);
                _settings.AddVariable("CratNuke", false);
                _settings.AddVariable("CratSpecialNuke", false);
                _settings.AddVariable("CutRedTape", 0);
                _settings.AddVariable("XPBonus", false);

                //fixer
                _settings.AddVariable("AOESnare", false);

                //soldier
                _settings.AddVariable("RiotControl", false);
                _settings.AddVariable("InitBuff", false);
                _settings.AddVariable("RKReflectSelection", 0);

                //MA
                _settings.AddVariable("RunSpeed", 0);
                _settings.AddVariable("BrawlBuff", 0);
                _settings.AddVariable("MABuff", 0);
                _settings.AddVariable("MAEvades", false);
                _settings.AddVariable("ControlledDestructionWithShutdown", false);
                _settings.AddVariable("DamageTypeSelection", 0);

                RegisterSettingsWindow("Agent Handler", "AgentSettingsView.xml");

                //Healing
                RegisterSpellProcessor(RelevantGenericNanos.FountainOfLife, Healing.FountainOfLife, CombatActionPriority.High);

                //Perk
                RegisterPerkProcessor(PerkHash.TheShot, TheShot);

                //Root/snares
                RegisterSpellProcessor(RelevantNanos.SuperiorHoldVictim, Root, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.GreaterDelayPursuers, AOESnare, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.GreaterDelayTheInevitable, Snare, CombatActionPriority.High);

                //Buffs
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AgilityBuff).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AimedShotBuffs).OrderByStackingOrder(), Aimed);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ConcealmentBuff).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalIncreaseBuff).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ExecutionerBuff).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AgentProcBuff).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RifleBuffs).OrderByStackingOrder(), Rifle);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ConcentrationCriticalLine).OrderByStackingOrder(), Concentration, CombatActionPriority.Medium);
                RegisterSpellProcessor(RelevantNanos.DetauntProcs, DetauntProc);
                RegisterSpellProcessor(RelevantNanos.DOTProcs, DamageProc);

                //Team Buffs

                RegisterSpellProcessor(RelevantNanos.AgentDamageBuffs,
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "AgentDamageBuff"));

                RegisterSpellProcessor(RelevantNanos.TeamCritBuffs, AAO);

                //Debuffs
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTAgentStrainA).OrderByStackingOrder(),
                    (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "DOTASelection"), CombatActionPriority.Low);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EvasionDebuffs_Agent).OrderByStackingOrder(),
                   (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "EvasionDebuffSelection"), CombatActionPriority.Low);
                //Items
                int intelligenceItem = _settings["IntelligenceSelection"].AsInt32();
                int maItem = _settings["MASelection"].AsInt32();
                if (maItem == 204329)
                {
                    foreach (var item in Inventory.FindAll("Bird of Prey").OrderBy(x => x.QualityLevel))
                    {

                        RegisterItemProcessor(item.LowId, item.HighId, MAItem);
                    }
                }
                else
                {
                    RegisterItemProcessor(maItem, maItem, MAItem);
                }
                RegisterItemProcessor(intelligenceItem, intelligenceItem, IntelligenceItem);

                //LE Procs
                //type1
                RegisterPerkProcessor(PerkHash.LEProcAgentGrimReaper, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcAgentDisableCuffs, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcAgentNoEscape, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcAgentIntenseMetabolism, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcAgentMinorNanobotEnhance, LEProc1, CombatActionPriority.Low);

                //type2
                RegisterPerkProcessor(PerkHash.LEProcAgentNotumChargedRounds, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcAgentLaserAim, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcAgentNanoEnhancedTargeting, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcAgentPlasteelPiercingRounds, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcAgentCellKiller, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcAgentImprovedFocus, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcAgentBrokenAnkle, LEProc2, CombatActionPriority.Low);

                Healing.TargetHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].TargetHealPercentage;
                Healing.CompleteHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentage;
                Healing.FountainOfLifeHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentage;
                Healing.TeamHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPercentage;
                Healing.CompleteTeamHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteTeamHealPercentage;

                StimTargetName = Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetName;
                StimHealthPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentage;
                StimNanoPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentage;
                KitHealthPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage;
                KitNanoPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage;
                AMSPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].AMSPercentage;
                CycleSpherePerkDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelay;
                CycleWitOfTheAtroxPerkDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelay;
                SelfHealPerkPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentage;
                SelfNanoPerkPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentage;
                TeamHealPerkPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentage;
                TeamNanoPerkPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentage;
                BodyDevAbsorbsItemPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentage;
                StrengthAbsorbsItemPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentage;
                MongoDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].MongoDelay;
                CycleChallengerDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].CycleChallengerDelay;
                SingleTauntDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelay;
                NullitySpherePercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].NullitySpherePercentage;

                Chat.RegisterCommand("petstats", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    foreach (var pet in DynelManager.LocalPlayer.Pets)
                    {
                        switch (pet.Type)
                        {
                            case PetType.Attack:
                                petColor = (int)ChatColor.Red;
                                break;
                            case PetType.Heal:
                                petColor = (int)ChatColor.LightBlue;
                                break;
                            case PetType.Support:
                                petColor = (int)ChatColor.Green;
                                break;
                            case PetType.Social:
                                petColor = (int)ChatColor.Yellow;
                                break;
                            default:
                                petColor = (int)ChatColor.White;
                                break;
                        }

                        var petassimplechar = pet.Character;

                        Chat.WriteLine($"{petassimplechar.Name} lvl {petassimplechar.Level} type {pet.Type}", (ChatColor)petColor);
                        Chat.WriteLine($"AddAllOff = {petassimplechar.GetStat(Stat.AddAllOff)}", (ChatColor)petColor);
                        Chat.WriteLine($"AddAllDef = {petassimplechar.GetStat(Stat.AddAllDef)}", (ChatColor)petColor);
                        Chat.WriteLine($"Aggressiveness = {petassimplechar.GetStat(Stat.Aggressiveness)}", (ChatColor)petColor);
                        Chat.WriteLine($"AggDef = {petassimplechar.GetStat(Stat.AggDef)}", (ChatColor)petColor);
                        Chat.WriteLine($"NPCType = {petassimplechar.GetStat(Stat.NPCFamily)}", (ChatColor)petColor);
                    }
                });

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

        public Window[] _windows => new Window[] { _buffWindow, _debuffWindow, _healingWindow, _procWindow, _itemWindow, _perkWindow, _falseProfWindow, _advyWindow, _cratWindow, _docWindow, _enfoWindow,
            _engieWindow, _fixerWindow, _maWindow, _mpWindow, _ntWindow, _soldWindow, _tradeWindow};

        #region Callbacks
        private void syncPetsOnEnabled()
        {
            _syncPets = true;
        }
        private void syncPetsOffDisabled()
        {
            _syncPets = false;
        }
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
        private void SyncPetsOnMessage(int sender, IPCMessage msg)
        {
            _settings["SyncPets"] = true;
            syncPetsOnEnabled();
        }

        private void SyncPetsOffMessage(int sender, IPCMessage msg)
        {
            _settings["SyncPets"] = false;
            syncPetsOffDisabled();
        }
        #endregion

        #region Handles
        private void HandleItemViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_itemView)) { return; }

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\AgentItemsView.xml");

                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "AgentItemsView" }, _itemView);

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
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "AgentItemsView" }, _itemView, out var container);
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
        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_perkView)) { return; }

                _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\AgentPerksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Perks", XmlViewName = "AgentPerksView" }, _perkView);

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
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "AgentPerksView" }, _perkView, out var container);
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
        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\AgentProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "AgentProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "AgentProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }
        private void HandleFalseProfViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_falseProfView)) { return; }

                _falseProfView = View.CreateFromXml(PluginDirectory + "\\UI\\AgentFalseProfsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "False Professions", XmlViewName = "AgentFalseProfsView" }, _falseProfView);
            }
            else if (_falseProfWindow == null || (_falseProfWindow != null && !_falseProfWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_falseProfWindow, PluginDir, new WindowOptions() { Name = "False Professions", XmlViewName = "AgentFalseProfsView" }, _falseProfView, out var container);
                _falseProfWindow = container;
            }
        }

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_buffView)) { return; }

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\AgentBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "AgentBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "AgentBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }
        private void HandleDebuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\AgentDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "AgentDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "AgentDebuffsView" }, _debuffView, out var container);
                _debuffWindow = container;
            }
        }
        private void HandleHealingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_healingView)) { return; }

                _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\AgentHealingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Healing", XmlViewName = "AgentHealingView" }, _healingView);

                window.FindView("TargetHealPercentageBox", out TextInputView TargetHealInput);
                window.FindView("CompleteHealPercentageBox", out TextInputView CompleteHealInput);
                window.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);
                window.FindView("TeamHealPercentageBox", out TextInputView TeamHealInput);
                window.FindView("CompleteTeamHealPercentageBox", out TextInputView CompleteTeamHealInput);

                if (TargetHealInput != null)
                {
                    TargetHealInput.Text = $"{Healing.TargetHealPercentage}";
                }

                if (CompleteHealInput != null)
                {
                    CompleteHealInput.Text = $"{Healing.CompleteHealPercentage}";
                }

                if (FountainOfLifeInput != null)
                {
                    FountainOfLifeInput.Text = $"{Healing.FountainOfLifeHealPercentage}";
                }
                if (TeamHealInput != null)
                {
                    TeamHealInput.Text = $"{Healing.TeamHealPercentage}";
                }
                if (CompleteTeamHealInput != null)
                {
                    CompleteTeamHealInput.Text = $"{Healing.CompleteTeamHealPercentage}";
                }
            }
            else if (_healingWindow == null || (_healingWindow != null && !_healingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_healingWindow, PluginDir, new WindowOptions() { Name = "Healing", XmlViewName = "AgentHealingView" }, _healingView, out var container);
                _healingWindow = container;

                container.FindView("TargetHealPercentageBox", out TextInputView TargetHealInput);
                container.FindView("CompleteHealPercentageBox", out TextInputView CompleteHealInput);
                container.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);
                container.FindView("TeamHealPercentageBox", out TextInputView TeamHealInput);
                container.FindView("CompleteTeamHealPercentageBox", out TextInputView CompleteTeamHealInput);

                if (TargetHealInput != null)
                {
                    TargetHealInput.Text = $"{Healing.TargetHealPercentage}";
                }

                if (CompleteHealInput != null)
                {
                    CompleteHealInput.Text = $"{Healing.CompleteHealPercentage}";
                }

                if (FountainOfLifeInput != null)
                {
                    FountainOfLifeInput.Text = $"{Healing.FountainOfLifeHealPercentage}";
                }
                if (TeamHealInput != null)
                {
                    TeamHealInput.Text = $"{Healing.TeamHealPercentage}";
                }
                if (CompleteTeamHealInput != null)
                {
                    CompleteTeamHealInput.Text = $"{Healing.CompleteTeamHealPercentage}";
                }
            }
        }

        private void HandleMPViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_mpView)) { return; }

                _mpView = View.CreateFromXml(PluginDirectory + "\\UI\\MPView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "MP", XmlViewName = "MPView" }, _mpView);
            }
            else if (_mpWindow == null || (_mpWindow != null && !_mpWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_mpWindow, PluginDir, new WindowOptions() { Name = "MP", XmlViewName = "MPView" }, _mpView, out var container);
                _mpWindow = container;
            }
        }
        private void HandleSoldViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_soldView)) { return; }

                _soldView = View.CreateFromXml(PluginDirectory + "\\UI\\SoldView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Sold", XmlViewName = "SoldView" }, _soldView);
                window.FindView("AMSPercentageBox", out TextInputView AMSInput);

                if (AMSInput != null)
                {
                    AMSInput.Text = $"{AMSPercentage}";
                }
            }
            else if (_soldWindow == null || (_soldWindow != null && !_soldWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_soldWindow, PluginDir, new WindowOptions() { Name = "Sold", XmlViewName = "SoldView" }, _soldView, out var container);
                _soldWindow = container;
                container.FindView("AMSPercentageBox", out TextInputView AMSInput);

                if (AMSInput != null)
                {
                    AMSInput.Text = $"{AMSPercentage}";
                }
            }
        }
        private void HandleEnfoViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_enfoView)) { return; }

                _enfoView = View.CreateFromXml(PluginDirectory + "\\UI\\EnfoView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Enfo", XmlViewName = "EnfoView" }, _enfoView);

                window.FindView("SingleTauntDelayBox", out TextInputView singleInput);
                window.FindView("MongoDelayBox", out TextInputView mongoInput);
                window.FindView("ChallengerDelayBox", out TextInputView challengerInput);

                if (singleInput != null)
                {
                    singleInput.Text = $"{SingleTauntDelay}";
                }
                if (mongoInput != null)
                {
                    mongoInput.Text = $"{MongoDelay}";
                }
                if (challengerInput != null)
                {
                    challengerInput.Text = $"{CycleChallengerDelay}";
                }
            }
            else if (_enfoWindow == null || (_enfoWindow != null && !_enfoWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_enfoWindow, PluginDir, new WindowOptions() { Name = "Enfo", XmlViewName = "EnfoView" }, _enfoView, out var container);
                _enfoWindow = container;

                container.FindView("SingleTauntDelayBox", out TextInputView singleInput);
                container.FindView("MongoDelayBox", out TextInputView mongoInput);
                container.FindView("ChallengerDelayBox", out TextInputView challengerInput);

                if (singleInput != null)
                {
                    singleInput.Text = $"{SingleTauntDelay}";
                }
                if (mongoInput != null)
                {
                    mongoInput.Text = $"{MongoDelay}";
                }
                if (challengerInput != null)
                {
                    challengerInput.Text = $"{CycleChallengerDelay}";
                }
            }
        }

        private void HandleEngieViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_engieView)) { return; }

                _engieView = View.CreateFromXml(PluginDirectory + "\\UI\\EngieView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Engie", XmlViewName = "EngieView" }, _engieView);
            }
            else if (_engieWindow == null || (_engieWindow != null && !_engieWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_engieWindow, PluginDir, new WindowOptions() { Name = "Engie", XmlViewName = "EngieView" }, _engieView, out var container);
                _engieWindow = container;
            }
        }
        private void HandleDocViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_docView)) { return; }

                _docView = View.CreateFromXml(PluginDirectory + "\\UI\\docView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Doc", XmlViewName = "DocView" }, _docView);
            }
            else if (_docWindow == null || (_docWindow != null && !_docWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_docWindow, PluginDir, new WindowOptions() { Name = "Doc", XmlViewName = "DocView" }, _docView, out var container);
                _docWindow = container;
            }
        }
        private void HandleFixerViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_fixerView)) { return; }

                _fixerView = View.CreateFromXml(PluginDirectory + "\\UI\\FixerView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Fixer", XmlViewName = "FixerView" }, _fixerView);
            }
            else if (_fixerWindow == null || (_fixerWindow != null && !_fixerWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_fixerWindow, PluginDir, new WindowOptions() { Name = "Fixer", XmlViewName = "FixerView" }, _fixerView, out var container);
                _fixerWindow = container;
            }
        }
        private void HandleCratViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_cratView)) { return; }

                _cratView = View.CreateFromXml(PluginDirectory + "\\UI\\CratView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Crat", XmlViewName = "CratView" }, _cratView);
            }
            else if (_cratWindow == null || (_cratWindow != null && !_cratWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_cratWindow, PluginDir, new WindowOptions() { Name = "Crat", XmlViewName = "CratView" }, _cratView, out var container);
                _cratWindow = container;
            }
        }
        private void HandleMAViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_maView)) { return; }

                _maView = View.CreateFromXml(PluginDirectory + "\\UI\\MAView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "MA", XmlViewName = "MAView" }, _maView);
            }
            else if (_maWindow == null || (_maWindow != null && !_maWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_maWindow, PluginDir, new WindowOptions() { Name = "MA", XmlViewName = "MAView" }, _maView, out var container);
                _maWindow = container;
            }
        }
        private void HandleNTViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {

                if (window.Views.Contains(_ntView)) { return; }

                _ntView = View.CreateFromXml(PluginDirectory + "\\UI\\NTView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "NT", XmlViewName = "NTView" }, _ntView);

                window.FindView("NullitySpherePercentageBox", out TextInputView nullSphereInput);

                if (nullSphereInput != null)
                {
                    nullSphereInput.Text = $"{NullitySpherePercentage}";
                }
            }
            else if (_ntWindow == null || (_ntWindow != null && !_ntWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_ntWindow, PluginDir, new WindowOptions() { Name = "NTView", XmlViewName = "NTView" }, _ntView, out var container);
                _ntWindow = container;

                container.FindView("NullitySpherePercentageBox", out TextInputView nullSphereInput);

                if (nullSphereInput != null)
                {
                    nullSphereInput.Text = $"{NullitySpherePercentage}";
                }


            }
        }
        private void HandleTradeViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_tradeView)) { return; }

                _tradeView = View.CreateFromXml(PluginDirectory + "\\UI\\TradeView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Trade", XmlViewName = "TradeView" }, _tradeView);
            }
            else if (_tradeWindow == null || (_tradeWindow != null && !_tradeWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_tradeWindow, PluginDir, new WindowOptions() { Name = "Trade", XmlViewName = "TradeView" }, _tradeView, out var container);
                _tradeWindow = container;
            }
        }
        private void HandleAdvyViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_advyView)) { return; }

                _advyView = View.CreateFromXml(PluginDirectory + "\\UI\\AdvyView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Advy", XmlViewName = "AdvyView" }, _advyView);
            }
            else if (_advyWindow == null || (_advyWindow != null && !_advyWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_advyWindow, PluginDir, new WindowOptions() { Name = "Advy", XmlViewName = "AdvyView" }, _advyView, out var container);
                _advyWindow = container;
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
            try
            {
                if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 0.6)
                {
                    return;
                }

                if (Time.NormalTime > _ncuUpdateTime + 1.0f)
                {
                    RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                    IPCChannel.Broadcast(ncuMessage);

                    OnRemainingNCUMessage(0, ncuMessage);

                    FP();

                    _ncuUpdateTime = Time.NormalTime;

                }
                CancelBuffs();

                Morphs();

                if (_settings["SyncPets"].AsBool())
                {
                    SynchronizePetCombatStateWithOwner(PetType.Attack, PetType.Support);
                }

                if (CanLookupPetsAfterZone())
                {
                    AssignTargetToHealPet();
                }

                var uiSetting = _settings["FalseProfSelection"].AsInt32();

                switch (uiSetting)
                {
                    case 0:
                        check = uiSetting;
                        break;
                    case 1://mp
                        if (uiSetting != check)
                        {
                            #region Metaphysicist

                            //buffs
                            RegisterSpellProcessor(RelevantNanos.CostBuffs, Cost);
                            //Pets
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AttackPets).OrderByStackingOrder(), AttackPetSpawner);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealPets).OrderByStackingOrder(), HealPetSpawner);
                            //petbuffs
                            RegisterSpellProcessor(RelevantNanos.InstillDamageBuffs, InstillDamage);
                            RegisterSpellProcessor(RelevantNanos.ChantBuffs, Chant);
                            RegisterSpellProcessor(RelevantNanos.PetShortTermDamage, ShortTermDamagePet);
                            RegisterSpellProcessor(RelevantNanos.CostBuffs, CostPet);
                            RegisterSpellProcessor(RelevantNanos.PetWarp, PetWarp);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Psy_IntBuff).OrderByStackingOrder(),
                               (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                               => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                            //nukes
                            RegisterSpellProcessor(RelevantNanos.WarmUpfNukes, WarmUpNuke, CombatActionPriority.Medium);
                            RegisterSpellProcessor(RelevantNanos.MPNukes, MPNuke);

                            #endregion
                            Chat.WriteLine("Setting FP to Metaphysicist");
                            check = uiSetting;
                        }
                        break;
                    case 2://sol
                        if (uiSetting != check)
                        {
                            #region Soldier

                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ReflectShield).Where(c => c.Name.Contains("Mirror")).OrderByStackingOrder(), AMS);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ReflectShield).Where(c => !c.Name.Contains("Mirror")).OrderByStackingOrder(), RKReflects);
                            RegisterSpellProcessor(29251, TeamRiotControl);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.BurstBuff).OrderByStackingOrder(),
                              (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                               => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TotalFocus).OrderByStackingOrder(),
                              (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                               => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(),
                              (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                               => NonComabtTeamBuff(buffSpell, fightingTarget, ref actionTarget, "InitBuff"));

                            #endregion
                            Chat.WriteLine("Setting FP to Soldier");
                            check = uiSetting;
                        }
                        break;
                    case 3://enf
                        if (uiSetting != check)
                        {
                            #region Enforcer

                            //Taunts
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MongoBuff).OrderByStackingOrder(), Mongo, CombatActionPriority.High);
                            RegisterSpellProcessor(RelevantNanos.SingleTargetTaunt, SingleTargetTaunt, CombatActionPriority.High);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Challenger).OrderByStackingOrder(), CycleChallenger, CombatActionPriority.High);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HPBuff).OrderByStackingOrder(),
                                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                            => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(),
                                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "EnfDmgShield"));


                            #endregion
                            Chat.WriteLine("Setting FP to Enforcer");
                            check = uiSetting;
                        }
                        break;
                    case 4://eng
                        if (uiSetting != check)
                        {
                            #region Eng

                            #endregion
                            Chat.WriteLine("Setting FP to Engineer");
                            check = uiSetting;
                        }
                        break;
                    case 5://doc
                        if (uiSetting != check)
                        {
                            #region Doctor

                            //heals
                            RegisterSpellProcessor(RelevantNanos.AlphaAndOmega, LockCH, CombatActionPriority.High);
                            RegisterSpellProcessor(RelevantNanos.DocHeals, DocTargetHealing, CombatActionPriority.High);
                            RegisterSpellProcessor(RelevantNanos.DocCompleteTargetHealing, DocCompleteHealing, CombatActionPriority.High);
                            RegisterSpellProcessor(RelevantNanos.DocTeamHeals,
                                       (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                                       DocTeamHealing(spell, fightingTarget, ref actionTarget, "TeamHealSelection"),
                                       CombatActionPriority.High);
                            RegisterSpellProcessor(RelevantNanos.TeamImprovedLifeChanneler,
                                      (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                                      TeamImprovedLifeChannelerAsTeamHeal(spell, fightingTarget, ref actionTarget, "TeamHealSelection"),
                                      CombatActionPriority.High);
                            RegisterSpellProcessor(RelevantNanos.AlphaAndOmega, DocCompleteTeamHealing, CombatActionPriority.High);

                            //Hots
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealOverTime).OrderByStackingOrder(), ShortHOT);
                            //buffs
                            RegisterSpellProcessor(RelevantNanos.HPBuffs, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(),
                                 (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "InitBuffSelection"));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceBuffs).OrderByStackingOrder(),
                                  (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "NanoResistSelection"));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealDeltaBuff).OrderByStackingOrder(),
                                  (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "HealDeltaBuffSelection"));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolTeam);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.StrengthBuff).OrderByStackingOrder(),
                                  (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "StrengthBuffSelection"));
                            //Epsilon Purge
                            RegisterSpellProcessor(RelevantNanos.EpsilonPurge, EpsilonPurge, CombatActionPriority.High);
                            //Short hp AGENT CANNOT CAST TILC
                            RegisterSpellProcessor(RelevantNanos.IndividualShortMaxHealths, ShortMaxHealth);
                            //Debuffs
                            RegisterSpellProcessor(RelevantNanos.InitDebuffs,
                                 (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                  => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "InitDebuffSelection"), CombatActionPriority.Medium);

                            #endregion
                            Chat.WriteLine("Setting FP to Doctor");
                            check = uiSetting;
                        }
                        break;
                    case 6://fix
                        if (uiSetting != check)
                        {
                            #region Fixer
                            //Root/Snare
                            RegisterSpellProcessor(RelevantNanos.SpinNanoweb, AOESnare, CombatActionPriority.High);
                            RegisterSpellProcessor(RelevantNanos.GravityBindings, FixerSnare, CombatActionPriority.High);
                            #endregion
                            Chat.WriteLine("Setting FP to Fixer");
                            check = uiSetting;
                        }
                        break;
                    case 7://crat
                        if (uiSetting != check)
                        {
                            #region Crat
                            //Calms
                            RegisterSpellProcessor(RelevantNanos.ShadowlandsCalms, ShadowlandsCalms, CombatActionPriority.High);
                            RegisterSpellProcessor(RelevantNanos.AOECalms, AOECalms, CombatActionPriority.High);
                            RegisterSpellProcessor(RelevantNanos.RkCalms, RkCalms, CombatActionPriority.High);
                            //buffs
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalDecreaseBuff).OrderByStackingOrder(),
                                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "CutRedTape"));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ExperienceConstructs_XPBonus).OrderByStackingOrder(), XPBonus);
                            //Root/Snare
                            RegisterSpellProcessor(RelevantNanos.GreaterFearofAttention, Root, CombatActionPriority.High);
                            RegisterSpellProcessor(RelevantNanos.ShacklesofObedience, Snare, CombatActionPriority.High);
                            //debuffs
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDeltaDebuff).OrderByStackingOrder(),
                            (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                             => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "IntensifyStressSelection"), CombatActionPriority.Medium);
                            //Nukes
                            RegisterSpellProcessor(RelevantNanos.CratSpecialNuke, CratSpecialNuke, CombatActionPriority.Low);
                            RegisterSpellProcessor(RelevantNanos.CratNuke, CratNuke, CombatActionPriority.Low);

                            #endregion
                            Chat.WriteLine("Setting FP to Bureaucrat");
                            check = uiSetting;
                        }
                        break;
                    case 8://ma
                        if (uiSetting != check)
                        {
                            #region MA
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SingleTargetHealing).OrderByStackingOrder(),
                                Healing.TargetHealing, CombatActionPriority.High);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TeamHealing).OrderByStackingOrder(),
                                Healing.TargetHealingAsTeam, CombatActionPriority.High);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(),
                                  (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "MAEvades"));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.BrawlBuff).OrderByStackingOrder(),
                                  (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "BrawlBuff"));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledRageBuff).OrderByStackingOrder(),
                                  (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "MABuff"));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RunspeedBuffs).OrderByStackingOrder(),
                                  (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "RunSpeed"));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.StrengthBuff).OrderByStackingOrder(),
                                  (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "StrengthBuffSelection"));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtsBuff).OrderByStackingOrder(),
                                  (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "MABuff"));
                            RegisterSpellProcessor(RelevantNanos.DamageTypeFire,
                                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                                MADamageType(spell, fightingTarget, ref actionTarget, 1));
                            RegisterSpellProcessor(RelevantNanos.DamageTypeEnergy,
                                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                                MADamageType(spell, fightingTarget, ref actionTarget, 2));
                            RegisterSpellProcessor(RelevantNanos.DamageTypeChemical,
                                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                                MADamageType(spell, fightingTarget, ref actionTarget, 3));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledDestructionBuff).Where(s => s.StackingOrder < 19).OrderByStackingOrder(), ControlledDestructionWithShutdown);

                            #endregion
                            Chat.WriteLine("Setting FP to Martial artist");
                            check = uiSetting;
                        }
                        break;
                    case 9://nt
                        if (uiSetting != check)
                        {
                            #region NT

                            //RK Nukes
                            RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke, CombatActionPriority.Low);
                            RegisterSpellProcessor(RelevantNanos.RKAOENukes,
                                (Spell aoeNuke, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => AOENuke(aoeNuke, fightingTarget, ref actionTarget, 1), CombatActionPriority.Low);
                            RegisterSpellProcessor(new[] { RelevantNanos.VolcanicEruption },
                            (Spell aoeNuke, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                            => AOENuke(aoeNuke, fightingTarget, ref actionTarget, 2));
                            RegisterSpellProcessor(RelevantNanos.HaloNanoDebuff, HaloNanoDebuff, CombatActionPriority.High);
                            //buffs
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Psy_IntBuff).OrderByStackingOrder(),
                                 (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NFRangeBuff).OrderByStackingOrder(),
                                  (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "NFRangeBuff"));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MatCreaBuff).OrderByStackingOrder(), MatCreaBuff);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoOverTime_LineA).OrderByStackingOrder(),
                                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "NanoHOTTeam"));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NullitySphereNano).OrderByStackingOrder(), NullitySphere, CombatActionPriority.Medium);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NPCostBuff).OrderByStackingOrder(),
                                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "NTCost"));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(),
                                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "NTEvades"));
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Fortify).OrderByStackingOrder(),
                                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "NTFortify"));

                            //debuffs
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AAODebuffs).OrderByStackingOrder(), SingleBlind, CombatActionPriority.High);
                            RegisterSpellProcessor(RelevantNanos.AOEBlinds, AOEBlind, CombatActionPriority.High);

                            #endregion
                            Chat.WriteLine("Setting FP to Nano-technician");
                            check = uiSetting;
                        }
                        break;
                    case 10://trader
                        if (uiSetting != check)
                        {
                            #region Trader

                            //debuffs
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDrain_LineA).OrderByStackingOrder(), RKNanoDrain);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderDebuffACNanos).OrderByStackingOrder(), ACDrain, CombatActionPriority.Medium);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderACTransferTargetDebuff_Draw).OrderByStackingOrder(), ACDrain, CombatActionPriority.Low);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderACTransferTargetDebuff_Siphon).OrderByStackingOrder(), ACDrain, CombatActionPriority.Low);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Deprive).OrderByStackingOrder(), DepriveDrain, CombatActionPriority.High);
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Ransack).OrderByStackingOrder(), RansackDrain, CombatActionPriority.High);
                            //buffs
                            RegisterSpellProcessor(RelevantNanos.QuantumUncertanity,
                                  (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                 => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "TdrEvades"));
                            //Mezz
                            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Mezz).OrderByStackingOrder(), Mezz, CombatActionPriority.High);
                            //Root/Snare
                            RegisterSpellProcessor(RelevantNanos.FlowofTime, Root, CombatActionPriority.High);

                            #endregion
                            Chat.WriteLine("Setting FP to Trader");
                            check = uiSetting;
                        }
                        break;
                    case 11://advy
                        if (uiSetting != check)
                        {
                            #region Adventurer
                            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfAdv))
                            {
                                //Heals
                                RegisterSpellProcessor(RelevantNanos.AdvyTargetHeals, AdvyTargetHealing, CombatActionPriority.High);
                                RegisterSpellProcessor(RelevantNanos.AdvyTeamHeals, AdvyTeamHealing, CombatActionPriority.High);
                                RegisterSpellProcessor(RelevantNanos.AdvyCompleteHeal, AdvyCompleteHealing, CombatActionPriority.High);

                                //buffs
                                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine._1HEdgedBuff).OrderByStackingOrder(), Melee);
                                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), Ranged);
                                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(),
                                    (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                    => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "AdvDmgShield"));
                                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MultiwieldBuff).OrderByStackingOrder(),
                                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                    => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                                RegisterSpellProcessor(RelevantNanos.ArmorBuffs,
                                    (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                    => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "AdvArmor"));

                                //Morphs
                                Morphs();

                                //Morph Buffs
                                RegisterSpellProcessor(RelevantNanos.DragonScales, DragonScales);
                                RegisterSpellProcessor(RelevantNanos.LeetCrit, LeetCrit);
                                RegisterSpellProcessor(RelevantNanos.WolfAgility, WolfAgility);
                                RegisterSpellProcessor(RelevantNanos.SaberDamage, SaberDamage);
                                RegisterSpellProcessor(RelevantNanos.TreeBuff, TreeBuff);
                                //Morph Cooldowns
                                RegisterSpellProcessor(RelevantNanos.CatDamage, CatDamage);

                            }
                            #endregion
                            Chat.WriteLine("Setting FP to Adventurer");
                            check = uiSetting;
                        }
                        break;
                }

                if (_settings["ProcSelection"].AsInt32() == 1)
                {
                    CancelBuffs(RelevantNanos.DetauntProcs);
                }
                if (_settings["ProcSelection"].AsInt32() == 2)
                {
                    CancelBuffs(RelevantNanos.DOTProcs);
                }

                #region UI

                var window = SettingsController.FindValidWindow(_windows);

                if (window != null && window.IsValid)
                {
                    window.FindView("TargetHealPercentageBox", out TextInputView TargetHealInput);
                    window.FindView("CompleteHealPercentageBox", out TextInputView CompleteHealInput);
                    window.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);
                    window.FindView("TeamHealPercentageBox", out TextInputView TeamHealInput);
                    window.FindView("CompleteTeamHealPercentageBox", out TextInputView CompleteTeamHealInput);
                    window.FindView("AMSPercentageBox", out TextInputView AMSInput);
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
                    window.FindView("MongoDelayBox", out TextInputView mongoInput);
                    window.FindView("ChallengerDelayBox", out TextInputView challengerInput);
                    window.FindView("SingleTauntDelayBox", out TextInputView singleInput);
                    window.FindView("NullitySpherePercentageBox", out TextInputView nullSphereInput);

                    if (AMSInput != null && !string.IsNullOrEmpty(AMSInput.Text))
                    {
                        if (int.TryParse(AMSInput.Text, out int AMSValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].AMSPercentage != AMSValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].AMSPercentage = AMSValue;
                            }
                        }
                    }
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

                    if (mongoInput != null && !string.IsNullOrEmpty(mongoInput.Text))
                    {
                        if (int.TryParse(mongoInput.Text, out int mongoValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].MongoDelay != mongoValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].MongoDelay = mongoValue;
                            }
                        }
                    }

                    if (challengerInput != null && !string.IsNullOrEmpty(challengerInput.Text))
                    {
                        if (int.TryParse(challengerInput.Text, out int challengerValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleChallengerDelay != challengerValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleChallengerDelay = challengerValue;
                            }
                        }
                    }

                    if (singleInput != null && !string.IsNullOrEmpty(singleInput.Text))
                    {
                        if (int.TryParse(singleInput.Text, out int singleValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelay != singleValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelay = singleValue;
                            }
                        }
                    }

                    if (CompleteHealInput != null && !string.IsNullOrEmpty(CompleteHealInput.Text))
                    {
                        if (int.TryParse(CompleteHealInput.Text, out int completeHealValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentage != completeHealValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentage = completeHealValue;
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

                    if (CompleteTeamHealInput != null && !string.IsNullOrEmpty(CompleteTeamHealInput.Text))
                    {
                        if (int.TryParse(CompleteTeamHealInput.Text, out int Value))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteTeamHealPercentage != Value)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteTeamHealPercentage = Value;
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

                    if (nullSphereInput != null && !string.IsNullOrEmpty(nullSphereInput.Text))
                    {
                        if (int.TryParse(nullSphereInput.Text, out int nullSphereValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].NullitySpherePercentage != nullSphereValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].NullitySpherePercentage = nullSphereValue;
                            }
                        }
                    }

                    if (!_settings["SyncPets"].AsBool() && _syncPets)
                    {
                        IPCChannel.Broadcast(new PetSyncOffMessage());
                        Chat.WriteLine("SyncPets disabled");
                        syncPetsOffDisabled();
                    }

                    if (_settings["SyncPets"].AsBool() && !_syncPets)
                    {
                        IPCChannel.Broadcast(new PetSyncOnMessag());
                        Chat.WriteLine("SyncPets enabled.");
                        syncPetsOnEnabled();
                    }
                    if (window.FindView("MPView", out Button mpView))
                    {
                        mpView.Tag = SettingsController.settingsWindow;
                        mpView.Clicked = HandleMPViewClick;
                    }
                    if (window.FindView("SoldView", out Button soldView))
                    {
                        soldView.Tag = SettingsController.settingsWindow;
                        soldView.Clicked = HandleSoldViewClick;
                    }
                    if (window.FindView("EnfoView", out Button EnfoView))
                    {
                        EnfoView.Tag = SettingsController.settingsWindow;
                        EnfoView.Clicked = HandleEnfoViewClick;
                    }
                    if (window.FindView("EngieView", out Button EngieView))
                    {
                        EngieView.Tag = SettingsController.settingsWindow;
                        EngieView.Clicked = HandleEngieViewClick;
                    }
                    if (window.FindView("DocView", out Button DocView))
                    {
                        DocView.Tag = SettingsController.settingsWindow;
                        DocView.Clicked = HandleDocViewClick;
                    }
                    if (window.FindView("FixerView", out Button FixerView))
                    {
                        FixerView.Tag = SettingsController.settingsWindow;
                        FixerView.Clicked = HandleFixerViewClick;
                    }
                    if (window.FindView("CratView", out Button CratView))
                    {
                        CratView.Tag = SettingsController.settingsWindow;
                        CratView.Clicked = HandleCratViewClick;
                    }
                    if (window.FindView("MAView", out Button MAView))
                    {
                        MAView.Tag = SettingsController.settingsWindow;
                        MAView.Clicked = HandleMAViewClick;
                    }
                    if (window.FindView("NTView", out Button NTView))
                    {
                        NTView.Tag = SettingsController.settingsWindow;
                        NTView.Clicked = HandleNTViewClick;
                    }
                    if (window.FindView("TradeView", out Button TradeView))
                    {
                        TradeView.Tag = SettingsController.settingsWindow;
                        TradeView.Clicked = HandleTradeViewClick;
                    }
                    if (window.FindView("AdvyView", out Button AdvyView))
                    {
                        AdvyView.Tag = SettingsController.settingsWindow;
                        AdvyView.Clicked = HandleAdvyViewClick;
                    }
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

                    if (SettingsController.settingsWindow.FindView("FalseProfsView", out Button falseProfView))
                    {
                        falseProfView.Tag = SettingsController.settingsWindow;
                        falseProfView.Clicked = HandleFalseProfViewClick;
                    }

                    if (SettingsController.settingsWindow.FindView("ProcView", out Button procView))
                    {
                        procView.Tag = SettingsController.settingsWindow;
                        procView.Clicked = HandleProcViewClick;
                    }

                    if (SettingsController.settingsWindow.FindView("MorphView", out Button morphView))
                    {
                        morphView.Tag = SettingsController.settingsWindow;
                        morphView.Clicked = HandleMorphViewClick;
                    }

                    if (_settings["MorphSelection"].AsInt32() != 1)
                    {
                        CancelBuffs(RelevantNanos.DragonMorph);
                    }
                    if (_settings["MorphSelection"].AsInt32() != 4)
                    {
                        CancelBuffs(RelevantNanos.LeetMorph);
                    }
                    if (_settings["MorphSelection"].AsInt32() != 2)
                    {
                        CancelBuffs(RelevantNanos.SaberMorph);
                    }
                    if (_settings["MorphSelection"].AsInt32() != 3)
                    {
                        CancelBuffs(RelevantNanos.WolfMorph);
                    }
                    if (_settings["MorphSelection"].AsInt32() != 5)
                    {
                        CancelBuffs(RelevantNanos.TreeMorph);
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

        #region Adventurer

        #region Healing

        private bool AdvyTargetHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfAdv)) { return false; }

            return Healing.TargetHealing(spell, fightingTarget, ref actionTarget);
        }

        private bool AdvyTeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfAdv)) { return false; }

            return Healing.TeamHealing(spell, fightingTarget, ref actionTarget);
        }

        private bool AdvyCompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfAdv)) { return false; }

            return Healing.CompleteHealing(spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Morphs

        private void MorphSwitch()
        {
            switch (_settings["MorphSelection"].AsInt32())
            {
                case 0://None
                    MorhpSpellArray = new int[0];
                    if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.Polymorph))
                    {
                        CancelBuffs(RelevantNanos.Morphs);
                    }
                    break;
                case 1://Dragon
                    MorhpSpellArray = RelevantNanos.DragonMorph;
                    break;
                case 2://Saber
                    MorhpSpellArray = RelevantNanos.SaberMorph;
                    break;
                case 3://Wolf
                    MorhpSpellArray = RelevantNanos.WolfMorph;
                    break;
                case 4://Leet
                    MorhpSpellArray = RelevantNanos.LeetMorph;
                    break;
                case 5://Tree
                    MorhpSpellArray = RelevantNanos.TreeMorph;
                    break;
            }
        }

        private void Morphs()
        {
            var localPlayer = DynelManager.LocalPlayer;

            if (localPlayer.GetStat(Stat.VisualProfession) != 6) { return; }
            if (localPlayer.Buffs.Contains(RelevantNanos.BirdMorph)) { return; }

            MorphSwitch();

            if (MorhpSpellArray.Length == 0) { return; }
            if (localPlayer.Buffs.Contains(MorhpSpellArray)) { return; }

            var MorphSpell = Spell.List.FirstOrDefault(h => MorhpSpellArray.Contains(h.Id));
            if (MorphSpell == null) { return; }

            if (!Spell.HasPendingCast && MorphSpell.MeetsUseReqs() && localPlayer.MovementStatePermitsCasting)
            {
                MorphSpell.Cast(localPlayer, true);
            }
        }

        #endregion

        #region Morph Buffs

        private bool WolfAgility(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var localPlayer = DynelManager.LocalPlayer;

            if (_settings["MorphSelection"].AsInt32() != 3) { return false; }

            if (!localPlayer.Buffs.Contains(RelevantNanos.WolfMorph)) { return false; }

            if (localPlayer.Buffs.Contains(RelevantNanos.WolfAgility)) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = DynelManager.LocalPlayer;
            return true;
        }
        private bool SaberDamage(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var localPlayer = DynelManager.LocalPlayer;

            if (_settings["MorphSelection"].AsInt32() != 2) { return false; }

            if (!localPlayer.Buffs.Contains(RelevantNanos.SaberMorph)) { return false; }

            if (localPlayer.Buffs.Contains(RelevantNanos.SaberDamage)) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = DynelManager.LocalPlayer;
            return true;
        }
        private bool LeetCrit(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var localPlayer = DynelManager.LocalPlayer;

            if (_settings["MorphSelection"].AsInt32() != 4) { return false; }

            if (!localPlayer.Buffs.Contains(RelevantNanos.LeetMorph)) { return false; }

            if (localPlayer.Buffs.Contains(RelevantNanos.LeetCrit)) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = DynelManager.LocalPlayer;
            return true;
        }
        private bool DragonScales(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var localPlayer = DynelManager.LocalPlayer;

            if (_settings["MorphSelection"].AsInt32() != 1) { return false; }

            if (!localPlayer.Buffs.Contains(RelevantNanos.DragonMorph)) { return false; }

            if (localPlayer.Buffs.Contains(RelevantNanos.DragonScales)) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = DynelManager.LocalPlayer;
            return true;
        }
        private bool TreeBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var localPlayer = DynelManager.LocalPlayer;

            if (_settings["MorphSelection"].AsInt32() != 5) { return false; }

            if (!localPlayer.Buffs.Contains(RelevantNanos.TreeMorph)) { return false; }

            if (localPlayer.Buffs.Contains(RelevantNanos.TreeBuff)) { return false; }
            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = DynelManager.LocalPlayer;
            return true;
        }
        private bool CatDamage(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["MorphSelection"].AsInt32() != 2) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.SaberMorph)) { return false; }

            return ToggledTargetDebuff("CatDamage", spell, spell.Nanoline, fightingTarget, ref actionTarget);

        }

        #endregion

        #region Buffs

        protected bool Ranged(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfAdv)) { return false; }

            return BuffWeaponSkill(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Ranged);
        }

        protected bool Melee(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfAdv)) { return false; }

            return BuffWeaponSkill(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Melee);
        }
        #endregion

        #endregion

        #region Agent

        #region Perks

        protected bool TheShot(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["TheShot"].AsBool()) { return false; }
            if (fightingTarget == null) { return false; }
            if (!perk.IsAvailable) { return false; }
            if (fightingTarget?.Buffs.Where(c => c.Name.ToLower().Contains(perk.Name.ToLower()) && c.RemainingTime > 3).Any() == true) { return false; }

            return PerkCondtionProcessors.DamagePerk(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Buffs

        private bool Concentration(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledTargetDebuff("Concentration", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool Rifle(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.AssassinsAimedShot, out Buff AAS)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.SteadyNerves, out Buff SN)) { return false; }

            return BuffWeaponSkill(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Rifle);
        }

        private bool Aimed(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponSkill(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Rifle);
        }

        private bool DetauntProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["ProcSelection"].AsInt32() != 2) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        private bool DamageProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["ProcSelection"].AsInt32() != 1) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        #endregion

        #region Debuffs
        //root/snare
        private bool Root(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Buffing"].AsBool()
                || !_settings["Root"].AsBool() || !CanCast(spell)) { return false; }

            var target = DynelManager.Characters
                    .Where(c => c.IsInLineOfSight
                        && IsMoving(c)
                        && !c.Buffs.Contains(NanoLine.Root)
                        && (c.Name == "Flaming Vengeance"
                            || c.Name == "Hand of the Colonel"
                            || c.Name == "Alien Seeker"))
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .FirstOrDefault();

            if (target == null) { return false; }

            actionTarget.Target = target;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        private bool Snare(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Buffing"].AsBool()
                || !_settings["Root"].AsBool() || !CanCast(spell)) { return false; }

            var target = DynelManager.Characters
                    .Where(c => c.IsInLineOfSight
                        && c.IsMoving
                        && !c.Buffs.Contains(NanoLine.Root)
                        && c.Name == "Alien Heavy Patroller")
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .FirstOrDefault();

            if (target == null) { return false; }

            actionTarget.Target = target;
            actionTarget.ShouldSetTarget = true;
            return true;
        }
        //snares
        private bool AOESnare(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Buffing"].AsBool() || !_settings["AOESnare"].AsBool() || !CanCast(spell)) { return false; }

            var target = DynelManager.Characters
                    .Where(c => c.IsInLineOfSight
                        && IsMoving(c)
                        && !c.Buffs.Contains(NanoLine.Root)
                        && (c.Name == "Flaming Vengeance"
                            || c.Name == "Hand of the Colonel"
                            || c.Name == "Alien Seeker"))
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .FirstOrDefault();

            if (target == null) { return false; }

            actionTarget.Target = target;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        private bool FixerSnare(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Buffing"].AsBool()  || !_settings["AOESnare"].AsBool() || !CanCast(spell)) { return false; }

            var target = DynelManager.Characters
                    .Where(c => c.IsInLineOfSight
                        && c.IsMoving
                        && !c.Buffs.Contains(NanoLine.Root)
                        && c.Name == "Alien Heavy Patroller")
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .FirstOrDefault();

            if (target == null) { return false; }

            actionTarget.Target = target;
            actionTarget.ShouldSetTarget = true;
            return true;
        }
        #endregion

        #endregion

        #region Doctor

        #region Healing

        private bool DocTargetHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfDoc)) { return false; }

            return Healing.TargetHealing(spell, fightingTarget, ref actionTarget);
        }

        private bool DocCompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfDoc)) { return false; }

            return Healing.CompleteHealing(spell, fightingTarget, ref actionTarget);
        }

        private bool DocTeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, string selectionSetting)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfDoc)) { return false; }

            if (_settings["TeamHealSelection"].AsInt32() != 0) { return false; }

            if (Healing.TeamHealPercentage == 0) { return false; }

            if (!Team.IsInTeam) { return false; }

            var dyingTeamMembersCount = Team.Members
                   .Count(m => m.Character != null
                            && m.Character.Health > 0
                            && m.Character.HealthPercent <= Healing.TeamHealPercentage);

            if (dyingTeamMembersCount < 2) { return false; }

            return true;
        }

        private bool DocCompleteTeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfDoc)) { return false; }

            return Healing.CompleteTeamHealing(spell, fightingTarget, ref actionTarget);
        }

        private bool TeamImprovedLifeChannelerAsTeamHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, string selectionSetting)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfDoc)) { return false; }

            if (_settings["TeamHealSelection"].AsInt32() != 1) { return false; }

            if (Healing.TeamHealPercentage == 0) { return false; }

            if (!Team.IsInTeam) { return false; }

            var dyingTeamMembersCount = Team.Members
                    .Count(m => m.Character != null
                             && m.Character.Health > 0
                             && m.Character.HealthPercent <= Healing.TeamHealPercentage);

            if (dyingTeamMembersCount < 2) { return false; }

            return true;
        }

        private bool LockCH(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["LockCH"].AsBool()) return false;

            switch (Playfield.ModelIdentity.Instance)
            {
                case 6015: // 12m
                    if (!DynelManager.NPCs.Any(c => c.IsAlive && c.Name == "Deranged Xan")) { return false; }
                    break;
                case 8020: // poh
                    if (!DynelManager.NPCs.Any(c => c.IsAlive && c.Name == "The Maiden")) { return false; }
                    break;
                default:
                    return false;
            }

            return true;
        }
        private bool ShortHOT(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["ShortHOT"].AsBool() || !InCombat()) { return false; }

            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Buffs
        private bool ShortMaxHealth(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            switch (_settings["ShortHpSelection"].AsInt32())
            {
                case 0://None
                    return false;
                case 1://Self
                    return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
                case 2://Team
                    return CombatTeamBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
                default:
                    return false;
            }
        }
        #endregion

        #region Epsilion Purge
        private bool EpsilonPurge(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["EpsilonPurge"].AsBool()) { return false; }

            var target = Team.Members
                .Select(m => m.Character)
                .FirstOrDefault(c => c != null
                                  && c.IsInLineOfSight
                                  && spell.IsInRange(c)
                                  && (c.Buffs.Contains(NanoLine.DOT_LineA)
                                      || c.Buffs.Contains(NanoLine.DOT_LineB)
                                      || c.Buffs.Contains(NanoLine.DOTAgentStrainA)
                                      || c.Buffs.Contains(NanoLine.DOTNanotechnicianStrainA)
                                      || c.Buffs.Contains(NanoLine.DOTNanotechnicianStrainB)
                                      || c.Buffs.Contains(NanoLine.DOTStrainC)));

            if (target == null) { return false; }

            actionTarget.Target = target;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        #endregion

        #endregion

        #region Enforcer

        private bool Mongo(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfEnf)) { return false; }
            if (!CanCast(spell)) { return false; }

            var mob = DynelManager.NPCs
                   .Where(attackingMob => attackingMob.IsAttacking && attackingMob.FightingTarget?.Identity != DynelManager.LocalPlayer.Identity
                   && attackingMob.Position.DistanceFrom(DynelManager.LocalPlayer.Position) <= 20f && !debuffAreaTargetsToIgnore.Contains(attackingMob.Name)
                       && AttackingTeam(attackingMob))
                   .FirstOrDefault();

            if (_settings["OSTMongo"].AsBool())
            {
                if (!spell.IsReady) { return false; }
                if (Spell.HasPendingCast) { return false; }
                return true;
            }
            else
            {
                var selection = _settings["MongoSelection"].AsInt32();

                if (selection == 0) { return false; }
                if (DynelManager.LocalPlayer.HealthPercent <= 30) { return false; }
                if (Time.AONormalTime < _mongo + MongoDelay) { return false; }

                switch (selection)
                {
                    case 1:
                        if (mob == null) { return false; }
                        _mongo = Time.AONormalTime;
                        return true;
                    case 2:

                        if (fightingTarget?.Position.DistanceFrom(DynelManager.LocalPlayer.Position) >= 20f
                        || debuffAreaTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }
                        _mongo = Time.AONormalTime;
                        return true;
                    default:
                        return false;
                }
            }
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var selection = _settings["SingleTauntsSelection"].AsInt32();

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfEnf)) { return false; }
            if (selection == 0) { return false; }
            if (!CanCast(spell)) { return false; }
            if (Time.AONormalTime < _singleTaunt + SingleTauntDelay) { return false; }
            if (DynelManager.LocalPlayer.HealthPercent <= 30) { return false; }
            if (debuffAreaTargetsToIgnore.Contains(fightingTarget?.Name)) { return false; }

            switch (selection)
            {
                case 1:
                    if (fightingTarget == null) { return false; }

                    _singleTaunt = Time.AONormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = fightingTarget;
                    return true;
                case 2:
                    var mob = DynelManager.NPCs
                   .Where(c => c != null && c.IsAttacking && c.FightingTarget?.Identity != DynelManager.LocalPlayer.Identity && c.IsInLineOfSight && spell.IsInRange(c) && AttackingTeam(c))
                   .OrderBy(c => c.MaxHealth).FirstOrDefault();

                    if (mob == null) { return false; }

                    _singleTaunt = Time.AONormalTime;
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = mob;
                    return true;
                default:
                    return false;
            }
        }
        private bool CycleChallenger(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfEnf)) { return false; }
            if (!_settings["Buffing"].AsBool() || !CanCast(spell)) { return false; }
            if (!_settings["CycleChallenger"].AsBool()) { return false; }
            if (fightingTarget == null || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }
            if (Time.AONormalTime < _challenger + CycleChallengerDelay) { return false; }

            _challenger = Time.AONormalTime;
            return true;
        }

        #endregion

        #region MP

        private bool Cost(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfMp)) { return false; }

            switch (_settings["CostBuffSelection"].AsInt32())
            {
                case 0:
                    return false;
                case 1:
                    return NonCombatBuff(spell, ref actionTarget, fightingTarget);
                case 2:
                    if (!Team.IsInTeam) { return false; }

                    var target = DynelManager.Players// fix this
                        .Where(c => Team.Members.Any(t => t.Identity.Instance == c.Identity.Instance)
                            && spell.IsInRange(c)
                            && c.Health > 0
                            && SpellChecksOther(spell, spell.Nanoline, c))
                        .FirstOrDefault();

                    if (target == null) { return false; }

                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = target;
                    return true;
                default:
                    return false;
            }
        }

        private bool Chant(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfMp)) { return false; }

            return PetTargetBuff(NanoLine.MPPetInitiativeBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool InstillDamage(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfMp)) { return false; }

            return PetTargetBuff(NanoLine.MPPetDamageBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool ShortTermDamagePet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfMp)) { return false; }

            return PetTargetBuff(NanoLine.PetShortTermDamageBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool CostPet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfMp)) { return false; }

            return PetTargetBuff(NanoLine.NPCostBuff, PetType.Heal, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(NanoLine.NPCostBuff, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool PetWarp(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["WarpPets"].AsBool() || !CanCast(spell) || !CanLookupPetsAfterZone()) { return false; }

            return DynelManager.LocalPlayer.Pets.Any(c => c.Character == null);
        }

        private bool AttackPetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfMp)) { return false; }

            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 0) { return false; }

            return NoShellPetSpawner(PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool HealPetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfMp)) { return false; }

            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 0) { return false; }

            return NoShellPetSpawner(PetType.Heal, spell, fightingTarget, ref actionTarget);
        }
        private bool WarmUpNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Nukes"].AsBool()) { return false; }

            if (!_settings["DebuffNuke"].AsBool()) { return false; }

            if (fightingTarget == null || !CanCast(spell)) { return false; }

            if (_settings["NormalNuke"].AsBool() && fightingTarget.Buffs.Contains(NanoLine.MetaphysicistMindDamageNanoDebuffs)) { return false; }

            return true;
        }
        private bool MPNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Nukes"].AsBool()) { return false; }

            if (!_settings["NormalNuke"].AsBool()) { return false; }

            if (fightingTarget == null || !CanCast(spell)) { return false; }

            if (_settings["DebuffNuke"].AsBool() && !fightingTarget.Buffs.Contains(NanoLine.MetaphysicistMindDamageNanoDebuffs)) { return false; }

            return true;
        }
        #endregion

        #region Soldier

        private bool AMS(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!_settings["Buffing"].AsBool()) { return false; }
            if (!CanCast(spell)) { return false; }

            return DynelManager.LocalPlayer.HealthPercent <= AMSPercentage;

        }
        private bool RKReflects(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            switch (_settings["RKReflectSelection"].AsInt32())
            {
                case 1:
                    return NonCombatBuff(spell, ref actionTarget, fightingTarget);
                case 2:
                    return NonComabtTeamBuff(spell, fightingTarget, ref actionTarget);
                default:
                    return false;
            }
        }
        private bool TeamRiotControl(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["RiotControl"].AsBool()) { return false; }
            if (!Team.IsInTeam) { return false; }

            var teamMember = Team.Members.Where(t => t?.Character != null
            && t.Character.Health > 0
            && t.Character.IsInLineOfSight
            && t.Character.SpecialAttacks.Contains(SpecialAttack.Burst)
            && SpellChecksOther(spell, spell.Nanoline, t.Character)
            && spell.IsInRange(t?.Character)).FirstOrDefault();

            if (teamMember == null) { return false; }

            actionTarget = (teamMember.Character, true);
            return true;

        }
        #endregion

        #region Trader

        private bool RKNanoDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfTrader)) { return false; }

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

        private bool ACDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfTrader)) { return false; }

            if (NeedsReload()) { return false; }

            if (!_settings["Buffing"].AsBool() || !CanCast(spell) || _drainTarget == null) { return false; }

            if (ACDrainSelection.Target == (ACDrainSelection)_settings["ACDrainSelection"].AsInt32())
            {
                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            if (ACDrainSelection.Boss == (ACDrainSelection)_settings["ACDrainSelection"].AsInt32())
            {
                if (fightingTarget?.MaxHealth < 1000000) { return false; }

                return TargetDebuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

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

        private bool RansackDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfTrader)) { return false; }

            RansackSelection ransackSelection = (RansackSelection)_settings["RansackSelection"].AsInt32();

            if (NeedsReload()) { return false; }

            switch (ransackSelection)
            {
                case RansackSelection.Target:
                    return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);

                case RansackSelection.Boss:
                    if (fightingTarget?.MaxHealth < 1000000)
                    {
                        if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Ransack, out Buff playerBuffs))
                        {
                            if (spell.StackingOrder > playerBuffs.StackingOrder)
                            {
                                return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);
                            }
                            else
                            {
                                if (playerBuffs.RemainingTime > 25)
                                {
                                    return false;
                                }
                                return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);
                            }
                        }
                        else
                        {
                            return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);
                        }
                    }
                    else
                    {
                        return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);
                    }

                case RansackSelection.Area:
                    if (!_settings["Buffing"].AsBool() || !CanCast(spell) || _drainTarget == null)
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
            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.FalseProfTrader)) { return false; }

            DepriveSelection depriveSelection = (DepriveSelection)_settings["DepriveSelection"].AsInt32();

            if (NeedsReload()) { return false; }

            switch (depriveSelection)
            {
                case DepriveSelection.Target:
                    return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);

                case DepriveSelection.Boss:
                    if (fightingTarget?.MaxHealth < 1000000)
                    {
                        if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Deprive, out Buff playerBuffs))
                        {
                            if (spell.StackingOrder > playerBuffs.StackingOrder)
                            {
                                return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);
                            }
                            else
                            {
                                if (playerBuffs.RemainingTime > 25)
                                {
                                    return false;
                                }
                                return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);
                            }
                        }
                        else
                        {
                            return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);
                        }
                    }
                    else
                    {
                        return TargetDebuff(spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);
                    }

                case DepriveSelection.Area:
                    if (!_settings["Buffing"].AsBool() || !CanCast(spell) || _drainTarget == null)
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
        private bool Mezz(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var setting = _settings["TraderModeSelection"].AsInt32();
            if (setting == 0) { return false; }
            if (!CanCast(spell)) { return false; }

            switch (setting)
            {
                case 1:
                    var allTarget = DynelManager.NPCs
                    .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name)
                        && c.Health > 0
                        && c.IsInLineOfSight
                        && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                        && spell.IsInRange(c)
                        && c.MaxHealth < 1000000)
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .ThenBy(c => c.Health)
                    .FirstOrDefault();

                    if (allTarget == null) { return false; }

                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = allTarget;
                    return true;
                case 2:
                    var adds = DynelManager.NPCs
                    .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name)
                        && c.Health > 0
                        && c.IsInLineOfSight
                        && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                        && spell.IsInRange(c)
                        && c.MaxHealth < 1000000
                        && c.FightingTarget != null
                        && !AttackingMob(c)
                        && AttackingTeam(c))
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .ThenBy(c => c.Health)
                    .FirstOrDefault();

                    if (adds == null) { return false; }

                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = adds;
                    return true;
                default:
                    return false;
            }
        }
        #endregion

        #region NT
        private bool AOENuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, int aoeType)
        {
            if (fightingTarget == null || !CanCast(spell) || _settings["AOESelection"].AsInt32() != aoeType) { return false; }

            return true;
        }
        private bool NullitySphere(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Buffing"].AsBool() || !CanCast(spell)) { return false; }

            return DynelManager.LocalPlayer.HealthPercent <= NullitySpherePercentage;
        }
        private bool MatCreaBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantIgnoreNanos.CompNanoSkills)) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }
        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["AOESelection"].AsInt32() != 0 || fightingTarget == null) { return false; }

            return true;
        }
        private bool AOEBlind(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["BlindSelection"].AsInt32() != 2 || fightingTarget == null || !CanCast(spell)) { return false; }

            return !fightingTarget.Buffs.Contains(NanoLine.AAODebuffs);
        }
        private bool SingleBlind(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["BlindSelection"].AsInt32() != 1 || fightingTarget == null || !CanCast(spell)) { return false; }

            return !fightingTarget.Buffs.Contains(NanoLine.AAODebuffs);
        }
        private bool HaloNanoDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["HaloSelection"].AsInt32() == 0) { return false; }

            if (!CanCast(spell)) { return false; }
            if (fightingTarget == null) { return false; }

            if (fightingTarget != null && fightingTarget.Buffs.Contains(NanoLine.HaloNanoDebuff)) { return false; }

            switch (_settings["HaloSelection"].AsInt32())
            {
                case 1:
                    return true;
                case 3:
                    if (fightingTarget?.MaxHealth < 1000000) { return false; }
                    return true;
                default:
                    return false;
            }
        }
        #endregion

        #region Crat
        //calms
        bool ShadowlandsCalms(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["CalmingSelection"].AsInt32() != 0) { return false; }
            if (!CanCast(spell)) { return false; }

            return CalmTarget(spell, fightingTarget, ref actionTarget);
        }

        bool RkCalms(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["CalmingSelection"].AsInt32() != 1) { return false; }
            if (!CanCast(spell)) { return false; }

            return CalmTarget(spell, fightingTarget, ref actionTarget);
        }

        bool AOECalms(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["CalmingSelection"].AsInt32() != 2) { return false; }
            if (!CanCast(spell)) { return false; }

            return CalmTarget(spell, fightingTarget, ref actionTarget);
        }

        bool CalmTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var modeSelection = _settings["CratModeSelection"].AsInt32();
            if (modeSelection == 0) { return false; }

            var target = DynelManager.NPCs
                  .Where(c => c != null && !debuffAreaTargetsToIgnore.Contains(c.Name)
                      && c.Health > 0
                      && c.IsInLineOfSight
                      && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                      && spell.IsInRange(c)
                      && c.MaxHealth < 1000000)
                  .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                  .ThenBy(c => c.Health)
                  .FirstOrDefault();

            switch (modeSelection)
            {
                case 1:
                    if (target == null) { return false; }

                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = target;
                    return true;
                case 2:
                    var adds = target?.FightingTarget != null
                       && !AttackingMob(target)
                       && AttackingTeam(target);

                    if (!adds) { return false; }

                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = target;
                    return true;
                default:
                    return false;
            }
        }


        //nukes
        private bool CratSpecialNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !CanCast(spell) || !_settings["CratSpecialNuke"].AsBool()) { return false; }

            if (Spell.Find(273631, out Spell workplace))
            {
                if (!fightingTarget.Buffs.Contains(273632) && !fightingTarget.Buffs.Contains(301842) &&
                    ((fightingTarget.HealthPercent >= 40 && fightingTarget.MaxHealth < 1000000)
                    || fightingTarget.MaxHealth > 1000000)) { return false; }
            }

            return true;
        }
        private bool CratNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !CanCast(spell) || !_settings["CratNuke"].AsBool()) { return false; }

            if (Spell.Find(273631, out Spell workplace))
            {
                if (!fightingTarget.Buffs.Contains(273632) && !fightingTarget.Buffs.Contains(301842) &&
                    ((fightingTarget.HealthPercent >= 40 && fightingTarget.MaxHealth < 1000000)
                    || fightingTarget.MaxHealth > 1000000)) { return false; }
            }

            return true;
        }
        #endregion

        #region Fixer


        #endregion

        #region MA
        private bool ControlledDestructionWithShutdown(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["ControlledDestructionWithShutdown"].AsBool() || fightingTarget == null
                || DynelManager.LocalPlayer.HealthPercent < 100) { return false; }

            return CombatBuff(spell, NanoLine.ControlledDestructionBuff, fightingTarget, ref actionTarget);
        }
        private bool MADamageType(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, int procType)
        {
            var currentSetting = _settings["DamageTypeSelection"].AsInt32();

            if (currentSetting != procType)
            {
                return false;
            }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }
        private void CancelBuffs()
        {
            var selection = _settings["DamageTypeSelection"].AsInt32();

            if (selection != 1)
            {
                CancelBuffs(RelevantNanos.DamageTypeFire);
            }
            if (selection != 2)
            {
                CancelBuffs(RelevantNanos.DamageTypeEnergy);
            }
            if (selection != 3)
            {
                CancelBuffs(RelevantNanos.DamageTypeChemical);
            }
        }
        #endregion
        #region Misc

        private void FPSwitch()
        {
            switch (_settings["FalseProfSelection"].AsInt32())
            {
                case 1:
                    FPSwitchSetting = RelevantNanos.FalseProfMp;
                    break;
                case 2:
                    FPSwitchSetting = RelevantNanos.FalseProfSol;
                    break;
                case 3:
                    FPSwitchSetting = RelevantNanos.FalseProfEnf;
                    break;
                case 4:
                    FPSwitchSetting = RelevantNanos.FalseProfEng;
                    break;
                case 5:
                    FPSwitchSetting = RelevantNanos.FalseProfDoc;
                    break;
                case 6:
                    FPSwitchSetting = RelevantNanos.FalseProfFixer;
                    break;
                case 7:
                    FPSwitchSetting = RelevantNanos.FalseProfCrat;
                    break;
                case 8:
                    FPSwitchSetting = RelevantNanos.FalseProfMa;
                    break;
                case 9:
                    FPSwitchSetting = RelevantNanos.FalseProfNt;
                    break;
                case 10:
                    FPSwitchSetting = RelevantNanos.FalseProfTrader;
                    break;
                case 11:
                    FPSwitchSetting = RelevantNanos.FalseProfAdv;
                    break;
                default:
                    FPSwitchSetting = new int[0];
                    break;
            }
        }

        private void FP()
        {
            FPSwitch();

            if (FPSwitchSetting.Length == 0) { return; }

            var fpBuff = Spell.List.FirstOrDefault(h => FPSwitchSetting.Contains(h.Id));

            if (fpBuff != null)
            {
                if (!Spell.HasPendingCast && !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.FalseProfession)
                                && fpBuff.MeetsUseReqs() && DynelManager.LocalPlayer.MovementStatePermitsCasting)
                {
                    fpBuff.Cast(DynelManager.LocalPlayer, true);
                }
            }
        }

        private void AssignTargetToHealPet()
        {
            var dyingTarget = GetTargetToHeal();

            var healPet = DynelManager.LocalPlayer.Pets.Where(pet => pet.Type == PetType.Heal
            && pet.Character.Nano >= 1).FirstOrDefault();

            if (healPet != null)
            {
                if (dyingTarget != null)
                {
                    if (Time.AONormalTime > _lastHealPetHealTime)
                    {
                        healPet.Heal(dyingTarget.Identity);
                        _lastHealPetHealTime = Time.AONormalTime + 3;
                    }
                }
            }
        }

        private SimpleChar GetTargetToHeal()
        {
            if (DynelManager.LocalPlayer.HealthPercent < 90)
            {
                var dyingTeamMember = DynelManager.Players
                    .Where(c => c.IsAlive && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.HealthPercent < 90)
                    .OrderBy(c => c.HealthPercent)
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    return dyingTeamMember;
                }
            }
            else
            {
                Pet dyingPet = DynelManager.LocalPlayer.Pets
                     .Where(pet => pet.Type == PetType.Attack || pet.Type == PetType.Social || pet.Type == PetType.Support)
                     .Where(pet => pet.Character.HealthPercent < 80)
                     .Where(pet => pet.Character.DistanceFrom(DynelManager.LocalPlayer) < 60f)
                     .OrderBy(pet => pet.Character.HealthPercent)
                     .FirstOrDefault();

                if (dyingPet != null)
                {
                    return dyingPet.Character;
                }
            }

            return null;
        }
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
            public static readonly int[] DetauntProcs = { 226437, 226435, 226433, 226431, 226429, 226427 };
            public static readonly int[] DOTProcs = { 226425, 226423, 226421, 226419, 226417, 226415, 226413, 226410 };

            public static readonly int[] FalseProfMp = { 117208, 117219, 32036 };
            public static readonly int[] FalseProfSol = { 117216, 117227, 32038 };
            public static readonly int[] FalseProfEnf = { 117217, 117228, 32041 };
            public static readonly int[] FalseProfEng = { 117213, 117224, 32034 };
            public static readonly int[] FalseProfDoc = { 117210, 117221, 32033 };
            public static readonly int[] FalseProfFixer = { 117212, 117223, 32039 };
            public static readonly int[] FalseProfCrat = { 117209, 117220, 32032 };
            public static readonly int[] FalseProfMa = { 117215, 117226, 32035 };
            public static readonly int[] FalseProfNt = { 117207, 117218, 32037 };
            public static readonly int[] FalseProfTrader = { 117211, 117222, 32040 };
            public static readonly int[] FalseProfAdv = { 117214, 117225, 32030 };

            public static readonly int[] TeamCritBuffs = { 160791, 160789, 160787 };
            public static int AssassinsAimedShot = 275007;
            public static int SteadyNerves = 160795;
            public const int SuperiorHoldVictim = 270249;
            public const int GreaterDelayPursuers = 85316;
            public const int GreaterDelayTheInevitable = 82545;
            public static readonly int[] AgentDamageBuffs = { 81856, 222838, 222837, 81851, 222835, 81852, 222833, 81853, 81854 };

            public static int CompleteHealing = 28650;

            public const int TiredLimbs = 99578;

            public static readonly Spell[] InitDebuffs = Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder().Where(spell => spell.Id != TiredLimbs).ToArray();

            //Adv
            public static int[] AdvyTargetHeals = new[] { 223167, 252008, 252006, 136674, 136673, 143908, 82059, 136675, 136676, 82060, 136677,
                136678, 136679, 136682, 82061, 136681, 136680, 136683, 136684, 136685, 82062, 136686, 136689, 82063, 136688, 136687,
                82064, 26695 };
            public static int[] AdvyTeamHeals = new[] { 273285, 270770, 82051, 82052, 82053, 82054, 82055, 82056, 82057, 82057, 82058, 26696 };
            public const int AdvyCompleteHeal = 136672;
            public static readonly int[] ArmorBuffs = { 74173, 74174, 74175, 74176, 74177, 74178 };

            public static readonly int[] DragonMorph = { 217670, 25994 };
            public static readonly int[] LeetMorph = { 263278, 82834 };
            public static readonly int[] WolfMorph = { 275005, 85062 };
            public static readonly int[] SaberMorph = { 217680, 85070 };
            public static readonly int[] TreeMorph = { 229666, 229884, 229887, 229889 };
            public static readonly int[] Morphs = { 217670, 25994, 263278, 82834, 275005, 85062, 217680, 85070, 229666, 229884, 229887, 229889 };
            public static readonly int[] BirdMorph = { 25997, 85066, 82835 };
            public static readonly int[] TreeBuff = { 302223, 302220 };
            public static readonly int[] DragonScales = { 302217, 302214 };
            public static readonly int[] WolfAgility = { 302235, 302232 };
            public static readonly int[] LeetCrit = { 302229, 302226 };
            public static readonly int[] SaberDamage = { 302243, 302240 };
            public static readonly int[] SaberBuff = { 162313, 162315, 162317, 162319, 162321 };
            public static readonly int[] CatDamage = { 162321, 162319, 162317, 162315, 162313, };

            //Doc
            public const int AlphaAndOmega = 42409;
            public static int[] DocCompleteTargetHealing = new[] { 270747, 28650 };

            public static int[] DocHeals = new[] {  223299, 223297, 223295, 223293, 223291, 223289, 223287, 223285, 223281, 43878, 43881, 43886, 43885,
                        43887, 43890, 43884, 43808, 43888, 43889, 43883, 43811, 43809, 43810, 28645, 43816, 43817, 43825, 43815,
                        43814, 43821, 43820, 28648, 43812, 43824, 43822, 43819, 43818, 43823, 28677, 43813, 43826, 43838, 43835,
                        28672, 43836, 28676, 43827, 43834, 28681, 43837, 43833, 43830, 43828, 28654, 43831, 43829, 43832, 28665 };

            public static int[] DocTeamHeals = new[]
            { 273312, 273315, 270349, 43891, 223291, 43892, 43893, 43894, 43895, 43896, 43897, 43898, 43899,
                        43900, 43901, 43903, 43902, 42404, 43905, 43904, 42395, 43907, 43908, 43906, 42398, 43910, 43909, 42402,
                        43911, 43913, 42405, 43912, 43914, 43915, 27804, 43916, 43917, 42403, 42408
            };

            public const int TeamImprovedLifeChanneler = 275011;
            public const int EpsilonPurge = 28659;
            public static readonly Spell[] IndividualShortMaxHealths = Spell.GetSpellsForNanoline(NanoLine.DoctorShortHPBuffs).OrderByStackingOrder()
                 .Where(spell => spell.Id != TeamImprovedLifeChanneler).ToArray();
            public static int[] HPBuffs = new[] { 95709, 28662, 95720, 95712, 95710, 95711, 28649, 95713, 28660, 95715, 95714, 95718, 95716, 95717, 95719, 42397 };

            //mp
            public static readonly int[] CostBuffs = { 95409, 29307, 95411, 95408, 95410 };
            public static readonly int[] InstillDamageBuffs = { 270800, 285101, 116814, 116817, 116812, 116816, 116821, 116815, 116813 };
            public static readonly int[] ChantBuffs = { 116819, 116818, 116811, 116820 };
            public static readonly int[] PetShortTermDamage = { 267598, 205193, 151827, 205189, 205187, 151828, 205185, 151824, 205183,
            151830, 205191, 151826, 205195, 151825, 205197, 151831 };
            public const int PetWarp = 209488;
            public static readonly int[] WarmUpfNukes = { 270355, 125761, 29297, 125762, 29298, 29114 };
            public static readonly int[] MPNukes = { 267878, 125763, 125760, 125765, 125764 };

            //enf
            public static readonly int[] SingleTargetTaunt = { 223121, 223119, 223117, 223115, 100209, 100210, 100212, 100211, 100213 };
            public const int CORUSCATING_SCREEN = 55751;
            public static readonly int[] FortifyBuffs = { 273320, 270350, 117686, 117688, 117682, 117687, 117685, 117684, 117683, 117680, 117681 };

            //Trader
            public const int QuantumUncertanity = 30745;
            public const int FlowofTime = 30719;

            //NT
            public static readonly int[] RKAOENukes = { 28620, 28638, 28637, 28594, 45922, 45906, 45884, 28635, 28593, 45925, 45940,
            45900, 28629, 45917, 45937, 28599, 45894, 45943, 28633, 28631};
            public const int VolcanicEruption = 28638;
            public static readonly int[] HaloNanoDebuff = { 45239, 45238, 45224 };
            public static readonly int[] SingleTargetNukes = { 45226, 45192, 45230, 28623, 28604, 28616, 28597, 45210, 45236, 45197,
                45233, 45247, 45199, 45235, 45234, 45258, 45217, 28600, 45198, 28613, 45919, 45195, 45225, 45260, 45891, 45254, 45890,
                45213, 45215, 45915, 45252, 45214, 45929, 45251, 45220, 45920, 45222, 45911, 28598, 45237, 45216, 45913, 45901, 45212,
                45912, 45206, 45883, 45245, 45904, 45140, 45218, 28626, 45261, 45909, 45203,
                45903, 45228, 45200, 45939, 28592, 45242, 45885, 45926, 45241, 45908, 44538, 45934, 45250, 45138, 45932,
                28632, 45205, 28609, 45209, 45246, 45935, 45921, 45227, 45207, 45942, 45924, 45191, 28610, 45914, 45893, 45208,
                28621, 45933, 45916, 45211, 45240, 45941, 45259, 45910, 45253, 28614, 45221, 28634, 45204, 45886, 45196,
                45928, 45201, 45193, 45323, 45889, 45895, 45244, 28605, 45219, 45938, 45223, 28628, 45232, 45248, 45898, 45923, 45202,
                45229, 45907, 45139, 45887, 45231, 45882, 28627, 45936, 45194, 28639, 45931, 45243, 28630, 45137, 28607, 45257, 45880,
                45256, 45249, 45888, 45881, 45255, 45927, 42543, 45902, 42540, 42541, 45899, 45905, 28611, 45897, 28601, 28608,
                45918, 45892, 45930, 45896, 28612 };
            public static readonly int[] AOEBlinds = { 83959, 83960, 83961, 83962, 83963, 83964 };

            //crat
            public const int GreaterFearofAttention = 82166;
            public const int ShacklesofObedience = 82463;
            public static readonly int[] ShadowlandsCalms = { 224147, 224145,
            224137, 224135, 224133, 224131, 219020 };
            public static readonly int[] RkCalms = { 155577, 100428, 100429, 100430, 100431, 100432,
            30093, 30056, 30065 };
            public static readonly int[] AOECalms = { 100422, 100424, 100426 };
            public static readonly int[] CratNuke = { 82000, 78396, 78397, 30091, 78399, 81996, 30083, 81997, 30068, 81998, 78398, 29618 };
            public static readonly int[] CratSpecialNuke = { 78400, 30082, 78394, 78395 };

            //fixer
            public const int SpinNanoweb = 85216;
            public const int GravityBindings = 82502;

            //MA
            public static int[] DamageTypeFire = { 81827, 81824, 28876 };
            public static int[] DamageTypeEnergy = { 81825, 81823, 81826, 81829 };
            public static int[] DamageTypeChemical = { 81822, 81830 };
            public static int[] TargetEvades = { 28903, 28878, 28872 };

        }

        #region Agent LE procs
        public enum ProcType1Selection
        {
            GrimReaper = 1464554561,
            DisableCuffs = 1347310415,
            NoEscape = 1162433352,
            IntenseMetabolism = 1229538903,
            MinorNanobotEnhance = 1464160321
        }

        public enum ProcType2Selection
        {
            NotumChargedRounds = 1196774223,
            LaserAim = 1111577165,
            NanoEnhancedTargeting = 1095520333,
            PlasteelPiercingRounds = 1280136015,
            CellKiller = 1329941332,
            ImprovedFocus = 1413562956,
            BrokenAnkle = 1481851730
        }
        #endregion

        #region Trader Selections

        public enum RansackSelection
        {
            None, Target, Area, Boss
        }
        public enum DepriveSelection
        {
            None, Target, Area, Boss
        }
        public enum RKNanoDrainSelection
        {
            None, Target, Boss
        }
        public enum ACDrainSelection
        {
            None, Target, Area, Boss
        }

        #endregion

        private static class RelevantIgnoreNanos
        {
            public static int[] CompNanoSkills = new[] { 220331, 220333, 220335, 220337, 292299, 220339, 220341, 220343 };

        }

        #endregion
    }
}
