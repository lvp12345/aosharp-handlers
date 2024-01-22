using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using PetManager.IPCMessages;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace PetManager
{
    public class PetManager : AOPluginEntry
    {
        private static IPCChannel IPCChannel;

        public static Config Config { get; private set; }

        public static string PluginDirectory;

        public static bool _syncPets;

        public static Window _infoWindow;

        public static View _infoView;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        protected Settings _settings;

        public static string PluginDir;

        public override void Run(string pluginDir)
        {

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\PetManager\\{DynelManager.LocalPlayer.Name}\\Config.json");
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel));

            IPCChannel.RegisterCallback((int)IPCOpcode.PetAttack, OnPetAttack);
            IPCChannel.RegisterCallback((int)IPCOpcode.PetWait, OnPetWait);
            IPCChannel.RegisterCallback((int)IPCOpcode.PetFollow, OnPetFollow);
            IPCChannel.RegisterCallback((int)IPCOpcode.PetWarp, OnPetWarp);
            IPCChannel.RegisterCallback((int)IPCOpcode.PetSyncOn, SyncPetsOnMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.PetSyncOff, SyncPetsOffMessage);

            PluginDir = pluginDir;

            _settings = new Settings("PetManager");

            Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannelChangedEvent += IPCChannel_Changed; ;

            RegisterSettingsWindow("Pet Manager", "PetManagerSettingWindow.xml");

            _settings.AddVariable("SyncPets", true);

            Game.OnUpdate += OnUpdate;

            //Chat.RegisterCommand("petattack", PetAttackCommand);
            Chat.RegisterCommand("petwait", PetWaitCommand);
            Chat.RegisterCommand("petwarp", PetWarpCommand);
            Chat.RegisterCommand("petfollow", PetFollowCommand);


            if (Game.IsNewEngine)
            {
                Chat.WriteLine("Does not work on this engine!");
            }
            else
            {
                Chat.WriteLine("PetManager Loaded!");
                Chat.WriteLine("/PetManager for settings.");
            }

            PluginDirectory = pluginDir;

            //Network.N3MessageSent += Network_N3MessageSent;
        }

        public Window[] _windows => new Window[] { _infoWindow };

        public static void IPCChannel_Changed(object s, int e)
        {
            IPCChannel.SetChannelId(Convert.ToByte(e));
            Config.Save();
        }

        private void InfoView(object s, ButtonBase button)
        {
            _infoWindow = Window.CreateFromXml("Info", PluginDirectory + "\\UI\\PetManagerInfoView.xml",
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

                if (channelInput != null && !string.IsNullOrEmpty(channelInput.Text))
                {
                    if (int.TryParse(channelInput.Text, out int channelValue)
                        && Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel != channelValue)
                    {
                        Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel = channelValue;
                    }
                }

                if (SettingsController.settingsWindow.FindView("PetManagerInfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = InfoView;
                }

                //attack
                if (SettingsController.settingsWindow.FindView("PetAttack", out Button PetAttack))
                {
                    PetAttack.Tag = SettingsController.settingsWindow;
                    PetAttack.Clicked = PetAttackClicked;
                }

                //wait
                if (SettingsController.settingsWindow.FindView("PetWait", out Button PetWait))
                {
                    PetWait.Tag = SettingsController.settingsWindow;
                    PetWait.Clicked = PetWaitClicked;
                }

                //follow
                if (SettingsController.settingsWindow.FindView("PetFollow", out Button PetFollow))
                {
                    PetFollow.Tag = SettingsController.settingsWindow;
                    PetFollow.Clicked = PetFollowClicked;
                }

                //warp
                if (SettingsController.settingsWindow.FindView("PetWarp", out Button PetWarp))
                {
                    PetWarp.Tag = SettingsController.settingsWindow;
                    PetWarp.Clicked = PetWarpClicked;
                }

                if (!_settings["SyncPets"].AsBool() && _syncPets) // Farming off
                {
                    IPCChannel.Broadcast(new PetSyncOffMessage());
                    Chat.WriteLine("SyncPets disabled");
                    syncPetsOffDisabled();
                }

                if (_settings["SyncPets"].AsBool() && !_syncPets) // farming on
                {
                    IPCChannel.Broadcast(new PetSyncOnMessag());
                    Chat.WriteLine("SyncPets enabled.");
                    syncPetsOnEnabled();
                }
            }
        }

        private void syncPetsOnEnabled()
        {
            _syncPets = true;
        }
        private void syncPetsOffDisabled()
        {
            _syncPets = false;
        }

        private void SyncPetsOnMessage(int sender, IPCMessage msg)
        {
            _settings["SyncPets"] = true;
            syncPetsOnEnabled();
        }

        private void SyncPetsOffMessage(int sender, IPCMessage msg)
        {
            _settings["SyncPets"] = false;
            syncPetsOffDisabled();
        }

        //attack
        private void PetAttackClicked(object s, ButtonBase button)
        {
            IPCChannel.Broadcast(new PetAttackMessage()
            {
                Target = (Identity)Targeting.Target?.Identity
            });
        }

        public static void OnPetAttack(int sender, IPCMessage msg)
        {
            PetAttackMessage attackMsg = (PetAttackMessage)msg;
            DynelManager.LocalPlayer.Pets.Attack(attackMsg.Target);
        }

        //wait
        private void PetWaitClicked(object s, ButtonBase button)
        {
            PetWaitCommand(null, null, null);
        }

        private static void PetWaitCommand(string command, string[] param, ChatWindow chatWindow)
        {
            IPCChannel.Broadcast(new PetWaitMessage());
            OnPetWait(0, null);
        }

        private static void OnPetWait(int sender, IPCMessage msg)
        {
            if (DynelManager.LocalPlayer.Pets.Length > 0)
            {
                foreach (Pet pet in DynelManager.LocalPlayer.Pets)
                {
                    pet.Wait();
                }
            }
        }

        //warp
        private void PetWarpClicked(object s, ButtonBase button)
        {
            PetWarpCommand(null, null, null);
        }

        private static void PetWarpCommand(string command, string[] param, ChatWindow chatWindow)
        {
            IPCChannel.Broadcast(new PetWarpMessage());
            OnPetWarp(0, null);
        }

        private static void OnPetWarp(int sender, IPCMessage msg)
        {
            if (DynelManager.LocalPlayer.Pets.Length > 0)
            {
                Spell warp = Spell.List.FirstOrDefault(x => RelevantNanos.Warps.Contains(x.Id));
                if (warp != null)
                {
                    warp.Cast(DynelManager.LocalPlayer, false);
                }
            }
        }

        //follow
        private void PetFollowClicked(object s, ButtonBase button)
        {
            PetFollowCommand(null, null, null);
        }

        private void PetFollowCommand(string command, string[] param, ChatWindow chatWindow)
        {
            IPCChannel.Broadcast(new PetFollowMessage());
            OnPetFollow(0, null);
        }

        private static void OnPetFollow(int sender, IPCMessage msg)
        {
            if (DynelManager.LocalPlayer.Pets.Length > 0)
            {
                foreach (Pet pet in DynelManager.LocalPlayer.Pets)
                {
                    pet.Follow();
                }
            }
        }

        private static class RelevantNanos
        {
            public static readonly int[] Warps = {
                209488
            };
        }
    }
}
