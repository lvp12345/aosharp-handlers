using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using AOSharp.Core.UI.Options;
using AOSharp.Common;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using MultiboxHelper.IPCMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace MultiboxHelper
{
    public class MultiboxHelper : IAOPluginEntry
    {
        private Menu _menu;
        private IPCChannel IPCChannel;

        public void Run(string pluginDir)
        {
            IPCChannel = new IPCChannel(111);
            IPCChannel.RegisterCallback((int)IPCOpcode.Move, OnMoveMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Target, OnTargetMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Attack, OnAttackMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.StopAttack, OnStopAttackMessage);

            _menu = new Menu("MultiboxHelper", "MultiboxHelper");
            _menu.AddItem(new MenuBool("SyncMove", "Sync Movement", true));
            _menu.AddItem(new MenuBool("SyncAttack", "Sync Attacks", true));
            OptionPanel.AddMenu(_menu);

            Network.N3MessageSent += Network_N3MessageSent;
        }

        private void Network_N3MessageSent(object s, N3Message n3Msg)
        {
            //Only the leader will issue commands
            if (!Team.IsInTeam || !Team.IsLeader)
                return;

            if (n3Msg.Identity != DynelManager.LocalPlayer.Identity)
                return;

            if(n3Msg.N3MessageType == N3MessageType.CharDCMove)
            {
                if (!_menu.GetBool("SyncMove"))
                    return;

                CharDCMoveMessage charDCMoveMsg = (CharDCMoveMessage)n3Msg;
                IPCChannel.Broadcast(new MoveMessage()
                {
                    MoveType = charDCMoveMsg.MoveType,
                    Position = charDCMoveMsg.Position,
                    Rotation = charDCMoveMsg.Heading
                });

            }
            else if (n3Msg.N3MessageType == N3MessageType.CharacterAction)
            {
                if (!_menu.GetBool("SyncMove"))
                    return;

                CharacterActionMessage charActionMsg = (CharacterActionMessage)n3Msg;

                if (charActionMsg.Action != CharacterActionType.StandUp)
                    return;

                IPCChannel.Broadcast(new MoveMessage()
                {
                    MoveType = MovementAction.LeaveSit,
                    Position = DynelManager.LocalPlayer.Position,
                    Rotation = DynelManager.LocalPlayer.Rotation
                });
            }
            else if (n3Msg.N3MessageType == N3MessageType.LookAt)
            {
                LookAtMessage lookAtMsg = (LookAtMessage)n3Msg;
                IPCChannel.Broadcast(new TargetMessage()
                {
                    Target = lookAtMsg.Target
                });
            }
            else if (n3Msg.N3MessageType == N3MessageType.Attack)
            {
                if (!_menu.GetBool("SyncAttack"))
                    return;

                AttackMessage attackMsg = (AttackMessage)n3Msg;
                IPCChannel.Broadcast(new AttackIPCMessage()
                {
                    Target = attackMsg.Target
                });
            }
            else if (n3Msg.N3MessageType == N3MessageType.StopFight)
            {
                if (!_menu.GetBool("SyncAttack"))
                    return;

                StopFightMessage lookAtMsg = (StopFightMessage)n3Msg;
                IPCChannel.Broadcast(new StopAttackIPCMessage());
            }
        }

        private void OnMoveMessage(int sender, IPCMessage msg)
        {
            //Only followers will act on commands
            if (!Team.IsInTeam || Team.IsLeader)
                return; 

            MoveMessage moveMsg = (MoveMessage)msg;

            DynelManager.LocalPlayer.Position = moveMsg.Position;
            DynelManager.LocalPlayer.Rotation = moveMsg.Rotation;
            MovementController.SetMovement(moveMsg.MoveType);
        }

        private void OnTargetMessage(int sender, IPCMessage msg)
        {
            if (!Team.IsInTeam || Team.IsLeader)
                return;

            TargetMessage targetMsg = (TargetMessage)msg;
            Targeting.SetTarget(targetMsg.Target);
        }

        private void OnAttackMessage(int sender, IPCMessage msg)
        {
            if (!Team.IsInTeam || Team.IsLeader)
                return;

            AttackIPCMessage attackMsg = (AttackIPCMessage)msg;
            DynelManager.LocalPlayer.Attack(attackMsg.Target);
        }

        private void OnStopAttackMessage(int sender, IPCMessage msg)
        {
            if (!Team.IsInTeam || Team.IsLeader)
                return;

            DynelManager.LocalPlayer.StopAttack();
        }
    }
}
