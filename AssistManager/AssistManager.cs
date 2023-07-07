using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;
using System.Linq;

namespace AssistManager
{
    public class AssistManager : AOPluginEntry
    {
        public static Config Config { get; private set; }

        protected Settings _settings;

        private static string AssistPlayer;

        //private static double _updateTick;
        private static double _assistTimer;

        public static Window _infoWindow;

        public static View _infoView;

        public static string PluginDir;

        public override void Run(string pluginDir)
        {
            _settings = new Settings("AssistManager");
            PluginDir = pluginDir;

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\AssistManager\\{Game.ClientInst}\\Config.json");

            Config.CharSettings[Game.ClientInst].AssistPlayerChangedEvent += AssistPlayer_Changed;

            Game.OnUpdate += OnUpdate;

            _settings.AddVariable("Toggle", false);

            Chat.RegisterCommand("toggle", (string command, string[] param, ChatWindow chatWindow) =>
            {
                _settings["Toggle"] = !_settings["Toggle"].AsBool();
                Chat.WriteLine($"Toggle : {_settings["Toggle"]}");
            });

            RegisterSettingsWindow("Assist Manager", "AssistManagerSettingWindow.xml");

            Chat.WriteLine("AssistManager Loaded!");
            Chat.WriteLine("/assistmanager for settings.");

            AssistPlayer = Config.CharSettings[Game.ClientInst].AssistPlayer;
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        public static void AssistPlayer_Changed(object s, string e)
        {
            Config.CharSettings[Game.ClientInst].AssistPlayer = e;
            AssistPlayer = e;
            //TODO: Change in config so it saves when needed to - interface name -> INotifyPropertyChanged
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
            //if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.F4) && !_init)
            //{
            //    _init = true;

            //    Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\HelpManager\\{Game.ClientInst}\\Config.json");

            //    SettingsController.settingsWindow = Window.Create(new Rect(50, 50, 300, 300), "Help Manager", "Settings", WindowStyle.Default, WindowFlags.AutoScale);

            //    if (SettingsController.settingsWindow != null && !SettingsController.settingsWindow.IsVisible)
            //    {
            //        foreach (string settingsName in SettingsController.settingsWindows.Keys.Where(x => x.Contains("Help Manager")))
            //        {
            //            SettingsController.AppendSettingsTab(settingsName, SettingsController.settingsWindow);

            //            SettingsController.settingsWindow.FindView("AssistNamedCharacter", out TextInputView assistInput);

            //            if (assistInput != null)
            //                assistInput.Text = Config.CharSettings[Game.ClientInst].AssistPlayer;
            //        }
            //    }

            //    _init = false;
            //}

            //if (Time.NormalTime > _updateTick + 8f)
            //{
            //    List<SimpleChar> PlayersInRange = DynelManager.Characters
            //        .Where(x => x.IsPlayer)
            //        .Where(x => DynelManager.LocalPlayer.DistanceFrom(x) < 30f)
            //        .ToList();

            //    foreach (SimpleChar player in PlayersInRange)
            //    {
            //        Network.Send(new CharacterActionMessage()
            //        {
            //            Action = CharacterActionType.InfoRequest,
            //            Target = player.Identity

            //        });
            //    }

            //    _updateTick = Time.NormalTime;
            //}

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("AssistNamedCharacter", out TextInputView assistInput);

                if (assistInput != null && !string.IsNullOrEmpty(assistInput.Text))
                {
                    if (Config.CharSettings[Game.ClientInst].AssistPlayer != assistInput.Text)
                    {
                        Config.CharSettings[Game.ClientInst].AssistPlayer = assistInput.Text;
                    }
                }

                if (SettingsController.settingsWindow.FindView("AssistManagerInfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = HandleInfoViewClick;
                }
            }

            if (_settings["Toggle"].AsBool()
                && Time.NormalTime > _assistTimer + 0.5)
            {
                SimpleChar identity = DynelManager.Characters
                    .Where(c => !string.IsNullOrEmpty(AssistPlayer)
                        && c.IsAlive && !c.Flags.HasFlag(CharacterFlags.Pet)
                        && c.Name == AssistPlayer)
                    .FirstOrDefault();

                if (identity == null) { return; }

                if (identity.FightingTarget == null &&
                    DynelManager.LocalPlayer.FightingTarget != null)
                {
                    DynelManager.LocalPlayer.StopAttack();

                    _assistTimer = Time.NormalTime;
                }

                if (identity.FightingTarget != null &&
                    (DynelManager.LocalPlayer.FightingTarget == null ||
                    (DynelManager.LocalPlayer.FightingTarget != null && DynelManager.LocalPlayer.FightingTarget.Identity != identity.FightingTarget.Identity)))
                {
                    DynelManager.LocalPlayer.Attack(identity.FightingTarget);

                    _assistTimer = Time.NormalTime;
                }
            }
        }
    }
}
