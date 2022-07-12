using AOSharp.Common.GameData;
using AOSharp.Core.UI;
using AOSharp.Core;
using System;
using System.Collections.Generic;
using AOSharp.Common.GameData.UI;

namespace LootManager
{
    public static class SettingsController
    {
        private static List<Settings> settingsToSave = new List<Settings>();
        public static Dictionary<string, string> settingsWindows = new Dictionary<string, string>();
        private static bool IsCommandRegistered;

        public static MultiListView searchList;

        public static string NameValue = string.Empty;
        public static int ItemIdValue = 0;
        public static int MinQlValue = 0;
        public static int MaxQlValue = 0;

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
            if (!IsCommandRegistered)
            {
                Chat.RegisterCommand("lootmanager", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    try
                    {
                        settingsWindow = Window.Create(new Rect(50, 50, 376, 600), "Loot Manager", "Settings", WindowStyle.Default, WindowFlags.None);

                        if (settingsWindow != null && !settingsWindow.IsVisible)
                        {
                            searchList = ItemListViewBase.Create(new Rect(999999, 999999, -999999, -999999), 0x40, 0x0f, 0);
                            SetupMultiListView(searchList);
                            for (int i = 1; i <= LootManager._settingsItems["ItemCount_ItemList"].AsInt32(); i++)
                            {
                                int lowId = LootManager._settingsItems[$"Item_LowId_ItemList_{i}"].AsInt32();
                                int highId = LootManager._settingsItems[$"Item_HighId_ItemList_{i}"].AsInt32();
                                int ql = LootManager._settingsItems[$"Item_Ql_ItemList_{i}"].AsInt32();

                                if (DummyItem.CreateDummyItemID(lowId, highId, ql, out Identity item))
                                {
                                    ItemModel ItemModel = new ItemModel { LowId = lowId, HighId = highId, Ql = ql };

                                    MultiListViewItem viewItem = InventoryListViewItem.Create(1, item, true);
                                    LootManager.PreItemList.Add(ItemModel, viewItem);
                                    searchList.AddItem(searchList.GetFirstFreePos(), viewItem, true);
                                }
                            }
                            AppendSettingsTab("Loot Manager", settingsWindow);

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
                if (settingsView.FindChild("searchRoot", out View searchRoot))
                {
                    Chat.WriteLine("adding");
                    searchRoot.AddChild(searchList, true);
                }
                testWindow.Show(true);
            }
            else
            {
                Chat.WriteLine($"{settingsWindows[settingsName]}");
                Chat.WriteLine("Failed to load settings schema from " + settingsWindowXmlPath);
            }
        }

        private static void SetupMultiListView(MultiListView multiListView)
        {
            multiListView.SetLayoutMode(1);
            multiListView.AddColumn(0, "", 15);
            multiListView.AddColumn(1, "Name", 300);
            //multiListView.AddColumn(4, "Ql", 60);
            //multiListView.AddColumn(5, "MaxQL", 60);
            //multiListView.SetLayoutMode(1);
            //multiListView.AddColumn(0, "Name", 300);
            //multiListView.AddColumn(1, "MinQL", 50);
            //multiListView.AddColumn(2, "MaxQL", 50);
        }
    }
}
