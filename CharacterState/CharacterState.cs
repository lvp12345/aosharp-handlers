using AOSharp.Character;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic.IPCMessages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Character.State
{
    public class CharacterState : AOPluginEntry
    {
        private static Dictionary<Identity, int> RemainingNCU = new Dictionary<Identity, int>();
        private static Dictionary<Identity, CharacterWeaponType> WeaponType = new Dictionary<Identity, CharacterWeaponType>();
        private static Dictionary<Identity, SpecialAttacks> SupportedSpecialAttacks = new Dictionary<Identity, SpecialAttacks>();

        private static IPCChannel ReportingIPCChannel;

        private static double _lastUpdateTime = 0;
        public override void Run(string pluginDir)
        {
            ReportingIPCChannel = new IPCChannel(112);

            ReportingIPCChannel.RegisterCallback((int)IPCOpcode.CharacterState, OnCharacterStateMessage);
            ReportingIPCChannel.RegisterCallback((int)IPCOpcode.CharacterSpecials, OnCharacterSpecialsMessage);
            ReportingIPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);
            Game.OnUpdate += ReportCharacterState;
            new TeamCommands().RegisterCommands();
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        public static void BroadcastDisband()
        {
            ReportingIPCChannel.Broadcast(new DisbandMessage());
        }

        public static int GetRemainingNCU(Identity target)
        {
            return CharacterState.RemainingNCU.ContainsKey(target) ? CharacterState.RemainingNCU[target] : 0;
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

        private static void ReportCharacterState(object sender, float deltaTime)
        {
            if (Time.NormalTime - _lastUpdateTime > 1)
            {
                CharacterStateMessage stateMessage = CharacterStateMessage.ForLocalPlayer();
                CharacterSpecialsMessage specialsMessage = CharacterSpecialsMessage.ForLocalPlayer();

                ReportingIPCChannel.Broadcast(stateMessage);
                ReportingIPCChannel.Broadcast(specialsMessage);

                OnCharacterSpecialsMessage(0, specialsMessage);
                OnCharacterStateMessage(0, stateMessage);

                _lastUpdateTime = Time.NormalTime;
            }
        }
    }
}
