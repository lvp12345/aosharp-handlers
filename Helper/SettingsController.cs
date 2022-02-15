using AOSharp.Common.GameData;
using AOSharp.Core.UI;
using AOSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData.UI;

namespace Helper
{
    public static class SettingsController
    {
        private static List<AOSharp.Core.Settings> settingsToSave = new List<AOSharp.Core.Settings>();
        public static Dictionary<string, string> settingsWindows = new Dictionary<string, string>();
        private static bool IsCommandRegistered;

        public static Window settingsWindow;
        public static View settingsView;

        public static string HelperAssistPlayer = String.Empty;
        public static string HelperFollowPlayer = String.Empty;
        public static string HelperNavFollowPlayer = String.Empty;

        public static string HelperChannel = String.Empty;

        public static int HelperNavFollowDistance = 0;

        public static Dictionary<Identity, int> RemainingNCU = new Dictionary<Identity, int>();


        public static int GetRemainingNCU(Identity target)
        {
            return RemainingNCU.ContainsKey(target) ? RemainingNCU[target] : 0;
        }

        public static Identity[] GetRegisteredCharacters()
        {
            return RemainingNCU.Keys.ToArray();
        }

        public static bool IsCharacterRegistered(Identity target)
        {
            return RemainingNCU.ContainsKey(target);
        }

        public static void RegisterCharacters(AOSharp.Core.Settings settings)
        {
            RegisterChatCommandIfNotRegistered();
            settingsToSave.Add(settings);
        }

        public static void RegisterSettingsWindow(string settingsName, string settingsWindowPath, AOSharp.Core.Settings settings)
        {
            RegisterChatCommandIfNotRegistered();
            settingsWindows[settingsName] = settingsWindowPath;
            settingsToSave.Add(settings);
        }

        public static void RegisterSettings(AOSharp.Core.Settings settings)
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
                Chat.RegisterCommand("helper", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    try
                    {
                        settingsWindow = Window.Create(new Rect(50, 50, 300, 300), "Helper", "Settings", WindowStyle.Default, WindowFlags.AutoScale);

                        foreach (string settingsName in settingsWindows.Keys)
                        {
                            AppendSettingsTab(settingsName, settingsWindow);

                            if (HelperChannel != String.Empty)
                            {
                                settingsWindow.FindView("ChannelBox", out TextInputView textinput);

                                if (textinput != null)
                                    textinput.Text = HelperChannel;
                            }

                            if (HelperNavFollowDistance.ToString() != String.Empty)
                            {
                                settingsWindow.FindView("NavFollowDistanceBox", out TextInputView textinput);

                                if (textinput != null)
                                    textinput.Text = HelperNavFollowDistance.ToString();
                            }

                            if (HelperAssistPlayer != String.Empty)
                            {
                                settingsWindow.FindView("AssistNamedCharacter", out TextInputView textinput);

                                if (textinput != null)
                                    textinput.Text = HelperAssistPlayer;
                            }

                            if (HelperFollowPlayer != String.Empty)
                            {
                                settingsWindow.FindView("FollowNamedCharacter", out TextInputView textinput);

                                if (textinput != null)
                                    textinput.Text = HelperFollowPlayer;
                            }

                            if (HelperNavFollowPlayer != String.Empty)
                            {
                                settingsWindow.FindView("FollowNamedIdentity", out TextInputView textinput);

                                if (textinput != null)
                                    textinput.Text = HelperNavFollowPlayer;
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
            settingsView = View.CreateFromXml(settingsWindowXmlPath);
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
