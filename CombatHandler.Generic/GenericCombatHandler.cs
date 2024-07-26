using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic.IPCMessages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static CombatHandler.Generic.PerkCondtionProcessors;
using System.Text.RegularExpressions;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.GameData;

namespace CombatHandler.Generic
{
    public class GenericCombatHandler : AOSharp.Core.Combat.CombatHandler
    {
        public static string previousErrorMessage = string.Empty;

        public int EvadeCycleTimeoutSeconds = 180;

        protected double _lastPetSyncTime = Time.AONormalTime;
        protected double _lastZonedTime = Time.NormalTime;
        protected double _lastCombatTime = double.MinValue;

        public static int BioCocoonPercentage = 0;
        public static int SingleTauntDelay = 0;
        public static int TimedTauntDelay = 0;
        public static int MongoDelay = 0;
        public static int CycleXpPerksDelay = 0;
        public static int CycleSpherePerkDelay = 0;
        public static int CycleWitOfTheAtroxPerkDelay = 0;
        public static int CycleBioRegrowthPerkDelay = 0;
        public static int CycleChallengerDelay = 0;
        public static int CycleRageDelay = 0;
        public static int CycleAbsorbsDelay = 0;
        public static int ShadesCaressPercentage = 0;
        public static int HealthDrainPercentage = 0;
        public static int NanoAegisPercentage = 0;
        public static int NullitySpherePercentage = 0;
        public static int IzgimmersWealthPercentage = 0;
        public static int ShadeTattooPercentage = 0;
        public static int SelfHealPerkPercentage = 0;
        public static int SelfNanoPerkPercentage = 0;
        public static int TeamHealPerkPercentage = 0;
        public static int TeamNanoPerkPercentage = 0;

        public static int BioRegrowthPercentage = 0;

        public static int BattleGroupHeal1Percentage = 0;
        public static int BattleGroupHeal2Percentage = 0;
        public static int BattleGroupHeal3Percentage = 0;
        public static int BattleGroupHeal4Percentage = 0;

        public static int DuckAbsorbsItemPercentage = 0;
        public static int BodyDevAbsorbsItemPercentage = 0;
        public static int StrengthAbsorbsItemPercentage = 0;
        public static int StaminaAbsorbsItemPercentage = 0;
        public static int TOTWPercentage = 0;

        public static int StimHealthPercentage = 0;
        public static int StimNanoPercentage = 0;
        public static int KitHealthPercentage = 0;
        public static int KitNanoPercentage = 0;
        public static string StimTargetName = string.Empty;

        public double CycleXpPerks = 0;
        private double CycleSpherePerk = 0;
        private double CycleWitOfTheAtroxPerk = 0;
        private double CycleBioRegrowthPerk = 0;

        public static List<int> AdvyMorphs = new List<int> { 217670, 25994, 263278, 82834, 275005, 85062, 217680, 85070 };

        public AttackInfoMessage lastAttackInfoMessage;

        private static Window _perkWindow;

        protected readonly string PluginDir;

        public static Settings _settings;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public static bool IsActiveWindow => GetForegroundWindow() == Process.GetCurrentProcess().MainWindowHandle;

        #region targets to not debuff

        protected static HashSet<string> debuffAreaTargetsToIgnore = new HashSet<string>
        {
                    "Dogmatic Pestilence",
                    //"Slayerdroid XXIV Turbo",
                    "Technological Officer Darwelsi",
                    "Immortal Guardian",
                    "Mature Abyss Orchid",
                    "Abyss Orchid Sprout",
                    "Tower of Astodan",
                    "Unicorn Commander Labbe",
                    "Calan-Cur",
                    "Spirit of Judgement",
                    "Wandering Spirit",
                    "Altar of Torture",
                    "Altar of Purification",
                    "Unicorn Coordinator Magnum Blaine",
                    "Watchful Spirit",
                    "Amesha Vizaresh",
                    "Guardian Spirit of Purification",
                    "Tibor 'Rocketman' Nagy",
                    "One Who Obeys Precepts",
                    "The Retainer Of Ergo",
                    "Green Tower",
                    "Blue Tower",
                    "Alien Cocoon",
                    "Alien Coccoon",
                    "Outzone Supplier",
                    "Hollow Island Weed",
                    "Sheila Marlene",
                    "Unicorn Advance Sentry",
                    "Unicorn Technician",
                    "Basic Tools Merchant",
                    "Container Supplier",
                    "Basic Quality Pharmacist",
                    "Basic Quality Armorer",
                    "Basic Quality Weaponsdealer",
                    "Tailor",
                    "Unicorn Commander Rufus",
                    "Ergo, Inferno Guardian of Shadows",
                    "Unicorn Trooper",
                    "Unicorn Squadleader",
                    "Rookie Alien Hunter",
                    "Unicorn Service Tower Alpha",
                    "Unicorn Service Tower Delta",
                    "Unicorn Service Tower Gamma",
                    "Sean Powell",
                    "Xan Spirit",
                    "Unicorn Guard",
                    "Essence Fragment",
                    "Scalding Flames",
                    "Guide",
                    "Guard",
                    "Awakened Xan",
                    "Fanatic",
                    "Peacekeeper Coursey",
                    "Harbinger of Pestilence",
                    "Pandemonium Idol",
                    "Laser Drone",
                    "Heatbeam",
                    "Thermal Detonator",
                    "Unstable Sentry Drone",
                    "Stasis Containment Field",
                    "Assault Drone",
                    "Scalding Flame",
                    "Automated Defense System",
                    "Medical Drone",
                    "Ju-Ju Doll",
                    "Temporal Vortex",
                    "Gateway to the Past",
                    "Gateway to the Present",
                    "Gateway to the Future",
                    "Masked Eleet",
                    "Dust Brigade Security Drone",
                    "Nanovoider",
                    "Punishment",
                    "Flaming Chaos",
                    "Flaming Punishment",
                    "Flaming Vengeance",
                    "Otacustes",
                    "Alien Heavy Patroller",
        };

        #endregion

        public static IPCChannel IPCChannel;
        public static Config Config { get; private set; }

        public GenericCombatHandler(string pluginDir)
        {
            try
            {
                Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\Generic\\{DynelManager.LocalPlayer.Name}\\Config.json");
                IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel));

                PluginDir = pluginDir;

                _settings = new Settings("CombatHandler");

                IPCChannel.RegisterCallback((int)IPCOpcode.ClearBuffs, OnClearBuffs);
                IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

                RegisterPerkProcessors();
                RegisterPerkProcessor(PerkHash.BioCocoon, BioCocoon);
                RegisterPerkProcessor(PerkHash.Sphere, Sphere, CombatActionPriority.High);
                RegisterPerkProcessor(PerkHash.WitOfTheAtrox, WitOfTheAtrox, CombatActionPriority.High);
                RegisterPerkProcessor(PerkHash.Limber, Limber, CombatActionPriority.High);
                RegisterPerkProcessor(PerkHash.DanceOfFools, DanceOfFools);
                RegisterPerkProcessor(PerkHash.BioRegrowth, BioRegrowth, CombatActionPriority.High);
                RegisterPerkProcessor(PerkHash.EncaseInStone, EncaseInStone);
                RegisterPerkProcessor(PerkHash.CrushBone, ToggledDamagePerk);
                RegisterPerkProcessor(PerkHash.LegShot, LegShot);
                RegisterPerkProcessor(PerkHash.PowerVolley, PowerUp);
                RegisterPerkProcessor(PerkHash.PowerShock, PowerUp);
                RegisterPerkProcessor(PerkHash.PowerBlast, PowerUp);
                RegisterPerkProcessor(PerkHash.PowerCombo, PowerUp);

                RegisterPerkProcessor(PerkHash.Survival, PowerUp);

                RegisterPerkProcessor(PerkHash.Avalanche,
                (PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => ToggledNonTargetedCombatPerk(perkAction, ref actionTarget, fightingTarget, "AOEPerks"));
                RegisterPerkProcessor(PerkHash.BringThePain,
                (PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                   => ToggledNonTargetedCombatPerk(perkAction, ref actionTarget, fightingTarget, "AOEPerks"));
                RegisterPerkProcessor(PerkHash.SeismicSmash,
                (PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                  => ToggledNonTargetedCombatPerk(perkAction, ref actionTarget, fightingTarget, "AOEPerks"));

                RegisterPerkProcessor(PerkHash.Clipfever,
                (PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                 => ToggledNonTargetedCombatPerk(perkAction, ref actionTarget, fightingTarget, "AOEPerks"));

                RegisterSpellProcessor(RelevantGenericNanos.FountainOfLife, Healing.FountainOfLife, CombatActionPriority.High);

                RegisterItemProcessor(new int[] { RelevantGenericItems.FlowerOfLifeLow, RelevantGenericItems.FlowerOfLifeHigh }, FlowerOfLife);
                RegisterItemProcessor(RelevantGenericItems.ReflectGraft, RelevantGenericItems.ReflectGraft, ReflectGraft);
                RegisterItemProcessor(RelevantGenericItems.SteamingHotCupOfEnhancedCoffee, RelevantGenericItems.SteamingHotCupOfEnhancedCoffee, Coffee);

                RegisterItemProcessor(new int[] { RelevantGenericItems.FlurryOfBlowsHigh, RelevantGenericItems.FlurryOfBlowsLow }, DamageItem);

                RegisterItemProcessor(RelevantGenericItems.DreadlochEnduranceBoosterEnforcerSpecial, RelevantGenericItems.DreadlochEnduranceBoosterEnforcerSpecial, EnforcerEnduranceBooster, CombatActionPriority.High);
                RegisterItemProcessor(RelevantGenericItems.DreadlochEnduranceBoosterNanomageEdition, RelevantGenericItems.DreadlochEnduranceBoosterNanomageEdition, NanomageEnduranceBooster, CombatActionPriority.High);

                //Taunt Tools
                RegisterItemProcessor(RelevantGenericItems.TauntTools, TauntTool, CombatActionPriority.Medium);

                RegisterItemProcessor(new int[] { RelevantGenericItems.StrengthOfTheImmortal, RelevantGenericItems.MightOfTheRevenant, RelevantGenericItems.BarrowStrength }, TotwDmgShoulder);

                RegisterItemProcessor(RelevantGenericItems.GnuffsEternalRiftCrystal, RelevantGenericItems.GnuffsEternalRiftCrystal, DamageItem);
                RegisterItemProcessor(RelevantGenericItems.Drone, RelevantGenericItems.Drone, DamageItem);
                RegisterItemProcessor(RelevantGenericItems.WenWen, RelevantGenericItems.WenWen, DamageItem);

                RegisterItemProcessor(RelevantGenericItems.RingofPurifyingFlame, RelevantGenericItems.RingofPurifyingFlame, DamageItem);
                RegisterItemProcessor(RelevantGenericItems.RingofBlightedFlesh, RelevantGenericItems.RingofBlightedFlesh, DamageItem);

                RegisterItemProcessor(RelevantGenericItems.RingofEternalNight, RelevantGenericItems.RingofEternalNight, DamageItem);

                RegisterItemProcessor(RelevantGenericItems.RingofTatteredFlame, RelevantGenericItems.RingofTatteredFlame, DamageItem);
                RegisterItemProcessor(RelevantGenericItems.RingofWeepingFlesh, RelevantGenericItems.RingofWeepingFlesh, DamageItem);

                RegisterItemProcessor(new int[] { RelevantGenericItems.DesecratedFlesh, RelevantGenericItems.CorruptedFlesh, RelevantGenericItems.WitheredFlesh }, TotwShieldShoulder);

                RegisterItemProcessor(RelevantGenericItems.AssaultClassTank, RelevantGenericItems.AssaultClassTank, AssaultClass, CombatActionPriority.High);

                RegisterItemProcessor(SharpObjectsItems.ItemsOrderbyQL, SharpObjects);

                RegisterItemProcessor(RelevantGenericItems.ThrowingGrenade, Grenades);
                RegisterItemProcessor(new int[] { RelevantGenericItems.UponAWaveOfSummerLow, RelevantGenericItems.UponAWaveOfSummerHigh }, DamageItem);
                RegisterItemProcessor(new int[] { RelevantGenericItems.BlessedWithThunderLow, RelevantGenericItems.BlessedWithThunderHigh }, DamageItem);

                RegisterItemProcessor(RelevantGenericItems.RezCanIds, RezCan);

                RegisterItemProcessor(RelevantGenericItems.ExpCans, ExpCan);
                RegisterItemProcessor(new int[] { RelevantGenericItems.InsuranceCan1, RelevantGenericItems.InsuranceCan2 }, InsuranceCan);
                RegisterItemProcessor(new int[] { RelevantGenericItems.HealthAndNanoStim1, RelevantGenericItems.HealthAndNanoStim200, RelevantGenericItems.HealthAndNanoStim400 }, HealthAndNanoStim, CombatActionPriority.High);

                RegisterItemProcessor(new int[] { RelevantGenericItems.PremSitKit, RelevantGenericItems.AreteSitKit, RelevantGenericItems.SitKit1,
                RelevantGenericItems.SitKit100, RelevantGenericItems.SitKit200, RelevantGenericItems.SitKit300, RelevantGenericItems.SitKit400 }, SitKit);

                RegisterItemProcessor(new int[] { RelevantGenericItems.DaTaunterLow, RelevantGenericItems.DaTaunterHigh }, DamageItem);

                RegisterItemProcessor(RelevantGenericItems.BracerofBrotherMalevolence, RelevantGenericItems.BracerofBrotherMalevolence, DamageItem);

                RegisterItemProcessor(new int[] { RelevantGenericItems.FreeStim1, RelevantGenericItems.FreeStim50, RelevantGenericItems.FreeStim100,
                RelevantGenericItems.FreeStim200, RelevantGenericItems.FreeStim300 }, FreeStim);

                RegisterItemProcessor(RelevantGenericItems.BootsOfGridspaceDistortion, RelevantGenericItems.BootsOfGridspaceDistortion, BootsofGridspaceDistortion);

                RegisterSpellProcessor(RelevantGenericNanos.CompositeNano, CompositeBuff);
                RegisterSpellProcessor(RelevantGenericNanos.CompositeAttribute, CompositeBuff);
                RegisterSpellProcessor(RelevantGenericNanos.CompositeUtility, CompositeBuff);
                RegisterSpellProcessor(RelevantGenericNanos.CompositeMartialProwess, CompositeBuff);

                RegisterSpellProcessor(RelevantGenericNanos.InsightIntoSL,
                    (Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
                        => NonComabtTeamBuff(spell, fightingTarget, ref actionTarget, "SLMap"));

                if (GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(CharacterWieldedWeapon.Melee))
                {
                    //We are melee
                    RegisterSpellProcessor(RelevantGenericNanos.CompositeMartial, CompositeBuff);
                    RegisterSpellProcessor(RelevantGenericNanos.CompositeMelee, CompositeBuff);
                    RegisterSpellProcessor(RelevantGenericNanos.CompositePhysicalSpecial, CompositeBuff);
                }

                if (GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(CharacterWieldedWeapon.Ranged))
                {
                    //We are ranged
                    RegisterSpellProcessor(RelevantGenericNanos.CompositeRanged, CompositeBuff);
                    RegisterSpellProcessor(RelevantGenericNanos.CompositeRangedSpecial, CompositeBuff);
                }

                Game.TeleportEnded += TeleportEnded;
                Team.TeamRequest += Team_TeamRequest;
                Network.N3MessageReceived += Network_N3MessageReceived;

                Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannelChangedEvent += IPCChannel_Changed;

                Chat.RegisterCommand("reform", ReformCommand);
                Chat.RegisterCommand("form", FormCommand);
                Chat.RegisterCommand("convert", RaidCommand);
                Chat.RegisterCommand("disband", DisbandCommand);
                Chat.RegisterCommand("rebuff", Rebuff);
                Chat.RegisterCommand("cleancache", (c, p, cw) =>
                {
                    SettingsController.RemainingNCU.Clear();
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

        public static Window[] _window => new Window[] { _perkWindow };

        public void OnDisband(int sender, IPCMessage msg)
        {
            Chat.WriteLine("Leaving team");
            Team.Leave();
        }

        public void OnClearBuffs(int sender, IPCMessage msg)
        {
            Chat.WriteLine("Rebuffing");
            CancelAllBuffs();
        }

        protected override void OnUpdate(float deltaTime)
        {
            try
            {
                if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 2.0) { return; }

                var fightingTarget = DynelManager.LocalPlayer?.FightingTarget;

                if (fightingTarget != null)
                {
                    SpecialAttacks(fightingTarget);
                }

                UseItems();
                Ammo.CrateOfAmmo();

                #region UI

                if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
                {
                    SettingsController.CleanUp();

                    SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView channelInput);

                    if (channelInput != null && !string.IsNullOrEmpty(channelInput.Text))
                    {
                        if (int.TryParse(channelInput.Text, out int channelValue)
                            && Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel != channelValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel = channelValue;
                        }
                    }
                }

                if (DynelManager.LocalPlayer.IsAttacking == true)
                {
                    if (DynelManager.Players.Any(p => p.Identity == DynelManager.LocalPlayer.FightingTarget?.Identity))
                    {
                        DynelManager.LocalPlayer.StopAttack();
                    }
                }

                if (DynelManager.LocalPlayer.IsAttacking || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 0)
                {
                    _lastCombatTime = Time.NormalTime;
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

        #region Perks

        protected bool LegShot(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["LegShot"].AsBool()) { return false; }

            if (fightingTarget == null || !perk.IsAvailable) { return false; }

            if (fightingTarget?.Buffs.Where(c => c.Name.ToLower().Contains(perk.Name.ToLower()) && c.RemainingTime > 3).Any() == true) { return false; }

            return DamagePerk(perk, fightingTarget, ref actionTarget);
        }

        protected bool ToggledNonTargetedCombatPerk(PerkAction perkAction, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, SimpleChar fightingTarget = null,
        string settingName = null)
        {
            if (settingName != null && !_settings[settingName].AsBool())
            {
                return false;
            }

            if (fightingTarget == null && DynelManager.LocalPlayer.FightingTarget == null)
            {
                return false;
            }

            return true;
        }

        protected bool ToggledDamagePerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (_settings["DamagePerk"].AsBool()) { return false; }

            return TargetedDamagePerk(perkAction, fightingTarget, ref actionTarget);
        }

        protected bool PowerUp(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!perkAction.IsAvailable || fightingTarget == null) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantGenericNanos.Energize)) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = fightingTarget;
            return true;
        }

        protected bool BioCocoon(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!perk.IsAvailable || !InCombat()
                || DynelManager.LocalPlayer.HealthPercent > BioCocoonPercentage
                || DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Where(c => c.Name.ToLower().Contains(perk.Name.ToLower())).Any()) { return false; }

            return BuffPerk(perk, fightingTarget, ref actionTarget);
        }

        protected bool BioRegrowth(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!InCombat()) { return false; }

            if (Time.NormalTime < CycleBioRegrowthPerk + CycleBioRegrowthPerkDelay) if (!InCombat()) { return false; }

            CycleBioRegrowthPerk = Time.NormalTime;

            var dyingTeamMember = DynelManager.Players
                .Where(c => c.Health > 70 && Team.Members.Any(t => t.Identity.Instance == c.Identity.Instance)
                    && c.HealthPercent <= BioRegrowthPercentage)
                .OrderBy(c => c.HealthPercent)
                .FirstOrDefault();

            if (DynelManager.LocalPlayer.Buffs.Where(c => c.Name.ToLower().Contains(perk.Name.ToLower())).Any()
                || dyingTeamMember == null) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = dyingTeamMember;
            return true;
        }

        protected bool CyclePerks(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!perk.IsAvailable || fightingTarget == null) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Where(c => c.Name.ToLower().Contains(perk.Name.ToLower())).Any()) { return false; }

            return BuffPerk(perk, fightingTarget, ref actionTarget);
        }

        protected bool Limber(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Find(RelevantGenericNanos.DanceOfFools, out Buff dof) && dof.RemainingTime > 10.0) { return false; }

            return CombatBuffPerk(perk, fightingTarget, ref actionTarget);
        }

        protected bool DanceOfFools(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Find(RelevantGenericNanos.Limber, out Buff limber) && limber.RemainingTime > 10.0) { return false; }

            return true;
        }
        protected bool EvasiveStance(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!perk.IsAvailable) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent >= 75) { return false; }

            return CombatBuffPerk(perk, fightingTarget, ref actionTarget);
        }

        protected bool WitOfTheAtrox(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Time.NormalTime < CycleWitOfTheAtroxPerk + CycleWitOfTheAtroxPerkDelay) { return false; }

            CycleWitOfTheAtroxPerk = Time.NormalTime;

            return CombatBuffPerk(perk, fightingTarget, ref actionTarget);
        }

        protected bool Sphere(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Time.NormalTime < CycleSpherePerk + CycleSpherePerkDelay) { return false; }

            CycleSpherePerk = Time.NormalTime;

            return CombatBuffPerk(perk, fightingTarget, ref actionTarget);
        }

        private bool EncaseInStone(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["EncaseInStone"].AsBool() || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            return CyclePerks(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Comps
        protected bool CompositeBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Composites"].AsBool() || RelevantGenericNanos.ShrinkingGrowingflesh.Contains(spell.Id)) { return false; }

            if (spell.Id == RelevantGenericNanos.CompositeMartial && IsInsideInnerSanctum()) { return false; }

            if (!SpellChecksPlayer(spell, spell.Nanoline)) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = DynelManager.LocalPlayer;
            return true;
        }

        #endregion

        #region Combat

        protected bool CombatBuff(Spell spell, NanoLine nanoline, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (RelevantGenericNanos.ShrinkingGrowingflesh.Contains(spell.Id) || DynelManager.LocalPlayer.FightingTarget == null) { return false; }

            if (!SpellChecksPlayer(spell, nanoline)) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = DynelManager.LocalPlayer;
            return true;
        }

        protected bool CombatTeamBuff(Spell spell, NanoLine nanoline, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (RelevantGenericNanos.ShrinkingGrowingflesh.Contains(spell.Id) ||
                DynelManager.LocalPlayer.FightingTarget == null) { return false; }

            var teamMember = Team.Members.Where(t => t.Character != null && t.Character.IsInLineOfSight && t.Character.IsAlive
             && spell.IsInRange(t.Character) && SpellChecksOther(spell, spell.Nanoline, t.Character))
            .FirstOrDefault();

            if (teamMember == null) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = teamMember.Character;
            return true;
        }

        #endregion

        #region Non Combat

        protected bool PistolTeam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam && _settings["PistolTeam"].AsBool())
            {
                return TeamBuffExclusionWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
            }

            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        protected bool NonCombatBuff(Spell spell, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, SimpleChar fightingTarget = null,
        string settingName = null)
        {
            if (settingName != null && !_settings[settingName].AsBool()) { return false; }
            if (fightingTarget != null && DynelManager.LocalPlayer.FightingTarget != null) { return false; }
            if (AdvyMorphs.Any(buffId => DynelManager.LocalPlayer.Buffs.Contains(buffId))) { return false; }
            if (RelevantGenericNanos.ShrinkingGrowingflesh.Contains(spell.Id)) { return false; }
            if (!SpellChecksPlayer(spell, spell.Nanoline)) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = DynelManager.LocalPlayer;
            return true;
        }

        protected bool XPBonus(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["XPBonus"].AsBool()) { return false; }

            if (Team.IsInTeam)
            {
                var teamMember = Team.Members.Where(t => t.Character != null && t.Character.IsInLineOfSight && t.Character.IsAlive
                 && spell.IsInRange(t.Character) && SpellChecksOther(spell, NanoLine.XPBonus, t.Character))
                .FirstOrDefault();

                if (teamMember == null) { return false; }

                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = teamMember.Character;
                return true;

            }
            else
            {
                if (SpellChecksOther(spell, NanoLine.XPBonus, DynelManager.LocalPlayer))
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = DynelManager.LocalPlayer;
                    return true;
                }
            }

            return false;
        }

        protected bool AAO(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["AAO"].AsBool()) { return false; }

            if (Team.IsInTeam)
            {
                var teamMember = Team.Members.Where(t => t.Character != null && t.Character.IsInLineOfSight && t.Character.IsAlive
                & !(t.Character.Buffs.Contains(NanoLine.AAOBuffs) || AdvyMorphs.Any(morphs => t.Character.Buffs.Contains(morphs)))
                 && spell.IsInRange(t.Character) && SpellChecksOther(spell, spell.Nanoline, t.Character))
                .FirstOrDefault();

                if (teamMember == null) { return false; }

                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = teamMember.Character;
                return true;

            }
            else
            {
                if (!DynelManager.LocalPlayer.Buffs.Contains(NanoLine.AAOBuffs))
                {
                    return NonCombatBuff(spell, ref actionTarget, fightingTarget);
                }
            }

            return false;
        }

        protected bool NonComabtTeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, string settingName = null)
        {
            if (settingName != null && !_settings[settingName].AsBool()) { return false; }
            if (RelevantGenericNanos.ShrinkingGrowingflesh.Contains(spell.Id) || DynelManager.LocalPlayer.FightingTarget != null) { return false; }

            if (!Team.IsInTeam) { return NonCombatBuff(spell, ref actionTarget, fightingTarget); }

            var teamMember = Team.Members.Where(t => t.Character != null && t.Character.IsInLineOfSight && t.Character.IsAlive
             && spell.IsInRange(t.Character) && SpellChecksOther(spell, spell.Nanoline, t.Character))
            .FirstOrDefault();

            if (teamMember == null) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = teamMember.Character;
            return true;
        }

        public bool GenericSelectionBuff(Spell buffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, string selectionSetting)
        {
            switch (_settings[selectionSetting].AsInt32())
            {
                case 0:
                    return false;
                case 1:
                    return NonCombatBuff(buffSpell, ref actionTarget, fightingTarget);
                case 2:
                    return NonComabtTeamBuff(buffSpell, fightingTarget, ref actionTarget);
                default:
                    return false;
            }
        }

        protected bool CheckNotProfsBeforeCast(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Buffing"].AsBool() || !CanCast(spell)) { return false; }

            if (Team.IsInTeam)
            {
                var teamMember = Team.Members.Where(t => t.Character != null && t.Character.IsInLineOfSight && t.Character.IsAlive
                && t.Character.Profession != Profession.Keeper && t.Character.Profession != Profession.Engineer
                 && spell.IsInRange(t.Character) && SpellChecksOther(spell, spell.Nanoline, t.Character))
                .FirstOrDefault();

                if (teamMember == null) { return false; }

                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = teamMember.Character;
                return true;
            }

            return NonCombatBuff(spell, ref actionTarget, fightingTarget);
        }

        #endregion

        #region LE Procs

        protected bool LEProc1(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (perk.Hash != ((PerkHash)_settings["ProcType1Selection"].AsInt32())) { return false; }

            if (!perk.IsAvailable) { return false; }

            if (IsPlayerFlyingOrFalling()) { return false; }

            var localPlayer = DynelManager.LocalPlayer;

            if (localPlayer.Buffs.Where(c => c.Name.ToLower().Contains(perk.Name.ToLower())).Any()) { return false; }

            if (localPlayer.Buffs.Any(buff => buff.Name == perk.Name)) { return false; }

            return true;
        }

        protected bool LEProc2(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (perk.Hash != ((PerkHash)_settings["ProcType2Selection"].AsInt32())) { return false; }

            if (!perk.IsAvailable) { return false; }

            if (IsPlayerFlyingOrFalling()) { return false; }

            var localPlayer = DynelManager.LocalPlayer;

            if (localPlayer.Buffs.Where(c => c.Name.ToLower().Contains(perk.Name.ToLower())).Any()) { return false; }

            if (localPlayer.Buffs.Any(buff => buff.Name == perk.Name)) { return false; }

            return true;
        }

        #endregion

        #region Debuffs

        public bool EnumDebuff(Spell debuffSpell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, string debuffType)
        {
            int settingValue = _settings[debuffType].AsInt32();

            if (settingValue == 0) { return false; }
            if (fightingTarget == null) { return false; }
            if (debuffAreaTargetsToIgnore.Contains(fightingTarget?.Name)) { return false; }
            if (NeedsReload()) { return false; }

            switch (settingValue)
            {
                case 1:
                    return TargetDebuff(debuffSpell, debuffSpell.Nanoline, fightingTarget, ref actionTarget);
                case 2:
                    return AreaDebuff(debuffSpell, ref actionTarget);
                case 3:
                    if (fightingTarget.MaxHealth < 1000000) { return false; }

                    return TargetDebuff(debuffSpell, debuffSpell.Nanoline, fightingTarget, ref actionTarget);
                default:
                    return false;
            }
        }

        public void GetBehindAndPoke(int settingValue)
        {
            bool GoodToStabyStaby = !NeedsReload() && DynelManager.LocalPlayer.FightingTarget != null
                && (settingValue == 1 || (settingValue == 2 && DynelManager.LocalPlayer.FightingTarget.MaxHealth > 1000000));

            var moveBehind = new GetBehind();

            if (GoodToStabyStaby)
            {
                moveBehind.MoveBehindFightingtarget();
            }
        }

        protected bool TargetDebuff(Spell spell, NanoLine nanoline, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }
            if (NeedsReload()) { return false; }
            if (debuffAreaTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }
            if (!SpellChecksOther(spell, nanoline, fightingTarget)) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = fightingTarget;
            return true;
        }
        protected bool ToggledTargetDebuff(string settingName, Spell spell, NanoLine nanoline, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings[settingName].AsBool()) { return false; }
            if (fightingTarget == null) { return false; }
            if (NeedsReload()) { return false; }
            if (debuffAreaTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }
            if (!SpellChecksOther(spell, nanoline, fightingTarget)) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = fightingTarget;
            return true;
        }

        protected bool AreaDebuff(Spell spell, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell)) { return false; }
            if (NeedsReload()) { return false; }

            var target = DynelManager.NPCs
                    .Where(c => !debuffAreaTargetsToIgnore.Contains(c.Name)
                        && c.FightingTarget != null
                        && c.Health > 0
                        && !c.Buffs.Contains(301844)
                        && c.IsInLineOfSight
                        && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                        && spell.IsInRange(c)
                        && SpellChecksOther(spell, spell.Nanoline, c))
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .FirstOrDefault();

            if (target == null) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = target;
            return true;
        }

        #endregion

        #region Weapon Type

        protected bool BuffWeaponType(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, CharacterWieldedWeapon supportedWeaponType)
        {
            if (fightingTarget != null) { return false; }

            if (SpellChecksPlayer(spell, spell.Nanoline) && GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(supportedWeaponType))
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        protected bool TeamBuffWeaponType(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, CharacterWieldedWeapon supportedWeaponType)
        {
            if (!Team.IsInTeam) { return false; }

            var teamMember = Team.Members.Where(t => t.Character != null && t.Character.IsInLineOfSight && t.Character.IsAlive
             && spell.IsInRange(t.Character) && SpellChecksOther(spell, spell.Nanoline, t.Character) && GetWieldedWeapons(t.Character).HasFlag(supportedWeaponType))
            .FirstOrDefault();

            if (teamMember == null) { return false; }

            if (teamMember.Character.Buffs.Contains(NanoLine.FixerSuppressorBuff) &&
                (spell.Nanoline == NanoLine.FixerSuppressorBuff || spell.Nanoline == NanoLine.AssaultRifleBuffs)) { return false; }

            if (teamMember.Character.Buffs.Contains(NanoLine.PistolBuff) &&
                spell.Nanoline == NanoLine.PistolBuff) { return false; }

            if (teamMember.Character.Buffs.Contains(NanoLine.AssaultRifleBuffs) &&
                (spell.Nanoline == NanoLine.AssaultRifleBuffs || spell.Nanoline == NanoLine.GrenadeBuffs)) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = teamMember.Character;
            return true;
        }

        protected bool TeamBuffExclusionWeaponType(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, CharacterWieldedWeapon supportedWeaponType)
        {
            if (!Team.IsInTeam) { return false; }

            var teamMember = Team.Members.Where(t => t.Character != null && t.Character.IsInLineOfSight && t.Character.IsAlive && t.Character.Profession != Profession.NanoTechnician
            && spell.IsInRange(t.Character) && SpellChecksOther(spell, spell.Nanoline, t.Character) && GetWieldedWeapons(t.Character).HasFlag(supportedWeaponType))
           .FirstOrDefault();

            if (teamMember == null) { return false; }

            if (teamMember.Character.Buffs.Contains(NanoLine.FixerSuppressorBuff) &&
                        (spell.Nanoline == NanoLine.FixerSuppressorBuff || spell.Nanoline == NanoLine.AssaultRifleBuffs)) { return false; }

            if (teamMember.Character.Buffs.Contains(NanoLine.PistolBuff) &&
                spell.Nanoline == NanoLine.PistolBuff) { return false; }

            if (teamMember.Character.Buffs.Contains(NanoLine.AssaultRifleBuffs) &&
                (spell.Nanoline == NanoLine.AssaultRifleBuffs || spell.Nanoline == NanoLine.GrenadeBuffs)) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = teamMember.Character;
            return true;
        }

        #endregion

        #region Items

        bool BootsofGridspaceDistortion(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var player = DynelManager.LocalPlayer;
            if (Inventory.Items.Where(c => c.Name == "Boots of Gridspace Distortion" && c.IsEquipped) == null) { return false; }
            if (player.Cooldowns.ContainsKey(Stat.RunSpeed)) { return false; }
            if (player.Buffs.Contains(305996)) { return false; }
            if (Item.HasPendingUse) { return false; }

            return true;
        }

        protected virtual bool SharpObjects(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["SharpObjects"].AsBool()) { return false; }
            if (fightingTarget == null) { return false; }
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.SharpObject)) { return false; }

            actionTarget = (fightingTarget, true);
            return true;
        }
        protected virtual bool Grenades(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Grenades"].AsBool()) { return false; }
            if (fightingTarget == null) { return false; }
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Grenade)) { return false; }

            actionTarget = (fightingTarget, true);
            return true;
        }

        protected virtual bool DamageItem(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }
            if (item == null || Item.HasPendingUse) { return false; }
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item))) { return false; }

            actionTarget = (fightingTarget, true);
            return true;
        }

        protected bool TotwDmgShoulder(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!Team.IsInTeam) { return false; }

            return !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength) && fightingTarget != null && fightingTarget.IsInAttackRange();
        }

        protected virtual bool ReflectGraft(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.SpaceTime) && !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ReflectShield);
        }

        private bool RezCan(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) <= 1) { return false; }
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.FirstAid)) { return false; }

            actiontarget.ShouldSetTarget = false;
            return true;
        }

        private bool FreeStim(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.FirstAid)
                || (!DynelManager.LocalPlayer.Buffs.Contains(NanoLine.Root) && !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.Snare)
                && !DynelManager.LocalPlayer.Buffs.Contains(258231))) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.FirstAid)) { return false; }

            actiontarget.Target = DynelManager.LocalPlayer;
            actiontarget.ShouldSetTarget = true;
            return true;
        }

        protected bool TauntTool(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !_settings["TauntTool"].AsBool()
                || DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Psychology) || Item.HasPendingUse) { return false; }

            actionTarget.Target = fightingTarget;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        private bool InsuranceCan(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.FirstAid)
                || DynelManager.LocalPlayer.GetStat(Stat.UnsavedXP) == 0
                || DynelManager.LocalPlayer.Buffs.Contains(300727)) { return false; }

            actionTarget.ShouldSetTarget = false;
            return true;
        }

        private bool ExpCan(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.FirstAid)) { return false; }

            actionTarget.ShouldSetTarget = false;
            return true;
        }

        private bool FlowerOfLife(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (fightingtarget == null || DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.MartialArts)) { return false; }

            int approximateHealing = item.QualityLevel * 10;

            return DynelManager.LocalPlayer.MissingHealth > approximateHealing;
        }

        private bool SitKit(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["Kits"].AsBool()) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Treatment)
                    || Item.HasPendingUse
                    || (DynelManager.LocalPlayer.HealthPercent >= KitHealthPercentage
                    && DynelManager.LocalPlayer.NanoPercent >= KitNanoPercentage)) { return false; }

            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        private bool HealthAndNanoStim(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            var targetSelection = _settings["StimTargetSelection"].AsInt32();

            if (targetSelection == 0) { return false; }

            bool hasFreeStims = HasFreeStimsInInventory();
            var player = DynelManager.LocalPlayer;

            if (player.Cooldowns.ContainsKey(Stat.FirstAid) || player.GetStat(Stat.TemporarySkillReduction) >= 1 || (!hasFreeStims && (player.Buffs.Contains(NanoLine.Root)
                || player.Buffs.Contains(NanoLine.Snare))) || player.Buffs.Contains(280470) || player.Buffs.Contains(258231)) { return false; }

            SimpleChar target = null;

            switch (targetSelection)
            {
                case 1:
                    if (player.HealthPercent <= StimHealthPercentage || player.NanoPercent <= StimNanoPercentage)
                    {
                        target = DynelManager.LocalPlayer;
                    }
                    break;
                case 2:
                    target = Team.Members.Where(t => t.Character != null && t.Character.IsInLineOfSight && t.Character.IsAlive &&
                    t.Character.Position.DistanceFrom(player.Position) < 10f && (t.Character.HealthPercent <= StimHealthPercentage || t.Character.NanoPercent <= StimNanoPercentage))
                    .OrderByDescending(c => c.Character.Profession == Profession.Doctor || c.Character.Profession == Profession.Enforcer || c.Character.Profession == Profession.Soldier)
                    .ThenBy(c => c.Character.HealthPercent)
                    .FirstOrDefault()?.Character;
                    break;
                case 3:
                    target = DynelManager.Players
                   .FirstOrDefault(c => c.IsInLineOfSight &&
                                        (c.HealthPercent <= StimHealthPercentage || c.NanoPercent <= StimNanoPercentage) &&
                                        c.Name == StimTargetName &&
                                        c.Position.DistanceFrom(player.Position) < 10f &&
                                        c.Health > 0);
                    break;
            }

            if (target == null) { return false; }

            actionTarget.Target = target;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        private bool HasFreeStimsInInventory()
        {
            int[] freeStimIds = new int[] { 204103, 204104, 204105, 204106, 204107 };

            foreach (Item item in Inventory.Items.Where(item => item.Slot.Type == IdentityType.Inventory))
            {
                if (freeStimIds.Contains(item.Id))
                {
                    return true;
                }
            }

            return false;
        }

        private bool EnforcerEnduranceBooster(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength)
                || DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)
                || Item.HasPendingUse
                || DynelManager.LocalPlayer.HealthPercent > StrengthAbsorbsItemPercentage
                || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            return item != null;
        }

        private bool NanomageEnduranceBooster(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength)
                || DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)
                || Team.IsInTeam
                || Item.HasPendingUse
                || DynelManager.LocalPlayer.HealthPercent > StrengthAbsorbsItemPercentage
                || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            return item != null;
        }

        private bool AssaultClass(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.DuckExp)
                || DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)
                || Item.HasPendingUse
                || DynelManager.LocalPlayer.HealthPercent > DuckAbsorbsItemPercentage
                || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            return item != null;
        }

        protected bool TotwShieldShoulder(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BodyDevelopment)
                || DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)
                || Item.HasPendingUse
                || DynelManager.LocalPlayer.HealthPercent > BodyDevAbsorbsItemPercentage
                || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            return item != null;
        }

        protected virtual bool Coffee(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.FoodandDrinkBuffs)) { return false; }

            return DamageItem(item, fightingTarget, ref actionTarget);
        }

        private void UseItems()
        {
            if (Item.HasPendingUse) { return; }

            foreach (Item item in Inventory.Items.Where(c => c.Slot.Type == IdentityType.Inventory
            || c.UniqueIdentity.Type == IdentityType.Container))
            {
                if (item.Name.Contains("Cell Templates") || item.Name.Contains("Plasmid Cultures")
                    || item.Name.Contains("Mitochondria Samples") || item.Name.Contains("Protein Mapping Data")
                    || item.Name.Contains("Mission Token"))
                {
                    item?.Use();
                }

                if (item.UniqueIdentity.Type == IdentityType.Container)
                {
                    List<Item> containerItems = Inventory.GetContainerItems(item.UniqueIdentity);
                    foreach (Item containerItem in containerItems)
                    {
                        if (containerItem.Name.Contains("Cell Templates") || containerItem.Name.Contains("Plasmid Cultures")
                            || containerItem.Name.Contains("Mitochondria Samples") || containerItem.Name.Contains("Protein Mapping Data")
                            || containerItem.Name.Contains("Mission Token"))
                        {
                            containerItem?.Use();
                        }
                    }
                }
            }
        }

        #endregion

        #region Pets

        protected bool NoShellPetSpawner(PetType petType, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanSpawnPets(petType)) { return false; }

            actionTarget.ShouldSetTarget = false;
            return true;
        }

        protected bool PetSpawner(Dictionary<int, PetSpellData> petData, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Game.IsZoning) { return false; }

            if (!petData.ContainsKey(spell.Id)) { return false; }

            if (Inventory.Find(petData[spell.Id].ShellId, out Item shell))
            {
                if (!CanSpawnPets(petData[spell.Id].PetType)) { return false; }
                if (Item.HasPendingUse) { return false; }
                shell?.Use();
            }

            if (Inventory.NumFreeSlots < 2) { return false; }

            if (DynelManager.LocalPlayer.Pets.Where(c => c.Type == petData[spell.Id].PetType || c.Type == PetType.Unknown).Count() >= 1) { return false; }

            return NoShellPetSpawner(petData[spell.Id].PetType, spell, fightingTarget, ref actionTarget);
        }

        protected bool CanSpawnPets(PetType petType)
        {
            return _settings["SpawnPets"].AsBool() && CanLookupPetsAfterZone() && !PetAlreadySpawned(petType);
        }

        private bool PetAlreadySpawned(PetType petType)
        {
            return DynelManager.LocalPlayer.Pets.Any(c => (c.Type == PetType.Unknown || c.Type == petType));
        }

        protected bool CanLookupPetsAfterZone()
        {
            return Time.NormalTime > _lastZonedTime + 5.0;
        }

        public bool PetCleanse(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanLookupPetsAfterZone()) { return false; }

            if (!CanCast(spell)) { return false; }

            var rootedPet = DynelManager.LocalPlayer.Pets.FirstOrDefault(c => !c.Character.Buffs.Contains(224391)
            && (c.Character.Buffs.Contains(NanoLine.Root) || c.Character.Buffs.Contains(NanoLine.Snare) || c.Character.Buffs.Contains(NanoLine.Mezz)));

            if (rootedPet == null) { return false; }

            actionTarget.Target = rootedPet.Character;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        protected bool PetTargetBuff(NanoLine buffNanoLine, PetType petType, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_settings["BuffPets"].AsBool() || !CanLookupPetsAfterZone()) { return false; }

            if (!CanCast(spell)) { return false; }

            var target = DynelManager.LocalPlayer.Pets
                    .Where(c => c.Type == petType
                        && !c.Character.Buffs.Contains(buffNanoLine))
                    .FirstOrDefault();

            if (target == null) { return false; }

            actionTarget.Target = target.Character;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        protected void SynchronizePetCombatStateWithOwner(PetType Attack, PetType Support)
        {
            if (CanLookupPetsAfterZone() && Time.AONormalTime > _lastPetSyncTime)
            {
                foreach (var pet in DynelManager.LocalPlayer.Pets.Where(c => c.Type == Attack || c.Type == Support))
                {
                    SynchronizePetCombatState(pet);
                }

                _lastPetSyncTime = Time.AONormalTime + 0.5;
            }
        }

        protected void SynchronizePetCombatState(Pet pet)
        {
            var localPlayer = DynelManager.LocalPlayer;

            if (localPlayer.IsAttacking == true)
            {
                if (pet.Character.IsAttacking == true)
                {
                    if (pet.Character.FightingTarget?.Identity != localPlayer.FightingTarget?.Identity)
                    {
                        pet?.Attack(localPlayer.FightingTarget.Identity);
                    }
                }
                else
                {
                    pet?.Attack(localPlayer.FightingTarget.Identity);
                }
            }
            else
            {
                if (pet.Character.IsAttacking == true)
                {
                    pet?.Follow();
                }
            }
        }

        #endregion

        #region Special attacks

        private void SpecialAttacks(SimpleChar target)
        {
            try
            {
                foreach (SpecialAttack special in DynelManager.LocalPlayer?.SpecialAttacks)
                {
                    if (!ShouldUseSpecialAttack(special))
                    {
                        continue;
                    }

                    if (!special.IsAvailable())
                    {
                        continue;
                    }

                    if (!special.IsInRange(target))
                    {
                        continue;
                    }

                    if (special == SpecialAttack.FullAuto)
                    {
                        if (special.IsAvailable())
                        {
                            Network.Send(new CharacterActionMessage()
                            {
                                Action = (CharacterActionType)210
                            });
                            continue;
                        }
                    }

                    if (special == SpecialAttack.Burst)
                    {
                        if (lastAttackInfoMessage != null && lastAttackInfoMessage.AmmoCount > 0)
                        {
                            if (lastAttackInfoMessage.AmmoCount <= 3)
                            {
                                Network.Send(new CharacterActionMessage()
                                {
                                    Action = (CharacterActionType)210
                                });
                            }
                            else if (special.IsAvailable())
                            {
                                continue;
                            }
                        }
                        continue;
                    }

                    if (special == SpecialAttack.SneakAttack || special == SpecialAttack.AimedShot)
                    {
                        if (DynelManager.LocalPlayer.MovementState == MovementState.Sneak &&
                            special.IsAvailable())
                        {
                            continue;
                        }
                        continue;
                    }

                    if (special == SpecialAttack.Backstab)
                    {
                        if (special.IsAvailable() && target.FightingTarget?.Identity != DynelManager.LocalPlayer.Identity)
                        {
                            continue;
                        }

                        continue;
                    }

                    if (special == SpecialAttack.Brawl)
                    {
                        if (special.IsAvailable() && special.IsInRange(target))
                        {
                            continue;
                        }
                    }
                    if (target != null)
                    {
                        special.UseOn(target);
                    }
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

        public bool NeedsReload()
        {
            if (lastAttackInfoMessage != null)
            {
                return DynelManager.LocalPlayer.Weapons.Any(w =>
                    w.Value.GetStat(Stat.RangedInit) > 0 && lastAttackInfoMessage.AmmoCount == 0);
            }

            return false;
        }

        #endregion

        #region Checks

        protected bool SpellChecksNanoSkillsPlayer(Spell spell, SimpleChar fightingTarget)
        {
            if (!_settings["Buffing"].AsBool() || !CanCast(spell) || Playfield.ModelIdentity.Instance == 152) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Find(spell.Nanoline, out Buff buff))
            {
                if (spell.StackingOrder < buff.StackingOrder || DynelManager.LocalPlayer.RemainingNCU < spell.NCU) { return false; }

                return buff.RemainingTime < 10;
            }

            return false;
        }

        protected bool SpellChecksNanoSkillsOther(Spell spell, SimpleChar fightingTarget)
        {
            if (!_settings["Buffing"].AsBool()
                || !CanCast(spell)
                || Playfield.ModelIdentity.Instance == 152
                || !fightingTarget.IsInLineOfSight
                || (fightingTarget.IsPlayer && !SettingsController.IsCharacterRegistered(fightingTarget.Identity))) { return false; }

            if (fightingTarget.Buffs.Find(spell.Nanoline, out Buff buff))
            {
                if (spell.StackingOrder < buff.StackingOrder || (fightingTarget.IsPlayer && !HasNCU(spell, fightingTarget))) { return false; }

                return buff.RemainingTime < 10;
            }

            return false;
        }
        protected bool SpellChecksOther(Spell spell, NanoLine nanoline, SimpleChar fightingTarget)
        {
            if (!_settings["Buffing"].AsBool()
                || !CanCast(spell)
                || !fightingTarget.IsInLineOfSight
                || (fightingTarget.IsPlayer && !SettingsController.IsCharacterRegistered(fightingTarget.Identity))) { return false; }

            if (fightingTarget.Buffs.Find(nanoline, out Buff buff))
            {
                if (spell.StackingOrder < buff.StackingOrder || (fightingTarget.IsPlayer && !HasNCU(spell, fightingTarget))) { return false; }

                if (spell.NanoSchool != NanoSchool.Combat && spell.StackingOrder == buff.StackingOrder && buff.RemainingTime > 20f) { return false; }

                if ((spell.NanoSchool == NanoSchool.Combat || spell.Nanoline == NanoLine.EvasionDebuffs_Agent)
                    && spell.StackingOrder == buff.StackingOrder && buff.RemainingTime > 8f) { return false; }

                return true;
            }

            if (fightingTarget.IsPlayer && !HasNCU(spell, fightingTarget)) { return false; }

            return true;
        }

        protected bool SpellChecksPlayer(Spell spell, NanoLine nanoline)
        {
            if (!_settings["Buffing"].AsBool() || !CanCast(spell) || Playfield.ModelIdentity.Instance == 152) { return false; }

            if (RelevantGenericNanos.HpBuffs.Contains(spell.Id) && DynelManager.LocalPlayer.Buffs.Contains(NanoLine.DoctorHPBuffs)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Find(nanoline, out Buff buff))
            {
                if (nanoline == NanoLine.FixerNCUBuff && buff.RemainingTime < 300f) return true;

                if (spell.StackingOrder < buff.StackingOrder || DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU)) { return false; }

                if (spell.StackingOrder == buff.StackingOrder && buff.RemainingTime > 20f) { return false; }

                return true;
            }

            return DynelManager.LocalPlayer.RemainingNCU >= spell.NCU;
        }

        public static bool CanCast(Spell spell)
        {
            if (Playfield.ModelIdentity.Instance == 152) { return false; }

            if (IsPlayerFlyingOrFalling()) { return false; }

            if (!Spell.List.Any(cast => cast.IsReady) || Spell.HasPendingCast) { return false; }

            if (_settings["GlobalRez"].AsBool())
            {
                if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 1)
                {
                    return false;
                }
            }

            return spell.Cost < DynelManager.LocalPlayer.Nano;
        }

        public static bool IsPlayerFlyingOrFalling()
        {
            var localPlayer = DynelManager.LocalPlayer;

            return localPlayer.MovementState == MovementState.Fly || localPlayer.IsFalling || DynelManager.LocalPlayer.Buffs.Contains(RelevantGenericNanos.Hoverboards);
        }

        public static void CancelAllBuffs()
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs
                .Where(x => !x.Name.Contains("Valid Pass")
                && x.Nanoline != NanoLine.BioMetBuff && x.Nanoline != NanoLine.MatCreaBuff
                && x.Nanoline != NanoLine.MatLocBuff && x.Nanoline != NanoLine.MatMetBuff
                && x.Nanoline != NanoLine.PsyModBuff && x.Nanoline != NanoLine.SenseImpBuff
                && x.Nanoline != NanoLine.TraderTeamSkillWranglerBuff
                && x.Nanoline != NanoLine.FixerNCUBuff))
            {
                buff.Remove();
            }
        }

        public static void CancelBuffs(int[] buffsToCancel)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs)
            {
                if (buffsToCancel.Contains(buff.Id))
                {
                    buff.Remove();
                }
            }
        }

        protected bool HasNCU(Spell spell, SimpleChar target)
        {
            return SettingsController.GetRemainingNCU(target.Identity) > spell.NCU;
        }

        protected void CancelHostileAuras(int[] auras)
        {
            if (Time.NormalTime - _lastCombatTime > 5)
            {
                CancelBuffs(auras);
            }
        }

        protected bool IsInsideInnerSanctum()
        {
            return DynelManager.LocalPlayer.Buffs.Any(buff => buff.Id == RelevantGenericNanos.InnerSanctumDebuff);
        }

        public bool AttackingMob(SimpleChar mob)
        {
            if (Team.IsInTeam)
            {
                return Team.Members.Any(c => c.Character?.FightingTarget?.Identity == mob.Identity);
            }

            return DynelManager.LocalPlayer.FightingTarget?.Identity == mob.Identity;
        }

        public bool AttackingTeam(SimpleChar mob)
        {
            if (mob.FightingTarget == null) { return false; }

            if (Team.IsInTeam)
            {
                return Team.Members.Any(t => t.Identity == mob.FightingTarget?.Identity)
                    || (bool)mob.FightingTarget?.IsPet;

            }

            return mob.FightingTarget?.Identity == DynelManager.LocalPlayer.Identity
                || (bool)mob.FightingTarget?.IsPet;
        }

        public static bool InCombat()
        {
            var localPlayer = DynelManager.LocalPlayer;

            if (Team.IsInTeam)
            {
                return Team.Members.Any(m => m.Character != null && m.Character.IsAttacking) ||
                       DynelManager.NPCs.Any(npc => npc.FightingTarget != null &&
                                                    Team.Members.Any(t => t.Identity.Instance == npc.Identity.Instance));
            }

            return localPlayer.IsAttacking ||
                   (localPlayer.Pets != null && localPlayer.Pets.Any(pet => pet.Character != null && pet.Character.IsAttacking)) ||
                   DynelManager.NPCs.Any(npc => npc.FightingTarget != null &&
                                                (npc.FightingTarget.Identity == localPlayer.Identity ||
                                                 (localPlayer.Pets != null && localPlayer.Pets.Any(pet => pet.Character != null && npc.FightingTarget.Identity == pet.Character.Identity))));
        }

        public static CharacterWieldedWeapon GetWieldedWeapons(SimpleChar local) => (CharacterWieldedWeapon)local.GetStat(Stat.EquippedWeapons);

        #endregion

        #region Misc

        private PerkConditionProcessor ToPerkConditionProcessor(GenericPerkConditionProcessor genericPerkConditionProcessor)
        {
            return (PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) => genericPerkConditionProcessor(perkAction, fightingTarget, ref actionTarget);
        }

        protected void RegisterPerkProcessors()
        {
            PerkAction.List.ForEach(perkAction => RegisterPerkAction(perkAction));
        }

        private void RegisterPerkAction(PerkAction perkAction)
        {
            GenericPerkConditionProcessor perkConditionProcessor = PerkCondtionProcessors.GetPerkConditionProcessor(perkAction);

            if (perkConditionProcessor != null)
            {
                RegisterPerkProcessor(perkAction.Hash, ToPerkConditionProcessor(perkConditionProcessor));
            }
        }

        public static bool IsRaidEnabled(string[] param)
        {
            return param.Length > 0 && "raid".Equals(param[0]);
        }

        public static Identity[] GetRegisteredCharactersInvite()
        {
            Identity[] registeredCharacters = SettingsController.GetRegisteredCharacters();
            int firstTeamCount = registeredCharacters.Length > 6 ? 6 : registeredCharacters.Length;
            Identity[] firstTeamCharacters = new Identity[firstTeamCount];
            Array.Copy(registeredCharacters, firstTeamCharacters, firstTeamCount);
            return firstTeamCharacters;
        }

        public static Identity[] GetRemainingRegisteredCharacters()
        {
            Identity[] registeredCharacters = SettingsController.GetRegisteredCharacters();
            int characterCount = registeredCharacters.Length - 6;
            Identity[] remainingCharacters = new Identity[characterCount];

            if (characterCount > 0)
            {
                Array.Copy(registeredCharacters, 6, remainingCharacters, 0, characterCount);
            }
            return remainingCharacters;
        }

        public static void SendTeamInvite(Identity[] targets)
        {
            foreach (Identity target in targets)
            {
                if (target != DynelManager.LocalPlayer.Identity)
                {
                    Team.Invite(target);
                }
            }
        }

        public static void DisbandCommand(string command, string[] param, ChatWindow chatWindow)
        {
            Team.Disband();
            IPCChannel.Broadcast(new DisbandMessage());
        }

        public static void RaidCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (Team.IsLeader)
            {
                Team.ConvertToRaid();
            }
            else
            {
                Chat.WriteLine("Needs to be used from leader.");
            }
        }

        public static void ReformCommand(string command, string[] param, ChatWindow chatWindow)
        {
            Team.Disband();
            IPCChannel.Broadcast(new DisbandMessage());

            Task.Factory.StartNew(
                async () =>
                {
                    await Task.Delay(1000);
                    FormCommand("form", param, chatWindow);
                });
        }

        public static void FormCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (!Team.IsInTeam)
            {
                SendTeamInvite(GetRegisteredCharactersInvite());

                if (IsRaidEnabled(param))
                {
                    Task.Factory.StartNew(
                        async () =>
                        {
                            await Task.Delay(1000);
                            Team.ConvertToRaid();
                            await Task.Delay(1000);
                            SendTeamInvite(GetRemainingRegisteredCharacters());
                            await Task.Delay(5000);
                        });
                }
            }
            else
            {
                Chat.WriteLine("Cannot form a team. Character already in team. Disband first.");
            }
        }
        public void Rebuff(string command, string[] param, ChatWindow chatWindow)
        {
            CancelAllBuffs();
            IPCChannel.Broadcast(new ClearBuffsMessage());
        }

        private void Network_N3MessageReceived(object sender, N3Message e)
        {
            if (e is AttackInfoMessage attackInfoMessage)
            {
                lastAttackInfoMessage = attackInfoMessage;
            }
        }

        [Flags]
        public enum CharacterWieldedWeapon
        {
            Fists = 0x0,              // 0x00000000000000000000b Fists / invalid
            MartialArts = 0x01,       // 0x00000000000000000001b martialarts / fists
            Melee = 0x02,             // 0x00000000000000000010b
            Ranged = 0x04,            // 0x00000000000000000100b
            Bow = 0x08,               // 0x00000000000000001000b
            Smg = 0x10,               // 0x00000000000000010000b
            Edged1H = 0x20,           // 0x00000000000000100000b
            Blunt1H = 0x40,           // 0x00000000000001000000b
            Edged2H = 0x80,           // 0x00000000000010000000b
            Blunt2H = 0x100,          // 0x00000000000100000000b
            Piercing = 0x200,         // 0x00000000001000000000b
            Pistol = 0x400,           // 0x00000000010000000000b
            AssaultRifle = 0x800,     // 0x00000000100000000000b
            Rifle = 0x1000,           // 0x00000001000000000000b
            Shotgun = 0x2000,         // 0x00000010000000000000b
            Grenade = 0x8000,         // 0x00000100000000000000b // 0x00001000000000000000b grenade / martial arts
            MeleeEnergy = 0x4000,     // 0x00001000000000000000b // 0x00000100000000000000b
            RangedEnergy = 0x10000,   // 0x00010000000000000000b
            Grenade2 = 0x20000,       // 0x00100000000000000000b
            HeavyWeapons = 0x40000,   // 0x01000000000000000000b
        }

        private Stat GetSkillLockStat(Item item)
        {
            switch (item.HighId)
            {
                case RelevantGenericItems.UponAWaveOfSummerLow:
                case RelevantGenericItems.UponAWaveOfSummerHigh:
                    return Stat.Riposte;

                case RelevantGenericItems.RingofEternalNight:
                    return Stat.SensoryImprovement;

                case RelevantGenericItems.BlessedWithThunderLow:
                case RelevantGenericItems.BlessedWithThunderHigh:
                    return Stat.MartialArts;

                case RelevantGenericItems.FlurryOfBlowsLow:
                case RelevantGenericItems.FlurryOfBlowsHigh:
                    return Stat.AggDef;

                case RelevantGenericItems.SteamingHotCupOfEnhancedCoffee:
                    return Stat.RunSpeed;

                case RelevantGenericItems.GnuffsEternalRiftCrystal:
                    return Stat.MapNavigation;

                case RelevantGenericItems.Drone:
                case RelevantGenericItems.RingofPurifyingFlame:
                case RelevantGenericItems.RingofTatteredFlame:
                    return Stat.MaterialCreation;

                case RelevantGenericItems.RingofBlightedFlesh:
                case RelevantGenericItems.RingofWeepingFlesh:
                    return Stat.BiologicalMetamorphosis;

                case RelevantGenericItems.WenWen:
                    return Stat.RangedEnergy;

                case RelevantGenericItems.DaTaunterLow:
                case RelevantGenericItems.DaTaunterHigh:
                    return Stat.Psychology;

                case RelevantGenericItems.BracerofBrotherMalevolence:
                    return Stat.Psychic;

                default:
                    throw new Exception($"No skill lock stat defined for item id {item.HighId}");
            }
        }

        private void TeleportEnded(object sender, EventArgs e)
        {
            _lastZonedTime = Time.NormalTime;
            _lastCombatTime = double.MinValue;
        }

        public static void Team_TeamRequest(object s, TeamRequestEventArgs e)
        {
            if (SettingsController.IsCharacterRegistered(e.Requester))
            {
                e.Accept();
            }
        }

        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDir + "\\UI\\" + xmlName, _settings);
        }

        public static class SharpObjectsItems
        {
            public static readonly int[] ItemsOrderbyQL = new[]
            {
                244214, //Fallen Star
                244215, //Heroes Discus
                244216, //Tear of Oedipus
                244211, //Koan Shuriken
                245990, //Lava capsule
                244208, //Poison Darts of the Deceptor
                244209, //Capsule of Fulminating Novictum
                164633, //Aluminum Throwing Dagger
                164779, //Aluminum Throwing Dagger
                244987, //Circus Throwing Dagger
                244210, //Ever burning Coal
                244204, //Meteorite Spikes
                244206, //Chunk of Eternal Ice
                244205, //Electric Bolts
                244986, //Circus Throwing Dagger
                245323, //Kizzermole Gumboil
                164632, //Aluminum Throwing Dagger
                164778, //Aluminum Throwing Dagger
            };
        }

        public static class RelevantGenericItems
        {
            public const int ReflectGraft = 95225; //Hacked Boosted-Graft: Lesser Deflection Shield (Extended) 
            public const int BracerofBrotherMalevolence = 301679;
            public const int FlurryOfBlowsLow = 85907;
            public const int FlurryOfBlowsHigh = 85908;

            public const int DreadlochEnduranceBoosterEnforcerSpecial = 267168;
            public const int DreadlochEnduranceBoosterNanomageEdition = 267167;

            public const int StrengthOfTheImmortal = 305478;
            public const int MightOfTheRevenant = 206013;
            public const int BarrowStrength = 204653;

            public const int LavaCapsule = 245990;

            public const int WitheredFlesh = 204698;
            public const int CorruptedFlesh = 206015;
            public const int DesecratedFlesh = 305476;

            public const int AssaultClassTank = 156576;

            public static readonly int[] ThrowingGrenade = new[]
            {
                164781, //HSR Hedgehog 23 Throwing Grenade
                165117, //May Fly Throwing Grenade
                164780, //HSR Hedgehog 23 Throwing Grenade
                165116, //May Fly Throwing Grenade
            };

            public const int RingofTatteredFlame = 204593;
            public const int RingofPurifyingFlame = 305493;

            public const int RingofWeepingFlesh = 204595;
            public const int RingofBlightedFlesh = 305491;

            public const int RingofEternalNight = 204598;

            public const int BloodthrallRing = 305495;

            public const int SteamingHotCupOfEnhancedCoffee = 157296;

            public const int FlowerOfLifeLow = 70614;
            public const int FlowerOfLifeHigh = 204326;

            public const int UponAWaveOfSummerLow = 205405;
            public const int UponAWaveOfSummerHigh = 205406;

            public const int BlessedWithThunderLow = 70612;
            public const int BlessedWithThunderHigh = 204327;

            public const int DaTaunterLow = 158045;
            public const int DaTaunterHigh = 158046;

            public const int GnuffsEternalRiftCrystal = 303179;
            public const int BootsOfGridspaceDistortion = 305995;

            public static int[] RezCanIds = new[] { 301070, 303390 };

            public const int RezCan1 = 301070;
            public const int RezCan2 = 303390;

            public const int InsuranceCan1 = 300728;
            public const int InsuranceCan2 = 303389;

            public const int PremSitKit = 297274;
            public const int AreteSitKit = 292256;
            public const int SitKit1 = 291082;
            public const int SitKit100 = 291083;
            public const int SitKit200 = 291084;
            public const int SitKit300 = 293296;
            public const int SitKit400 = 293297;

            public const int FreeStim1 = 204103;
            public const int FreeStim50 = 204104;
            public const int FreeStim100 = 204105;
            public const int FreeStim200 = 204106;
            public const int FreeStim300 = 204107;

            public const int HealthAndNanoStim1 = 291043;
            public const int HealthAndNanoStim200 = 291044;
            public const int HealthAndNanoStim400 = 291045;
            public const int DeathsDoor = 303071;

            public const int WenWen = 129656;

            public const int Drone = 303188;

            public static readonly int[] ExpCans = new[]
           {
                303376, 288772, 288771, 288769, 288788, 288787, 288786, 288792, 288791, 288790
            };

            public static readonly int[] TauntTools = new[]
            {
                244655,  // Scorpio's Aim of Anger
                152028,  // Aggression Multiplier (Jealousy Augmented)
                253187,   // Codex of the Insulting Emerto (High)
                151693,  // Modified Aggression Enhancer (High)
                83919,   // Aggression Multiplier
                152029,  // Aggression Enhancer (Jealousy Augmented)
                151692,  // Modified Aggression Enhancer (Low)
                253186,  // Codex of the Insulting Emerto (Low)
                83920,   // Aggression Enhancer 
            };
        };

        public static class RelevantGenericNanos
        {
            public static int[] HpBuffs = new[] { 95709, 28662, 95720, 95712, 95710, 95711, 28649, 95713, 28660, 95715, 95714, 95718, 95716, 95717, 95719, 42397 };
            public const int FountainOfLife = 302907;
            public const int DanceOfFools = 210159;
            public const int Limber = 210158;
            public const int CompositeAttribute = 223372;
            public const int CompositeNano = 223380;
            public const int CompositeUtility = 287046;
            public const int CompositeMartialProwess = 302158;
            public const int CompositeMartial = 302158;
            public const int CompositeMelee = 223360;
            public const int CompositePhysicalSpecial = 215264;
            public const int CompositeRanged = 223348;
            public const int CompositeRangedSpecial = 223364;
            public const int InnerSanctumDebuff = 206387;
            public static int[] Energize = new[] { 226851, 226850, 226849, 226848, 226847, 226846, 226845, 226844, 226843, 226842 };

            public const int InsightIntoSL = 268610;

            public const int BlightedFlesh = 305492;
            public const int WeepingFlesh = 204594;

            public static int[] ShrinkingGrowingflesh = new[] { 302535, 302534, 302544, 302542, 302540, 302538, 302532, 302530 };
            public static int[] AAOTransfer = new[] { 301524, 301520, 267263, 267265 };
            public static int[] KeeperStrStamAgiBuff = new[] { 211158, 211160, 211162, 273365 };

            public static readonly int[] Hoverboards = {
                270634, 270632, 270636, 270327, 277712, 288804, 270643, 270641, 270431, 270540, 270542, 274272,
                288808, 281684, 288814, 270538, 281668, 288812, 270544, 270546,
            };

        }

        public class PetSpellData
        {
            public int ShellId;
            public int ShellId2;
            public PetType PetType;

            public PetSpellData(int shellId, PetType petType)
            {
                ShellId = shellId;
                PetType = petType;
            }
            public PetSpellData(int shellId, int shellId2, PetType petType)
            {
                ShellId = shellId;
                ShellId2 = shellId2;
                PetType = petType;
            }
        }

        public static void IPCChannel_Changed(object s, int e)
        {
            SettingsController.RemainingNCU.Clear();
            IPCChannel.SetChannelId(Convert.ToByte(e));
            Config.Save();
        }
        public static void BioCocoonPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage = e;
            BioCocoonPercentage = e;
            Config.Save();
        }

        public static void SingleTauntDelay_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelay = e;
            SingleTauntDelay = e;
            Config.Save();
        }

        public static void TimedTauntDelay_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].TimedTauntDelay = e;
            TimedTauntDelay = e;
            Config.Save();
        }

        public static void MongoDelay_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].MongoDelay = e;
            MongoDelay = e;
            Config.Save();
        }
        public static void CycleAbsorbsDelay_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].CycleAbsorbsDelay = e;
            CycleAbsorbsDelay = e;
            Config.Save();
        }
        public static void CycleChallengerDelay_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].CycleChallengerDelay = e;
            CycleChallengerDelay = e;
            Config.Save();
        }
        public static void CycleRageDelay_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].CycleRageDelay = e;
            CycleRageDelay = e;
            Config.Save();
        }
        public static void StimTargetName_Changed(object s, string e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].StimTargetName = e;
            StimTargetName = e;
            Config.Save();
        }
        public static void StimHealthPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentage = e;
            StimHealthPercentage = e;
            Config.Save();
        }
        public static void StimNanoPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentage = e;
            StimNanoPercentage = e;
            Config.Save();
        }
        public static void KitHealthPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage = e;
            KitHealthPercentage = e;
            Config.Save();
        }
        public static void KitNanoPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage = e;
            KitNanoPercentage = e;
            Config.Save();
        }
        public static void CycleXpPerksDelay_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].CycleXpPerksDelay = e;
            CycleXpPerksDelay = e;
            Config.Save();
        }

        public static void TargetHealPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].TargetHealPercentage = e;
            Healing.TargetHealPercentage = e;
            Config.Save();
        }
        public static void DragonHealingPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].DragonHealingPercentage = e;
            Healing.DragonHealingPercentage = e;
            Config.Save();
        }
        public static void CompleteHealPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentage = e;
            Healing.CompleteHealPercentage = e;
            Config.Save();
        }

        public static void FountainOfLifeHealPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentage = e;
            Healing.FountainOfLifeHealPercentage = e;
            Config.Save();
        }

        public static void TeamHealPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPercentage = e;
            Healing.TeamHealPercentage = e;
            Config.Save();
        }

        public static void CompleteTeamHealPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].CompleteTeamHealPercentage = e;
            Healing.CompleteTeamHealPercentage = e;
            Config.Save();
        }

        public static void TOTWPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].TOTWPercentage = e;
            TOTWPercentage = e;
            Config.Save();
        }

        public static void HealthDrainPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].HealthDrainPercentage = e;
            HealthDrainPercentage = e;
            Config.Save();
        }
        public static void NanoAegisPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].NanoAegisPercentage = e;
            NanoAegisPercentage = e;
            Config.Save();
        }
        public static void NullitySpherePercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].NullitySpherePercentage = e;
            NullitySpherePercentage = e;
            Config.Save();
        }
        public static void IzgimmersWealthPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].IzgimmersWealthPercentage = e;
            IzgimmersWealthPercentage = e;
            Config.Save();
        }
        public static void CycleSpherePerkDelay_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelay = e;
            CycleSpherePerkDelay = e;
            Config.Save();
        }
        public static void CycleWitOfTheAtroxPerkDelay_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelay = e;
            CycleWitOfTheAtroxPerkDelay = e;
            Config.Save();
        }

        public static void CycleBioRegrowthPerkDelay_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].CycleBioRegrowthPerkDelay = e;
            CycleBioRegrowthPerkDelay = e;
            Config.Save();
        }
        public static void BioRegrowthPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].BioRegrowthPercentage = e;
            BioRegrowthPercentage = e;
            Config.Save();
        }
        public static void SelfHealPerkPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentage = e;
            SelfHealPerkPercentage = e;
            Config.Save();
        }
        public static void SelfNanoPerkPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentage = e;
            SelfNanoPerkPercentage = e;
            Config.Save();
        }
        public static void TeamHealPerkPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentage = e;
            TeamHealPerkPercentage = e;
            Config.Save();
        }
        public static void TeamNanoPerkPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentage = e;
            TeamNanoPerkPercentage = e;
            Config.Save();
        }
        public static void BattleGroupHeal1Percentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal1Percentage = e;
            BattleGroupHeal1Percentage = e;
            Config.Save();
        }
        public static void BattleGroupHeal2Percentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal2Percentage = e;
            BattleGroupHeal2Percentage = e;
            Config.Save();
        }
        public static void BattleGroupHeal3Percentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal3Percentage = e;
            BattleGroupHeal3Percentage = e;
            Config.Save();
        }
        public static void BattleGroupHeal4Percentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal4Percentage = e;
            BattleGroupHeal4Percentage = e;
            Config.Save();
        }
        public static void DuckAbsorbsItemPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].DuckAbsorbsItemPercentage = e;
            DuckAbsorbsItemPercentage = e;
            Config.Save();
        }
        public static void BodyDevAbsorbsItemPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentage = e;
            BodyDevAbsorbsItemPercentage = e;
            Config.Save();
        }
        public static void StrengthAbsorbsItemPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentage = e;
            StrengthAbsorbsItemPercentage = e;
            Config.Save();
        }
        public static void ShadesCaressPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].ShadesCaressPercentage = e;
            ShadesCaressPercentage = e;
            Config.Save();
        }
        public static void ShadeTattooPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].ShadeTattooPercentage = e;
            ShadeTattooPercentage = e;
            Config.Save();
        }
        public static void StaminaAbsorbsItemPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].StaminaAbsorbsItemPercentage = e;
            StaminaAbsorbsItemPercentage = e;
            Config.Save();
        }

        public static int GetLineNumber(Exception ex)
        {
            var lineNumber = 0;

            var lineMatch = Regex.Match(ex.StackTrace ?? "", @":line (\d+)$", RegexOptions.Multiline);

            if (lineMatch.Success)
            {
                lineNumber = int.Parse(lineMatch.Groups[1].Value);
            }

            return lineNumber;
        }

        #endregion
    }
}