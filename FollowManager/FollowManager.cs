using System;
using System.Linq;
using System.Diagnostics;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using FollowManager.IPCMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.GameData;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AOSharp.Core.Inventory;
using AOSharp.Common.GameData.UI;
using System.Windows.Input;

namespace FollowManager
{
    public class FollowManager : AOPluginEntry
    {
        private static IPCChannel IPCChannel;

        public static Config Config { get; private set; }

        private static string FollowPlayer;
        private static string NavFollowIdentity;
        private static int NavFollowDistance;

        private static double _followTimer;

        private static bool _init = false;

        public static Window _infoWindow;

        public static View _infoView;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        protected Settings _settings;

        public static string PluginDir;

        public override void Run(string pluginDir)
        {
            _settings = new Settings("FollowManager");
            PluginDir = pluginDir;

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\AOSP\\FollowManager\\{Game.ClientInst}\\Config.json");
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[Game.ClientInst].IPCChannel));

            IPCChannel.RegisterCallback((int)IPCOpcode.Follow, OnFollowMessage);

            Config.CharSettings[Game.ClientInst].IPCChannelChangedEvent += IPCChannel_Changed;
            Config.CharSettings[Game.ClientInst].FollowPlayerChangedEvent += FollowPlayer_Changed;
            Config.CharSettings[Game.ClientInst].NavFollowIdentityChangedEvent += NavFollowIdentity_Changed;
            Config.CharSettings[Game.ClientInst].NavFollowDistanceChangedEvent += NavFollowDistance_Changed;

            RegisterSettingsWindow("Follow Manager", "FollowManagerSettingWindow.xml");

            Game.OnUpdate += OnUpdate;

            _settings.AddVariable("Toggle", false);

            Chat.RegisterCommand("toggle", (string command, string[] param, ChatWindow chatWindow) =>
            {
                _settings["Toggle"] = !_settings["Toggle"].AsBool();
                Chat.WriteLine($"Toggle : {_settings["Toggle"]}");
            });

            _settings.AddVariable("FollowSelection", (int)FollowSelection.None);

            Chat.WriteLine("FollowManager Loaded!");
            Chat.WriteLine("/followmanager for settings.");


            FollowPlayer = Config.CharSettings[Game.ClientInst].FollowPlayer;
            NavFollowIdentity = Config.CharSettings[Game.ClientInst].NavFollowIdentity;
            NavFollowDistance = Config.CharSettings[Game.ClientInst].NavFollowDistance;
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        public static void IPCChannel_Changed(object s, int e)
        {
            IPCChannel.SetChannelId(Convert.ToByte(e));
            Config.Save();
        }
        public static void FollowPlayer_Changed(object s, string e)
        {
            Config.CharSettings[Game.ClientInst].FollowPlayer = e;
            FollowPlayer = e;
            Config.Save();
        }
        public static void NavFollowIdentity_Changed(object s, string e)
        {
            Config.CharSettings[Game.ClientInst].NavFollowIdentity = e;
            NavFollowIdentity = e;
            Config.Save();
        }
        public static void NavFollowDistance_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].NavFollowDistance = e;
            NavFollowDistance = e;
            Config.Save();
        }

        private void HandleInfoViewClick(object s, ButtonBase button)
        {
            _infoWindow = Window.CreateFromXml("Info", PluginDir + "\\UI\\FollowManagerInfoView.xml",
                windowSize: new Rect(0, 0, 440, 510),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            _infoWindow.Show(true);
        }

        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDir + "\\UI\\" + xmlName, _settings);
        }


        private void OnUpdate(object s, float deltaTime)
        {
            //if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.F3) && !_init)
            //{
            //    _init = true;

            //    Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\AOSP\\FollowManager\\{Game.ClientInst}\\Config.json");

            //    SettingsController.settingsWindow = Window.Create(new Rect(50, 50, 300, 300), "Follow Manager", "Settings", WindowStyle.Default, WindowFlags.AutoScale);

            //    if (SettingsController.settingsWindow != null && !SettingsController.settingsWindow.IsVisible)
            //    {
            //        foreach (string settingsName in SettingsController.settingsWindows.Keys.Where(x => x.Contains("Follow Manager")))
            //        {
            //            SettingsController.AppendSettingsTab(settingsName, SettingsController.settingsWindow);

            //            SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView channelInput);
            //            SettingsController.settingsWindow.FindView("FollowNamedCharacter", out TextInputView followBox);
            //            SettingsController.settingsWindow.FindView("FollowNamedIdentity", out TextInputView navFollowBox);
            //            SettingsController.settingsWindow.FindView("NavFollowDistanceBox", out TextInputView navFollowDistanceBox);

            //            if (channelInput != null)
            //                channelInput.Text = $"{Config.CharSettings[Game.ClientInst].IPCChannel}";
            //            if (followBox != null)
            //                followBox.Text = $"{Config.CharSettings[Game.ClientInst].FollowPlayer}";
            //            if (navFollowBox != null)
            //                navFollowBox.Text = $"{Config.CharSettings[Game.ClientInst].NavFollowIdentity}";
            //            if (navFollowDistanceBox != null)
            //                navFollowDistanceBox.Text = $"{Config.CharSettings[Game.ClientInst].NavFollowDistance}";
            //        }
            //    }

            //    _init = false;
            //}

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView channelInput);
                SettingsController.settingsWindow.FindView("FollowNamedCharacter", out TextInputView followInput);
                SettingsController.settingsWindow.FindView("FollowNamedIdentity", out TextInputView navFollowInput);
                SettingsController.settingsWindow.FindView("NavFollowDistanceBox", out TextInputView navFollowDistanceInput);

                if (channelInput != null && !string.IsNullOrEmpty(channelInput.Text))
                {
                    if (int.TryParse(channelInput.Text, out int channelValue)
                        && Config.CharSettings[Game.ClientInst].IPCChannel != channelValue)
                    {
                        Config.CharSettings[Game.ClientInst].IPCChannel = channelValue;
                    }
                }
                if (followInput != null && !string.IsNullOrEmpty(followInput.Text))
                {
                    Config.CharSettings[Game.ClientInst].FollowPlayer = followInput.Text;
                }
                if (navFollowInput != null && !string.IsNullOrEmpty(navFollowInput.Text))
                {
                    Config.CharSettings[Game.ClientInst].NavFollowIdentity = navFollowInput.Text;
                }
                if (navFollowDistanceInput != null && !string.IsNullOrEmpty(navFollowDistanceInput.Text))
                {
                    if (int.TryParse(navFollowDistanceInput.Text, out int navFollowDistanceValue)
                        && Config.CharSettings[Game.ClientInst].NavFollowDistance != navFollowDistanceValue)
                    {
                        Config.CharSettings[Game.ClientInst].NavFollowDistance = navFollowDistanceValue;
                    }
                }

                if (SettingsController.settingsWindow.FindView("FollowManagerInfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = HandleInfoViewClick;
                }
            }

            if (_settings["Toggle"].AsBool() && FollowSelection.LeadFollow == (FollowSelection)_settings["FollowSelection"].AsInt32()
                && Time.NormalTime > _followTimer + 1)
            {
                IPCChannel.Broadcast(new FollowMessage()
                {
                    Target = DynelManager.LocalPlayer.Identity
                });
                _followTimer = Time.NormalTime;
            }

            if (_settings["Toggle"].AsBool() && FollowSelection.NavFollow == (FollowSelection)_settings["FollowSelection"].AsInt32()
                && Time.NormalTime > _followTimer + 1)
            {
                Dynel identity = DynelManager.AllDynels
                    .Where(x => !string.IsNullOrEmpty(NavFollowIdentity) 
                        && !x.Flags.HasFlag(CharacterFlags.Pet) 
                        && x.Name == NavFollowIdentity)
                    .FirstOrDefault();

                if (identity != null)
                {
                    if (DynelManager.LocalPlayer.DistanceFrom(identity) <= NavFollowDistance)
                        MovementController.Instance.Halt();

                    if (DynelManager.LocalPlayer.DistanceFrom(identity) > NavFollowDistance)
                        MovementController.Instance.SetDestination(identity.Position);

                    _followTimer = Time.NormalTime;
                }
            }

            if (_settings["Toggle"].AsBool() && FollowSelection.NamedFollow == (FollowSelection)_settings["FollowSelection"].AsInt32()
                && Time.NormalTime > _followTimer + 1)
            {
                Dynel identity = DynelManager.AllDynels
                    .Where(x => !string.IsNullOrEmpty(FollowPlayer) 
                        && !x.Flags.HasFlag(CharacterFlags.Pet) 
                        && x.Name == FollowPlayer)
                    .FirstOrDefault();

                if (identity != null)
                {
                    if (identity.Identity != DynelManager.LocalPlayer.Identity)
                        NamedFollow(identity);

                    _followTimer = Time.NormalTime;
                }
            }
        }
        private void NamedFollow(Dynel dynel)
        {
            MovementController.Instance.Follow(dynel.Identity);
            //FollowTargetMessage n3Msg = new FollowTargetMessage()
            //{
            //    Target = dynel.Identity,
            //    Unknown1 = 0,
            //    Unknown2 = 0,
            //    Unknown3 = 0,
            //    Unknown4 = 0,
            //    Unknown5 = 0,
            //    Unknown6 = 0,
            //    Unknown7 = 0
            //};
            //Network.Send(n3Msg);
            MovementController.Instance.SetMovement(MovementAction.Update);
        }

        private void OnFollowMessage(int sender, IPCMessage msg)
        {
            FollowMessage followMessage = (FollowMessage)msg;
            //FollowTargetMessage n3Msg = new FollowTargetMessage()
            //{
            //    Target = followMessage.Target,
            //    Unknown1 = 0,
            //    Unknown2 = 0,
            //    Unknown3 = 0,
            //    Unknown4 = 0,
            //    Unknown5 = 0,
            //    Unknown6 = 0,
            //    Unknown7 = 0
            //};
            //Network.Send(n3Msg);
            MovementController.Instance.Follow(followMessage.Target);
            MovementController.Instance.SetMovement(MovementAction.Update);
        }

        public enum FollowSelection
        {
            None, LeadFollow, NamedFollow, NavFollow
        }
    }
}
