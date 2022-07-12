using AOSharp.Common.GameData;
using AOSharp.Core.UI;
using AOSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData.UI;

namespace AssistManager
{
    public class WindowOptions
    {
        public string Name { get; set; }
        public string XmlViewName { get; set; }
        public Rect WindowSize { get; set; } = new Rect(0, 0, 240, 345);
        public WindowStyle Style { get; set; } = WindowStyle.Default;
        public WindowFlags Flags { get; set; } = WindowFlags.AutoScale | WindowFlags.NoFade;
    }

    public static class SettingsController
    {
        private static List<Settings> settingsToSave = new List<Settings>();
        public static Dictionary<string, string> settingsWindows = new Dictionary<string, string>();
        private static bool IsCommandRegistered;

        public static string _staticName = string.Empty;

        public static Window settingsWindow;
        public static View settingsView;

        public static Config Config { get; private set; }

        public static void RegisterCharacters(Settings settings)
        {
            RegisterChatCommandIfNotRegistered();
            settingsToSave.Add(settings);
        }

        public static void RegisterSettingsWindow(string settingsName, string settingsWindowPath, Settings settings)
        {
            RegisterChatCommandIfNotRegistered();
            settingsWindows[settingsName] = settingsWindowPath;
            settingsToSave.Add(settings);
        }

        public static void RegisterSettings(Settings settings)
        {
            RegisterChatCommandIfNotRegistered();
            settingsToSave.Add(settings);
        }

        public static void CleanUp()
        {
            settingsToSave.ForEach(settings => settings.Save());
        }

        private static void RegisterChatCommandIfNotRegistered()
        {
            if (!IsCommandRegistered)
            {
                Chat.RegisterCommand("assistmanager", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    try
                    {
                        Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\AssistManager\\{Game.ClientInst}\\Config.json");

                        settingsWindow = Window.Create(new Rect(50, 50, 300, 300), "Assist Manager", "Settings", WindowStyle.Default, WindowFlags.AutoScale);

                        if (settingsWindow != null && !settingsWindow.IsVisible)
                        {
                            AppendSettingsTab("Assist Manager", settingsWindow);

                            settingsWindow.FindView("ChannelBox", out TextInputView channelValue);
                            settingsWindow.FindView("AssistNamedCharacter", out TextInputView assistInput);

                            if (channelValue != null)
                                channelValue.Text = $"{Config.CharSettings[Game.ClientInst].IPCChannel}";
                            if (assistInput != null)
                                assistInput.Text = Config.CharSettings[Game.ClientInst].AssistPlayer;
                        }
                    }
                    catch (Exception e)
                    {
                        Chat.WriteLine(e);
                    }
                });

                IsCommandRegistered = true;
            }
        }

        public static void AppendSettingsTab(String settingsName, Window testWindow)
        {
            String settingsWindowXmlPath = settingsWindows[settingsName];
            View settingsView = View.CreateFromXml(settingsWindowXmlPath);

            if (settingsView != null)
            {
                testWindow.AppendTab(settingsName, settingsView);
                testWindow.Show(true);
            }
            else
            {
                Chat.WriteLine($"{settingsWindows[settingsName]}");
                Chat.WriteLine("Failed to load settings schema from " + settingsWindowXmlPath);
            }
        }
    }
}
