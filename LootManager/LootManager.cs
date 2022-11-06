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
using System.Windows.Input;

namespace LootManager
{
    public class LootManager : AOPluginEntry
    {
        private double _lastCheckTime = Time.NormalTime;

        public static List<MultiListViewItem> MultiListViewItemList = new List<MultiListViewItem>();
        public static Dictionary<ItemModel, MultiListViewItem> PreItemList = new Dictionary<ItemModel, MultiListViewItem>();

        private static List<Identity> _corpseIdList = new List<Identity>();

        private static int MinQlValue;
        private static int MaxQlValue;
        private static int ItemIdValue;
        private static string ItemNameValue;

        protected Settings _settings;
        public static Settings _settingsItems;

        private static bool _init = false;

        private static int _currentIgnore = 0;

        private Window _infoWindow;

        private static List<Item> _invItems = new List<Item>();

        public static string PluginDir;

        public override void Run(string pluginDir)
        {
            try
            {
                _settings = new Settings("LootManager");
                PluginDir = pluginDir;

                Game.OnUpdate += OnUpdate;
                Inventory.ContainerOpened += OnContainerOpened;

                _settings.AddVariable("Toggle", false);
                _settings.AddVariable("ApplyRules", false);

                _settings["Toggle"] = false;

                _settingsItems = new Settings("LootManager_Items");

                _settingsItems.AddVariable("ItemCount_ItemList", 0);

                for (int i = 1; i <= _settingsItems["ItemCount_ItemList"].AsInt32(); i++)
                {
                    _settingsItems.AddVariable($"Item_LowId_ItemList_{i}", 0);
                    _settingsItems.AddVariable($"Item_HighId_ItemList_{i}", 0);
                    _settingsItems.AddVariable($"Item_Ql_ItemList_{i}", 0);
                    _settingsItems.AddVariable($"Item_MinQl_ItemList_{i}", 0);
                    _settingsItems.AddVariable($"Item_MaxQl_ItemList_{i}", 0);
                }

                RegisterSettingsWindow("Loot Manager", "LootManagerSettingWindow.xml");

                Chat.RegisterCommand("setinv", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    foreach (Item item in Inventory.Items.Where(c => c.Slot.Type == IdentityType.Inventory))
                        if (!_invItems.Contains(item))
                            _invItems.Add(item);
                });

                Chat.RegisterCommand("clearinv", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    _invItems.Clear();
                });

                //Chat.RegisterCommand("ignoreloot", (string command, string[] param, ChatWindow chatWindow) =>
                //{
                //    _basicIgnores.Add(param[0]);
                //    Chat.WriteLine($"Ignore item {param[0]} added.");
                //});

                //Chat.RegisterCommand("printignore", (string command, string[] param, ChatWindow chatWindow) =>
                //{
                //    for (int i = 0; i <= _basicIgnores.Capacity; i++)
                //    {
                //        Chat.WriteLine($"{_basicIgnores[i]}.");
                //    }
                //});

                Chat.WriteLine("Loot Manager loaded!");
                Chat.WriteLine("/lootmanager for settings.");


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
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void ClearConfigItems(MultiListView searchList, Dictionary<ItemModel, MultiListViewItem> dictionary, string listType)
        {
            int count = _settingsItems[$"ItemCount_{listType}"].AsInt32();

            foreach (var item in dictionary)
                searchList.RemoveItem(item.Value);

            for (int i = 1; i <= count; i++)
            {
                _settingsItems.DeleteVariable($"Item_LowId_{listType}_{count}");
                _settingsItems.DeleteVariable($"Item_HighId_{listType}_{count}");
                _settingsItems.DeleteVariable($"Item_Ql_{listType}_{count}");
                _settingsItems.DeleteVariable($"Item_MinQl_{listType}_{count}");
                _settingsItems.DeleteVariable($"Item_MaxQl_{listType}_{count}");
            }

            dictionary.Clear();
            _settingsItems[$"ItemCount_{listType}"] = 0;
            _settingsItems.Save();
        }

        private void AddItemToConfig(ItemModel ItemModel, MultiListViewItem viewItem, string listType)
        {
            if (listType == "ItemList")
                PreItemList.Add(ItemModel, viewItem);


            int count = _settingsItems[$"ItemCount_{listType}"].AsInt32() + 1;

            if (_settingsItems[$"Item_LowId_{listType}_{count}"] == null)
            {
                _settingsItems.AddVariable($"Item_LowId_{listType}_{count}", 0);
                _settingsItems.AddVariable($"Item_HighId_{listType}_{count}", 0);
                _settingsItems.AddVariable($"Item_Ql_{listType}_{count}", 0);
                _settingsItems.AddVariable($"Item_MinQl_{listType}_{count}", 0);
                _settingsItems.AddVariable($"Item_MaxQl_{listType}_{count}", 0);
            }

            _settingsItems[$"Item_LowId_{listType}_{count}"] = ItemModel.LowId;
            _settingsItems[$"Item_HighId_{listType}_{count}"] = ItemModel.HighId;
            _settingsItems[$"Item_Ql_{listType}_{count}"] = ItemModel.Ql;
            _settingsItems[$"Item_MinQl_{listType}_{count}"] = MinQlValue;
            _settingsItems[$"Item_MaxQl_{listType}_{count}"] = MaxQlValue;
            _settingsItems[$"ItemCount_{listType}"] = count;

            //Chat.WriteLine($"{ItemModel.ItemName}");
            //Chat.WriteLine($"{_settingsItems[$"Item_MinQl_{listType}_{count}"]}");
            //Chat.WriteLine($"{_settingsItems[$"Item_MaxQl_{listType}_{count}"]}");

            _settingsItems.Save();
        }

        private void HandleClearViewClick(object s, ButtonBase button)
        {
            SettingsViewModel settingsViewModel = (SettingsViewModel)button.Tag;
            string type = settingsViewModel.Type;
            MultiListView searchView = settingsViewModel.MultiListView;
            Dictionary<ItemModel, MultiListViewItem> dictionary = settingsViewModel.Dictionary;

            ClearConfigItems(searchView, dictionary, type);
        }
        private void HandleAddItemViewClick(object s, ButtonBase button)
        {
            if (DummyItem.CreateDummyItemID(ItemIdValue, ItemIdValue, 69, out Identity item))
            {
                try
                {
                    MultiListViewItem viewItem = InventoryListViewItem.Create(1, item, true);
                    ItemModel ItemModel = new ItemModel { LowId = ItemIdValue, HighId = ItemIdValue, Ql = 69 };
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

        private void HandleRemoveItemViewClick(object s, ButtonBase button)
        {
            SettingsViewModel settingsViewModel = (SettingsViewModel)button.Tag;
            string type = settingsViewModel.Type;
            MultiListView searchView = settingsViewModel.MultiListView;
            Dictionary<ItemModel, MultiListViewItem> searchDict = settingsViewModel.Dictionary;
            int count = _settingsItems[$"ItemCount_{type}"].AsInt32();
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
                            if (_settingsItems[$"Item_LowId_{type}_{i}"].AsInt32() == itemView.Key.LowId &&
                                _settingsItems[$"Item_HighId_{type}_{i}"].AsInt32() == itemView.Key.HighId &&
                                _settingsItems[$"Item_Ql_{type}_{i}"].AsInt32() == itemView.Key.Ql &&
                                _settingsItems[$"Item_MinQl_{type}_{i}"].AsInt32() == MinQlValue &&
                                _settingsItems[$"Item_MaxQl_{type}_{i}"].AsInt32() == MaxQlValue)
                            {
                                if (i != count)
                                {
                                    _settingsItems[$"Item_LowId_{type}_{i}"] = _settingsItems[$"Item_LowId_{type}_{count}"].AsInt32();
                                    _settingsItems[$"Item_HighId_{type}_{i}"] = _settingsItems[$"Item_HighId_{type}_{count}"].AsInt32();
                                    _settingsItems[$"Item_Ql_{type}_{i}"] = _settingsItems[$"Item_Ql_{type}_{count}"].AsInt32();
                                    _settingsItems[$"Item_MinQl_{type}_{i}"] = _settingsItems[$"Item_MinQl_{type}_{count}"].AsInt32();
                                    _settingsItems[$"Item_MaxQl_{type}_{i}"] = _settingsItems[$"Item_MaxQl_{type}_{count}"].AsInt32();

                                    _settingsItems.DeleteVariable($"Item_LowId_{type}_{count}");
                                    _settingsItems.DeleteVariable($"Item_HighId_{type}_{count}");
                                    _settingsItems.DeleteVariable($"Item_Ql_{type}_{count}");
                                    _settingsItems.DeleteVariable($"Item_MinQl_{type}_{count}");
                                    _settingsItems.DeleteVariable($"Item_MaxQl_{type}_{count}");
                                }
                                else
                                {
                                    _settingsItems.DeleteVariable($"Item_LowId_{type}_{count}");
                                    _settingsItems.DeleteVariable($"Item_HighId_{type}_{count}");
                                    _settingsItems.DeleteVariable($"Item_Ql_{type}_{count}");
                                    _settingsItems.DeleteVariable($"Item_MinQl_{type}_{count}");
                                    _settingsItems.DeleteVariable($"Item_MaxQl_{type}_{count}");
                                }
                                break;
                            }
                        }
                    }
            if (itemModel != null)
                searchDict.Remove(itemModel);
            _settingsItems[$"ItemCount_{type}"] = count - 1;
            _settingsItems.Save();
        }

        private void HandleInfoViewClick(object s, ButtonBase button)
        {
            _infoWindow = Window.CreateFromXml("Info", PluginDir + "\\UI\\LootManagerInfoView.xml",
                windowSize: new Rect(0, 0, 440, 510),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            _infoWindow.Show(true);
        }
        public override void Teardown()
        {
            SettingsController.CleanUp();

        }

        public bool RulesApply(Item item)
        {
            int count = _settingsItems[$"ItemCount_ItemList"].AsInt32();

            for (int i = 1; i <= count; i++)
            {
                if (_settingsItems[$"Item_LowId_ItemList_{i}"].AsInt32() == item.Id ||
                    _settingsItems[$"Item_LowId_ItemList_{i}"].AsInt32() == item.HighId ||
                    _settingsItems[$"Item_HighId_ItemList_{i}"].AsInt32() == item.Id ||
                    _settingsItems[$"Item_HighId_ItemList_{i}"].AsInt32() == item.HighId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ItemExists(Item item)
        {
            if (Inventory.Items.Contains(item)) { return true; }

            foreach (Backpack backpack in Inventory.Backpacks.Where(c => c.Name.Contains("loot")))
            {
                if (backpack.Items.Contains(item))
                    return true;
            }

            return false;
        }

        private static Backpack FindBagWithSpace()
        {
            foreach (Backpack backpack in Inventory.Backpacks.Where(c => c.Name.Contains("loot")))
            {
                if (backpack.Items.Count < 21)
                    return backpack;
            }

            return null;
        }

        private void OnContainerOpened(object sender, Container container)
        {
            if (!_settings["Toggle"].AsBool()
                || container.Identity.Type != IdentityType.Corpse) { return; }

            foreach (Item item in container.Items)
            {
                if (Inventory.NumFreeSlots >= 1)
                {
                    if (RulesApply(item))
                        item.MoveToInventory();
                }
                else
                {
                    Backpack _bag = FindBagWithSpace();

                    if (_bag == null) { return; }

                    foreach (Item itemtomove in Inventory.Items.Where(c => !_invItems.Contains(c)))
                    {
                        itemtomove.MoveToContainer(_bag);
                    }

                    item.MoveToInventory();
                }
            }

            Item.Use(container.Identity);
        }

        private void OnUpdate(object sender, float deltaTime)
        {
            //if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.F6) && !_init)
            //{
            //    _init = true;

            //    SettingsController.settingsWindow = Window.Create(new Rect(50, 50, 390, 630), "Loot Manager", "Settings", WindowStyle.Default, WindowFlags.None);

            //    if (SettingsController.settingsWindow != null && !SettingsController.settingsWindow.IsVisible)
            //    {
            //        foreach (string settingsName in SettingsController.settingsWindows.Keys.Where(x => x.Contains("Loot Manager")))
            //        {
            //            SettingsController.searchList = ItemListViewBase.Create(new Rect(999999, 999999, -999999, -999999), 0x40, 0x0f, 0);
            //            SettingsController.SetupMultiListView(SettingsController.searchList);
            //            for (int i = 1; i <= _settingsItems["ItemCount_ItemList"].AsInt32(); i++)
            //            {
            //                int lowId = _settingsItems[$"Item_LowId_ItemList_{i}"].AsInt32();
            //                int highId = _settingsItems[$"Item_HighId_ItemList_{i}"].AsInt32();
            //                int ql = _settingsItems[$"Item_Ql_ItemList_{i}"].AsInt32();

            //                if (DummyItem.CreateDummyItemID(lowId, highId, ql, out Identity item))
            //                {
            //                    ItemModel itemModel = new ItemModel { LowId = lowId, HighId = highId, Ql = ql };

            //                    MultiListViewItem viewItem = InventoryListViewItem.Create(1, item, true);
            //                    PreItemList.Add(itemModel, viewItem);
            //                    SettingsController.searchList.AddItem(SettingsController.searchList.GetFirstFreePos(), viewItem, true);
            //                }
            //            }

            //            SettingsController.AppendSettingsTab(settingsName, SettingsController.settingsWindow);
            //        }
            //    }

            //    _init = false;
            //}

            if (_settings["Toggle"].AsBool())
            {
                foreach (Identity corpseId in _corpseIdList)
                    if (DynelManager.Corpses.Where(c => c.Identity == corpseId).ToList().Count == 0)
                    {
                        _corpseIdList.Remove(corpseId);
                        return;
                    }

                if (Time.NormalTime - _lastCheckTime > new Random().Next(1, 3)
                    && !_init)
                {

                    _lastCheckTime = Time.NormalTime;
                    _init = true;

                    foreach (Corpse corpse in DynelManager.Corpses.Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 7
                        && !_corpseIdList.Contains(c.Identity)))
                    {
                        Corpse _corpse = DynelManager.Corpses.FirstOrDefault(c =>
                            c.Identity != corpse.Identity
                            && c.Position.DistanceFrom(corpse.Position) <= 1f);

                        if (_corpse != null) { continue; }

                        corpse.Open();
                        _corpseIdList.Add(corpse.Identity);
                    }

                    _init = false;
                }
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                //SettingsController.settingsWindow.FindView("NameValue", out TextInputView _nameBox);
                SettingsController.settingsWindow.FindView("ItemIdValue", out TextInputView _itemIdBox);
                SettingsController.settingsWindow.FindView("MinQlValue", out TextInputView _minQlBox);
                SettingsController.settingsWindow.FindView("MaxQlValue", out TextInputView _maxQlBox);

                SettingsViewModel settingsViewModel = new SettingsViewModel
                {
                    MultiListView = SettingsController.searchList,
                    Dictionary = PreItemList,
                    Type = "ItemList"
                };

                MultiListView searchView = settingsViewModel.MultiListView;
                int count = _settingsItems[$"ItemCount_ItemList"].AsInt32();
                Dictionary<ItemModel, MultiListViewItem> searchDict = settingsViewModel.Dictionary;
                ItemModel itemModel = new ItemModel();
                if (searchView.GetSelectedItem(out MultiListViewItem selectedItem))
                    foreach (var itemView in searchDict)
                        if (itemView.Value.Pointer == selectedItem.Pointer)
                        {
                            itemModel = itemView.Key;

                            for (int i = 1; i <= count; i++)
                            {
                                if (_settingsItems[$"Item_LowId_ItemList_{i}"].AsInt32() == itemView.Key.LowId &&
                                    _settingsItems[$"Item_HighId_ItemList_{i}"].AsInt32() == itemView.Key.HighId &&
                                    _settingsItems[$"Item_Ql_ItemList_{i}"].AsInt32() == itemView.Key.Ql)
                                {
                                    if (i != count)
                                    {
                                        //if (_nameBox != null && itemView.Key.ItemName != null)
                                        //    _nameBox.Text = itemView.Key.ItemName;
                                        if (_itemIdBox != null && itemModel != null)
                                            _itemIdBox.Text = itemModel.LowId.ToString();
                                        if (_minQlBox != null && _settingsItems[$"Item_MinQl_ItemList_{i}"] != null)
                                            _minQlBox.Text = _settingsItems[$"Item_MinQl_ItemList_{i}"].ToString();
                                        if (_maxQlBox != null && _settingsItems[$"Item_MaxQl_ItemList_{i}"] != null)
                                            _maxQlBox.Text = _settingsItems[$"Item_MaxQl_ItemList_{i}"].ToString();
                                    }
                                    else
                                    {
                                        //if (_nameBox != null && itemView.Key.ItemName != null)
                                        //    _nameBox.Text = itemView.Key.ItemName;
                                        if (_itemIdBox != null && itemModel != null)
                                            _itemIdBox.Text = itemModel.LowId.ToString();
                                        if (_minQlBox != null && _settingsItems[$"Item_MinQl_ItemList_{i}"] != null)
                                            _minQlBox.Text = _settingsItems[$"Item_MinQl_ItemList_{i}"].AsString();
                                        if (_maxQlBox != null && _settingsItems[$"Item_MaxQl_ItemList_{i}"] != null)
                                            _maxQlBox.Text = _settingsItems[$"Item_MaxQl_ItemList_{i}"].AsString();
                                    }
                                    break;
                                }
                            }
                        }

                //if (_nameBox != null && !string.IsNullOrEmpty(_nameBox.Text))
                //{
                //    if (ItemNameValue != _nameBox.Text)
                //        ItemNameValue = _nameBox.Text;
                //}
                if (_itemIdBox != null && !string.IsNullOrEmpty(_itemIdBox.Text))
                {
                    if (int.TryParse(_itemIdBox.Text, out int itemIdValue))
                    {
                        if (ItemIdValue != itemIdValue)
                            ItemIdValue = itemIdValue;
                    }
                }
                if (_minQlBox != null && !string.IsNullOrEmpty(_minQlBox.Text))
                {
                    if (int.TryParse(_minQlBox.Text, out int minQlValue))
                    {
                        if (MinQlValue != minQlValue)
                            MinQlValue = minQlValue;
                    }
                }
                if (_maxQlBox != null && !string.IsNullOrEmpty(_maxQlBox.Text))
                {
                    if (int.TryParse(_maxQlBox.Text, out int maxQlValue))
                    {
                        if (MaxQlValue != maxQlValue)
                            MaxQlValue = maxQlValue;
                    }
                }

                if (SettingsController.settingsWindow.FindView("LootManagerInfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = HandleInfoViewClick;
                }

                if (SettingsController.settingsWindow.FindView("ClearView", out Button clearView))
                {
                    clearView.Tag = new SettingsViewModel
                    {
                        MultiListView = SettingsController.searchList,
                        Dictionary = PreItemList,
                        Type = "ItemList"
                    };
                    clearView.Clicked = HandleClearViewClick;
                }

                if (SettingsController.settingsWindow.FindView("AddItemView", out Button addItemView))
                {
                    addItemView.Tag = SettingsController.settingsWindow;
                    addItemView.Clicked = HandleAddItemViewClick;
                }

                if (SettingsController.settingsWindow.FindView("RemoveItemView", out Button removeItemView))
                {
                    removeItemView.Tag = new SettingsViewModel
                    {
                        MultiListView = SettingsController.searchList,
                        Dictionary = PreItemList,
                        Type = "ItemList"
                    };
                    removeItemView.Clicked = HandleRemoveItemViewClick;
                }
            }
        }

        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDir + "\\UI\\" + xmlName, _settings);
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
