using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Common.SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using AOSharp.Core;
using AOSharp.Core.UI;
using SmokeLounge.AOtomation.Messaging.Messages;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zoltu.IO;

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

        [Obsolete]
        public override void Run(string pluginDir)
        {
            _settings = new Settings("MailManager");
            PluginDir = pluginDir;

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\MailManager\\{DynelManager.LocalPlayer.Name}\\Config.json");

            Config.CharSettings[DynelManager.LocalPlayer.Name].MailCharacterNameChangedEvent += MailCharacterName_Changed;
            Config.CharSettings[DynelManager.LocalPlayer.Name].MailAmountChangedEvent += MailAmount_Changed;
            Network.PacketReceived += HandleMail;

            Game.OnUpdate += OnUpdate;

            _settings.AddVariable("Enable", false);

            Chat.RegisterCommand("mail", (string command, string[] param, ChatWindow chatWindow) =>
            {
                if (!string.IsNullOrEmpty(MailCharacterName) && MailAmount > 0)
                {
                    _settings["Enable"] = !_settings["Enable"].AsBool();
                    Chat.WriteLine("Sending..");
                }
            });

            RegisterSettingsWindow("Mail Manager", $"MailManagerSettingWindow.xml");

            if (Game.IsNewEngine)
            {
                Chat.WriteLine("Does not work on this engine!");
            }
            else
            {
                Chat.WriteLine("Mail Manager Loaded!");
                Chat.WriteLine("/mailmanager for settings.");
            }

            _settings["Enable"] = false;

            MailCharacterName = Config.CharSettings[DynelManager.LocalPlayer.Name].MailCharacterName;
            MailAmount = Config.CharSettings[DynelManager.LocalPlayer.Name].MailAmount;
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
            Config.CharSettings[DynelManager.LocalPlayer.Name].MailCharacterName = e;
            MailCharacterName = e;
            Config.Save();
        }

        public static void MailAmount_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].MailAmount = e;
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
                    {
                        PopMail(reader);
                    }
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
            {
                _mailId = messageId;
            }
        }


        private void OnUpdate(object s, float deltaTime)
        {
            if (!_settings["Enable"].AsBool() || Game.IsZoning) { return; }
            else
            {
                if (Time.AONormalTime > _mailOpenTimer)
                {
                    if (DynelManager.LocalPlayer.GetStat(Stat.Cash) == 0)
                    {
                        Task.Factory.StartNew(
                            async () =>
                            {
                                if (!_init)
                                {
                                    Dynel _terminal = DynelManager.AllDynels.FirstOrDefault(c => c.Identity == IdentityType.MailTerminal);
                                    await Task.Delay(1000);
                                    _terminal?.Use();

                                    _currentMailAmount = 0;
                                    _init = true;
                                }

                                if (_mailId > 0)
                                {
                                    _currentMailAmount++;

                                    if (_currentMailAmount >= MailAmount)
                                    {
                                        Chat.WriteLine($"Reached mail amount {MailAmount}.");
                                        _settings["Enable"] = false;
                                    }

                                    Chat.WriteLine("Handling mail..");
                                    await Task.Delay(1000);
                                    ReadMail(_mailId);
                                    Chat.WriteLine($"ReadMail: {_mailId}");
                                    await Task.Delay(1100);
                                    TakeAllMail(_mailId);
                                    Chat.WriteLine($"TakeAllMail: {_mailId}");
                                    await Task.Delay(1100);
                                    DeleteMail(_mailId);
                                    Chat.WriteLine($"DeleteMail: {_mailId}");
                                    await Task.Delay(1100);
                                    _mailId = 0;
                                    await Task.Delay(2000);
                                    ReadMail(0);
                                    await Task.Delay(1000);
                                }
                            });
                    }

                    _mailOpenTimer = Time.AONormalTime + 3;
                }

                if (Time.AONormalTime > _mailSendTimer)
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

                    _mailSendTimer = Time.AONormalTime + 1;
                }
            }

            if (!_settings["Enable"].AsBool() && _init)
            {
                _currentMailAmount = 0;
                _init = false;
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("MailCharacterName", out TextInputView mailCharacterNameInput);
                SettingsController.settingsWindow.FindView("MailAmount", out TextInputView mailAmountInput);

                if (mailCharacterNameInput != null && !string.IsNullOrEmpty(mailCharacterNameInput.Text))
                {
                    if (Config.CharSettings[DynelManager.LocalPlayer.Name].MailCharacterName != mailCharacterNameInput.Text)
                    {
                        Config.CharSettings[DynelManager.LocalPlayer.Name].MailCharacterName = mailCharacterNameInput.Text;
                    }
                }

                if (mailAmountInput != null && !string.IsNullOrEmpty(mailAmountInput.Text))
                {
                    if (int.TryParse(mailAmountInput.Text, out int mailAmountValue)
                        && Config.CharSettings[DynelManager.LocalPlayer.Name].MailAmount != mailAmountValue)
                    {
                        Config.CharSettings[DynelManager.LocalPlayer.Name].MailAmount = mailAmountValue;
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
