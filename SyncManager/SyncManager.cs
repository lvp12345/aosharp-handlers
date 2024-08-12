using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Common.Unmanaged.Imports;
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace SyncManager
{
    public class SyncManager : AOPluginEntry
    {
        static IPCChannel IPCChannel;

        public static Config Config { get; set; }

        protected Settings _settings;

        Item UseItem = null;
        Identity UseTarget = Identity.None;
        int UseType = 3;

        bool _openBags;
        bool Enable = false;

        double UseDelay;

        static Window _infoWindow;

        string PluginDir;

        Dictionary<RingName, string> _ringNameToItemNameMap;
        Dictionary<string, RingName> _itemNameToRingNameMap;
        Dictionary<int, int> invSlots = new Dictionary<int, int>();

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        bool IsActiveWindow => GetForegroundWindow() == Process.GetCurrentProcess().MainWindowHandle;

        List<int> NPCReceivedItem = new List<int>();

        [Obsolete]
        public override void Run(string pluginDir)
        {
            _settings = new Settings("SyncManager");
            PluginDir = pluginDir;

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\SyncManager\\{DynelManager.LocalPlayer.Name}\\Config.json");
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel));

            Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannelChangedEvent += IPCChannel_Changed;

            Game.OnUpdate += OnUpdate;
            Network.N3MessageSent += Network_N3MessageSent;
            Network.N3MessageReceived += SyncTrade;
            Game.TeleportEnded += OnZoned;

            _settings.AddVariable("Enable", true);
            _settings["Enable"] = true;

            _settings.AddVariable("SyncAttack", false);
            _settings.AddVariable("SyncMove", false);
            _settings.AddVariable("SyncBags", false);
            _settings.AddVariable("SyncUse", true);
            _settings.AddVariable("SyncChat", false);
            _settings.AddVariable("NPCTrade", false);
            _settings.AddVariable("SyncTrade", false);

            IPCChannel.RegisterCallback((int)IPCOpcode.StartStop, OnStartStopMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Attack, OnAttackMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Use, OnUseMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Move, OnMoveMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.Target, OnLookAt);
            IPCChannel.RegisterCallback((int)IPCOpcode.UISettings, BroadcastSettingsReceived);
            IPCChannel.RegisterCallback((int)IPCOpcode.Spread, ReceivedSpreadOutCommand);

            IPCChannel.RegisterCallback((int)IPCOpcode.NpcChat, OnNpcChatMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NPCStartTrade, OnNPCStartTrade);
            IPCChannel.RegisterCallback((int)IPCOpcode.NPCTrade, OnNPCTrade);
            IPCChannel.RegisterCallback((int)IPCOpcode.NPCFinishTrade, OnNPCFinishTrade);

            RegisterSettingsWindow("Sync Manager", "SyncManagerSettingWindow.xml");

            Chat.RegisterCommand("sync", SyncManagerCommand);
            Chat.RegisterCommand("syncattack", SyncAttackSwitch);
            Chat.RegisterCommand("syncmove", SyncMoveSwitch);
            Chat.RegisterCommand("syncbags", SyncBagsSwitch);
            Chat.RegisterCommand("syncuse", SyncUseSwitch);
            Chat.RegisterCommand("synctrade", SyncTradeSwitch);
            Chat.RegisterCommand("syncchat", SyncChatSwitch);
            Chat.RegisterCommand("syncnpctrade", SyncNpcTradeSwitch);
            Chat.RegisterCommand("spreadem", (string command, string[] param, ChatWindow chatWindow) =>
            {
                IPCChannel.Broadcast(new SpreadCommand
                {
                    Position = DynelManager.LocalPlayer.Position,
                    instance = Playfield.ModelIdentity.Instance,
                });
            });

            if (!Game.IsNewEngine)
            {
                Chat.WriteLine("SyncManager Loaded!");
                Chat.WriteLine("/syncmanager for settings.");
            }
            else
            {
                Chat.WriteLine("Does not work on this engine!");
            }

            UseItem = null;
            UseTarget = Identity.None;
            UseType = 3;
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        void ReceivedSpreadOutCommand(int arg1, IPCMessage message)
        {
            var msg = message as SpreadCommand;
            var randoPos = msg.Position;
            randoPos.AddRandomness((int)3.0f);
            var player = DynelManager.LocalPlayer;

            if (msg.instance == Playfield.ModelIdentity.Instance)
            {
                if (player.Position.Distance2DFrom(randoPos) < 10 && player.Position.Distance2DFrom(randoPos) > 1)
                {
                    MovementController.Instance.SetDestination(randoPos);
                }
            }
        }

        void BroadcastSettingsReceived(int arg1, IPCMessage message)
        {
            if (message is UISettings uISettings)
            {
                _settings["SyncAttack"] = uISettings.Attack;
                _settings["SyncBags"] = uISettings.Bags;
                _settings["SyncUse"] = uISettings.Use;
                _settings["SyncChat"] = uISettings.Chat;
                _settings["SyncTrade"] = uISettings.Trade;
                _settings["NPCTrade"] = uISettings.NpcTrade;
            }
        }

        void OnUpdate(object s, float deltaTime)
        {
            if (Game.IsZoning) { return; }

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

                if (SettingsController.settingsWindow.FindView("BroadcastSettingsView", out Button settingsButton))
                {
                    settingsButton.Tag = SettingsController.settingsWindow;
                    settingsButton.Clicked = UISettingsButtonClicked;
                }

                if (SettingsController.settingsWindow.FindView("SpreadOut", out Button SpreadButton))
                {
                    SpreadButton.Tag = SettingsController.settingsWindow;
                    SpreadButton.Clicked = HandleSpreadButtonClicked;
                }
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

            if (_settings["Enable"].AsBool())
            {
                foreach (Item item in Inventory.Items)
                {
                    if (item.Slot.Type != IdentityType.Inventory) { continue; }
                    if (invSlots.ContainsKey(item.Slot.Instance)) { continue; }

                    invSlots.Add(item.Slot.Instance, item.Id);
                }

                if (_settings["SyncBags"].AsBool())
                {
                    HandleSyncBagsTick();
                }

                if (_settings["SyncUse"].AsBool())
                {
                    HandleSyncUseTick();
                }
            }
        }

        void HandleSyncBagsTick()
        {
            if (_openBags) { return; }

            var syncBags = Inventory.Backpacks.Where(bag => bag.Name.Contains("syncbag")).ToList();

            foreach (var item in Inventory.Items)
            {
                if (syncBags.Any(bag => bag.Identity.Instance == item.UniqueIdentity.Instance))
                {
                    item?.Use(); // Open
                    item?.Use(); // Close
                }
            }

            _openBags = true;
        }

        void HandleSyncUseTick()
        {
            _ringNameToItemNameMap = new Dictionary<RingName, string>
                        {
                            { RingName.PureNovictumRing, "Pure Novictum Ring" },
                            { RingName.RimyRing, "Rimy Ring" },
                            { RingName.AchromicRing, "Achromic Ring" },
                            { RingName.SanguineRing, "Sanguine Ring" },
                            { RingName.CaliginousRing, "Caliginous Ring" }
                        };

            _itemNameToRingNameMap = _ringNameToItemNameMap.ToDictionary(pair => pair.Value, pair => pair.Key);

            if (Item.HasPendingUse) { return; }
            if (PerkAction.List.Any(perk => perk.IsExecuting)) { return; }
            if (Spell.HasPendingCast) { return; }
            if (Time.AONormalTime < UseDelay) { return; }
            if (UseType == 3) { return; }
            UseDelay = Time.AONormalTime + 1.0;
            var playerPos = DynelManager.LocalPlayer.Position;

            var item = Inventory.Items.FirstOrDefault(i => i == UseItem) ?? Inventory.Backpacks.SelectMany(b => b.Items).FirstOrDefault(i => i == UseItem);

            var target = DynelManager.AllDynels.FirstOrDefault(x => x != null && x.Identity == UseTarget
                    && playerPos.DistanceFrom(x.Position) < 8 && x.Name != "Rubi-Ka Banking Service Terminal" && x.Name != "Mail Terminal");

            switch (UseType)
            {
                case 0:// UseItem
                    //Chat.WriteLine("UseItem");
                    if (item == null)
                    {
                        UseItem = null;
                        UseType = 3; return;
                    }
                    item?.Use();
                    UseItem = null;
                    UseType = 3;
                    break;
                case 1:// UseItemOnTarget

                    //Chat.WriteLine("UseItemOnTarget");

                    if (target == null)
                    {
                        UseTarget = Identity.None;
                        UseItem = null; return;
                    }

                    Network.Send(new GenericCmdMessage()
                    {
                        Unknown = 1,
                        Action = GenericCmdAction.UseItemOnItem,
                        Temp1 = 0,
                        Temp4 = 0,
                        Identity = DynelManager.LocalPlayer.Identity,
                        User = DynelManager.LocalPlayer.Identity,
                        Target = UseTarget,
                        Source = item?.Slot,

                    });

                    UseTarget = Identity.None;
                    UseItem = null;
                    UseType = 3;
                    break;
                case 2:// UseTarget
                    //Chat.WriteLine("UseTarget");

                    if (target == null)
                    {
                        UseTarget = Identity.None;
                        UseType = 3; return;
                    }

                    target?.Use();
                    UseTarget = Identity.None;
                    UseType = 3;
                    break;
            }
        }

        void HandleSpreadButtonClicked(object sender, ButtonBase e)
        {
            IPCChannel.Broadcast(new SpreadCommand
            {
                Position = DynelManager.LocalPlayer.Position,
                instance = Playfield.ModelIdentity.Instance,
            });
        }

        void OnStartStopMessage(int sender, IPCMessage msg)
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

        void Start()
        {
            if (Enable) { return; }
            Enable = true;
            Chat.WriteLine("Sync enabled");
        }

        void Stop()
        {
            if (!Enable) { return; }
            Enable = false;
            Chat.WriteLine("Sync disabled");
        }

        void OnZoned(object s, EventArgs e)
        {
            if (!_settings["Enable"].AsBool()) { return; }

            if (_settings["SyncBags"].AsBool()) { _openBags = false; }
            UseItem = null;
            UseTarget = Identity.None;
            UseType = 3;

        }

        #region IncomingCommunication

        void OnMoveMessage(int sender, IPCMessage msg)
        {
            if (IsActiveWindow) { return; }

            if (Game.IsZoning) { return; }

            if (!_settings["Enable"].AsBool()) { return; }

            var moveMsg = (MoveMessage)msg;

            if (Playfield.Identity.Instance != moveMsg.PlayfieldId) { return; }

            DynelManager.LocalPlayer.Position = moveMsg.Position;
            DynelManager.LocalPlayer.Rotation = moveMsg.Rotation;
            MovementController.Instance.SetMovement(moveMsg.MoveType);
        }

        void OnLookAt(int sender, IPCMessage look)
        {
            if (!_settings["Enable"].AsBool()) { return; }
            if (IsActiveWindow) { return; }

            var targetMsg = (TargetMessage)look;
            var localPlayer = DynelManager.LocalPlayer;

            if (localPlayer.IsAttacking) { return; }
            if (localPlayer.IsAttackPending) { return; }
            if (localPlayer.FightingTarget != null) { return; }
            if (Spell.HasPendingCast) { return; }
            if (Item.HasPendingUse) { return; }
            if (PerkAction.List.Any(p => p.IsExecuting)) { return; }

            Targeting.SetTarget(targetMsg.Target);
        }

        void OnAttackMessage(int sender, IPCMessage msg)
        {
            if (IsActiveWindow) { return; }
            if (!_settings["Enable"].AsBool()) { return; }
            if (!_settings["SyncAttack"].AsBool()) { return; }

            var attackMsg = (AttackIPCMessage)msg;

            if (attackMsg.Start)
            {
                var targetDynel = DynelManager.GetDynel(attackMsg.Target);
                DynelManager.LocalPlayer.Attack(targetDynel, true);
            }
            else { DynelManager.LocalPlayer.StopAttack(); }
        }

        void OnUseMessage(int sender, IPCMessage msg)
        {
            if (IsActiveWindow) { return; }
            if (!_settings["Enable"].AsBool()) { return; }
            if (!_settings["SyncUse"].AsBool()) { return; }

            var useMsg = (UseMessage)msg;
            if (useMsg.Sender == DynelManager.LocalPlayer.Identity) { return; }
            //Chat.WriteLine($"Action receved = {useMsg.Action}");

            switch (useMsg.Action)
            {
                case UseAction.UseItem:
                    ProcessIDItem(useMsg);
                    if (UseItem == null) { return; }
                    //Chat.WriteLine($"Found item {UseItem.Name}");
                    UseType = 0;
                    break;
                case UseAction.UseItemOnTarget:
                    if (useMsg.RingName != RingName.Unknown)
                    {
                        string ringName = GetItemNameFromRingName(useMsg.RingName);

                        if (ringName == null) { return; }
                        //Chat.WriteLine($"found ring {ringName}");
                        UseTarget = useMsg.Target;
                        FindUseRing(ringName);
                    }
                    else
                    {
                        UseTarget = useMsg.Target;
                        ProcessIDItem(useMsg);
                        //Chat.WriteLine($"Found item {UseItem.Name}");
                    }

                    if (UseItem == null) { return; }

                    UseType = 1;
                    break;
                case UseAction.UseTarget:
                    UseTarget = useMsg.Target;
                    //Chat.WriteLine($"UseTarget {useMsg.Target}");
                    UseType = 2;
                    break;
                default:
                    UseType = 3;
                    break;
            }
        }

        void FindUseRing(string itemName)
        {
            if (itemName == null) { return; }
            var ring = Inventory.Items.FirstOrDefault(c => c.Name.Contains(itemName)) ??
                            Inventory.Backpacks.SelectMany(b => b.Items).FirstOrDefault(c => c.Name.Contains(itemName));

            if (ring == null) { return; }

            UseItem = ring;
        }

        void ProcessIDItem(UseMessage usableMsg)
        {
            int[] ignoredItemIds = { 301679, 85907, 85908, 267167, 305478, 206013, 204653, 245990, 204698, 206015, 305476, 156576,
                164780, 164781, 244204, 245323, 244214, 244216, 204593, 305493, 204595, 305491, 204598, 305495, 157296, 303179,
                267168, 244655, 152028, 253187, 151693, 83919, 152029, 151692,253186, 83920, 291043, 204103, 204104, 204105, 204106, 204107,
                303138, 303141, 303137, 204698, 204653, 206013, 267168, 267167, 305476, 305478, 303179 };

            int[] ICCModifiedHackingTool = { 273512, 273513, 273514, 273515, 273516, 273517, 273230 };
            int[] ZeroPointTransmissionRelayScoop = { 275035, 375038, 275039, 275040, 275042 };

            if (ignoredItemIds.Contains(usableMsg.ItemId)) { return; }

            if (ICCModifiedHackingTool.Contains(usableMsg.ItemId))
            {
                if (Inventory.Find("ICC Modified Hacking Tool", out Item tool, false))
                {
                    UseItem = tool;
                }
            }

            if (ZeroPointTransmissionRelayScoop.Contains(usableMsg.ItemId))
            {
                if (Inventory.Find("Zero-Point Transmission Relay Scoop", out Item tool, false))
                {
                    UseItem = tool;
                }
            }

            UseItem = Inventory.Items.FirstOrDefault(i => i.Id == usableMsg.ItemId) ??
                Inventory.Backpacks.SelectMany(b => b.Items).FirstOrDefault(i => i.Id == usableMsg.ItemId);
        }
        void OnNpcChatMessage(int sender, IPCMessage msg)
        {
            if (!_settings["Enable"].AsBool()) { return; }
            if (!_settings["SyncChat"].AsBool()) { return; }
            if (IsActiveWindow) { return; }

            var chatMsg = (NpcChatIPCMessage)msg;

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
                    Target = chatMsg.Target
                });
            }
        }

        void OnNPCStartTrade(int sender, IPCMessage msg)
        {
            if (!_settings["Enable"].AsBool()) { return; }
            if (!_settings["NPCTrade"].AsBool()) { return; }
            if (IsActiveWindow) { return; }
            var NPCStartTrade = (NPCStartTradeIPCMessage)msg;

            NPCChatStartTrade(DynelManager.LocalPlayer.Identity, NPCStartTrade.Target);
        }

        void OnNPCTrade(int sender, IPCMessage msg)
        {
            if (!_settings["Enable"].AsBool()) { return; }
            if (!_settings["NPCTrade"].AsBool()) { return; }
            if (IsActiveWindow) { return; }

            var NpcTrade = (NpcTradeIPCMessage)msg;

            if (!NPCReceivedItem.Contains(NpcTrade.Id))
            {
                NPCReceivedItem.Add(NpcTrade.Id);
            }

            foreach (var NpcItemID in NPCReceivedItem)
            {
                var item = Inventory.Items.FirstOrDefault(i => i.Id == NpcItemID);
                if (item == null) { continue; }
                NPCChatAddTradeItem(DynelManager.LocalPlayer.Identity, NpcTrade.Target, item.Slot);
                NPCReceivedItem.Remove(NpcItemID);
            }
        }

        void OnNPCFinishTrade(int sender, IPCMessage msg)
        {
            if (!_settings["Enable"].AsBool()) { return; }
            if (!_settings["NPCTrade"].AsBool()) { return; }
            if (IsActiveWindow) { return; }

            var NpcFinishTrade = (NpcFinishTradeMessage)msg;

            switch (NpcFinishTrade.Decline)
            {
                case 0:
                    NPCChatEndTrade(DynelManager.LocalPlayer.Identity, NpcFinishTrade.Target, NpcFinishTrade.Amount);
                    NPCReceivedItem.Clear();
                    break;
                case 1:
                    NPCReceivedItem.Clear();
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region OutgoingCommunication

        void Network_N3MessageSent(object s, N3Message n3Msg)
        {
            if (!IsActiveWindow || n3Msg.Identity != DynelManager.LocalPlayer.Identity) { return; }

            if (!_settings["Enable"].AsBool()) { return; }

            if (n3Msg.N3MessageType == N3MessageType.LookAt)
            {
                var lookAtMsg = (LookAtMessage)n3Msg;

                IPCChannel.Broadcast(new TargetMessage()
                {
                    Target = lookAtMsg.Target
                });
            }

            if (_settings["SyncMove"].AsBool())
            {
                switch (n3Msg.N3MessageType)
                {
                    case N3MessageType.CharDCMove:
                        var charDCMoveMsg = (CharDCMoveMessage)n3Msg;

                        IPCChannel.Broadcast(new MoveMessage()
                        {
                            MoveType = charDCMoveMsg.MoveType,
                            PlayfieldId = Playfield.Identity.Instance,
                            Position = charDCMoveMsg.Position,
                            Rotation = charDCMoveMsg.Heading
                        });
                        break;
                    case N3MessageType.CharacterAction:
                        var charActionMsg = (CharacterActionMessage)n3Msg;

                        if (charActionMsg.Action == CharacterActionType.StandUp)
                        {
                            IPCChannel.Broadcast(new MoveMessage()
                            {
                                MoveType = MovementAction.LeaveSit,
                                PlayfieldId = Playfield.Identity.Instance,
                                Position = DynelManager.LocalPlayer.Position,
                                Rotation = DynelManager.LocalPlayer.Rotation
                            });
                        }
                        break;
                    default:
                        break;
                }
            }

            if (_settings["SyncAttack"].AsBool())
            {
                switch (n3Msg.N3MessageType)
                {
                    case N3MessageType.Attack:
                        var attackMsg = (AttackMessage)n3Msg;

                        IPCChannel.Broadcast(new AttackIPCMessage
                        {
                            Target = attackMsg.Target,
                            Start = true
                        });
                        break;
                    case N3MessageType.StopFight:
                        IPCChannel.Broadcast(new AttackIPCMessage
                        {
                            Start = false
                        });
                        break;
                    default:
                        break;
                }
            }

            if (_settings["SyncUse"].AsBool())
            {
                if (n3Msg.N3MessageType == N3MessageType.GenericCmd)
                {
                    var genericCmdMsg = (GenericCmdMessage)n3Msg;

                    switch (genericCmdMsg.Action)
                    {
                        case GenericCmdAction.Use:
                            if (genericCmdMsg.Target.Type == IdentityType.Terminal)
                            {
                                UseMessage useMsg = new UseMessage()
                                {
                                    Action = UseAction.UseTarget,
                                    Target = genericCmdMsg.Target,
                                    Sender = DynelManager.LocalPlayer.Identity,
                                };

                                IPCChannel.Broadcast(useMsg);
                            }
                            else
                            {

                                BroadcastUsableMessage(FindItem(genericCmdMsg.Target), Identity.None);
                            }
                            break;
                        case GenericCmdAction.UseItemOnItem:

                            var item = FindItem(genericCmdMsg.Source.Value);

                            RingName ringName = GetRingNameFromItemName(item?.Name);

                            if (ringName != RingName.Unknown)
                            {
                                var useMsg = new UseMessage()
                                {
                                    Action = UseAction.UseItemOnTarget,
                                    Target = genericCmdMsg.Target,
                                    RingName = ringName,
                                    Sender = DynelManager.LocalPlayer.Identity,
                                };

                                IPCChannel.Broadcast(useMsg);
                            }
                            else
                            {
                                BroadcastUsableMessage(FindItem(genericCmdMsg.Source.Value), genericCmdMsg.Target);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            if (_settings["SyncChat"].AsBool())
            {
                switch (n3Msg.N3MessageType)
                {
                    case N3MessageType.KnubotOpenChatWindow:

                        var n3OpenChatMessage = (KnuBotOpenChatWindowMessage)n3Msg;

                        IPCChannel.Broadcast(new NpcChatIPCMessage
                        {
                            Target = n3OpenChatMessage.Target,
                            OpenClose = true,
                            Answer = -1
                        });
                        break;
                    case N3MessageType.KnubotAnswer:
                        var n3AnswerMsg = (KnuBotAnswerMessage)n3Msg;

                        IPCChannel.Broadcast(new NpcChatIPCMessage
                        {
                            Target = n3AnswerMsg.Target,
                            OpenClose = true,
                            Answer = n3AnswerMsg.Answer
                        });
                        break;
                    case N3MessageType.KnubotCloseChatWindow:
                        var n3CloseChatMessage = (KnuBotCloseChatWindowMessage)n3Msg;

                        IPCChannel.Broadcast(new NpcChatIPCMessage
                        {
                            Target = n3CloseChatMessage.Target,
                            OpenClose = false,
                            Answer = -1
                        });
                        break;
                    default:
                        break;
                }
            }

            if (_settings["NPCTrade"].AsBool())
            {
                switch (n3Msg.N3MessageType)
                {
                    case N3MessageType.KnubotStartTrade:
                        var startTradeMsg = (KnuBotStartTradeMessage)n3Msg;
                        IPCChannel.Broadcast(new NPCStartTradeIPCMessage
                        {
                            Target = startTradeMsg.Target,
                        });
                        break;
                    case N3MessageType.KnubotTrade:
                        var tradeMsg = (KnuBotTradeMessage)n3Msg;

                        int slotInstance = tradeMsg.Container.Instance;

                        if (invSlots.TryGetValue(slotInstance, out int itemId))
                        {
                            IPCChannel.Broadcast(new NpcTradeIPCMessage
                            {
                                Id = itemId,
                                Target = tradeMsg.Target,
                            });
                        };
                        break;
                    case N3MessageType.KnubotFinishTrade:
                        var finishTradeMsg = (KnuBotFinishTradeMessage)n3Msg;

                        IPCChannel.Broadcast(new NpcFinishTradeMessage
                        {
                            Target = finishTradeMsg.Target,
                            Decline = finishTradeMsg.Decline,
                            Amount = finishTradeMsg.Amount,
                        });
                        break;
                    default:
                        break;
                }
            }
        }

        Item FindItem(Identity target)
        {
            return Inventory.Find(target, out Item item) ? item :
                   Inventory.Backpacks
                            .SelectMany(b => b.Items)
                            .FirstOrDefault(i => i.Slot.Instance == target.Instance);
        }

        void BroadcastUsableMessage(Item item, Identity target)
        {
            if (item == null) { return; }

            UseAction useAction = UseAction.UseItem;

            if (target != Identity.None)
            {
                useAction = UseAction.UseItemOnTarget;
            }

            if (!IsOther(item))
            {
                var usableMsg = new UseMessage()
                {
                    Action = useAction,
                    ItemId = item.Id,
                    Target = target,
                    Sender = DynelManager.LocalPlayer.Identity,
                };

                //Chat.WriteLine($"Sending {item.Name}");
                IPCChannel.Broadcast(usableMsg);
            }
        }

        void SyncTrade(object s, N3Message n3Msg)
        {
            if (!_settings["Enable"].AsBool() && _settings["SyncTrade"].AsBool()) { return; }

            if (n3Msg.N3MessageType == N3MessageType.Trade)
            {
                var tradeMsg = (TradeMessage)n3Msg;

                if (DynelManager.LocalPlayer.Identity == tradeMsg.Identity)
                {
                    if (tradeMsg.Action != TradeAction.Accept) { return; }

                    if (Inventory.NumFreeSlots >= 1)
                    {
                        Trade.Accept(tradeMsg.Identity);
                    }
                    else
                    {
                        Trade.Decline();
                    }
                }
                else
                {
                    if (tradeMsg.Action != TradeAction.Confirm) { return; }

                    Trade.Confirm(tradeMsg.Identity);
                }
            }
        }

        #endregion

        #region Settings
        static void IPCChannel_Changed(object s, int e)
        {
            IPCChannel.SetChannelId(Convert.ToByte(e));

            Config.Save();
        }
        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDir + "\\UI\\" + xmlName, _settings);
        }

        void HandleInfoViewClick(object s, ButtonBase button)
        {
            _infoWindow = Window.CreateFromXml("Info", PluginDir + "\\UI\\SyncManagerInfoView.xml",
                windowSize: new Rect(0, 0, 310, 510),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            _infoWindow.Show(true);
        }

        void UISettingsButtonClicked(object s, ButtonBase button)
        {
            IPCChannel.Broadcast(new UISettings()
            {
                Attack = _settings["SyncAttack"].AsBool(),
                Bags = _settings["SyncBags"].AsBool(),
                Use = _settings["SyncUse"].AsBool(),
                Chat = _settings["SyncChat"].AsBool(),
                Trade = _settings["SyncTrade"].AsBool(),
                NpcTrade = _settings["NPCTrade"].AsBool(),
            });
        }

        #endregion

        #region Misc

        public enum UseAction
        {
            UseItem,
            UseItemOnTarget,
            UseTarget,
            Null,
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

        RingName GetRingNameFromItemName(string itemName)
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

        string GetItemNameFromRingName(RingName ringName)
        {
            if (_ringNameToItemNameMap.TryGetValue(ringName, out var itemName))
            {
                return itemName;
            }

            return null;
        }

        void SyncManagerCommand(string command, string[] param, ChatWindow chatWindow)
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

        void SyncAttackSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["SyncAttack"] = !_settings["SyncAttack"].AsBool();
                Chat.WriteLine($"Sync attack : {_settings["SyncAttack"].AsBool()}");
            }
        }

        void SyncUseSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["SyncUse"] = !_settings["SyncUse"].AsBool();
                Chat.WriteLine($"Sync use : {_settings["SyncUse"].AsBool()}");
            }
        }

        void SyncChatSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["SyncChat"] = !_settings["SyncChat"].AsBool();
                Chat.WriteLine($"Sync chat : {_settings["SyncChat"].AsBool()}");
            }
        }

        void SyncNpcTradeSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["NPCTrade"] = !_settings["NPCTrade"].AsBool();
                Chat.WriteLine($"Npc trade : {_settings["NPCTrade"].AsBool()}");
            }
        }

        void SyncTradeSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["SyncTrade"] = !_settings["SyncTrade"].AsBool();
                Chat.WriteLine($"Sync trading : {_settings["SyncTrade"].AsBool()}");
            }
        }

        void SyncMoveSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["SyncMove"] = !_settings["SyncMove"].AsBool();
                Chat.WriteLine($"Sync move : {_settings["SyncMove"].AsBool()}");
            }
        }
        void SyncBagsSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["SyncBags"] = !_settings["SyncBags"].AsBool();
                Chat.WriteLine($"Sync bags : {_settings["SyncBags"].AsBool()}");
            }
        }


        static bool IsOther(Item item)
        {
            return item.Id == 305476 || item.Id == 204698 || item.Id == 156576 || item.Id == 267168 || item.Id == 267167
                || item.Id == 204593 || item.Id == 305492 || item.Id == 204595 || item.Id == 305491 || item.Id == 305478
                || item.Id == 206013 || item.Id == 204653 || item.Id == 204698 || item.Id == 206015 || item.Id == 305476
                || item.Id == 267168 || item.Id == 267167 || item.Name.Contains("Health") || item.Name.Contains("Newcomer")
                || item.Name.Contains("Stim") || item.Name.Contains("syncbag") || item.UniqueIdentity.Type == IdentityType.Container;
        }

        #endregion

        #region Dll Imports

        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_NPCChatStartTrade@n3EngineClientAnarchy_t@@QAEXABVIdentity_t@@0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void eNPCChatStartTrade(IntPtr pEngine, ref Identity self, ref Identity npc);
        public static void NPCChatStartTrade(Identity self, Identity npc)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine != IntPtr.Zero)
            {
                eNPCChatStartTrade(pEngine, ref self, ref npc);
            }
        }

        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_NPCChatAddTradeItem@n3EngineClientAnarchy_t@@QAEXABVIdentity_t@@00@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void eNPCChatAddTradeItem(IntPtr pEngine, ref Identity self, ref Identity npc, ref Identity slot);
        public static void NPCChatAddTradeItem(Identity self, Identity npc, Identity item)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine != IntPtr.Zero)
            {
                eNPCChatAddTradeItem(pEngine, ref self, ref npc, ref item);
            }
        }

        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_NPCChatEndTrade@n3EngineClientAnarchy_t@@QAEXABVIdentity_t@@0H_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void eNPCChatEndTrade(IntPtr pEngine, ref Identity self, ref Identity npc, int credits, bool decline);
        public static void NPCChatEndTrade(Identity self, Identity npc, int credits = 0, bool accept = true)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine != IntPtr.Zero)
            {
                eNPCChatEndTrade(pEngine, ref self, ref npc, credits, accept);
            }
        }

        #endregion
    }
}
