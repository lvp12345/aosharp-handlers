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
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Windows.Interop;

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
            _settings.AddVariable("NPCTrade", false);
            _settings.AddVariable("SyncTrade", false);

            _settings["Enable"] = true;

            IPCChannel.RegisterCallback((int)IPCOpcode.StartStop, OnStartStopMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Move, OnMoveMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Attack, OnAttackMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Target, Lookat);
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
                    
                    _settings["Enable"] = true;
                    Start();
                }
                else
                {
                    
                    _settings["Enable"] = false;
                    Stop();
                }
            }
        }

        private void Start()
        {
            Enable = true;

        }

        private void Stop()
        {
            Enable = false;

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

                if (n3Msg.N3MessageType == N3MessageType.LookAt)
                {
                    LookAtMessage lookAtMsg = (LookAtMessage)n3Msg;

                    IPCChannel.Broadcast(new TargetMessage()
                    {
                        Target = lookAtMsg.Target

                        
                    }) ;
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

                    if (genericCmdMsg.Action == GenericCmdAction.Use)
                    {
                        if (genericCmdMsg.Target.Type == IdentityType.Terminal)
                        {
                            UseMessage useMsg = new UseMessage()
                            {
                                Target = genericCmdMsg.Target,
                                PfId = Playfield.ModelIdentity.Instance
                            };

                            IPCChannel.Broadcast(useMsg);
                        }
                        else
                        {
                            BroadcastUsableMessage(FindItem(genericCmdMsg.Target), Identity.None);
                        }
                    }
                    else if (genericCmdMsg.Action == GenericCmdAction.UseItemOnItem)
                    {
                        Item item = FindItem(genericCmdMsg.Source.Value);

                        RingName ringName = GetRingNameFromItemName(item?.Name);

                        if (ringName != RingName.Unknown)
                        {
                            UseMessage useMsg = new UseMessage()
                            {
                                Target = genericCmdMsg.Target,
                                RingName = ringName
                            };

                            IPCChannel.Broadcast(useMsg);
                        }
                        else
                        {
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

                if (_settings["NPCTrade"].AsBool())
                {
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

                }
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

            if (!IsOther(item))
            {
                UseMessage usableMsg = new UseMessage()
                {
                    ItemId = item.Id,
                    ItemHighId = item.HighId,
                    Target = target,
                };
                IPCChannel.Broadcast(usableMsg);
            }
            
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

        private void Lookat(int sender, IPCMessage look)
        {
            TargetMessage targetMsg = (TargetMessage)look;

            var localPlayer = DynelManager.LocalPlayer;

            if (!localPlayer.IsAttacking && !localPlayer.IsAttackPending
                && localPlayer.FightingTarget == null
                && Spell.List.Any(spell => spell.IsReady) && !Spell.HasPendingCast)
            {
                Targeting.SetTarget(targetMsg.Target);
            }
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

        private void OnUseMessage(int sender, IPCMessage msg)
        {
            if (IsActiveWindow || Game.IsZoning) { return; }

            UseMessage useMsg = (UseMessage)msg;

            int[] ignoredItemIds = { 291043, 204103, 204104, 204105, 204106, 204107, 303138, 303141, 303137, 204698, 204653, 206013, 267168, 267167, 305476, 305478, 303179 };

            if (useMsg.RingName != RingName.Unknown)
            {
                string ringName = GetItemNameFromRingName(useMsg.RingName);

                if (ringName != null)
                {
                    FindUseRing(ringName, useMsg.Target);
                }
            }
            else
            {
                if (useMsg.Target.Type == IdentityType.Terminal)
                {
                    useDynel = useMsg.Target;
                }
                else
                {
                    ProcessIDItem(useMsg);
                }
            }
        }

        private void FindUseRing(string itemName, Identity target)
        {
            Item ring = Inventory.Items.FirstOrDefault(c => c.Name.Contains(itemName)) ??
                             Inventory.Backpacks.SelectMany(b => b.Items).FirstOrDefault(c => c.Name.Contains(itemName));

            if (ring != null)
            {
                useItem = new Identity(IdentityType.Inventory, ring.Slot.Instance);
                useOnDynel = target;
            }
        }

        private void ProcessIDItem(UseMessage usableMsg)
        {
            Item itemID = Inventory.Items.FirstOrDefault(i => i.Id == usableMsg.ItemId || i.HighId == usableMsg.ItemHighId) ??
                             Inventory.Backpacks.SelectMany(b => b.Items)
                                                .FirstOrDefault(i => i.Id == usableMsg.ItemId || i.HighId == usableMsg.ItemHighId);

            if (itemID != null)
            {
                if (usableMsg.Target == Identity.None)
                {
                    itemID.Use();
                }
                else
                {
                    useItem = new Identity(IdentityType.Inventory, itemID.Slot.Instance);
                    useOnDynel = usableMsg.Target;
                }
            }
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
                    Unknown1 = 2,
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
                || item.Id == 204593 || item.Id == 305492 || item.Id == 204595 || item.Id == 305491 || item.Id == 305478
                || item.Id == 206013 || item.Id == 204653 || item.Id == 204698 || item.Id == 206015 || item.Id == 305476
                || item.Id == 267168 || item.Id == 267167 || item.Name.Contains("Health") || item.Name.Contains("Newcomer");
        }

        #endregion
    }
}
