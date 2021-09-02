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
using AOSharp.Common.GameData.UI;

namespace MultiboxHelper
{
    public class MultiboxHelper : AOPluginEntry
    {
        public static string PluginDir;

        private static IPCChannel IPCChannel;

        private double _ncuUpdateTime = 0;

        private static Identity useDynel;
        private static Identity useOnDynel;
        private static Identity useItem;

        public static string playersname = String.Empty;
        public static string identitiesname = String.Empty;
        public static string assistersname = String.Empty;


        private static double posUpdateTimer;
        private static double sitUpdateTimer;


        private static Dictionary<Identity, int> RemainingNCU = new Dictionary<Identity, int>();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private static Settings settings = new Settings("MultiboxHelper");

        //private string[] playername = null;
        //private string[] identityname = null;
        //private string[] assistname = null;

        private double _lastFollowTime = Time.NormalTime;

        List<Vector3> birdy = new List<Vector3>
        {
            new Vector3(75.5, 29.0, 58.6),
            new Vector3(37.3, 29.0, 59.0),
            new Vector3(35.6, 29.3, 30.5),
            new Vector3(37.3, 29.0, 59.0),
            new Vector3(75.5, 29.0, 58.6),
            new Vector3(76.1, 29.0, 28.3)
        };

        List<Vector3> horsey = new List<Vector3>
        {
            new Vector3(128.4, 29.0, 59.6),
            new Vector3(161.9, 29.0, 59.5),
            new Vector3(163.9, 29.4, 29.6),
            new Vector3(161.9, 29.0, 59.5),
            new Vector3(128.4, 29.0, 59.6),
            new Vector3(76.1, 29.0, 28.3)
        };

        public byte _channelId;

        private static bool justusedsitkit = false;

        private bool IsActiveWindow => GetForegroundWindow() == Process.GetCurrentProcess().MainWindowHandle;

        public override void Run(string pluginDir)
        {
            PluginDir = pluginDir;

            settings.AddVariable("ChannelID", 10);
            settings.AddVariable("Follow", false);
            settings.AddVariable("OSFollow", false);
            settings.AddVariable("SyncMove", false);
            settings.AddVariable("SyncUse", true);
            settings.AddVariable("SyncAttack", true);
            settings.AddVariable("SyncChat", false);
            settings.AddVariable("AutoSit", true);
            settings.AddVariable("SyncTrade", false);
            settings.AddVariable("NavFollow", false);
            settings.AddVariable("AssistPlayer", false);

            settings["Follow"] = false;
            settings["OSFollow"] = false;
            settings["NavFollow"] = false;
            settings["SyncAttack"] = true;

            _channelId = Convert.ToByte(settings["ChannelID"].AsInt32());

            IPCChannel = new IPCChannel(_channelId);

            IPCChannel.RegisterCallback((int)IPCOpcode.Move, OnMoveMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Target, OnTargetMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Attack, OnAttackMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.StopAttack, OnStopAttackMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Trade, TradeMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Use, OnUseMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.UseItem, OnUseItemMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChatOpen, OnNpcChatOpenMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChatClose, OnNpcChatCloseMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChatAnswer, OnNpcChatAnswerMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Follow, OnFollowMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NavFollow, OnNavFollowMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);
            IPCChannel.RegisterCallback((int)IPCOpcode.Assist, OnAssistMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Jump, OnJumpMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.ChannelAll, OnChannelAll);


            SettingsController.RegisterSettingsWindow("Multibox Helper", pluginDir + "\\UI\\MultiboxSettingWindow.xml", settings);

            Chat.RegisterCommand("mbhelp", HelpCommand);

            Chat.RegisterCommand("mbchannel", ChannelCommand);
            Chat.RegisterCommand("mbchannelall", ChannelAllCommand);

            Chat.RegisterCommand("allfollow", Allfollow);
            Chat.RegisterCommand("leadfollow", LeadFollow);
            Chat.RegisterCommand("navfollow", Navfollow);
            Chat.RegisterCommand("assistplayer", AssistPlayer);
            Chat.RegisterCommand("sync", SyncSwitch);
            Chat.RegisterCommand("syncuse", SyncUseSwitch);
            Chat.RegisterCommand("syncchat", SyncChatSwitch);
            Chat.RegisterCommand("autosit", AutoSitSwitch);
            Chat.RegisterCommand("synctrade", SyncTradeSwitch);

            Chat.RegisterCommand("reform", ReformCommand);
            Chat.RegisterCommand("form", FormCommand);
            Chat.RegisterCommand("disband", DisbandCommand);
            Chat.RegisterCommand("convert", RaidCommand);


            Game.OnUpdate += OnUpdate;
            Network.N3MessageSent += Network_N3MessageSent;
            Team.TeamRequest = Team_TeamRequest;

            Chat.WriteLine("Multibox Helper Loaded!");
            Chat.WriteLine("/mbhelp for a list of shortcut commands.");
            Chat.WriteLine($"IPC Channel for MultiboxHelper - {_channelId}");
        }

        public static int GetRemainingNCU(Identity target)
        {
            return RemainingNCU.ContainsKey(target) ? RemainingNCU[target] : 0;
        }

        public static Identity[] GetRegisteredCharacters()
        {
            return RemainingNCU.Keys.ToArray();
        }

        public static bool IsCharacterRegistered(Identity target)
        {
            return RemainingNCU.ContainsKey(target);
        }

        private void OnUpdate(object s, float deltaTime)
        {
            if (Time.NormalTime > _ncuUpdateTime + 0.5f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }

            if (!MovementController.Instance.IsNavigating && DynelManager.LocalPlayer.Buffs.Contains(281109))
            {
                MovementController.Instance.SetPath(birdy);
            }

            if (!MovementController.Instance.IsNavigating && DynelManager.LocalPlayer.Buffs.Contains(281108))
            {
                MovementController.Instance.SetPath(horsey);
            }

            if (Time.NormalTime > sitUpdateTimer + 0.1)
            {
                ListenerSit();

                sitUpdateTimer = Time.NormalTime;
            }

            if (Time.NormalTime > posUpdateTimer + 0.1)
            {
                if (!IsActiveWindow)
                {
                    ListenerUseSync();
                }
                posUpdateTimer = Time.NormalTime;
            }

            if (IsActiveCharacter() && settings["Follow"].AsBool() && Time.NormalTime - _lastFollowTime > 1)
            {
                if (settings["OSFollow"].AsBool() || settings["NavFollow"].AsBool())
                {
                    settings["OSFollow"] = false;
                    settings["Follow"] = false;
                    settings["NavFollow"] = false;
                    Chat.WriteLine($"Can only have one follow active at once.");
                }

                IPCChannel.Broadcast(new FollowMessage()
                {
                    Target = DynelManager.LocalPlayer.Identity
                });
                _lastFollowTime = Time.NormalTime;
            }

            //if (!settings["OSFollow"].AsBool())
            //{
            //    if (playersname != String.Empty)
            //    {
            //        playersname = String.Empty;
            //        return;
            //    }
            //}

            //if (!settings["NavFollow"].AsBool())
            //{
            //    if (identitiesname != String.Empty)
            //    {
            //        identitiesname = String.Empty;
            //        return;
            //    }
            //}

            //if (!settings["AssistPlayer"].AsBool())
            //{
            //    if (assistersname != String.Empty)
            //    {
            //        assistersname = String.Empty;
            //        return;
            //    }
            //}

            if (settings["AssistPlayer"].AsBool() && Time.NormalTime - _lastFollowTime > 1)
            {
                if (settings["SyncAttack"].AsBool())
                {
                    settings["SyncAttack"] = false;
                    settings["AssistPlayer"] = false;
                    Chat.WriteLine($"Can only have one form of sync attack active at once.");
                }

                if (assistersname == String.Empty)
                {
                    Window textboxwindow = SettingsController.settingsWindow;

                    textboxwindow.FindView("AssistNamedCharacter", out TextInputView textinput);

                    if (textinput.Text == String.Empty)
                    {
                        Chat.WriteLine("You must enter a characters name.");
                        settings["AssistPlayer"] = false;
                        return;
                    }

                    if (textinput.Text != String.Empty)
                    {
                        assistersname = textinput.Text;
                        return;
                    }
                }

                SimpleChar identity = DynelManager.Characters
                    .Where(c => assistersname != String.Empty)
                    .Where(c => c.IsAlive)
                    .Where(c => c.IsInLineOfSight)
                    .Where(c => c.Name.Contains(assistersname))
                    .FirstOrDefault();

                if (identity != null && identity.FightingTarget == null &&
                    DynelManager.LocalPlayer.FightingTarget != null)
                {
                    DynelManager.LocalPlayer.StopAttack();

                    IPCChannel.Broadcast(new StopAttackIPCMessage());
                    _lastFollowTime = Time.NormalTime;
                }

                if (identity != null && identity.FightingTarget != null && 
                    (DynelManager.LocalPlayer.FightingTarget == null || 
                    (DynelManager.LocalPlayer.FightingTarget != null && DynelManager.LocalPlayer.FightingTarget.Identity != identity.FightingTarget.Identity)))
                {
                    DynelManager.LocalPlayer.Attack(identity.FightingTarget);

                    IPCChannel.Broadcast(new AssistMessage()
                    {
                        Target = identity.Identity
                    });
                    _lastFollowTime = Time.NormalTime;
                }
                else if (identity == null)
                {
                    Chat.WriteLine($"Cannot find {assistersname}. Make sure to type captial first letter.");
                    settings["AssistPlayer"] = false;
                    return;
                }
            }

            if (settings["NavFollow"].AsBool() && Time.NormalTime - _lastFollowTime > 1)
            {
                if (settings["Follow"].AsBool())
                {
                    settings["OSFollow"] = false;
                    settings["Follow"] = false;
                    settings["NavFollow"] = false;
                    Chat.WriteLine($"Can only have one follow active at once.");
                }

                if (identitiesname == String.Empty)
                {
                    Window textboxwindow = SettingsController.settingsWindow;

                    textboxwindow.FindView("FollowNamedIdentity", out TextInputView textinput);

                    if (textinput.Text == String.Empty)
                    {
                        Chat.WriteLine("You must enter a characters name.");
                        settings["NavFollow"] = false;
                        return;
                    }

                    if (textinput.Text != String.Empty)
                    {
                        identitiesname = textinput.Text;
                        return;
                    }
                }

                Dynel identity = DynelManager.AllDynels.Where(x => x.Name.Contains(identitiesname)).FirstOrDefault();

                if (identity != null)
                {
                    if (DynelManager.LocalPlayer.DistanceFrom(identity) <= 6f)
                        MovementController.Instance.Halt();

                    if (DynelManager.LocalPlayer.DistanceFrom(identity) > 6f)
                        MovementController.Instance.SetDestination(identity.Position);

                    IPCChannel.Broadcast(new NavFollowMessage()
                    {
                          Target = identity.Identity
                    });
                    _lastFollowTime = Time.NormalTime;
                }
                else
                {
                    Chat.WriteLine($"Cannot find {identitiesname}. Make sure to type captial first letter.");
                    settings["NavFollow"] = false;
                    return;
                }
            }

            if (settings["OSFollow"].AsBool() && Time.NormalTime - _lastFollowTime > 1)
            {
                if (settings["Follow"].AsBool())
                {
                    settings["OSFollow"] = false;
                    settings["Follow"] = false;
                    Chat.WriteLine($"Can only have one follow active at once.");
                }

                if (playersname == String.Empty)
                {
                    Window textboxwindow = SettingsController.settingsWindow;

                    textboxwindow.FindView("FollowNamedCharacter", out TextInputView textinput);

                    if (textinput.Text == String.Empty)
                    {
                        Chat.WriteLine("You must enter a characters name.");
                        settings["OSFollow"] = false;
                        return;
                    }

                    if (textinput.Text != String.Empty)
                    {
                        playersname = textinput.Text;
                        return;
                    }
                }

                if (playersname != String.Empty)
                {
                    Dynel npc = DynelManager.AllDynels.Where(x => x.Name.Contains(playersname)).FirstOrDefault();

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
                        Chat.WriteLine($"Cannot find {playersname}. Make sure to type captial first letter.");
                        settings["OSFollow"] = false;
                        return;
                    }
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
                CharDCMoveMessage charDCMoveMsg = (CharDCMoveMessage)n3Msg;

                if (charDCMoveMsg.MoveType == MovementAction.JumpStart && !settings["SyncMove"].AsBool())
                {
                    IPCChannel.Broadcast(new JumpMessage()
                    {
                        MoveType = charDCMoveMsg.MoveType,
                        PlayfieldId = Playfield.Identity.Instance,
                    });
                }
                else
                {
                    if (!settings["SyncMove"].AsBool())
                        return;

                    IPCChannel.Broadcast(new MoveMessage()
                    {
                        MoveType = charDCMoveMsg.MoveType,
                        PlayfieldId = Playfield.Identity.Instance,
                        Position = charDCMoveMsg.Position,
                        Rotation = charDCMoveMsg.Heading
                    });
                }
            }
            else if (n3Msg.N3MessageType == N3MessageType.Trade)
            {
                if (!settings["SyncTrade"].AsBool())
                    return;

                TradeMessage charTradeIpcMsg = (TradeMessage)n3Msg;

                if (charTradeIpcMsg.Action == TradeAction.Confirm)
                {
                    IPCChannel.Broadcast(new TradeHandleMessage()
                    {
                        Unknown1 = charTradeIpcMsg.Unknown1,
                        Action = charTradeIpcMsg.Action,
                        Target = charTradeIpcMsg.Target,
                        Container = charTradeIpcMsg.Container,
                    });
                }

                if (charTradeIpcMsg.Action == TradeAction.Accept)
                {
                    IPCChannel.Broadcast(new TradeHandleMessage()
                    {
                        Unknown1 = charTradeIpcMsg.Unknown1,
                        Action = charTradeIpcMsg.Action,
                        Target = charTradeIpcMsg.Target,
                        Container = charTradeIpcMsg.Container,
                    });
                }
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
                GenericCmdMessage genericCmdMsg = (GenericCmdMessage)n3Msg;

                Dynel target = DynelManager.AllDynels.FirstOrDefault(x => x.Identity == genericCmdMsg.Target);

                if (genericCmdMsg.Action == GenericCmdAction.Use && genericCmdMsg.Target.Type == IdentityType.Terminal)
                {
                    IPCChannel.Broadcast(new UseMessage()
                    {
                        Target = genericCmdMsg.Target,
                        PfId = Playfield.ModelIdentity.Instance
                    });
                }
                else if (genericCmdMsg.Action == GenericCmdAction.Use && settings["SyncUse"].AsBool())
                {
                    Inventory.Find(genericCmdMsg.Target, out Item item);

                    if (!IsBackpack(item))
                    {
                        IPCChannel.Broadcast(new UsableMessage()
                        {
                            ItemLowId = item.LowId,
                            ItemHighId = item.HighId,
                        });
                    }
                }
                else if (genericCmdMsg.Action == GenericCmdAction.UseItemOnItem)
                {
                    Inventory.Find(genericCmdMsg.Source, out Item item);

                    IPCChannel.Broadcast(new UsableMessage()
                    {
                        ItemLowId = item.LowId,
                        ItemHighId = item.HighId,
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

        private void OnJumpMessage(int sender, IPCMessage msg)
        {

            //Only followers will act on commands
            if (IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            JumpMessage jumpMsg = (JumpMessage)msg;

            if (Playfield.Identity.Instance != jumpMsg.PlayfieldId)
                return;

            MovementController.Instance.SetMovement(jumpMsg.MoveType);
        }

        private void OnMoveMessage(int sender, IPCMessage msg)
        {

            //Only followers will act on commands
            if (IsActiveWindow)
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

            if (IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            TargetMessage targetMsg = (TargetMessage)msg;
            Targeting.SetTarget(targetMsg.Target);
        }

        private void OnAttackMessage(int sender, IPCMessage msg)
        {

            if (IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;
            AttackIPCMessage attackMsg = (AttackIPCMessage)msg;
            Dynel targetDynel = DynelManager.GetDynel(attackMsg.Target);
            DynelManager.LocalPlayer.Attack(targetDynel, true);
        }

        private void TradeMessage(int sender, IPCMessage msg)
        {
            if (Game.IsZoning)
                return;

            TradeHandleMessage charTradeIpcMsg = (TradeHandleMessage)msg;
            TradeMessage charTradeMsg = new TradeMessage()
            {
                Unknown1 = charTradeIpcMsg.Unknown1,
                Action = charTradeIpcMsg.Action,
                Target = charTradeIpcMsg.Target,
                Container = charTradeIpcMsg.Container,
            };
            Network.Send(charTradeMsg);
        }

        private void OnStopAttackMessage(int sender, IPCMessage msg)
        {

            if (IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            DynelManager.LocalPlayer.StopAttack();
        }

        private void ListenerSit()
        {
            Spell spell = Spell.List.FirstOrDefault();

            if (spell != null && spell.IsReady && settings["AutoSit"].AsBool())
            {
                if (DynelManager.LocalPlayer.IsAlive && !IsFightingAny() && DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0 && !Team.IsInCombat && DynelManager.LocalPlayer.FightingTarget == null && !DynelManager.LocalPlayer.IsMoving && !Game.IsZoning && !DynelManager.LocalPlayer.Buffs.Contains(280488))
                {
                    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Treatment) && justusedsitkit == false && (DynelManager.LocalPlayer.NanoPercent <= 65 || DynelManager.LocalPlayer.HealthPercent <= 65))
                    {
                        MovementController.Instance.SetMovement(MovementAction.SwitchToSit);
                        justusedsitkit = true;
                    }
                }

                if (DynelManager.LocalPlayer.IsAlive && DynelManager.LocalPlayer.MovementState == MovementState.Sit && justusedsitkit == true && DynelManager.LocalPlayer.NanoPercent > 65 && DynelManager.LocalPlayer.HealthPercent > 65)
                {
                    MovementController.Instance.SetMovement(MovementAction.LeaveSit);
                    justusedsitkit = false;
                }
            }
        }

        //private bool IsFightingAny(SimpleChar target)
        //{
        //    if (Team.IsInTeam)
        //    {
        //        if (target.IsAttacking && (Team.Members.Where(c => c.Name == target.FightingTarget.Name).Any() || AttackBuddy.OSHelpers.Where(c => target.FightingTarget.Name == c).Any()))
        //            return true;
        //        if (target.IsAttacking && (target.FightingTarget.IsPet && Team.Members.Where(c => c.Name == target.FightingTarget.Name).Any()))
        //            return true;
        //        else
        //            return false;
        //    }
        //    else
        //    {
        //        if (target.IsAttacking && (target.FightingTarget.Name == DynelManager.LocalPlayer.Name || AttackBuddy.OSHelpers.Where(c => target.FightingTarget.Name == c).Any()))
        //            return true;
        //        if (target.IsAttacking && (DynelManager.LocalPlayer.Pets.Where(c => target.FightingTarget.Name == c.Character.Name).Any()))
        //            return true;
        //        else
        //            return false;
        //    }
        //}

        private bool IsFightingAny()
        {
            SimpleChar target = DynelManager.NPCs
                .Where(c => c.IsAlive)
                .Where(c => c.IsInLineOfSight)
                .Where(c => c.FightingTarget != null)
                .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f)
                .FirstOrDefault();

            if (target == null)
                return false;

            if (Team.IsInTeam)
            {
                if (Team.Members.Where(c => c.Character != null).Where(c => target.FightingTarget.Name == c.Character.Name).Any() || target.FightingTarget.DistanceFrom(DynelManager.LocalPlayer) < 6f)
                    return true;
                if (target.FightingTarget.IsPet && Team.Members.Where(c => c.Character != null).Where(c => c.Name == target.FightingTarget.Name).Any())
                    return true;
                else
                    return false;
                // maybe some sort of assist function??
                //return target.IsAttacking && (Team.Members.Where(c => target.FightingTarget == c.Character).Any() || target.FightingTarget.DistanceFrom(DynelManager.LocalPlayer) < 6f || target.FightingTarget.IsPet);
                //Team.Members.Any(x => target.FightingTarget.Identity == x.Character.Identity) || (target.IsAttacking && Team.Members.Where(x => x.Character.FightingTarget != null).Any(x => x.Character.FightingTarget.Identity == target.Identity));
            }
            else
            {
                if (target.FightingTarget.Identity == DynelManager.LocalPlayer.Identity || target.FightingTarget.DistanceFrom(DynelManager.LocalPlayer) < 6f)
                    return true;
                if (DynelManager.LocalPlayer.Pets.Where(c => target.FightingTarget.Name == c.Character.Name).Any())
                    return true;
                else
                    return false;

                //return target.IsAttacking && (target.FightingTarget.Identity == DynelManager.LocalPlayer.Identity || target.FightingTarget.DistanceFrom(DynelManager.LocalPlayer) < 6f || target.FightingTarget.IsPet);
            }
        }

        private void ListenerUseSync()
        {
            Vector3 playerPos = DynelManager.LocalPlayer.Position;

            // delayed dynel use
            if (useDynel != Identity.None)
            {
                Dynel usedynel = DynelManager.AllDynels.FirstOrDefault(x => x.Identity == useDynel);

                if (usedynel != null && Vector3.Distance(playerPos, usedynel.Position) < 8 && 
                    usedynel.Name != "Rubi-Ka Banking Service Terminal" && usedynel.Name != "Mail Terminal") //Add more
                {
                    DynelManager.GetDynel<SimpleItem>(useDynel)?.Use();
                    useDynel = Identity.None;
                }
            }
            // delayed itemonitem dynel use
            if (useOnDynel != Identity.None)
            {
                Dynel _useOnDynel = DynelManager.AllDynels.FirstOrDefault(x => x.Identity == useOnDynel);
                if (_useOnDynel != null && Vector3.Distance(playerPos, _useOnDynel.Position) < 8)
                {
                    Network.Send(new GenericCmdMessage()
                    {
                        Unknown = 1,
                        Action = GenericCmdAction.UseItemOnItem,
                        Temp1 = 0,
                        Temp4 = 0,
                        Identity = DynelManager.LocalPlayer.Identity,
                        User = DynelManager.LocalPlayer.Identity,
                        Target = useOnDynel,
                        Source = useItem

                    });
                    useOnDynel = Identity.None;
                    useItem = Identity.None;
                }
            }
        }

        private static void OnRemainingNCUMessage(int sender, IPCMessage msg)
        {
            try
            {
                if (Game.IsZoning)
                    return;

                RemainingNCUMessage ncuMessage = (RemainingNCUMessage)msg;
                    RemainingNCU[ncuMessage.Character] = ncuMessage.RemainingNCU;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e);
            }
        }

        private void OnUseMessage(int sender, IPCMessage msg)
        {

            if (/*!Team.IsInTeam || */IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            UseMessage useMsg = (UseMessage)msg;
            //DynelManager.GetDynel<SimpleItem>(useMsg.Target)?.Use();
            if (useMsg.PfId != Playfield.ModelIdentity.Instance)
                return;

            useDynel = useMsg.Target;
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

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        private void DisbandCommand(string command, string[] param, ChatWindow chatWindow)
        {
            Team.Disband();
            MultiboxHelper.BroadcastDisband();
        }

        private void RaidCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (Team.IsLeader)
                Team.ConvertToRaid();
            else
                Chat.WriteLine("Needs to be used from leader.");
        }

        private void ReformCommand(string command, string[] param, ChatWindow chatWindow)
        {
            Team.Disband();
            MultiboxHelper.BroadcastDisband();
            Task task = new Task(() =>
            {
                Thread.Sleep(1000);
                FormCommand("form", param, chatWindow);
            });
            task.Start();
        }

        private void FormCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (!DynelManager.LocalPlayer.IsInTeam())
            {
                SendTeamInvite(GetRegisteredCharactersInvite());

                if (IsRaidEnabled(param))
                {
                    Task task = new Task(() =>
                    {
                        Thread.Sleep(1000);
                        Team.ConvertToRaid();
                        Thread.Sleep(1000);
                        SendTeamInvite(GetRemainingRegisteredCharacters());
                    });
                    task.Start();
                }
            }
            else
            {
                Chat.WriteLine("Cannot form a team. Character already in team. Disband first.");
            }
        }

        private bool IsRaidEnabled(string[] param)
        {
            return param.Length > 0 && "raid".Equals(param[0]);
        }

        private Identity[] GetRegisteredCharactersInvite()
        {
            Identity[] registeredCharacters = MultiboxHelper.GetRegisteredCharacters();
            int firstTeamCount = registeredCharacters.Length > 6 ? 6 : registeredCharacters.Length;
            Identity[] firstTeamCharacters = new Identity[firstTeamCount];
            Array.Copy(registeredCharacters, firstTeamCharacters, firstTeamCount);
            return firstTeamCharacters;
        }

        private Identity[] GetRemainingRegisteredCharacters()
        {
            Identity[] registeredCharacters = MultiboxHelper.GetRegisteredCharacters();
            int characterCount = registeredCharacters.Length - 6;
            Identity[] remainingCharacters = new Identity[characterCount];
            if (characterCount > 0)
            {
                Array.Copy(registeredCharacters, 6, remainingCharacters, 0, characterCount);
            }
            return remainingCharacters;
        }

        private void SendTeamInvite(Identity[] targets)
        {
            foreach (Identity target in targets)
            {
                if (target != DynelManager.LocalPlayer.Identity)
                    Team.Invite(target);
            }
        }

        private void Team_TeamRequest(object s, TeamRequestEventArgs e)
        {
            if (MultiboxHelper.IsCharacterRegistered(e.Requester))
            {
                e.Accept();
            }
        }

        private void OnUseItemMessage(int sender, IPCMessage msg)
        {
            if (!settings["SyncUse"].AsBool())
                return;

            if (IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            UsableMessage usableMsg = (UsableMessage)msg;

            if (usableMsg.ItemLowId == 291043 || usableMsg.ItemLowId == 291043 || usableMsg.ItemLowId == 204103 || usableMsg.ItemLowId == 204104 || 
                usableMsg.ItemLowId == 204105 || usableMsg.ItemLowId == 204106 || usableMsg.ItemLowId == 204107 || usableMsg.ItemHighId == 204107 ||
                usableMsg.ItemLowId == 303138 || usableMsg.ItemLowId == 303141 || usableMsg.ItemLowId == 303137 || usableMsg.ItemHighId == 303136)
                return;

            if (usableMsg.ItemLowId == 226308 || usableMsg.ItemLowId == 226290 || usableMsg.ItemLowId == 226291 || usableMsg.ItemLowId == 226307 || usableMsg.ItemLowId == 226288)
            {
                Item NoviRings = Inventory.Items
                .Where(c => c.Name.Contains("Pure Novictum Ring"))
                .FirstOrDefault();

                if (NoviRings != null)
                {
                    useItem = new Identity(IdentityType.Inventory, NoviRings.Slot.Instance);
                    useOnDynel = usableMsg.Target;
                    usableMsg.Target = Identity.None;
                }
            }
            if (usableMsg.ItemLowId == 226188 || usableMsg.ItemLowId == 226189 || usableMsg.ItemLowId == 226190 || usableMsg.ItemLowId == 226191 || usableMsg.ItemLowId == 226192)
            {
                Item RimyRings = Inventory.Items
                .Where(c => c.Name.Contains("Rimy Ring for"))
                .FirstOrDefault();

                if (RimyRings != null)
                {
                    useItem = new Identity(IdentityType.Inventory, RimyRings.Slot.Instance);
                    useOnDynel = usableMsg.Target;
                    usableMsg.Target = Identity.None;
                }
            }
            if (usableMsg.ItemLowId == 226065 || usableMsg.ItemLowId == 226066 || usableMsg.ItemLowId == 226067 || usableMsg.ItemLowId == 226068 || usableMsg.ItemLowId == 226069)
            {
                Item AchromRings = Inventory.Items
                .Where(c => c.Name.Contains("Achromic Ring for"))
                .FirstOrDefault();

                if (AchromRings != null)
                {
                    useItem = new Identity(IdentityType.Inventory, AchromRings.Slot.Instance);
                    useOnDynel = usableMsg.Target;
                    usableMsg.Target = Identity.None;
                }
            }
            if (usableMsg.ItemLowId == 226287 || usableMsg.ItemLowId == 226293 || usableMsg.ItemLowId == 226294 || usableMsg.ItemLowId == 226295 || usableMsg.ItemLowId == 226306)
            {
                Item SangRings = Inventory.Items
                .Where(c => c.Name.Contains("Sanguine Ring for"))
                .FirstOrDefault();

                if (SangRings != null)
                {
                    useItem = new Identity(IdentityType.Inventory, SangRings.Slot.Instance);
                    useOnDynel = usableMsg.Target;
                    usableMsg.Target = Identity.None;
                }
            }
            if (usableMsg.ItemLowId == 226125 || usableMsg.ItemLowId == 226127 || usableMsg.ItemLowId == 226126 || usableMsg.ItemLowId == 226023 || usableMsg.ItemLowId == 226005)
            {
                Item CaligRings = Inventory.Items
                .Where(c => c.Name.Contains("Caliginous Ring "))
                .FirstOrDefault();

                if (CaligRings != null)
                {
                    useItem = new Identity(IdentityType.Inventory, CaligRings.Slot.Instance);
                    useOnDynel = usableMsg.Target;
                    usableMsg.Target = Identity.None;
                }
            }

            else
            {
                if (Inventory.Find(usableMsg.ItemLowId, usableMsg.ItemHighId, out Item item))
                {
                    if (usableMsg.Target == Identity.None)
                    {
                        Network.Send(new GenericCmdMessage()
                        {
                            Unknown = 1,
                            Action = GenericCmdAction.Use,
                            User = DynelManager.LocalPlayer.Identity,
                            Target = new Identity(IdentityType.Inventory, item.Slot.Instance)
                        });
                    }
                    else
                    {

                        useItem = new Identity(IdentityType.Inventory, item.Slot.Instance);
                        useOnDynel = usableMsg.Target;
                        usableMsg.Target = Identity.None;
                    }
                }
            }
        }

        public static void BroadcastDisband()
        {
            IPCChannel.Broadcast(new DisbandMessage());
        }

        private static void OnDisband(int sender, IPCMessage msg)
        {
            Team.Leave();
        }

        private void SyncUseSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0 && settings["SyncUse"].AsBool())
            {
                settings["SyncUse"] = false;
                Chat.WriteLine($"Sync use stopped.");
                return;
            }
            if (param.Length == 0 && !settings["SyncUse"].AsBool())
            {
                settings["SyncUse"] = true;
                Chat.WriteLine($"Sync use started.");
                return;
            }
        }

        private void SyncChatSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0 && settings["SyncChat"].AsBool())
            {
                settings["SyncChat"] = false;
                Chat.WriteLine($"Sync chat stopped.");
                return;
            }
            if (param.Length == 0 && !settings["SyncChat"].AsBool())
            {
                settings["SyncChat"] = true;
                Chat.WriteLine($"Sync chat started.");
                return;
            }
        }

        private void SyncTradeSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0 && settings["SyncTrade"].AsBool())
            {
                settings["SyncTrade"] = false;
                Chat.WriteLine($"Sync trading disabled.");
                return;
            }
            if (param.Length == 0 && !settings["SyncTrade"].AsBool())
            {
                settings["SyncTrade"] = true;
                Chat.WriteLine($"Sync trading enabled.");
                return;
            }
        }

        private void AutoSitSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0 && settings["AutoSit"].AsBool())
            {
                settings["AutoSit"] = false;
                Chat.WriteLine($"Auto sit stopped.");
                return;
            }
            if (param.Length == 0 && !settings["AutoSit"].AsBool())
            {
                settings["AutoSit"] = true;
                Chat.WriteLine($"Auto sit started.");
                return;
            }
        }

        private void SyncSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0 && settings["SyncMove"].AsBool())
            {
                settings["SyncMove"] = false;
                Chat.WriteLine($"Sync move stopped.");
                return;
            }
            if (param.Length == 0 && !settings["SyncMove"].AsBool())
            {
                settings["SyncMove"] = true;
                Chat.WriteLine($"Sync move started.");
                return;
            }
        }


        private void LeadFollow(string command, string[] param, ChatWindow chatWindow)
        {
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

        private void AssistPlayer(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                if (assistersname == String.Empty)
                {
                    Chat.WriteLine($"Wrong syntax, /assistplayer playername");
                    settings["AssistPlayer"] = false;
                    return;
                }
                if (!settings["AssistPlayer"].AsBool() && assistersname != String.Empty)
                {
                    settings["AssistPlayer"] = true;
                    Chat.WriteLine($"Following {assistersname}.");
                    return;
                }
                if (settings["AssistPlayer"].AsBool() && assistersname != String.Empty)
                {
                    settings["AssistPlayer"] = false;
                    Chat.WriteLine($"Stopped assisting.");
                    return;
                }
            }

            if (assistersname == String.Empty && settings["AssistPlayer"].AsBool())
            {
                Chat.WriteLine($"Cannot find player.");
                settings["AssistPlayer"] = false;
            }

            if (param.Length >= 1)
            {
                if (param[0].ToLower() == "clear")
                {
                    if (settings["AssistPlayer"].AsBool())
                    {
                        settings["AssistPlayer"] = false;
                        Chat.WriteLine($"Disabled AssistPlayer.");
                    }

                    assistersname = String.Empty;
                    Chat.WriteLine($"Assister cleared.");
                }
                else
                {
                    assistersname = param[0];
                    Chat.WriteLine($"Assisting set to {assistersname}.");
                }
            }
        }

        private void Navfollow(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                if (identitiesname == String.Empty)
                {
                    Chat.WriteLine($"Wrong syntax, /navfollow playername");
                    settings["NavFollow"] = false;
                    return;
                }
                if (!settings["NavFollow"].AsBool() && identitiesname != String.Empty)
                {
                    settings["NavFollow"] = true;
                    Chat.WriteLine($"Following {identitiesname}.");
                    return;
                }
                if (settings["NavFollow"].AsBool() && identitiesname != String.Empty)
                {
                    settings["NavFollow"] = false;
                    Chat.WriteLine($"Stopped following.");
                    return;
                }
            }

            if (identitiesname == String.Empty && settings["NavFollow"].AsBool())
            {
                Chat.WriteLine($"Cannot find player.");
                settings["NavFollow"] = false;
            }

            if (param.Length >= 1)
            {
                if (param[0].ToLower() == "clear")
                {
                    if (settings["NavFollow"].AsBool())
                    {
                        settings["NavFollow"] = false;
                        Chat.WriteLine($"Disabled NavFollow.");
                    }

                    identitiesname = String.Empty;
                    Chat.WriteLine($"Follower cleared.");
                }
                else
                {
                    identitiesname = param[0];
                    Chat.WriteLine($"Follower set to {identitiesname}.");
                }
            }
        }

        private void Allfollow(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                if (playersname == String.Empty)
                {
                    Chat.WriteLine($"Wrong syntax, /allfollow playername");
                    settings["OSFollow"] = false;
                    return;
                }
                if (!settings["OSFollow"].AsBool() && playersname != String.Empty)
                {
                    settings["OSFollow"] = true;
                    Chat.WriteLine($"Following {playersname}.");
                    return;
                }
                if (settings["OSFollow"].AsBool() && playersname != String.Empty)
                {
                    settings["OSFollow"] = false;
                    Chat.WriteLine($"Stopped following.");
                    return;
                }
            }

            if (playersname == String.Empty && settings["OSFollow"].AsBool())
            {
                Chat.WriteLine($"Cannot find player.");
                settings["OSFollow"] = false;
            }

            if (param.Length >= 1)
            {
                if (param[0].ToLower() == "clear")
                {
                    if (settings["OSFollow"].AsBool())
                    {
                        settings["OSFollow"] = false;
                        Chat.WriteLine($"Disabled OSFollow.");
                    }

                    playersname = String.Empty;
                    Chat.WriteLine($"Follower cleared.");
                }
                else
                {
                    playersname = param[0];
                    Chat.WriteLine($"Follower set to {playersname}.");
                }
            }
        }

        private void OnChannelAll(int sender, IPCMessage msg)
        {
            IPCChannelAllMessage channelMsg = (IPCChannelAllMessage)msg;

            _channelId = Convert.ToByte(channelMsg.Channel);
            IPCChannel.SetChannelId(_channelId);
            settings["ChannelID"] = _channelId;

            Chat.WriteLine($"IPC for MultiboxHelper Channel is - {_channelId}");
        }

        private void ChannelAllCommand(string command, string[] param, ChatWindow chatWindow)
        {
            try
            {
                if (IPCChannel != null)
                {
                    _channelId = Convert.ToByte(param[0]);
                    IPCChannel.SetChannelId(_channelId);
                    settings["ChannelID"] = _channelId;

                    IPCChannel.Broadcast(new IPCChannelAllMessage()
                    {
                        Channel = Convert.ToUInt16(_channelId),
                    });

                    Chat.WriteLine($"IPC for MultiboxHelper Channel is - {_channelId}");
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void ChannelCommand(string command, string[] param, ChatWindow chatWindow)
        {
            try
            {
                if (IPCChannel != null)
                {
                    if (param.Length == 0)
                    {
                        Chat.WriteLine($"IPC for MultiboxHelper Channel is - {_channelId}");
                    }

                    if (param.Length > 0)
                    {
                        _channelId = Convert.ToByte(param[0]);

                        IPCChannel.SetChannelId(_channelId);
                        settings["ChannelID"] = _channelId;

                        Chat.WriteLine($"IPC for MultiboxHelper Channel is now - {_channelId}");
                    }
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void HelpCommand(string command, string[] param, ChatWindow chatWindow)
        {
            string help = "For team commands;\n" +
                            "\n" +
                            "/form and /form raid\n" +
                            "\n" +
                            "/disband\n" +
                            "\n" +
                            "/convert to convert to raid (must be done from leader)\n" +
                            "\n" +
                            "\n" +
                            "For shortcuts to /aosharp settings;\n" +
                            "\n" +
                            "/syncchat syncs chat from current player to all\n" +
                            "\n" +
                            "/synctrade syncs trade from current player to all\n" +
                            "\n" +
                            "/syncuse for syncing items from current player to all\n" +
                            "\n" +
                            "/sync for syncing trade from current player to all\n" +
                            "\n" +
                            "/autosit auto sits to use kits\n" +
                            "\n" +
                            "/allfollow name then /allfollow to toggle\n" +
                            "\n" +
                            "/navfollow name then /navfollow to toggle\n" +
                            "(Follow the npc or player using waypoints)\n" +
                            "\n" +
                            "/assistplayer name then /assistplayer to toggle\n" +
                            "(This is implemented to avoid KSing)\n" +
                            "\n" +
                            "Add clear to the end of each of these to clear the name\n" +
                            "\n" +
                            "\n" +
                            "For IPC Channel;\n" +
                            "\n" +
                            "/mbchannel # or /mbchannelall #\n" +
                            $"Currently: {_channelId}";

            Chat.WriteLine(help, ChatColor.LightBlue);
        }

        public static bool IsBackpack(Item item)
        {
            return item.LowId == 275381 || item.LowId == 143832 || item.LowId == 157684 || item.LowId == 157689 || item.LowId == 157686 ||
                item.LowId == 157691 || item.LowId == 157692 || item.LowId == 157693 || item.LowId == 157683 || item.LowId == 157682 ||
                item.LowId == 157685 || item.LowId == 157687 || item.LowId == 157688 || item.LowId == 157694 || item.LowId == 157695 ||
                item.LowId == 157690 || item.LowId == 99241 || item.LowId == 304586 || item.LowId == 158790 || item.LowId == 99228 ||
                item.LowId == 223770 || item.LowId == 152039 || item.LowId == 156831 || item.LowId == 259016 || item.LowId == 259382 ||
                item.LowId == 287417 || item.LowId == 287418 || item.LowId == 287419 || item.LowId == 287420 || item.LowId == 287421 ||
                item.LowId == 287422 || item.LowId == 287423 || item.LowId == 287424 || item.LowId == 287425 || item.LowId == 287426 ||
                item.LowId == 287427 || item.LowId == 287428 || item.LowId == 287429 || item.LowId == 287430 || item.LowId == 287431 ||
                item.LowId == 287432 || item.LowId == 287433 | item.LowId == 287434 || item.LowId == 287435 || item.LowId == 287436 ||
                item.LowId == 287437 || item.LowId == 287438 || item.LowId == 287439 || item.LowId == 287440 || item.LowId == 287441 ||
                item.LowId == 287442 || item.LowId == 287443 || item.LowId == 287444 || item.LowId == 287445 || item.LowId == 287446 ||
                item.LowId == 287447 || item.LowId == 287448 || item.LowId == 287609 || item.LowId == 287610 || item.LowId == 287611 ||
                item.LowId == 287612 || item.LowId == 287613 || item.LowId == 287614 || item.LowId == 287615 || item.LowId == 287616 ||
                item.LowId == 287617 || item.LowId == 287618 || item.LowId == 287619 || item.LowId == 287620;
        }

        private void OnNavFollowMessage(int sender, IPCMessage msg)
        {

            NavFollowMessage followMessage = (NavFollowMessage)msg;

            Dynel targetDynel = DynelManager.GetDynel(followMessage.Target);

            if (targetDynel != null)
            {
                if (DynelManager.LocalPlayer.DistanceFrom(targetDynel) <= 6f)
                    MovementController.Instance.Halt();

                if (DynelManager.LocalPlayer.DistanceFrom(targetDynel) > 6f)
                    MovementController.Instance.SetDestination(targetDynel.Position);
                _lastFollowTime = Time.NormalTime;
            }
            else
            {
                Chat.WriteLine($"Cannot find {targetDynel.Name}. Make sure to type captial first letter.");
                settings["NavFollow"] = false;
                return;
            }
        }

        private void OnAssistMessage(int sender, IPCMessage msg)
        {
            AssistMessage assistMessage = (AssistMessage)msg;

            Dynel targetDynel = DynelManager.GetDynel(assistMessage.Target);

            if (targetDynel != null && DynelManager.LocalPlayer.FightingTarget == null)
            {
                DynelManager.LocalPlayer.Attack(targetDynel);
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
            MovementController.Instance.SetMovement(MovementAction.Update);
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
            MovementController.Instance.SetMovement(MovementAction.Update);
        }
    }
}
