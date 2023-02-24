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

        private const float PostZonePetCheckBuffer = 5;

        protected double _lastPetWaitTime = Time.NormalTime;
        protected double _lastZonedTime = Time.NormalTime;

        private bool IsActiveWindow => GetForegroundWindow() == Process.GetCurrentProcess().MainWindowHandle;

        public override void Run(string pluginDir)
        {

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\PetHelper\\{Game.ClientInst}\\Config.json");
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[Game.ClientInst].IPCChannel));

            PluginDir = pluginDir;

            _settings = new Settings("PetHelper");


            //RegisterSpellProcessor(RelevantNanos.PetWarp, PetWarp);


            Config.CharSettings[Game.ClientInst].IPCChannelChangedEvent += IPCChannel_Changed; ;

            RegisterSettingsWindow("Pet Helper", "PetHelperSettingWindow.xml");

            Game.OnUpdate += OnUpdate;

            _settings.AddVariable("Wait", false);
            _settings.AddVariable("Warp", false);
            _settings.AddVariable("Follow", false);

            Chat.RegisterCommand("petwait", PetWait);
            Chat.RegisterCommand("petwarp", Warp);
            Chat.RegisterCommand("petfollow", PetFollow);


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
                SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView channelInput);

                if (channelInput != null && !string.IsNullOrEmpty(channelInput.Text))
                {
                    if (int.TryParse(channelInput.Text, out int channelValue)
                        && Config.CharSettings[Game.ClientInst].IPCChannel != channelValue)
                    {
                        Config.CharSettings[Game.ClientInst].IPCChannel = channelValue;
                    }
                }

                if (SettingsController.settingsWindow.FindView("PetHelperInfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = InfoView;
                }
            }


            if (IsSettingEnabled("Wait"))
                PetWait();

            if (IsSettingEnabled("Warp"))
                PetWarp();

            if (IsSettingEnabled("Follow"))
                PetFollow();

        }

        private void PetWait(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["Wait"] = !_settings["Wait"].AsBool();
                Chat.WriteLine($"pet wait : {_settings["Wait"].AsBool()}");
            }
        }

        private void Warp(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["Warp"] = !_settings["Warp"].AsBool();
                Chat.WriteLine($"Warp : {_settings["Warp"].AsBool()}");
            }
        }

        private void PetFollow(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["Follow"] = !_settings["Follow"].AsBool();
                Chat.WriteLine($"pet follow : {_settings["Follow"].AsBool()}");
            }
        }

        protected bool PetWarp(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            return DynelManager.LocalPlayer.Pets.Any(c => c.Character == null);
        }

        protected void PetWait()
        {
            if (CanLookupPetsAfterZone() && Time.NormalTime - _lastPetWaitTime > 1)
            {
                foreach (Pet _pet in DynelManager.LocalPlayer.Pets.Where(c => c.Type == PetType.Attack || c.Type == PetType.Support))
                    PetWaitState(_pet);

                _lastPetWaitTime = Time.NormalTime;
            }
        }

        protected void PetWarp()
        {
            if (CanLookupPetsAfterZone() && Time.NormalTime - _lastPetWaitTime > 1) ;
            {
            }
        }

        protected void PetFollow()
        {
            if (CanLookupPetsAfterZone() && Time.NormalTime - _lastPetWaitTime > 1)
            {
                foreach (Pet _pet in DynelManager.LocalPlayer.Pets.Where(c => c.Type == PetType.Attack || c.Type == PetType.Support))
                    PetFollowState(_pet);

                _lastPetWaitTime = Time.NormalTime;
            }
        }

        private void PetWaitState(Pet pet)
        {
            pet?.Wait();
        }

        private void PetFollowState(Pet pet)
        {
            pet?.Follow();
        }

        protected bool CanLookupPetsAfterZone()
        {
            return Time.NormalTime > _lastZonedTime + PostZonePetCheckBuffer;
        }


        protected bool IsSettingEnabled(string settingName)
        {
            return _settings[settingName].AsBool();
        }

        private static class RelevantNanos
        {

            public const int PetWarp = 209488;

        }
    }
}
