using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using System.Collections.Generic;
using AOSharp.Common.Unmanaged.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AOSharp.Common.GameData.UI;
using System.IO;
using SmokeLounge.AOtomation.Messaging.Messages;
using System.Text;
using AOSharp.Common.SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using Zoltu.IO;
using System;
using AOSharp.Core.GMI;

namespace GMIManager
{
    public class GMIManager : AOPluginEntry
    {
        public static Config Config { get; private set; }

        private static string GMIBuyOrderName;
        private static long GMIBuyOrderEndPrice;
        private static int GMIWithdrawAmount;

        protected Settings _settings;

        public static bool Toggle = false;

        private static double _timeOut = Time.NormalTime;

        private static double _mailOpenTimer;
        private static double _gmiUpdateTimer;
        private static double _gmiWithdrawTimer;
        private static int _gmiWithdrawAmount = 0;

        private static int _mailId = 0;

        private static bool _init = false;

        public static Window _infoWindow;

        public static View _infoView;

        public static string PluginDir;
        private static ulong _queuedCash = 0;
        private static ulong _maxCredits = 999999999;
        private static ulong _maxMarketCredits = 3999999999;
        private static bool _initStart = true;
        private static bool _initMarketCredits = false;
        private static long _marketCredits = 0;
        private static MyMarketBuyOrder _oldOrder;
        private static MyMarketBuyOrder _newOrder;
        private static long _modifyAmount = 3600000000;
        private static bool _modifyingAmount = false;
        private static bool _lastDeposit = false;

        //private static long _modifyAmount = 20000;

        public override void Run(string pluginDir)
        {
            _settings = new Settings("GMIManager");
            PluginDir = pluginDir;

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\GMIManager\\{Game.ClientInst}\\Config.json");

            Config.CharSettings[Game.ClientInst].GMIBuyOrderNameChangedEventChangedEvent += GMIBuyOrderName_Changed;
            Config.CharSettings[Game.ClientInst].GMIBuyOrderEndPriceChangedEvent += GMIBuyOrderEndPrice_Changed;
            Config.CharSettings[Game.ClientInst].GMIWithdrawAmountChangedEvent += GMIWithdrawAmount_Changed;
            Network.PacketReceived += HandleMail;

            Game.OnUpdate += OnUpdate;

            _settings.AddVariable("ModeSelection", (int)ModeSelection.Withdraw);

            _settings.AddVariable("Toggle", false);

            Chat.RegisterCommand("gmi", (string command, string[] param, ChatWindow chatWindow) =>
            {
                if (param.Length == 2)
                {
                    if (param[0] == "deposit")
                        GMI.Deposit(Convert.ToInt32(param[1]));
                }
                else if (param.Length == 0)
                {
                    _queuedCash -= 4345454;
                    Chat.WriteLine($"{_queuedCash}");

                    //if (ModeSelection.Withdraw == (ModeSelection)_settings["ModeSelection"].AsInt32())
                    //{
                    //    if (GMIWithdrawAmount > 0)
                    //    {
                    //        _settings["Toggle"] = !_settings["Toggle"].AsBool();
                    //        Chat.WriteLine("Starting.");
                    //    }
                    //}

                    //if (ModeSelection.Modify == (ModeSelection)_settings["ModeSelection"].AsInt32())
                    //{
                    //    if (!string.IsNullOrEmpty(GMIBuyOrderName) && GMIBuyOrderEndPrice > 0)
                    //    {
                    //        _settings["Toggle"] = !_settings["Toggle"].AsBool();
                    //        Chat.WriteLine("Starting.");
                    //    }
                    //}
                }
            });

            RegisterSettingsWindow("GMI Manager", $"GMIManagerSettingWindow.xml");

            Chat.WriteLine("GMI Manager Loaded!");
            Chat.WriteLine("/gmimanager for settings.");

            _settings["Toggle"] = false;

            GMIWithdrawAmount = Config.CharSettings[Game.ClientInst].GMIWithdrawAmount;
            GMIBuyOrderName = Config.CharSettings[Game.ClientInst].GMIBuyOrderName;
            GMIBuyOrderEndPrice = Config.CharSettings[Game.ClientInst].GMIBuyOrderEndPrice;
        }

        private void HandleInfoViewClick(object s, ButtonBase button)
        {
            _infoWindow = Window.CreateFromXml("Info", PluginDir + "\\UI\\GMIManagerInfoView.xml",
                windowSize: new Rect(0, 0, 440, 510),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            _infoWindow.Show(true);
        }
        public static void GMIWithdrawAmount_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].GMIWithdrawAmount = e;
            GMIWithdrawAmount = e;
            Config.Save();
        }

        public static void GMIBuyOrderName_Changed(object s, string e)
        {
            Config.CharSettings[Game.ClientInst].GMIBuyOrderName = e;
            GMIBuyOrderName = e;
            Config.Save();
        }

        public static void GMIBuyOrderEndPrice_Changed(object s, long e)
        {
            Config.CharSettings[Game.ClientInst].GMIBuyOrderEndPrice = e;
            GMIBuyOrderEndPrice = e;
            Config.Save();
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }
        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDir + "\\UI\\" + xmlName, _settings);
        }


        public static void ReadMail(int msgId)
        {
            SendShortMailMsg(MailMsgType.Read, msgId);
        }
        public static void TakeAllMail(int msgId)
        {
            SendShortMailMsg(MailMsgType.TakeAll, msgId);
        }
        public static void DeleteMail(int msgId)
        {
            SendShortMailMsg(MailMsgType.Delete, msgId);
        }

        public enum MailMsgType
        {
            Read = 1,
            TakeAll = 3,
            Delete = 5,
        }
        private static void SendShortMailMsg(MailMsgType msgType, int msgId)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BigEndianBinaryWriter writer = new BigEndianBinaryWriter(stream))
                {
                    //Header
                    writer.Write((short)0);
                    writer.Write((short)PacketType.N3Message);
                    writer.Write((short)1);
                    writer.Write((short)0);
                    writer.Write(Game.ClientInst);
                    writer.Write((int)2);
                    writer.Write((int)N3MessageType.Mail);
                    writer.Write((int)IdentityType.SimpleChar);
                    writer.Write(Game.ClientInst);
                    writer.Write((byte)0);

                    //Body
                    writer.Write((short)msgType);
                    writer.Write((int)0);
                    writer.Write(msgId);


                    //Fix packet length
                    short length = (short)writer.BaseStream.Position;
                    writer.BaseStream.Position = 6;
                    writer.Write(length);

                    Network.Send(stream.ToArray());
                }
            }
        }

        public static void HandleMail(object s, byte[] packet)
        {
            using (MemoryStream stream = new MemoryStream(packet))
            {
                using (BigEndianBinaryReader reader = new BigEndianBinaryReader(stream))
                {
                    reader.BaseStream.Position = 30;
                    byte mailInstance = reader.ReadByte();

                    if (mailInstance == 0)
                        PopMail(reader);
                }
            }
        }

        private static void PopMail(BigEndianBinaryReader reader)
        {
            int messageCount = (reader.ReadInt32() - 0x3f1) / 0x3f1;

            reader.ReadInt32();
            int messageId = reader.ReadInt32();
            int source = reader.ReadInt32();
            reader.ReadByte();
            byte fromLength = reader.ReadByte();
            string fromTitle = Encoding.Default.GetString(reader.ReadBytes(Convert.ToInt32(fromLength)));
            reader.ReadByte();
            byte subjectLength = reader.ReadByte();
            string subjectTitle = Encoding.Default.GetString(reader.ReadBytes(Convert.ToInt32(subjectLength)));
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadByte();


            Chat.WriteLine($"ID: {messageId} / From: {fromTitle} / Subject: {subjectTitle}");
            Chat.WriteLine($"Mail populated.");

            if (_mailId == 0)
                _mailId = messageId;
        }

        public enum ModeSelection
        {
            Withdraw, Modify, Refresh
        }

        private void RequestGMIInventory()
        {
            GMI.GetInventory().ContinueWith(async marketInventory =>
            {
                if (marketInventory.IsFaulted || marketInventory.Result == null)
                {
                    await Task.Delay(1000);
                    RequestGMIInventory();
                    return;
                }

                OnMarketInventoryLoaded(marketInventory.Result);
            });
        }

        private void OnMarketInventoryLoaded(MarketInventory marketInventory)
        {
            GMI.GetMarketOrders().ContinueWith(marketOrders =>
            {
                if (!marketOrders.IsFaulted && marketOrders.Result != null)
                    OnMarketOrdersLoaded(marketInventory, marketOrders.Result);
            });
        }

        private void OnMarketOrdersLoaded(MarketInventory marketInventory, MyMarketOrders myOrders)
        {
            if (ModeSelection.Refresh == (ModeSelection)_settings["ModeSelection"].AsInt32())
            {
                if (!_init)
                {
                    _oldOrder = myOrders.BuyOrders.Where(x => x.Name == GMIBuyOrderName
                        && x.Price > 1).FirstOrDefault();

                    _newOrder = myOrders.BuyOrders.Where(x => x.Name == GMIBuyOrderName
                        && x.Price == 1).FirstOrDefault();

                    _init = true;
                }

                _oldOrder = myOrders.BuyOrders.Where(c => c.Id == _oldOrder.Id).FirstOrDefault();
                _newOrder = myOrders.BuyOrders.Where(c => c.Id == _newOrder.Id).FirstOrDefault();

                if (_lastDeposit && _modifyingAmount == false && _oldOrder == null)
                {
                    _newOrder.ModifyPrice(_newOrder.Price + (long)(marketInventory.Credits * 0.980f) / _newOrder.Count).ContinueWith(modifyNewOrder =>
                    {
                        if (modifyNewOrder.Result.Succeeded)
                        {
                            Chat.WriteLine($"Modified.");
                            _settings["Toggle"] = false;
                            _lastDeposit = false;
                            Chat.WriteLine("Finished.");
                            return;
                        }
                    });
                }

                if (_oldOrder.Price < _modifyAmount && marketInventory.Credits < _oldOrder.Price
                    && _modifyingAmount == false)
                {
                    _oldOrder.Cancel();
                    _lastDeposit = true;
                }

                if (_modifyingAmount)
                {
                    _newOrder.ModifyPrice(_newOrder.Price + ((long)(marketInventory.Credits * 0.980f) / _newOrder.Count)).ContinueWith(modifyNewOrder =>
                    {
                        if (modifyNewOrder.Result.Succeeded)
                        {
                            _modifyingAmount = false;
                            Chat.WriteLine($"Modified.");
                        }
                    });
                }
                else
                {
                    _oldOrder.ModifyPrice(_oldOrder.Price - _modifyAmount).ContinueWith(modifyOldOrder =>
                    {
                        if (modifyOldOrder.Result.Succeeded)
                        {
                            _modifyingAmount = true;
                            Chat.WriteLine($"Modified.");
                        }
                    });
                }
            }

            if (ModeSelection.Modify == (ModeSelection)_settings["ModeSelection"].AsInt32())
            {

                MyMarketBuyOrder ourOrder = myOrders.BuyOrders.Where(x => x.Name == GMIBuyOrderName
                && x.Price < GMIBuyOrderEndPrice).OrderByDescending(x => x.Price).FirstOrDefault();

                _marketCredits = marketInventory.Credits;

                if (ourOrder == null)
                {
                    _init = false;
                    _initStart = true;
                    _settings["Toggle"] = false;
                    _initMarketCredits = false;
                    Chat.WriteLine("Finished.");
                    return;
                }

                long desiredIncrease = Math.Min(GMIBuyOrderEndPrice - ourOrder.Price, (long)(marketInventory.Credits * 0.980f)) / ourOrder.Count;

                ourOrder.ModifyPrice(ourOrder.Price + desiredIncrease).ContinueWith(modifyOrder =>
                {
                    if (!_initMarketCredits)
                        _initMarketCredits = true;

                    if (modifyOrder.Result.Succeeded)
                    {
                        Chat.WriteLine($"{GMIBuyOrderName} successfully increased to {(ourOrder.Price + desiredIncrease):N0}");
                        _queuedCash = (ulong)_marketCredits - (ulong)marketInventory.Credits;
                        _marketCredits = marketInventory.Credits;
                    }
                    else
                    {
                        //Chat.WriteLine($"No credits {marketInventory.Credits}.");
                    }
                });
            }
        }


        private void OnUpdate(object s, float deltaTime)
        {
            if (Game.IsZoning) { return; }

            if (!_settings["Toggle"].AsBool())
                _init = false;

            if (_settings["Toggle"].AsBool())
            {
                if (_initStart)
                {
                    Chat.WriteLine($"Starting.");
                    _timeOut = Time.NormalTime;
                    _initStart = false;
                }

                if (!_initStart
                    && ModeSelection.Modify == (ModeSelection)_settings["ModeSelection"].AsInt32()
                    && Time.NormalTime > _timeOut + 340)
                {
                    _init = false;
                    _initStart = true;
                    _settings["Toggle"] = false;
                    _initMarketCredits = false;
                    Chat.WriteLine("Timed out.");
                }

                if (ModeSelection.Withdraw == (ModeSelection)_settings["ModeSelection"].AsInt32()
                    && Time.NormalTime > _gmiWithdrawTimer + 9)
                {
                    if ((_gmiWithdrawAmount < GMIWithdrawAmount) || GMIWithdrawAmount == 0)
                    {
                        Task.Factory.StartNew(
                            async () =>
                            {
                                await Task.Delay(500);
                                await GMI.WithdrawCash(900000000);
                                await Task.Delay(500);
                                _gmiWithdrawAmount++;
                                Chat.WriteLine($"Withdrawn {_gmiWithdrawAmount}b. {_gmiWithdrawAmount}b/{GMIWithdrawAmount}b");
                            });
                    }
                    else
                    {
                        _settings["Toggle"] = false;
                        Chat.WriteLine($"Finished withdrawing.");
                    }

                    _gmiWithdrawTimer = Time.NormalTime;
                }

                if (ModeSelection.Refresh == (ModeSelection)_settings["ModeSelection"].AsInt32()
                    && Time.NormalTime > _gmiUpdateTimer + 2.5)
                {
                    RequestGMIInventory();

                    _gmiUpdateTimer = Time.NormalTime;
                }

                if (_init
                    && ModeSelection.Modify == (ModeSelection)_settings["ModeSelection"].AsInt32()
                    && Time.NormalTime > _gmiUpdateTimer + 3)
                {
                    RequestGMIInventory();

                    _gmiUpdateTimer = Time.NormalTime;
                }

                if (ModeSelection.Modify == (ModeSelection)_settings["ModeSelection"].AsInt32()
                    && Time.NormalTime > _mailOpenTimer + 7)
                {
                    Task.Factory.StartNew(
                        async () =>
                        {
                            if (!_init)
                            {
                                Dynel _mailTerminal = DynelManager.AllDynels.FirstOrDefault(c => c.Name == "Mail Terminal");
                                Dynel _marketTerminal = DynelManager.AllDynels.FirstOrDefault(c => c.Name == "Market Terminal");
                                await Task.Delay(500);
                                _mailTerminal?.Use();
                                _marketTerminal?.Use();
                                await Task.Delay(500);
                                _marketTerminal?.Use();
                                _init = true;
                            }

                            if (_mailId > 0
                                && _initMarketCredits
                                && _queuedCash < (_maxMarketCredits - _maxCredits))
                            {
                                Chat.WriteLine("Handling mail..");
                                _timeOut = Time.NormalTime;
                                await Task.Delay(500);
                                ReadMail(_mailId);
                                await Task.Delay(1100);
                                TakeAllMail(_mailId);
                                await Task.Delay(1100);
                                DeleteMail(_mailId);
                                await Task.Delay(1100);
                                if ((_maxMarketCredits - _queuedCash) >= _maxCredits)
                                {
                                    _queuedCash += (ulong)DynelManager.LocalPlayer.GetStat(Stat.Cash);
                                    GMI.Deposit(DynelManager.LocalPlayer.GetStat(Stat.Cash));
                                }
                                else
                                {
                                    _queuedCash += _maxMarketCredits - _queuedCash;
                                    GMI.Deposit((int)_maxMarketCredits - (int)_queuedCash);
                                }
                                _mailId = 0;
                                ReadMail(0);
                            }
                            else
                            {
                                ReadMail(0);
                            }
                        });

                    _mailOpenTimer = Time.NormalTime;
                }
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("GMIWithdrawAmount", out TextInputView gMIWithdrawAmountInput);
                SettingsController.settingsWindow.FindView("GMIBuyOrdersName", out TextInputView gMIBuyOrdersNameInput);
                SettingsController.settingsWindow.FindView("GMIBuyOrdersEndPrice", out TextInputView gMIBuyOrdersEndPriceInput);

                if (gMIBuyOrdersNameInput != null && !string.IsNullOrEmpty(gMIBuyOrdersNameInput.Text))
                {
                    if (Config.CharSettings[Game.ClientInst].GMIBuyOrderName != gMIBuyOrdersNameInput.Text)
                    {
                        Config.CharSettings[Game.ClientInst].GMIBuyOrderName = gMIBuyOrdersNameInput.Text;
                    }
                }

                if (gMIBuyOrdersEndPriceInput != null && !string.IsNullOrEmpty(gMIBuyOrdersEndPriceInput.Text))
                {
                    if (long.TryParse(gMIBuyOrdersEndPriceInput.Text, out long gMIBuyOrdersEndPriceValue)
                        && Config.CharSettings[Game.ClientInst].GMIBuyOrderEndPrice != gMIBuyOrdersEndPriceValue)
                    {
                        Config.CharSettings[Game.ClientInst].GMIBuyOrderEndPrice = gMIBuyOrdersEndPriceValue;
                    }
                }


                if (gMIWithdrawAmountInput != null && !string.IsNullOrEmpty(gMIWithdrawAmountInput.Text))
                {
                    if (int.TryParse(gMIWithdrawAmountInput.Text, out int gMIWithdrawAmountValue)
                        && Config.CharSettings[Game.ClientInst].GMIWithdrawAmount != gMIWithdrawAmountValue)
                    {
                        Config.CharSettings[Game.ClientInst].GMIWithdrawAmount = gMIWithdrawAmountValue;
                    }
                }

                if (SettingsController.settingsWindow.FindView("GMIManagerInfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = HandleInfoViewClick;
                }
            }
        }
    }
}
