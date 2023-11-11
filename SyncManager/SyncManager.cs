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

        private Dictionary<RingName, string> _ringNameToItemNameMap;
        private Dictionary<string, RingName> _itemNameToRingNameMap;

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

            IPCChannel.RegisterCallback((int)IPCOpcode.StartStop, OnStartStopMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Move, OnMoveMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Attack, OnAttackMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Use, OnUseMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChat, OnNpcChatMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Trade, OnTradeMessage);

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

            _ringNameToItemNameMap = new Dictionary<RingName, string>
        {
            { RingName.PureNovictumRing, "Pure Novictum Ring" },
            { RingName.RimyRing, "Rimy Ring" },
            { RingName.AchromicRing, "Achromic Ring" },
            { RingName.SanguineRing, "Sanguine Ring" },
            { RingName.CaliginousRing, "Caliginous Ring" }
        };

            _itemNameToRingNameMap = _ringNameToItemNameMap.ToDictionary(pair => pair.Value, pair => pair.Key);

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

                if (_settings["SyncUse"].AsBool() && n3Msg.N3MessageType == N3MessageType.GenericCmd)
                {
                    GenericCmdMessage genericCmdMsg = (GenericCmdMessage)n3Msg;

                    // Log the received GenericCmdMessage action
                    Chat.WriteLine($"Received GenericCmdMessage:");
                    Chat.WriteLine($" Temp1: {genericCmdMsg.Temp1}");
                    Chat.WriteLine($" Count: {genericCmdMsg.Count}");
                    Chat.WriteLine($" Action: {genericCmdMsg.Action}");
                    Chat.WriteLine($" Temp4: {genericCmdMsg.Temp4}");
                    Chat.WriteLine($" User: {genericCmdMsg.User}");
                    Chat.WriteLine($" Source: {genericCmdMsg.Source}");
                    Chat.WriteLine($" Target: {genericCmdMsg.Target}");


                    if (genericCmdMsg.Action == GenericCmdAction.Use && genericCmdMsg.Target.Type == IdentityType.Terminal)
                    {
                        UseMessage useMsg = new UseMessage()
                        {
                            Target = genericCmdMsg.Target,
                            PfId = Playfield.ModelIdentity.Instance
                        };

                        // Log the Use action details
                        Chat.WriteLine($"Action: Use (Terminal). Sending UseMessage: Target={useMsg.Target}, PfId={useMsg.PfId}");

                        IPCChannel.Broadcast(useMsg);
                    }
                    else if (genericCmdMsg.Action == GenericCmdAction.Use && genericCmdMsg.Target == Identity.None)
                    {
                        // Log the Use action with non-terminal target
                        Chat.WriteLine($"Action: Use (Non-Terminal). Finding item for Target={genericCmdMsg.Target}");

                        BroadcastUsableMessage(FindItem(genericCmdMsg.Target), Identity.None);
                    }
                    else if (genericCmdMsg.Action == GenericCmdAction.UseItemOnItem)
                    {
                        Item item = FindItem(genericCmdMsg.Source.Value);

                        // Log item details
                        Chat.WriteLine($"Action: Use Item on Item. Item found: {item?.Name ?? "null"}");

                        RingName ringName = GetRingNameFromItemName(item?.Name);

                        // Log the ring name if it's not unknown
                        if (ringName != RingName.Unknown)
                        {
                            UseMessage useMsg = new UseMessage()
                            {
                                Target = genericCmdMsg.Target,
                                RingName = ringName
                            };

                            // Log the Use Item on Item action with ring name
                            Chat.WriteLine($"Using Ring Item. RingName={ringName}, Target={useMsg.Target}");

                            IPCChannel.Broadcast(useMsg);
                        }
                        else
                        {
                            // Log the fallback case for Use Item on Item
                            Chat.WriteLine($"Fallback for Use Item on Item. Source={genericCmdMsg.Source.Value}, Target={genericCmdMsg.Target}");
                            BroadcastUsableMessage(FindItem(genericCmdMsg.Source.Value), genericCmdMsg.Target);
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

        private Item FindItem(Identity target)
        {
            return Inventory.Find(target, out Item item) ? item :
                   Inventory.Backpacks
                            .SelectMany(b => b.Items)
                            .FirstOrDefault(i => i.Slot.Instance == target.Instance);
        }

        private void BroadcastUsableMessage(Item item, Identity target)
        {
            if (item == null) return;

            UseMessage usableMsg = new UseMessage()
            {
                ItemId = item.Id,
                ItemHighId = item.HighId,
                Target = target,
            };
            Chat.WriteLine($"Sending UseMessage: ItemId={usableMsg.ItemId}, ItemHighId={usableMsg.ItemHighId}, Target={usableMsg.Target}");
            IPCChannel.Broadcast(usableMsg);
        }

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

        #region IncomingCommunication

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
            if (IsActiveWindow || Game.IsZoning) return;

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

        private void OnUseMessage(int sender, IPCMessage msg)
        {
            if (IsActiveWindow || Game.IsZoning) { return; }

            UseMessage useMsg = (UseMessage)msg;

            Chat.WriteLine($"Received UseMessage: ItemId={useMsg.ItemId}, ItemHighId={useMsg.ItemHighId}, Target={useMsg.Target}, Ring enum={useMsg.RingName}, Playfield = {useMsg.PfId}");

            int[] ignoredItemIds = { 291043, 204103, 204104, 204105, 204106, 204107, 303138, 303141, 303137, 204698, 204653, 206013, 267168, 267167, 305476, 305478, 303179 };

            if (useMsg.RingName != RingName.Unknown)
            {
                string ringName = GetItemNameFromRingName(useMsg.RingName);

                if (ringName != null)
                {
                    TryUseItem(ringName, useMsg.Target);
                }
            }
            else if (useMsg.Target.Type == IdentityType.Terminal)
            {
                useDynel = useMsg.Target;
                Chat.WriteLine($"UseMessage: Use Terminal {useMsg.Target}");
            }
            else
            {
                Chat.WriteLine("Processing a non-ring item.");
                ProcessNonRingItem(useMsg);
            }
        }

        private void TryUseItem(string itemName, Identity target)
        {
            Item itemToUse = Inventory.Items.FirstOrDefault(c => c.Name.Contains(itemName)) ??
                             Inventory.Backpacks.SelectMany(b => b.Items).FirstOrDefault(c => c.Name.Contains(itemName));

            if (itemToUse != null)
            {
                
                useItem = new Identity(IdentityType.Inventory, itemToUse.Slot.Instance);
                useOnDynel = target;
            }
        }

        private void ProcessNonRingItem(UseMessage usableMsg)
        {
            // Try to find the item in the inventory.
            if (Inventory.Find(usableMsg.ItemId, usableMsg.ItemHighId, out Item item))
            {
                SendUseCommand(item.Slot, usableMsg.Target);
            }
            else
            {
                Chat.WriteLine($"Item with ID: {usableMsg.ItemId} or High ID: {usableMsg.ItemHighId} not found in main inventory. Checking backpacks.");
                bool foundInBackpack = false;

                // Search through each backpack for the item.
                foreach (Backpack bag in Inventory.Backpacks)
                {
                    Item _bagItem = bag.Items.FirstOrDefault(c => c.HighId == usableMsg.ItemHighId);
                    if (_bagItem != null)
                    {
                        Chat.WriteLine($"Item found in backpack: {_bagItem.Name} (Slot: {_bagItem.Slot}). Sending use command.");
                        SendUseCommand(_bagItem.Slot, usableMsg.Target);
                        foundInBackpack = true;
                        break; // Item found, no need to check further backpacks.
                    }
                }

                // If the item was not found in any backpack, log that the item is missing.
                if (!foundInBackpack)
                {
                    Chat.WriteLine($"Item with High ID: {usableMsg.ItemHighId} not found in any backpacks.");
                }
            }
        }

        //private void SendUseCommand(Identity slot, Identity target)
        //{
        //    GenericCmdAction action = target == Identity.None ? GenericCmdAction.Use : GenericCmdAction.UseItemOnItem;

        //    Chat.WriteLine($"Preparing to Send Command:");
        //    Chat.WriteLine($" Action: {action}");
        //    Chat.WriteLine($" User: {DynelManager.LocalPlayer.Identity}");
        //    Chat.WriteLine($" Source: {slot}");
        //    Chat.WriteLine($" Target: {target}");

        //    GenericCmdMessage message = new GenericCmdMessage()
        //    {
        //        Unknown = 1,
        //        User = DynelManager.LocalPlayer.Identity,
        //        Source = slot,
        //        Action = action,
        //        Target = target 
        //    };

        //    Chat.WriteLine($"Sending Command:");
        //    Chat.WriteLine($" Action: {message.Action}");
        //    Chat.WriteLine($" User: {message.User}");
        //    Chat.WriteLine($" Source: {message.Source}");
        //    Chat.WriteLine($" Target: {message.Target}");


        //    // Send the message
        //    Network.Send(message);

        //    GenericCmdMessage received = new GenericCmdMessage();

        //    Chat.WriteLine($"Received GenericCmdMessage:");
        //    Chat.WriteLine($" Temp1: {received.Temp1}");
        //    Chat.WriteLine($" Count: {received.Count}");
        //    Chat.WriteLine($" Action: {received.Action}");
        //    Chat.WriteLine($" Temp4: {received.Temp4}");
        //    Chat.WriteLine($" User: {received.User}");
        //    Chat.WriteLine($" Source: {received.Source}");
        //    Chat.WriteLine($" Target: {received.Target}");
        //}



        private void SendUseCommand(Identity slot, Identity target)
        {
            Chat.WriteLine($"Preparing to send command: Use for Slot: {slot}, Target: {target}");

            GenericCmdMessage message = new GenericCmdMessage()
            {
                Unknown = 1,
                Action = GenericCmdAction.Use,
                User = DynelManager.LocalPlayer.Identity,
                Target = new Identity(IdentityType.Inventory, slot.Instance)
            };

            Chat.WriteLine($"Action: {message.Action}");

            // Send the command.
            Network.Send(message);

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

        public enum RingName
        {
            Unknown = 0,
            PureNovictumRing,
            RimyRing,
            AchromicRing,
            SanguineRing,
            CaliginousRing
        }

        public RingName GetRingNameFromItemName(string itemName)
        {
            foreach (var pair in _itemNameToRingNameMap)
            {
                if (itemName.Contains(pair.Key))
                {
                    return pair.Value;
                }
            }

            return RingName.Unknown;
        }

        public string GetItemNameFromRingName(RingName ringName)
        {
            if (_ringNameToItemNameMap.TryGetValue(ringName, out var itemName))
            {
                return itemName;
            }

            return null;
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
            return item.Id == 305476 || item.Id == 204698 || item.Id == 156576 || item.Id == 267168 || item.Id == 267167
                || item.Name.Contains("Health");
        }

        #endregion
    }
}
