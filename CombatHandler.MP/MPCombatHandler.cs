using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CombatHandler.Metaphysicist
{
    public class MPCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleRez = false;

        public static bool _syncPets;

        private static Window _buffWindow;
        private static Window _debuffWindow;
        private static Window _petWindow;
        private static Window _petCommandWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;
        private static Window _nukesWindow;
        private static Window _weaponWindow;
        private static Window _healingWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _petView;
        private static View _petCommandView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;
        private static View _nukesView;
        private static View _weaponView;
        private static View _healingView;

        private double _lastHealPetHealTime = 0.0;
        private double _lastMezzPetMezzTime = 0.0;

        private static double _ncuUpdateTime;
        public static double weaponDelay;
        double weaponCheckDelay;

        public static List<string> allWeaponNames = new List<string>
            {
                "Azure Cobra of Orma",
                "Wixel's Notum Python",
                "Asp of Semol",
                "Viper Staff",
                "Asp of Titaniush",
                "Gold Acantophis",
                "Bitis Striker",
                "Coplan's Hand Taipan",
                "The Crotalus",
                "Shield of Zset",
                "Shield of Esa",
                "Shield of Asmodian",
                "Mocham's Guard",
                "Death Ward",
                "Belthior's Flame Ward",
                "Wave Breaker",
                "Solar Guard",
                "Notum Defender",
                "Vital Buckler",
                "Living Shield of Evernan"
            };

        public MPCombatHandler(string pluginDir) : base(pluginDir)
        {
            try
            {
                IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalBuffing, OnGlobalBuffingMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalComposites, OnGlobalCompositesMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.GlobalRez, OnGlobalRezMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetAttack, OnPetAttack);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetWait, OnPetWait);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetFollow, OnPetFollow);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetWarp, OnPetWarp);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetSyncOn, SyncPetsOnMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.PetSyncOff, SyncPetsOffMessage);
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

                _settings.AddVariable("AllPlayers", false);
                _settings["AllPlayers"] = false;

                _settings.AddVariable("Buffing", true);
                _settings.AddVariable("Composites", true);

                _settings.AddVariable("GlobalBuffing", true);
                _settings.AddVariable("GlobalComposites", true);
                _settings.AddVariable("GlobalRez", true);

                _settings.AddVariable("AOEPerks", false);

                _settings.AddVariable("SharpObjects", true);
                _settings.AddVariable("Grenades", true);

                _settings.AddVariable("TauntTool", false);

                _settings.AddVariable("StimTargetSelection", (int)StimTargetSelection.Self);

                _settings.AddVariable("Kits", true);

                _settings.AddVariable("Sacrificial", false);

                _settings.AddVariable("SyncPets", true);
                _settings.AddVariable("SpawnPets", true);
                _settings.AddVariable("BuffPets", true);
                _settings.AddVariable("CEPetBuff", false);
                _settings.AddVariable("MezzPet", false);
                _settings.AddVariable("WarpPets", false);

                _settings.AddVariable("PetProcSelection", (int)PetProcSelection.None);
                _settings.AddVariable("PetMezzingSelection", (int)PetMezzingSelection.Adds);

                _settings.AddVariable("CompositeNanoSkillsBuffSelection", (int)CompositeNanoSkillsBuffSelection.None);
                _settings.AddVariable("CostBuffSelection", (int)CostBuffSelection.Self);
                _settings.AddVariable("InterruptSelection", (int)InterruptSelection.None);

                _settings.AddVariable("DamageDebuffSelection", (int)DamageDebuffSelection.None);
                _settings.AddVariable("DamageDebuffASelection", (int)DamageDebuffASelection.None);
                _settings.AddVariable("DamageDebuffBSelection", (int)DamageDebuffBSelection.None);

                _settings.AddVariable("NanoResistanceDebuffSelection", (int)NanoResistanceDebuffSelection.None);
                _settings.AddVariable("NanoShutdownDebuffSelection", (int)NanoShutdownDebuffSelection.None);

                _settings.AddVariable("SummonedWeaponSelection", (int)SummonedWeaponSelection.None);

                //_settings.AddVariable("CompositesNanoSkills", false);
                //_settings.AddVariable("CompositesNanoSkillsTeam", false);

                _settings.AddVariable("Evades", false);
                _settings.AddVariable("PistolTeam", false);
                _settings.AddVariable("SLMap", false);

                _settings.AddVariable("DamagePerk", false);

                //LE Proc
                _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.AnticipatedEvasion);
                _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.DiffuseRage);

                _settings.AddVariable("Replenish", false);

                //_settings.AddVariable("MatterCrea", false);
                //_settings.AddVariable("PyschoModi", false);
                //_settings.AddVariable("TimeSpace", false);
                //_settings.AddVariable("SenseImprov", false);
                //_settings.AddVariable("BioMet", false);
                //_settings.AddVariable("MattMet", false);

                _settings.AddVariable("Nukes", false);
                _settings.AddVariable("NormalNuke", false);
                _settings.AddVariable("DebuffNuke", false);

                //settings.AddVariable("NanoBuffsSelection", (int)NanoBuffsSelection.SL);

                RegisterSettingsWindow("MP Handler", "MPSettingsView.xml");

                //Debuffs
                //nukes
                RegisterSpellProcessor(RelevantNanos.WarmUpfNukes, WarmUpNuke, CombatActionPriority.Medium);
                RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke);

                //debuffs
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MetaPhysicistDamageDebuff).OrderByStackingOrder(),
                   (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "DamageDebuffSelection"), CombatActionPriority.High);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPDamageDebuffLineA).OrderByStackingOrder(),
                   (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "DamageDebuffASelection"), CombatActionPriority.High);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPDamageDebuffLineB).OrderByStackingOrder(),
                   (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "DamageDebuffBSelection"), CombatActionPriority.High);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceDebuff_LineA).OrderByStackingOrder(),
                   (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "NanoResistanceDebuffSelection"), CombatActionPriority.High);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoShutdownDebuff).OrderByStackingOrder(),
                   (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "NanoShutdownDebuffSelection"), CombatActionPriority.High);

                //Buffs
                //self buffs
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), SelfEvades);
                RegisterSpellProcessor(RelevantNanos.Sacrificial, Sacrificial);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtistBowBuffs).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Psy_IntBuff).OrderByStackingOrder(),
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                                => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));

                //weapons
                RegisterSpellProcessor(RelevantNanos.TwoHanded, TwoHandedWeapon);
                RegisterSpellProcessor(RelevantNanos.OneHanded, OneHandedWeapon);
                RegisterSpellProcessor(RelevantNanos.Shield, ShieldWeapon);

                //team buffs
                RegisterSpellProcessor(RelevantNanos.MPCompositeNano, NanoCompBuff);

                RegisterSpellProcessor(RelevantNanos.AnticipationofRetaliation, Evades);

                RegisterSpellProcessor(RelevantNanos.PetWarp, PetWarp);

                //RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InterruptModifier).OrderByStackingOrder(),
                //    (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                //    => Replenish(buffSpell, fightingTarget, ref actionTarget, "InterruptSelection"));

                RegisterSpellProcessor(RelevantNanos.MatMetBuffs, MattMet);
                RegisterSpellProcessor(RelevantNanos.BioMetBuffs, BioMet);
                RegisterSpellProcessor(RelevantNanos.PsyModBuffs, PsyMod);
                RegisterSpellProcessor(RelevantNanos.SenImpBuffs, SenImp);
                RegisterSpellProcessor(RelevantNanos.MatCreBuffs, MatCre);
                RegisterSpellProcessor(RelevantNanos.MatLocBuffs, MatLoc);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InterruptModifier).OrderByStackingOrder(),
                    (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "InterruptSelection"));
                
                RegisterSpellProcessor(RelevantNanos.CostBuffs, Cost);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolTeam);

                //Pets
                RegisterSpellProcessor(GetAttackPetsWithSLPetsFirst(), AttackPetSpawner, CombatActionPriority.High);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SupportPets).OrderByStackingOrder(), SupportPetSpawner);
                RegisterSpellProcessor(RelevantNanos.HealPets, HealPetSpawner, CombatActionPriority.High);

                //Pet Buffs
                RegisterSpellProcessor(RelevantNanos.PetCleanse, PetCleanse, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.MastersBidding, MastersBidding, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.InducedApathy, InducedApathy, CombatActionPriority.High);

                RegisterSpellProcessor(RelevantNanos.AnticipationofRetaliation, EvasionPet, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.InstillDamageBuffs, InstillDamage, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.ChantBuffs, Chant, CombatActionPriority.High);

                RegisterSpellProcessor(RelevantNanos.MPCompositeNano, MezzPetMochies, CombatActionPriority.High);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MesmerizationConstructEmpowerment).OrderByStackingOrder(), MezzPetSeed, CombatActionPriority.High);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealingConstructEmpowerment).OrderByStackingOrder(), HealPetSeed, CombatActionPriority.High);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AggressiveConstructEmpowerment).OrderByStackingOrder(), AttackPetSeed, CombatActionPriority.High);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPAttackPetDamageType).OrderByStackingOrder(), DamageTypePet, CombatActionPriority.High);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDamageOverTimeResistNanos).OrderByStackingOrder(), NanoResistancePet, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.PetDefensive, DefensivePet, CombatActionPriority.High);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetHealDelta843).OrderByStackingOrder(), HealDeltaPet, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.PetShortTermDamage, ShortTermDamagePet, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.CostBuffs, CostPet, CombatActionPriority.High);

                //Pet Perks

                //LE Proc
                RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistNanobotContingentArrest, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistAnticipatedEvasion, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistThoughtfulMeans, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistRegainFocus, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistEconomicNanobotUse, LEProc1, CombatActionPriority.Low);

                RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistSuperEgoStrike, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistSuppressFury, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistEgoStrike, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistMindWail, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistSowDoubt, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistSowDespair, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcMetaPhysicistDiffuseRage, LEProc2, CombatActionPriority.Low);

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

        public Window[] _windows => new Window[] { _petWindow, _petCommandWindow, _buffWindow, _healingWindow, _debuffWindow, _itemWindow, _perkWindow, _nukesWindow, _weaponWindow };

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

        public static void OnPetAttack(int sender, IPCMessage msg)
        {
            PetAttackMessage attackMsg = (PetAttackMessage)msg;
            DynelManager.LocalPlayer.Pets.Attack(attackMsg.Target);
        }

        private static void OnPetWait(int sender, IPCMessage msg)
        {
            if (DynelManager.LocalPlayer.Pets.Length > 0)
            {
                foreach (Pet pet in DynelManager.LocalPlayer.Pets)
                {
                    pet.Wait();
                }
            }
        }

        private static void OnPetWarp(int sender, IPCMessage msg)
        {
            if (DynelManager.LocalPlayer.Pets.Length > 0)
            {
                Spell warp = Spell.List.FirstOrDefault(x => RelevantNanos.Warps.Contains(x.Id));
                warp?.Cast(DynelManager.LocalPlayer, false);
            }
        }

        private static void OnPetFollow(int sender, IPCMessage msg)
        {
            if (DynelManager.LocalPlayer.Pets.Length > 0)
            {
                foreach (Pet pet in DynelManager.LocalPlayer.Pets)
                {
                    pet.Follow();
                }
            }
        }

        #endregion

        #region Handles

        private void HandleHealingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_healingView)) { return; }

                _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\MPHealingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Healing", XmlViewName = "MPHealingView" }, _healingView);

                window.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);

                if (FountainOfLifeInput != null)
                {
                    FountainOfLifeInput.Text = $"{Healing.FountainOfLifeHealPercentage}";
                }
            }
            else if (_healingWindow == null || (_healingWindow != null && !_healingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_healingWindow, PluginDir, new WindowOptions() { Name = "Healing", XmlViewName = "MPHealingView" }, _healingView, out var container);
                _healingWindow = container;

                container.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);

                if (FountainOfLifeInput != null)
                {
                    FountainOfLifeInput.Text = $"{Healing.FountainOfLifeHealPercentage}";
                }
            }
        }
        private void PetAttackClicked(object s, ButtonBase button)
        {
            if (DynelManager.LocalPlayer.Pets.Length > 0)
            {
                foreach (Pet pet in DynelManager.LocalPlayer.Pets.Where(c => c.Type != PetType.Heal))
                {
                    pet.Attack((Identity)Targeting.Target?.Identity);
                    IPCChannel.Broadcast(new PetAttackMessage()
                    {
                        Target = (Identity)Targeting.Target?.Identity
                    });
                }
            }
        }
        private void PetWaitClicked(object s, ButtonBase button)
        {
            PetWaitCommand(null, null, null);
        }
        private void PetWarpClicked(object s, ButtonBase button)
        {
            PetWarpCommand(null, null, null);
        }
        private void PetFollowClicked(object s, ButtonBase button)
        {
            PetFollowCommand(null, null, null);
        }
        private void HandlePetViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_petView)) { return; }

                _petView = View.CreateFromXml(PluginDirectory + "\\UI\\MPPetsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Pets", XmlViewName = "MPPetsView" }, _petView);
            }
            else if (_petWindow == null || (_petWindow != null && !_petWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_petWindow, PluginDir, new WindowOptions() { Name = "Pets", XmlViewName = "MPPetsView" }, _petView, out var container);
                _petWindow = container;
            }
        }
        private void HandlePetCommandViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_petCommandView)) { return; }

                _petCommandView = View.CreateFromXml(PluginDirectory + "\\UI\\MPPetCommandView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Commands", XmlViewName = "MPPetCommandView" }, _petCommandView);
            }
            else if (_petCommandWindow == null || (_petCommandWindow != null && !_petCommandWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_petCommandWindow, PluginDir, new WindowOptions() { Name = "Commands", XmlViewName = "MPPetCommandView" }, _petCommandView, out var container);
                _petCommandWindow = container;
            }
        }
        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_perkView)) { return; }

                _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\MPPerksView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Perks", XmlViewName = "MPPerksView" }, _perkView);

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
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "MPPerksView" }, _perkView, out var container);
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
        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_buffView)) { return; }

                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\MPBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "MPBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "MPBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }
        private void HandleWeaponViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_weaponView)) { return; }

                _weaponView = View.CreateFromXml(PluginDirectory + "\\UI\\MPWeaponView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Weapon", XmlViewName = "MPWeaponView" }, _weaponView);
            }
            else if (_weaponView == null || (_weaponView != null && !_weaponWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_weaponWindow, PluginDir, new WindowOptions() { Name = "Weapon", XmlViewName = "MPWeaponView " }, _weaponView, out var container);
                _weaponWindow = container;
            }
        }
        private void HandleDebuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_debuffView)) { return; }

                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\MPDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "MPDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "MPDebuffsView" }, _debuffView, out var container);
                _debuffWindow = container;
            }
        }
        private void HandleItemViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_itemView)) { return; }

                _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\MPItemsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Items", XmlViewName = "MPItemsView" }, _itemView);

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
                SettingsController.CreateSettingsTab(_itemWindow, PluginDir, new WindowOptions() { Name = "Items", XmlViewName = "MPItemsView" }, _itemView, out var container);
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

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\MPProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "MPProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "MPProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }
        private void HandleNukesViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_nukesView)) { return; }

                _nukesView = View.CreateFromXml(PluginDirectory + "\\UI\\MPNukesView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Nukes", XmlViewName = "MPNukesView" }, _nukesView);
            }
            else if (_nukesWindow == null || (_nukesWindow != null && !_nukesWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_nukesWindow, PluginDir, new WindowOptions() { Name = "Nukes", XmlViewName = "MPNukesView" }, _nukesView, out var container);
                _nukesWindow = container;
            }
        }

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            try
            {
                if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 2.8) { return; }

                base.OnUpdate(deltaTime);

                if (Time.NormalTime > _ncuUpdateTime + 2.0f)
                {
                    RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                    IPCChannel.Broadcast(ncuMessage);

                    OnRemainingNCUMessage(0, ncuMessage);

                    _ncuUpdateTime = Time.NormalTime;
                }

                if (_settings["SyncPets"].AsBool())
                {
                    SynchronizePetCombatStateWithOwner(PetType.Attack, PetType.Support);
                }

                if (CanLookupPetsAfterZone())
                {
                    AssignTargetToHealPet();
                    AssignTargetToMezzPet();
                }

                if ((SummonedWeaponSelection)_settings["SummonedWeaponSelection"].AsInt32() != SummonedWeaponSelection.None)
                {
                    if (Time.AONormalTime > weaponCheckDelay && !DynelManager.LocalPlayer.IsAttacking && !Spell.HasPendingCast && Spell.List.Any(s => s.IsReady))
                    {
                        //Chat.WriteLine("checking weapon in tick");
                        if (HasWeapon())
                        {
                            foreach (Item weapon in Inventory.Items)
                            {
                                if (allWeaponNames.Contains(weapon.Name))
                                {
                                    List<EquipSlot> slot = weapon.EquipSlots;

                                    if (weapon.Slot.Type != IdentityType.WeaponPage)
                                    {
                                        if (Time.AONormalTime > weaponDelay + 10)
                                        {
                                            foreach (EquipSlot equipSlot in weapon.EquipSlots)
                                            {
                                                //Chat.WriteLine($"Weapon: {weapon.Name}, Slot: {equipSlot}");
                                                weapon.Equip(equipSlot);
                                                break;
                                            }

                                            weaponDelay = Time.AONormalTime;
                                        }
                                    }
                                }
                            }
                        }

                        weaponCheckDelay = Time.AONormalTime + 10;

                    }
                    
                }

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

                    //attack
                    if (window.FindView("CombatHandlerPetAttack", out Button PetAttack))
                    {
                        PetAttack.Tag = window;
                        PetAttack.Clicked = PetAttackClicked;
                    }

                    //wait
                    if (window.FindView("CombatHandlerPetWait", out Button PetWait))
                    {
                        PetWait.Tag = window;
                        PetWait.Clicked = PetWaitClicked;
                    }

                    //warp
                    if (window.FindView("CombatHandlerPetWarp", out Button PetWarp))
                    {
                        PetWarp.Tag = window;
                        PetWarp.Clicked = PetWarpClicked;
                    }

                    //follow
                    if (window.FindView("CombatHandlertPetFollow", out Button PetFollow))
                    {
                        PetFollow.Tag = window;
                        PetFollow.Clicked = PetFollowClicked;
                    }
                }

                //if (_settings["Replenish"].AsBool() && (_settings["CompositesNanoSkills"].AsBool() || _settings["CompositesNanoSkillsTeam"].AsBool()))
                //{
                //    _settings["CompositesNanoSkills"] = false;
                //    _settings["CompositesNanoSkillsTeam"] = false;
                //    _settings["Replenish"] = false;

                //    Chat.WriteLine("Only activate one option.");
                //}

                if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
                {
                    if (SettingsController.settingsWindow.FindView("PetsView", out Button petView))
                    {
                        petView.Tag = SettingsController.settingsWindow;
                        petView.Clicked = HandlePetViewClick;
                    }

                    if (SettingsController.settingsWindow.FindView("PetCommandView", out Button petCommandView))
                    {
                        petCommandView.Tag = SettingsController.settingsWindow;
                        petCommandView.Clicked = HandlePetCommandViewClick;
                    }

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

                    if (SettingsController.settingsWindow.FindView("NukesView", out Button nukesView))
                    {
                        nukesView.Tag = SettingsController.settingsWindow;
                        nukesView.Clicked = HandleNukesViewClick;
                    }

                    if (SettingsController.settingsWindow.FindView("WeaponView", out Button weaponView))
                    {
                        weaponView.Tag = SettingsController.settingsWindow;
                        weaponView.Clicked = HandleWeaponViewClick;
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
        private bool WarmUpNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Nukes"].AsBool()) { return false; }

            if (!_settings["DebuffNuke"].AsBool()) { return false; }

            if (fightingTarget == null || !CanCast(spell)) { return false; }

            if (_settings["NormalNuke"].AsBool() && fightingTarget.Buffs.Contains(NanoLine.MetaphysicistMindDamageNanoDebuffs)) { return false; }

            return true;
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Nukes"].AsBool()) { return false; }

            if (!_settings["NormalNuke"].AsBool()) { return false; }

            if (fightingTarget == null || !CanCast(spell)) { return false; }

            if (_settings["DebuffNuke"].AsBool() && !fightingTarget.Buffs.Contains(NanoLine.MetaphysicistMindDamageNanoDebuffs)) { return false; }

            return true;
        }

        #endregion

        #region Debufs

        #endregion

        #region Pets

        private bool AttackPetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 0) { return false; }

            return NoShellPetSpawner(PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool SupportPetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 0 || !_settings["MezzPet"].AsBool()) { return false; }

            return NoShellPetSpawner(PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool HealPetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 0) { return false; }

            return NoShellPetSpawner(PetType.Heal, spell, fightingTarget, ref actionTarget);
        }

        #region Buffs

        private bool MezzPetMochies(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.SenseImpBuff, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool MezzPetSeed(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["CEPetBuff"].AsBool()) { return false; }

            return PetTargetBuff(NanoLine.MesmerizationConstructEmpowerment, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool HealPetSeed(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["CEPetBuff"].AsBool()) { return false; }

            return PetTargetBuff(NanoLine.HealingConstructEmpowerment, PetType.Heal, spell, fightingTarget, ref actionTarget);
        }

        private bool AttackPetSeed(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["CEPetBuff"].AsBool()) { return false; }

            return PetTargetBuff(NanoLine.AggressiveConstructEmpowerment, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool DamageTypePet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.MPAttackPetDamageType, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool EvasionPet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.MajorEvasionBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(NanoLine.MajorEvasionBuffs, PetType.Heal, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(NanoLine.MajorEvasionBuffs, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool Chant(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.MPPetInitiativeBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool InstillDamage(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.MPPetDamageBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool HealDeltaPet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetHealDelta843, PetType.Attack, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(NanoLine.PetHealDelta843, PetType.Heal, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(NanoLine.PetHealDelta843, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool DefensivePet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetDefensiveNanos, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool NanoResistancePet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetDamageOverTimeResistNanos, PetType.Attack, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(NanoLine.PetDamageOverTimeResistNanos, PetType.Heal, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(NanoLine.PetDamageOverTimeResistNanos, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool ShortTermDamagePet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.PetShortTermDamageBuffs, PetType.Attack, spell, fightingTarget, ref actionTarget);
        }

        private bool CostPet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return PetTargetBuff(NanoLine.NPCostBuff, PetType.Heal, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(NanoLine.NPCostBuff, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool InducedApathy(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["BuffPets"].AsBool() || !CanLookupPetsAfterZone()) { return false; }

            if (!CanCast(spell)) { return false; }

            if (PetProcSelection.InducedApathy != (PetProcSelection)_settings["PetProcSelection"].AsInt32()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null || pet.Type != PetType.Attack) continue;

                if (!pet.Character.Buffs.Contains(NanoLine.SiphonBox683))
                {
                    if (spell.IsReady)
                    {
                        spell.Cast(pet.Character, true);
                    }
                }
            }

            return false;
        }

        private bool MastersBidding(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["BuffPets"].AsBool() || !CanLookupPetsAfterZone()) { return false; }

            if (!CanCast(spell)) { return false; }

            if (PetProcSelection.MastersBidding != (PetProcSelection)_settings["PetProcSelection"].AsInt32()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null || pet.Type != PetType.Attack) continue;

                if (!pet.Character.Buffs.Contains(NanoLine.SiphonBox683))
                {
                    if (spell.IsReady)
                    {
                        spell.Cast(pet.Character, true);
                    }
                }
            }

            return false;
        }

        #endregion

        #endregion

        #region Warp

        private bool PetWarp(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["WarpPets"].AsBool() || !CanCast(spell) || !CanLookupPetsAfterZone()) { return false; }

            return DynelManager.LocalPlayer.Pets.Any(c => c.Character == null);
        }

        #endregion

        #region Perks


        #endregion

        #region Buffs

        private bool Sacrificial(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Sacrificial"].AsBool()) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.SacrificialBond)) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }
        private bool SelfEvades(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        private bool Cost(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (CostBuffSelection.Team == (CostBuffSelection)_settings["CostBuffSelection"].AsInt32())
            {
                if (Team.IsInTeam)
                {
                    var target = DynelManager.Players
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && spell.IsInRange(c)
                            && c.Health > 0
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
            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }
        private bool Replenish(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, string settingName = null)
        {
            if (settingName != null && !_settings[settingName].AsBool()){ return false;}

            if (!_settings["Replenish"].AsBool()) { return false; }

            return GenericNanoSkillsBuff(spell, fightingTarget, ref actionTarget);

        }


        private bool MatCre(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["Replenish"].AsBool() && CompositeNanoSkillsBuffSelection.None == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
            {
                return GenericNanoSkillsBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool PsyMod(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["Replenish"].AsBool() && CompositeNanoSkillsBuffSelection.None == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
            {
                return GenericNanoSkillsBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool MatLoc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["Replenish"].AsBool() && CompositeNanoSkillsBuffSelection.None == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
            {
                return GenericNanoSkillsBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool SenImp(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["Replenish"].AsBool() && CompositeNanoSkillsBuffSelection.None == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
            {
                return GenericNanoSkillsBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool BioMet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["Replenish"].AsBool() && CompositeNanoSkillsBuffSelection.None == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
            {
                return GenericNanoSkillsBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool MattMet(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["Replenish"].AsBool() && CompositeNanoSkillsBuffSelection.None == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
            {
                return GenericNanoSkillsBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        protected bool GenericNanoSkillsBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null) { return false; }

            if (Team.IsInTeam)
            {
                return NanoSkillsTeamBuff(spell, fightingTarget, ref actionTarget);
            }

            return NanoSkillsBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool NanoSkillsBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null) { return false; }

            if (SpellChecksNanoSkillsPlayer(spell, fightingTarget))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        protected bool NanoSkillsTeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null) { return false; }

            if (Team.IsInTeam)
            {
                var target = DynelManager.Players
                    .Where(c => c.IsInLineOfSight
                        && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && spell.IsInRange(c)
                        && c.Health > 0
                        && SpellChecksNanoSkillsOther(spell, c))
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

        #region Weapons

        protected bool TwoHandedWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SummonedWeaponSelection.TwoHand == (SummonedWeaponSelection)_settings["SummonedWeaponSelection"].AsInt32())
            {
                if (Time.AONormalTime > weaponCheckDelay && !DynelManager.LocalPlayer.IsAttacking && !Spell.HasPendingCast && Spell.List.Any(s => s.IsReady))
                {
                    //Chat.WriteLine("Checking for two hand weapon");
                    if (HasWeapon()) { return false; }
                    return true;
                } 
            }

            return false;
        }
        protected bool OneHandedWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SummonedWeaponSelection.OnHand == (SummonedWeaponSelection)_settings["SummonedWeaponSelection"].AsInt32())
            {
                if (Time.AONormalTime > weaponCheckDelay && !DynelManager.LocalPlayer.IsAttacking && !Spell.HasPendingCast && Spell.List.Any(s => s.IsReady))
                {
                    //Chat.WriteLine("Checking for one hand weapons");
                    if (HasWeapon()) { return false; }
                }
                    
                return true;
            }

            return false;
        }
        protected bool ShieldWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SummonedWeaponSelection.Shield == (SummonedWeaponSelection)_settings["SummonedWeaponSelection"].AsInt32())
            {
                if (Time.AONormalTime > weaponCheckDelay && !DynelManager.LocalPlayer.IsAttacking && !Spell.HasPendingCast && Spell.List.Any(s => s.IsReady))
                {
                    //Chat.WriteLine("Checking for Shield");
                    if (HasWeapon()) { return false; }
                }
                   
                return true;
            }

            return false;
        }

        #endregion

        #region Team Buffs

        protected bool NanoCompBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.FightingTarget != null) { return false; }

            if (CompositeNanoSkillsBuffSelection.Self == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
            {
                return NonCombatBuff(spell, ref actionTarget, fightingTarget);
            }

            if (CompositeNanoSkillsBuffSelection.Team == (CompositeNanoSkillsBuffSelection)_settings["CompositeNanoSkillsBuffSelection"].AsInt32())
            {
                if (Team.IsInTeam)
                {
                    var target = DynelManager.Players
                   .Where(c => c.IsInLineOfSight
                       && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                       && c.Profession != Profession.NanoTechnician
                       && spell.IsInRange(c)
                       && c.Health > 0 && SpellChecksOther(spell, spell.Nanoline, c))
                   .FirstOrDefault();

                    if (target != null)
                    {
                        if (target.Buffs.Any(c => RelevantNanos.MPCompositeNano.Contains(c.Id))) { return false; }

                        actionTarget.ShouldSetTarget = true;
                        actionTarget.Target = target;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool Evades(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (_settings["Evades"].AsBool())
            {
                return NonComabtTeamBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        #endregion

        #region Misc

        private Spell[] GetAttackPetsWithSLPetsFirst()
        {
            List<Spell> attackPetsWithoutSL = Spell.GetSpellsForNanoline(NanoLine.AttackPets).Where(spell => !RelevantNanos.SLAttackPets.Contains(spell.Id)).OrderByStackingOrder().ToList();
            List<Spell> attackPets = RelevantNanos.SLAttackPets.Select(FindSpell).Where(spell => spell != null).ToList();
            attackPets.AddRange(attackPetsWithoutSL);
            return attackPets.ToArray();
        }

        private Spell FindSpell(int spellHash)
        {
            if (Spell.Find(spellHash, out Spell spell))
            {
                return spell;
            }
            return null;
        }

        private SimpleChar GetTargetToHeal()
        {
            if (DynelManager.LocalPlayer.HealthPercent < 90)
            {
                return DynelManager.LocalPlayer;
            }
            else if (DynelManager.LocalPlayer.IsInTeam())
            {
                var dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent < 85)
                    .Where(c => DynelManager.LocalPlayer.DistanceFrom(c) < 30f)
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

        private void AssignTargetToHealPet()
        {
            var dyingTarget = GetTargetToHeal();

            Pet healPet = DynelManager.LocalPlayer.Pets.Where(pet => pet.Type == PetType.Heal
            && pet.Character.Nano >= 1).FirstOrDefault();

            if (healPet != null)
            {
                if (dyingTarget != null)
                {
                    if (Time.AONormalTime > _lastHealPetHealTime)
                    {
                        //Chat.WriteLine($"{healPet.Character.Name} healing {dyingTarget.Name}", ChatColor.Green);
                        healPet.Heal(dyingTarget.Identity);
                        _lastHealPetHealTime = Time.AONormalTime + 3;
                    }
                }
            }
        }

        private void AssignTargetToMezzPet()
        {
            var mezzPet = DynelManager.LocalPlayer.Pets.Where(pet => pet.Type == PetType.Support
            && pet.Character.Nano >= 1).FirstOrDefault();

            if (mezzPet != null)
            {
                if ((PetMezzingSelection)_settings["PetMezzingSelection"].AsInt32() == PetMezzingSelection.Target)
                {
                        if (CanLookupPetsAfterZone() && Time.AONormalTime - _lastMezzPetMezzTime > 1)
                        {
                            SynchronizePetCombatState(mezzPet);

                            _lastMezzPetMezzTime = Time.AONormalTime;
                        }
                    }
                else
                {
                    var targetToMezz = GetTargetToMezz();

                    if (targetToMezz != null)
                    {
                        PetMezzing(mezzPet, targetToMezz);
                    }
                    else
                    {
                        if (CanLookupPetsAfterZone() && Time.AONormalTime - _lastMezzPetMezzTime > 1)
                        {
                            SynchronizePetCombatState(mezzPet);

                            _lastMezzPetMezzTime = Time.AONormalTime;
                        }
                    }
                }
            }
        }

        private void PetMezzing(Pet mezzPet, SimpleChar targetToMezz)
        {
            if (mezzPet == null || targetToMezz == null) { return; } 

            if (targetToMezz != null)
            {
                if (Time.AONormalTime > _lastMezzPetMezzTime)
                {
                    if (mezzPet?.Character.IsAttacking == true)
                    {
                        if (mezzPet?.Character.FightingTarget.Identity != targetToMezz.Identity)
                        {
                            mezzPet?.Attack(targetToMezz.Identity);
                            _lastMezzPetMezzTime = Time.AONormalTime + 2;
                        }
                    }
                    else
                    {
                        mezzPet?.Attack(targetToMezz.Identity);
                        _lastMezzPetMezzTime = Time.AONormalTime + 2;
                    }
                }
            }
            else
            {
                if (mezzPet?.Character.IsAttacking == true)
                {
                    mezzPet.Follow();
                }
            }
        }

        private SimpleChar GetTargetToMezz()
        {
            var targets = DynelManager.NPCs
                .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name))
                .Where(c => !(c.Buffs.Contains(NanoLine.Mezz) || c.Buffs.Contains(NanoLine.AOEMezz) || c.Buffs.Contains(NanoLine.Charm_Short)
                || c.Buffs.Contains(NanoLine.CharmOther)))
                .Where(c => !c.IsPlayer)
                .Where(c => !c.IsPet)
                .Where(c =>
                {
                    if (DynelManager.LocalPlayer.IsAttacking)
                    {
                        return c.Identity != DynelManager.LocalPlayer.FightingTarget.Identity;
                    }
                    else
                    {
                        return c.FightingTarget?.Identity == DynelManager.LocalPlayer.Identity;
                    }
                });


            if (Team.IsInTeam)
            {
                targets = targets.Where(c =>
                   Team.Members.Any(teammate =>
                      c.FightingTarget?.Identity == teammate?.Character.Identity));
            }

            return targets
                .Where(c => c.IsValid)
                .Where(c => c.IsInLineOfSight)
                .OrderBy(c => c.Position.DistanceFrom(DynelManager.LocalPlayer.Position))
                .ThenBy(c => c.HealthPercent)
                .FirstOrDefault();

        }

        private static void PetWaitCommand(string command, string[] param, ChatWindow chatWindow)
        {
            IPCChannel.Broadcast(new PetWaitMessage());
            OnPetWait(0, null);
        }

        private static void PetWarpCommand(string command, string[] param, ChatWindow chatWindow)
        {
            IPCChannel.Broadcast(new PetWarpMessage());
            OnPetWarp(0, null);
        }

        private void PetFollowCommand(string command, string[] param, ChatWindow chatWindow)
        {
            IPCChannel.Broadcast(new PetFollowMessage());
            OnPetFollow(0, null);
        }

        private static class RelevantNanos
        {
            public const int MastersBidding = 268171;
            public const int InducedApathy = 301888;
            public const int AnticipationofRetaliation = 29272;
            public const int ImprovedAnticipationofRetaliation = 302188;
            public const int PetWarp = 209488;
            public static readonly int[] Warps = { 209488 };

            public static readonly int[] CostBuffs = { 95409, 29307, 95411, 95408, 95410 };
            public static readonly int[] HealPets = { 225902, 125746, 125739, 125740, 125741, 125742, 125743, 125744, 125745, 125738 }; //Belamorte has a higher stacking order than Moritficant
            public static readonly int[] SLAttackPets = { 254859, 225900, 254859, 225900, 225898, 225896, 225894 };
            public static readonly int[] MPCompositeNano = { 220343, 220341, 220339, 220337, 220335, 220333, 220331 };
            public static readonly int[] PetDefensive = { 267601, 267600, 267599 };
            public static readonly int[] PetCleanse = { 269870, 269869 };
            public static readonly int[] PetShortTermDamage = { 267598, 205193, 151827, 205189, 205187, 151828, 205185, 151824, 205183,
            151830, 205191, 151826, 205195, 151825, 205197, 151831 };
            public static readonly int[] WarmUpfNukes = { 270355, 125761, 29297, 125762, 29298, 29114 };
            public static readonly int[] SingleTargetNukes = { 267878, 125763, 125760, 125765, 125764 };
            public static readonly int[] InstillDamageBuffs = { 270800, 285101, 116814, 116817, 116812, 116816, 116821, 116815, 116813 };
            public static readonly int[] ChantBuffs = { 116819, 116818, 116811, 116820 };
            public static readonly int[] MatMetBuffs = Spell.GetSpellsForNanoline(NanoLine.MatMetBuff).OrderByStackingOrder().Select(spell => spell.Id).ToArray();
            public static readonly int[] BioMetBuffs = Spell.GetSpellsForNanoline(NanoLine.BioMetBuff).OrderByStackingOrder().Select(spell => spell.Id).ToArray();
            public static readonly int[] PsyModBuffs = Spell.GetSpellsForNanoline(NanoLine.PsyModBuff).OrderByStackingOrder().Select(spell => spell.Id).ToArray();
            public static readonly int[] SenImpBuffs = { 29304, 151757, 29315, 151764 }; //Composites count as SenseImp buffs. Have to be excluded
            public static readonly int[] MatCreBuffs = Spell.GetSpellsForNanoline(NanoLine.MatCreaBuff).OrderByStackingOrder().Select(spell => spell.Id).ToArray();
            public static readonly int[] MatLocBuffs = Spell.GetSpellsForNanoline(NanoLine.MatLocBuff).OrderByStackingOrder().Select(spell => spell.Id).ToArray();
            public static readonly int[] Sacrificial = new[] { 267281, 300506 };
            public static int SacrificialBond = 300505;

            public static readonly int[] TwoHanded =
            {
                154981, //Azure Cobra of Orma
                154982, //Wixel's Notum Python
                154983, //Asp of Semol
                154984, //Viper Staff
            };
            public static readonly int[] OneHanded =
            {
                 //Asp of Titaniush //couldn’t find the nano ID
                154977, //Gold Acantophis
                154978, //Bitis Striker
                154979, //Coplan's Hand Taipan
                154980, //The Crotalus
            };
            public static readonly int[] Shield =
            {
                273376, //Shield of Zset
                275851, //Shield of Esa
                154971, //Shield of Asmodian
                154974, //Mocham's Guard"
                154972, //Death Ward
                154968, //Belthior's Flame Ward
                154975, //Wave Breaker
                154973, //Solar Guard"
                154976, //Notum Defender
                154970, //Vital Buckler
                154969, //Living Shield of Evernan
            };
        }

        public static bool HasWeapon()
        {
            foreach (Item weapon in Inventory.Items)
            {
                if (allWeaponNames.Contains(weapon.Name))
                {
                    return true;
                }
            }
            return false;
        }

        public enum PetProcSelection
        {
            None, InducedApathy, MastersBidding
        }
        public enum PetMezzingSelection
        {
            Target, Adds
        }

        public enum DamageDebuffSelection
        {
            None, Target, Area, Boss
        }
        public enum DamageDebuffASelection
        {
            None, Target, Area, Boss
        }
        public enum DamageDebuffBSelection
        {
            None, Target, Area, Boss
        }
        public enum NanoShutdownDebuffSelection
        {
            None, Target, Area, Boss
        }
        public enum NanoResistanceDebuffSelection
        {
            None, Target, Area, Boss
        }
        public enum CompositeNanoSkillsBuffSelection
        {
            None, Self, Team
        }
        public enum InterruptSelection
        {
            None, Self, Team
        }
        public enum CostBuffSelection
        {
            None, Self, Team
        }

        private enum SummonedWeaponSelection
        {
            None, TwoHand, OnHand, Shield
        }

        public enum ProcType1Selection
        {
            NanobotContingentArrest = 1178949448,
            AnticipatedEvasion = 1398228037,
            ThoughtfulMeans = 1163284553,
            RegainFocus = 1229673298,
            EconomicNanobotUse = 1162302292
        }

        public enum ProcType2Selection
        {
            SuperEgoStrike = 1380271683,
            SuppressFury = 1397703763,
            EgoStrike = 1196837713,
            MindWail = 1212240981,
            SowDoubt = 1398228047,
            SowDespair = 1347310663,
            DiffuseRage = 1296385093
        }

        #endregion
    }
}
