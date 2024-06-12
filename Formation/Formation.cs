using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using Formation.IPCMessages;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Formation
{
    public class Formation : AOPluginEntry
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
            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\Formation\\{DynelManager.LocalPlayer.Name}\\Config.json");
           
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel));

            IPCChannel.RegisterCallback((int)IPCOpcode.Formation, OnFormation);

            PluginDir = pluginDir;

            _settings = new Settings("Formation");

            Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannelChangedEvent += IPCChannel_Changed; ;

            RegisterSettingsWindow("Formation", "FormationSettingWindow.xml");

            Game.OnUpdate += OnUpdate;

            Chat.WriteLine("Formation Loaded!");
            Chat.WriteLine("/formation for settings.");

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
            _infoWindow = Window.CreateFromXml("Info", PluginDirectory + "\\UI\\FormationInfoView.xml",
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

                if (SettingsController.settingsWindow.FindView("FormationInfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = InfoView;
                }

                if (SettingsController.settingsWindow.FindView("FormationButton", out Button Formation))
                {
                    Formation.Tag = SettingsController.settingsWindow;
                    Formation.Clicked = FormationClicked;
                }
            }
        }

        private void FormationClicked(object s, ButtonBase button)
        {
            IPCChannel.Broadcast(new FormationMessage()
            { 
                Position = DynelManager.LocalPlayer.Position 
            });
        }

        public static void OnFormation(int sender, IPCMessage msg)
        {
            FormationMessage formationMsg = (FormationMessage)msg;

            Vector3 localPlayerPosition = DynelManager.LocalPlayer.Position;

            Vector3[] formationPositions = CreateLineFormation(formationMsg.Position, 1, 10);

            Random random = new Random();

            Vector3 selectedPosition = formationPositions[random.Next(formationPositions.Length)];

            Chat.WriteLine($"Position, {selectedPosition}");

            
            if (localPlayerPosition.DistanceFrom(selectedPosition) > 1 )
            {
                MovementController.Instance.SetDestination(selectedPosition);
            }
        }

        private static Vector3[] CreateLineFormation(Vector3 start, int increment, int count)
        {
            Vector3[] positions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                // Corrected to multiply increment by i
                positions[i] = new Vector3(start.X + (i * increment), start.Y, start.Z + (i * increment));
            }
            return positions;
        }
    }
}
