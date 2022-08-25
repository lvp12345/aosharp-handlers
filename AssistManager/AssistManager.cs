using System;
using System.Linq;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using AssistManager.IPCMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.GameData;
using System.Collections.Generic;
using AOSharp.Common.GameData.UI;

namespace AssistManager
{
    public class AssistManager : AOPluginEntry
    {
        private static IPCChannel IPCChannel;

        public static Config Config { get; private set; }

        protected Settings _settings;

        private static string AssistPlayer;

        private static double _updateTick;
        private static double _assistTimer;

        public static Window _infoWindow;

        public static View _infoView;

        public static string PluginDir;
        public override void Run(string pluginDir)
        {
            _settings = new Settings("AssistManager");
            PluginDir = pluginDir;

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\AssistManager\\{Game.ClientInst}\\Config.json");
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[Game.ClientInst].IPCChannel));

            Config.CharSettings[Game.ClientInst].IPCChannelChangedEvent += IPCChannel_Changed;
            Config.CharSettings[Game.ClientInst].AssistPlayerChangedEvent += AssistPlayer_Changed;
            Game.OnUpdate += OnUpdate;

            _settings.AddVariable("AttackSelection", (int)AttackSelection.None);

            IPCChannel.RegisterCallback((int)IPCOpcode.Assist, OnAssistMessage);

            RegisterSettingsWindow("Assist Manager", "AssistManagerSettingWindow.xml");

            Chat.WriteLine("AssistManager Loaded!");
            Chat.WriteLine("/assistmanager for settings.");

            AssistPlayer = Config.CharSettings[Game.ClientInst].AssistPlayer;
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        private void OnZoned(object s, EventArgs e)
        {

        }

        public static void IPCChannel_Changed(object s, int e)
        {
            IPCChannel.SetChannelId(Convert.ToByte(e));

            ////TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }
        public static void AssistPlayer_Changed(object s, string e)
        {
            Config.CharSettings[Game.ClientInst].AssistPlayer = e;

            ////TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
            Config.Save();
        }

        private void HandleInfoViewClick(object s, ButtonBase button)
        {
            _infoWindow = Window.CreateFromXml("Info", PluginDir + "\\UI\\AssistManagerInfoView.xml",
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
            if (Time.NormalTime > _updateTick + 8f)
            {
                List<SimpleChar> PlayersInRange = DynelManager.Characters
                    .Where(x => x.IsPlayer)
                    .Where(x => DynelManager.LocalPlayer.DistanceFrom(x) < 30f)
                    .ToList();

                foreach (SimpleChar player in PlayersInRange)
                {
                    Network.Send(new CharacterActionMessage()
                    {
                        Action = CharacterActionType.InfoRequest,
                        Target = player.Identity

                    });
                }

                _updateTick = Time.NormalTime;
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView channelBox);
                SettingsController.settingsWindow.FindView("AssistNamedCharacter", out TextInputView assistValue);

                if (channelBox != null && !string.IsNullOrEmpty(channelBox.Text))
                {
                    if (int.TryParse(channelBox.Text, out int channelValue))
                    {
                        Config.CharSettings[Game.ClientInst].IPCChannel = channelValue;
                    }
                }
                if (assistValue != null && assistValue.Text != String.Empty)
                {
                    if (Config.CharSettings[Game.ClientInst].AssistPlayer != assistValue.Text)
                    {
                        Config.CharSettings[Game.ClientInst].AssistPlayer = assistValue.Text;
                        Config.Save();
                    }
                }

                if (SettingsController.settingsWindow.FindView("AssistManagerInfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = HandleInfoViewClick;
                }
            }

            if (AttackSelection.Assist == (AttackSelection)_settings["AttackSelection"].AsInt32()
                && Time.NormalTime > _assistTimer + 1)
            {
                SimpleChar identity = DynelManager.Characters
                    .Where(c => !string.IsNullOrEmpty(Config.CharSettings[Game.ClientInst].AssistPlayer))
                    .Where(c => c.IsAlive)
                    .Where(x => !x.Flags.HasFlag(CharacterFlags.Pet))
                    .Where(c => c.Name == Config.CharSettings[Game.ClientInst].AssistPlayer)
                    .FirstOrDefault();

                if (identity == null) { return; }

                if (identity != null && identity.FightingTarget == null &&
                    DynelManager.LocalPlayer.FightingTarget != null)
                {
                    DynelManager.LocalPlayer.StopAttack();

                    _assistTimer = Time.NormalTime;
                }

                if (identity != null && identity.FightingTarget != null &&
                    (DynelManager.LocalPlayer.FightingTarget == null ||
                    (DynelManager.LocalPlayer.FightingTarget != null && DynelManager.LocalPlayer.FightingTarget.Identity != identity.FightingTarget.Identity)))
                {
                    DynelManager.LocalPlayer.Attack(identity.FightingTarget);

                    IPCChannel.Broadcast(new AssistMessage()
                    {
                        Target = identity.Identity
                    });
                    _assistTimer = Time.NormalTime;
                }
            }
        }

        private void OnAssistMessage(int sender, IPCMessage msg)
        {
            AssistMessage assistMessage = (AssistMessage)msg;

            Dynel targetDynel = DynelManager.GetDynel(assistMessage.Target);

            if (targetDynel != null && DynelManager.LocalPlayer.FightingTarget == null)
            {
                DynelManager.LocalPlayer.Attack(targetDynel);
            }
        }

        public enum AttackSelection
        {
            None, Assist
        }
    }
}
