using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using static CombatHandler.Generic.PerkCondtionProcessors;

namespace CombatHandler.Generic
{
    public class GenericCombatHandler : AOSharp.Core.Combat.CombatHandler
    {
        private const float PostZonePetCheckBuffer = 2;
        public int EvadeCycleTimeoutSeconds = 180;

        private double _lastPetSyncTime = Time.NormalTime;
        protected double _lastZonedTime = Time.NormalTime;
        protected double _lastCombatTime = double.MinValue;

        private static bool _init = false;

        private static double _updateTick;

        protected readonly string PluginDir;

        protected Settings _settings;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public static bool IsActiveWindow => GetForegroundWindow() == Process.GetCurrentProcess().MainWindowHandle;

        protected static HashSet<string> debuffTargetsToIgnore = new HashSet<string>
        {
                    "Immortal Guardian",
                    "Mature Abyss Orchid",
                    "Abyss Orchid Sprout",
                    "Tower of Astodan",
                    "Spirit of Judgement",
                    "Guardian Spirit of Purification",
                    "Green Tower",
                    "Blue Tower",
                    "Alien Cocoon",
                    "Sheila Marlene",
                    "Rookie Alien Hunter",
                    "Sean Powell",
                    "Unicorn Guard",
                    "Essence Fragment",
                    "Awakened Xan",
                    "Fanatic",
                    "Harbinger of Pestilence",
                    "Pandemonium Idol"
        };

        protected static HashSet<string> debuffOSTargetsToIgnore = new HashSet<string>
        {
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
                    "Punishment"
        };

        public static IPCChannel IPCChannel;
        public static Config Config { get; private set; }

        public GenericCombatHandler(string pluginDir)
        {
            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\Generic\\{Game.ClientInst}\\Config.json");
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[Game.ClientInst].IPCChannel));

            PluginDir = pluginDir;

            _settings = new Settings("CombatHandler");

            RegisterPerkProcessors();
            RegisterPerkProcessor(PerkHash.Limber, Limber, CombatActionPriority.High);
            RegisterPerkProcessor(PerkHash.DanceOfFools, DanceOfFools, CombatActionPriority.High);

            RegisterSpellProcessor(RelevantNanos.FountainOfLife, FountainOfLife);
            RegisterItemProcessor(RelevantItems.FlowerOfLifeLow, RelevantItems.FlowerOfLifeHigh, FlowerOfLife);

            RegisterItemProcessor(RelevantItems.ReflectGraft, RelevantItems.ReflectGraft, ReflectGraft);

            RegisterItemProcessor(RelevantItems.SteamingHotCupOfEnhancedCoffee, RelevantItems.SteamingHotCupOfEnhancedCoffee, Coffee);

            RegisterItemProcessor(RelevantItems.FlurryOfBlowsLow, RelevantItems.FlurryOfBlowsHigh, DamageItem);

            RegisterItemProcessor(RelevantItems.StrengthOfTheImmortal, RelevantItems.StrengthOfTheImmortal, DamageItem);
            RegisterItemProcessor(RelevantItems.MightOfTheRevenant, RelevantItems.MightOfTheRevenant, DamageItem);
            RegisterItemProcessor(RelevantItems.BarrowStrength, RelevantItems.BarrowStrength, DamageItem);

            RegisterItemProcessor(RelevantItems.GnuffsEternalRiftCrystal, RelevantItems.GnuffsEternalRiftCrystal, DamageItem);
            RegisterItemProcessor(RelevantItems.Drone, RelevantItems.Drone, DamageItem);

            RegisterItemProcessor(RelevantItems.WenWen, RelevantItems.WenWen, DamageItem);

            RegisterItemProcessor(RelevantItems.DreadlochEnduranceBooster, RelevantItems.DreadlochEnduranceBooster, EnduranceBooster, CombatActionPriority.High);
            RegisterItemProcessor(RelevantItems.DreadlochEnduranceBoosterNanomageEdition, RelevantItems.DreadlochEnduranceBoosterNanomageEdition, EnduranceBooster, CombatActionPriority.High);
            RegisterItemProcessor(RelevantItems.WitheredFlesh, RelevantItems.WitheredFlesh, WithFlesh, CombatActionPriority.High);
            RegisterItemProcessor(RelevantItems.DesecratedFlesh, RelevantItems.DesecratedFlesh, DescFlesh, CombatActionPriority.High);
            RegisterItemProcessor(RelevantItems.AssaultClassTank, RelevantItems.AssaultClassTank, AssaultClass, CombatActionPriority.High);

            RegisterItemProcessor(RelevantItems.MeteoriteSpikes, RelevantItems.MeteoriteSpikes, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.LavaCapsule, RelevantItems.LavaCapsule, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.HSRLow, RelevantItems.HSRHigh, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.KizzermoleGumboil, RelevantItems.KizzermoleGumboil, TargetedDamageItem);

            RegisterItemProcessor(RelevantItems.UponAWaveOfSummerLow, RelevantItems.UponAWaveOfSummerHigh, TargetedDamageItem);
            RegisterItemProcessor(RelevantItems.BlessedWithThunderLow, RelevantItems.BlessedWithThunderHigh, TargetedDamageItem);

            RegisterItemProcessor(new int[] { RelevantItems.RezCan1, RelevantItems.RezCan2 }, RezCan);
            RegisterItemProcessor(new int[] { RelevantItems.ExpCan1, RelevantItems.ExpCan2 }, ExpCan);
            RegisterItemProcessor(new int[] { RelevantItems.InsuranceCan1, RelevantItems.InsuranceCan2 }, InsuranceCan);

            RegisterItemProcessor(new int[] { RelevantItems.HealthAndNanoStim1, RelevantItems.HealthAndNanoStim200, 
            RelevantItems.HealthAndNanoStim400, }, HealthAndNanoStim, CombatActionPriority.High);

            RegisterItemProcessor(new int[] { RelevantItems.PremSitKit, RelevantItems.AreteSitKit, RelevantItems.SitKit1,
            RelevantItems.SitKit100, RelevantItems.SitKit200, RelevantItems.SitKit300, RelevantItems.SitKit400 }, SitKit);

            RegisterItemProcessor(new int[] { RelevantItems.DaTaunterLow, RelevantItems.DaTaunterHigh }, TargetedDamageItem);

            RegisterItemProcessor(new int[] { RelevantItems.FreeStim1, RelevantItems.FreeStim50, RelevantItems.FreeStim100,
            RelevantItems.FreeStim200, RelevantItems.FreeStim300 }, FreeStim);


            RegisterItemProcessor(RelevantItems.AmmoBoxArrows, RelevantItems.AmmoBoxArrows, AmmoBoxArrows);
            RegisterItemProcessor(RelevantItems.AmmoBoxBullets, RelevantItems.AmmoBoxBullets, AmmoBoxBullets);
            RegisterItemProcessor(RelevantItems.AmmoBoxEnergy, RelevantItems.AmmoBoxEnergy, AmmoBoxEnergy);
            RegisterItemProcessor(RelevantItems.AmmoBoxShotgun, RelevantItems.AmmoBoxShotgun, AmmoBoxShotgun);

            RegisterSpellProcessor(RelevantNanos.CompositeNano, CompositesBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeAttribute, CompositesBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeUtility, CompositesBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeMartialProwess, CompositesBuff);

            RegisterSpellProcessor(RelevantNanos.InsightIntoSL, CompositesBuff);

            if (GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(CharacterWieldedWeapon.Melee))
            {
                //We are melee
                RegisterSpellProcessor(RelevantNanos.CompositeMartial, CompositeBuffExcludeInnerSanctum);
                RegisterSpellProcessor(RelevantNanos.CompositeMelee, CompositesBuff);
                RegisterSpellProcessor(RelevantNanos.CompositePhysicalSpecial, CompositesBuff);
            }


            if (GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(CharacterWieldedWeapon.Ranged))
            {
                //We are ranged
                RegisterSpellProcessor(RelevantNanos.CompositeRanged, CompositesBuff);
                RegisterSpellProcessor(RelevantNanos.CompositeRangedSpecial, CompositesBuff);
            }

            Game.TeleportEnded += OnZoned;
            Game.TeleportEnded += TeleportEnded;
            Team.TeamRequest += Team_TeamRequest;
            Config.CharSettings[Game.ClientInst].IPCChannelChangedEvent += IPCChannel_Changed;
            //Network.N3MessageSent += Network_N3MessageSent;

            Chat.RegisterCommand("reform", ReformCommand);
            Chat.RegisterCommand("form", FormCommand);
            Chat.RegisterCommand("disband", DisbandCommand);
            Chat.RegisterCommand("convert", RaidCommand);

            //foreach (var kvp in Config.CharSettings)
            //{
            //    kvp.Value.IPCChannelChangedEvent += (sender, e) =>
            //    {
            //        Chat.WriteLine("we're doin the things");
            //        IPCChannel.SetChannelId(Convert.ToByte(e));
            //        Config.Save();
            //    };
            //}
        }

        private void OnZoned(object s, EventArgs e)
        {
            _lastZonedTime = Time.NormalTime;
        }

        public static void IPCChannel_Changed(object s, int e)
        {
            IPCChannel.SetChannelId(Convert.ToByte(e));

            ////TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
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

        //public static void Network_N3MessageSent(object s, N3Message n3Msg)
        //{
        //    if (!IsActiveWindow || n3Msg.Identity != DynelManager.LocalPlayer.Identity) { return; }

        //    //Chat.WriteLine($"{n3Msg.Identity != DynelManager.LocalPlayer.Identity}");

        //    if (n3Msg.N3MessageType == N3MessageType.LookAt)
        //    {
        //        LookAtMessage lookAtMsg = (LookAtMessage)n3Msg;
        //        IPCChannel.Broadcast(new TargetMessage()
        //        {
        //            Target = lookAtMsg.Target
        //        });
        //    }
        //    else if (n3Msg.N3MessageType == N3MessageType.Attack)
        //    {
        //        AttackMessage attackMsg = (AttackMessage)n3Msg;
        //        IPCChannel.Broadcast(new AttackIPCMessage()
        //        {
        //            Target = attackMsg.Target
        //        });
        //    }
        //    else if (n3Msg.N3MessageType == N3MessageType.StopFight)
        //    {
        //        StopFightMessage stopAttackMsg = (StopFightMessage)n3Msg;
        //        IPCChannel.Broadcast(new StopAttackIPCMessage());
        //    }
        //}

        //public static void OnStopAttackMessage(int sender, IPCMessage msg)
        //{
        //    if (IsActiveWindow)
        //        return;

        //    if (Game.IsZoning)
        //        return;

        //    DynelManager.LocalPlayer.StopAttack();
        //}

        //public static void OnTargetMessage(int sender, IPCMessage msg)
        //{
        //    if (IsActiveWindow)
        //        return;

        //    if (Game.IsZoning)
        //        return;

        //    TargetMessage targetMsg = (TargetMessage)msg;
        //    Targeting.SetTarget(targetMsg.Target);
        //}

        //public static void OnAttackMessage(int sender, IPCMessage msg)
        //{
        //    if (IsActiveWindow)
        //        return;

        //    if (Game.IsZoning)
        //        return;

        //    AttackIPCMessage attackMsg = (AttackIPCMessage)msg;
        //    Dynel targetDynel = DynelManager.GetDynel(attackMsg.Target);
        //    DynelManager.LocalPlayer.Attack(targetDynel, true);
        //}

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
                    Team.Invite(target);
            }
        }

        public static void OnDisband(int sender, IPCMessage msg)
        {
            Team.Leave();
        }

        public static void DisbandCommand(string command, string[] param, ChatWindow chatWindow)
        {
            Team.Disband();
            IPCChannel.Broadcast(new DisbandMessage());
        }

        public static void RaidCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (Team.IsLeader)
                Team.ConvertToRaid();
            else
                Chat.WriteLine("Needs to be used from leader.");
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
            if (!DynelManager.LocalPlayer.IsInTeam())
            {
                SendTeamInvite(GetRegisteredCharactersInvite());

                if (IsRaidEnabled(param))
                {
                    Task.Factory.StartNew(
                        async () =>
                        {
                            await Task.Delay(100);
                            Team.ConvertToRaid();
                            await Task.Delay(1000);
                            SendTeamInvite(GetRemainingRegisteredCharacters());
                            await Task.Delay(100);
                        });
                }
            }
            else
            {
                Chat.WriteLine("Cannot form a team. Character already in team. Disband first.");
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            SettingsController.CleanUp();

            //Chat.WriteLine($"{SettingsController.GetRegisteredCharacters().Length}");

            //Chat.WriteLine($"{Config.CharSettings[Game.ClientInst].IPCChannel}");

            //if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.F1) && !_init
            //    && IsActiveWindow)
            //{
            //    _init = true;

            //    Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\Generic\\{Game.ClientInst}\\Config.json");

            //    SettingsController.settingsWindow = Window.Create(new Rect(50, 50, 300, 300), "CombatHandler", "Settings", WindowStyle.Default, WindowFlags.AutoScale);

            //    if (SettingsController.settingsWindow != null && !SettingsController.settingsWindow.IsVisible)
            //    {
            //        foreach (string settingsName in SettingsController.settingsWindows.Keys.Where(x => x.Contains("Handler")))
            //        {
            //            SettingsController.AppendSettingsTab(settingsName, SettingsController.settingsWindow);

            //            SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView channelInput);
            //            SettingsController.settingsWindow.FindView("EngiBioCocoonPercentageBox", out TextInputView engiBioCocoonInput);

            //            if (channelInput != null)
            //                channelInput.Text = $"{Config.CharSettings[Game.ClientInst].IPCChannel}";
            //            if (engiBioCocoonInput != null)
            //                engiBioCocoonInput.Text = $"{Config.CharSettings[Game.ClientInst].EngiBioCocoonPercentage}";
            //        }
            //    }

            //    _init = false;
            //}

            if (Time.NormalTime > _updateTick + 1f)
            {
                foreach (SimpleChar player in DynelManager.Characters
                    .Where(c => c.IsPlayer && DynelManager.LocalPlayer.DistanceFrom(c) < 30f))
                {
                    Network.Send(new CharacterActionMessage()
                    {
                        Action = CharacterActionType.InfoRequest,
                        Target = player.Identity
                    });
                }

                _updateTick = Time.NormalTime;
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView channelInput);

                if (channelInput != null && !string.IsNullOrEmpty(channelInput.Text))
                {
                    if (int.TryParse(channelInput.Text, out int channelValue)
                        && Config.CharSettings[Game.ClientInst].IPCChannel != channelValue)
                    {
                        Config.CharSettings[Game.ClientInst].IPCChannel = channelValue;
                    }
                }
            }

            if (DynelManager.LocalPlayer.IsAttacking || DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 0)
            {
                _lastCombatTime = Time.NormalTime;
            }
        }


        #region Perks

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

        public static bool TrollFormPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null)
                return false;

            return true;
        }

        protected bool CyclePerks(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Where(c => c.Name.ToLower().Contains(perk.Name.ToLower())).Any()) { return false; }

            return true;
        }

        protected bool LEProc(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Where(c => c.Name.ToLower().Contains(perk.Name.ToLower())).Any()) { return false; }

            return true;
        }

        private bool Limber(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking) { return false; }

            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.DanceOfFools, out Buff dof) && dof.RemainingTime > 12.5f) { return false; }

            // stop cycling if we haven't fought anything for over 10 minutes
            if (Time.NormalTime - _lastCombatTime > EvadeCycleTimeoutSeconds) { return false; }

            return true;
        }

        private bool DanceOfFools(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking) { return false; }

            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.Limber, out Buff limber) || limber.RemainingTime > 12.5f) { return false; }

            // stop cycling if we haven't fought anything for over 10 minutes
            if (Time.NormalTime - _lastCombatTime > EvadeCycleTimeoutSeconds) { return false; }

            return true;
        }

        #endregion

        #region Instanced Logic

        protected bool BuffInitEngi(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell) || !IsSettingEnabled("Buffing")) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                if (DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && c.Profession != Profession.Doctor && c.Profession != Profession.NanoTechnician
                            && SpellChecksOther(spell, spell.Nanoline, c)
                            && GetWieldedWeapons(c).HasFlag(CharacterWieldedWeapon.Ranged))
                    .Any())
                {
                    actionTarget.Target = DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && c.Profession != Profession.Doctor && c.Profession != Profession.NanoTechnician
                            && SpellChecksOther(spell, spell.Nanoline, c)
                            && GetWieldedWeapons(c).HasFlag(CharacterWieldedWeapon.Ranged))
                        .FirstOrDefault();

                    if (actionTarget.Target != null)
                    {
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            if (SpellChecksPlayer(spell))
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        //TODO: Add UI
        private bool FountainOfLife(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            // Prioritize keeping ourself alive
            if (DynelManager.LocalPlayer.HealthPercent <= 30)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                if (DynelManager.Characters
                    .Where(c => c.IsAlive && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && c.HealthPercent < 30)
                    .Any())
                {
                    actionTarget.Target = DynelManager.Characters
                        .Where(c => c.IsAlive && Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && c.HealthPercent < 30)
                        .FirstOrDefault();

                    if (actionTarget.Target != null)
                    {
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Logic

        protected bool GenericBuffExcludeInnerSanctum(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || IsInsideInnerSanctum() 
                || DynelManager.LocalPlayer.Buffs.Contains(NanoLine.MajorEvasionBuffs)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool CompositeBuffExcludeInnerSanctum(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !IsSettingEnabled("Composites") 
                || IsInsideInnerSanctum() || DynelManager.LocalPlayer.Buffs.Contains(NanoLine.MajorEvasionBuffs)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool AllTeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                if (DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && SpellChecksOther(spell, spell.Nanoline, c))
                    .Any())
                {
                    actionTarget.Target = DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && SpellChecksOther(spell, spell.Nanoline, c))
                        .FirstOrDefault();

                    if (actionTarget.Target != null)
                    {
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool AllBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (SpellChecksPlayer(spell))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        protected bool CombatBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !CanCast(spell) || !IsSettingEnabled("Buffing")) { return false; }

            if (SpellChecksPlayer(spell))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        protected bool DebuffTarget(Spell spell, NanoLine nanoline, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || fightingTarget == null) { return false; }

            if (SpellChecksOther(spell, nanoline, fightingTarget))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = fightingTarget;
                return true;
            }

            return false;
        }

        protected bool ToggledDebuffTarget(string settingName, Spell spell, NanoLine nanoline, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !IsSettingEnabled(settingName) || !IsSettingEnabled("Buffing")) { return false; }

            if (SpellChecksOther(spell, nanoline, fightingTarget))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = fightingTarget;
                return true;
            }

            return false;
        }

        protected bool ToggledDebuffOthersInCombat(string toggleName, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled(toggleName) || !CanCast(spell) || !IsSettingEnabled("Buffing")) { return false; }

            if (DynelManager.NPCs
                .Where(c => !debuffOSTargetsToIgnore.Contains(c.Name)
                        && c.FightingTarget != null
                        && !c.Buffs.Contains(301844)
                        && c.IsInLineOfSight
                        && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && SpellChecksOther(spell, spell.Nanoline, c))
                .Any())
            {
                actionTarget.Target = DynelManager.NPCs
                    .Where(c => !debuffOSTargetsToIgnore.Contains(c.Name)
                        && c.FightingTarget != null
                        && !c.Buffs.Contains(301844)
                        && c.IsInLineOfSight
                        && !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz)
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && SpellChecksOther(spell, spell.Nanoline, c))
                    .FirstOrDefault();

                if (actionTarget.Target != null)
                {
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected virtual bool TargetedDamageItem(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = true;
            return DamageItem(item, fightingTarget, ref actionTarget);
        }

        protected virtual bool ReflectGraft(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return !DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item)) && !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ReflectShield);
        }

        protected virtual bool DamageItem(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return !DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item)) && fightingTarget != null && fightingTarget.IsInAttackRange();
        }

        protected bool ToggledBuff(string settingName, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !IsSettingEnabled(settingName)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool TeamBuffExcludeInnerSanctum(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || IsInsideInnerSanctum()) { return false; }

            return TeamBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool NanoSkillsBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || fightingTarget != null 
                || !CanCast(spell) || RelevantNanos.IgnoreNanos.Contains(spell.Id)) { return false; }

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
            if (!IsSettingEnabled("Buffing") 
                || fightingTarget != null || !CanCast(spell) || RelevantNanos.IgnoreNanos.Contains(spell.Id)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                if (DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.Identity != DynelManager.LocalPlayer.Identity
                            && SpellChecksNanoSkillsOther(spell, c))
                    .Any())
                {
                    actionTarget.Target = DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && c.Identity != DynelManager.LocalPlayer.Identity
                            && SpellChecksNanoSkillsOther(spell, c))
                        .FirstOrDefault();

                    if (actionTarget.Target != null)
                    {
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            return false;
        }

        protected bool TeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") 
                || fightingTarget != null || !CanCast(spell) || RelevantNanos.IgnoreNanos.Contains(spell.Id)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                if (DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && SpellChecksOther(spell, spell.Nanoline, c))
                    .Any())
                {
                    actionTarget.Target = DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && SpellChecksOther(spell, spell.Nanoline, c))
                        .FirstOrDefault();

                    if (actionTarget.Target != null)
                    {
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool CompositesBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") 
                || !IsSettingEnabled("Composites")
                || fightingTarget != null || !CanCast(spell) || RelevantNanos.IgnoreNanos.Contains(spell.Id)) { return false; }

            if (SpellChecksPlayer(spell))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        protected bool GenericBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") 
                || fightingTarget != null || !CanCast(spell) || RelevantNanos.IgnoreNanos.Contains(spell.Id)) { return false; }

            if (SpellChecksPlayer(spell))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        protected bool TeamBuffNoNTWeaponType(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, CharacterWieldedWeapon supportedWeaponType)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                if (DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && c.Profession != Profession.NanoTechnician
                            && SpellChecksOther(spell, spell.Nanoline, c)
                            && GetWieldedWeapons(c).HasFlag(supportedWeaponType))
                    .Any())
                {
                    actionTarget.Target = DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                                && c.Profession != Profession.NanoTechnician
                                && SpellChecksOther(spell, spell.Nanoline, c)
                                && GetWieldedWeapons(c).HasFlag(supportedWeaponType))
                        .FirstOrDefault();

                    if (actionTarget.Target != null)
                    {
                        if (actionTarget.Target.Buffs.Contains(NanoLine.FixerSuppressorBuff) &&
                            (spell.Nanoline == NanoLine.FixerSuppressorBuff || spell.Nanoline == NanoLine.AssaultRifleBuffs)) { return false; }

                        if (actionTarget.Target.Buffs.Contains(NanoLine.PistolBuff) &&
                            spell.Nanoline == NanoLine.PistolBuff) { return false; }

                        if (actionTarget.Target.Buffs.Contains(NanoLine.AssaultRifleBuffs) &&
                            (spell.Nanoline == NanoLine.AssaultRifleBuffs || spell.Nanoline == NanoLine.GrenadeBuffs)) { return false; }

                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            return false;
        }

        protected bool TeamBuffWeaponType(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, CharacterWieldedWeapon supportedWeaponType)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                if (DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && SpellChecksOther(spell, spell.Nanoline, c)
                            && GetWieldedWeapons(c).HasFlag(supportedWeaponType))
                    .Any())
                {
                    actionTarget.Target = DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && SpellChecksOther(spell, spell.Nanoline, c)
                            && GetWieldedWeapons(c).HasFlag(supportedWeaponType))
                        .FirstOrDefault();

                    if (actionTarget.Target != null)
                    {
                        if (actionTarget.Target.Buffs.Contains(NanoLine.FixerSuppressorBuff) &&
                            (spell.Nanoline == NanoLine.FixerSuppressorBuff || spell.Nanoline == NanoLine.AssaultRifleBuffs)) { return false; }

                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            return false;
        }

        protected bool BuffWeaponType(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget, CharacterWieldedWeapon supportedWeaponType)
        {
            if (!IsSettingEnabled("Buffing") || fightingTarget != null || !CanCast(spell)) { return false; }

            if (SpellChecksPlayer(spell) && GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(supportedWeaponType))
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        public bool IsAttackingUs(SimpleChar mob)
        {
            if (Team.IsInTeam)
            {
                if (Team.Members.Select(m => m.Character.FightingTarget != null).Any())
                {
                    return !Team.Members.Select(m => m.Name).Contains(mob.FightingTarget.Name);
                }

                return Team.Members.Select(m => m.Name).Contains(mob.FightingTarget.Name);
            }

            if (DynelManager.LocalPlayer.FightingTarget != null)
            {
                return mob.FightingTarget.Name == DynelManager.LocalPlayer.Name
                    && DynelManager.LocalPlayer.FightingTarget.Identity != mob.Identity;
            }

            return mob.FightingTarget.Name == DynelManager.LocalPlayer.Name;
        }


        public bool IsNotFightingMe(SimpleChar target)
        {
            return target.IsAttacking && target.FightingTarget.Identity != DynelManager.LocalPlayer.Identity;
        }

        // expression body method / inline method   
        public static CharacterWieldedWeapon GetWieldedWeapons(SimpleChar local) => (CharacterWieldedWeapon)local.GetStat(Stat.EquippedWeapons);

        protected bool RangedBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) => BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Ranged);

        protected bool MeleeBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget) => BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Melee);

        protected bool PistolSelfBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
                return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }

        protected bool PistolMasteryBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam)
            {
                return TeamBuffNoNTWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
            }
            else
                return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Pistol);
        }
        #endregion

        #region Items

        private bool RezCan(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
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

            actiontarget.ShouldSetTarget = true;
            return true;

        }

        protected bool TauntTool(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null || !IsSettingEnabled("ScorpioTauntTool")
                || DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Psychology)) { return false; }

            actionTarget.Target = fightingTarget;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        private bool InsuranceCan(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.FirstAid)
                || DynelManager.LocalPlayer.GetStat(Stat.UnsavedXP) == 0
                || DynelManager.LocalPlayer.Buffs.Contains(300727)) { return false; }

            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = false;
            return true;

        }

        private bool ExpCan(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.FirstAid)) { return false; }

            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = false;
            return true;

        }

        private bool FlowerOfLife(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (fightingtarget == null || DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item))) { return false; }

            int approximateHealing = item.QualityLevel * 10;

            return DynelManager.LocalPlayer.MissingHealth > approximateHealing;
        }

        private bool SitKit(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Treatment)
                || (DynelManager.LocalPlayer.HealthPercent >= 66 && DynelManager.LocalPlayer.NanoPercent >= 66)) { return false; }

            actionTarget.Target = DynelManager.LocalPlayer;
            actionTarget.ShouldSetTarget = true;
            return true;
        }

        private bool HealthAndNanoStim(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.FirstAid) 
                || DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) >= 8
                || (DynelManager.LocalPlayer.Buffs.Contains(280470) || DynelManager.LocalPlayer.Buffs.Contains(258231))) { return false; }

            int targetHealing = item.UseModifiers
                .Where(x => x is SpellData.Healing hx && hx.ApplyOn == SpellModifierTarget.Target)
                .Cast<SpellData.Healing>()
                .Sum(x => x.Average);

            if (DynelManager.LocalPlayer.MissingHealth >= targetHealing || DynelManager.LocalPlayer.MissingNano >= targetHealing)
            {
                actiontarget.ShouldSetTarget = true;
                actiontarget.Target = DynelManager.LocalPlayer;

                return true;
            }

            return false;
        }

        private bool AmmoBoxBullets(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.WeaponSmithing)) { return false; }

            return !Inventory.Items
                .Where(c => c.Name == "Ammo: Box of Bullets")
                .Any();

            //Item bulletsammo = Inventory.Items
            //    .Where(c => c.Name.Contains("Bullets") && !c.Name.Contains("Crate"))
            //    .FirstOrDefault();

            //return bulletsammo == null && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.WeaponSmithing);
        }

        private bool AmmoBoxEnergy(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.WeaponSmithing)) { return false; }

            return !Inventory.Items
                .Where(c => c.Name == "Ammo: Box of Energy Weapon Ammo")
                .Any();


            //Item energyammo = Inventory.Items
            //    .Where(c => c.Name.Contains("Energy") && !c.Name.Contains("Crate"))
            //    .FirstOrDefault();

            //return energyammo == null && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.WeaponSmithing);
        }

        private bool AmmoBoxShotgun(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.WeaponSmithing)) { return false; }

            return !Inventory.Items
                .Where(c => c.Name == "Ammo: Box of Shotgun Shells")
                .Any();

            //Item shotgunammo = Inventory.Items
            //    .Where(c => c.Name.Contains("Shotgun") && !c.Name.Contains("Crate"))
            //    .FirstOrDefault();

            //return shotgunammo == null && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.WeaponSmithing);
        }

        private bool AmmoBoxArrows(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.WeaponSmithing)) { return false; }

            return !Inventory.Items
                .Where(c => c.Name == "Ammo: Box of Arrows")
                .Any();

            //Item arrowammo = Inventory.Items
            //    .Where(c => c.Name.Contains("Arrows") && !c.Name.Contains("Crate"))
            //    .FirstOrDefault();

            //return arrowammo == null && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.WeaponSmithing);
        }

        private bool EnduranceBooster(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            //if (Inventory.Find(305476, 305476, out Item absorbdesflesh))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BodyDevelopment)) { return false; }
            //}
            //if (Inventory.Find(204698, 204698, out Item absorbwithflesh))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BodyDevelopment)) { return false; }
            //}
            //if (Inventory.Find(156576, 156576, out Item absorbassaultclass))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.DuckExp)) { return false; }
            //}

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength) 
                || DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)
                || Item.HasPendingUse
                || DynelManager.LocalPlayer.HealthPercent > 75) { return false; }

            return item != null;
        }

        private bool AssaultClass(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.DuckExp) 
                || DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)
                || Item.HasPendingUse
                || DynelManager.LocalPlayer.HealthPercent > 75
                //|| DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0
                ) { return false; }

            return item != null;
        }

        private bool DescFlesh(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            //if (Inventory.Find(267168, 267168, out Item enduranceabsorbenf))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength)) { return false; }
            //}
            //if (Inventory.Find(267167, 267167, out Item enduranceabsorbnanomage))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength)) { return false; }
            //}
            //if (Inventory.Find(156576, 156576, out Item absorbassaultclass))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.DuckExp)) { return false; }
            //}

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BodyDevelopment) 
                || DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)
                || Item.HasPendingUse
                || DynelManager.LocalPlayer.HealthPercent > 75) { return false; }

            return item != null;
        }

        private bool WithFlesh(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            //Inventory.Find(305476, 305476, out Item absorbdesflesh);

            //if (Inventory.Find(267168, 267168, out Item enduranceabsorbenf))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength)) { return false; }
            //}
            //if (Inventory.Find(267167, 267167, out Item enduranceabsorbnanomage))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength)) { return false; }
            //}
            //if (Inventory.Find(156576, 156576, out Item absorbassaultclass))
            //{
            //    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.DuckExp)) { return false; }
            //}

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BodyDevelopment) 
                || DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)
                || Item.HasPendingUse
                || DynelManager.LocalPlayer.HealthPercent > 75) { return false; }

            return item != null;
        }

        protected virtual bool Coffee(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.FoodandDrinkBuffs)) { return false; }

            return DamageItem(item, fightingTarget, ref actionTarget);
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
            if (!petData.ContainsKey(spell.Id) || Inventory.Find(petData[spell.Id].ShellId, out Item shell)) { return false; }

            return NoShellPetSpawner(petData[spell.Id].PetType, spell, fightingTarget, ref actionTarget);
        }

        protected virtual bool PetSpawnerItem(Dictionary<int, PetSpellData> petData, Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("SpawnPets") || !CanLookupPetsAfterZone()
                || (!petData.Values.Any(x => (x.ShellId == item.Id || x.ShellId == item.HighId) 
                    && !DynelManager.LocalPlayer.Pets.Any(p => p.Type == x.PetType)))) { return false; }

            actionTarget.ShouldSetTarget = false;
            return true;
        }

        protected bool CanSpawnPets(PetType petType)
        {
            if (!IsSettingEnabled("SpawnPets") || !CanLookupPetsAfterZone() || PetAlreadySpawned(petType)) { return false; }

            return true;
        }

        private bool PetAlreadySpawned(PetType petType)
        {
            return DynelManager.LocalPlayer.Pets.Any(c => (c.Type == PetType.Unknown || c.Type == petType));
        }

        protected bool CanLookupPetsAfterZone()
        {
            return Time.NormalTime > _lastZonedTime + PostZonePetCheckBuffer;
        }

        protected bool PetTargetBuff(NanoLine buffNanoLine, PetType petType, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("BuffPets") || !CanLookupPetsAfterZone()) { return false; }

            if (DynelManager.LocalPlayer.Pets
                .Where(c => c.Type == petType
                    && !c.Character.Buffs.Contains(buffNanoLine))
                .Any())
            {
                actionTarget.Target = DynelManager.LocalPlayer.Pets
                    .Where(c => c.Type == petType
                        && !c.Character.Buffs.Contains(buffNanoLine))
                    .FirstOrDefault().Character;

                if (actionTarget.Target != null)
                {
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected void SynchronizePetCombatStateWithOwner()
        {
            if (CanLookupPetsAfterZone() && Time.NormalTime - _lastPetSyncTime > 1)
            {
                foreach (Pet _pet in DynelManager.LocalPlayer.Pets.Where(c => c.Type == PetType.Attack || c.Type == PetType.Support))
                    SynchronizePetCombatState(_pet);

                _lastPetSyncTime = Time.NormalTime;
            }
        }

        private void SynchronizePetCombatState(Pet pet)
        {
            if (!DynelManager.LocalPlayer.IsAttacking && pet?.Character.IsAttacking == true)
                pet?.Follow();

            if (DynelManager.LocalPlayer.IsAttacking && DynelManager.LocalPlayer.FightingTarget != null)
            {
                if (pet?.Character.IsAttacking == false)
                    pet?.Attack(DynelManager.LocalPlayer.FightingTarget.Identity);

                if (pet?.Character.IsAttacking == true && pet?.Character.FightingTarget != null
                    && pet?.Character.FightingTarget.Identity != DynelManager.LocalPlayer.FightingTarget.Identity)
                        pet?.Attack(DynelManager.LocalPlayer.FightingTarget.Identity);
            }
        }

        #endregion

        #region Checks

        protected bool HasBuffNanoLine(NanoLine nanoLine, SimpleChar target)
        {
            return target.Buffs.Contains(nanoLine);
        }

        protected bool CheckNotProfsBeforeCast(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                if (DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                                && c.Profession != Profession.Keeper && c.Profession != Profession.Engineer
                                && SpellChecksOther(spell, spell.Nanoline, c))
                    .Any())
                {
                    actionTarget.Target = DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                                && c.Profession != Profession.Keeper && c.Profession != Profession.Engineer
                                && SpellChecksOther(spell, spell.Nanoline, c))
                        .FirstOrDefault();

                    if (actionTarget.Target != null)
                    {
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            return false;
        }

        //This should be a float to be more accurate?
        protected bool FindMemberWithHealthBelow(int healthPercentTreshold, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.HealthPercent <= healthPercentTreshold)
            {
                //Task.Factory.StartNew(
                //    async () =>
                //    {
                //        DynelManager.LocalPlayer.SetStat(Stat.AggDef, 100);
                //        await Task.Delay(444);
                //        DynelManager.LocalPlayer.SetStat(Stat.AggDef, -100);
                //    });

                //if (DynelManager.LocalPlayer.GetStat(Stat.AggDef) == 100)
                //{
                //    actionTarget.Target = DynelManager.LocalPlayer;
                //    return true;
                //}

                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                if (DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                        && c.HealthPercent <= healthPercentTreshold && c.IsInLineOfSight)
                    .OrderBy(c => c.Profession == Profession.Doctor)
                    .ThenBy(c => c.Profession == Profession.Enforcer)
                    .ThenBy(c => c.HealthPercent)
                    .Any())
                {
                    actionTarget.Target = DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                            && c.HealthPercent <= healthPercentTreshold && c.IsInLineOfSight)
                        .OrderBy(c => c.Profession == Profession.Doctor)
                        .ThenBy(c => c.Profession == Profession.Enforcer)
                        .ThenBy(c => c.HealthPercent)
                        .FirstOrDefault();

                    if (actionTarget.Target != null)
                    {
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            return false;
        }

        protected bool FindPlayerWithHealthBelow(int healthPercentTreshold, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.HealthPercent <= healthPercentTreshold)
            {
                //Task.Factory.StartNew(
                //    async () =>
                //    {
                //        DynelManager.LocalPlayer.SetStat(Stat.AggDef, 100);
                //        await Task.Delay(444);
                //        DynelManager.LocalPlayer.SetStat(Stat.AggDef, -100);
                //    });

                //if (DynelManager.LocalPlayer.GetStat(Stat.AggDef) == 100)
                //{
                //    actionTarget.Target = DynelManager.LocalPlayer;
                //    return true;
                //}

                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            if (DynelManager.Characters
                .Where(c => c.IsPlayer && c.HealthPercent <= healthPercentTreshold
                    && c.DistanceFrom(DynelManager.LocalPlayer) < 30f && c.IsInLineOfSight)
                .OrderBy(c => c.Profession == Profession.Doctor)
                .ThenBy(c => c.Profession == Profession.Enforcer)
                .Any())
            {
                actionTarget.Target = DynelManager.Characters
                    .Where(c => c.IsPlayer && c.HealthPercent <= healthPercentTreshold
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f && c.IsInLineOfSight)
                    .OrderBy(c => c.Profession == Profession.Doctor)
                    .ThenBy(c => c.Profession == Profession.Enforcer)
                    .FirstOrDefault();

                if (actionTarget.Target != null)
                {
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        protected bool SpellChecksNanoSkillsPlayer(Spell spell, SimpleChar fightingTarget)
        {
            if (!IsSettingEnabled("Buffing")
                || !CanCast(spell)
                || Playfield.ModelIdentity.Instance == 152) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Find(spell.Nanoline, out Buff buff))
            {
                if (spell.StackingOrder < buff.StackingOrder || DynelManager.LocalPlayer.RemainingNCU < spell.NCU) { return false; }

                return buff.RemainingTime < 10;
            }

            return false;
        }

        protected bool SpellChecksNanoSkillsOther(Spell spell, SimpleChar fightingTarget)
        {
            if (!IsSettingEnabled("Buffing")
                || !CanCast(spell)
                || Playfield.ModelIdentity.Instance == 152
                || (fightingTarget.IsPlayer && !SettingsController.IsCharacterRegistered(fightingTarget.Identity))) { return false; }

            if (fightingTarget.Buffs.Find(spell.Nanoline, out Buff buff))
            {
                if (spell.StackingOrder < buff.StackingOrder || fightingTarget.IsPlayer && !HasNCU(spell, fightingTarget)) { return false; }

                return buff.RemainingTime < 10;
            }

            return false;
        }
        protected bool SpellChecksOther(Spell spell, NanoLine nanoline, SimpleChar fightingTarget)
        {
            if (!IsSettingEnabled("Buffing")
                || !CanCast(spell)
                || Playfield.ModelIdentity.Instance == 152
                || (fightingTarget.IsPlayer && !SettingsController.IsCharacterRegistered(fightingTarget.Identity))) { return false; }

            if (fightingTarget.Buffs.Find(nanoline, out Buff buff))
            {
                if (spell.StackingOrder < buff.StackingOrder || (fightingTarget.IsPlayer && !HasNCU(spell, fightingTarget))) { return false; }

                if (spell.NanoSchool != NanoSchool.Combat && spell.StackingOrder == buff.StackingOrder && buff.RemainingTime > 20f) { return false; }

                if (spell.NanoSchool == NanoSchool.Combat && spell.StackingOrder == buff.StackingOrder && buff.RemainingTime > 5f) { return false; }

                return true;
            }

            if (fightingTarget.IsPlayer && !HasNCU(spell, fightingTarget)) { return false; }

            return true;
        }

        protected bool SpellChecksPlayer(Spell spell)
        {
            if (!IsSettingEnabled("Buffing")
                || !CanCast(spell)
                || Playfield.ModelIdentity.Instance == 152) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Find(spell.Nanoline, out Buff buff))
            {
                if (spell.StackingOrder < buff.StackingOrder || DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU)) { return false; }

                if (spell.StackingOrder == buff.StackingOrder && buff.RemainingTime > 20f) { return false; }

                return true;
            }

            return DynelManager.LocalPlayer.RemainingNCU >= spell.NCU;
        }

        protected bool CanCast(Spell spell)
        {
            return spell.Cost < DynelManager.LocalPlayer.Nano;
        }

        public static void CancelBuffs(int[] buffsToCancel)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs)
            {
                if (buffsToCancel.Contains(buff.Id))
                    buff.Remove();
            }
        }

        protected bool IsSettingEnabled(string settingName)
        {
            return _settings[settingName].AsBool();
        }

        protected bool HasNCU(Spell spell, SimpleChar target)
        {
            return SettingsController.GetRemainingNCU(target.Identity) > spell.NCU;
        }

        private void TeleportEnded(object sender, EventArgs e)
        {
            _lastCombatTime = double.MinValue;
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
            return DynelManager.LocalPlayer.Buffs.Any(buff => buff.Id == RelevantNanos.InnerSanctumDebuff);
        }

        #endregion

        #region Misc

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

        // This will eventually be done dynamically but for now I will implement
        // it statically so we can have it functional
        private Stat GetSkillLockStat(Item item)
        {
            switch (item.HighId)
            {
                case RelevantItems.ReflectGraft:
                    return Stat.SpaceTime;
                case RelevantItems.UponAWaveOfSummerLow:
                case RelevantItems.UponAWaveOfSummerHigh:
                    return Stat.Riposte;
                case RelevantItems.FlowerOfLifeLow:
                case RelevantItems.FlowerOfLifeHigh:
                case RelevantItems.BlessedWithThunderLow:
                case RelevantItems.BlessedWithThunderHigh:
                    return Stat.MartialArts;
                case RelevantItems.FlurryOfBlowsLow:
                case RelevantItems.FlurryOfBlowsHigh:
                    return Stat.AggDef;
                case RelevantItems.StrengthOfTheImmortal:
                case RelevantItems.MightOfTheRevenant:
                case RelevantItems.BarrowStrength:
                    return Stat.Strength;
                case RelevantItems.MeteoriteSpikes:
                case RelevantItems.LavaCapsule:
                case RelevantItems.KizzermoleGumboil:
                    return Stat.SharpObject;
                case RelevantItems.SteamingHotCupOfEnhancedCoffee:
                    return Stat.RunSpeed;
                case RelevantItems.GnuffsEternalRiftCrystal:
                    return Stat.MapNavigation;
                case RelevantItems.Drone:
                    return Stat.MaterialCreation;
                case RelevantItems.WenWen:
                    return Stat.RangedEnergy;
                case RelevantItems.DaTaunterLow:
                case RelevantItems.DaTaunterHigh:
                    return Stat.Psychology;
                case RelevantItems.HSRLow:
                case RelevantItems.HSRHigh:
                    return Stat.Grenade;
                default:
                    throw new Exception($"No skill lock stat defined for item id {item.HighId}");
            }
        }


        private static class RelevantItems
        {
            public const int ReflectGraft = 95225;
            public const int FlurryOfBlowsLow = 85907;
            public const int FlurryOfBlowsHigh = 85908;
            public const int StrengthOfTheImmortal = 305478;
            public const int MightOfTheRevenant = 206013;
            public const int BarrowStrength = 204653;
            public const int LavaCapsule = 245990;
            public const int WitheredFlesh = 204698;
            public const int DesecratedFlesh = 305476;
            public const int AssaultClassTank = 156576;
            public const int HSRLow = 164780;
            public const int HSRHigh = 164781;
            public const int KizzermoleGumboil = 245323;
            public const int SteamingHotCupOfEnhancedCoffee = 157296;
            public const int DreadlochEnduranceBooster = 267168;
            public const int DreadlochEnduranceBoosterNanomageEdition = 267167;
            public const int MeteoriteSpikes = 244204;
            public const int FlowerOfLifeLow = 70614;
            public const int FlowerOfLifeHigh = 204326;
            public const int UponAWaveOfSummerLow = 205405;
            public const int UponAWaveOfSummerHigh = 205406;
            public const int BlessedWithThunderLow = 70612;
            public const int BlessedWithThunderHigh = 204327;
            public const int GnuffsEternalRiftCrystal = 303179;
            public const int Drone = 303188;
            public const int RezCan1 = 301070;
            public const int RezCan2 = 303390;
            public const int ExpCan1 = 288769;
            public const int ExpCan2 = 303376;
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
            public const int AmmoBoxEnergy = 303138;
            public const int AmmoBoxShotgun = 303141;
            public const int AmmoBoxBullets = 303137;
            public const int AmmoBoxArrows = 303136;
            public const int DaTaunterLow = 158045;
            public const int DaTaunterHigh = 158046;
            public const int WenWen = 129656;
        };

        public static class RelevantNanos
        {
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
            public const int InsightIntoSL = 268610;
            public static int[] IgnoreNanos = new[] { 302535, 302534, 302544, 302542, 302540, 302538, 302532, 302530 };
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
        #endregion
    }
}