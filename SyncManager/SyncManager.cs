using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SyncManager.IPCMessages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;

namespace SyncManager
{
    public class SyncManager : AOPluginEntry
    {
        private static IPCChannel IPCChannel;

        public static Config Config { get; private set; }

        protected Settings _settings;

        private static Identity useDynel;
        private static Identity useOnDynel;
        private static Identity useItem;

        private static Item _bagItem;

        public static bool _openBags = false;
        private static bool _init = false;
        public static bool Enable = false;

        private static double _useTimer;

        public static Window _infoWindow;

        public static string PluginDir;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        private bool IsActiveWindow => GetForegroundWindow() == Process.GetCurrentProcess().MainWindowHandle;

        public override void Run(string pluginDir)
        {
            _settings = new Settings("SyncManager");
            PluginDir = pluginDir;

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\SyncManager\\{DynelManager.LocalPlayer.Name}\\Config.json");
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel));

            Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannelChangedEvent += IPCChannel_Changed;
            Game.OnUpdate += OnUpdate;
            Network.N3MessageSent += Network_N3MessageSent;
            Network.N3MessageReceived += Network_N3MessageReceived;
            Game.TeleportEnded += OnZoned;

            _settings.AddVariable("Enable", true);
            _settings.AddVariable("SyncMove", false);
            _settings.AddVariable("SyncBags", false);
            _settings.AddVariable("SyncUse", true);
            _settings.AddVariable("SyncChat", false);
            _settings.AddVariable("SyncTrade", false);

            _settings["Enable"] = true;

            IPCChannel.RegisterCallback((int)IPCOpcode.StartStop, OnStartStopMessage); ;

            IPCChannel.RegisterCallback((int)IPCOpcode.Attack, OnAttackMessage);

            IPCChannel.RegisterCallback((int)IPCOpcode.Move, OnMoveMessage);

            IPCChannel.RegisterCallback((int)IPCOpcode.Trade, OnTradeMessage);

            IPCChannel.RegisterCallback((int)IPCOpcode.Use, OnUseMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.UseItem, OnUseItemMessage);

            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChat, OnNpcChatMessage);

            RegisterSettingsWindow("Sync Manager", "SyncManagerSettingWindow.xml");

            Chat.RegisterCommand("sync", SyncManagerCommand);
            Chat.RegisterCommand("syncmove", SyncSwitch);
            Chat.RegisterCommand("syncbags", SyncBagsSwitch);
            Chat.RegisterCommand("syncuse", SyncUseSwitch);
            Chat.RegisterCommand("syncchat", SyncChatSwitch);
            Chat.RegisterCommand("synctrade", SyncTradeSwitch);


            Chat.WriteLine("SyncManager Loaded!");
            Chat.WriteLine("/syncmanager for settings.");
        }

        private void OnUpdate(object s, float deltaTime)
        {
            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView channelInput);

                if (channelInput != null && !string.IsNullOrEmpty(channelInput.Text))
                {
                    if (int.TryParse(channelInput.Text, out int channelValue)
                        && Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel != channelValue)
                    {
                        Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel = channelValue;
                    }
                }

                if (SettingsController.settingsWindow.FindView("SyncManagerInfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = HandleInfoViewClick;
                }
            }

            if (!_openBags && _settings["SyncBags"].AsBool())
            {
                Inventory.Backpacks.ForEach(b =>
                {
                    if (!b.IsOpen)
                    {
                        Item.Use(b.Slot);
                        Item.Use(b.Slot);
                    }
                });
                if (Inventory.Backpacks.All(b => b.IsOpen))
                {
                    _openBags = true;
                }
            }

            if (Time.NormalTime > _useTimer + 0.1)
            {
                if (!IsActiveWindow)
                {
                    ListenerUseSync();
                }
                _useTimer = Time.NormalTime;
            }

            if (!_settings["Enable"].AsBool() && Enable)
            {
                IPCChannel.Broadcast(new StartStopIPCMessage() { IsStarting = false });
                Stop();
            }
            if (_settings["Enable"].AsBool() && !Enable)
            {
                IPCChannel.Broadcast(new StartStopIPCMessage() { IsStarting = true });
                Start();
            }
        }

        #region Callbacks

        private void OnStartStopMessage(int sender, IPCMessage msg)
        {
            if (msg is StartStopIPCMessage startStopMessage)
            {
                if (startStopMessage.IsStarting)
                {
                    // Update the setting and start the process.
                    _settings["Enable"] = true;
                    Start();
                }
                else
                {
                    // Update the setting and stop the process.
                    _settings["Enable"] = false;
                    Stop();
                }
            }
        }

        private void Start()
        {
            Enable = true;

            //Chat.WriteLine("Syncmanager enabled.");
        }

        private void Stop()
        {
            Enable = false;

            //Chat.WriteLine("Syncmanager disabled.");
        }


        private void OnZoned(object s, EventArgs e)
        {

            if (!_settings["Enable"].AsBool())
                return;

            if (_settings["SyncBags"].AsBool())
            {
                _openBags = false;
            }
        }



        #region OutgoingCommunication

        private void Network_N3MessageSent(object s, N3Message n3Msg)
        {
            if (!IsActiveCharacter() || n3Msg.Identity != DynelManager.LocalPlayer.Identity) { return; }

            if (_settings["Enable"].AsBool())
            {
                if (_settings["SyncMove"].AsBool())
                {
                    if (n3Msg.N3MessageType == N3MessageType.CharDCMove)
                    {
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
                }

                if (n3Msg.N3MessageType == N3MessageType.LookAt) // what is this for? what are we sending the target ?
                {
                    LookAtMessage lookAtMsg = (LookAtMessage)n3Msg;
                    IPCChannel.Broadcast(new TargetMessage()
                    {
                        Target = lookAtMsg.Target
                    });
                }

                //sync attack
                if (n3Msg.N3MessageType == N3MessageType.Attack)
                {
                    AttackMessage attackMsg = (AttackMessage)n3Msg;
                    IPCChannel.Broadcast(new AttackIPCMessage
                    {
                        Target = attackMsg.Target,
                        Start = true
                    });
                }
                if (n3Msg.N3MessageType == N3MessageType.StopFight)
                {
                    IPCChannel.Broadcast(new AttackIPCMessage
                    {
                        Start = false
                    });
                }

                //sync use
                if (_settings["SyncUse"].AsBool())
                {
                    if (n3Msg.N3MessageType == N3MessageType.GenericCmd)
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
                        else if (genericCmdMsg.Action == GenericCmdAction.Use)
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
                            if (Inventory.Find(genericCmdMsg.Source.Value, out Item item))
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
                                        .Where(c => c.Slot.Instance == genericCmdMsg.Source.Value.Instance)
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
                }

                if (_settings["SyncChat"].AsBool())
                {
                    if (n3Msg.N3MessageType == N3MessageType.KnubotOpenChatWindow)
                    {
                        KnuBotOpenChatWindowMessage n3OpenChatMessage = (KnuBotOpenChatWindowMessage)n3Msg;
                        IPCChannel.Broadcast(new NpcChatIPCMessage
                        {
                            Target = n3OpenChatMessage.Target,
                            OpenClose = true,
                            Answer = -1
                        });
                    }
                    if (n3Msg.N3MessageType == N3MessageType.KnubotAnswer)
                    {
                        KnuBotAnswerMessage n3AnswerMsg = (KnuBotAnswerMessage)n3Msg;
                        IPCChannel.Broadcast(new NpcChatIPCMessage
                        {
                            Target = n3AnswerMsg.Target,
                            OpenClose = true, 
                            Answer = n3AnswerMsg.Answer
                        });
                    }
                    if (n3Msg.N3MessageType == N3MessageType.KnubotStartTrade)
                    {
                        KnuBotStartTradeMessage startTradeMsg = (KnuBotStartTradeMessage)n3Msg;
                        IPCChannel.Broadcast(new NpcChatIPCMessage
                        {
                            Target = startTradeMsg.Target,
                            OpenClose = true,
                            IsStartTrade = true,
                            NumberOfItemSlotsInTradeWindow = startTradeMsg.NumberOfItemSlotsInTradeWindow
                        });
                    }
                    if (n3Msg.N3MessageType == N3MessageType.KnubotTrade)
                    {
                        KnuBotTradeMessage tradeMsg = (KnuBotTradeMessage)n3Msg;
                        IPCChannel.Broadcast(new NpcChatIPCMessage
                        {
                            Target = tradeMsg.Target,
                            OpenClose = true,
                            IsTrade = true,
                            Container = tradeMsg.Container
                        });
                    }
                    if (n3Msg.N3MessageType == N3MessageType.KnubotFinishTrade)
                    {
                        KnuBotFinishTradeMessage finishTradeMsg = (KnuBotFinishTradeMessage)n3Msg;
                        IPCChannel.Broadcast(new NpcChatIPCMessage
                        {
                            Target = finishTradeMsg.Target,
                            OpenClose = true,
                            IsFinishTrade = true,
                            Decline = finishTradeMsg.Decline,
                            Amount = finishTradeMsg.Amount
                        });

                    }
                    if (n3Msg.N3MessageType == N3MessageType.KnubotCloseChatWindow)
                    {
                        KnuBotCloseChatWindowMessage n3CloseChatMessage = (KnuBotCloseChatWindowMessage)n3Msg;
                        IPCChannel.Broadcast(new NpcChatIPCMessage
                        {
                            Target = n3CloseChatMessage.Target,
                            OpenClose = false,
                            Answer = -1
                        });
                    }
                }

            }
        }

        //sync trade
        private void OnTradeMessage(int sender, IPCMessage msg)
        {
            if (Game.IsZoning)
                return;

            if (!_settings["Enable"].AsBool() && _settings["SyncTrade"].AsBool()) return;

            TradeHandleMessage charTradeIpcMsg = (TradeHandleMessage)msg;

            if (charTradeIpcMsg.Action == TradeAction.Confirm)
            {
                Network.Send(new TradeMessage()
                {
                    Unknown1 = 2,
                    Action = (TradeAction)3,
                });
            }
            else if (charTradeIpcMsg.Action == TradeAction.Accept)
            {
                Network.Send(new TradeMessage()
                {
                    Unknown1 = 2,
                    Action = (TradeAction)1,
                });
            }
        }


        #endregion

        #region IncomingCommunication

        //sync trade
        private void Network_N3MessageReceived(object s, N3Message n3Msg)
        {
            if (!_settings["Enable"].AsBool() && _settings["SyncTrade"].AsBool()) return;

            if (n3Msg.N3MessageType == N3MessageType.Trade)
            {
                TradeMessage tradeMsg = (TradeMessage)n3Msg;

                if (tradeMsg.Action == TradeAction.Accept)
                {
                    Network.Send(new TradeMessage()
                    {
                        Unknown1 = 2,
                        Action = (TradeAction)3,
                    });
                }
                if (tradeMsg.Action == TradeAction.Confirm)
                {
                    Network.Send(new TradeMessage()
                    {
                        Unknown1 = 2,
                        Action = (TradeAction)1,
                    });
                }
            }
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

        private void OnAttackMessage(int sender, IPCMessage msg)
        {
            if (IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            var attackMsg = (AttackIPCMessage)msg;
            if (attackMsg.Start)
            {
                Dynel targetDynel = DynelManager.GetDynel(attackMsg.Target);
                DynelManager.LocalPlayer.Attack(targetDynel, true);
            }
            else
            {
                DynelManager.LocalPlayer.StopAttack();
            }
        }

        private void OnUseItemMessage(int sender, IPCMessage msg)
        {
            if (!_settings["SyncUse"].AsBool() || IsActiveWindow || Game.IsZoning) { return; }

            if (!_settings["Enable"].AsBool())
                return;

            UsableMessage usableMsg = (UsableMessage)msg;

            if (usableMsg.ItemLowId == 291043 || usableMsg.ItemLowId == 291043 || usableMsg.ItemLowId == 204103 || usableMsg.ItemLowId == 204104 ||
                usableMsg.ItemLowId == 204105 || usableMsg.ItemLowId == 204106 || usableMsg.ItemLowId == 204107 || usableMsg.ItemHighId == 204107 ||
                usableMsg.ItemLowId == 303138 || usableMsg.ItemLowId == 303141 || usableMsg.ItemLowId == 303137 || usableMsg.ItemHighId == 303136 ||
                usableMsg.ItemLowId == 204698 || usableMsg.ItemLowId == 204653 || usableMsg.ItemLowId == 206013 || usableMsg.ItemLowId == 267168 ||
                usableMsg.ItemLowId == 267167 || usableMsg.ItemLowId == 305476 || usableMsg.ItemLowId == 305478 || usableMsg.ItemLowId == 303179)
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

            if (useMsg.PfId != Playfield.ModelIdentity.Instance) { return; }

            useDynel = useMsg.Target;
        }

        private void OnNpcChatMessage(int sender, IPCMessage msg)
        {
            NpcChatIPCMessage chatMsg = (NpcChatIPCMessage)msg;

            if (chatMsg.OpenClose == true)
            {
                NpcDialog.Open(chatMsg.Target);
            }

            if (chatMsg.Answer != -1)
            {
                NpcDialog.SelectAnswer(chatMsg.Target, chatMsg.Answer);
            }

            if (chatMsg.OpenClose == false)
            {
                Network.Send(new KnuBotCloseChatWindowMessage
                {
                    Unknown1 = 2, // is always 2, need to look into closing the window
                    Unknown2 = 0,
                    Unknown3 = 0,
                    Target = chatMsg.Target
                });
            }

            if (chatMsg.IsStartTrade)
            {
                NpcDialog.OpenTrade(chatMsg.Target, chatMsg.NumberOfItemSlotsInTradeWindow);
            }

            if (chatMsg.IsTrade)
            {
                
            }

            if (chatMsg.IsFinishTrade)
            {
                NpcDialog.FinishTrade(chatMsg.Target, chatMsg.Amount);
            }
        }

        #endregion

        #endregion

        #region Settings
        public static void IPCChannel_Changed(object s, int e)
        {
            IPCChannel.SetChannelId(Convert.ToByte(e));

            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }
        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDir + "\\UI\\" + xmlName, _settings);
        }

        private void HandleInfoViewClick(object s, ButtonBase button)
        {
            _infoWindow = Window.CreateFromXml("Info", PluginDir + "\\UI\\SyncManagerInfoView.xml",
                windowSize: new Rect(0, 0, 310, 510),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            _infoWindow.Show(true);
        }

        #endregion

        #region Misc

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        private void SyncManagerCommand(string command, string[] param, ChatWindow chatWindow)
        {
            try
            {
                if (param.Length < 1)
                {
                    if (!_settings["Enable"].AsBool())
                    {
                        _settings["Enable"] = true;
                        IPCChannel.Broadcast(new StartStopIPCMessage() { IsStarting = true });
                        Start();
                    }
                    else
                    {
                        _settings["Enable"] = false;
                        IPCChannel.Broadcast(new StartStopIPCMessage() { IsStarting = false });
                        Stop();
                    }
                    return;
                }

                Config.Save();
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
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
        private void SyncBagsSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["SyncBags"] = !_settings["SyncBags"].AsBool();
                Chat.WriteLine($"Sync bags : {_settings["SyncBags"].AsBool()}");
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

        private bool IsActiveCharacter()
        {
            return IsActiveWindow;
        }

        public static bool IsOther(Item item)
        {
            return item.LowId == 305476 || item.LowId == 204698 || item.LowId == 156576 || item.LowId == 267168 || item.LowId == 267167
                || item.Name.Contains("Health");
        }

        #endregion
    }
}
