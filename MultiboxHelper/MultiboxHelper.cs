using System;
using System.Linq;
using System.Diagnostics;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using MultiboxHelper.IPCMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.GameData;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AOSharp.Core.Inventory;
using AOSharp.Character;
using CombatHandler.Generic;

namespace MultiboxHelper
{
    public class MultiboxHelper : AOPluginEntry
    {
        public static string PluginDir;

        private IPCChannel IPCChannel;
        private StatusWindow _statusWindow;
        private double _lastUpdateTime = 0;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private Settings settings = new Settings("MultiboxHelper");

        private double _lastFollowTime = Time.NormalTime;

        private bool IsActiveWindow => GetForegroundWindow() == Process.GetCurrentProcess().MainWindowHandle;

        public override void Run(string pluginDir)
        {
            PluginDir = pluginDir;
            _statusWindow = new StatusWindow();

            IPCChannel = new IPCChannel(111);
            IPCChannel.RegisterCallback((int)IPCOpcode.Move, OnMoveMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Target, OnTargetMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Attack, OnAttackMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.StopAttack, OnStopAttackMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Use, OnUseMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.CharStatus, OnCharStatusMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.CharLeft, OnCharLeftMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.UseNoviRing, OnUseNoviRingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChatOpen, OnNpcChatOpenMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChatClose, OnNpcChatCloseMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChatAnswer, OnNpcChatAnswerMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Follow, OnFollowMessage);

            settings.AddVariable("UseFollow", false);
            settings.AddVariable("SyncMove", false);
            settings.AddVariable("SyncUse", true);
            settings.AddVariable("SyncAttack", true);
            settings.AddVariable("SyncChat", false);
            settings.AddVariable("Disabled", false);
            SettingsController.RegisterSettingsWindow("Multibox Helper", pluginDir + "\\UI\\MultiboxSettingWindow.xml", settings);

            Chat.RegisterCommand("mb", MbCommand);

            Game.OnUpdate += OnUpdate;
            Network.N3MessageSent += Network_N3MessageSent;
            Chat.WriteLine("Multibox Helper Loaded!");
        }

        private void OnUpdate(object s, float deltaTime)
        {
            if (Time.NormalTime > _lastUpdateTime + 0.5f)
            {
                IPCChannel.Broadcast(new CharStatusMessage
                {
                    Name = DynelManager.LocalPlayer.Name,
                    Health = DynelManager.LocalPlayer.Health,
                    MaxHealth = DynelManager.LocalPlayer.MaxHealth,
                    Nano = DynelManager.LocalPlayer.Nano,
                    MaxNano = DynelManager.LocalPlayer.MaxNano,
                });

                _lastUpdateTime = Time.NormalTime;
            }

            if(IsActiveCharacter() && settings["UseFollow"].AsBool() && Time.NormalTime - _lastFollowTime > 3)
            {
                IPCChannel.Broadcast(new FollowMessage()
                {
                    Target = DynelManager.LocalPlayer.Identity
                });
                _lastFollowTime = Time.NormalTime;
            }
        }

        private bool IsActiveCharacter()
        {
            return Team.IsInTeam && IsActiveWindow;
        }

        private void Network_N3MessageSent(object s, N3Message n3Msg)
        {
            //Only the active window will issue commands
            if (!IsActiveCharacter())
                return;

            if (n3Msg.Identity != DynelManager.LocalPlayer.Identity)
                return;

            if (n3Msg.N3MessageType == N3MessageType.CharDCMove)
            {
                if (!settings["SyncMove"].AsBool())
                    return;

                CharDCMoveMessage charDCMoveMsg = (CharDCMoveMessage)n3Msg;
                IPCChannel.Broadcast(new MoveMessage()
                {
                    MoveType = charDCMoveMsg.MoveType,
                    PlayfieldId = Playfield.Identity.Instance,
                    Position = charDCMoveMsg.Position,
                    Rotation = charDCMoveMsg.Heading
                });

            }
            else if (n3Msg.N3MessageType == N3MessageType.CharacterAction)
            {
                if (!settings["SyncMove"].AsBool())
                    return;

                CharacterActionMessage charActionMsg = (CharacterActionMessage)n3Msg;

                if (charActionMsg.Action != CharacterActionType.StandUp)
                    return;

                IPCChannel.Broadcast(new MoveMessage()
                {
                    MoveType = MovementAction.LeaveSit,
                    PlayfieldId = Playfield.Identity.Instance,
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
                if (!settings["SyncAttack"].AsBool())
                    return;

                AttackMessage attackMsg = (AttackMessage)n3Msg;
                IPCChannel.Broadcast(new AttackIPCMessage()
                {
                    Target = attackMsg.Target
                });
            }
            else if (n3Msg.N3MessageType == N3MessageType.StopFight)
            {
                if (!settings["SyncAttack"].AsBool())
                    return;

                StopFightMessage lookAtMsg = (StopFightMessage)n3Msg;
                IPCChannel.Broadcast(new StopAttackIPCMessage());
            }
            else if (n3Msg.N3MessageType == N3MessageType.GenericCmd)
            {
                if (!settings["SyncUse"].AsBool())
                    return;

                GenericCmdMessage genericCmdMsg = (GenericCmdMessage)n3Msg;

                if (genericCmdMsg.Action == GenericCmdAction.UseItemOnItem)
                {
                    if (Inventory.Find(genericCmdMsg.Source, out Item item))
                    {
                        if (item.Name.StartsWith("Pure Novictum"))
                        {
                            IPCChannel.Broadcast(new UseNoviRingMessage()
                            {
                                Target = genericCmdMsg.Target
                            });
                        }
                    }
                }
                if (genericCmdMsg.Action == GenericCmdAction.Use && genericCmdMsg.Target.Type == IdentityType.Terminal)
                {
                    IPCChannel.Broadcast(new UseMessage()
                    {
                        Target = genericCmdMsg.Target
                    });
                }
            }
            else if (n3Msg.N3MessageType == N3MessageType.KnubotOpenChatWindow)
            {
                if(!settings["SyncChat"].AsBool())
                {
                    return;
                }
                KnuBotOpenChatWindowMessage n3OpenChatMessage = (KnuBotOpenChatWindowMessage)n3Msg;
                IPCChannel.Broadcast(new NpcChatOpenMessage()
                {
                    Target = n3OpenChatMessage.Target
                });
            }
            else if (n3Msg.N3MessageType == N3MessageType.KnubotCloseChatWindow)
            {
                if (!settings["SyncChat"].AsBool())
                {
                    return;
                }
                KnuBotCloseChatWindowMessage n3CloseChatMessage = (KnuBotCloseChatWindowMessage)n3Msg;
                IPCChannel.Broadcast(new NpcChatCloseMessage()
                {
                    Target = n3CloseChatMessage.Target
                });
            }
            else if (n3Msg.N3MessageType == N3MessageType.KnubotAnswer)
            {
                if (!settings["SyncChat"].AsBool())
                {
                    return;
                }
                KnuBotAnswerMessage n3AnswerMsg = (KnuBotAnswerMessage)n3Msg;
                IPCChannel.Broadcast(new NpcChatAnswerMessage()
                {
                    Target = n3AnswerMsg.Target,
                    Answer = n3AnswerMsg.Answer
                });
            }
        }

        private bool IsSyncDisabled()
        {
            return settings["Disabled"].AsBool();
        }

        private void OnMoveMessage(int sender, IPCMessage msg)
        {
            if (IsSyncDisabled())
                return;

            //Only followers will act on commands
            if (!Team.IsInTeam || IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            MoveMessage moveMsg = (MoveMessage)msg;

            if (Playfield.Identity.Instance != moveMsg.PlayfieldId)
                return;

            DynelManager.LocalPlayer.Position = moveMsg.Position;
            DynelManager.LocalPlayer.Rotation = moveMsg.Rotation;
            MovementController.Instance.SetMovement(moveMsg.MoveType);
        }

        private void OnTargetMessage(int sender, IPCMessage msg)
        {
            if (IsSyncDisabled())
                return;

            if (!Team.IsInTeam || IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            TargetMessage targetMsg = (TargetMessage)msg;
            Targeting.SetTarget(targetMsg.Target);
        }

        private void OnAttackMessage(int sender, IPCMessage msg)
        {
            if (IsSyncDisabled())
                return;

            if (!Team.IsInTeam || IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;
            AttackIPCMessage attackMsg = (AttackIPCMessage)msg;
            Dynel targetDynel = DynelManager.GetDynel(attackMsg.Target);
            DynelManager.LocalPlayer.Attack(targetDynel, true);
        }

        private void OnStopAttackMessage(int sender, IPCMessage msg)
        {
            if (IsSyncDisabled())

                return;
            if (!Team.IsInTeam || IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            DynelManager.LocalPlayer.StopAttack();
        }

        private void OnUseMessage(int sender, IPCMessage msg)
        {
            if (IsSyncDisabled())
                return;

            if (!Team.IsInTeam || IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            UseMessage useMsg = (UseMessage)msg;
            DynelManager.GetDynel<SimpleItem>(useMsg.Target)?.Use();
        }

        private void OnUseNoviRingMessage(int sender, IPCMessage msg)
        {
            if (!Team.IsInTeam || IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            Item noviRing = Inventory.Items
                .Where(item => item.Name.Contains("Pure Novictum"))
                .FirstOrDefault();

            if (noviRing != null)
            {
                UseNoviRingMessage useNoviRingMessage = (UseNoviRingMessage)msg;
                noviRing.UseOn(useNoviRingMessage.Target);
            }
        }

        private void OnNpcChatOpenMessage(int sender, IPCMessage msg)
        {
            NpcChatOpenMessage message = (NpcChatOpenMessage)msg;
            NpcDialog.Open(message.Target);
        }

        private void OnNpcChatCloseMessage(int sender, IPCMessage msg)
        {
            NpcChatCloseMessage message = (NpcChatCloseMessage)msg;
            KnuBotCloseChatWindowMessage closeChatMessage = new KnuBotCloseChatWindowMessage()
            {
                Unknown1 = 2,
                Unknown2 = 0,
                Unknown3 = 0,
                Target = message.Target
            };
            Network.Send(closeChatMessage);
        }

        private void OnNpcChatAnswerMessage(int sender, IPCMessage msg)
        {
            NpcChatAnswerMessage message = (NpcChatAnswerMessage)msg;
            NpcDialog.SelectAnswer(message.Target, message.Answer);
        }

        private void OnCharStatusMessage(int sender, IPCMessage msg)
        {
            CharStatusMessage statusMsg = (CharStatusMessage)msg;

            _statusWindow.SetCharStatus(sender, new CharacterStatus
            {
                Name = statusMsg.Name,
                Health = statusMsg.Health,
                MaxHealth = statusMsg.MaxHealth,
                Nano = statusMsg.Nano,
                MaxNano = statusMsg.MaxNano
            });
        }

        private void OnCharLeftMessage(int sender, IPCMessage msg)
        {
            _statusWindow.RemoveChar(sender);
        }

        public override void Teardown()
        {
            IPCChannel.Broadcast(new CharLeftMessage());
        }

        private void PrintCommandUsage(ChatWindow chatWindow)
        {
            string help = "Usage:\nStatus - toggles status window";

            chatWindow.WriteLine(help, ChatColor.LightBlue);
        }

        private void MbCommand(string command, string[] param, ChatWindow chatWindow)
        {
            try
            {
                if (param.Length < 1)
                {
                    PrintCommandUsage(chatWindow);
                    return;
                }

                switch (param[0].ToLower())
                {
                    case "status":
                        _statusWindow.Open();
                        break;
                    default:
                        PrintCommandUsage(chatWindow);
                        break;
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void OnFollowMessage(int sender, IPCMessage msg)
        {
            FollowMessage followMessage = (FollowMessage)msg;
            FollowTargetMessage n3Msg = new FollowTargetMessage()
            {
                Target = followMessage.Target,
                Unknown1 = 0,
                Unknown2 = 0,
                Unknown3 = 0,
                Unknown4 = 0,
                Unknown5 = 0,
                Unknown6 = 0,
                Unknown7 = 0
            };
            Network.Send(n3Msg);
        }
    }
}
