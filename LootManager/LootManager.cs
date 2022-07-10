using AOSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using AOSharp.Core.IPC;
using AOSharp.Common.GameData.UI;
using System.Threading.Tasks;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Helpers;
using System.Runtime.InteropServices;

namespace LootManager
{
    public class LootManager : AOPluginEntry
    {
        private double _lastCheckTime = Time.NormalTime;
        private AOSharp.Core.Settings LootBuddySettings = new AOSharp.Core.Settings("LootBuddy");

        public static List<MultiListViewItem> MultiListViewItemList = new List<MultiListViewItem>();
        public static Dictionary<ItemModel, MultiListViewItem> PreItemList = new Dictionary<ItemModel, MultiListViewItem>();

        public static Settings SettingsItems;

        public static string PluginDirectory;

        public static Corpse corpsesToLoot;

        public static Container extrabag1 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra1");
        public static Container extrabag2 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra2");
        public static Container extrabag3 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra3");
        public static Container extrabag4 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra4");
        public static Container extrabag5 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra5");
        public static Container extrabag6 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra6");
        public static Container extrabag7 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra7");
        public static Container extrabag8 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra8");
        public static Container extrabag9 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra9");
        public static Container extrabag10 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra10");
        public static Container extrabag11 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra11");
        public static Container extrabag12 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra12");
        public static Container extrabag13 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra13");
        public static Container extrabag14 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra14");
        public static Container extrabag15 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra15");
        public static Container extrabag16 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra16");
        public static Container extrabag17 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra17");
        public static Container extrabag18 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra18");

        public static List<string> IgnoreItems = new List<string>();

        public override void Run(string pluginDir)
        {
            try
            {
                PluginDirectory = pluginDir;

                Chat.WriteLine("Loot Manager loaded!");
                Chat.WriteLine("/lootlist for settings.");

                //Chat.RegisterCommand("buddy", LootBuddyCommand);

                SettingsController.RegisterSettingsWindow("Loot Manager", pluginDir + "\\UI\\LootManagerSettingWindow.xml", LootBuddySettings);

                LootBuddySettings.AddVariable("Toggle", false);
                LootBuddySettings.AddVariable("ApplyRules", false);
                LootBuddySettings.AddVariable("SingleItem", false);

                LootBuddySettings["Toggle"] = false;

                SettingsItems = new Settings("LootManager_Items");

                SettingsItems.AddVariable("ItemCount_ItemList", 0);

                for (int i = 1; i <= SettingsItems["ItemCount_ItemList"].AsInt32(); i++)
                {
                    SettingsItems.AddVariable($"Item_LowId_ItemList_{i}", 0);
                    SettingsItems.AddVariable($"Item_HighId_ItemList_{i}", 0);
                    SettingsItems.AddVariable($"Item_Ql_ItemList_{i}", 0);
                    SettingsItems.AddVariable($"Item_MinQl_ItemList_{i}", 0);
                    SettingsItems.AddVariable($"Item_MaxQl_ItemList_{i}", 0);
                }

                Chat.RegisterCommand("lootignore", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    IgnoreItems.Add(param[0]);
                    Chat.WriteLine($"Ignore item {param[0]} added.");
                });

                //Chat.RegisterCommand("add", (string command, string[] param, ChatWindow chatWindow) =>
                //{
                //    int lowId = 81901;
                //    int highId = 81901;
                //    int ql = 139;

                //    if (DummyItem.CreateDummyItemID(lowId, highId, ql, out Identity item))
                //    {
                //        MultiListViewItem viewItem = InventoryListViewItem.Create(1, item, true);
                //        SettingsController.searchList.AddItem(SettingsController.searchList.GetFirstFreePos(), viewItem, true);
                //    }
                //});

                Game.OnUpdate += OnUpdate;
                Inventory.ContainerOpened = OnContainerOpened;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void ClearConfigItems(MultiListView searchList, Dictionary<ItemModel, MultiListViewItem> dictionary, string listType)
        {
            int count = SettingsItems[$"ItemCount_{listType}"].AsInt32();

            foreach (var item in dictionary)
                searchList.RemoveItem(item.Value);

            for (int i = 1; i <= count; i++)
            {
                SettingsItems.DeleteVariable($"Item_LowId_{listType}_{count}");
                SettingsItems.DeleteVariable($"Item_HighId_{listType}_{count}");
                SettingsItems.DeleteVariable($"Item_Ql_{listType}_{count}");
                SettingsItems.DeleteVariable($"Item_MinQl_{listType}_{count}");
                SettingsItems.DeleteVariable($"Item_MaxQl_{listType}_{count}");
            }

            dictionary.Clear();
            SettingsItems[$"ItemCount_{listType}"] = 0;
            SettingsItems.Save();
        }

        private void ClearView(object s, ButtonBase button)
        {
            SettingsViewModel settingsViewModel = (SettingsViewModel)button.Tag;
            string type = settingsViewModel.Type;
            MultiListView searchView = settingsViewModel.MultiListView;
            Dictionary<ItemModel, MultiListViewItem> dictionary = settingsViewModel.Dictionary;

            ClearConfigItems(searchView, dictionary, type);
            //foreach (var item in dictionary)
            //    settingscontroller.searchlist.removeitem(item.value);
            //preitemlist.clear();
        }
        private void AddItemToConfig(ItemModel ItemModel, MultiListViewItem viewItem, string listType)
        {
            if (listType == "ItemList")
                PreItemList.Add(ItemModel, viewItem);


            int count = SettingsItems[$"ItemCount_{listType}"].AsInt32() + 1;

            if (SettingsItems[$"Item_LowId_{listType}_{count}"] == null)
            {
                SettingsItems.AddVariable($"Item_LowId_{listType}_{count}", 0);
                SettingsItems.AddVariable($"Item_HighId_{listType}_{count}", 0);
                SettingsItems.AddVariable($"Item_Ql_{listType}_{count}", 0);
                SettingsItems.AddVariable($"Item_MinQl_{listType}_{count}", 0);
                SettingsItems.AddVariable($"Item_MaxQl_{listType}_{count}", 0);
            }

            SettingsItems[$"Item_LowId_{listType}_{count}"] = ItemModel.LowId;
            SettingsItems[$"Item_HighId_{listType}_{count}"] = ItemModel.HighId;
            SettingsItems[$"Item_Ql_{listType}_{count}"] = ItemModel.Ql;
            SettingsItems[$"Item_MinQl_{listType}_{count}"] = SettingsController.MinQlValue;
            SettingsItems[$"Item_MaxQl_{listType}_{count}"] = SettingsController.MaxQlValue;
            SettingsItems[$"ItemCount_{listType}"] = count;

            Chat.WriteLine($"{ItemModel.ItemName}");
            Chat.WriteLine($"{SettingsItems[$"Item_MinQl_{listType}_{count}"]}");
            Chat.WriteLine($"{SettingsItems[$"Item_MaxQl_{listType}_{count}"]}");

            SettingsItems.Save();
        }

        private void AddItemView(object s, ButtonBase button)
        {
            int lowId = SettingsController.ItemIdValue;
            int highId = SettingsController.ItemIdValue;
            int ql = SettingsController.MinQlValue;

            if (DummyItem.CreateDummyItemID(lowId, highId, ql, out Identity item))
            {
                try
                {
                    MultiListViewItem viewItem = InventoryListViewItem.Create(1, item, true);
                    ItemModel ItemModel = new ItemModel { LowId = lowId, HighId = highId, Ql = ql, ItemName = SettingsController.NameValue };
                    if (!SettingsController.searchList.Items.Contains(viewItem))
                    {
                        SettingsController.searchList.AddItem(SettingsController.searchList.GetFirstFreePos(), viewItem, true);
                        AddItemToConfig(ItemModel, viewItem, "ItemList");
                    }
                }
                catch (Exception e)
                {
                    Chat.WriteLine(e.Message);
                }
            }
        }

        private void RemoveItemView(object s, ButtonBase button)
        {
            SettingsViewModel settingsViewModel = (SettingsViewModel)button.Tag;
            string type = settingsViewModel.Type;
            MultiListView searchView = settingsViewModel.MultiListView;
            Dictionary<ItemModel, MultiListViewItem> searchDict = settingsViewModel.Dictionary;
            int count = SettingsItems[$"ItemCount_{type}"].AsInt32();
            ItemModel itemModel = new ItemModel();
            if (searchView.GetSelectedItem(out MultiListViewItem selectedItem))
                foreach (var itemView in searchDict)
                    if (itemView.Value.Pointer == selectedItem.Pointer)
                    {
                        itemModel = itemView.Key;
                        searchView.RemoveItem(selectedItem);
                        itemView.Value.Select(false);

                        for (int i = 1; i <= count; i++)
                        {
                            if (SettingsItems[$"Item_LowId_{type}_{i}"].AsInt32() == itemView.Key.LowId &&
                                SettingsItems[$"Item_HighId_{type}_{i}"].AsInt32() == itemView.Key.HighId &&
                                SettingsItems[$"Item_Ql_{type}_{i}"].AsInt32() == itemView.Key.Ql &&
                                SettingsItems[$"Item_MinQl_{type}_{i}"].AsInt32() == SettingsController.MinQlValue &&
                                SettingsItems[$"Item_MaxQl_{type}_{i}"].AsInt32() == SettingsController.MaxQlValue)
                            {
                                if (i != count)
                                {
                                    SettingsItems[$"Item_LowId_{type}_{i}"] = SettingsItems[$"Item_LowId_{type}_{count}"].AsInt32();
                                    SettingsItems[$"Item_HighId_{type}_{i}"] = SettingsItems[$"Item_HighId_{type}_{count}"].AsInt32();
                                    SettingsItems[$"Item_Ql_{type}_{i}"] = SettingsItems[$"Item_Ql_{type}_{count}"].AsInt32();
                                    SettingsItems[$"Item_MinQl_{type}_{i}"] = SettingsItems[$"Item_MinQl_{type}_{count}"].AsInt32();
                                    SettingsItems[$"Item_MaxQl_{type}_{i}"] = SettingsItems[$"Item_MaxQl_{type}_{count}"].AsInt32();

                                    Chat.WriteLine($"{SettingsItems[$"Item_MinQl_{type}_{count}"]}");
                                    Chat.WriteLine($"{SettingsItems[$"Item_MaxQl_{type}_{count}"]}");

                                    SettingsItems.DeleteVariable($"Item_LowId_{type}_{count}");
                                    SettingsItems.DeleteVariable($"Item_HighId_{type}_{count}");
                                    SettingsItems.DeleteVariable($"Item_Ql_{type}_{count}");
                                    SettingsItems.DeleteVariable($"Item_MinQl_{type}_{count}");
                                    SettingsItems.DeleteVariable($"Item_MaxQl_{type}_{count}");
                                }
                                else
                                {
                                    Chat.WriteLine($"{SettingsItems[$"Item_MinQl_{type}_{count}"]}");
                                    Chat.WriteLine($"{SettingsItems[$"Item_MaxQl_{type}_{count}"]}");

                                    SettingsItems.DeleteVariable($"Item_LowId_{type}_{count}");
                                    SettingsItems.DeleteVariable($"Item_HighId_{type}_{count}");
                                    SettingsItems.DeleteVariable($"Item_Ql_{type}_{count}");
                                    SettingsItems.DeleteVariable($"Item_MinQl_{type}_{count}");
                                    SettingsItems.DeleteVariable($"Item_MaxQl_{type}_{count}");
                                }
                                break;
                            }
                        }
                    }
            if (itemModel != null)
                searchDict.Remove(itemModel);
            SettingsItems[$"ItemCount_{type}"] = count - 1;
            SettingsItems.Save();
        }

        private void InfoView(object s, ButtonBase button)
        {
            //infoWindow = Window.CreateFromXml("Info", PluginDirectory + "\\UI\\LootManagerInfoView.xml",
            //    windowSize: new Rect(0, 0, 440, 510),
            //    windowStyle: WindowStyle.Default,
            //    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            //infoWindow.Show(true);
        }
        public override void Teardown()
        {
            SettingsController.CleanUp();

        }

        public bool RulesApply(Item item)
        {
            int count = SettingsItems[$"ItemCount_ItemList"].AsInt32();

            for (int i = 1; i <= count; i++)
            {
                if (SettingsItems[$"Item_LowId_ItemList_{i}"].AsInt32() == item.Id &&
                    SettingsItems[$"Item_HighId_ItemList_{i}"].AsInt32() == item.HighId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ItemExists(Item item)
        {
            if (extrabag1 != null && !extrabag1.Items.Contains(item) || (extrabag2 != null && !extrabag2.Items.Contains(item))
                 || (extrabag3 != null && !extrabag3.Items.Contains(item)) || (extrabag4 != null && !extrabag4.Items.Contains(item))
                 || !Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Contains(item))
                return false;
            else
                return true;
        }

        private void OnContainerOpened(object sender, Container container)
        {
            if (!LootBuddySettings["Toggle"].AsBool()) { return; }

            if (container.Identity.Type == IdentityType.Corpse && container.Items.Count >= 0)
            {
                foreach (Item item in container.Items)
                {
                    if (LootBuddySettings["ApplyRules"].AsBool())
                    {
                        if (RulesApply(item))
                        {
                            if (LootBuddySettings["SingleItem"].AsBool() && !ItemExists(item))
                                continue;

                            if (Inventory.NumFreeSlots >= 1)
                                item.MoveToInventory();
                            else
                            {
                                if (extrabag1 != null && extrabag1.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag1);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag2 != null && extrabag2.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag2);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag3 != null && extrabag3.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag3);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag4 != null && extrabag4.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag4);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag5 != null && extrabag5.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag5);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag6 != null && extrabag6.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag6);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag7 != null && extrabag7.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag7);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag8 != null && extrabag8.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag8);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag9 != null && extrabag9.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag9);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag10 != null && extrabag10.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag10);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag11 != null && extrabag11.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag11);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag12 != null && extrabag12.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag12);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag13 != null && extrabag13.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag13);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag14 != null && extrabag14.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag14);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag15 != null && extrabag15.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag15);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag16 != null && extrabag16.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag16);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag17 != null && extrabag17.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag17);
                                    }
                                    item.MoveToInventory();
                                }
                                else if (extrabag18 != null && extrabag18.Items.Count < 21)
                                {
                                    foreach (Item itemtomove in Inventory.Items)
                                    {
                                        if (RulesApply(itemtomove))
                                            itemtomove.MoveToContainer(extrabag18);
                                    }
                                    item.MoveToInventory();
                                }
                            }
                        }
                        else
                        {
                            item.Delete();
                        }
                    }
                    else
                    {
                        if (Inventory.NumFreeSlots >= 1)
                            item.MoveToInventory();
                        else
                        {
                            if (extrabag1 != null && extrabag1.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag1);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag2 != null && extrabag2.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag2);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag3 != null && extrabag3.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag3);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag4 != null && extrabag4.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag4);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag5 != null && extrabag5.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag5);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag6 != null && extrabag6.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag6);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag7 != null && extrabag7.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag7);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag8 != null && extrabag8.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag8);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag9 != null && extrabag9.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag9);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag10 != null && extrabag10.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag10);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag11 != null && extrabag11.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag11);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag12 != null && extrabag12.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag12);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag13 != null && extrabag13.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag13);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag14 != null && extrabag14.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag14);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag15 != null && extrabag15.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag15);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag16 != null && extrabag16.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag16);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag17 != null && extrabag17.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag17);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag18 != null && extrabag18.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory).Where(c => !IgnoreItems.Contains(c.Name)))
                                {
                                    if (itemtomove.Name != "Health and Nano Recharger" && itemtomove.Name != "Health and Nano Stim"
                                        && itemtomove.Name != "Aggression Enhancer" && !itemtomove.Name.Contains("Ammo"))
                                        itemtomove.MoveToContainer(extrabag18);
                                }
                                item.MoveToInventory();
                            }
                        }
                    }
                }
            }
        }

        private void OnUpdate(object sender, float deltaTime)
        {
            if (LootBuddySettings["Toggle"].AsBool())
            {
                if (Time.NormalTime - _lastCheckTime > new Random().Next(1, 6))
                {
                    _lastCheckTime = Time.NormalTime;

                    corpsesToLoot = DynelManager.Corpses
                        .Where(corpse => corpse.DistanceFrom(DynelManager.LocalPlayer) < 7)
                        .FirstOrDefault();

                    if (corpsesToLoot != null)
                    {
                        corpsesToLoot.Open();
                    }
                }
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("NameValue", out TextInputView textinput1);
                SettingsController.settingsWindow.FindView("ItemIdValue", out TextInputView textinput2);
                SettingsController.settingsWindow.FindView("MinQlValue", out TextInputView textinput3);
                SettingsController.settingsWindow.FindView("MaxQlValue", out TextInputView textinput4);

                SettingsViewModel settingsViewModel = new SettingsViewModel
                {
                    MultiListView = SettingsController.searchList,
                    Dictionary = PreItemList,
                    Type = "ItemList"
                };

                MultiListView searchView = settingsViewModel.MultiListView;
                int count = SettingsItems[$"ItemCount_ItemList"].AsInt32();
                Dictionary<ItemModel, MultiListViewItem> searchDict = settingsViewModel.Dictionary;
                ItemModel itemModel = new ItemModel();
                if (searchView.GetSelectedItem(out MultiListViewItem selectedItem))
                    foreach (var itemView in searchDict)
                        if (itemView.Value.Pointer == selectedItem.Pointer)
                        {
                            itemModel = itemView.Key;

                            for (int i = 1; i <= count; i++)
                            {
                                if (SettingsItems[$"Item_LowId_ItemList_{i}"].AsInt32() == itemView.Key.LowId &&
                                    SettingsItems[$"Item_HighId_ItemList_{i}"].AsInt32() == itemView.Key.HighId &&
                                    SettingsItems[$"Item_Ql_ItemList_{i}"].AsInt32() == itemView.Key.Ql)
                                {
                                    if (i != count)
                                    {
                                        if (textinput1 != null && itemView.Key.ItemName != null)
                                            textinput1.Text = itemView.Key.ItemName.ToString();
                                        if (textinput2 != null && itemModel != null)
                                            textinput2.Text = itemModel.LowId.ToString();
                                        if (textinput3 != null && SettingsItems[$"Item_MinQl_ItemList_{i}"] != null)
                                            textinput3.Text = SettingsItems[$"Item_MinQl_ItemList_{i}"].ToString();
                                        if (textinput4 != null && SettingsItems[$"Item_MaxQl_ItemList_{i}"] != null)
                                            textinput4.Text = SettingsItems[$"Item_MaxQl_ItemList_{i}"].ToString();
                                    }
                                    else
                                    {
                                        if (textinput1 != null && itemView.Key.ItemName != null)
                                            textinput1.Text = itemView.Key.ItemName.ToString();
                                        if (textinput2 != null && itemModel != null)
                                            textinput2.Text = itemModel.LowId.ToString();
                                        if (textinput3 != null && SettingsItems[$"Item_MinQl_ItemList_{i}"] != null)
                                            textinput3.Text = SettingsItems[$"Item_MinQl_ItemList_{i}"].AsString();
                                        if (textinput4 != null && SettingsItems[$"Item_MaxQl_ItemList_{i}"] != null)
                                            textinput4.Text = SettingsItems[$"Item_MaxQl_ItemList_{i}"].AsString();
                                    }
                                    break;
                                }
                            }
                        }

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    SettingsController.NameValue = textinput1.Text;
                }
                if (textinput2 != null && textinput2.Text != String.Empty)
                {
                    if (int.TryParse(textinput2.Text, out int itemIdValue))
                    {
                        SettingsController.ItemIdValue = itemIdValue;
                    }
                }
                if (textinput3 != null && textinput3.Text != String.Empty)
                {
                    if (int.TryParse(textinput3.Text, out int minQlValue))
                    {
                        SettingsController.MinQlValue = minQlValue;
                    }
                }
                if (textinput4 != null && textinput4.Text != String.Empty)
                {
                    if (int.TryParse(textinput4.Text, out int maxQlValue))
                    {
                        SettingsController.MaxQlValue = maxQlValue;
                    }
                }

                if (SettingsController.settingsWindow.FindView("InfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = InfoView;
                }

                if (SettingsController.settingsWindow.FindView("ClearView", out Button clearView))
                {
                    clearView.Tag = new SettingsViewModel
                    {
                        MultiListView = SettingsController.searchList,
                        Dictionary = PreItemList,
                        Type = "ItemList"
                    };
                    clearView.Clicked = ClearView;
                }

                if (SettingsController.settingsWindow.FindView("AddItemView", out Button addItemView))
                {
                    addItemView.Tag = SettingsController.settingsWindow;
                    addItemView.Clicked = AddItemView;
                }

                if (SettingsController.settingsWindow.FindView("RemoveItemView", out Button removeItemView))
                {
                    removeItemView.Tag = new SettingsViewModel
                    {
                        MultiListView = SettingsController.searchList,
                        Dictionary = PreItemList,
                        Type = "ItemList"
                    };
                    removeItemView.Clicked = RemoveItemView;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Pack = 0)]
    public struct MemStruct
    {
        [FieldOffset(0x14)]
        public Identity Identity;

        [FieldOffset(0x9C)]
        public IntPtr Name;
    }

    public class RemoveItemModel
    {
        public MultiListView MultiListView;
        public MultiListViewItem MultiListViewItem;
        public View ViewSettings;
        public View ViewButton;
    }

    public class SettingsViewModel
    {
        public string Type;
        public MultiListView MultiListView;
        public Dictionary<ItemModel, MultiListViewItem> Dictionary;
    }

    public class ItemModel
    {
        public string ItemName;
        public int LowId;
        public int HighId;
        public int Ql;
    }
}
