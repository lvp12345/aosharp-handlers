using System;
using System.Linq;
using System.Diagnostics;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using PetHelper.IPCMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.GameData;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AOSharp.Core.Inventory;
using AOSharp.Common.GameData.UI;
using System.Windows.Input;
using AOSharp.Common.SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using AOSharp.Core.GMI;
using Zoltu.IO;
using System.IO;
using System.Text;
using System.Windows.Media;

namespace PetHelper
{
    public class PetHelper : AOPluginEntry
    {
        private static IPCChannel IPCChannel;

        public static Config Config { get; private set; }

        public static string PluginDirectory;

        public static Window _infoWindow;

        public static View _infoView;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        protected Settings _settings;

        public static string PluginDir;

        private bool IsActiveWindow => GetForegroundWindow() == Process.GetCurrentProcess().MainWindowHandle;

        public override void Run(string pluginDir)
        {

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\PetHelper\\{Game.ClientInst}\\Config.json");
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[Game.ClientInst].IPCChannel));

            PluginDir = pluginDir;

            _settings = new Settings("PetHelper");

            Config.CharSettings[Game.ClientInst].IPCChannelChangedEvent += IPCChannel_Changed; ;

            RegisterSettingsWindow("Pet Helper", "PetHelperSettingWindow.xml");

            Game.OnUpdate += OnUpdate;

            //Chat.RegisterCommand("petwait", PetWait);
            //Chat.RegisterCommand("petwarp", PetWarp);
            //Chat.RegisterCommand("petfollow", PetFollow);


            Chat.WriteLine("PetHelper Loaded!");
            Chat.WriteLine("/Pethelper for settings.");

            PluginDirectory = pluginDir;
        }

        public Window[] _windows => new Window[] { _infoWindow };

        public static void IPCChannel_Changed(object s, int e)
        {
            IPCChannel.SetChannelId(Convert.ToByte(e));
            Config.Save();
        }

        private void InfoView(object s, ButtonBase button)
        {
            _infoWindow = Window.CreateFromXml("Info", PluginDirectory + "\\UI\\PetHelperInfoView.xml",
                windowSize: new Rect(0, 0, 140, 300),
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
                SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView channelInput);
                { 
                    if (channelInput != null && !string.IsNullOrEmpty(channelInput.Text))
                    {
                        if (int.TryParse(channelInput.Text, out int channelValue)
                            && Config.CharSettings[Game.ClientInst].IPCChannel != channelValue)
                        {
                            Config.CharSettings[Game.ClientInst].IPCChannel = channelValue;
                        }
                    }
                }

                if (SettingsController.settingsWindow.FindView("PetHelperInfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = InfoView;
                }

                //wait
                if (SettingsController.settingsWindow.FindView("PetWait", out Button PetWait))
                {
                    PetWait.Tag = SettingsController.settingsWindow;
                    PetWait.Clicked += PetWaitClicked;
                }

                //warp
                if (SettingsController.settingsWindow.FindView("PetWarp", out Button PetWarp))
                {
                    PetWarp.Tag = SettingsController.settingsWindow;
                    PetWarp.Clicked += PetWarpClicked;
                }

                //follow
                if (SettingsController.settingsWindow.FindView("PetFollow", out Button PetFollow))
                {
                    PetFollow.Tag = SettingsController.settingsWindow;
                    PetFollow.Clicked += PetFollowClicked;
                }
            }
        }


        //private void PetWait(string command, string[] param, ChatWindow chatWindow)
        //{
        //    if (param.Length == 0)
        //    {
        //        _settings["PetWait"] = !_settings["PetWait"].AsBool();
        //        Chat.WriteLine($"Pet Wait : {_settings["PetWait"].AsBool()}");
        //    }
        //}

        //private void PetWarp(string command, string[] param, ChatWindow chatWindow)
        //{
        //    if (param.Length == 0)
        //    {
        //        _settings["PetWarp"] = !_settings["PetWarp"].AsBool();
        //        Chat.WriteLine($"Pet Warp : {_settings["PetWarp"].AsBool()}");
        //    }
        //}

        //private void PetFollow(string command, string[] param, ChatWindow chatWindow)
        //{
        //    if (param.Length == 0)
        //    {
        //        _settings["PetFollow"] = !_settings["PetFollow"].AsBool();
        //        Chat.WriteLine($"Pet Follow : {_settings["PetFollow"].AsBool()}");
        //    }
        //}



        //wait
        private void PetWaitClicked(object s, ButtonBase button)
        {
            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
                pet.Wait();
        }

        //warp
        private void PetWarpClicked(object s, ButtonBase button)
        {
            Spell spell = Spell.List.FirstOrDefault(c => c.Id == 209488);

            if ((bool)(spell?.IsReady))
                spell?.Cast(DynelManager.LocalPlayer, false);
        }

        //follow
        private void PetFollowClicked(object s, ButtonBase button)
        {
            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
                pet.Follow();
        }
    }
}
