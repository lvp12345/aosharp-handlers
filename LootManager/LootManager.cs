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

namespace LootManager
{
    public class LootManager : AOPluginEntry
    {
        public static string previousErrorMessage = string.Empty;

        protected double _lastZonedTime = Time.NormalTime;
        private double uiDelay;
        double _lootingTimer;

        AutoResetInterval openCorpseInterval = new AutoResetInterval(5000); // 5000 milliseconds = 5 seconds
        AutoResetInterval closeCorpseInterval = new AutoResetInterval(3000);
        Corpse currentCorpse = null;

        public static Config Config { get; private set; }

        public static List<Rule> Rules;

        private Dictionary<Vector3, Identity> openedCorpses = new Dictionary<Vector3, Identity>();

        private Dictionary<Identity, BackpackInfo> backpackDictionary = new Dictionary<Identity, BackpackInfo>();

        protected Settings _settings;
        public static Settings _settingsItems;

        private Window _infoWindow;

        public static string PluginDir;

        bool isBackpackInfoInitialized = false;

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

                LoadRules();

                _settings.AddVariable("Enabled", false);
                _settings.AddVariable("Delete", false);
                _settings.AddVariable("Exact", false);

                Chat.RegisterCommand("lm", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    _settings["Enabled"] = !_settings["Enabled"].AsBool();
                    Chat.WriteLine($"Enabled : {_settings["Enabled"]}");
                });

                Chat.WriteLine("Loot Manager loaded!");
                Chat.WriteLine("/lootmanager for settings. /lm to enable/disable");
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
            var originalEnabledValue = _settings["Enabled"].AsBool();
            _settings["Enabled"] = false; // Set to default value

            Config.Save();
            SaveRules();
            SettingsController.CleanUp();

            _settings["Enabled"] = originalEnabledValue; // Revert back to original value after saving
        }


        private void MoveItemsToBag()
        {
            if (!backpackDictionary.Any()) // If no backpacks are available, return
            {
                return;
            }

            // Find a backpack with the name containing "loot" and less than 21 items
            var availableBackpack = backpackDictionary.FirstOrDefault(backpack => backpack.Value.FreeSlots > 0 && backpack.Value.Name.Contains("loot"));

            if (!availableBackpack.Equals(default(KeyValuePair<Identity, BackpackInfo>)))
            {
                // Chat.WriteLine($"Found an available backpack: {availableBackpack.Value.Name}");

                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot.Type == IdentityType.Inventory))
                {
                    // Only check to move if there is something to move
                    if (CheckRules(itemtomove))
                    {
                        //Chat.WriteLine($"Moving item {itemtomove.Name} to {availableBackpack.Value.Name}");

                        // Move the item to the available backpack with free space
                        itemtomove.MoveToContainer(availableBackpack.Key);

                        // Decrease the free slot count in the backpackDictionary
                        availableBackpack.Value.FreeSlots--;

                        //Chat.WriteLine($"Item {itemtomove.Name} moved to {availableBackpack.Value.Name}");

                        // No need to check further, break out of the loop - item moved
                        break;
                    }
                }
            }
        }

        private void UpdateBackpackDictionary()
        {
            bool dictionaryChanged = false; // Flag to track if there's a change in the dictionary

            foreach (var backpack in Inventory.Backpacks.Where(b => b.Name.Contains("loot")))
            {
                int freeSlots = 21 - backpack.Items.Count;
                if (backpackDictionary.ContainsKey(backpack.Identity))
                {
                    // Check if there's a change in free slots
                    if (backpackDictionary[backpack.Identity].FreeSlots != freeSlots)
                    {
                        //Chat.WriteLine($"Updating existing backpack entry: {backpack.Name}, Free Slots: {freeSlots}");

                        // Update the existing entry with new information
                        backpackDictionary[backpack.Identity].FreeSlots = freeSlots;
                        dictionaryChanged = true; // Mark dictionary as changed
                    }
                }
                else
                {
                    //Chat.WriteLine($"Adding a new backpack entry: {backpack.Name}, Free Slots: {freeSlots}");

                    // Add a new entry for the backpack
                    backpackDictionary.Add(backpack.Identity, new BackpackInfo
                    {
                        Name = backpack.Name,
                        FreeSlots = freeSlots
                    });
                    dictionaryChanged = true; // Mark dictionary as changed
                }
            }

            // Only set the flag to indicate that initialization is done if there was a change
            if (dictionaryChanged)
            {
                isBackpackInfoInitialized = true;
                //Chat.WriteLine("Backpack information updated.");
            }
            else
            {
                //Chat.WriteLine("No changes in backpack information.");
            }
        }


        private void InitializeBackpackInfo()
        {
            if (isBackpackInfoInitialized)
            {
                //Chat.WriteLine("Backpack information is already initialized.");
                return; // Already initialized, no need to do it again
            }

            //Chat.WriteLine("Initializing backpack information...");

            // Open all backpacks to set the item count and names
            List<Item> bags = Inventory.Items.Where(c => c.UniqueIdentity.Type == IdentityType.Container).ToList();
            foreach (Item bag in bags)
            {
                bag.Use();
                bag.Use();
            }

            // Chat.WriteLine("Backpacks have been opened for initialization.");

            // Loop through all backpacks to update backpackDictionary
            foreach (Backpack backpack in Inventory.Backpacks)
            {
                UpdateBackpackDictionary();
            }

            //Chat.WriteLine("Backpack information initialization complete.");

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

                if (CheckRules(item))
                    item.MoveToInventory();
                else if (_settings["Delete"].AsBool())
                    item.Delete();
            }
        }
        public void ProcessCorpses()
        {
            Corpse corpseToOpen = DynelManager.Corpses.FirstOrDefault(c =>
                c.DistanceFrom(DynelManager.LocalPlayer) < 9 &&
                !openedCorpses.ContainsKey(c.Position));

            if (corpseToOpen != null)
            {
                if (Spell.List.Any(c => c.IsReady) && !Spell.HasPendingCast
                    && !DynelManager.LocalPlayer.IsAttacking && DynelManager.LocalPlayer.FightingTarget == null
                    && !DynelManager.LocalPlayer.IsAttackPending)
                {
                    // Check if it's time to open a new corpse
                    if (openCorpseInterval.Elapsed)
                    {
                        currentCorpse = corpseToOpen;
                        currentCorpse.Open(); // Open the corpseToOpen
                                              //Chat.WriteLine("Opened a corpse.");

                        // Reset the closeCorpseInterval when opening a corpse
                        closeCorpseInterval.Reset();
                    }

                }
                if (currentCorpse != null && closeCorpseInterval.Elapsed)
                {
                    // Close the currentCorpse
                    currentCorpse.Open();
                    openedCorpses[currentCorpse.Position] = currentCorpse.Identity;
                    currentCorpse = null;
                    //Chat.WriteLine("Closed the current corpse.");

                    // Reset the closeCorpseInterval after closing the currentCorpse
                    //closeCorpseInterval.Reset();
                }
            }
        }
        private void OnUpdate(object sender, float deltaTime)
        {
            try
            {
                if (Game.IsZoning)
                {
                    return;
                }

                if (_settings["Enabled"].AsBool())
                {
                    InitializeBackpackInfo();

                    

                    if (backpackDictionary.Values.All(backpack => backpack.FreeSlots == 0))
                    {
                        if (Inventory.NumFreeSlots == 0)
                        {
                            _settings["Enabled"] = false;
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
                            if (Spell.List.Any(c => c.IsReady) && !Spell.HasPendingCast && Time.NormalTime > _lootingTimer + 2)
                            {
                                corpse.Open();
                                _lootingTimer = Time.NormalTime;
                            }
                        }
                    }

                    MoveItemsToBag();
                    UpdateBackpackDictionary();
                }

                #region UI

                if (Time.NormalTime > uiDelay + 0.5)
                {
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

                    uiDelay = Time.NormalTime;
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
            SettingsController.settingsWindow.FindView("ScrollListRoot", out MultiListView _multiListView);

            SettingsController.settingsWindow.FindView("tivName", out TextInputView _itemName);
            SettingsController.settingsWindow.FindView("_itemMinQL", out TextInputView _itemMinQL);
            SettingsController.settingsWindow.FindView("_itemMaxQL", out TextInputView _itemMaxQL);

            SettingsController.settingsWindow.FindView("tvErr", out TextView txErr);

            if (_itemName.Text.Trim() == "")
            {
                txErr.Text = "Can't add an empty name";
                return;
            }

            int minql = 0;
            int maxql = 0;
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


            SettingsController.settingsWindow.FindView("chkGlobal", out Checkbox chkGlobal);
            bool GlobalScope = chkGlobal.IsChecked;


            _multiListView.DeleteAllChildren();


            Rules.Add(new Rule(_itemName.Text.Trim(), _itemMinQL.Text, _itemMaxQL.Text, GlobalScope));

            Rules = Rules.OrderBy(o => o.Name.ToUpper()).ToList();

            int iEntry = 0;
            foreach (Rule r in Rules)
            {
                View entry = View.CreateFromXml(PluginDir + "\\UI\\ItemEntry.xml");
                entry.FindChild("ItemName", out TextView _textView);
                string globalscope = "";
                if (r.Global)
                    globalscope = "G";
                else
                    globalscope = "L";

                //entry.Tag = iEntry;
                _textView.Text = (iEntry + 1).ToString() + " - " + globalscope + " - [" + r.Lql.PadLeft(3, ' ') + "-" + r.Hql.PadLeft(3, ' ') + " ] - " + r.Name;

                _multiListView.AddChild(entry, false);
                iEntry++;
            }


            _itemName.Text = "";
            _itemMinQL.Text = "1";
            _itemMaxQL.Text = "500";
            txErr.Text = "";

        }

        private void remButtonClicked(object sender, ButtonBase e)
        {
            try
            {
                SettingsController.settingsWindow.FindView("ScrollListRoot", out MultiListView _multiListView);

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

                _multiListView.DeleteAllChildren();
                //viewitems.Clear();

                int iEntry = 0;
                foreach (Rule r in Rules)
                {
                    View entry = View.CreateFromXml(PluginDir + "\\UI\\ItemEntry.xml");
                    entry.FindChild("ItemName", out TextView tx);

                    //entry.Tag = iEntry;

                    string scope = "";
                    if (r.Global)
                        scope = "G";
                    else
                        scope = "L";
                    tx.Text = (iEntry + 1).ToString() + " - " + scope + " - [" + r.Lql.PadLeft(3, ' ') + "-" + r.Hql.PadLeft(3, ' ') + "] - " + r.Name;


                    _multiListView.AddChild(entry, false);
                    iEntry++;
                }

                txErr.Text = "";
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

        private void LoadRules()
        {
            Rules = new List<Rule>();

            string filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\LootManager\\Global.json";
            if (File.Exists(filename))
            {
                string rulesJson = File.ReadAllText(filename);
                Rules = JsonConvert.DeserializeObject<List<Rule>>(rulesJson);
                foreach (Rule rule in Rules)
                    rule.Global = true;
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
                    Rules.Add(rule);
                }
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
        }

        public bool CheckRules(Item item)
        {
            foreach (Rule rule in Rules)
            {
                if (_settings["Exact"].AsBool())
                {
                    if (String.Equals(item.Name, rule.Name, StringComparison.OrdinalIgnoreCase) &&
                        item.QualityLevel >= Convert.ToInt32(rule.Lql) &&
                        item.QualityLevel <= Convert.ToInt32(rule.Hql))
                    {
                        return true;
                    }
                }
                else
                {
                    if (item.Name.ToUpper().Contains(rule.Name.ToUpper()) &&
                        item.QualityLevel >= Convert.ToInt32(rule.Lql) &&
                        item.QualityLevel <= Convert.ToInt32(rule.Hql))
                    {
                        return true;
                    }
                }
            }
            return false;
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
    }
}
