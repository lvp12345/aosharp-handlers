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

namespace MailManager
{
    public class MailManager : AOPluginEntry
    {
        public static Config Config { get; private set; }

        private static string MailCharacterName;
        private static int MailAmount;

        protected Settings _settings;

        private static double _mailOpenTimer;
        private static double _mailSendTimer;
        private static int _mailId = 0;

        private static bool _init = false;

        public static Window _infoWindow;

        public static View _infoView;

        public static string PluginDir;
        private static int _currentMailAmount = 0;

        public override void Run(string pluginDir)
        {
            _settings = new Settings("MailManager");
            PluginDir = pluginDir;

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\MailManager\\{Game.ClientInst}\\Config.json");

            Config.CharSettings[Game.ClientInst].MailCharacterNameChangedEvent += MailCharacterName_Changed;
            Config.CharSettings[Game.ClientInst].MailAmountChangedEvent += MailAmount_Changed;
            Network.PacketReceived += HandleMail;

            Game.OnUpdate += OnUpdate;

            _settings.AddVariable("Toggle", false);

            Chat.RegisterCommand("mail", (string command, string[] param, ChatWindow chatWindow) =>
            {
                if (!string.IsNullOrEmpty(MailCharacterName) && MailAmount > 0)
                {
                    _settings["Toggle"] = !_settings["Toggle"].AsBool();
                    Chat.WriteLine("Sending..");
                }
            });

            RegisterSettingsWindow("Mail Manager", $"MailManagerSettingWindow.xml");

            Chat.WriteLine("Mail Manager Loaded!");
            Chat.WriteLine("/mailmanager for settings.");

            _settings["Toggle"] = false;

            MailCharacterName = Config.CharSettings[Game.ClientInst].MailCharacterName;
            MailAmount = Config.CharSettings[Game.ClientInst].MailAmount;
        }

        private void HandleInfoViewClick(object s, ButtonBase button)
        {
            _infoWindow = Window.CreateFromXml("Info", PluginDir + "\\UI\\MailManagerInfoView.xml",
                windowSize: new Rect(0, 0, 440, 510),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            _infoWindow.Show(true);
        }

        public static void MailCharacterName_Changed(object s, string e)
        {
            Config.CharSettings[Game.ClientInst].MailCharacterName = e;
            MailCharacterName = e;
            Config.Save();
        }

        public static void MailAmount_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].MailAmount = e;
            MailAmount = e;
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



        private void OnUpdate(object s, float deltaTime)
        {
            if (!_settings["Toggle"].AsBool() && _init)
            {
                _currentMailAmount = 0;
                _init = false;
            }

            if (_settings["Toggle"].AsBool() && !Game.IsZoning && Time.NormalTime > _mailOpenTimer + 10)
            {
                if (DynelManager.LocalPlayer.GetStat(Stat.Cash) == 0)
                {
                    Task.Factory.StartNew(
                        async () =>
                        {
                            if (!_init)
                            {
                                Dynel _terminal = DynelManager.AllDynels.FirstOrDefault(c => c.Name == "Mail Terminal");
                                await Task.Delay(500);
                                if (_terminal != null)
                                    _terminal.Use();

                                _currentMailAmount = 0;
                                _init = true;
                            }

                            if (_mailId > 0 && _init)
                            {
                                _currentMailAmount++;
                                if (_currentMailAmount >= MailAmount)
                                {
                                    Chat.WriteLine($"Reached mail amount {MailAmount}.");
                                    _settings["Toggle"] = false;
                                }

                                Chat.WriteLine("Handling mail..");
                                await Task.Delay(500);
                                ReadMail(_mailId);
                                await Task.Delay(1000);
                                TakeAllMail(_mailId);
                                await Task.Delay(1000);
                                DeleteMail(_mailId);
                                await Task.Delay(1000);
                                _mailId = 0;
                                await Task.Delay(2000);
                                ReadMail(0);
                                await Task.Delay(1000);
                            }
                            else if (_mailId == 0)
                            {
                                Chat.WriteLine($"No mail.");
                                _settings["Toggle"] = false;
                            }
                        });
                }

                _mailOpenTimer = Time.NormalTime;
            }

            if (_settings["Toggle"].AsBool() && !Game.IsZoning && Time.NormalTime > _mailSendTimer + 3)
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.ComputerLiteracy))
                {
                    if (DynelManager.LocalPlayer.GetStat(Stat.Cash) > 0)
                    {
                        Chat.WriteLine($"Sent {DynelManager.LocalPlayer.GetStat(Stat.Cash) - 200000} credits to {MailCharacterName}. {_currentMailAmount}/{MailAmount}");

                        Network.Send(new MailMessage()
                        {
                            Unknown1 = 06,
                            Recipient = $"{MailCharacterName}",
                            Subject = "Sending creds.",
                            Body = $"I've sent you {DynelManager.LocalPlayer.GetStat(Stat.Cash) - 200000} credits.",
                            Item = Identity.None,
                            Credits = DynelManager.LocalPlayer.GetStat(Stat.Cash) - 200000,
                            Express = true
                        });
                    }
                }

                _mailSendTimer = Time.NormalTime;
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("MailCharacterName", out TextInputView mailCharacterNameInput);
                SettingsController.settingsWindow.FindView("MailAmount", out TextInputView mailAmountInput);

                if (mailCharacterNameInput != null && !string.IsNullOrEmpty(mailCharacterNameInput.Text))
                {
                    if (Config.CharSettings[Game.ClientInst].MailCharacterName != mailCharacterNameInput.Text)
                    {
                        Config.CharSettings[Game.ClientInst].MailCharacterName = mailCharacterNameInput.Text;
                    }
                }

                if (mailAmountInput != null && !string.IsNullOrEmpty(mailAmountInput.Text))
                {
                    if (int.TryParse(mailAmountInput.Text, out int mailAmountValue) 
                        && Config.CharSettings[Game.ClientInst].MailAmount != mailAmountValue)
                    {
                        Config.CharSettings[Game.ClientInst].MailAmount = mailAmountValue;
                    }
                }

                if (SettingsController.settingsWindow.FindView("MailManagerInfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = HandleInfoViewClick;
                }
            }
        }
    }
}
