using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;

namespace GMIManager
{
    public static class SettingsController
    {
        private static List<Settings> settingsToSave = new List<Settings>();
        public static Dictionary<string, string> settingsWindows = new Dictionary<string, string>();
        private static bool IsCommandRegistered;

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
                Chat.RegisterCommand("gmimanager", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    try
                    {
                        Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\GMIManager\\{Game.ClientInst}\\Config.json");

                        settingsWindow = Window.Create(new Rect(50, 50, 300, 300), "GMI Manager", "Settings", WindowStyle.Default, WindowFlags.AutoScale);

                        if (settingsWindow != null && !settingsWindow.IsVisible)
                        {
                            AppendSettingsTab("GMI Manager", settingsWindow);

                            settingsWindow.FindView("GMIWithdrawAmount", out TextInputView gMIWithdrawAmount);
                            settingsWindow.FindView("GMIBuyOrdersName", out TextInputView gMIBuyOrdersName);
                            settingsWindow.FindView("GMIBuyOrdersEndPrice", out TextInputView gMIBuyOrdersEndPrice);
                            settingsWindow.FindView("GMIItemName", out TextInputView gMIItemName);

                            if (gMIBuyOrdersName != null)
                            {
                                gMIBuyOrdersName.Text = $"{Config.CharSettings[Game.ClientInst].GMIBuyOrderName}";
                            }

                            if (gMIBuyOrdersEndPrice != null)
                            {
                                gMIBuyOrdersEndPrice.Text = $"{Config.CharSettings[Game.ClientInst].GMIBuyOrderEndPrice}";
                            }

                            if (gMIWithdrawAmount != null)
                            {
                                gMIWithdrawAmount.Text = $"{Config.CharSettings[Game.ClientInst].GMIWithdrawAmount}";
                            }

                            if (gMIItemName != null)
                            {
                                gMIItemName.Text = $"{Config.CharSettings[Game.ClientInst].GMIItemName}";
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
                Chat.WriteLine($"{settingsWindows[settingsName]}");
                Chat.WriteLine("Failed to load settings schema from " + settingsWindowXmlPath);
            }
        }
    }
}
