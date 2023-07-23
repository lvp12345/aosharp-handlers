using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;

namespace LootManager
{
    public static class SettingsController
    {
        private static readonly List<Settings> settingsToSave = new List<Settings>();
        private static readonly Dictionary<string, string> settingsWindows = new Dictionary<string, string>();
        private static bool isCommandRegistered;

        public static MultiListView searchList;

        public static Config Config { get; private set; }

        public static string NameValue = string.Empty;
        public static int ItemIdValue = 0;
        public static int MinQlValue = 0;
        public static int MaxQlValue = 0;
        public static int QtyValue = 0;

        public static bool Delete;

        public static Window settingsWindow;

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
            if (!isCommandRegistered)
            {
                Chat.RegisterCommand("lootmanager", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    try
                    {
                        Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\LootManager\\{DynelManager.LocalPlayer.Name}\\Config.json");

                        settingsWindow = Window.Create(new Rect(50, 50, 300, 300), "Loot Manager", "Settings", WindowStyle.Default, WindowFlags.AutoScale);

                        if (settingsWindow != null && !settingsWindow.IsVisible)
                        {
                            AppendSettingsTab("Loot Manager", settingsWindow);

                            if (settingsWindow.FindView("ScrollListRoot", out MultiListView _multiListView) &&
                                settingsWindow.FindView("_itemMinQL", out TextInputView _itemMinQL) &&
                                settingsWindow.FindView("_itemMaxQL", out TextInputView _itemMaxQL))
                            {
                                _itemMinQL.Text = "1";
                                _itemMaxQL.Text = "500";
                                _multiListView.DeleteAllChildren();
                                int iEntry = 0;
                                foreach (Rule r in LootManager.Rules)
                                {
                                    View entry = View.CreateFromXml(LootManager.PluginDir + "\\UI\\ItemEntry.xml");
                                    entry.FindChild("ItemName", out TextView tx);

                                    string scope = r.Global ? "Global" : "Local";
                                    tx.Text = (iEntry + 1).ToString() + " - " + scope + " - [" + r.Lql.PadLeft(3, ' ') + "-" + r.Hql.PadLeft(3, ' ') + "  ] - " + r.Name;

                                    _multiListView.AddChild(entry, false);
                                    iEntry++;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = "An error occurred on line " + LootManager.GetLineNumber(ex) + ": " + ex.Message;

                        if (errorMessage != LootManager.previousErrorMessage)
                        {
                            Chat.WriteLine(errorMessage);
                            Chat.WriteLine("Stack Trace: " + ex.StackTrace);
                            LootManager.previousErrorMessage = errorMessage;
                        }
                    }
                });

                isCommandRegistered = true;
            }
        }

        public static void AppendSettingsTab(string settingsName, Window testWindow)
        {
            if (settingsWindows.TryGetValue(settingsName, out string settingsWindowXmlPath))
            {
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
}
