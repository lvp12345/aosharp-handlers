using AOSharp.Common.GameData;
using AOSharp.Core.UI;
using AOSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData.UI;

namespace MultiboxHelper
{
    public static class SettingsController
    {
        private static List<Settings> settingsToSave = new List<Settings>();
        public static Dictionary<string, string> settingsWindows = new Dictionary<string, string>();
        private static bool IsCommandRegistered;
        public static Window settingsWindow;

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
                Chat.RegisterCommand("aosharp", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    try
                    {
                        if (param.Length > 0)
                        {
                            if ("settings" == param[0])
                            {
                                settingsWindow = Window.Create(new Rect(50, 50, 300, 300), "AOSharp", "Settings", WindowStyle.Default, WindowFlags.AutoScale);

                                foreach (string settingsName in settingsWindows.Keys)
                                {
                                    AppendSettingsTab(settingsName, settingsWindow);

                                    if (MultiboxHelper.playersname != String.Empty)
                                    {
                                        settingsWindow.FindView("FollowNamedCharacter", out TextInputView textinput);
                                        textinput.Text = MultiboxHelper.playersname;
                                    }
                                }
                            }
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
                Chat.WriteLine("Failed to load settings schema from " + settingsWindowXmlPath);
            }
        }
    }
}
