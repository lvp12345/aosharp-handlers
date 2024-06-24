using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CombatHandler.Bureaucrat
{
    public class CratCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private double _lastTrimTime = 0;
        //private const float DelayBetweenTrims = 1;
        private const float DelayBetweenDiverTrims = 305;

        private static bool ToggleBuffing = false;
        private static bool ToggleComposites = false;
        private static bool ToggleRez = false;

        //private bool attackPetTrimmedAggressive = false;

        public static bool _syncPets;

        private static Window _buffWindow;
        private static Window _debuffWindow;
        private static Window _petWindow;
        private static Window _petCommandWindow;
        private static Window _calmingWindow;
        private static Window _procWindow;
        private static Window _itemWindow;
        private static Window _perkWindow;
        private static Window _healingWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _calmView;
        private static View _petView;
        private static View _petCommandView;
        private static View _procView;
        private static View _itemView;
        private static View _perkView;
        private static View _healingView;

        private Dictionary<PetType, bool> petTrimmedAggressive = new Dictionary<PetType, bool>();
        private Dictionary<PetType, bool> petTrimmedAggDef = new Dictionary<PetType, bool>();
        private Dictionary<PetType, bool> petTrimmedHpDiv = new Dictionary<PetType, bool>();
        private Dictionary<PetType, bool> petTrimmedOffDiv = new Dictionary<PetType, bool>();

        private Dictionary<PetType, double> _lastPetTrimDivertOffTime = new Dictionary<PetType, double>()
        {
            { PetType.Attack, 0 },
        };
        private Dictionary<PetType, double> _lastPetTrimDivertHpTime = new Dictionary<PetType, double>()
        {
            { PetType.Attack, 0 },
        };

        private static double _ncuUpdateTime;

        public CratCombatHandler(string pluginDir) : base(pluginDir)
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
                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleXpPerksDelayChangedEvent += CycleXpPerksDelay_Changed;
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
                _settings.AddVariable("SLMap", false);

                _settings.AddVariable("SyncPets", true);
                _settings.AddVariable("SpawnPets", true);
                _settings.AddVariable("BuffPets", true);
                _settings.AddVariable("WarpPets", false);

                _settings.AddVariable("SupportPetProcSelection", (int)SupportPetProcSelection.MastersBidding);
                _settings.AddVariable("AttackPetProcSelection", (int)AttackPetProcSelection.MastersBidding);

                _settings.AddVariable("InitDebuffSelection", (int)InitDebuffSelection.Boss);
                _settings.AddVariable("RedTapeSelection", (int)RedTapeSelection.Boss);
                _settings.AddVariable("IntensifyStressSelection", (int)IntensifyStressSelection.Boss);

                _settings.AddVariable("TauntTrimmer", false);
                _settings.AddVariable("AggDefTrimmer", false);
                _settings.AddVariable("DivertHpTrimmer", false);
                _settings.AddVariable("DivertOffTrimmer", false);

                _settings.AddVariable("Nuking", true);
                _settings.AddVariable("Root", true);

                _settings.AddVariable("CycleXpPerks", false);
                _settings.AddVariable("XPBonus", false);

                _settings.AddVariable("Calm12Man", false);
                //_settings.AddVariable("CalmSector7", false);

                RegisterSettingsWindow("Bureaucrat Handler", "BureaucratSettingsView.xml");

                Game.TeleportEnded += OnZoned;

                // Exonerationon/ AOE Root Reducer
                RegisterSpellProcessor(RelevantNanos.CorporateLeadership, RootReducer);

                //Calms
                RegisterSpellProcessor(RelevantNanos.ShadowlandsCalms, Calm, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.AOECalms, Calm, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.RkCalms, Calm, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.LastMinNegotiations, Calm12Man, CombatActionPriority.High);
                //RegisterSpellProcessor(RelevantNanos.RkCalms, CalmSector7, CombatActionPriority.High);

                //Root/Snare
                RegisterSpellProcessor(RelevantNanos.PuissantVoidInertia, Root, CombatActionPriority.High);
                RegisterSpellProcessor(RelevantNanos.ShacklesofObedience, Snare, CombatActionPriority.High);

                //Debuffs
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder(),
                    (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "InitDebuffSelection"), CombatActionPriority.Medium);

                RegisterSpellProcessor(RelevantNanos.GeneralRadACDebuff,
                (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "InitDebuffSelection"),
                CombatActionPriority.Medium);

                RegisterSpellProcessor(RelevantNanos.GeneralProjACDebuff,
               (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
               => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "InitDebuffSelection"),
               CombatActionPriority.Medium);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SkillLockModifierDebuff847).OrderByStackingOrder(),
                   (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "RedTapeSelection"), CombatActionPriority.Medium);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDeltaDebuff).OrderByStackingOrder(),
                   (Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => EnumDebuff(debuffSpell, fightingTarget, ref actionTarget, "IntensifyStressSelection"), CombatActionPriority.Medium);

                //Nukes
                RegisterSpellProcessor(RelevantNanos.WorkplaceDepression, WorkplaceDepressionTargetDebuff, CombatActionPriority.Low);
                RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke, CombatActionPriority.Low);

                //Buffs
                RegisterSpellProcessor(RelevantNanos.PistolBuffsSelf, PistolSelfOnly);

                //Team Buffs
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(),
                    (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => GenericSelectionBuff(buffSpell, fightingTarget, ref actionTarget, "InitBuffSelection"));

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ExperienceConstructs_XPBonus).OrderByStackingOrder(), XPBonus);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDeltaBuffs).OrderByStackingOrder(), NanoDelta);
                RegisterSpellProcessor(RelevantNanos.PistolBuffs, PistolTeam);
                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Psy_IntBuff).OrderByStackingOrder(), PsyIntBuff);

                RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalDecreaseBuff).OrderByStackingOrder(),
                (Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => NonComabtTeamBuff(buffSpell, fightingTarget, ref actionTarget, "CutRedTape"));

                //Buff Aura   AAOAAD, Crit, NanoResist
                RegisterSpellProcessor(RelevantNanos.AadBuffAuras, (Spell aura, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericAuraBuff(aura, fightingTarget, ref actionTarget, BuffingAuraSelection.AAOAAD));
                RegisterSpellProcessor(RelevantNanos.CritBuffAuras, (Spell aura, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                   GenericAuraBuff(aura, fightingTarget, ref actionTarget, BuffingAuraSelection.Crit));
                RegisterSpellProcessor(RelevantNanos.NanoResBuffAuras, (Spell aura, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                   GenericAuraBuff(aura, fightingTarget, ref actionTarget, BuffingAuraSelection.NanoResist));

                //Debuff Aura  NanoResist, Crit, MaxNano
                RegisterSpellProcessor(RelevantNanos.NanoResDebuffAuras, (Spell debuffAuru, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericDebuffingAura(debuffAuru, fightingTarget, ref actionTarget, DebuffingAuraSelection.NanoResist));
                RegisterSpellProcessor(RelevantNanos.CritDebuffAuras, (Spell debuffAuru, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericDebuffingAura(debuffAuru, fightingTarget, ref actionTarget, DebuffingAuraSelection.Crit));
                RegisterSpellProcessor(RelevantNanos.NanoPointsDebuffAuras, (Spell debuffAuru, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) =>
                    GenericDebuffingAura(debuffAuru, fightingTarget, ref actionTarget, DebuffingAuraSelection.MaxNano));

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
                RegisterSpellProcessor(RelevantNanos.CompositeMelee, PetSupportTargetBuff);
                RegisterSpellProcessor(RelevantNanos.CompositeMartialProwess, PetSupportTargetBuff);

                RegisterSpellProcessor(RelevantNanos.DroidDamageMatrix, DroidMatrixBuff);
                //RegisterSpellProcessor(RelevantNanos.DroidPressureMatrix, DroidMatrixBuff);

                //Pet Procs
                RegisterSpellProcessor(RelevantNanos.MastersBidding,
                    (Spell petProc, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => SupportPetProc(petProc, fightingTarget, ref actionTarget, SupportPetProcSelection.MastersBidding));


                RegisterSpellProcessor(RelevantNanos.SedativeInjectors,
                    (Spell petProc, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => SupportPetProc(petProc, fightingTarget, ref actionTarget, SupportPetProcSelection.SedativeInjectors));

                RegisterSpellProcessor(RelevantNanos.MastersBidding,
                    (Spell petProc, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => AttackPetProc(petProc, fightingTarget, ref actionTarget, AttackPetProcSelection.MastersBidding));

                RegisterSpellProcessor(RelevantNanos.DroidPressureMatrix,
                    (Spell petProc, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                    => AttackPetProc(petProc, fightingTarget, ref actionTarget, AttackPetProcSelection.DroidPressureMatrix));

                RegisterSpellProcessor(RelevantNanos.PetCleanse, PetCleanse);

                //Pet warp
                RegisterSpellProcessor(RelevantNanos.PetWarp, PetWarp);

                //Pet Trimmers
                RegisterItemProcessor(RelevantTrimmers.IncreaseAggressiveness,
                    (Item item, SimpleChar target, ref (SimpleChar Target, bool ShouldSetTarget) action) =>
                    PetTrimmer(item, target, ref action, "TauntTrimmer", CanTauntTrim, petType =>
                    {
                        petTrimmedAggressive[petType] = true;
                    }, petType => _lastTrimTime = Time.NormalTime));

                RegisterItemProcessor(RelevantTrimmers.PositiveAggressiveDefensive,
                    (Item item, SimpleChar target, ref (SimpleChar Target, bool ShouldSetTarget) action) =>
                    PetTrimmer(item, target, ref action, "AggDefTrimmer", CanAggDefTrim, petType =>
                    {
                        petTrimmedAggDef[petType] = true;
                    }, petType => _lastTrimTime = Time.NormalTime));

                RegisterItemProcessor(RelevantTrimmers.DivertEnergyToHitpoints,
                    (Item item, SimpleChar target, ref (SimpleChar Target, bool ShouldSetTarget) action) =>
                    PetTrimmer(item, target, ref action, "DivertHpTrimmer", CanDivertHpTrim, petType =>
                    {
                        petTrimmedHpDiv[petType] = true;
                    }, petType => _lastPetTrimDivertHpTime[petType] = Time.NormalTime));

                RegisterItemProcessor(RelevantTrimmers.DivertEnergyToOffense,
                    (Item item, SimpleChar target, ref (SimpleChar Target, bool ShouldSetTarget) action) =>
                    PetTrimmer(item, target, ref action, "DivertOffTrimmer", CanDivertOffTrim, petType =>
                    {
                        petTrimmedOffDiv[petType] = true;
                    }, petType => _lastPetTrimDivertOffTime[petType] = Time.NormalTime));

                ResetTrimmers();

                //Pet Perks

                //Le procs
                //type 1
                RegisterPerkProcessor(PerkHash.LEProcBureaucratPleaseHold, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcBureaucratFormsInTriplicate, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcBureaucratSocialServices, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcBureaucratNextWindowOver, LEProc1, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcBureaucratWaitInThatQueue, LEProc1, CombatActionPriority.Low);

                //type2
                RegisterPerkProcessor(PerkHash.LEProcBureaucratMobilityEmbargo, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcBureaucratWrongWindow, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcBureaucratTaxAudit, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcBureaucratLostPaperwork, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcBureaucratDeflation, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcBureaucratInflationAdjustment, LEProc2, CombatActionPriority.Low);
                RegisterPerkProcessor(PerkHash.LEProcBureaucratPapercut, LEProc2, CombatActionPriority.Low);

                PluginDirectory = pluginDir;

                Healing.FountainOfLifeHealPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentage;
                CycleXpPerksDelay = Config.CharSettings[DynelManager.LocalPlayer.Name].CycleXpPerksDelay;
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

        public Window[] _windows => new Window[] { _calmingWindow, _buffWindow, _healingWindow, _petWindow, _petCommandWindow, _procWindow, _debuffWindow, _itemWindow, _perkWindow };

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
        private void HandlePetCommandViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                if (window.Views.Contains(_petCommandView)) { return; }

                _petCommandView = View.CreateFromXml(PluginDirectory + "\\UI\\BureaucratPetCommandView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Commands", XmlViewName = "BureaucratPetCommandView" }, _petCommandView);
            }
            else if (_petCommandWindow == null || (_petCommandWindow != null && !_petCommandWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_petCommandWindow, PluginDir, new WindowOptions() { Name = "Commands", XmlViewName = "BureaucratPetCommandView" }, _petCommandView, out var container);
                _petCommandWindow = container;
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

                window.FindView("SelfHealPerkPercentageBox", out TextInputView selfHealInput);
                window.FindView("SelfNanoPerkPercentageBox", out TextInputView selfNanoInput);
                window.FindView("TeamHealPerkPercentageBox", out TextInputView teamHealInput);
                window.FindView("TeamNanoPerkPercentageBox", out TextInputView teamNanoInput);

                if (xpPerksInput != null)
                {
                    xpPerksInput.Text = $"{CycleXpPerksDelay}";
                }
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
                SettingsController.CreateSettingsTab(_perkWindow, PluginDir, new WindowOptions() { Name = "Perks", XmlViewName = "BureaucratPerksView" }, _perkView, out var container);
                _perkWindow = container;

                container.FindView("XpPerksDelayBox", out TextInputView xpPerksInput);
                container.FindView("SphereDelayBox", out TextInputView sphereInput);
                container.FindView("WitDelayBox", out TextInputView witOfTheAtroxInput);

                container.FindView("SelfHealPerkPercentageBox", out TextInputView selfHealInput);
                container.FindView("SelfNanoPerkPercentageBox", out TextInputView selfNanoInput);
                container.FindView("TeamHealPerkPercentageBox", out TextInputView teamHealInput);
                container.FindView("TeamNanoPerkPercentageBox", out TextInputView teamNanoInput);

                if (xpPerksInput != null)
                {
                    xpPerksInput.Text = $"{CycleXpPerksDelay}";
                }
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
        private void HandleHealingViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();

            if (window != null)
            {
                if (window.Views.Contains(_healingView)) { return; }

                _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\BureaucratHealingView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Healing", XmlViewName = "BureaucratHealingView" }, _healingView);

                window.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);

                if (FountainOfLifeInput != null)
                {
                    FountainOfLifeInput.Text = $"{Healing.FountainOfLifeHealPercentage}";
                }
            }
            else if (_healingWindow == null || (_healingWindow != null && !_healingWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_healingWindow, PluginDir, new WindowOptions() { Name = "Healing", XmlViewName = "BureaucratHealingView" }, _healingView, out var container);
                _healingWindow = container;

                container.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);

                if (FountainOfLifeInput != null)
                {
                    FountainOfLifeInput.Text = $"{Healing.FountainOfLifeHealPercentage}";
                }
            }
        }

        #endregion

        protected override void OnUpdate(float deltaTime)
        {
            try
            {
                if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 1.2)
                    return;

                base.OnUpdate(deltaTime);

                if (Time.NormalTime > _ncuUpdateTime + 1.0f)
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

                #region UI

                var window = SettingsController.FindValidWindow(_windows);

                if (window != null && window.IsValid)
                {
                    window.FindView("FountainOfLifeHealPercentageBox", out TextInputView FountainOfLifeInput);
                    window.FindView("XpPerksDelayBox", out TextInputView xpPerksInput);
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

                    if (xpPerksInput != null && !string.IsNullOrEmpty(xpPerksInput.Text))
                    {
                        if (int.TryParse(xpPerksInput.Text, out int xpPerksValue))
                        {
                            if (Config.CharSettings[DynelManager.LocalPlayer.Name].CycleXpPerksDelay != xpPerksValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].CycleXpPerksDelay = xpPerksValue;
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
                    if (window.FindView("CombatHandlerPetAttack", out Button PetAttack))//
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
                        buffView.Clicked = HanndleBuffViewClick;
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

                    if (!_settings["SyncPets"].AsBool() && _syncPets)
                    {
                        IPCChannel.Broadcast(new PetSyncOffMessage());
                        syncPetsOffDisabled();
                    }

                    if (_settings["SyncPets"].AsBool() && !_syncPets)
                    {
                        IPCChannel.Broadcast(new PetSyncOnMessag());
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

                HandleCancelDebuffAuras();
                HandleCancelBuffAuras();
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

        #region Exoneration

        private bool RootReducer(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Exoneration"].AsBool()) { return false; }

            var target = DynelManager.Players
            .Where(c => Team.Members.Any(t => t.Identity.Instance == c.Identity.Instance)
            && c.DistanceFrom(DynelManager.LocalPlayer) < 40f
            && c.Buffs.Contains(NanoLine.Root)
            || c.Buffs.Contains(NanoLine.Snare)
            || c.Buffs.Contains(305244) //Pause for Reflection
            || c.Buffs.Contains(268174) //Cunning of The Voracious Horror
            || c.Buffs.Contains(82166) //Greater Fear of Attention
            && SpellChecksOther(spell, spell.Nanoline, c))
            .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = false;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        #endregion

        #region Perks

        protected bool Leadership(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["CycleXpPerks"].AsBool() || !perk.IsAvailable) { return false; }

            if (Time.NormalTime > CycleXpPerks + CycleXpPerksDelay)
            {
                CycleXpPerks = Time.NormalTime;

                if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ShortTermXPGain)) { return false; }

                if (DynelManager.NPCs.Any(c => c.FightingTarget != null && AttackingTeam(c)))
                {
                    return PerkCondtionProcessors.LeadershipPerk(perk);
                }
            }

            return false;
        }

        protected bool Governance(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["CycleXpPerks"].AsBool() || !perk.IsAvailable) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ShortTermXPGain)) { return false; }

            if (DynelManager.NPCs.Any(c => c.FightingTarget != null && AttackingTeam(c)))
            {
                return PerkCondtionProcessors.GovernancePerk(perk);
            }

            return false;
        }
        protected bool TheDirector(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["CycleXpPerks"].AsBool() || !perk.IsAvailable) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ShortTermXPGain)) { return false; }

            if (DynelManager.NPCs.Any(c => c.FightingTarget != null && AttackingTeam(c)))
            {
                return PerkCondtionProcessors.TheDirectorPerk(perk);
            }

            return false;
        }

        #endregion

        #region Buffs

        private bool PistolSelfOnly(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        private bool NanoDelta(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["NanoDeltaTeam"].AsBool())
            {
                return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);
            }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        private bool PsyIntBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["NeuronalStimulatorTeam"].AsBool())
            {
                return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);
            }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        #endregion

        #region Auras

        private bool GenericAuraBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, BuffingAuraSelection auraType)
        {
            BuffingAuraSelection currentSetting = (BuffingAuraSelection)_settings["BuffingAuraSelection"].AsInt32();

            if (currentSetting != auraType)
            {
                return false;
            }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        private bool GenericDebuffingAura(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, DebuffingAuraSelection debuffType)
        {
            DebuffingAuraSelection currentSetting = (DebuffingAuraSelection)_settings["DebuffingAuraSelection"].AsInt32();

            if (currentSetting != debuffType)
            {
                return false;
            }

            if (fightingTarget == null) { return false; }

            return CombatBuff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Nukes

        private bool WorkplaceDepressionTargetDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !CanCast(spell) || !_settings["Nuking"].AsBool()
                || fightingTarget.Buffs.Contains(273632) || fightingTarget.Buffs.Contains(301842)
                || (fightingTarget.HealthPercent < 40 && fightingTarget.MaxHealth < 1000000)) { return false; }

            return true;
        }
        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !CanCast(spell) || !_settings["Nuking"].AsBool()) { return false; }

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

        private bool Calm(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            CalmingSelection calmingSelection = (CalmingSelection)_settings["CalmingSelection"].AsInt32();
            ModeSelection modeSelection = (ModeSelection)_settings["ModeSelection"].AsInt32();

            if (calmingSelection != CalmingSelection.SL && calmingSelection != CalmingSelection.AOE && calmingSelection != CalmingSelection.RK)
            {
                return false;
            }

            if (!CanCast(spell) || modeSelection == ModeSelection.None)
            {
                return false;
            }

            var targets = DynelManager.NPCs
                .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name)
                    && c.Health > 0
                    && c.IsInLineOfSight
                    && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                    && spell.IsInRange(c)
                    //&& spell.IsInRange(c)
                    && c.MaxHealth < 1000000);

            if (modeSelection == ModeSelection.Adds)
            {
                targets = targets
                    .Where(c => c.FightingTarget != null
                        && !AttackingMob(c)
                        && AttackingTeam(c));
            }

            var target = targets
                .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                .ThenBy(c => c.Health)
                .FirstOrDefault();

            if (target != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = target;
                return true;
            }

            return false;
        }

        private bool Calm12Man(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Buffing"].AsBool() || !_settings["Calm12Man"].AsBool() || !CanCast(spell)) { return false; }

            var targets = DynelManager.NPCs
                .Where(c => c.IsAlive
                    && spell.IsInRange(c)
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
            if (!_settings["Buffing"].AsBool()
                || !_settings["Root"].AsBool() || !CanCast(spell)) { return false; }

            var target = DynelManager.Characters
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

        private bool CarloSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 0) { return false; }

            return NoShellPetSpawner(PetType.Support, spell, fightingTarget, ref actionTarget);
        }

        private bool RobotSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
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

        private bool DroidMatrixBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["BuffPets"].AsBool() || !CanLookupPetsAfterZone()) { return false; }

            if (!CanCast(spell)) { return false; }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null)
                {
                    continue;
                }

                if (RobotNeedsBuff(spell, pet))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }


        #endregion

        #region Proc

        private bool SupportPetProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, SupportPetProcSelection petProcSelection)
        {
            SupportPetProcSelection currentSetting = (SupportPetProcSelection)_settings["SupportPetProcSelection"].AsInt32();

            if (currentSetting != petProcSelection || !CanLookupPetsAfterZone() || !_settings["BuffPets"].AsBool())
            {
                return false;
            }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null)
                {
                    continue;
                }

                if (!pet.Character.Buffs.Contains(NanoLine.SiphonBox683)
                    && (pet.Type == PetType.Support))
                {
                    if (spell.IsReady)
                    {
                        spell.Cast(pet.Character, true);
                    }
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = pet.Character;
                    return true;
                }
            }

            return false;
        }

        private bool AttackPetProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, AttackPetProcSelection petProcSelection)
        {
            AttackPetProcSelection currentSetting = (AttackPetProcSelection)_settings["AttackPetProcSelection"].AsInt32();

            if (currentSetting != petProcSelection || !CanLookupPetsAfterZone() || !_settings["BuffPets"].AsBool())
            {
                return false;
            }

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Character == null)
                {
                    continue;
                }

                if (!pet.Character.Buffs.Contains(NanoLine.SiphonBox683)
                    && (pet.Type == PetType.Attack))
                {
                    if (spell.IsReady)
                    {
                        spell.Cast(pet.Character, true);
                    }
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

        #region Trimmers

        private bool PetTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget,
                        string settingName, Func<Pet, bool> canTrimFunc, Action<PetType> updateStatus, Action<PetType> updateTime)
        {
            if (!_settings[settingName].AsBool() || !CanLookupPetsAfterZone() || !CanTrim()) { return false; }

            double currentTime = Time.NormalTime;
            if ((settingName == "DivertHpTrimmer" && currentTime - _lastPetTrimDivertHpTime[PetType.Attack] < DelayBetweenDiverTrims) ||
                (settingName == "DivertOffTrimmer" && currentTime - _lastPetTrimDivertOffTime[PetType.Attack] < DelayBetweenDiverTrims))
            {
                return false;
            }

            Pet _attackPet = DynelManager.LocalPlayer.Pets
                .FirstOrDefault(c => c.Character != null && c.Type == PetType.Attack && canTrimFunc(c));
            if (_attackPet != null)
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _attackPet.Character;
                updateStatus(PetType.Attack);
                updateTime(PetType.Attack);
                return true;
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

        private bool FindSpellNanoLineFallbackToId(Spell spell, Buff[] buffs, out Buff buff)
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

        private bool CanTrim()
        {
            return _lastTrimTime + 1 < Time.NormalTime;
        }

        private bool CanDivertOffTrim(Pet pet)
        {
            return _lastPetTrimDivertOffTime[pet.Type] + DelayBetweenDiverTrims < Time.NormalTime || !petTrimmedOffDiv[pet.Type];
        }

        private bool CanDivertHpTrim(Pet pet)
        {
            return _lastPetTrimDivertHpTime[pet.Type] + DelayBetweenDiverTrims < Time.NormalTime || !petTrimmedHpDiv[pet.Type];
        }


        private bool CanAggDefTrim(Pet pet)
        {

            return !petTrimmedAggDef[pet.Type];
        }

        private bool CanTauntTrim(Pet pet)
        {
            return !petTrimmedAggressive[pet.Type];
        }

        private void ResetTrimmers()
        {
            petTrimmedAggressive[PetType.Attack] = false;
            petTrimmedAggressive[PetType.Support] = false;
            petTrimmedOffDiv[PetType.Attack] = false;
            petTrimmedOffDiv[PetType.Support] = false;
            petTrimmedHpDiv[PetType.Attack] = false;
            petTrimmedHpDiv[PetType.Support] = false;
            petTrimmedAggDef[PetType.Attack] = false;
            petTrimmedAggDef[PetType.Support] = false;
        }

        private void OnZoned(object s, EventArgs e)
        {
            _lastZonedTime = Time.NormalTime;
            _lastCombatTime = double.MinValue;
            ResetTrimmers();
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
            public const int WorkplaceDepression = 273631;
            public const int DroidDamageMatrix = 267916;
            public const int DroidPressureMatrix = 302247;
            public const int CorporateStrategy = 267611;
            public const int LastMinNegotiations = 267535;
            public const int SkilledGunSlinger = 263251;
            public const int GreaterGunSlinger = 263250;

            public const int MastersBidding = 268171;
            public const int SedativeInjectors = 302254;

            public const int PuissantVoidInertia = 224129;
            public const int ShacklesofObedience = 82463;
            public const int CompositeMartialProwess = 302158;
            public const int CompositeMelee = 223360;
            public static readonly int[] CorporateLeadership = { 205439, 205437, 205435, 205433 };
            public const int PetWarp = 209488;
            public static readonly int[] Warps = {
                209488
            };

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
            public static readonly int[] IncreaseAggressiveness = { 154940, 154939 }; // Mech. Engi
            public static readonly int[] PositiveAggressiveDefensive = { 88384, 88383 }; // Mech. Engi
            public static readonly int[] DivertEnergyToHitpoints = { 88382, 88381 }; // Lock skill Elec. Engi for 5m.
            public static readonly int[] DivertEnergyToOffense = { 88378, 88377 }; // Lock skill Mech. Engi for 5m.
        }

        public enum SupportPetProcSelection
        {
            None, MastersBidding, SedativeInjectors
        }

        public enum AttackPetProcSelection
        {
            None, MastersBidding, DroidPressureMatrix
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
            PleaseHold = 1280593985,
            FormsinTriplicate = 1179601236,
            SocialServices = 1514685775,
            NextWindowOver = 1314412356,
            WaitInThatQueue = 1346720323
        }

        public enum ProcType2Selection
        {
            MobilityEmbargo = 1314214988,
            WrongWindow = 1465014094,
            TaxAudit = 1415070025,
            LostPaperwork = 1346717775,
            Deflation = 1398166355,
            InflationAdjustment = 1229340996,
            Papercut = 1346459477
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
