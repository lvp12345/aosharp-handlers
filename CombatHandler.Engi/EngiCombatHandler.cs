using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CombatHandler.Engineer
{
    class EngiCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleRez = false;

        public static bool _syncPets;
        private static Identity _lastAttackTarget;

        private static Window _petWindow;
        private static Window _petCommandWindow;
        private static Window _buffWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;
        private static Window _trimmersWindow;
        private static Window _healingWindow;
        private static Window _specialAttacksWindow;

        private static View _buffView;
        private static View _petCommandView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;
        private static View _trimmersView;
        private static View _healingView;
        private static View _specialAttacksView;

        private static double _ncuUpdateTime;

        int petColor;

        public EngiCombatHandler(string pluginDir) : base(pluginDir)
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

                Network.N3MessageReceived += Network_N3MessageReceived;

                Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentageChangedEvent += FountainOfLifeHealPercentage_Changed;
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

                _settings.AddVariable("StimTargetSelection", 1);

                _settings.AddVariable("Kits", true);

                _settings.AddVariable("SyncPets", true);
                _settings.AddVariable("SpawnPets", true);
                _settings.AddVariable("BuffPets", true);
                _settings.AddVariable("HealPets", false);
                _settings.AddVariable("WarpPets", false);

                _settings.AddVariable("PetDefensiveNanos", false);
                _settings.AddVariable("PetArmorBuff", false);
                _settings.AddVariable("PetDamageBuffBuff", false);

                _settings.AddVariable("MechEngiSelection", 0);
                _settings.AddVariable("ElecEngiSelection", 0);
                _settings.AddVariable("AggressiveDefensiveSelection", 0);

                _settings.AddVariable("SupportMechEngiSelection", 0);
                _settings.AddVariable("SupportElecEngiSelection", 0);
                _settings.AddVariable("SupportAggressiveDefensiveSelection", 0);

                _settings.AddVariable("IncreaseAggressivenessTrimmer", true);
                _settings.AddVariable("SupportIncreaseAggressivenessTrimmer", true);

                _settings.AddVariable("DamageSelection", 1);
                _settings.AddVariable("InitBuffSelection", 1);
                _settings.AddVariable("TeamArmorBuff", true);
                _settings.AddVariable("PistolTeam", true);
                _settings.AddVariable("GrenadeTeam", true);
                _settings.AddVariable("ShadowlandReflectBase", true);
                _settings.AddVariable("RKReflectSelection", 0);
                _settings.AddVariable("DamageShields", false);
                _settings.AddVariable("SLMap", false);
                _settings.AddVariable("MEBuff", false);
                _settings.AddVariable("SelfBlockers", false);
                _settings.AddVariable("TeamBlockers", false);

                _settings.AddVariable("BuffingAuraSelection", 0);
                _settings.AddVariable("DebuffingAuraSelection", 0);

                _settings.AddVariable("MASelection", 267525);
                _settings.AddVariable("IntelligenceSelection", 0);

                _settings.AddVariable("PetPerkSelection", 0);
                _settings.AddVariable("PetProcSelection", 0);

                _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.ReactiveArmor);
                _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.AssaultForceRelief);

                _settings.AddVariable("LegShot", false);

                RegisterSettingsWindow("Engi Handler", "EngineerSettingsView.xml");

                Game.TeleportEnded += OnZoned;

                //Pet heals
                RegisterSpellProcessor(RelevantNanos.PetPercentHealing, PetHealing, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.PetTargetHealing, PetHealing, CombatActionPriority.High);

                //Pet spawners
                RegisterSpellProcessor(PetsList.Pets.Where(c => c.Value.PetType == PetType.Attack).Select(c => c.Key).ToArray(), CastPets, CombatActionPriority.High);
                RegisterSpellProcessor(PetsList.Pets.Where(c => c.Value.PetType == PetType.Support).Select(c => c.Key).ToArray(), CastPets, CombatActionPriority.High);

                //Pet Aura 
                //buffing
                RegisterSpellProcessor(RelevantNanos.ArmorAura, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericAuraBuff(spell, fightingTarget, ref actionTarget, 1));

                RegisterSpellProcessor(RelevantNanos.ReflectAura, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericAuraBuff(spell, fightingTarget, ref actionTarget, 2));

                RegisterSpellProcessor(RelevantNanos.DamageAura, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericAuraBuff(spell, fightingTarget, ref actionTarget, 3));

                RegisterSpellProcessor(RelevantNanos.ShieldAura, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericAuraBuff(spell, fightingTarget, ref actionTarget, 4));

                //debuffing
                RegisterSpellProcessor(RelevantNanos.Blinds, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericDebuffingAura(spell, fightingTarget, ref actionTarget, 1));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerPetAOESnareBuff).OrderByStackingOrder(), (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericDebuffingAura(spell, fightingTarget, ref actionTarget, 2));

                RegisterSpellProcessor(RelevantNanos.ShieldRippers, (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericDebuffingAura(spell, fightingTarget, ref actionTarget, 3));

                RegisterSpellProcessor(RelevantNanos.IntrusiveAuraCancellation, AuraCancellation);

                //Buffs
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonComabtTeamBuff(buffSpell, fightingTarget, ref actionTarget, "TeamArmorBuff"));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonComabtTeamBuff(buffSpell, fightingTarget, ref actionTarget, "DamageShields"));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ReflectShield).OrderByStackingOrder(),
                   (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "RKReflectSelection"));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadowlandReflectBase).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonCombatBuff(spell, ref actionTarget, fightingTarget, "ShadowlandReflectBase"));

                RegisterSpellProcessor(RelevantNanos.EngineeringBuff, MechanicalEngineering);


                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolTeam);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.GrenadeBuffs).OrderByStackingOrder(), Grenade);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SpecialAttackAbsorberBase).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonCombatBuff(spell, ref actionTarget, fightingTarget, "TeamBlockers"));
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerSpecialAttackAbsorber).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonCombatBuff(spell, ref actionTarget, fightingTarget, "SelfBlockers"));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(), SelfDamageBuff);

                RegisterSpellProcessor(RelevantNanos.DamageBuffLineA, TeamDamageBuff);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.GeneralMechanicalEngineeringBuff).OrderByStackingOrder(),
                (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonCombatBuff(spell, ref actionTarget, fightingTarget, null));

                //Pets
               
                //pet buffs
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerMiniaturization).OrderByStackingOrder(),
                    (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => SettingPetBuff(buffSpell, fightingTarget, ref actionTarget, null));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetShortTermDamageBuffs).OrderByStackingOrder(),
                    (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => SettingPetBuff(buffSpell, fightingTarget, ref actionTarget, null));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPPetInitiativeBuffs).OrderByStackingOrder(),
                    (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => SettingPetBuff(buffSpell, fightingTarget, ref actionTarget, null));

                RegisterSpellProcessor(RelevantNanos.DamageBuffLineA,
                   (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => SettingPetBuff(buffSpell, fightingTarget, ref actionTarget, "PetDamageBuffBuff"));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => SettingPetBuff(buffSpell, fightingTarget, ref actionTarget, "PetArmorBuff"));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDefensiveNanos).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => SettingPetBuff(buffSpell, fightingTarget, ref actionTarget, "PetDefensiveNanos"));

                RegisterSpellProcessor(RelevantNanos.ShieldOfObedientServant, ShieldOfTheObedientServant);

                RegisterSpellProcessor(RelevantNanos.PetCleanse, PetCleanse);

                RegisterSpellProcessor(RelevantNanos.PetWarp, PetWarp, CombatActionPriority.High);

                //pet procs
                RegisterSpellProcessor(RelevantNanos.MastersBidding,
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => GenericPetProc(spell, fightingTarget, ref actionTarget, 1));

                RegisterSpellProcessor(RelevantNanos.SedativeInjectors,
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => GenericPetProc(spell, fightingTarget, ref actionTarget, 2));

                //pet perks
                RegisterPerkProcessor(PerkHash.TauntBox,
                    (PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => GenericPetPerk(perkAction, fightingTarget, ref actionTarget, 1));

                RegisterPerkProcessor(PerkHash.ChaoticEnergy,
                    (PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => GenericPetPerk(perkAction, fightingTarget, ref actionTarget, 2));

                RegisterPerkProcessor(PerkHash.SiphonBox,
                    (PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => GenericPetPerk(perkAction, fightingTarget, ref actionTarget, 3));

                //pet trimmers
                RegisterItemProcessor(RelevantTrimmers.IncreaseAggressiveness, Trimmers.IncreaseAggressivenessTrimmer);

                RegisterItemProcessor(RelevantTrimmers.DivertEnergyToDefense, Trimmers.DivertEnergyToDefense);
                RegisterItemProcessor(RelevantTrimmers.DivertEnergyToOffense, Trimmers.DivertEnergyToOffense);
                RegisterItemProcessor(RelevantTrimmers.ColdDamageModifier, Trimmers.ColdDamageModifier);
                RegisterItemProcessor(RelevantTrimmers.FireDamageModifier, Trimmers.FireDamageModifier);
                RegisterItemProcessor(RelevantTrimmers.EnergyDamageModifier, Trimmers.EnergyDamageModifier);
                RegisterItemProcessor(RelevantTrimmers.ImproveActuators, Trimmers.ImproveActuators);

                RegisterItemProcessor(RelevantTrimmers.DivertEnergyToAvoidance, Trimmers.DivertEnergyToAvoidance);
                RegisterItemProcessor(RelevantTrimmers.DivertEnergyToHitpoints, Trimmers.DivertEnergyToHitpoints);

                RegisterItemProcessor(RelevantTrimmers.NegativeAggressiveDefensive, Trimmers.NegativeAggressiveDefensive);
                RegisterItemProcessor(RelevantTrimmers.PositiveAggressiveDefensive, Trimmers.PositiveAggressiveDefensive);

                Trimmers.LastTrimTime = Time.AONormalTime;
                //Items
                int intelligenceItem = _settings["IntelligenceSelection"].AsInt32();
                int maItem = _settings["MASelection"].AsInt32();
                if (maItem == 204329)
                {
                    foreach (var item in Inventory.FindAll("Bird of Prey").OrderBy(x => x.QualityLevel))
                    {

                        RegisterItemProcessor(item.Id, item.HighId, MAItem);
                    }
                }
                else
                {
                    RegisterItemProcessor(maItem, maItem, MAItem);
                }
                RegisterItemProcessor(intelligenceItem, intelligenceItem, IntelligenceItem);

                //LE Procs
                RegisterPerkProcessor(PerkHash.LEProcEngineerReactiveArmor, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerDestructiveTheorem, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerEnergyTransfer, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerEndureBarrage, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerDestructiveSignal, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerSplinterPreservation, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerCushionBlows, LEProc1, CombatActionPriority.Low);

                RegisterPerkProcessor(PerkHash.LEProcEngineerAssaultForceRelief, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerDroneMissiles, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerDroneExplosives, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerCongenialEncasement, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcEngineerPersonalProtection, LEProc2, CombatActionPriority.Low);

                PluginDirectory = pluginDir;

                Healing.FountainOfLifeHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentage;
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

        public Window[] _windows => new Window[] { _petWindow, _petCommandWindow, _buffWindow, _healingWindow, _procWindow, _itemWindow, _perkWindow, _trimmersWindow, _specialAttacksWindow };

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
                if (warp != null)
                {
                    warp.Cast(DynelManager.LocalPlayer, false);
                }
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

        private void Network_N3MessageReceived(object sender, N3Message e)
        {
            if (!_settings["SyncPets"].AsBool()) { return; }

            var localPlayer = DynelManager.LocalPlayer;

            switch (e.N3MessageType)
            {
                case N3MessageType.Attack:
                    var attack = (AttackMessage)e;
                    if (attack.Identity != localPlayer.Identity) { return; }

                    // Only send attack command if target changed (new target)
                    if (_lastAttackTarget != attack.Target)
                    {
                        _lastAttackTarget = attack.Target;

                        foreach (Pet pet in localPlayer.Pets.Where(p => p.Type == PetType.Attack || p.Type == PetType.Support))
                        {
                            pet?.Attack(attack.Target);
                        }
                    }
                    break;

                case N3MessageType.StopFight:
                    var stop = (StopFightMessage)e;
                    if (stop.Identity != localPlayer.Identity) { return; }

                    _lastAttackTarget = Identity.None;
                    // Don't send follow commands anymore - let pets continue fighting
                    break;
            }
        }

        #endregion

        #region Window Management

        private Window CreateWindowWithAllTabs()
        {
            // Create the main window using the Pets view as the base (this becomes the first tab content)
            Window window = Window.CreateFromXml("Pets", $@"{PluginDirectory}\UI\EngineerPetsView.xml",
                windowSize: new Rect(0, 0, 320, 345),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            // Load all other views (skip _petView since the base window already contains pets content)
            _petCommandView = View.CreateFromXml(PluginDirectory + "\\UI\\EngineerPetCommandView.xml");
            _itemView = View.CreateFromXml(PluginDirectory + "\\UI\\EngineerItemsView.xml");
            _perkView = View.CreateFromXml(PluginDirectory + "\\UI\\EngineerPerksView.xml");
            _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\EngineerBuffsView.xml");
            _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\EngineerHealingView.xml");
            _procView = View.CreateFromXml(PluginDirectory + "\\UI\\EngineerProcsView.xml");
            _trimmersView = View.CreateFromXml(PluginDirectory + "\\UI\\EngineerTrimmersView.xml");
            _specialAttacksView = View.CreateFromXml(PluginDirectory + "\\UI\\EngSpecialAttacksView.xml");

            // Add all tabs to the window (skip "Pets" since it's already the base window content)
            window.AppendTab("Commands", _petCommandView);
            window.AppendTab("Items", _itemView);
            window.AppendTab("Perks", _perkView);
            window.AppendTab("Buffs", _buffView);
            window.AppendTab("Healing", _healingView);
            window.AppendTab("Procs", _procView);
            window.AppendTab("Trimmers", _trimmersView);
            window.AppendTab("SpecialAttacks", _specialAttacksView);

            // Set up input field values for tabs that need them
            SetupInputFields(window);

            window.Show(true);
            return window;
        }

        private void SetupInputFields(Window window)
        {
            // Setup Healing tab inputs
            window.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);
            if (FountainOfLifeInput != null)
            {
                FountainOfLifeInput.Text = $"{Healing.FountainOfLifeHealPercentage}";
            }

            // Setup Perks tab inputs
            window.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);
            window.FindView("SphereDelayBox", out TextInputView sphereInput);
            window.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);
            window.FindView("SelfHealPerkPercentageBox", out TextInputView selfHealInput);
            window.FindView("SelfNanoPerkPercentageBox", out TextInputView selfNanoInput);
            window.FindView("TeamHealPerkPercentageBox", out TextInputView teamHealInput);
            window.FindView("TeamNanoPerkPercentageBox", out TextInputView teamNanoInput);
            window.FindView("BioRegrowthPercentageBox", out TextInputView bioRegrowthPercentageInput);
            window.FindView("BioRegrowthDelayBox", out TextInputView bioRegrowthDelayInput);

            if (bioCocoonInput != null) bioCocoonInput.Text = $"{BioCocoonPercentage}";
            if (sphereInput != null) sphereInput.Text = $"{CycleSpherePerkDelay}";
            if (witOfTheAtroxInput != null) witOfTheAtroxInput.Text = $"{CycleWitOfTheAtroxPerkDelay}";
            if (selfHealInput != null) selfHealInput.Text = $"{SelfHealPerkPercentage}";
            if (selfNanoInput != null) selfNanoInput.Text = $"{SelfNanoPerkPercentage}";
            if (teamHealInput != null) teamHealInput.Text = $"{TeamHealPerkPercentage}";
            if (teamNanoInput != null) teamNanoInput.Text = $"{TeamNanoPerkPercentage}";
            if (bioRegrowthPercentageInput != null) bioRegrowthPercentageInput.Text = $"{BioRegrowthPercentage}";
            if (bioRegrowthDelayInput != null) bioRegrowthDelayInput.Text = $"{CycleBioRegrowthPerkDelay}";

            // Setup Items tab inputs
            window.FindView("StimTargetBox", out TextInputView stimTargetInput);
            window.FindView("StimHealthPercentageBox", out TextInputView stimHealthInput);
            window.FindView("StimNanoPercentageBox", out TextInputView stimNanoInput);
            window.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
            window.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);
            window.FindView("BodyDevAbsorbsItemPercentageBox", out TextInputView bodyDevInput);
            window.FindView("StrengthAbsorbsItemPercentageBox", out TextInputView strengthInput);

            if (stimTargetInput != null) stimTargetInput.Text = $"{StimTargetName}";
            if (stimHealthInput != null) stimHealthInput.Text = $"{StimHealthPercentage}";
            if (stimNanoInput != null) stimNanoInput.Text = $"{StimNanoPercentage}";
            if (kitHealthInput != null) kitHealthInput.Text = $"{KitHealthPercentage}";
            if (kitNanoInput != null) kitNanoInput.Text = $"{KitNanoPercentage}";
            if (bodyDevInput != null) bodyDevInput.Text = $"{BodyDevAbsorbsItemPercentage}";
            if (strengthInput != null) strengthInput.Text = $"{StrengthAbsorbsItemPercentage}";
        }

        #endregion

        #region Handles

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
            if (window != null && window.IsValid)
            {
                return; // Window already exists with all tabs
            }

            Window newWindow = CreateWindowWithAllTabs();
            // Assign the same window to all window variables so they all reference the same window
            _petWindow = newWindow;
            _petCommandWindow = newWindow;
            _perkWindow = newWindow;
            _buffWindow = newWindow;
            _healingWindow = newWindow;
            _itemWindow = newWindow;
            _trimmersWindow = newWindow;
            _procWindow = newWindow;
            _specialAttacksWindow = newWindow;
        }

        private void HandlePetCommandViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null && window.IsValid)
            {
                return; // Window already exists with all tabs
            }

            Window newWindow = CreateWindowWithAllTabs();
            // Assign the same window to all window variables so they all reference the same window
            _petWindow = newWindow;
            _petCommandWindow = newWindow;
            _perkWindow = newWindow;
            _buffWindow = newWindow;
            _healingWindow = newWindow;
            _itemWindow = newWindow;
            _trimmersWindow = newWindow;
            _procWindow = newWindow;
            _specialAttacksWindow = newWindow;
        }

        private void HandlePerkViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null && window.IsValid)
            {
                return; // Window already exists with all tabs
            }

            Window newWindow = CreateWindowWithAllTabs();
            // Assign the same window to all window variables so they all reference the same window
            _petWindow = newWindow;
            _petCommandWindow = newWindow;
            _perkWindow = newWindow;
            _buffWindow = newWindow;
            _healingWindow = newWindow;
            _itemWindow = newWindow;
            _trimmersWindow = newWindow;
            _procWindow = newWindow;
            _specialAttacksWindow = newWindow;
        }

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null && window.IsValid)
            {
                return; // Window already exists with all tabs
            }

            Window newWindow = CreateWindowWithAllTabs();
            // Assign the same window to all window variables so they all reference the same window
            _petWindow = newWindow;
            _petCommandWindow = newWindow;
            _perkWindow = newWindow;
            _buffWindow = newWindow;
            _healingWindow = newWindow;
            _itemWindow = newWindow;
            _trimmersWindow = newWindow;
            _procWindow = newWindow;
            _specialAttacksWindow = newWindow;
        }

        private void HandleHealingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null && window.IsValid)
            {
                return; // Window already exists with all tabs
            }

            Window newWindow = CreateWindowWithAllTabs();
            // Assign the same window to all window variables so they all reference the same window
            _petWindow = newWindow;
            _petCommandWindow = newWindow;
            _perkWindow = newWindow;
            _buffWindow = newWindow;
            _healingWindow = newWindow;
            _itemWindow = newWindow;
            _trimmersWindow = newWindow;
            _procWindow = newWindow;
            _specialAttacksWindow = newWindow;
        }

        private void HandleItemViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null && window.IsValid)
            {
                return; // Window already exists with all tabs
            }

            Window newWindow = CreateWindowWithAllTabs();
            // Assign the same window to all window variables so they all reference the same window
            _petWindow = newWindow;
            _petCommandWindow = newWindow;
            _perkWindow = newWindow;
            _buffWindow = newWindow;
            _healingWindow = newWindow;
            _itemWindow = newWindow;
            _trimmersWindow = newWindow;
            _procWindow = newWindow;
            _specialAttacksWindow = newWindow;
        }

        private void HandleTrimmersViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null && window.IsValid)
            {
                return; // Window already exists with all tabs
            }

            Window newWindow = CreateWindowWithAllTabs();
            // Assign the same window to all window variables so they all reference the same window
            _petWindow = newWindow;
            _petCommandWindow = newWindow;
            _perkWindow = newWindow;
            _buffWindow = newWindow;
            _healingWindow = newWindow;
            _itemWindow = newWindow;
            _trimmersWindow = newWindow;
            _procWindow = newWindow;
            _specialAttacksWindow = newWindow;
        }

        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null && window.IsValid)
            {
                return; // Window already exists with all tabs
            }

            Window newWindow = CreateWindowWithAllTabs();
            // Assign the same window to all window variables so they all reference the same window
            _petWindow = newWindow;
            _petCommandWindow = newWindow;
            _perkWindow = newWindow;
            _buffWindow = newWindow;
            _healingWindow = newWindow;
            _itemWindow = newWindow;
            _trimmersWindow = newWindow;
            _procWindow = newWindow;
            _specialAttacksWindow = newWindow;
        }

        private void HandleSpecialAttacksViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null && window.IsValid)
            {
                return; // Window already exists with all tabs
            }

            Window newWindow = CreateWindowWithAllTabs();
            // Assign the same window to all window variables so they all reference the same window
            _petWindow = newWindow;
            _petCommandWindow = newWindow;
            _perkWindow = newWindow;
            _buffWindow = newWindow;
            _healingWindow = newWindow;
            _itemWindow = newWindow;
            _trimmersWindow = newWindow;
            _procWindow = newWindow;
            _specialAttacksWindow = newWindow;
        }
        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            try
            {
                if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 1.6) { return; }

                if (Time.NormalTime > _ncuUpdateTime + 1.0f)
                {
                    RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                    IPCChannel.Broadcast(ncuMessage);

                    OnRemainingNCUMessage(0, ncuMessage);

                    _ncuUpdateTime = Time.NormalTime;
                }

                CancelBuffs();
                CancelHostileAuras(RelevantNanos.Blinds);
                CancelHostileAuras(RelevantNanos.ShieldRippers);

                #region UI

                var window = SettingsController.FindValidWindow(_windows);

                if (window != null && window.IsValid)
                {
                    window.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);
                    window.FindView("BioCocoonPercentageBox", out TextInputView bioCocoonInput);
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
                    window.FindView("BioRegrowthPercentageBox", out TextInputView bioRegrowthPercentageInput);
                    window.FindView("BioRegrowthDelayBox", out TextInputView bioRegrowthDelayInput);

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

                    if (bioCocoonInput != null && !string.IsNullOrEmpty(bioCocoonInput.Text))
                    {
                        if (int.TryParse(bioCocoonInput.Text, out int bioCocoonValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage != bioCocoonValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage = bioCocoonValue;
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

                    if (bioRegrowthPercentageInput != null && !string.IsNullOrEmpty(bioRegrowthPercentageInput.Text))
                    {
                        if (int.TryParse(bioRegrowthPercentageInput.Text, out int bioRegrowthPercentageValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].BioRegrowthPercentage != bioRegrowthPercentageValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].BioRegrowthPercentage = bioRegrowthPercentageValue;
                            }
                        }
                    }

                    if (bioRegrowthDelayInput != null && !string.IsNullOrEmpty(bioRegrowthDelayInput.Text))
                    {
                        if (int.TryParse(bioRegrowthDelayInput.Text, out int bioRegrowthDelayValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleBioRegrowthPerkDelay != bioRegrowthDelayValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleBioRegrowthPerkDelay = bioRegrowthDelayValue;
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
                    if (window.FindView("CombatHandlerPetFollow", out Button PetFollow))
                    {
                        PetFollow.Tag = window;
                        PetFollow.Clicked = PetFollowClicked;
                    }

                    if (window.FindView("TrimmersView", out Button trimmerView))
                    {
                        trimmerView.Tag = SettingsController.settingsWindow;
                        trimmerView.Clicked = HandleTrimmersViewClick;
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

                    if (SettingsController.settingsWindow.FindView("ProcsView", out Button procView))
                    {
                        procView.Tag = SettingsController.settingsWindow;
                        procView.Clicked = HandleProcViewClick;
                    }

                    if (SettingsController.settingsWindow.FindView("SpecialAttacksView", out Button specialAttacksView))
                    {
                        specialAttacksView.Tag = SettingsController.settingsWindow;
                        specialAttacksView.Clicked = HandleSpecialAttacksViewClick;
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

        #region Buffs

        private bool SelfDamageBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["DamageSelection"].AsInt32() != 1) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        private bool TeamDamageBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["DamageSelection"].AsInt32() != 2) { return false; }

            return NonComabtTeamBuff(spell, fightingTarget, ref actionTarget, null);
        }

        private bool Grenade(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam && _settings["GrenadeTeam"].AsBool())
            {
                return TeamBuffExclusionCharacterWieldedWeapon(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade) || TeamBuffExclusionCharacterWieldedWeapon(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
            }

            if (DynelManager.LocalPlayer.Buffs.Contains(269482)) { return false; }

            return BuffWeaponSkill(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade)
                    || BuffWeaponSkill(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var setting = _settings["InitBuffSelection"].AsInt32();

            switch (setting)
            {
                case 0:
                    return false;
                case 1:
                    if (!GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(CharacterWieldedWeapon.Ranged)) { return false; }
                    return NonCombatBuff(spell, ref actionTarget, fightingTarget);
                case 2:
                    var teamMember = Team.Members.Where(t => t?.Character != null && t.Character.IsInLineOfSight && t.Character.IsAlive
                       && t.Profession != Profession.Doctor && t.Profession != Profession.NanoTechnician && spell.IsInRange(t?.Character)
                       && GetWieldedWeapons(t.Character).HasFlag(CharacterWieldedWeapon.Ranged) && SpellCheckLocalTeam(spell, t.Character))
                       .FirstOrDefault();

                    if (teamMember == null) return false;

                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = teamMember.Character;
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Pets

        #region Pet Spawners

        private bool CastPets(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 0) { return false; }

           return PetSpawner2(PetsList.Pets, spell, fightingTarget, ref actionTarget);

        }

        protected bool PetSpawner2(Dictionary<int, PetSpellData> petData, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Game.IsZoning) { return false; }
            if (!petData.ContainsKey(spell.Id)) { return false; }

            if (Inventory.NumFreeSlots < 2) { return false; }

            if (Inventory.Find(petData[spell.Id].ShellId, out Item shell))
            {
                if (Item.HasPendingUse) { return false; }
                if (!CanSpawnPets(petData[spell.Id].PetType)) { return false; }

                shell?.Use();
            }
            else
            {
                return true;
            }
            return false;
        }

        #endregion

        #region Buffs

        private bool MechanicalEngineering(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["MEBuff"].AsBool()) { return false; }

            if (fightingTarget != null) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.EngineeringBuff)) { return false; }
            if (!CanCast(spell)) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = DynelManager.LocalPlayer;
            return true;
        }

        private bool SettingPetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, string settingName)
        {
            if (settingName != null && !_settings[settingName].AsBool()) { return false; }

            return PetTargetBuff(spell.Nanoline, PetType.Attack, spell, fightingTarget, ref actionTarget)
                || PetTargetBuff(spell.Nanoline, PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool ShieldOfTheObedientServant(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["BuffPets"].AsBool() || !CanLookupPetsAfterZone()) { return false; }

            if (!CanCast(spell)) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null)
                {
                    continue;
                }

                if (!pet.Character.Buffs.Contains(NanoLine.ShieldoftheObedientServant))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Warp

        private bool PetWarp(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["WarpPets"].AsBool() || !CanCast(spell) || !CanLookupPetsAfterZone()) { return false; }

            return DynelManager.LocalPlayer.Pets.Any(c => c.Character == null);
        }

        #endregion

        #region Healing

        private bool PetHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["HealPets"].AsBool() || !CanLookupPetsAfterZone()) { return false; }

            if (!CanCast(spell)) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null)
                {
                    continue;
                }

                if (pet.Character.HealthPercent <= 90 && pet.Character.Health > 0)
                {
                    actionTarget.ShouldSetTarget = spell.Id != RelevantNanos.PetPercentHealing;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Auras

        private bool GenericAuraBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, int auraType)
        {
            if (_settings["BuffingAuraSelection"].AsInt32() != auraType) { return false; }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        private bool GenericDebuffingAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget)
            actionTarget, int debuffType)
        {
            if (_settings["DebuffingAuraSelection"].AsInt32() != debuffType || fightingTarget == null) {return false;}

            switch (debuffType)
            {
                case 1:
                    return true;
                case 3:
                    return CheckDebuffCondition();

                case 2:
                    return SpamSnare(spell.Nanoline, spell, fightingTarget, ref actionTarget);

                default:
                    return false;
            }
        }

        private bool SpamSnare(NanoLine buffNanoLine, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            if (!CanCast(spell)) { return false; }

            if (!spell.IsReady) { return false; }

            var target = DynelManager.LocalPlayer.Pets.Where(c => c.Type == PetType.Attack).FirstOrDefault();

            if (target == null) { return false; }

            actionTarget.Target = target.Character;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        private bool CheckDebuffCondition()
        {
            return DynelManager.NPCs.Any(c => c.Health > 0
                && c.FightingTarget?.Buffs.Contains(202732) == false && c.FightingTarget?.Buffs.Contains(214879) == false
                && c.FightingTarget?.Buffs.Contains(284620) == false && c.FightingTarget?.Buffs.Contains(216382) == false
                && c.FightingTarget?.IsPet == false
                && c.Position.DistanceFrom(DynelManager.LocalPlayer.Position) <= 9f);
        }

        private bool AuraCancellation(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null) { return false; }

            if (DynelManager.LocalPlayer.Pets == null) { return false; }

            var pet = DynelManager.LocalPlayer.Pets
                .FirstOrDefault(c => c != null && c.Character != null && c.Character.Buffs.Contains(NanoLine.EngineerPetAOESnareBuff));

            if (pet == null) { return false; }

            actionTarget.Target = pet.Character;
            actionTarget.ShouldSetTarget = true;
            return true;
        }


        #endregion

        #region Proc

        private bool GenericPetProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, int petProcSelection)
        {
            if (!_settings["BuffPets"].AsBool()) { return false; }
            if (_settings["PetProcSelection"].AsInt32() != petProcSelection ) { return false; }
            if (!CanCast(spell)) { return false; }
            if (!CanLookupPetsAfterZone()) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null)
                {
                    continue;
                }

                if (!pet.Character.Buffs.Contains(NanoLine.SiphonBox683)
                    && (pet.Type == PetType.Attack || pet.Type == PetType.Support))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Perks

        private bool GenericPetPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, int petPerkSelection)
        {
            var setting = _settings["PetPerkSelection"].AsInt32();

            if (setting != petPerkSelection || !CanLookupPetsAfterZone() || !_settings["BuffPets"].AsBool())
            {
                return false;
            }

            int[] relevantNano;
            switch (setting)
            {
                case 0:
                    return false;
                case 1:
                    relevantNano = RelevantNanos.PerkTauntBox;
                    break;
                case 2:
                    relevantNano = RelevantNanos.PerkChaoticBox;
                    break;
                case 3:
                    relevantNano = RelevantNanos.PerkSiphonBox;
                    break;
                default:
                    return false;
            }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null)
                {
                    continue;
                }

                if (!relevantNano.Any(nano => pet.Character.Buffs.Contains(nano)))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #endregion

        #region Misc

        private void OnZoned(object s, EventArgs e)
        {
            _lastZonedTime = Time.NormalTime;
            _lastCombatTime = double.MinValue;
            Trimmers.LastTrimTime = Time.AONormalTime;
            Trimmers.ResetTrimmers();
        }
        private void CancelBuffs()
        {
            if (BuffingAuraSelection.Shield != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ShieldAura);
            }

            if (BuffingAuraSelection.Damage != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.DamageAura);
            }

            if (BuffingAuraSelection.Armor != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ArmorAura);
            }

            if (BuffingAuraSelection.Reflect != (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32())
            {
                CancelBuffs(RelevantNanos.ReflectAura);
            }

            CancelBuffs(DebuffingAuraSelection.ShieldRipper == (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32()
                ? RelevantNanos.Blinds : RelevantNanos.ShieldRippers);
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
            public const int CompositeAttribute = 223372;
            public const int CompositeNano = 223380;

            public const int MastersBidding = 268171;
            public const int SedativeInjectors = 302254;

            public const int CompositeUtility = 287046;
            public const int CompositeRanged = 223348;
            public const int CompositeRangedSpec = 223364;

            public const int SympatheticReactiveCocoon = 154550;
            public const int IntrusiveAuraCancellation = 204372;
            public const int BoostedTendons = 269463;
            public const int PetWarp = 209488;
            public static readonly int[] Warps = {
                209488
            };

            public static readonly Spell[] DamageBuffLineA = Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA)
                .Where(spell => spell.Id != BoostedTendons).OrderByStackingOrder().ToArray();

            public static readonly int[] PerkTauntBox = { 229131, 229130, 229129, 229128, 229127, 229126 };
            public static readonly int[] PerkSiphonBox = { 229657, 229656, 229655, 229654 };
            public static readonly int[] PerkChaoticBox = { 227787 };

            public static readonly int[] PetCleanse = { 269870, 269869 };

            public static readonly int[] ShieldRippers = { 154725, 154726, 154727, 154728 };
            public static readonly int[] Blinds = { 154715, 154716, 154717, 154718, 154719 };

            public static readonly int[] ReflectAura = { 154550, 154551, 154552, 154553 };
            public static readonly int[] DamageAura = { 154560, 154561 };
            public static readonly int[] ArmorAura = { 154562, 154563, 154564, 154565, 154566, 154567 };
            public static readonly int[] ShieldAura = { 154557, 154558, 154559 };
            public const int PetPercentHealing = 270351;
            public static readonly int[] PetTargetHealing = { 116791, 116795, 116796, 116792, 116797, 116794, 116793 };
            public static readonly int[] ShieldOfObedientServant = { 270790, 202260 };
            public static readonly int[] EngineeringBuff = { 273346, 227667, 227657 };

        }
        private static class RelevantTrimmers
        {
            public static readonly int[] IncreaseAggressiveness = { 154940, 154939 }; // Mech. Engi

            public static readonly int[] PositiveAggressiveDefensive = { 88384, 88383 }; // Mech. Engi
            public static readonly int[] NegativeAggressiveDefensive = { 88386, 88385 }; // Mech. Engi

            public static readonly int[] DivertEnergyToDefense = { 87936, 87893 }; // Lock skill Mech. Engi for 5m.
            public static readonly int[] DivertEnergyToOffense = { 88378, 88377 }; // Lock skill Mech. Engi for 5m.

            public static readonly int[] DivertEnergyToHitpoints = { 88382, 88381 }; // Lock skill Elec. Engi for 5m.
            public static readonly int[] DivertEnergyToAvoidance = { 88380, 88379 };// Lock skill Elec. Engi for 5m.

            public static readonly int[] FireDamageModifier = { 249109 };// Lock skill Mech. Engi for 5m.
            public static readonly int[] EnergyDamageModifier = { 249110 };// Lock skill Mech. Engi for 5m.
            public static readonly int[] ColdDamageModifier = { 249107 };// Lock skill Mech. Engi for 5m.

            public static readonly int[] ImproveActuators = { 253189, 253188 };// Lock skill Mech. Engi for 60m.

        }
        
        public enum BuffingAuraSelection
        {
            None, Armor, Reflect, Damage, Shield
        }
        public enum DebuffingAuraSelection
        {
            None, Blind, PetSnare, ShieldRipper
        }
        public enum MechEngiSelection
        {
            None, DivertEnergyToDefense, DivertEnergyToOffense, ColdDamageModifier, FireDamageModifier, EnergyDamageModifier, ImproveActuators
        }
        public enum SupportMechEngiSelection
        {
            None, DivertEnergyToDefense, DivertEnergyToOffense, ColdDamageModifier, FireDamageModifier, EnergyDamageModifier, ImproveActuators
        }
        public enum ElecEngiSelection
        {
            None, DivertEnergyToAvoidance, DivertEnergyToHitpoints
        }
        public enum SupportElecEngiSelection
        {
            None, DivertEnergyToAvoidance, DivertEnergyToHitpoints
        }
        public enum AggressiveDefensiveSelection
        {
            None, NegativeAggressiveDefensive, PositiveAggressiveDefensive
        }
        public enum SupportAggressiveDefensiveSelection
        {
            None, NegativeAggressiveDefensive, PositiveAggressiveDefensive
        }
        public enum ProcType1Selection
        {
            ReactiveArmor = 1146377031,
            DestructiveTheorem = 1380274768,
            EnergyTransfer = 1145654611,
            EndureBarrage = 1146245699,
            DestructiveSignal = 1095717441,
            SplinterPreservation = 1162171474,
            CushionBlows = 1146242392
        }
        public enum ProcType2Selection
        {
            AssaultForceRelief = 1380995154,
            DroneMissiles = 1145394248,
            DroneExplosives = 1112425541,
            CongenialEncasement = 1381254213,
            PersonalProtection = 1145395030
        }

        #endregion
    }
}
