using System;
using System.Linq;
using System.Diagnostics;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using Helper.IPCMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.GameData;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AOSharp.Core.Inventory;
using AOSharp.Common.GameData.UI;

namespace Helper
{
    public class Helper : AOPluginEntry
    {
        private static IPCChannel IPCChannel;

        public static Config Config { get; private set; }

        private static Identity useDynel;
        private static Identity useOnDynel;
        private static Identity useItem;

        private static Pet healpet;

        public static string PluginDirectory;

        private static double _ncuUpdateTime;
        private static double _updateTick;
        private static double _useTimer;
        private static double _sitUpdateTimer;
        private static double _sitPetUpdateTimer;
        private static double _sitPetUsedTimer;
        private static double _shapeUsedTimer;
        private static double _followTimer;
        private static double _assistTimer;
        private static double _morphPathingTimer;
        private static double _bellyPathingTimer;
        private static double _zixMorphTimer;

        public static bool Sitting = false;
        public static bool HealingPet = false;
        public static bool OpenBackpacks = false;

        public static Window followWindow;
        public static Window assistWindow;
        public static Window infoWindow;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private static Settings _settings = new Settings("Helper");

        private static Settings _assist = new Settings("Assist");
        private static Settings _follow = new Settings("Follow");
        private static Settings _info = new Settings("Info");

        private static Item _bagItem;

        List<Vector3> MorphBird = new List<Vector3>
        {
            new Vector3(75.5, 29.0, 58.6),
            new Vector3(37.3, 29.0, 59.0),
            new Vector3(35.6, 29.3, 30.5),
            new Vector3(37.3, 29.0, 59.0),
            new Vector3(75.5, 29.0, 58.6),
            new Vector3(75.5, 29.0, 58.6)
            //new Vector3(76.1, 29.0, 28.3)
        };

        List<Vector3> BellyPath = new List<Vector3>
        {
            new Vector3(143.1f, 90.0f, 108.2f),
            new Vector3(156.1f, 90.0f, 102.3f),
            new Vector3(178.0f, 90.0f, 97.6f)
        };

        List<Vector3> MorphHorse = new List<Vector3>
        {
            new Vector3(128.4, 29.0, 59.6),
            new Vector3(161.9, 29.0, 59.5),
            new Vector3(163.9, 29.4, 29.6),
            new Vector3(161.9, 29.0, 59.5),
            new Vector3(128.4, 29.0, 59.6),
            new Vector3(128.4, 29.0, 59.6)
            //new Vector3(76.1, 29.0, 28.3)
        };

        public byte _channelId;

        private bool IsActiveWindow => GetForegroundWindow() == Process.GetCurrentProcess().MainWindowHandle;

        public override void Run(string pluginDir)
        {
            PluginDirectory = pluginDir;

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\Helper\\{Game.ClientInst}\\Config.json");

            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[Game.ClientInst].IPCChannel));

            _follow.AddVariable("FollowSelection", (int)FollowSelection.None);
            _assist.AddVariable("AttackSelection", (int)AttackSelection.Sync);

            _settings.AddVariable("AutoSit", true);
            _settings.AddVariable("AutoBags", true);

            _settings.AddVariable("SyncMove", false);
            _settings.AddVariable("SyncUse", true);
            _settings.AddVariable("SyncChat", false);
            _settings.AddVariable("SyncTrade", false);

            _settings.AddVariable("MorphPathing", false);
            _settings.AddVariable("BellyPathing", false);
            _settings.AddVariable("Db3Shapes", false);

            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            IPCChannel.RegisterCallback((int)IPCOpcode.Move, OnMoveMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Jump, OnJumpMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Target, OnTargetMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Attack, OnAttackMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.StopAttack, OnStopAttackMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Assist, OnAssistMessage);

            IPCChannel.RegisterCallback((int)IPCOpcode.Follow, OnFollowMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NavFollow, OnNavFollowMessage);

            IPCChannel.RegisterCallback((int)IPCOpcode.Trade, OnTradeMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Use, OnUseMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.UseItem, OnUseItemMessage);

            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChatOpen, OnNpcChatOpenMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChatClose, OnNpcChatCloseMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChatAnswer, OnNpcChatAnswerMessage);

            IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);
            IPCChannel.RegisterCallback((int)IPCOpcode.ClearBuffs, OnClearBuffs);

            IPCChannel.RegisterCallback((int)IPCOpcode.YalmOn, OnYalmCast);
            IPCChannel.RegisterCallback((int)IPCOpcode.YalmUse, OnYalmUse);
            IPCChannel.RegisterCallback((int)IPCOpcode.YalmOff, OnYalmCancel);

            SettingsController.RegisterSettingsWindow("Helper", pluginDir + "\\UI\\HelperSettingWindow.xml", _settings);

            SettingsController.RegisterSettingsWindow("Assist", pluginDir + "\\UI\\HelperAssistView.xml", _assist);
            SettingsController.RegisterSettingsWindow("Follow", pluginDir + "\\UI\\HelperFollowView.xml", _follow);
            SettingsController.RegisterSettingsWindow("Info", pluginDir + "\\UI\\HelperInfoView.xml", _info);

            Chat.RegisterCommand("sync", SyncSwitch);
            Chat.RegisterCommand("leadfollow", LeadFollowSwitch);
            Chat.RegisterCommand("autosit", AutoSitSwitch);

            Chat.RegisterCommand("syncuse", SyncUseSwitch);
            Chat.RegisterCommand("syncchat", SyncChatSwitch);
            Chat.RegisterCommand("synctrade", SyncTradeSwitch);

            Chat.RegisterCommand("reform", ReformCommand);
            Chat.RegisterCommand("form", FormCommand);
            Chat.RegisterCommand("disband", DisbandCommand);
            Chat.RegisterCommand("convert", RaidCommand);

            Chat.RegisterCommand("yalm", YalmCommand);
            Chat.RegisterCommand("rebuff", Rebuff);

            //Chat.RegisterCommand("bags", (string command, string[] param, ChatWindow chatWindow) =>
            //{
            //    List<Item> bags = Inventory.Items
            //    .Where(c => c.UniqueIdentity.Type == IdentityType.Container)
            //    .ToList();

            //    Chat.WriteLine($"{bags.Count()}");

            //    foreach (Item bag in bags)
            //    {
            //        bag.Use();
            //        bag.Use();
            //    }
            //});

            Chat.RegisterCommand("doc", DocTarget);


            Game.OnUpdate += OnUpdate;
            Network.N3MessageSent += Network_N3MessageSent;
            Team.TeamRequest = Team_TeamRequest;
            Game.TeleportEnded += OnZoned;


            Chat.WriteLine("Helper Loaded!");
            Chat.WriteLine("/helper for settings.");
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        private void OnZoned(object s, EventArgs e)
        {
            if (_settings["AutoBags"].AsBool())
            {
                Task.Factory.StartNew(
                    async () =>
                    {
                        await Task.Delay(100);

                        List<Item> bags = Inventory.Items
                            .Where(c => c.UniqueIdentity.Type == IdentityType.Container)
                            .ToList();

                        foreach (Item bag in bags)
                        {
                            bag.Use();
                            bag.Use();
                        }
                    });
            }
        }


        private void Network_N3MessageSent(object s, N3Message n3Msg)
        {
            if (!IsActiveCharacter() || n3Msg.Identity != DynelManager.LocalPlayer.Identity) { return; }

            if (n3Msg.N3MessageType == N3MessageType.CharDCMove)
            {
                CharDCMoveMessage charDCMoveMsg = (CharDCMoveMessage)n3Msg;

                if (charDCMoveMsg.MoveType == MovementAction.JumpStart && !_settings["SyncMove"].AsBool())
                {
                    IPCChannel.Broadcast(new JumpMessage()
                    {
                        MoveType = charDCMoveMsg.MoveType,
                        PlayfieldId = Playfield.Identity.Instance,
                    });
                }
                else
                {
                    if (!_settings["SyncMove"].AsBool()) { return; }

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
                if (!_settings["SyncTrade"].AsBool()) { return; }

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
                if (!_settings["SyncMove"].AsBool()) { return; }

                CharacterActionMessage charActionMsg = (CharacterActionMessage)n3Msg;

                if (charActionMsg.Action != CharacterActionType.StandUp) { return; }

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
            else if (n3Msg.N3MessageType == N3MessageType.Attack )
            {
                if (AttackSelection.Sync != (AttackSelection)_assist["AttackSelection"].AsInt32()) { return; }

                AttackMessage attackMsg = (AttackMessage)n3Msg;
                IPCChannel.Broadcast(new AttackIPCMessage()
                {
                    Target = attackMsg.Target
                });
            }
            else if (n3Msg.N3MessageType == N3MessageType.StopFight)
            {
                if (AttackSelection.Sync != (AttackSelection)_assist["AttackSelection"].AsInt32()) { return; }

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
                else if (genericCmdMsg.Action == GenericCmdAction.Use && _settings["SyncUse"].AsBool())
                {
                    if (Inventory.Find(genericCmdMsg.Target, out Item item) && item.UniqueIdentity == Identity.None && !IsOther(item))
                    {
                        IPCChannel.Broadcast(new UsableMessage()
                        {
                            ItemLowId = item.LowId,
                            ItemHighId = item.HighId,
                        });
                    }
                    else
                    {
                        foreach (Backpack bag in Inventory.Backpacks)
                        {
                            _bagItem = bag.Items
                                .Where(c => c.Slot.Instance == genericCmdMsg.Target.Instance)
                                .FirstOrDefault();

                            if (_bagItem != null)
                            {
                                IPCChannel.Broadcast(new UsableMessage()
                                {
                                    ItemLowId = _bagItem.LowId,
                                    ItemHighId = _bagItem.HighId
                                });
                            }
                        }
                    }
                }
                else if (genericCmdMsg.Action == GenericCmdAction.UseItemOnItem)
                {
                    if (Inventory.Find(genericCmdMsg.Source, out Item item))
                    {
                        IPCChannel.Broadcast(new UsableMessage()
                        {
                            ItemLowId = item.LowId,
                            ItemHighId = item.HighId,
                            Target = genericCmdMsg.Target
                        });
                    }
                    else 
                    {
                        foreach (Backpack bag in Inventory.Backpacks)
                        {
                            _bagItem = bag.Items
                                .Where(c => c.Slot.Instance == genericCmdMsg.Source.Instance)
                                .FirstOrDefault();

                            if (_bagItem != null)
                            {
                                IPCChannel.Broadcast(new UsableMessage()
                                {
                                    ItemLowId = _bagItem.LowId,
                                    ItemHighId = _bagItem.HighId,
                                    Target = genericCmdMsg.Target
                                });
                            }
                        }
                    }
                }
            }
            else if (n3Msg.N3MessageType == N3MessageType.KnubotOpenChatWindow)
            {
                if (!_settings["SyncChat"].AsBool()) { return; }

                KnuBotOpenChatWindowMessage n3OpenChatMessage = (KnuBotOpenChatWindowMessage)n3Msg;
                IPCChannel.Broadcast(new NpcChatOpenMessage()
                {
                    Target = n3OpenChatMessage.Target
                });
            }
            else if (n3Msg.N3MessageType == N3MessageType.KnubotCloseChatWindow)
            {
                if (!_settings["SyncChat"].AsBool()) { return; }

                KnuBotCloseChatWindowMessage n3CloseChatMessage = (KnuBotCloseChatWindowMessage)n3Msg;
                IPCChannel.Broadcast(new NpcChatCloseMessage()
                {
                    Target = n3CloseChatMessage.Target
                });
            }
            else if (n3Msg.N3MessageType == N3MessageType.KnubotAnswer)
            {
                if (!_settings["SyncChat"].AsBool()) { return; }

                KnuBotAnswerMessage n3AnswerMsg = (KnuBotAnswerMessage)n3Msg;
                IPCChannel.Broadcast(new NpcChatAnswerMessage()
                {
                    Target = n3AnswerMsg.Target,
                    Answer = n3AnswerMsg.Answer
                });
            }
        }


        private void OnUpdate(object s, float deltaTime)
        {
            if (assistWindow != null && assistWindow.IsValid)
            {
                assistWindow.FindView("FollowNamedCharacter", out TextInputView textinput1);
                assistWindow.FindView("FollowNamedIdentity", out TextInputView textinput2);
                assistWindow.FindView("NavFollowDistanceBox", out TextInputView textinput3);
                assistWindow.FindView("AssistNamedCharacter", out TextInputView textinput4);

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (Config.CharSettings[Game.ClientInst].FollowPlayer != textinput1.Text)
                    {
                        Config.CharSettings[Game.ClientInst].FollowPlayer = textinput1.Text;
                        SettingsController.HelperFollowPlayer = textinput1.Text;
                        Config.Save();
                    }
                }

                if (textinput2 != null && textinput2.Text != String.Empty)
                {
                    if (Config.CharSettings[Game.ClientInst].NavFollowPlayer != textinput2.Text)
                    {
                        Config.CharSettings[Game.ClientInst].NavFollowPlayer = textinput2.Text;
                        SettingsController.HelperNavFollowPlayer = textinput2.Text;
                        Config.Save();
                    }
                }

                if (textinput3 != null && textinput3.Text != String.Empty)
                {
                    if (int.TryParse(textinput3.Text, out int rangeValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].NavFollowDistance != rangeValue)
                        {
                            Config.CharSettings[Game.ClientInst].NavFollowDistance = rangeValue;
                            SettingsController.HelperNavFollowDistance = rangeValue;
                            Config.Save();
                        }
                    }
                }
                if (textinput4 != null && textinput4.Text != String.Empty)
                {
                    if (Config.CharSettings[Game.ClientInst].AssistPlayer != textinput4.Text)
                    {
                        Config.CharSettings[Game.ClientInst].AssistPlayer = textinput4.Text;
                        SettingsController.HelperAssistPlayer = textinput4.Text;
                        Config.Save();
                    }
                }
            }

            if (followWindow != null && followWindow.IsValid)
            {
                followWindow.FindView("FollowNamedCharacter", out TextInputView textinput1);
                followWindow.FindView("FollowNamedIdentity", out TextInputView textinput2);
                followWindow.FindView("NavFollowDistanceBox", out TextInputView textinput3);
                followWindow.FindView("AssistNamedCharacter", out TextInputView textinput4);

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (Config.CharSettings[Game.ClientInst].FollowPlayer != textinput1.Text)
                    {
                        Config.CharSettings[Game.ClientInst].FollowPlayer = textinput1.Text;
                        SettingsController.HelperFollowPlayer = textinput1.Text;
                        Config.Save();
                    }
                }

                if (textinput2 != null && textinput2.Text != String.Empty)
                {
                    if (Config.CharSettings[Game.ClientInst].NavFollowPlayer != textinput2.Text)
                    {
                        Config.CharSettings[Game.ClientInst].NavFollowPlayer = textinput2.Text;
                        SettingsController.HelperNavFollowPlayer = textinput2.Text;
                        Config.Save();
                    }
                }

                if (textinput3 != null && textinput3.Text != String.Empty)
                {
                    if (int.TryParse(textinput3.Text, out int rangeValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].NavFollowDistance != rangeValue)
                        {
                            Config.CharSettings[Game.ClientInst].NavFollowDistance = rangeValue;
                            SettingsController.HelperNavFollowDistance = rangeValue;
                            Config.Save();
                        }
                    }
                }

                if (textinput4 != null && textinput4.Text != String.Empty)
                {
                    if (Config.CharSettings[Game.ClientInst].AssistPlayer != textinput4.Text)
                    {
                        Config.CharSettings[Game.ClientInst].AssistPlayer = textinput4.Text;
                        SettingsController.HelperAssistPlayer = textinput4.Text;
                        Config.Save();
                    }
                }
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView textinput1);

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (int.TryParse(textinput1.Text, out int channelValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].IPCChannel != channelValue)
                        {
                            IPCChannel.SetChannelId(Convert.ToByte(channelValue));
                            Config.CharSettings[Game.ClientInst].IPCChannel = Convert.ToByte(channelValue);
                            SettingsController.HelperChannel = channelValue.ToString();
                            Config.Save();
                        }
                    }
                }

                if (SettingsController.settingsWindow.FindView("HelperInfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = InfoView;
                }

                if (SettingsController.settingsWindow.FindView("HelperFollowView", out Button followView))
                {
                    followView.Tag = SettingsController.settingsWindow;
                    followView.Clicked = FollowView;
                }

                if (SettingsController.settingsWindow.FindView("HelperAssistView", out Button assistView))
                {
                    assistView.Tag = SettingsController.settingsWindow;
                    assistView.Clicked = AssistView;
                }
            }

            if (SettingsController.HelperChannel == String.Empty)
            {
                SettingsController.HelperChannel = Config.IPCChannel.ToString();
            }

            if (SettingsController.HelperAssistPlayer == String.Empty)
            {
                SettingsController.HelperAssistPlayer = Config.AssistPlayer;
            }

            if (SettingsController.HelperFollowPlayer == String.Empty)
            {
                SettingsController.HelperFollowPlayer = Config.FollowPlayer;
            }

            if (SettingsController.HelperNavFollowPlayer == String.Empty)
            {
                SettingsController.HelperNavFollowPlayer = Config.NavFollowPlayer;
            }

            if (SettingsController.HelperNavFollowDistance != Config.NavFollowDistance)
            {
                SettingsController.HelperNavFollowDistance = Config.NavFollowDistance;
            }

            if (!OpenBackpacks && _settings["AutoBags"].AsBool())
            {
                List<Item> bags = Inventory.Items
                    .Where(c => c.UniqueIdentity.Type == IdentityType.Container)
                    .ToList();

                foreach (Item bag in bags)
                {
                    bag.Use();
                    bag.Use();
                }

                OpenBackpacks = true;
            }

            if (Time.NormalTime > _ncuUpdateTime + 0.5f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }


            if (Time.NormalTime > _updateTick + 8f)
            {
                List<SimpleChar> PlayersInRange = DynelManager.Characters
                    .Where(x => x.IsPlayer)
                    .Where(x => DynelManager.LocalPlayer.DistanceFrom(x) < 30f)
                    .ToList();

                foreach (SimpleChar player in PlayersInRange)
                {
                    Network.Send(new CharacterActionMessage()
                    {
                        Action = CharacterActionType.InfoRequest,
                        Target = player.Identity

                    });
                }

                _updateTick = Time.NormalTime;
            }

            if (_settings["BellyPathing"].AsBool() && Time.NormalTime > _bellyPathingTimer + 1)
            {
                Dynel Pustule = DynelManager.AllDynels
                    .Where(x => x.Identity.Type == IdentityType.Terminal && DynelManager.LocalPlayer.DistanceFrom(x) < 7f
                        && x.Name == "Glowing Pustule")
                    .FirstOrDefault();

                if (Pustule != null)
                {
                    Pustule.Use();
                    return;
                }

                if (DynelManager.LocalPlayer.Position.DistanceFrom(new Vector3(132.0f, 90.0f, 117.0f)) < 10f
                    && !MovementController.Instance.IsNavigating)
                {
                    MovementController.Instance.SetPath(BellyPath);
                }

                _bellyPathingTimer = Time.NormalTime;
            }

            if (_settings["MorphPathing"].AsBool() && Time.NormalTime > _morphPathingTimer + 3)
            {
                if (!MovementController.Instance.IsNavigating && DynelManager.LocalPlayer.Buffs.Contains(281109))
                {
                    Vector3 curr = DynelManager.LocalPlayer.Position;

                    MovementController.Instance.SetPath(MorphBird);
                    MovementController.Instance.AppendDestination(curr);
                }

                if (!MovementController.Instance.IsNavigating && DynelManager.LocalPlayer.Buffs.Contains(281108))
                {
                    Vector3 curr = DynelManager.LocalPlayer.Position;

                    MovementController.Instance.SetPath(MorphHorse);
                    MovementController.Instance.AppendDestination(curr);
                }

                _morphPathingTimer = Time.NormalTime;
            }

            if (_settings["Db3Shapes"].AsBool() && Time.NormalTime > _shapeUsedTimer + 0.5)
            {
                Dynel shape = DynelManager.AllDynels
                    .Where(x => x.Identity.Type == IdentityType.Terminal && DynelManager.LocalPlayer.DistanceFrom(x) < 5f
                        && (x.Name == "Triangle of Nano Power" || x.Name == "Cylinder of Speed"
                    || x.Name == "Torus of Aim" || x.Name == "Square of Attack Power"))
                    .FirstOrDefault();

                if (shape != null)
                {
                    shape.Use();
                }

                _shapeUsedTimer = Time.NormalTime;
            }


            if (Time.NormalTime > _sitUpdateTimer + 0.5)
            {
                ListenerSit();

                _sitUpdateTimer = Time.NormalTime;
            }

            if (Time.NormalTime > _zixMorphTimer + 3)
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(288532) || DynelManager.LocalPlayer.Buffs.Contains(302212))
                {
                    CancelBuffs(RelevantNanos.ZixMorph);
                }

                _zixMorphTimer = Time.NormalTime;
            }

            if (Time.NormalTime > _sitPetUpdateTimer + 2)
            {
                if (DynelManager.LocalPlayer.Profession == Profession.Metaphysicist)
                    ListenerPetSit();

                _sitPetUpdateTimer = Time.NormalTime;
            }

            if (Time.NormalTime > _useTimer + 0.1)
            {
                if (!IsActiveWindow)
                {
                    ListenerUseSync();
                }
                _useTimer = Time.NormalTime;
            }

            if (AttackSelection.Assist == (AttackSelection)_assist["AttackSelection"].AsInt32()
                && Time.NormalTime > _assistTimer + 1)
            {
                //if (settings["SyncAttack"].AsBool())
                //{
                //    settings["SyncAttack"] = false;
                //    settings["AssistPlayer"] = false;
                //    Chat.WriteLine($"Can only have one form of sync attack active at once.");
                //}

                SimpleChar identity = DynelManager.Characters
                    .Where(c => SettingsController.HelperAssistPlayer != String.Empty)
                    .Where(c => c.IsAlive)
                    .Where(x => !x.Flags.HasFlag(CharacterFlags.Pet))
                    .Where(c => c.Name == SettingsController.HelperAssistPlayer)
                    .FirstOrDefault();

                if (identity == null) { return; }

                if (identity != null && identity.FightingTarget == null &&
                    DynelManager.LocalPlayer.FightingTarget != null)
                {
                    DynelManager.LocalPlayer.StopAttack();

                    IPCChannel.Broadcast(new StopAttackIPCMessage());
                    _assistTimer = Time.NormalTime;
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
                    _assistTimer = Time.NormalTime;
                }
            }

            if (FollowSelection.LeadFollow == (FollowSelection)_follow["FollowSelection"].AsInt32()
                && Time.NormalTime > _followTimer + 1)
            {
                //if (settings["OSFollow"].AsBool() || settings["NavFollow"].AsBool())
                //{
                //    settings["OSFollow"] = false;
                //    settings["Follow"] = false;
                //    settings["NavFollow"] = false;
                //    Chat.WriteLine($"Can only have one follow active at once.");
                //}

                IPCChannel.Broadcast(new FollowMessage()
                {
                    Target = DynelManager.LocalPlayer.Identity
                });
                _followTimer = Time.NormalTime;
            }

            if (FollowSelection.NavFollow == (FollowSelection)_follow["FollowSelection"].AsInt32()
                && Time.NormalTime > _followTimer + 1)
            {
                //if (settings["Follow"].AsBool() || settings["OSFollow"].AsBool())
                //{
                //    settings["OSFollow"] = false;
                //    settings["Follow"] = false;
                //    settings["NavFollow"] = false;
                //    Chat.WriteLine($"Can only have one follow active at once.");
                //}

                Dynel identity = DynelManager.AllDynels
                    .Where(x => !x.Flags.HasFlag(CharacterFlags.Pet))
                    .Where(x => SettingsController.HelperNavFollowPlayer != String.Empty)
                    .Where(x => x.Name == SettingsController.HelperNavFollowPlayer)
                    .FirstOrDefault();

                if (identity != null)
                {
                    if (DynelManager.LocalPlayer.DistanceFrom(identity) <= Config.CharSettings[Game.ClientInst].NavFollowDistance)
                        MovementController.Instance.Halt();

                    if (DynelManager.LocalPlayer.DistanceFrom(identity) > Config.CharSettings[Game.ClientInst].NavFollowDistance)
                        MovementController.Instance.SetDestination(identity.Position);

                    IPCChannel.Broadcast(new NavFollowMessage()
                    {
                        Target = identity.Identity
                    });
                    _followTimer = Time.NormalTime;
                }
            }

            if (FollowSelection.OSFollow == (FollowSelection)_follow["FollowSelection"].AsInt32()
                && Time.NormalTime > _followTimer + 1)
            {
                if (SettingsController.HelperFollowPlayer != String.Empty)
                {
                    Dynel identity = DynelManager.AllDynels
                        .Where(x => !x.Flags.HasFlag(CharacterFlags.Pet))
                        .Where(x => SettingsController.HelperFollowPlayer != String.Empty)
                        .Where(x => x.Name == SettingsController.HelperFollowPlayer)
                        .FirstOrDefault();

                    if (identity != null)
                    {
                        if (identity.Identity != DynelManager.LocalPlayer.Identity)
                            OSFollow(identity);

                        IPCChannel.Broadcast(new FollowMessage()
                        {
                            Target = identity.Identity // change this to the new target with selection param
                        });

                        _followTimer = Time.NormalTime;
                    }
                }
            }
        }

        private void OnClearBuffs(int sender, IPCMessage msg)
        {
            CancelAllBuffs();
        }

        private void OnDisband(int sender, IPCMessage msg)
        {
            Team.Leave();
        }

        private void OnNavFollowMessage(int sender, IPCMessage msg)
        {

            NavFollowMessage followMessage = (NavFollowMessage)msg;

            Dynel targetDynel = DynelManager.GetDynel(followMessage.Target);

            if (targetDynel != null)
            {
                if (DynelManager.LocalPlayer.DistanceFrom(targetDynel) <= 15f)
                    MovementController.Instance.Halt();

                if (DynelManager.LocalPlayer.DistanceFrom(targetDynel) > 15f)
                    MovementController.Instance.SetDestination(targetDynel.Position);
                _followTimer = Time.NormalTime;
            }
            else
            {
                Chat.WriteLine($"Cannot find {targetDynel.Name}. Make sure to type captial first letter.");
                _settings["NavFollow"] = false;
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

        private void OnYalmCast(int sender, IPCMessage msg)
        {
            YalmOnMessage yalmMsg = (YalmOnMessage)msg;

            Spell yalm = Spell.List.FirstOrDefault(x => x.Identity.Instance == yalmMsg.Spell);

            Spell yalm2 = Spell.List.FirstOrDefault(x => RelevantNanos.Yalms.Contains(x.Identity.Instance));

            if (yalm != null)
            {
                yalm.Cast(false);
            }
            else if (yalm2 != null)
            {
                yalm2.Cast(false);
            }
            else
            {
                Item yalm3 = Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.Inventory).FirstOrDefault();

                if (yalm3 != null)
                    yalm3.Equip(EquipSlot.Weap_Hud1);
            }
        }

        private void OnYalmUse(int sender, IPCMessage msg)
        {
            YalmUseMessage yalmMsg = (YalmUseMessage)msg;

            Item yalm = Inventory.Items.FirstOrDefault(x => x.HighId == yalmMsg.Item);

            Item yalm2 = Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.Inventory).FirstOrDefault();

            if (yalm != null)
            {
                yalm.Equip(EquipSlot.Weap_Hud1);
            }
            else if (yalm2 != null)
            {
                yalm2.Equip(EquipSlot.Weap_Hud1);
            }
            else
            {
                Spell yalm3 = Spell.List.FirstOrDefault(x => RelevantNanos.Yalms.Contains(x.Identity.Instance));

                if (yalm3 != null)
                    yalm3.Cast(false);
            }
        }

        private void OnYalmCancel(int sender, IPCMessage msg)
        {
            if (Inventory.Items.Where(x => x.Name.Contains("Yalm")).Where(x => x.Slot.Type == IdentityType.WeaponPage).Any())
            {
                Item yalm = Inventory.Items.Where(x => x.Name.Contains("Yalm")).Where(x => x.Slot.Type == IdentityType.WeaponPage).FirstOrDefault();

                if (yalm != null)
                    yalm.MoveToInventory();
            }
            else
                CancelBuffs(RelevantNanos.Yalms);
        }

        private void OnJumpMessage(int sender, IPCMessage msg)
        {
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

        private void OnTradeMessage(int sender, IPCMessage msg)
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

        private void OnRemainingNCUMessage(int sender, IPCMessage msg)
        {
            try
            {
                if (Game.IsZoning)
                    return;

                RemainingNCUMessage ncuMessage = (RemainingNCUMessage)msg;
                SettingsController.RemainingNCU[ncuMessage.Character] = ncuMessage.RemainingNCU;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e);
            }
        }

        private void OnUseItemMessage(int sender, IPCMessage msg)
        {
            if (!_settings["SyncUse"].AsBool() || IsActiveWindow || Game.IsZoning) { return; }

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
                else
                {
                    foreach (Backpack bag in Inventory.Backpacks)
                    {
                        _bagItem = bag.Items
                            .Where(c => c.Name.Contains("Pure Novictum Ring"))
                            .FirstOrDefault();

                        if (_bagItem != null)
                        {
                            useItem = _bagItem.Slot;
                            useOnDynel = usableMsg.Target;
                            usableMsg.Target = Identity.None;
                        }
                    }
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
                else
                {
                    foreach (Backpack bag in Inventory.Backpacks)
                    {
                        _bagItem = bag.Items
                            .Where(c => c.Name.Contains("Rimy Ring for"))
                            .FirstOrDefault();

                        if (_bagItem != null)
                        {
                            useItem = _bagItem.Slot;
                            useOnDynel = usableMsg.Target;
                            usableMsg.Target = Identity.None;
                        }
                    }
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
                else
                {
                    foreach (Backpack bag in Inventory.Backpacks)
                    {
                        _bagItem = bag.Items
                            .Where(c => c.Name.Contains("Achromic Ring for"))
                            .FirstOrDefault();

                        if (_bagItem != null)
                        {
                            useItem = _bagItem.Slot;
                            useOnDynel = usableMsg.Target;
                            usableMsg.Target = Identity.None;
                        }
                    }
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
                else
                {
                    foreach (Backpack bag in Inventory.Backpacks)
                    {
                        _bagItem = bag.Items
                            .Where(c => c.Name.Contains("Sanguine Ring for"))
                            .FirstOrDefault();

                        if (_bagItem != null)
                        {
                            useItem = _bagItem.Slot;
                            useOnDynel = usableMsg.Target;
                            usableMsg.Target = Identity.None;
                        }
                    }
                }
            }
            if (usableMsg.ItemLowId == 226125 || usableMsg.ItemLowId == 226127 || usableMsg.ItemLowId == 226126 || usableMsg.ItemLowId == 226023 || usableMsg.ItemLowId == 226005)
            {
                Item CaligRings = Inventory.Items
                .Where(c => c.Name.Contains("Caliginous Ring"))
                .FirstOrDefault();

                if (CaligRings != null)
                {
                    useItem = new Identity(IdentityType.Inventory, CaligRings.Slot.Instance);
                    useOnDynel = usableMsg.Target;
                    usableMsg.Target = Identity.None;
                }
                else
                {
                    foreach (Backpack bag in Inventory.Backpacks)
                    {
                        _bagItem = bag.Items
                            .Where(c => c.Name.Contains("Caliginous Ring"))
                            .FirstOrDefault();

                        if (_bagItem != null)
                        {
                            useItem = _bagItem.Slot;
                            useOnDynel = usableMsg.Target;
                            usableMsg.Target = Identity.None;
                        }
                    }
                }
            }
            else
            {
                if (usableMsg.Target == Identity.None)
                {
                    if (Inventory.Find(usableMsg.ItemLowId, usableMsg.ItemHighId, out Item item))
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
                        foreach (Backpack bag in Inventory.Backpacks)
                        {
                            _bagItem = bag.Items
                                .Where(c => c.HighId == usableMsg.ItemHighId)
                                .FirstOrDefault();

                            if (_bagItem != null)
                            {
                                Network.Send(new GenericCmdMessage()
                                {
                                    Unknown = 1,
                                    Action = GenericCmdAction.Use,
                                    User = DynelManager.LocalPlayer.Identity,
                                    Target = _bagItem.Slot
                                });
                            }
                        }
                    }
                }
                else
                {
                    if (Inventory.Find(usableMsg.ItemLowId, usableMsg.ItemHighId, out Item item))
                    {
                        useItem = new Identity(IdentityType.Inventory, item.Slot.Instance);
                        useOnDynel = usableMsg.Target;
                        usableMsg.Target = Identity.None;
                    }
                    else
                    {
                        foreach (Backpack bag in Inventory.Backpacks)
                        {
                            _bagItem = bag.Items
                                .Where(c => c.HighId == usableMsg.ItemHighId)
                                .FirstOrDefault();

                            if (_bagItem != null)
                            {
                                Network.Send(new GenericCmdMessage()
                                {
                                    Unknown = 1,
                                    Action = GenericCmdAction.UseItemOnItem,
                                    User = DynelManager.LocalPlayer.Identity,
                                    Target = usableMsg.Target,
                                    Source = _bagItem.Slot
                                });
                            }
                        }
                    }
                }
            }
        }

        private void OnUseMessage(int sender, IPCMessage msg)
        {

            if (IsActiveWindow || Game.IsZoning) { return; }

            UseMessage useMsg = (UseMessage)msg;
            //DynelManager.GetDynel<SimpleItem>(useMsg.Target)?.Use();
            if (useMsg.PfId != Playfield.ModelIdentity.Instance) { return; }

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

        private void SyncUseSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["SyncUse"] = !_settings["SyncUse"].AsBool();
                Chat.WriteLine($"Sync use : {_settings["SyncUse"].AsBool()}");
            }
        }

        private void SyncChatSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["SyncChat"] = !_settings["SyncChat"].AsBool();
                Chat.WriteLine($"Sync chat : {_settings["SyncChat"].AsBool()}");
            }
        }

        private void SyncTradeSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["SyncTrade"] = !_settings["SyncTrade"].AsBool();
                Chat.WriteLine($"Sync trading : {_settings["SyncTrade"].AsBool()}");
            }
        }

        private void SyncSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["SyncMove"] = !_settings["SyncMove"].AsBool();
                Chat.WriteLine($"Sync move : {_settings["SyncMove"].AsBool()}");
            }
        }


        private void AutoSitSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["AutoSit"] = !_settings["AutoSit"].AsBool();
                Chat.WriteLine($"Auto sit : {_settings["AutoSit"].AsBool()}");
            }
        }

        private void LeadFollowSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["Follow"] = !_settings["Follow"].AsBool();
                Chat.WriteLine($"Lead follow : {_settings["Follow"].AsBool()}");
            }
        }

        private void YalmCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.Yalms))
            {
                CancelBuffs(RelevantNanos.Yalms);
                IPCChannel.Broadcast(new YalmOffMessage());
            }
            else if (Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.WeaponPage).Any())
            {
                Item yalm = Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.WeaponPage).FirstOrDefault();

                if (yalm != null)
                {
                    yalm.MoveToInventory();

                    IPCChannel.Broadcast(new YalmOffMessage());
                }
            }
            else if (Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.Inventory).Any())
            {
                Item yalm = Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.Inventory).FirstOrDefault();

                if (yalm != null)
                {
                    yalm.Equip(EquipSlot.Weap_Hud1);

                    IPCChannel.Broadcast(new YalmUseMessage()
                    {
                        Item = yalm.HighId
                    });
                }
            }
            else 
            {
                Spell yalmbuff = Spell.List.FirstOrDefault(x => RelevantNanos.Yalms.Contains(x.Identity.Instance));

                if (yalmbuff != null)
                {
                    yalmbuff.Cast(false);

                    IPCChannel.Broadcast(new YalmOnMessage()
                    {
                        Spell = yalmbuff.Identity.Instance
                    });
                }
            }
        }

        private void DisbandCommand(string command, string[] param, ChatWindow chatWindow)
        {
            Team.Disband();
            IPCChannel.Broadcast(new DisbandMessage());
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
            IPCChannel.Broadcast(new DisbandMessage());
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

        private void DocTarget(string command, string[] param, ChatWindow chatWindow)
        {
            SimpleChar doctor = DynelManager.Characters
                .Where(c => c.IsAlive)
                .Where(c => c.Profession == Profession.Doctor)
                .Where(c => c.IsPlayer)
                .Where(c => !Team.Members.Contains(c.Identity))
                .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f)
                .FirstOrDefault();

            if (doctor != null)
                Targeting.SetTarget(doctor);
        }

        private void Rebuff(string command, string[] param, ChatWindow chatWindow)
        {
            CancelAllBuffs();
            IPCChannel.Broadcast(new ClearBuffsMessage());
        }

        //private void HelpCommand(string command, string[] param, ChatWindow chatWindow)
        //{
        //    string help = "For team commands;\n" +
        //                    "\n" +
        //                    "/form and /form raid\n" +
        //                    "\n" +
        //                    "/disband\n" +
        //                    "\n" +
        //                    "/convert to convert to raid (must be done from leader)\n" +
        //                    "\n" +
        //                    "\n" +
        //                    "For shortcuts to /aosharp settings;\n" +
        //                    "\n" +
        //                    "/syncchat syncs chat from current player to all\n" +
        //                    "\n" +
        //                    "/synctrade syncs trade from current player to all\n" +
        //                    "\n" +
        //                    "/syncuse for syncing items from current player to all\n" +
        //                    "\n" +
        //                    "/sync for syncing trade from current player to all\n" +
        //                    "\n" +
        //                    "/autosit auto sits to use kits\n" +
        //                    "\n" +
        //                    "/allfollow name then /allfollow to toggle\n" +
        //                    "\n" +
        //                    "/yalm all will use yalm then /yalm to toggle\n" +
        //                    "\n" +
        //                    "/rebuff to clear buffs\n" +
        //                    "\n" +
        //                    "/navfollow name then /navfollow to toggle\n" +
        //                    "(Follow the npc or player using waypoints)\n" +
        //                    "\n" +
        //                    "/assistplayer name then /assistplayer to toggle\n" +
        //                    "(This is implemented to avoid KSing)\n" +
        //                    "\n" +
        //                    "Add clear to the end of each of these to clear the name\n" +
        //                    "\n" +
        //                    "\n" +
        //                    "For IPC Channel;\n" +
        //                    "\n" +
        //                    "/mbchannel # or /mbchannelall #\n" +
        //                    $"Currently: {Config.IPCChannel}";

        //    Chat.WriteLine(help, ChatColor.LightBlue);
        //}

        private void OSFollow(Dynel dynel)
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

        private void ListenerPetSit()
        {
            healpet = DynelManager.LocalPlayer.Pets.Where(x => x.Type == PetType.Heal).FirstOrDefault();

            Item kit = Inventory.Items.Where(x => RelevantItems.Kits.Contains(x.LowId)).FirstOrDefault();

            if (healpet == null || kit == null) { return; }

            if (_settings["AutoSit"].AsBool())
            {
                if (CanUseSitKit() && Time.NormalTime > _sitPetUsedTimer + 16
                    && DynelManager.LocalPlayer.DistanceFrom(healpet.Character) < 10f && healpet.Character.IsInLineOfSight)
                {
                    if (healpet.Character.Nano == 10) { return; }

                    if (healpet.Character.Nano / PetMaxNanoPool() * 100 > 55) { return; }

                    MovementController.Instance.SetMovement(MovementAction.SwitchToSit);

                    if (DynelManager.LocalPlayer.MovementState == MovementState.Sit)
                    {
                        kit.Use(healpet.Character, true);
                        Task.Factory.StartNew(
                            async () =>
                            {
                                await Task.Delay(100);
                                MovementController.Instance.SetMovement(MovementAction.LeaveSit);
                            });
                        _sitPetUsedTimer = Time.NormalTime;
                    }
                }
            }
        }

        private void ListenerSit()
        {
            Spell spell = Spell.List.FirstOrDefault(x => x.IsReady);

            Item kit = Inventory.Items.Where(x => RelevantItems.Kits.Contains(x.LowId)).FirstOrDefault();

            if (kit == null) { return; }

            if (spell != null && _settings["AutoSit"].AsBool())
            {
                if (!DynelManager.LocalPlayer.Buffs.Contains(280488) && CanUseSitKit())
                {
                    if (spell != null && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Treatment) && Sitting == false
                        && DynelManager.LocalPlayer.MovementState != MovementState.Sit)
                    {
                        if (DynelManager.LocalPlayer.NanoPercent < 66 || DynelManager.LocalPlayer.HealthPercent < 66)
                        {
                            Task.Factory.StartNew(
                               async () =>
                               {
                                   Sitting = true;
                                   await Task.Delay(400);
                                   MovementController.Instance.SetMovement(MovementAction.SwitchToSit);
                                   await Task.Delay(800);
                                   MovementController.Instance.SetMovement(MovementAction.LeaveSit);
                                   await Task.Delay(200);
                                   Sitting = false;
                               });
                        }
                    }
                }
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

        private bool IsRaidEnabled(string[] param)
        {
            return param.Length > 0 && "raid".Equals(param[0]);
        }

        private Identity[] GetRegisteredCharactersInvite()
        {
            Identity[] registeredCharacters = SettingsController.GetRegisteredCharacters();
            int firstTeamCount = registeredCharacters.Length > 6 ? 6 : registeredCharacters.Length;
            Identity[] firstTeamCharacters = new Identity[firstTeamCount];
            Array.Copy(registeredCharacters, firstTeamCharacters, firstTeamCount);
            return firstTeamCharacters;
        }

        private Identity[] GetRemainingRegisteredCharacters()
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
            if (SettingsController.IsCharacterRegistered(e.Requester))
            {
                e.Accept();
            }
        }
        public static void CancelAllBuffs()
        {
            //foreach (Buff buff in DynelManager.LocalPlayer.Buffs.Where(x => !RelevantNanos.DontRemoveNanos.Contains(x.Identity.Instance)))
            //{
            //    buff.Remove();
            //}

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
                if (buffsToCancel.Contains(buff.Identity.Instance))
                    buff.Remove();
            }
        }

        private void AssistView(object s, ButtonBase button)
        {
            if (followWindow != null && followWindow.IsValid)
            {
                if (!followWindow.Views.Contains(SettingsController.assistView))
                {
                    SettingsController.assistView = View.CreateFromXml(PluginDirectory + "\\UI\\HelperAssistView.xml");

                    if (SettingsController.HelperAssistPlayer != String.Empty)
                    {
                        SettingsController.assistView.FindChild("AssistNamedCharacter", out TextInputView textinput);

                        if (textinput != null)
                            textinput.Text = SettingsController.HelperAssistPlayer;
                    }

                    followWindow.AppendTab("Assist", SettingsController.assistView);
                }
            }
            else
            {
                assistWindow = Window.CreateFromXml("Assist", PluginDirectory + "\\UI\\HelperAssistView.xml",
                        windowSize: new Rect(0, 0, 220, 345),
                        windowStyle: WindowStyle.Default,
                        windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                if (SettingsController.HelperFollowPlayer != String.Empty)
                {
                    assistWindow.FindView("FollowNamedCharacter", out TextInputView textinput);

                    if (textinput != null)
                        textinput.Text = SettingsController.HelperFollowPlayer;
                }

                if (SettingsController.HelperNavFollowPlayer != String.Empty)
                {
                    assistWindow.FindView("FollowNamedIdentity", out TextInputView textinput);

                    if (textinput != null)
                        textinput.Text = SettingsController.HelperNavFollowPlayer;
                }

                if (SettingsController.HelperNavFollowDistance.ToString() != String.Empty)
                {
                    assistWindow.FindView("NavFollowDistanceBox", out TextInputView textinput);

                    if (textinput != null)
                        textinput.Text = SettingsController.HelperNavFollowDistance.ToString();
                }

                if (SettingsController.HelperAssistPlayer != String.Empty)
                {
                    assistWindow.FindView("AssistNamedCharacter", out TextInputView textinput);

                    if (textinput != null)
                        textinput.Text = SettingsController.HelperAssistPlayer;
                }

                assistWindow.Show(true);
            }
        }

        private void FollowView(object s, ButtonBase button)
        {
            if (assistWindow != null && assistWindow.IsValid)
            {
                if (!assistWindow.Views.Contains(SettingsController.followView))
                {
                    SettingsController.followView = View.CreateFromXml(PluginDirectory + "\\UI\\HelperFollowView.xml");

                    if (SettingsController.HelperFollowPlayer != String.Empty)
                    {
                        SettingsController.followView.FindChild("FollowNamedCharacter", out TextInputView textinput);

                        if (textinput != null)
                            textinput.Text = SettingsController.HelperFollowPlayer;
                    }

                    if (SettingsController.HelperNavFollowPlayer != String.Empty)
                    {
                        SettingsController.followView.FindChild("FollowNamedIdentity", out TextInputView textinput);

                        if (textinput != null)
                            textinput.Text = SettingsController.HelperNavFollowPlayer;
                    }

                    if (SettingsController.HelperNavFollowDistance.ToString() != String.Empty)
                    {
                        SettingsController.followView.FindChild("NavFollowDistanceBox", out TextInputView textinput);

                        if (textinput != null)
                            textinput.Text = SettingsController.HelperNavFollowDistance.ToString();
                    }

                    assistWindow.AppendTab("Follow", SettingsController.followView);
                }
            }
            else
            {
                followWindow = Window.CreateFromXml("Follow", PluginDirectory + "\\UI\\HelperFollowView.xml",
                        windowSize: new Rect(0, 0, 220, 345),
                        windowStyle: WindowStyle.Default,
                        windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                if (SettingsController.HelperAssistPlayer != String.Empty)
                {
                    followWindow.FindView("AssistNamedCharacter", out TextInputView textinput);

                    if (textinput != null)
                        textinput.Text = SettingsController.HelperAssistPlayer;
                }

                if (SettingsController.HelperFollowPlayer != String.Empty)
                {
                    followWindow.FindView("FollowNamedCharacter", out TextInputView textinput);

                    if (textinput != null)
                        textinput.Text = SettingsController.HelperFollowPlayer;
                }

                if (SettingsController.HelperNavFollowPlayer != String.Empty)
                {
                    followWindow.FindView("FollowNamedIdentity", out TextInputView textinput);

                    if (textinput != null)
                        textinput.Text = SettingsController.HelperNavFollowPlayer;
                }

                if (SettingsController.HelperNavFollowDistance.ToString() != String.Empty)
                {
                    followWindow.FindView("NavFollowDistanceBox", out TextInputView textinput);

                    if (textinput != null)
                        textinput.Text = SettingsController.HelperNavFollowDistance.ToString();
                }

                followWindow.Show(true);
            }
        }

        private void InfoView(object s, ButtonBase button)
        {
            infoWindow = Window.CreateFromXml("Info", PluginDirectory + "\\UI\\HelperInfoView.xml",
                windowSize: new Rect(0, 0, 440, 510),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            infoWindow.Show(true);
        }

        private bool IsActiveCharacter()
        {
            return IsActiveWindow;
        }

        private bool BeingAttacked()
        {
            if (Team.IsInTeam)
            {
                return DynelManager.Characters
                    .Any(c => c.FightingTarget != null
                        && Team.Members.Select(m => m.Name).Contains(c.FightingTarget.Name));
            }

            return DynelManager.Characters
                    .Any(c => c.FightingTarget != null
                        && c.FightingTarget.Name == DynelManager.LocalPlayer.Name);

            //if (target == null) { return false; }

            //if (Team.IsInTeam)
            //{
            //    if (Team.Members.Where(c => c.Character != null).Where(c => target.FightingTarget.Name == c.Character.Name).Any() || target.FightingTarget.DistanceFrom(DynelManager.LocalPlayer) < 6f)
            //        return true;
            //    if (target.FightingTarget.IsPet && Team.Members.Where(c => c.Character != null).Where(c => c.Name == target.FightingTarget.Name).Any())
            //        return true;

            //        return false;
            //}
            //else
            //{
            //    if (target.FightingTarget.Identity == DynelManager.LocalPlayer.Identity || target.FightingTarget.DistanceFrom(DynelManager.LocalPlayer) < 6f)
            //        return true;
            //    if (DynelManager.LocalPlayer.Pets.Where(c => target.FightingTarget.Name == c.Character.Name).Any())
            //        return true;

            //        return false;
            //}
        }

        private bool CanUseSitKit()
        {
            List<Item> sitKits = Inventory.FindAll("Health and Nano Recharger").Where(c => c.LowId != 297274).ToList();

            if (Inventory.Find(297274, out Item premSitKit))
            {
                if (DynelManager.LocalPlayer.IsAlive && !BeingAttacked() && DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0
                    && !Team.IsInCombat() && DynelManager.LocalPlayer.FightingTarget == null
                    && !DynelManager.LocalPlayer.IsMoving && !Game.IsZoning) { return true; }
            }

            if (!sitKits.Any()) { return false; }

            if (DynelManager.LocalPlayer.IsAlive && !BeingAttacked() && DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0
                    && !Team.IsInCombat() && DynelManager.LocalPlayer.FightingTarget == null
                    && !DynelManager.LocalPlayer.IsMoving && !Game.IsZoning)
            {
                foreach (Item sitKit in sitKits.OrderBy(x => x.QualityLevel))
                {
                    int skillReq = (sitKit.QualityLevel > 200 ? (sitKit.QualityLevel % 200 * 3) + 1501 : (int)(sitKit.QualityLevel * 7.5f));

                    if (DynelManager.LocalPlayer.GetStat(Stat.FirstAid) >= skillReq || DynelManager.LocalPlayer.GetStat(Stat.Treatment) >= skillReq)
                        return true;
                }
            }

            return false;
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

        public static bool IsOther(Item item)
        {
            return item.LowId == 305476 || item.LowId == 204698 || item.LowId == 156576 || item.LowId == 267168 || item.LowId == 267167
                || item.Name.Contains("Health");
        }

        private float PetMaxNanoPool()
        {
            if (healpet.Character.Level == 215)
                return 5803;
            else if (healpet.Character.Level == 192)
                return 13310;
            else if (healpet.Character.Level == 169)
                return 11231;
            else if (healpet.Character.Level == 146)
                return 9153;
            else if (healpet.Character.Level == 123)
                return 7169;
            else if (healpet.Character.Level == 99)
                return 5327;
            else if (healpet.Character.Level == 77)
                return 3807;
            else if (healpet.Character.Level == 55)
                return 2404;
            else if (healpet.Character.Level == 33)
                return 1234;
            else if (healpet.Character.Level == 14)
                return 414;

            return 0;
        }

        public enum FollowSelection
        {
            None, LeadFollow, OSFollow, NavFollow
        }

        public enum AttackSelection
        {
            None, Sync, Assist
        }

        private static class RelevantNanos
        {
            public static readonly int[] ZixMorph = { 288532, 302212 };
            public static readonly int[] Yalms = {
                290473, 281569, 301672, 270984, 270991, 273468, 288795, 270993, 270995, 270986, 270982,
                296034, 296669, 304437, 270884, 270941, 270836, 287285, 288816, 270943, 270939, 270945,
                270711, 270731, 270645, 284061, 288802, 270764, 277426, 288799, 270738, 270779, 293619,
                294781, 301669, 301700, 301670, 120499, 82835
            };
            //public static readonly int[] DontRemoveNanos = {};
        }

        private static class RelevantItems
        {
            public static readonly int[] Kits = {
                297274, 293296, 291084, 291083, 291082
            };
        }
    }
}
