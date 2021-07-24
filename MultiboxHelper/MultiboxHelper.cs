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
using System.Xml.Linq;
using Character.State;

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

        private string[] playername = null;

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
            IPCChannel.RegisterCallback((int)IPCOpcode.UseSangRing, OnUseSangRingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.UseRimyRing, OnUseRimyRingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.UseAchromRing, OnUseAchromRingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.UseCaligRing, OnUseCaligRingMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.UseFGrid, OnUseFGridMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChatOpen, OnNpcChatOpenMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChatClose, OnNpcChatCloseMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChatAnswer, OnNpcChatAnswerMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Follow, OnFollowMessage);

            settings.AddVariable("Follow", false);
            settings.AddVariable("OSFollow", false);
            settings.AddVariable("SyncMove", false);
            settings.AddVariable("SyncUse", true);
            settings.AddVariable("SyncAttack", true);
            settings.AddVariable("SyncChat", false);
            settings.AddVariable("AutoSit", false);

            settings["Follow"] = false;
            settings["OSFollow"] = false;

            SettingsController.RegisterSettingsWindow("Multibox Helper", pluginDir + "\\UI\\MultiboxSettingWindow.xml", settings);

            Chat.RegisterCommand("mb", MbCommand);

            Chat.RegisterCommand("followplayer", FollowName);
            Chat.RegisterCommand("allfollow", Allfollow);
            Chat.RegisterCommand("leadfollow", LeadFollow);

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

            if (settings["AutoSit"].AsBool())
                CharacterState.AutoSitSwitch = true;

            if (!settings["AutoSit"].AsBool())
                CharacterState.AutoSitSwitch = false;

            if (IsActiveCharacter() && settings["Follow"].AsBool() && Time.NormalTime - _lastFollowTime > 1)
            {
                if (settings["OSFollow"].AsBool())
                {
                    settings["OSFollow"] = false;
                    settings["Follow"] = false;
                    Chat.WriteLine($"Can only have one follow active at once.");
                }

                IPCChannel.Broadcast(new FollowMessage()
                {
                    Target = DynelManager.LocalPlayer.Identity
                });
                _lastFollowTime = Time.NormalTime;
            }

            if (settings["OSFollow"].AsBool() && Time.NormalTime - _lastFollowTime > 1)
            {
                if (settings["Follow"].AsBool())
                {
                    settings["OSFollow"] = false;
                    settings["Follow"] = false;
                    Chat.WriteLine($"Can only have one follow active at once.");
                }

                if (playername == null)
                {
                    Chat.WriteLine($"Cannot find player, try setting the name using /followplayer playername");
                    settings["OSFollow"] = false;
                    return;
                }

                Dynel npc = DynelManager.AllDynels.Where(x => x.Name.Contains(playername[0])).FirstOrDefault();

                if (npc != null)
                {
                    OnSelfFollowMessage(npc);

                    IPCChannel.Broadcast(new FollowMessage()
                    {
                        Target = npc.Identity // change this to the new target with selection param
                    });
                    _lastFollowTime = Time.NormalTime;
                }
                else
                {
                    Chat.WriteLine($"Cannot find {playername[0]}, turning off follow.");
                    settings["OSFollow"] = false;
                    return;
                }
            }
        }

        private bool IsActiveCharacter()
        {
            return IsActiveWindow;
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
                if (genericCmdMsg.Action == GenericCmdAction.UseItemOnItem)
                {
                    if (Inventory.Find(genericCmdMsg.Source, out Item item))
                    {
                        if (item.Name.StartsWith("Sanguine Ring "))
                        {
                            IPCChannel.Broadcast(new UseSangRingMessage()
                            {
                                Target = genericCmdMsg.Target
                            });
                        }
                    }
                }
                if (genericCmdMsg.Action == GenericCmdAction.UseItemOnItem)
                {
                    if (Inventory.Find(genericCmdMsg.Source, out Item item))
                    {
                        if (item.Name.StartsWith("Rimy Ring for "))
                        {
                            IPCChannel.Broadcast(new UseRimyRingMessage()
                            {
                                Target = genericCmdMsg.Target
                            });
                        }
                    }
                }
                if (genericCmdMsg.Action == GenericCmdAction.UseItemOnItem)
                {
                    if (Inventory.Find(genericCmdMsg.Source, out Item item))
                    {
                        if (item.Name.StartsWith("Achromic Ring "))
                        {
                            IPCChannel.Broadcast(new UseAchromRingMessage()
                            {
                                Target = genericCmdMsg.Target
                            });
                        }
                    }
                }
                if (genericCmdMsg.Action == GenericCmdAction.UseItemOnItem)
                {
                    if (Inventory.Find(genericCmdMsg.Source, out Item item))
                    {
                        if (item.Name.StartsWith("Caliginous Ring "))
                        {
                            IPCChannel.Broadcast(new UseCaligRingMessage()
                            {
                                Target = genericCmdMsg.Target
                            });
                        }
                    }
                }
                if (genericCmdMsg.Action == GenericCmdAction.UseItemOnItem)
                {
                    if (Inventory.Find(genericCmdMsg.Source, out Item item))
                    {
                        if (item.Name.StartsWith("Data Receptacle"))
                        {
                            IPCChannel.Broadcast(new UseDataReceptacle()
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

        private void OnMoveMessage(int sender, IPCMessage msg)
        {

            //Only followers will act on commands
            if (/*!Team.IsInTeam || */IsActiveWindow)
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

            if (/*!Team.IsInTeam || */IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            TargetMessage targetMsg = (TargetMessage)msg;
            Targeting.SetTarget(targetMsg.Target);
        }

        private void OnAttackMessage(int sender, IPCMessage msg)
        {

            if (/*!Team.IsInTeam || */IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;
            AttackIPCMessage attackMsg = (AttackIPCMessage)msg;
            Dynel targetDynel = DynelManager.GetDynel(attackMsg.Target);
            DynelManager.LocalPlayer.Attack(targetDynel, true);
        }

        private void OnStopAttackMessage(int sender, IPCMessage msg)
        {

            if (/*!Team.IsInTeam || */IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            DynelManager.LocalPlayer.StopAttack();
        }

        private void OnUseMessage(int sender, IPCMessage msg)
        {

            if (/*!Team.IsInTeam || */IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            UseMessage useMsg = (UseMessage)msg;
            DynelManager.GetDynel<SimpleItem>(useMsg.Target)?.Use();
        }

        private void OnUseFGridMessage(int sender, IPCMessage msg)
        {
            if (/*!Team.IsInTeam || */IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            Item Fgrid = Inventory.Items
                .Where(item => item.Name.Contains("Data Receptacle"))
                .FirstOrDefault();

            if (Fgrid != null)
            {
                UseDataReceptacle useDataReceptacle = (UseDataReceptacle)msg;
                Fgrid.UseOn(useDataReceptacle.Target);
            }
        }

        private void OnUseNoviRingMessage(int sender, IPCMessage msg)
        {
            if (/*!Team.IsInTeam || */IsActiveWindow)
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

        private void OnUseSangRingMessage(int sender, IPCMessage msg)
        {
            if (/*!Team.IsInTeam || */IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            Item SangRing = Inventory.Items
                .Where(item => item.Name.Contains("Sanguine Ring "))
                .FirstOrDefault();

            if (SangRing != null)
            {
                UseSangRingMessage useSangRingMessage = (UseSangRingMessage)msg;
                SangRing.UseOn(useSangRingMessage.Target);
            }
        }

        private void OnUseRimyRingMessage(int sender, IPCMessage msg)
        {
            if (/*!Team.IsInTeam || */IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            Item RimyRing = Inventory.Items
                .Where(item => item.Name.Contains("Rimy Ring "))
                .FirstOrDefault();

            if (RimyRing != null)
            {
                UseRimyRingMessage useRimyRingMessage = (UseRimyRingMessage)msg;
                RimyRing.UseOn(useRimyRingMessage.Target);
            }
        }

        private void OnUseAchromRingMessage(int sender, IPCMessage msg)
        {
            if (/*!Team.IsInTeam || */IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            Item AchromRing = Inventory.Items
                .Where(item => item.Name.Contains("Achromic Ring "))
                .FirstOrDefault();

            if (AchromRing != null)
            {
                UseAchromRingMessage useAchromRingMessage = (UseAchromRingMessage)msg;
                AchromRing.UseOn(useAchromRingMessage.Target);
            }
        }

        private void OnUseCaligRingMessage(int sender, IPCMessage msg)
        {
            if (/*!Team.IsInTeam || */IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            Item CaligRing = Inventory.Items
                .Where(item => item.Name.Contains("Caliginous Ring "))
                .FirstOrDefault();

            if (CaligRing != null)
            {
                UseCaligRingMessage useCaligRingMessage = (UseCaligRingMessage)msg;
                CaligRing.UseOn(useCaligRingMessage.Target);
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

        private void FollowName(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0 && playername != null)
            {
                Chat.WriteLine($"Follow player name is - {playername[0]}");
                return;
            }

            if (param.Length == 0)
            {
                Chat.WriteLine($"Wrong syntax, /followplayer playername");
                return;
            }

            if (param[0] == "clear")
            {
                playername = null;
                Chat.WriteLine($"Follow player name has been cleared.");
                return;
            }

            if (param.Length >= 1)
            {
                playername = param;
                Chat.WriteLine($"Follow player name set to - {playername[0]}");
            }
        }

        private void LeadFollow(string command, string[] param, ChatWindow chatWindow)
        {
            //if (settings["OSFollow"].AsBool())
            //{
            //    settings["OSFollow"] = false;
            //    Chat.WriteLine($"Named Following is on, turning off.");
            //}

            if (param.Length == 0 && settings["Follow"].AsBool())
            {
                settings["Follow"] = false;
                Chat.WriteLine($"Stopped following.");
                return;
            }
            if (param.Length == 0 && !settings["Follow"].AsBool())
            {
                settings["Follow"] = true;
                Chat.WriteLine($"Following active window.");
                return;
            }
        }

        private void Allfollow(string command, string[] param, ChatWindow chatWindow)
        {
            //if (settings["Follow"].AsBool())
            //{
            //    settings["Follow"] = false;
            //    Chat.WriteLine($"Lead Following is on, turning off.");
            //}

            if (param.Length == 0 && settings["OSFollow"].AsBool())
            {
                Chat.WriteLine($"Stopped following.");
                settings["OSFollow"] = false;
                return;
            }

            if (param.Length == 0 && !settings["OSFollow"].AsBool() && playername == null)
            {
                Chat.WriteLine($"Wrong syntax, /allfollow playername");
                settings["OSFollow"] = false;
                return;
            }

            if (param.Length == 0 && !settings["OSFollow"].AsBool() && playername != null)
            {
                settings["OSFollow"] = true;
                Chat.WriteLine($"Following {playername[0]}.");
            }

            if (playername == null && settings["OSFollow"].AsBool())
            {
                Chat.WriteLine($"Cannot find player.");
                settings["OSFollow"] = false;
            }

            if (param.Length >= 1 && !settings["OSFollow"].AsBool())
            {
                playername = param;
                settings["OSFollow"] = true;
                Chat.WriteLine($"Following {playername[0]}.");

            }
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

        private void OnSelfFollowMessage(Dynel dynel)
        {
            FollowTargetMessage n3Msg = new FollowTargetMessage()
            {
                Target = dynel.Identity,
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
