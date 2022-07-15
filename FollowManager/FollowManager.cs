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

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\FollowManager\\{Game.ClientInst}\\Config.json");
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[Game.ClientInst].IPCChannel));

            IPCChannel.RegisterCallback((int)IPCOpcode.Follow, OnFollowMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.NavFollow, OnNavFollowMessage);

            Config.CharSettings[Game.ClientInst].IPCChannelChangedEvent += IPCChannel_Changed;
            Config.CharSettings[Game.ClientInst].FollowPlayerChangedEvent += FollowPlayer_Changed;
            Config.CharSettings[Game.ClientInst].NavFollowIdentityChangedEvent += NavFollowIdentity_Changed;
            Config.CharSettings[Game.ClientInst].NavFollowDistanceChangedEvent += NavFollowDistance_Changed;

            RegisterSettingsWindow("Follow Manager", "FollowManagerSettingWindow.xml");

            Game.OnUpdate += OnUpdate;

            _settings.AddVariable("FollowSelection", (int)FollowSelection.None);

            Chat.RegisterCommand("leadfollow", LeadFollowSwitch);

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

            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }
        public static void FollowPlayer_Changed(object s, string e)
        {
            Config.CharSettings[Game.ClientInst].FollowPlayer = e;

            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }
        public static void NavFollowIdentity_Changed(object s, string e)
        {
            Config.CharSettings[Game.ClientInst].NavFollowIdentity = e;

            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }
        public static void NavFollowDistance_Changed(object s, int e)
        {
            Config.CharSettings[Game.ClientInst].NavFollowDistance = e;

            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
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
            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView channelBox);
                SettingsController.settingsWindow.FindView("FollowNamedCharacter", out TextInputView followBox);
                SettingsController.settingsWindow.FindView("FollowNamedIdentity", out TextInputView navFollowBox);
                SettingsController.settingsWindow.FindView("NavFollowDistanceBox", out TextInputView navFollowDistanceBox);

                if (channelBox != null && !string.IsNullOrEmpty(channelBox.Text))
                {
                    if (int.TryParse(channelBox.Text, out int channelValue)
                        && Config.CharSettings[Game.ClientInst].IPCChannel != channelValue)
                    {
                        Config.CharSettings[Game.ClientInst].IPCChannel = channelValue;
                    }
                }
                if (followBox != null && !string.IsNullOrEmpty(followBox.Text))
                {
                    Config.CharSettings[Game.ClientInst].FollowPlayer = followBox.Text;
                }
                if (navFollowBox != null && !string.IsNullOrEmpty(navFollowBox.Text))
                {
                    Config.CharSettings[Game.ClientInst].NavFollowIdentity = navFollowBox.Text;
                }
                if (navFollowDistanceBox != null && !string.IsNullOrEmpty(navFollowDistanceBox.Text))
                {
                    if (int.TryParse(navFollowDistanceBox.Text, out int navFollowDistanceValue))
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

            if (FollowSelection.LeadFollow == (FollowSelection)_settings["FollowSelection"].AsInt32()
                && Time.NormalTime > _followTimer + 1)
            {
                IPCChannel.Broadcast(new FollowMessage()
                {
                    Target = DynelManager.LocalPlayer.Identity
                });
                _followTimer = Time.NormalTime;
            }

            if (FollowSelection.NavFollow == (FollowSelection)_settings["FollowSelection"].AsInt32()
                && Time.NormalTime > _followTimer + 1)
            {
                Dynel identity = DynelManager.AllDynels
                    .Where(x => !x.Flags.HasFlag(CharacterFlags.Pet))
                    .Where(x => !string.IsNullOrEmpty(Config.CharSettings[Game.ClientInst].NavFollowIdentity))
                    .Where(x => x.Name == Config.CharSettings[Game.ClientInst].NavFollowIdentity)
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

            if (FollowSelection.OSFollow == (FollowSelection)_settings["FollowSelection"].AsInt32()
                && Time.NormalTime > _followTimer + 1)
            {
                Dynel identity = DynelManager.AllDynels
                    .Where(x => !x.Flags.HasFlag(CharacterFlags.Pet))
                    .Where(x => !string.IsNullOrEmpty(Config.CharSettings[Game.ClientInst].FollowPlayer))
                    .Where(x => x.Name == Config.CharSettings[Game.ClientInst].FollowPlayer)
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

        private void LeadFollowSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["Follow"] = !_settings["Follow"].AsBool();
                Chat.WriteLine($"Lead follow : {_settings["Follow"].AsBool()}");
            }
        }

        public enum FollowSelection
        {
            None, LeadFollow, OSFollow, NavFollow
        }
    }
}
