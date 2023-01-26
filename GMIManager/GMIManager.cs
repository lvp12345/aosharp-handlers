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

        private static double _mailOpenTimer;
        private static double _gmiUpdateTimer;
        private static double _gmiWithdrawTimer;
        private static int _gmiWithdrawAmount = 0;

        private static int _mailId = 0;

        private static bool _init = false;

        public static Window _infoWindow;

        public static View _infoView;

        public static string PluginDir;

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
                if (ModeSelection.Withdraw == (ModeSelection)_settings["ModeSelection"].AsInt32())
                {
                    if (GMIWithdrawAmount > 0)
                    {
                        _settings["Toggle"] = !_settings["Toggle"].AsBool();
                        Chat.WriteLine("Starting.");
                    }
                }

                if (ModeSelection.Modify == (ModeSelection)_settings["ModeSelection"].AsInt32())
                {
                    if (!string.IsNullOrEmpty(GMIBuyOrderName) && GMIBuyOrderEndPrice > 0)
                    {
                        _settings["Toggle"] = !_settings["Toggle"].AsBool();
                        Chat.WriteLine("Starting.");
                    }
                }
            });

            RegisterSettingsWindow("GMI Manager", $"GMIManagerSettingWindow.xml");

            Chat.WriteLine("GMI Manager Loaded!");
            Chat.WriteLine("/gmimanager for settings.");

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


            //Chat.WriteLine($"ID: {messageId} / From: {fromTitle} / Subject: {subjectTitle}");
            //Chat.WriteLine($"Mail populated.");

            if (_mailId == 0)
                _mailId = messageId;
        }

        public enum ModeSelection
        {
            Withdraw, Modify
        }

        private static void RequestGMIInventory()
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

        private static void OnMarketInventoryLoaded(MarketInventory marketInventory)
        {
            GMI.GetMarketOrders().ContinueWith(marketOrders =>
            {
                if (!marketOrders.IsFaulted && marketOrders.Result != null)
                    OnMarketOrdersLoaded(marketInventory, marketOrders.Result);
            });
        }

        private static void OnMarketOrdersLoaded(MarketInventory marketInventory, MyMarketOrders myOrders)
        {
            MyMarketBuyOrder ourOrder = myOrders.BuyOrders.Where(x => x.Name == GMIBuyOrderName
                && x.Price < GMIBuyOrderEndPrice).OrderByDescending(x => x.Price).FirstOrDefault();

            if (ourOrder == null)
            {
                Toggle = false;
                Chat.WriteLine("Finished");
                return;
            }

            long desiredIncrease = Math.Min(GMIBuyOrderEndPrice - ourOrder.Price, (long)(marketInventory.Credits * 0.980f)) / ourOrder.Count;

            ourOrder.ModifyPrice(ourOrder.Price + desiredIncrease).ContinueWith(modifyOrder =>
            {
                if (modifyOrder.Result.Succeeded)
                {
                    Chat.WriteLine($"{GMIBuyOrderName} successfully increased to {(ourOrder.Price + desiredIncrease):N0}");
                }
                else
                {
                    Chat.WriteLine($"No credits.");
                }
            });
        }


        private void OnUpdate(object s, float deltaTime)
        {
            if (!_settings["Toggle"].AsBool() && Toggle)
            {
                Toggle = false;
                _init = false;
            }

            if (_settings["Toggle"].AsBool() && !Game.IsZoning)
            {
                if (!Toggle)
                {
                    Chat.WriteLine("Starting.");
                    Toggle = true;
                }

                if (_settings["Toggle"].AsBool() && !Game.IsZoning 
                    && ModeSelection.Withdraw == (ModeSelection)_settings["ModeSelection"].AsInt32()
                    && Time.NormalTime > _gmiWithdrawTimer + 10)
                {
                    if (_gmiWithdrawAmount < GMIBuyOrderEndPrice)
                    {
                        Task.Factory.StartNew(
                            async () =>
                            {
                                await Task.Delay(500);
                                await GMI.WithdrawCash(999999999);
                                await Task.Delay(500);
                                _gmiWithdrawAmount++;
                                Chat.WriteLine($"Withdrew 1b, currently withdrawn {_gmiWithdrawAmount}b.");
                            });
                    }
                    else
                    {
                        _settings["Toggle"] = false;
                        Toggle = false;
                        Chat.WriteLine($"Finished withdrawing.");
                    }

                    _gmiWithdrawTimer = Time.NormalTime;
                }

                if (_settings["Toggle"].AsBool() && !Game.IsZoning
                    && _init
                    && ModeSelection.Modify == (ModeSelection)_settings["ModeSelection"].AsInt32()
                    && Time.NormalTime > _gmiUpdateTimer + 5)
                {
                    Chat.WriteLine($"Requesting..");
                    RequestGMIInventory();

                    _gmiUpdateTimer = Time.NormalTime;
                }

                if (_settings["Toggle"].AsBool()
                    && ModeSelection.Modify == (ModeSelection)_settings["ModeSelection"].AsInt32()
                    && Time.NormalTime > _mailOpenTimer + 10)
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

                            if (_mailId > 0)
                            {
                                await Task.Delay(500);
                                ReadMail(_mailId);
                                await Task.Delay(1000);
                                TakeAllMail(_mailId);
                                await Task.Delay(1000);
                                DeleteMail(_mailId);
                                await Task.Delay(1000);
                                GMI.Deposit(DynelManager.LocalPlayer.GetStat(Stat.Cash));
                                _mailId = 0;
                                await Task.Delay(2000);
                                ReadMail(0);
                                await Task.Delay(1000);
                            }
                            else
                            {
                                Chat.WriteLine($"No mail.");
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
