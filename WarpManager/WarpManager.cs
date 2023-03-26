using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using System;
using System.Runtime.InteropServices;
using WarpManager.IPCMessages;

namespace WarpManager
{
    public class WarpManager : AOPluginEntry
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

        public override void Run(string pluginDir)
        {

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\WarpManager\\{Game.ClientInst}\\Config.json");
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[Game.ClientInst].IPCChannel));

            IPCChannel.RegisterCallback((int)IPCOpcode.WarpToTarget, OnWarpToTarget);

            PluginDir = pluginDir;

            _settings = new Settings("WarpManager");

            Config.CharSettings[Game.ClientInst].IPCChannelChangedEvent += IPCChannel_Changed; ;

            RegisterSettingsWindow("Warp Manager", "WarpManagerSettingWindow.xml");

            Game.OnUpdate += OnUpdate;

            Chat.RegisterCommand("warptotarget", WarpToTargetCommand);


            Chat.WriteLine("WarpManager Loaded!");
            Chat.WriteLine("/warpmanager for settings.");

            PluginDirectory = pluginDir;
        }

        public static void IPCChannel_Changed(object s, int e)
        {
            IPCChannel.SetChannelId(Convert.ToByte(e));
            Config.Save();
        }

        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDir + "\\UI\\" + xmlName, _settings);
        }

        private void WarpToTargetClicked(object s, ButtonBase button)
        {
            WarpToTargetCommand(null, null, null);
        }

                //        if (SettingsController.settingsWindow.FindView("warpToTargetButton", out ButtonBase warpToTargetButton))
                //{
                //    warpToTargetButton.Clicked += WarpManagerComponent.WarpToTargetButtonCallBack;
                //}

    private static void WarpToTargetCommand(string command, string[] param, ChatWindow chatWindow)
        {
            IPCChannel.Broadcast(new WarpToTargetMessage());
            OnWarpToTarget(0, null);
        }

        private static void OnWarpToTarget(int sender, IPCMessage msg)
        {
            if (DynelManager.LocalPlayer.Pets.Length > 0)
            {
                foreach (Pet pet in DynelManager.LocalPlayer.Pets)
                {
                    pet.Wait();
                }
            }
        }


        private void OnUpdate(object s, float deltaTime)
        {
            if(SettingsController.settingsWindow != null)
                SettingsController.UIUpdate();
        }

        public void Teardown()
        {
            throw new NotImplementedException();
        }
    }
}


