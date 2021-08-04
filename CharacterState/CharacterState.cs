using AOSharp.Character;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using CombatHandler.Generic.IPCMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Character.State
{
    public class CharacterState : AOPluginEntry
    {
        private static Dictionary<Identity, int> RemainingNCU = new Dictionary<Identity, int>();
        private static Dictionary<Identity, CharacterWeaponType> WeaponType = new Dictionary<Identity, CharacterWeaponType>();
        private static Dictionary<Identity, SpecialAttacks> SupportedSpecialAttacks = new Dictionary<Identity, SpecialAttacks>();

        private static IPCChannel IPCChannel;

        private static double _lastUpdateTime = 0;

        private static Settings settings = new Settings("CharacterState");

        private static bool justusedsitkit = false;

        public static bool AutoSitSwitch = false;

        public const double satdowntoontimer = 0.9f;
        public static double _satdowntoontime;

        public const double satdowntoontimer2 = 15f;
        public static double _satdowntoontime2;

        private byte _channelId;
        public override void Run(string pluginDir)
        {
            settings.AddVariable("ChannelID", 11);

            _channelId = Convert.ToByte(settings["ChannelID"].AsInt32());

            IPCChannel = new IPCChannel(_channelId);

            Chat.WriteLine($"IPC Channel for CharacterState - {_channelId}");

            IPCChannel.RegisterCallback((int)IPCOpcode.CharacterState, OnCharacterStateMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.CharacterSpecials, OnCharacterSpecialsMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

            Chat.RegisterCommand("statechannel", channelcommand);

            SettingsController.RegisterSettings(settings);

            Game.OnUpdate += ReportCharacterState;
            new TeamCommands().RegisterCommands();
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        public static void BroadcastDisband()
        {
            IPCChannel.Broadcast(new DisbandMessage());
        }
        public static void BroadcastUseGrid()
        {
            IPCChannel.Broadcast(new UseGrid());
        }

        public static void BroadcastUseFGrid()
        {
            IPCChannel.Broadcast(new UseFGrid());
        }

        private void channelcommand(string command, string[] param, ChatWindow chatWindow)
        {
            try
            {
                if (IPCChannel != null)
                {

                    if (param.Length == 0)
                    {
                        Chat.WriteLine($"IPC Channel for CharacterState is - {_channelId}");
                    }

                    if (param.Length > 0)
                    {
                        _channelId = Convert.ToByte(param[0]);

                        IPCChannel.SetChannelId(_channelId);
                        settings["ChannelID"] = _channelId;

                        Chat.WriteLine($"IPC Channel for CharacterState is now - {_channelId}");
                    }
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        public static int GetRemainingNCU(Identity target)
        {
            return CharacterState.RemainingNCU.ContainsKey(target) ? CharacterState.RemainingNCU[target] : 0;
        }

        public static CharacterWieldedWeapon GetWieldedWeapon(LocalPlayer local)
        {
            if (local.GetStat(Stat.EquippedWeapons) == 1028)
                return CharacterWieldedWeapon.Pistol;
            if (local.GetStat(Stat.EquippedWeapons) == 8196)
                return CharacterWieldedWeapon.Shotgun;
            if (local.GetStat(Stat.EquippedWeapons) == 3076)
                return CharacterWieldedWeapon.PistolAndAssaultRifle;
            if (local.GetStat(Stat.EquippedWeapons) == 4100)
                return CharacterWieldedWeapon.Rifle;
            if (local.GetStat(Stat.EquippedWeapons) == 2052)
                return CharacterWieldedWeapon.AssaultRifle;
            if (local.GetStat(Stat.EquippedWeapons) == 20)
                return CharacterWieldedWeapon.Smg;
            if (local.GetStat(Stat.EquippedWeapons) == 12)
                return CharacterWieldedWeapon.Bow;
            if (local.GetStat(Stat.EquippedWeapons) == 1)
                return CharacterWieldedWeapon.Fists;
            if (local.GetStat(Stat.EquippedWeapons) == 258)
                return CharacterWieldedWeapon.Blunt2H;
            if (local.GetStat(Stat.EquippedWeapons) == 66)
                return CharacterWieldedWeapon.Blunt1H;
            if (local.GetStat(Stat.EquippedWeapons) == 130)
                return CharacterWieldedWeapon.Edged2H;
            if (local.GetStat(Stat.EquippedWeapons) == 34)
                return CharacterWieldedWeapon.Edged1H;
            if (local.GetStat(Stat.EquippedWeapons) == 514)
                return CharacterWieldedWeapon.Piercing;
            if (local.GetStat(Stat.EquippedWeapons) == 546)
                return CharacterWieldedWeapon.Edged1HAndPiercing;
            if (local.GetStat(Stat.EquippedWeapons) == 98)
                return CharacterWieldedWeapon.Blunt1HAndEdged1H;
            if (local.GetStat(Stat.EquippedWeapons) == 16450)
                return CharacterWieldedWeapon.Blunt1HAndEnergy;
            if (local.GetStat(Stat.EquippedWeapons) == 578)
                return CharacterWieldedWeapon.Blunt1HAndPiercing;
            if (local.GetStat(Stat.EquippedWeapons) == 32772)
                return CharacterWieldedWeapon.Grenade;
            if (local.GetStat(Stat.EquippedWeapons) == 65540)
                return CharacterWieldedWeapon.HeavyWeapons;
            if (local.GetStat(Stat.EquippedWeapons) == 16386)
                return CharacterWieldedWeapon.Energy;

            return CharacterWieldedWeapon.Invalid;
        }
        public static CharacterWieldedWeapon GetWieldedWeaponOther(SimpleChar local)
        {
            if (local.GetStat(Stat.EquippedWeapons) == 1028)
                return CharacterWieldedWeapon.Pistol;
            if (local.GetStat(Stat.EquippedWeapons) == 8196)
                return CharacterWieldedWeapon.Shotgun;
            if (local.GetStat(Stat.EquippedWeapons) == 3076)
                return CharacterWieldedWeapon.PistolAndAssaultRifle;
            if (local.GetStat(Stat.EquippedWeapons) == 4100)
                return CharacterWieldedWeapon.Rifle;
            if (local.GetStat(Stat.EquippedWeapons) == 2052)
                return CharacterWieldedWeapon.AssaultRifle;
            if (local.GetStat(Stat.EquippedWeapons) == 20)
                return CharacterWieldedWeapon.Smg;
            if (local.GetStat(Stat.EquippedWeapons) == 12)
                return CharacterWieldedWeapon.Bow;
            if (local.GetStat(Stat.EquippedWeapons) == 1)
                return CharacterWieldedWeapon.Fists;
            if (local.GetStat(Stat.EquippedWeapons) == 258)
                return CharacterWieldedWeapon.Blunt2H;
            if (local.GetStat(Stat.EquippedWeapons) == 66)
                return CharacterWieldedWeapon.Blunt1H;
            if (local.GetStat(Stat.EquippedWeapons) == 130)
                return CharacterWieldedWeapon.Edged2H;
            if (local.GetStat(Stat.EquippedWeapons) == 34)
                return CharacterWieldedWeapon.Edged1H;
            if (local.GetStat(Stat.EquippedWeapons) == 514)
                return CharacterWieldedWeapon.Piercing;
            if (local.GetStat(Stat.EquippedWeapons) == 546)
                return CharacterWieldedWeapon.Edged1HAndPiercing;
            if (local.GetStat(Stat.EquippedWeapons) == 98)
                return CharacterWieldedWeapon.Blunt1HAndEdged1H;
            if (local.GetStat(Stat.EquippedWeapons) == 16450)
                return CharacterWieldedWeapon.Blunt1HAndEnergy;
            if (local.GetStat(Stat.EquippedWeapons) == 578)
                return CharacterWieldedWeapon.Blunt1HAndPiercing;
            if (local.GetStat(Stat.EquippedWeapons) == 32772)
                return CharacterWieldedWeapon.Grenade;
            if (local.GetStat(Stat.EquippedWeapons) == 65540)
                return CharacterWieldedWeapon.HeavyWeapons;
            if (local.GetStat(Stat.EquippedWeapons) == 16386)
                return CharacterWieldedWeapon.Energy;

            return CharacterWieldedWeapon.Invalid;
        }

        public static CharacterWeaponType GetWeaponType(Identity target)
        {
            return CharacterState.WeaponType.ContainsKey(target) ? CharacterState.WeaponType[target] : CharacterWeaponType.UNAVAILABLE;
        }

        public static Identity[] GetRegisteredCharacters()
        {
            return CharacterState.RemainingNCU.Keys.ToArray();
        }

        public static bool IsCharacterRegistered(Identity target)
        {
            return CharacterState.RemainingNCU.ContainsKey(target);
        }        

        public static SpecialAttacks GetSpecialAttacks(Identity target)
        {
            return CharacterState.SupportedSpecialAttacks.ContainsKey(target) ? CharacterState.SupportedSpecialAttacks[target] : new SpecialAttacks();
        }

        public static void OnCharacterSpecialsMessage(int sender, IPCMessage msg)
        {
            try
            {
                if (Game.IsZoning)
                    return;

                CharacterSpecialsMessage specialsMessage = (CharacterSpecialsMessage)msg;
                SupportedSpecialAttacks[specialsMessage.Character] = SpecialAttacks.FromMessage(specialsMessage);
            }
            catch (Exception e)
            {
                Chat.WriteLine(e);
            }
        }

        private static void OnCharacterStateMessage(int sender, IPCMessage msg)
        {
            try
            {
                if (Game.IsZoning)
                    return;

                CharacterStateMessage stateMessage = (CharacterStateMessage)msg;
                RemainingNCU[stateMessage.Character] = stateMessage.RemainingNCU;
                WeaponType[stateMessage.Character] = stateMessage.WeaponType;
            } catch(Exception e)
            {
                Chat.WriteLine(e);
            }
        }

        private static void OnDisband(int sender, IPCMessage msg)
        {
            Team.Leave();
        }

        private static bool IsFightingAny()
        {
            SimpleChar target = DynelManager.NPCs
                .Where(x => x.IsAlive)
                .Where(x => x.FightingTarget != null)
                .Where(x => x.FightingTarget.Identity == DynelManager.LocalPlayer.Identity)
                .FirstOrDefault();

            if (target == null)

            if (target != null)
            {
                if (Team.IsInTeam)
                {
                    // maybe some sort of assist function??
                    return (target.IsAttacking && Team.Members.Any(x => target.FightingTarget.Identity == x.Character.Identity)) ||
                        (target.IsAttacking && Team.Members.Where(x => x.Character.FightingTarget != null).Any(x => x.Character.FightingTarget.Identity == target.Identity));
                }
                else
                {
                    return target.IsAttacking && target.FightingTarget.Identity == DynelManager.LocalPlayer.Identity ||
                        target.IsAttacking && DynelManager.LocalPlayer.Pets.Any(pet => target.FightingTarget.Identity == pet.Identity);
                }
            }

            return false;
        }

        private static void ReportCharacterState(object sender, float deltaTime)
        {
            if (Time.NormalTime - _lastUpdateTime > 1)
            {
                CharacterStateMessage stateMessage = CharacterStateMessage.ForLocalPlayer();
                CharacterSpecialsMessage specialsMessage = CharacterSpecialsMessage.ForLocalPlayer();

                IPCChannel.Broadcast(stateMessage);
                IPCChannel.Broadcast(specialsMessage);

                OnCharacterSpecialsMessage(0, specialsMessage);
                OnCharacterStateMessage(0, stateMessage);

                if (!IsFightingAny() && DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0 && !Team.IsInCombat && !DynelManager.LocalPlayer.IsAttacking && !DynelManager.LocalPlayer.IsAttackPending && AutoSitSwitch == true && !DynelManager.LocalPlayer.IsMoving && !Game.IsZoning && !DynelManager.LocalPlayer.Buffs.Contains(280488))
                {
                    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Treatment) && justusedsitkit == false && (DynelManager.LocalPlayer.NanoPercent <= 65 || DynelManager.LocalPlayer.HealthPercent <= 65))
                    {
                        MovementController.Instance.SetMovement(MovementAction.SwitchToSit);
                        justusedsitkit = true;
                    }
                }

                if (DynelManager.LocalPlayer.MovementState == MovementState.Sit && justusedsitkit == true)
                {
                    MovementController.Instance.SetMovement(MovementAction.LeaveSit);
                    justusedsitkit = false;
                }

                _lastUpdateTime = Time.NormalTime;
            }
        }
    }
}
