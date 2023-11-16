using AOSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using AOSharp.Common.GameData.UI;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using AOSharp.Core.Misc;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace LootManager
{
    public class LootManager : AOPluginEntry
    {
        public static string previousErrorMessage = string.Empty;

        double _lootingTimer;

        AutoResetInterval openCorpseInterval = new AutoResetInterval(4000); // 5000 milliseconds = 5 seconds
        private Stopwatch closeCorpseStopwatch = new Stopwatch();

        public static Config Config { get; private set; }

        public static List<Rule> Rules;

        private Dictionary<Vector3, Identity> openedCorpses = new Dictionary<Vector3, Identity>();

        private Dictionary<Identity, BackpackInfo> backpackDictionary = new Dictionary<Identity, BackpackInfo>();

        Corpse corpseToClose = null;

        protected Settings _settings;
        public static Settings _settingsItems;

        private Window _infoWindow;

        public static string PluginDir;

        bool isBackpackInfoInitialized = false;

        private double _inventorySpaceReminder;

        public override void Run(string pluginDir)
        {
            try
            {
                _settings = new Settings("LootManager");

                PluginDir = pluginDir;

                Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\LootManager\\{DynelManager.LocalPlayer.Name}\\Config.json");

                Game.OnUpdate += OnUpdate;
                Inventory.ContainerOpened += ProcessItemsInCorpseContainer;

                RegisterSettingsWindow("Loot Manager", "LootManagerSettingWindow.xml");

                _settings.AddVariable("Enabled", false);
                _settings.AddVariable("Delete", false);
                _settings.AddVariable("Exact", false);
                _settings.AddVariable("Disable", false);

                LoadRules();

                Chat.RegisterCommand("lm", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    _settings["Enabled"] = !_settings["Enabled"].AsBool();
                });

                Chat.WriteLine("Loot Manager loaded!");
                Chat.WriteLine("/lootmanager for settings. /lm to enable/disable");
                string _lootManagerEnabled = _settings["Enabled"].AsBool() ? "Enabled" : "Disabled";
                Chat.WriteLine($"Loot Manager is currently {_lootManagerEnabled}");

            }
            catch (Exception ex)
            {
                var errorMessage = "An error occurred on line " + GetLineNumber(ex) + ": " + ex.Message;

                if (errorMessage != previousErrorMessage)
                {
                    Chat.WriteLine(errorMessage);
                    Chat.WriteLine("Stack Trace: " + ex.StackTrace);
                    previousErrorMessage = errorMessage;
                }
            }
        }
        public override void Teardown()
        {
            Config.Save();
            SaveRules();
            SettingsController.CleanUp();
        }
        private void MoveItemsToBag()
        {
            if (!backpackDictionary.Any())
            {
                return;
            }

            var availableBackpack = backpackDictionary.FirstOrDefault(backpack =>
                 backpack.Value.FreeSlots > 0 &&
                 backpack.Value.FreeSlots <= 21);

            if (!availableBackpack.Equals(default(KeyValuePair<Identity, BackpackInfo>)))
            {
                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot.Type == IdentityType.Inventory))
                {
                    if (CheckRules(itemtomove))
                    {
                        itemtomove.MoveToContainer(availableBackpack.Key);
                        availableBackpack.Value.FreeSlots--;
                        break;
                    }
                    if (availableBackpack.Value.FreeSlots == 0)
                    {
                        break;
                    }
                }
            }
        }

        private void UpdateBackpackDictionary()
        {
            bool dictionaryChanged = false;

            foreach (var backpack in Inventory.Backpacks.Where(b => b.Name.Contains("loot")))
            {
                int freeSlots = 21 - backpack.Items.Count;

                if (backpackDictionary.ContainsKey(backpack.Identity))
                {
                    // Check if there's a change in free slots
                    if (backpackDictionary[backpack.Identity].FreeSlots != freeSlots)
                    {
                        // Update the existing entry with new information
                        backpackDictionary[backpack.Identity].FreeSlots = freeSlots;
                        dictionaryChanged = true; // Mark dictionary as changed
                        SaveBackpackDictionaryToJson();
                    }
                }
                else
                {
                    // Add a new entry for the backpack
                    backpackDictionary.Add(backpack.Identity, new BackpackInfo
                    {
                        Name = backpack.Name,
                        FreeSlots = freeSlots
                    });
                    dictionaryChanged = true; // Mark dictionary as changed
                    SaveBackpackDictionaryToJson();
                }
            }

            // Only set the flag to indicate that initialization is done if there was a change
            if (dictionaryChanged)
            {
                isBackpackInfoInitialized = true;
            }
        }

        private void InitializeBackpackInfo()
        {
            if (isBackpackInfoInitialized)
            {
                return; // Already initialized, no need to do it again
            }

            // Open all backpacks to set the item count and names
            List<Item> bags = Inventory.Items.Where(c => c.UniqueIdentity.Type == IdentityType.Container).ToList();

            foreach (Item bag in bags)
            {
                bag.Use();
                bag.Use();
            }
            //// Loop through all backpacks to update backpackDictionary
            //foreach (Backpack backpack in Inventory.Backpacks)
            //{
            //    UpdateBackpackDictionary();
            //}
            // Set the flag to indicate that initialization is done
            isBackpackInfoInitialized = true;
        }

        private void ProcessItemsInCorpseContainer(object sender, Container container)
        {

            if (!_settings["Enabled"].AsBool()) return;

            if (container.Identity.Type != IdentityType.Corpse) return;

            foreach (Item item in container.Items)
            {
                if (Inventory.NumFreeSlots <= 1)
                    return;

                if (CheckRules(item, true))
                {
                    item.MoveToInventory();
                }
                else if (_settings["Delete"].AsBool())
                    item.Delete();
            }
        }

        public void ProcessCorpses()
        {
            // If there's a corpse to close and 3 seconds have passed, attempt to close it
            if (corpseToClose != null && closeCorpseStopwatch.ElapsedMilliseconds >= 3000)
            {
                corpseToClose.Open();
                closeCorpseStopwatch.Stop();
                corpseToClose = null;
                return; // Don't process any other corpses this tick
            }

            // If there's no corpse to close, try to find a new corpse to open
            if (corpseToClose == null)
            {
                Corpse corpseToOpen = DynelManager.Corpses.FirstOrDefault(c =>
                    c.DistanceFrom(DynelManager.LocalPlayer) < 7 &&
                    !openedCorpses.ContainsKey(c.Position));

                if (corpseToOpen != null)
                {
                    var itemsInCorpse = corpseToOpen.Container.Items;

                    // Opening the corpse
                    if (Spell.List.Any(c => c.IsReady) && !Spell.HasPendingCast)
                        //&& !DynelManager.LocalPlayer.IsAttacking && DynelManager.LocalPlayer.FightingTarget == null
                        //&& !DynelManager.LocalPlayer.IsAttackPending)
                    {
                        if (openCorpseInterval.Elapsed)
                        {
                            corpseToOpen.Open();
                            closeCorpseStopwatch.Restart();
                        }
                    }

                    // Check if the corpse has items not matching rules and hasn't been added to the dictionary
                    if (corpseToOpen.IsOpen && itemsInCorpse.Any(item => !CheckRules(item)) && !openedCorpses.ContainsKey(corpseToOpen.Position))
                    {
                        corpseToClose = corpseToOpen;
                        openedCorpses[corpseToOpen.Position] = corpseToOpen.Identity;
                    }
                }
            }
        }
             
        private void OnUpdate(object sender, float deltaTime)
        {
            try
            {
                if (Game.IsZoning)
                {
                    isBackpackInfoInitialized = false;

                    return;
                }

                if (_settings["Enabled"].AsBool())
                {
                    InitializeBackpackInfo();

                    if (_settings["Disable"].AsBool())
                    {
                        if (backpackDictionary.Values.All(backpack => backpack.FreeSlots == 0))
                        {
                            if (Inventory.NumFreeSlots == 0)
                            {
                                _settings["Enabled"] = false;
                            }
                        }
                    }
                    
                    if (!_settings["Delete"].AsBool())
                    {
                        ProcessCorpses();
                    }

                    if (_settings["Delete"].AsBool())
                    {
                        Corpse corpse = DynelManager.Corpses.Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 6).FirstOrDefault();

                        if (corpse != null)
                        {
                            if (Spell.List.Any(c => c.IsReady) && !Spell.HasPendingCast 
                                && Time.NormalTime > _lootingTimer + 1)
                            {
                                corpse.Open();
                                _lootingTimer = Time.NormalTime;
                            }
                        }
                    }

                    UpdateBackpackDictionary();
                    MoveItemsToBag();
                    _inventorySpaceReminder = Time.AONormalTime;
                }

                #region UI
                if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
                {

                    if (SettingsController.settingsWindow.FindView("buttonAdd", out Button addbut))
                    {
                        if (addbut.Clicked == null)
                            addbut.Clicked += addButtonClicked;
                    }

                    if (SettingsController.settingsWindow.FindView("buttonDel", out Button rembut))
                    {
                        if (rembut.Clicked == null)
                            rembut.Clicked += remButtonClicked;
                    }

                    if (SettingsController.settingsWindow.FindView("LootManagerInfoView", out Button infoView))
                    {
                        infoView.Tag = SettingsController.settingsWindow;
                        infoView.Clicked = InfoView;
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                var errorMessage = "An error occurred on line " + GetLineNumber(ex) + ": " + ex.Message;

                if (errorMessage != previousErrorMessage)
                {
                    Chat.WriteLine(errorMessage);
                    Chat.WriteLine("Stack Trace: " + ex.StackTrace);
                    previousErrorMessage = errorMessage;
                }
            }
        }

        private void InfoView(object s, ButtonBase button)
        {
            _infoWindow = Window.CreateFromXml("Info", PluginDir + "\\UI\\LootManagerInfoView.xml",
                windowSize: new Rect(0, 0, 440, 510),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            _infoWindow.Show(true);
        }

        private void addButtonClicked(object sender, ButtonBase e)
        {
            //SettingsController.settingsWindow.FindView("ScrollListRoot", out MultiListView _multiListView);

            SettingsController.settingsWindow.FindView("tivName", out TextInputView _itemName);
            SettingsController.settingsWindow.FindView("_itemMinQL", out TextInputView _itemMinQL);
            SettingsController.settingsWindow.FindView("_itemMaxQL", out TextInputView _itemMaxQL);
            SettingsController.settingsWindow.FindView("_itemQuantity", out TextInputView _itemQuantity);
            //SettingsController.settingsWindow.FindView("_itemBagName", out TextInputView _itemBagName);

            SettingsController.settingsWindow.FindView("tvErr", out TextView txErr);

            if (_itemName.Text.Trim() == "")
            {
                txErr.Text = "Can't add an empty name";
                return;
            }

            int minql = 0;
            int maxql = 0;
            int quantity = 0;
            try
            {
                minql = Convert.ToInt32(_itemMinQL.Text);
                maxql = Convert.ToInt32(_itemMaxQL.Text);
            }
            catch
            {
                txErr.Text = "Quality entries must be numbers!";
                return;
            }

            if (minql > maxql)
            {
                txErr.Text = "Min Quality must be less or equal than the high quality!";
                return;
            }
            if (minql <= 0)
            {
                txErr.Text = "Min Quality must be least 1!";
                return;
            }
            if (maxql > 500)
            {
                txErr.Text = "Max Quality must be 500!";
                return;
            }
            try
            {
                quantity = Convert.ToInt32(_itemQuantity.Text);
            }
            catch
            {
                txErr.Text = "Quantity entries must be numbers!";
                return;
            }
            if (maxql > 999)
            {
                txErr.Text = "Max Quantity must be no more than 999!";
                return;
            }

            SettingsController.settingsWindow.FindView("chkGlobal", out Checkbox chkGlobal);
            bool GlobalScope = chkGlobal.IsChecked;

            //_multiListView.DeleteAllChildren();

            Rules.Add(new Rule(_itemName.Text.Trim(), _itemMinQL.Text, _itemMaxQL.Text, GlobalScope, _itemQuantity.Text, "loot"));
            
            _itemName.Text = "";
            _itemMinQL.Text = "1";
            _itemMaxQL.Text = "500";
            _itemQuantity.Text = "999";
            txErr.Text = "";
            SaveRules();
            RefreshList();
        }

        private static void RefreshList()
        {
            SettingsController.settingsWindow.FindView("ScrollListRoot", out MultiListView _multiListView);
            
            _multiListView.DeleteAllChildren();
            
            Rules = Rules.OrderBy(o => o.Name.ToUpper()).ToList();

            int iEntry = 0;
            foreach (Rule r in Rules)
            {
                View entry = View.CreateFromXml(PluginDir + "\\UI\\ItemEntry.xml");
                entry.FindChild("ItemName", out TextView _textView);
                string globalscope = r.Global ? "G" : "L";
 
                _textView.Text = $"{(iEntry + 1).ToString()} - {globalscope} - [ {r.Lql.PadLeft(3, ' ')} - {r.Hql.PadLeft(3, ' ')} ] - {r.Name} - {r.Quantity} - {r.BagName}";

                _multiListView.AddChild(entry, false);
                iEntry++;
            }
            Chat.WriteLine($"Refreshed {Rules.Count} rules");
        }

        private void remButtonClicked(object sender, ButtonBase e)
        {
            try
            {

                SettingsController.settingsWindow.FindView("tivindex", out TextInputView txIndex);
                SettingsController.settingsWindow.FindView("tvErr", out TextView txErr);

                if (txIndex.Text.Trim() == "")
                {
                    txErr.Text = "Cant remove an empty entry";
                    return;
                }

                int index = 0;

                try
                {
                    index = Convert.ToInt32(txIndex.Text) - 1;
                }
                catch
                {
                    txErr.Text = "Entry must be a number!";
                    return;
                }

                if (index < 0 || index >= Rules.Count)
                {
                    txErr.Text = "Invalid entry!";
                    return;
                }

                Rules.RemoveAt(index);

                //_multiListView.DeleteAllChildren();
                //viewitems.Clear();

                txErr.Text = "";
                SaveRules();
                RefreshList();
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
        }
 
        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDir + "\\UI\\" + xmlName, _settings);
        }

        private void SaveBackpackDictionaryToJson()
        {
            try
            {
                // Create a list to store the serialized BackpackInfoData objects
                List<BackpackInfo> backpackInfoList = new List<BackpackInfo>();

                // Convert each BackpackInfo object in backpackDictionary to BackpackInfoData and add to the list
                foreach (var backpackInfo in backpackDictionary.Values)
                {
                    backpackInfoList.Add(new BackpackInfo
                    {
                        Name = backpackInfo.Name,
                        FreeSlots = backpackInfo.FreeSlots,
                        ItemNames = backpackInfo.ItemNames
                    });
                }

                // Serialize the list to JSON
                string json = JsonConvert.SerializeObject(backpackInfoList, Formatting.Indented);

                // Define the path for the JSON file
                string filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\" +
                    $"{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\LootManager\\{DynelManager.LocalPlayer.Name}\\BackpackInfo.json";

                // Write the JSON data to the file
                File.WriteAllText(filename, json);

            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during the save process
                Chat.WriteLine("Error while saving backpackDictionary to JSON: " + ex.Message);
            }
        }

        private void LoadRules()
        {
            Rules = new List<Rule>();

            string filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\LootManager\\Global.json";
            if (File.Exists(filename))
            {
                string rulesJson = File.ReadAllText(filename);
                Rules = JsonConvert.DeserializeObject<List<Rule>>(rulesJson);
                foreach (Rule rule in Rules)
                {
                    if (string.IsNullOrEmpty(rule.Quantity))
                        rule.Quantity = "999";
                    if (string.IsNullOrEmpty(rule.BagName))
                        rule.BagName = "loot";
                    rule.Global = true;
                }
                    
            }

            filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\LootManager\\{DynelManager.LocalPlayer.Name}\\Rules.json";
            if (File.Exists(filename))
            {
                List<Rule> scopedRules = new List<Rule>();
                string rulesJson = File.ReadAllText(filename);
                scopedRules = JsonConvert.DeserializeObject<List<Rule>>(rulesJson);
                foreach (Rule rule in scopedRules)
                {
                    rule.Global = false;
                    if (string.IsNullOrEmpty(rule.Quantity))
                        rule.Quantity = "999";
                    if (string.IsNullOrEmpty(rule.BagName))
                        rule.BagName = "loot";
                    Rules.Add(rule);
                }
                Chat.WriteLine($"Loaded {scopedRules.Count.ToString()}");
            }
            Rules = Rules.OrderBy(o => o.Name.ToUpper()).ToList();
            
        }

        private void SaveRules()
        {
            List<Rule> GlobalRules = new List<Rule>();
            List<Rule> ScopeRules = new List<Rule>();

            string filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\LootManager\\Global.json";

            GlobalRules = Rules.Where(o => o.Global == true).ToList();
            ScopeRules = Rules.Where(o => o.Global == false).ToList();

            string rulesJson = JsonConvert.SerializeObject(GlobalRules);
            File.WriteAllText(filename, rulesJson);

            filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\LootManager\\{DynelManager.LocalPlayer.Name}\\Rules.json";
            rulesJson = JsonConvert.SerializeObject(ScopeRules);
            File.WriteAllText(filename, rulesJson);
            Chat.WriteLine($"Saved {ScopeRules.Count.ToString()} Rules");
        }

        public bool CheckRules(Item item, bool updateRule = false)
        {
            foreach (Rule rule in Rules)
            {
                if (_settings["Exact"].AsBool())
                {
                    if (String.Equals(item.Name, rule.Name, StringComparison.OrdinalIgnoreCase) &&
                        item.QualityLevel >= Convert.ToInt32(rule.Lql) &&
                        item.QualityLevel <= Convert.ToInt32(rule.Hql) &&
                        Convert.ToInt32(rule.Quantity) >= 1)
                    {
                        UpdateRule(rule, updateRule);
                        return true;
                    }
                }
                else
                {
                    if (item.Name.ToUpper().Contains(rule.Name.ToUpper()) &&
                        item.QualityLevel >= Convert.ToInt32(rule.Lql) &&
                        item.QualityLevel <= Convert.ToInt32(rule.Hql) &&
                        Convert.ToInt32(rule.Quantity) >= 1
                        )
                    {
                        UpdateRule(rule, updateRule);
                        return true;
                    }
                }
            }
            return false;
        }

        private void UpdateRule(Rule rule, bool update)
        {
            if (!update || Convert.ToInt32(rule.Quantity) == 999) return;
            rule.Quantity = (Convert.ToInt32(rule.Quantity) - 1).ToString();
            Chat.WriteLine($"Rule {rule.Name} - {rule.Quantity}");
            if (Convert.ToInt32(rule.Quantity) == 0)
            {
                Chat.WriteLine($"Removing Rule {rule.Name}");
                Rules.Remove(rule);
            }
            SaveRules();
            RefreshList();
        }

        public static int GetLineNumber(Exception ex)
        {
            var lineNumber = 0;

            var lineMatch = Regex.Match(ex.StackTrace ?? "", @":line (\d+)$", RegexOptions.Multiline);

            if (lineMatch.Success)
                lineNumber = int.Parse(lineMatch.Groups[1].Value);

            return lineNumber;
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

    public class BackpackInfo
    {
        public string Name { get; set; }
        public int FreeSlots { get; set; }
        public List<string> ItemNames { get; set; }

        public BackpackInfo()
        {
            Name = string.Empty;
            FreeSlots = 0;
            ItemNames = new List<string>();
        }

        public BackpackInfo(string name, int freeSlots)
        {
            Name = name;
            FreeSlots = freeSlots;
            ItemNames = new List<string>();
        }
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
        public int Quantity;
        public string BagName;
    }
}
