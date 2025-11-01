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
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LootManager
{
    public class LootManager : AOPluginEntry
    {
        protected Settings _settings;
        public static Settings _settingsItems;
        private Window _infoWindow;

        public static Config Config { get; private set; }
        public static string PluginDir;

        public static List<Rule> Rules;

        public static string previousErrorMessage = string.Empty;
        bool isBackpackInfoInitialized = false;
        double moveDelay;

        // Item database for autocomplete
        private static List<string> ItemDatabase = new List<string>();
        private static List<string> FilteredItems = new List<string>();
        private static string LastSearchText = "";
        private static TextInputView CurrentItemNameInput;

        // Track items looted with Loot All enabled (name + timestamp)
        private static Dictionary<string, double> _lootAllItems = new Dictionary<string, double>();

        // Track corpses that have been fully processed (not just opened)
        private static HashSet<uint> _fullyProcessedCorpses = new HashSet<uint>();
        private static double _corpseCleanupTimer = 0;

        // Track currently open corpses for dynamic list updates
        private static List<Container> _openCorpses = new List<Container>();

        // Track corpses that have been opened to prevent spam opening
        private static Dictionary<uint, double> _corpseOpenedTime = new Dictionary<uint, double>();

        // Track corpse processing state to prevent premature closing
        private static Dictionary<uint, double> _corpseLastItemTime = new Dictionary<uint, double>();
        private static Dictionary<uint, bool> _corpseHasItems = new Dictionary<uint, bool>();
        private static Dictionary<uint, bool> _corpseNeedsReprocessing = new Dictionary<uint, bool>();
        private static Dictionary<uint, double> _corpseLastInventoryFullTime = new Dictionary<uint, double>();

        public override void Run()
        {
            try
            {
                
                _settings = new Settings("LootManager");

                PluginDir = PluginDirectory;

                Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\LootManager\\{DynelManager.LocalPlayer.Name}\\Config.json");

                Game.OnUpdate += OnUpdate;
                Inventory.ContainerOpened += ContainerOpened;

                RegisterSettingsWindow("Loot Manager", "LootManagerSettingWindow.xml");

                _settings.AddVariable("Enabled", false);
                _settings.AddVariable("Delete", false);
                _settings.AddVariable("Disable", false);
                _settings.AddVariable("ExactMatchSelection", 0);
                _settings.AddVariable("LootAll", false);
                _settings.AddVariable("LeaveOpen", false);

                LoadRules();
                LoadItemDatabase();

                Chat.RegisterCommand("lm", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    _settings["Enabled"] = !_settings["Enabled"].AsBool();
                });

                Chat.RegisterCommand("lmsave", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    if (param.Length == 0)
                    {
                        Chat.WriteLine("Usage: /lmsave <name>");
                        Chat.WriteLine("Example: /lmsave Monster Parts");
                        Chat.WriteLine("Example: /lmsave Daily Farming");
                        return;
                    }

                    string configName = string.Join(" ", param);
                    SaveLootListWithName(configName);
                });

                Chat.RegisterCommand("lmload", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    if (param.Length == 0)
                    {
                        Chat.WriteLine("Usage: /lmload <number>, /lmload <name>, or /lmload latest");
                        Chat.WriteLine("Click 'Load List' button first to see available files.");
                        return;
                    }

                    string loadParam = string.Join(" ", param);

                    if (loadParam.ToLower() == "latest")
                    {
                        LoadSelectedExportFile(0); // First file is most recent
                    }
                    else if (int.TryParse(param[0], out int fileNumber) && param.Length == 1)
                    {
                        LoadSelectedExportFile(fileNumber - 1); // Convert to 0-based index
                    }
                    else
                    {
                        LoadLootListByName(loadParam);
                    }
                });

                Chat.RegisterCommand("lmlist", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    ListAvailableLootLists();
                });



                if (!Game.IsNewEngine)
                {
                    Chat.WriteLine("Loot Manager loaded!");
                    Chat.WriteLine("/lootmanager for settings. /lm to enable/disable");

                    // Auto-enable LootManager at startup
                    _settings["Enabled"] = true;

                    // Auto-open the LootManager settings window
                    OpenLootManagerWindow();
                }
                else
                {
                    Chat.WriteLine("Does not work on this engine!");
                }

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



        private void ContainerOpened(object sender, Container container)
        {
            try
            {
                if (!_settings["Enabled"].AsBool()) { return; }

                switch (container.Identity.Type)
                {
                    case IdentityType.Corpse:
                        // Track this corpse as currently open for dynamic updates
                        if (!_openCorpses.Any(c => c.Identity.Instance == container.Identity.Instance))
                        {
                            _openCorpses.Add(container);
                        }

                        // Record when this corpse was opened to prevent spam opening
                        _corpseOpenedTime[(uint)container.Identity.Instance] = Time.AONormalTime;



                        // Track if this corpse has any items to process
                        uint corpseId = (uint)container.Identity.Instance;
                        bool hasItemsToProcess = false;
                        bool inventoryWasFull = false;

                        foreach (var item in container.Items.ToList())
                        {
                            bool itemProcessed = false;
                            bool itemCouldBeProcessed = false;

                            // Check if "Loot All" is enabled
                            if (_settings["LootAll"].AsBool())
                            {
                                itemCouldBeProcessed = true;
                                if (Inventory.NumFreeSlots >= 1)
                                {
                                    LogLootAction(item.Name, "LOOTED", "Loot All enabled");

                                    // Track this item by name and timestamp for Loot All
                                    _lootAllItems[item.Name] = Time.AONormalTime;

                                    item.MoveToInventory();
                                    itemProcessed = true;
                                }
                                else
                                {
                                    inventoryWasFull = true;
                                }
                            }
                            else if (CheckRules(item, true))
                            {
                                itemCouldBeProcessed = true;
                                if (Inventory.NumFreeSlots >= 1)
                                {
                                    LogLootAction(item.Name, "LOOTED", "matches loot list");
                                    item.MoveToInventory();
                                    itemProcessed = true;
                                }
                                else
                                {
                                    inventoryWasFull = true;
                                }
                            }
                            else if (_settings["Delete"].AsBool())
                            {
                                LogLootAction(item.Name, "DELETED", "not in list");
                                item?.Delete();
                                itemProcessed = true;
                            }

                            if (itemProcessed)
                            {
                                hasItemsToProcess = true;
                                _corpseLastItemTime[corpseId] = Time.AONormalTime;
                            }
                            else if (itemCouldBeProcessed)
                            {
                                // Item could be processed but wasn't (likely due to full inventory)
                                hasItemsToProcess = true;
                            }
                        }

                        // Track if this corpse had items to process
                        _corpseHasItems[corpseId] = hasItemsToProcess;

                        // If inventory was full, mark corpse for reprocessing after bag movement
                        if (inventoryWasFull)
                        {
                            _corpseNeedsReprocessing[corpseId] = true;
                            _corpseLastInventoryFullTime[corpseId] = Time.AONormalTime;
                        }

                        // Always schedule intelligent closing/reprocessing
                        ScheduleCorpseClosing(container);

                        break;
                }
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
            // Unregister event handlers to prevent crashes
            Game.OnUpdate -= OnUpdate;
            Inventory.ContainerOpened -= ContainerOpened;

            Config.Save();
            SaveRules();
            SettingsController.CleanUp();
        }

        private void MoveItemsToBag()
        {
            // Find loot bags (case insensitive search)
            var backpack = Inventory.Backpacks.Where(a => a.Name.ToLower().Contains("loot")).OrderBy(b => b.Name).FirstOrDefault(c => c.Items.Count >= 0 && c.Items.Count < 21);

            if (Time.AONormalTime < moveDelay) { return; }

            // Show error if no loot bags found when Loot All is enabled
            if (_settings["LootAll"].AsBool() && backpack == null)
            {
                var allBags = Inventory.Backpacks.ToList();
                if (allBags.Count > 0)
                {
                    Chat.WriteLine($"No loot bags found. Available bags: {string.Join(", ", allBags.Select(b => b.Name))}");
                }
                else
                {
                    Chat.WriteLine("No backpacks found at all for moving items");
                }
                return;
            }

            foreach (var itemtomove in Inventory.Items.Where(c => c.Slot.Type == IdentityType.Inventory))
            {
                if (backpack == null) { return; }

                // Skip backpacks/containers - don't try to move them
                if (itemtomove.Name.ToLower().Contains("backpack") ||
                    itemtomove.Name.ToLower().Contains("bag") ||
                    itemtomove.Name.ToLower().Contains("container"))
                {
                    continue;
                }

                // If "Loot All" is enabled, only move items that were looted with Loot All
                if (_settings["LootAll"].AsBool())
                {
                    // Only move items that were tracked as looted with Loot All (within last 10 seconds)
                    if (_lootAllItems.ContainsKey(itemtomove.Name) &&
                        (Time.AONormalTime - _lootAllItems[itemtomove.Name]) < 10.0)
                    {
                        // Remove from tracking since we're moving it
                        _lootAllItems.Remove(itemtomove.Name);

                        itemtomove.MoveToContainer(backpack);
                        break;
                    }

                }
                // Otherwise, only move items that match rules
                else if (CheckRules(itemtomove))
                {
                    itemtomove.MoveToContainer(backpack);
                    break;
                }
            }

            moveDelay = Time.AONormalTime + 0.2;

        }

        private void InitializeBackpackInfo()
        {
            if (isBackpackInfoInitialized) { return; }

            var lootBags = Inventory.Backpacks.Where(bag => bag.Name.Contains("loot")).ToList();
            var bagItems = new List<Item>();

            // Collect all loot bag items
            foreach (var item in Inventory.Items)
            {
                if (lootBags.Any(bag => bag.Identity.Instance == item.UniqueIdentity.Instance))
                {
                    bagItems.Add(item);
                }
            }

            // Open all bags simultaneously
            foreach (var item in bagItems)
            {
                item?.Use(); // Open
            }

            // Wait 1 second then close all bags
            if (bagItems.Count > 0)
            {
                Task.Delay(1000).ContinueWith(_ =>
                {
                    try
                    {
                        foreach (var item in bagItems)
                        {
                            item?.Use(); // Close
                        }
                    }
                    catch (Exception ex)
                    {
                        Chat.WriteLine($"Error closing bags in InitializeBackpackInfo: {ex.Message}");
                    }
                });
            }

            isBackpackInfoInitialized = true;
        }

        public void ProcessCorpses()
        {
            // Clean up old processed corpses every 3 seconds
            if (Time.AONormalTime > _corpseCleanupTimer)
            {
                CleanupProcessedCorpses();
                CleanupOpenCorpses();
                _corpseCleanupTimer = Time.AONormalTime + 3.0;
            }

            // Get all corpses within range that haven't been fully processed
            var corpses = DynelManager.Corpses.Where(c =>
                DynelManager.LocalPlayer.Position.DistanceFrom(c.Position) < 5 &&
                !_fullyProcessedCorpses.Contains((uint)c.Identity.Instance)).ToList();

            if (corpses.Count == 0) { return; }
            if (Spell.HasPendingCast) { return; }
            if (Item.HasPendingUse) { return; }
            if (PerkAction.List.Any(perk => perk.IsExecuting)) { return; }

            // Open all nearby corpses that haven't been opened recently
            foreach (var corpse in corpses)
            {
                uint corpseId = (uint)corpse.Identity.Instance;

                // Check if this corpse was opened recently (within last 2 seconds) to prevent spam
                if (_corpseOpenedTime.ContainsKey(corpseId) &&
                    (Time.AONormalTime - _corpseOpenedTime[corpseId]) < 2.0)
                {
                    continue; // Skip this corpse, it was opened recently
                }

                // Check if corpse is already open
                if (_openCorpses.Any(c => c.Identity.Instance == corpse.Identity.Instance))
                {
                    continue; // Skip, already open
                }

                corpse.Open(); // Open corpse
            }
        }

        private void CleanupProcessedCorpses()
        {
            try
            {
                // Get all current corpse IDs within a larger range
                var currentCorpseIds = DynelManager.Corpses
                    .Where(c => DynelManager.LocalPlayer.Position.DistanceFrom(c.Position) < 20)
                    .Select(c => (uint)c.Identity.Instance)
                    .ToHashSet();

                // Remove fully processed corpses that no longer exist
                var corpsesToRemove = _fullyProcessedCorpses.Where(id => !currentCorpseIds.Contains(id)).ToList();
                foreach (var corpseId in corpsesToRemove)
                {
                    _fullyProcessedCorpses.Remove(corpseId);
                }

                // Clean up opened time tracking for corpses that no longer exist
                var openedTimesToRemove = _corpseOpenedTime.Keys.Where(id => !currentCorpseIds.Contains(id)).ToList();
                foreach (var corpseId in openedTimesToRemove)
                {
                    _corpseOpenedTime.Remove(corpseId);
                }

                // Clean up old opened times (older than 10 seconds)
                var oldOpenedTimes = _corpseOpenedTime.Where(kvp =>
                    (Time.AONormalTime - kvp.Value) > 10.0).Select(kvp => kvp.Key).ToList();
                foreach (var corpseId in oldOpenedTimes)
                {
                    _corpseOpenedTime.Remove(corpseId);
                }

                // Silently clean up old corpses
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error cleaning up processed corpses: {ex.Message}");
            }
        }

        private void CloseCorpseAfterProcessing(Container container)
        {
            try
            {
                // Check if "Leave corpse open" is enabled - if so, never close
                if (_settings["LeaveOpen"].AsBool())
                {
                    return;
                }

                // Determine if we should close the corpse based on current settings
                bool shouldClose = false;

                if (!_settings["Delete"].AsBool() && !_settings["LootAll"].AsBool())
                {
                    // Neither Delete nor Loot All - close corpse when all list items are looted
                    shouldClose = true;
                }
                else if (_settings["Delete"].AsBool())
                {
                    // Delete mode - corpse will be processed for deletion, keep open for now
                    shouldClose = false;
                }
                else if (_settings["LootAll"].AsBool())
                {
                    // Loot All mode - close after looting everything
                    shouldClose = true;
                }

                if (shouldClose)
                {
                    // Schedule corpse closing after a longer delay to ensure all items are processed
                    Task.Delay(2000).ContinueWith(_ =>
                    {
                        try
                        {
                            // Find the corpse and close it
                            var corpse = DynelManager.Corpses.FirstOrDefault(c =>
                                c.Identity.Instance == container.Identity.Instance);

                            if (corpse != null)
                            {
                                corpse.Open(); // Toggle to close
                            }
                        }
                        catch
                        {
                            // Silently handle errors
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error in CloseCorpseAfterProcessing: {ex.Message}");
            }
        }

        private void ScheduleCorpseClosing(Container container)
        {
            try
            {
                uint corpseId = (uint)container.Identity.Instance;

                // Schedule a check to see if the corpse should be closed
                // This runs repeatedly until all item processing is complete
                Task.Delay(1000).ContinueWith(_ =>
                {
                    CheckAndCloseCorpse(corpseId);
                });
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error scheduling corpse closing: {ex.Message}");
            }
        }

        private void CheckAndCloseCorpse(uint corpseId)
        {
            try
            {
                // Find the corpse
                var corpse = DynelManager.Corpses.FirstOrDefault(c =>
                    (uint)c.Identity.Instance == corpseId);

                if (corpse == null)
                {
                    // Corpse no longer exists, clean up tracking
                    CleanupCorpseTracking(corpseId);
                    return;
                }

                // Check if "Leave corpse open" is enabled
                if (_settings["LeaveOpen"].AsBool())
                {
                    return;
                }

                // Find the opened container for this corpse
                var openContainer = _openCorpses.FirstOrDefault(c =>
                    (uint)c.Identity.Instance == corpseId);

                if (openContainer == null)
                {
                    // Container not found, clean up and close corpse
                    CleanupCorpseTracking(corpseId);
                    return;
                }

                // Check if corpse needs reprocessing due to inventory space becoming available
                if (_corpseNeedsReprocessing.ContainsKey(corpseId) && _corpseNeedsReprocessing[corpseId])
                {
                    // Check if inventory has space now and some time has passed for bag movement
                    if (Inventory.NumFreeSlots >= 1 &&
                        _corpseLastInventoryFullTime.ContainsKey(corpseId) &&
                        (Time.AONormalTime - _corpseLastInventoryFullTime[corpseId]) > 2.0)
                    {
                        // Reprocess the corpse now that we have inventory space
                        ReprocessCorpseItems(openContainer);
                        _corpseNeedsReprocessing[corpseId] = false;

                        // Schedule another check after reprocessing
                        Task.Delay(1000).ContinueWith(_ =>
                        {
                            CheckAndCloseCorpse(corpseId);
                        });
                        return;
                    }
                    else if (Inventory.NumFreeSlots == 0)
                    {
                        // Still no space, check again later
                        Task.Delay(2000).ContinueWith(_ =>
                        {
                            CheckAndCloseCorpse(corpseId);
                        });
                        return;
                    }
                }

                // Check if enough time has passed since the last item was processed
                if (_corpseLastItemTime.ContainsKey(corpseId))
                {
                    double timeSinceLastItem = Time.AONormalTime - _corpseLastItemTime[corpseId];

                    // Wait at least 2 seconds after the last item was processed
                    if (timeSinceLastItem < 2.0)
                    {
                        // Not enough time has passed, schedule another check
                        Task.Delay(1000).ContinueWith(_ =>
                        {
                            CheckAndCloseCorpse(corpseId);
                        });
                        return;
                    }
                }

                // Check if there are still items that could be processed
                bool stillHasRelevantItems = false;

                foreach (var item in openContainer.Items.ToList())
                {
                    if (_settings["LootAll"].AsBool())
                    {
                        stillHasRelevantItems = true;
                        break;
                    }
                    else if (CheckRules(item, false))
                    {
                        stillHasRelevantItems = true;
                        break;
                    }
                    else if (_settings["Delete"].AsBool() && !CheckRules(item, false))
                    {
                        stillHasRelevantItems = true;
                        break;
                    }
                }

                if (stillHasRelevantItems)
                {
                    // Check if we can process items now
                    if (Inventory.NumFreeSlots >= 1 || _settings["Delete"].AsBool())
                    {
                        // We have space or can delete, reprocess the corpse
                        ReprocessCorpseItems(openContainer);

                        // Schedule another check after reprocessing
                        Task.Delay(1000).ContinueWith(_ =>
                        {
                            CheckAndCloseCorpse(corpseId);
                        });
                        return;
                    }
                    else
                    {
                        // No space and can't delete, wait for space
                        _corpseNeedsReprocessing[corpseId] = true;
                        _corpseLastInventoryFullTime[corpseId] = Time.AONormalTime;

                        Task.Delay(2000).ContinueWith(_ =>
                        {
                            CheckAndCloseCorpse(corpseId);
                        });
                        return;
                    }
                }

                // All items processed, mark corpse as fully processed
                _fullyProcessedCorpses.Add(corpseId);

                // Close the corpse if appropriate
                bool shouldClose = ShouldCloseCorpse();

                if (shouldClose)
                {
                    corpse.Open(); // Toggle to close
                }

                // Clean up tracking
                CleanupCorpseTracking(corpseId);
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error checking corpse for closing: {ex.Message}");
            }
        }

        private void CleanupCorpseTracking(uint corpseId)
        {
            _corpseLastItemTime.Remove(corpseId);
            _corpseHasItems.Remove(corpseId);
            _corpseNeedsReprocessing.Remove(corpseId);
            _corpseLastInventoryFullTime.Remove(corpseId);
        }

        private void ReprocessCorpseItems(Container container)
        {
            try
            {
                uint corpseId = (uint)container.Identity.Instance;
                bool processedAnyItems = false;

                foreach (var item in container.Items.ToList())
                {
                    bool itemProcessed = false;

                    // Check if "Loot All" is enabled
                    if (_settings["LootAll"].AsBool())
                    {
                        if (Inventory.NumFreeSlots >= 1)
                        {
                            LogLootAction(item.Name, "LOOTED", "Loot All continued");

                            // Track this item by name and timestamp for Loot All
                            _lootAllItems[item.Name] = Time.AONormalTime;

                            item.MoveToInventory();
                            itemProcessed = true;
                        }
                        else
                        {
                            // Still no space, mark for reprocessing
                            _corpseNeedsReprocessing[corpseId] = true;
                            _corpseLastInventoryFullTime[corpseId] = Time.AONormalTime;
                            break;
                        }
                    }
                    else if (CheckRules(item, true))
                    {
                        if (Inventory.NumFreeSlots >= 1)
                        {
                            LogLootAction(item.Name, "LOOTED", "matches loot list - continued");
                            item.MoveToInventory();
                            itemProcessed = true;
                        }
                        else
                        {
                            // Still no space, mark for reprocessing
                            _corpseNeedsReprocessing[corpseId] = true;
                            _corpseLastInventoryFullTime[corpseId] = Time.AONormalTime;
                            break;
                        }
                    }
                    else if (_settings["Delete"].AsBool())
                    {
                        LogLootAction(item.Name, "DELETED", "not in list - continued");
                        item?.Delete();
                        itemProcessed = true;
                    }

                    if (itemProcessed)
                    {
                        processedAnyItems = true;
                        _corpseLastItemTime[corpseId] = Time.AONormalTime;
                    }
                }

                if (!processedAnyItems && Inventory.NumFreeSlots == 0)
                {
                    // No items processed and inventory still full
                    _corpseNeedsReprocessing[corpseId] = true;
                    _corpseLastInventoryFullTime[corpseId] = Time.AONormalTime;
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error reprocessing corpse items: {ex.Message}");
            }
        }

        private bool ShouldCloseCorpse()
        {
            if (!_settings["Delete"].AsBool() && !_settings["LootAll"].AsBool())
            {
                // Neither Delete nor Loot All - close corpse when all list items are looted
                return true;
            }
            else if (_settings["LootAll"].AsBool())
            {
                // Loot All mode - close after looting everything
                return true;
            }
            else if (_settings["Delete"].AsBool())
            {
                // Delete mode - keep open for now (could be changed later)
                return false;
            }

            return false;
        }

        private void CheckOpenCorpsesForNewItem(string itemName, string minQL, string maxQL, bool exactMatch)
        {
            try
            {
                if (_openCorpses.Count == 0) return;

                // Silently check open corpses for newly added items

                // Clean up invalid corpses first
                _openCorpses.RemoveAll(c => c == null);

                foreach (var corpse in _openCorpses.ToList())
                {
                    foreach (var item in corpse.Items.ToList())
                    {
                        bool matches = false;

                        if (exactMatch)
                        {
                            matches = string.Equals(item.Name, itemName, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            matches = item.Name.ToUpper().Contains(itemName.ToUpper());
                        }

                        if (matches &&
                            item.QualityLevel >= Convert.ToInt32(minQL) &&
                            item.QualityLevel <= Convert.ToInt32(maxQL))
                        {
                            if (Inventory.NumFreeSlots >= 1)
                            {
                                LogLootAction(item.Name, "LOOTED", "matches newly added rule");
                                item.MoveToInventory();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error checking open corpses for new item: {ex.Message}");
            }
        }

        private void CleanupOpenCorpses()
        {
            try
            {
                // Remove corpses that are no longer valid or no longer exist
                var validCorpseIds = DynelManager.Corpses
                    .Where(c => DynelManager.LocalPlayer.Position.DistanceFrom(c.Position) < 20)
                    .Select(c => c.Identity.Instance)
                    .ToHashSet();

                var corpsesToRemove = _openCorpses.Where(c =>
                    c == null || !validCorpseIds.Contains(c.Identity.Instance)).ToList();

                foreach (var corpse in corpsesToRemove)
                {
                    _openCorpses.Remove(corpse);
                }

                // Silently clean up invalid corpses
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error cleaning up open corpses: {ex.Message}");
            }
        }

        private void OnUpdate(object sender, float deltaTime)
        {
            try
            {
                var player = DynelManager.LocalPlayer;

                if (Game.IsZoning) { isBackpackInfoInitialized = false; return; }

                #region UI

                if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
                {
                    if (SettingsController.settingsWindow.FindView("buttonAdd", out Button addbut))
                    {
                        if (addbut.Clicked == null)
                        {
                            addbut.Clicked += AddButtonClicked;
                        }
                    }

                    if (SettingsController.settingsWindow.FindView("buttonDel", out Button rembut))
                    {
                        if (rembut.Clicked == null)
                        {
                            rembut.Clicked += RemButtonClicked;
                        }
                    }

                    if (SettingsController.settingsWindow.FindView("LootManagerInfoView", out Button infoView))
                    {
                        infoView.Tag = SettingsController.settingsWindow;
                        infoView.Clicked = InfoView;
                    }



                    if (SettingsController.settingsWindow.FindView("FolderOpener", out Button folderOpener))
                    {
                        if (folderOpener.Clicked == null)
                        {
                            folderOpener.Clicked += FolderOpenerClicked;
                        }
                    }

                    if (SettingsController.settingsWindow.FindView("SaveLootList", out Button saveLootList))
                    {
                        if (saveLootList.Clicked == null)
                        {
                            saveLootList.Clicked += SaveLootListClicked;
                        }
                    }

                    if (SettingsController.settingsWindow.FindView("LoadLootList", out Button loadLootList))
                    {
                        if (loadLootList.Clicked == null)
                        {
                            loadLootList.Clicked += LoadLootListClicked;
                        }
                    }



                    // Handle mutual exclusivity between "Delete leftovers" and "Loot All"
                    HandleMutualExclusivity();

                    // Handle item name autocomplete
                    HandleItemNameAutocomplete();
                }

                #endregion

                if (_settings["Enabled"].AsBool())
                {
                    InitializeBackpackInfo();
                    ProcessCorpses();
                    MoveItemsToBag();
                }
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
            _infoWindow = Window.CreateFromXml("Info", PluginDirectory + "\\UI\\LootManagerInfoView.xml",
                windowSize: new Rect(0, 0, 440, 510),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            _infoWindow.Show(true);
        }

        private void AddButtonClicked(object sender, ButtonBase e)
        {
            //Chat.WriteLine("AddButtonClicked");
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

            // Get exact matching selection from radio button group
            bool exactMatch = _settings["ExactMatchSelection"].AsInt32() == 1;

            //_multiListView.DeleteAllChildren();

            // Store the values before clearing for dynamic update check
            string addedItemName = _itemName.Text.Trim();
            string addedMinQL = _itemMinQL.Text;
            string addedMaxQL = _itemMaxQL.Text;

            Rules.Add(new Rule(addedItemName, addedMinQL, addedMaxQL, GlobalScope, _itemQuantity.Text, "loot", exactMatch));

            _itemName.Text = "";
            _itemMinQL.Text = "1";
            _itemMaxQL.Text = "500";
            _itemQuantity.Text = "999";
            _settings["ExactMatchSelection"] = 0; // Reset to "Not Exact"
            txErr.Text = "";
            SaveRules();
            RefreshList();

            // Check open corpses for the newly added item
            CheckOpenCorpsesForNewItem(addedItemName, addedMinQL, addedMaxQL, exactMatch);
        }

        private static void RefreshList()
        {
            try
            {
                //Chat.WriteLine("RefreshList()");

                if (SettingsController.settingsWindow == null || !SettingsController.settingsWindow.IsValid) { return; }

                SettingsController.settingsWindow.FindView("ScrollListRoot", out MultiListView _multiListView);

                _multiListView.DeleteAllChildren();

                Rules = Rules.OrderBy(o => o.Name.ToUpper()).ToList();
                int iEntry = 0;

                foreach (Rule r in Rules)
                {
                    var entry = View.CreateFromXml(PluginDir + "\\UI\\ItemEntry.xml");

                    entry.FindChild("ItemName", out TextView _textView);
                    string globalscope = r.Global ? "Global" : "Local";
                    string exactMatch = r.ExactMatch ? "Exact" : "NotExact";
                    _textView.Text = $"{iEntry + 1} - {globalscope} - {exactMatch} - [ {r.Lql.PadLeft(3, ' ')} - {r.Hql.PadLeft(3, ' ')} ] - {r.Name} - {r.Quantity} - {r.BagName}";
                    _multiListView.AddChild(entry, false);
                    iEntry++;
                }

                //Chat.WriteLine($"Refreshed {Rules.Count} rules");
            }
            catch (Exception ex)
            {
                var stackTrace = new StackTrace(ex, true);
                var frame = stackTrace.GetFrame(0);
                var lineNumber = frame.GetFileLineNumber();
                var errorMessage = $"An error occurred on line {lineNumber}: {ex.Message}";
                if (errorMessage != previousErrorMessage)
                {
                    Chat.WriteLine(errorMessage);
                    Chat.WriteLine("Stack Trace: " + ex.StackTrace);
                    previousErrorMessage = errorMessage;
                }
            }
        }

        private void RemButtonClicked(object sender, ButtonBase e)
        {
            //Chat.WriteLine("RemButtonClicked");

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

        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDirectory + "\\UI\\" + xmlName, _settings);
        }

        private void LoadRules()
        {
            try
            {
                Rules = new List<Rule>();

                string filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\LootManager\\Global.json";

                if (File.Exists(filename))
                {
                    string rulesJson = File.ReadAllText(filename);
                    Rules = JsonConvert.DeserializeObject<List<Rule>>(rulesJson);

                    foreach (Rule rule in Rules)
                    {
                        if (string.IsNullOrEmpty(rule.Quantity))
                        {
                            rule.Quantity = "999";

                        }

                        if (string.IsNullOrEmpty(rule.BagName))
                        {
                            rule.BagName = "loot";
                        }

                        rule.Global = true;
                    }
                }

                filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\LootManager\\{DynelManager.LocalPlayer.Name}\\Rules.json";

                if (File.Exists(filename))
                {
                    List<Rule> scopedRules = new List<Rule>();
                    string rulesJson = File.ReadAllText(filename);
                    scopedRules = JsonConvert.DeserializeObject<List<Rule>>(rulesJson);

                    foreach (Rule rule in scopedRules)
                    {
                        rule.Global = false;

                        if (string.IsNullOrEmpty(rule.Quantity))
                        {
                            rule.Quantity = "999";
                        }

                        if (string.IsNullOrEmpty(rule.BagName))
                        {
                            rule.BagName = "loot";
                        }

                        Rules.Add(rule);
                    }

                    //Chat.WriteLine($"Loaded {scopedRules.Count.ToString()}");
                }

                Rules = Rules.OrderBy(o => o.Name.ToUpper()).ToList();
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

        private void SaveRules()
        {
            try
            {
                List<Rule> GlobalRules = new List<Rule>();
                List<Rule> ScopeRules = new List<Rule>();

                string filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\LootManager\\Global.json";

                GlobalRules = Rules.Where(o => o.Global == true).ToList();
                ScopeRules = Rules.Where(o => o.Global == false).ToList();

                string rulesJson = JsonConvert.SerializeObject(GlobalRules);
                File.WriteAllText(filename, rulesJson);

                filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\LootManager\\{DynelManager.LocalPlayer.Name}\\Rules.json";
                rulesJson = JsonConvert.SerializeObject(ScopeRules);
                File.WriteAllText(filename, rulesJson);
                //Chat.WriteLine($"Saved {ScopeRules.Count} Rules");
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

        public bool CheckRules(Item item, bool updateRule = false)
        {
            foreach (Rule rule in Rules)
            {
                if (rule.ExactMatch)
                {
                    if (string.Equals(item.Name, rule.Name, StringComparison.OrdinalIgnoreCase) &&
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
                        Convert.ToInt32(rule.Quantity) >= 1)
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
            try
            {
                //Chat.WriteLine("UpdateRule");

                if (!update || Convert.ToInt32(rule.Quantity) == 999) { return; }

                rule.Quantity = (Convert.ToInt32(rule.Quantity) - 1).ToString();

                //Chat.WriteLine($"Rule {rule.Name} - {rule.Quantity}");

                if (Convert.ToInt32(rule.Quantity) == 0)
                {
                    //Chat.WriteLine($"Removing Rule {rule.Name}");
                    Rules.Remove(rule);
                }

                SaveRules();
                RefreshList();
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

        private void OpenLootBags()
        {
            try
            {
                // Find all backpacks with "loot" in their name (case insensitive)
                var lootBags = Inventory.Backpacks.Where(bag =>
                    bag.Name.ToLower().Contains("loot")).ToList();

                foreach (var bag in lootBags)
                {
                    // Find the corresponding item in inventory to open it
                    var bagItem = Inventory.Items.FirstOrDefault(item =>
                        item.UniqueIdentity.Instance == bag.Identity.Instance);

                    if (bagItem != null)
                    {
                        bagItem.Use(); // Open the bag
                        Chat.WriteLine($"Opened loot bag: {bag.Name}");
                    }
                }

                if (lootBags.Count > 0)
                {
                    Chat.WriteLine($"Found and opened {lootBags.Count} loot bag(s)");

                    // Schedule auto-close after 1 second (changed from 3 seconds)
                    Task.Delay(1000).ContinueWith(_ => CloseLootBags());
                }
                else
                {
                    Chat.WriteLine("No loot bags found in inventory");
                    // Debug: Show what bags are available
                    var allBags = Inventory.Backpacks.ToList();
                    if (allBags.Count > 0)
                    {
                        Chat.WriteLine($"Available bags: {string.Join(", ", allBags.Select(b => b.Name))}");
                    }
                    else
                    {
                        Chat.WriteLine("No backpacks found at all");
                    }
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error opening loot bags: {ex.Message}");
            }
        }

        private void CloseLootBags()
        {
            try
            {
                // Find all backpacks with "loot" in their name (case insensitive)
                var lootBags = Inventory.Backpacks.Where(bag =>
                    bag.Name.ToLower().Contains("loot")).ToList();

                foreach (var bag in lootBags)
                {
                    // Find the corresponding item in inventory to close it
                    var bagItem = Inventory.Items.FirstOrDefault(item =>
                        item.UniqueIdentity.Instance == bag.Identity.Instance);

                    if (bagItem != null)
                    {
                        bagItem.Use(); // Close the bag (Use toggles open/close)
                        Chat.WriteLine($"Closed loot bag: {bag.Name}");
                    }
                }

                if (lootBags.Count > 0)
                {
                    Chat.WriteLine($"Auto-closed {lootBags.Count} loot bag(s)");
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error closing loot bags: {ex.Message}");
            }
        }

        private void OpenLootManagerWindow()
        {
            try
            {
                Chat.WriteLine("Opening LootManager settings window...");

                // Replicate the logic from the /lootmanager command in SettingsController
                Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\LootManager\\{DynelManager.LocalPlayer.Name}\\Config.json");

                SettingsController.settingsWindow = Window.Create(new Rect(50, 50, 600, 400), "Loot Manager", "Settings", WindowStyle.Default, WindowFlags.AutoScale);

                if (SettingsController.settingsWindow != null && !SettingsController.settingsWindow.IsVisible)
                {
                    SettingsController.AppendSettingsTab("Loot Manager", SettingsController.settingsWindow);

                    if (SettingsController.settingsWindow.FindView("ScrollListRoot", out MultiListView _multiListView) &&
                        SettingsController.settingsWindow.FindView("_itemMinQL", out TextInputView _itemMinQL) &&
                        SettingsController.settingsWindow.FindView("_itemMaxQL", out TextInputView _itemMaxQL) &&
                        SettingsController.settingsWindow.FindView("_itemQuantity", out TextInputView _itemQuantity))
                    {
                        _itemMinQL.Text = "1";
                        _itemMaxQL.Text = "500";
                        _itemQuantity.Text = "999";

                        _multiListView.DeleteAllChildren();
                        int iEntry = 0;
                        foreach (Rule r in Rules)
                        {
                            View entry = View.CreateFromXml(PluginDir + "\\UI\\ItemEntry.xml");
                            entry.FindChild("ItemName", out TextView tx);

                            string scope = r.Global ? "Global" : "Local";
                            string exactMatch = r.ExactMatch ? "Exact" : "NotExact";
                            tx.Text = $"{(iEntry + 1)} - {scope} - {exactMatch} - [ {r.Lql.PadLeft(3, ' ')} - {r.Hql.PadLeft(3, ' ')} ] - {r.Name} - {r.Quantity} - {r.BagName}";

                            _multiListView.AddChild(entry, false);
                            iEntry++;
                        }
                    }

                    Chat.WriteLine("LootManager settings window opened");
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error opening LootManager window: {ex.Message}");
            }
        }



        private void FolderOpenerClicked(object sender, ButtonBase e)
        {
            try
            {
                // Get the LootManager directory path
                string lootManagerDir = GetLootManagerDirectory();

                // Ensure directory exists
                Directory.CreateDirectory(lootManagerDir);

                // Open the folder in Windows Explorer
                System.Diagnostics.Process.Start("explorer.exe", lootManagerDir);

                Chat.WriteLine($"Opened LootManager folder: {lootManagerDir}");
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error opening LootManager folder: {ex.Message}");
            }
        }

        private void SaveLootListClicked(object sender, ButtonBase e)
        {
            try
            {
                Chat.WriteLine("=== Save Loot List ===");
                Chat.WriteLine("Use the command: /lmsave <name>");
                Chat.WriteLine("Examples:");
                Chat.WriteLine("  /lmsave Monster Parts");
                Chat.WriteLine("  /lmsave Daily Farming");
                Chat.WriteLine("  /lmsave Daily Farming");
                Chat.WriteLine("======================");
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error showing save instructions: {ex.Message}");
            }
        }

        private static List<string> _availableExportFiles = new List<string>();

        private void LoadLootListClicked(object sender, ButtonBase e)
        {
            try
            {
                string lootManagerDir = GetLootManagerDirectory();
                string savedListsDir = Path.Combine(lootManagerDir, "SavedLootLists");

                // Find saved configurations
                var savedFiles = new List<string>();
                if (Directory.Exists(savedListsDir))
                {
                    savedFiles = Directory.GetFiles(savedListsDir, "*.json")
                        .OrderBy(f => File.GetCreationTime(f))
                        .ToList();
                }

                // Find old export files for backward compatibility
                var exportFiles = Directory.GetFiles(lootManagerDir, "LootList_Export_*.json")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();

                if (savedFiles.Count == 0 && exportFiles.Count == 0)
                {
                    Chat.WriteLine("No saved loot configurations found.");
                    Chat.WriteLine("Use '/lmsave <name>' to save your current loot list.");
                    return;
                }

                // Store files for selection and display list
                _availableExportFiles = exportFiles;

                Chat.WriteLine("=== Available Loot Configurations ===");

                if (savedFiles.Count > 0)
                {
                    Chat.WriteLine("Saved Configurations (use /lmload <name>):");
                    foreach (var file in savedFiles)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        string configName = fileName.Replace("_", " ");
                        DateTime fileDate = File.GetCreationTime(file);

                        try
                        {
                            string json = File.ReadAllText(file);
                            dynamic data = JsonConvert.DeserializeObject(json);
                            string displayName = data.ConfigName?.ToString() ?? configName;
                            int ruleCount = data.Rules?.Count ?? 0;
                            Chat.WriteLine($"  • {displayName} ({ruleCount} rules, saved {fileDate:yyyy-MM-dd HH:mm})");
                        }
                        catch
                        {
                            Chat.WriteLine($"  • {configName} (saved {fileDate:yyyy-MM-dd HH:mm})");
                        }
                    }
                }

                if (exportFiles.Count > 0)
                {
                    Chat.WriteLine("Old Export Files (use /lmload <number>):");
                    for (int i = 0; i < exportFiles.Count; i++)
                    {
                        string fileName = Path.GetFileName(exportFiles[i]);
                        DateTime fileDate = File.GetCreationTime(exportFiles[i]);
                        Chat.WriteLine($"  {i + 1}. {fileName} (Created: {fileDate:yyyy-MM-dd HH:mm})");
                    }
                }

                Chat.WriteLine("==========================================");
                Chat.WriteLine("Commands:");
                Chat.WriteLine("  /lmload <name> - Load saved configuration by name");
                Chat.WriteLine("  /lmload <number> - Load old export file by number");
                Chat.WriteLine("  /lmload latest - Load most recent export file");
                Chat.WriteLine("  /lmlist - Show all available configurations");
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error listing export files: {ex.Message}");
            }
        }

        private void LoadSelectedExportFile(int fileIndex)
        {
            try
            {
                if (_availableExportFiles.Count == 0)
                {
                    Chat.WriteLine("No export files available. Click 'Load List' first to see available files.");
                    return;
                }

                if (fileIndex < 0 || fileIndex >= _availableExportFiles.Count)
                {
                    Chat.WriteLine($"Invalid file number. Please choose between 1 and {_availableExportFiles.Count}.");
                    return;
                }

                string selectedFile = _availableExportFiles[fileIndex];
                string fileName = Path.GetFileName(selectedFile);

                // Read and parse the selected export file
                string json = File.ReadAllText(selectedFile);
                dynamic importData = JsonConvert.DeserializeObject(json);

                // Clear current rules and load imported ones
                Rules.Clear();

                foreach (var rule in importData.Rules)
                {
                    Rules.Add(new Rule(
                        rule.Name.ToString(),
                        rule.MinQL.ToString(),
                        rule.MaxQL.ToString(),
                        (bool)rule.Global,
                        rule.Quantity.ToString(),
                        rule.BagName.ToString(),
                        (bool)rule.ExactMatch
                    ));
                }

                // Save the imported rules and refresh UI
                SaveRules();
                RefreshList();

                Chat.WriteLine($"Loot list imported successfully!");
                Chat.WriteLine($"Loaded {Rules.Count} rules from: {fileName}");
                Chat.WriteLine($"Original export date: {importData.ExportDate}");
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error importing loot list: {ex.Message}");
            }
        }

        private void SaveLootListWithName(string configName)
        {
            try
            {
                string lootManagerDir = GetLootManagerDirectory();
                string savedListsDir = Path.Combine(lootManagerDir, "SavedLootLists");
                Directory.CreateDirectory(savedListsDir);

                // Sanitize filename
                string safeFileName = string.Join("_", configName.Split(Path.GetInvalidFileNameChars()));
                string filename = Path.Combine(savedListsDir, $"{safeFileName}.json");

                // Create export data with current rules
                var exportData = new
                {
                    ConfigName = configName,
                    ExportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    CharacterName = DynelManager.LocalPlayer.Name,
                    TotalRules = Rules.Count,
                    Rules = Rules.Select(r => new
                    {
                        Name = r.Name,
                        MinQL = r.Lql,
                        MaxQL = r.Hql,
                        Quantity = r.Quantity,
                        BagName = r.BagName,
                        Global = r.Global,
                        ExactMatch = r.ExactMatch
                    }).ToList()
                };

                // Save to file
                string json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
                File.WriteAllText(filename, json);

                Chat.WriteLine($"Loot list '{configName}' saved successfully!");
                Chat.WriteLine($"Saved {Rules.Count} rules to: SavedLootLists/{safeFileName}.json");
                Chat.WriteLine($"Use '/lmload {configName}' to load this configuration.");
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error saving loot list '{configName}': {ex.Message}");
            }
        }

        private void LoadLootListByName(string configName)
        {
            try
            {
                string lootManagerDir = GetLootManagerDirectory();
                string savedListsDir = Path.Combine(lootManagerDir, "SavedLootLists");

                // Try to find file by name
                string safeFileName = string.Join("_", configName.Split(Path.GetInvalidFileNameChars()));
                string filename = Path.Combine(savedListsDir, $"{safeFileName}.json");

                if (!File.Exists(filename))
                {
                    Chat.WriteLine($"Configuration '{configName}' not found.");
                    Chat.WriteLine("Use /lmlist to see available configurations.");
                    return;
                }

                // Read and parse the export file
                string json = File.ReadAllText(filename);
                dynamic importData = JsonConvert.DeserializeObject(json);

                // Clear current rules and load imported ones
                Rules.Clear();

                foreach (var rule in importData.Rules)
                {
                    Rules.Add(new Rule(
                        rule.Name.ToString(),
                        rule.MinQL.ToString(),
                        rule.MaxQL.ToString(),
                        (bool)rule.Global,
                        rule.Quantity.ToString(),
                        rule.BagName.ToString(),
                        (bool)rule.ExactMatch
                    ));
                }

                // Save the imported rules and refresh UI
                SaveRules();
                RefreshList();

                Chat.WriteLine($"Loot list '{configName}' loaded successfully!");
                Chat.WriteLine($"Loaded {Rules.Count} rules from configuration.");
                if (importData.ExportDate != null)
                {
                    Chat.WriteLine($"Original save date: {importData.ExportDate}");
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error loading loot list '{configName}': {ex.Message}");
            }
        }

        private void ListAvailableLootLists()
        {
            try
            {
                string lootManagerDir = GetLootManagerDirectory();
                string savedListsDir = Path.Combine(lootManagerDir, "SavedLootLists");

                // Find all saved configuration files in the dedicated folder
                var savedFiles = new List<string>();
                if (Directory.Exists(savedListsDir))
                {
                    savedFiles = Directory.GetFiles(savedListsDir, "*.json")
                        .OrderBy(f => File.GetCreationTime(f))
                        .ToList();
                }

                // Find old timestamped export files in main directory (for backward compatibility)
                var exportFiles = Directory.GetFiles(lootManagerDir, "LootList_Export_*.json")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();

                Chat.WriteLine("=== Available Loot Configurations ===");

                if (savedFiles.Count > 0)
                {
                    Chat.WriteLine("Saved Configurations:");
                    foreach (var file in savedFiles)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        string configName = fileName.Replace("_", " ");
                        DateTime fileDate = File.GetCreationTime(file);

                        try
                        {
                            string json = File.ReadAllText(file);
                            dynamic data = JsonConvert.DeserializeObject(json);
                            int ruleCount = data.Rules?.Count ?? 0;
                            string displayName = data.ConfigName?.ToString() ?? configName;
                            Chat.WriteLine($"  • {displayName} ({ruleCount} rules, saved {fileDate:yyyy-MM-dd HH:mm})");
                        }
                        catch
                        {
                            Chat.WriteLine($"  • {configName} (saved {fileDate:yyyy-MM-dd HH:mm})");
                        }
                    }
                }

                if (exportFiles.Count > 0)
                {
                    Chat.WriteLine("Timestamped Exports:");
                    for (int i = 0; i < Math.Min(exportFiles.Count, 5); i++) // Show max 5 recent exports
                    {
                        string fileName = Path.GetFileName(exportFiles[i]);
                        DateTime fileDate = File.GetCreationTime(exportFiles[i]);
                        Chat.WriteLine($"  {i + 1}. {fileName} (Created: {fileDate:yyyy-MM-dd HH:mm})");
                    }
                    if (exportFiles.Count > 5)
                    {
                        Chat.WriteLine($"  ... and {exportFiles.Count - 5} more export files");
                    }
                }

                if (savedFiles.Count == 0 && exportFiles.Count == 0)
                {
                    Chat.WriteLine("No saved configurations found.");
                    Chat.WriteLine("Use '/lmsave <name>' to save your current loot list.");
                }

                Chat.WriteLine("=====================================");
                Chat.WriteLine("Commands:");
                Chat.WriteLine("  /lmsave <name> - Save current list with custom name");
                Chat.WriteLine("  /lmload <name> - Load named configuration");
                Chat.WriteLine("  /lmload <number> - Load timestamped export by number");
                Chat.WriteLine("  /lmload latest - Load most recent export");
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error listing loot configurations: {ex.Message}");
            }
        }

        private static bool _lastDeleteState = false;
        private static bool _lastLootAllState = false;
        private static bool _lastLeaveOpenState = false;
        private static bool _lastDisableState = false;

        private void HandleMutualExclusivity()
        {
            try
            {
                // Make "Delete leftovers" and "Loot All" mutually exclusive
                // Allow three states: Delete only, Loot All only, or Neither
                bool deleteEnabled = _settings["Delete"].AsBool();
                bool lootAllEnabled = _settings["LootAll"].AsBool();
                bool leaveOpenEnabled = _settings["LeaveOpen"].AsBool();
                bool disableEnabled = _settings["Disable"].AsBool();

                // Check for setting changes to trigger re-processing
                bool deleteChanged = deleteEnabled != _lastDeleteState;
                bool lootAllChanged = lootAllEnabled != _lastLootAllState;
                bool leaveOpenChanged = leaveOpenEnabled != _lastLeaveOpenState;
                bool disableChanged = disableEnabled != _lastDisableState;

                // Handle mutual exclusivity between Delete and Loot All
                if (deleteEnabled && lootAllEnabled)
                {
                    if (deleteChanged && !lootAllChanged)
                    {
                        // Delete was just enabled, disable Loot All
                        _settings["LootAll"] = false;
                        lootAllEnabled = false;
                    }
                    else if (lootAllChanged && !deleteChanged)
                    {
                        // Loot All was just enabled, disable Delete
                        _settings["Delete"] = false;
                        deleteEnabled = false;
                    }
                    else
                    {
                        // Both changed or unclear - default to keeping Loot All
                        _settings["Delete"] = false;
                        deleteEnabled = false;
                    }
                }

                // If any setting changed, re-process open corpses AND allow re-opening of nearby corpses
                if ((deleteChanged || lootAllChanged || leaveOpenChanged || disableChanged) &&
                    _settings["Enabled"].AsBool())
                {
                    if (_openCorpses.Count > 0)
                    {
                        ReprocessOpenCorpses();
                    }

                    // Clear fully processed corpses list to allow re-opening nearby corpses with new settings
                    var nearbyCorpses = DynelManager.Corpses.Where(c =>
                        DynelManager.LocalPlayer.Position.DistanceFrom(c.Position) < 5).ToList();

                    if (nearbyCorpses.Count > 0)
                    {
                        _fullyProcessedCorpses.Clear();
                        _corpseOpenedTime.Clear(); // Also clear opened times to allow immediate reopening
                    }
                }

                // Update last known states
                _lastDeleteState = deleteEnabled;
                _lastLootAllState = lootAllEnabled;
                _lastLeaveOpenState = leaveOpenEnabled;
                _lastDisableState = disableEnabled;
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error handling mutual exclusivity: {ex.Message}");
            }
        }

        private void ReprocessOpenCorpses()
        {
            try
            {
                // Clean up invalid corpses first
                _openCorpses.RemoveAll(c => c == null);

                foreach (var corpse in _openCorpses.ToList())
                {
                    // Silently re-process corpses with new settings

                    // Only process if we have inventory space OR if we're deleting items
                    if (Inventory.NumFreeSlots >= 1 || _settings["Delete"].AsBool())
                    {
                        foreach (var item in corpse.Items.ToList())
                        {
                            // Check if "Loot All" is enabled
                            if (_settings["LootAll"].AsBool())
                            {
                                if (Inventory.NumFreeSlots >= 1)
                                {
                                    WriteToSelectedChatWindow($"{item.Name} looted (Loot All re-enabled)");
                                    LogLootAction(item.Name, "LOOTED", "Loot All re-enabled");

                                    // Track this item by name and timestamp for Loot All
                                    _lootAllItems[item.Name] = Time.AONormalTime;

                                    item.MoveToInventory();
                                }
                            }
                            else if (CheckRules(item, true))
                            {
                                if (Inventory.NumFreeSlots >= 1)
                                {
                                    WriteToSelectedChatWindow($"{item.Name} matches loot list (re-processing)");
                                    LogLootAction(item.Name, "LOOTED", "matches loot list - re-processing");
                                    item.MoveToInventory();
                                }
                            }
                            else if (_settings["Delete"].AsBool())
                            {
                                WriteToSelectedChatWindow($"{item.Name} delete not in list (re-processing)");
                                LogLootAction(item.Name, "DELETED", "not in list - re-processing");
                                item?.Delete();
                            }
                        }
                    }

                    // Apply new corpse closing logic
                    CloseCorpseAfterProcessing(corpse);
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error re-processing open corpses: {ex.Message}");
            }
        }

        private void WriteToSelectedChatWindow(string message)
        {
            try
            {
                // For now, just use the standard Chat.WriteLine
                // This can be enhanced in the future to support specific chat windows
                Chat.WriteLine(message);
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error writing to chat window: {ex.Message}");
            }
        }

        private static string GetLootManagerDirectory()
        {
            return $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\LootManager\\{DynelManager.LocalPlayer.Name}";
        }



        private void LoadItemDatabase()
        {
            try
            {
                // Try the new simplified format first
                string lootManagerDir = GetLootManagerDirectory();
                string itemNamesPath = Path.Combine(lootManagerDir, "ItemNames.txt");
                string itemDatabasePath = Path.Combine(lootManagerDir, "Item_Database_Complete_List.txt");

                // Ensure the directory exists
                Directory.CreateDirectory(lootManagerDir);

                if (File.Exists(itemNamesPath))
                {
                    // Load from the simplified format (one item name per line)
                    ItemDatabase = File.ReadAllLines(itemNamesPath)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrEmpty(line))
                        .Distinct()
                        .OrderBy(name => name)
                        .ToList();

                    Chat.WriteLine($"Loaded {ItemDatabase.Count} items from ItemNames.txt");
                    Chat.WriteLine("Item database autocomplete is now active");
                }
                else
                {
                    // Create the ItemNames.txt file with a basic set of items
                    CreateDefaultItemDatabase(itemNamesPath);

                    // Try loading again
                    if (File.Exists(itemNamesPath))
                    {
                        ItemDatabase = File.ReadAllLines(itemNamesPath)
                            .Where(line => !string.IsNullOrWhiteSpace(line))
                            .Select(line => line.Trim())
                            .Where(line => !string.IsNullOrEmpty(line))
                            .Distinct()
                            .OrderBy(name => name)
                            .ToList();

                        Chat.WriteLine($"Created and loaded {ItemDatabase.Count} items from ItemNames.txt");
                        Chat.WriteLine("Item database autocomplete is now active");
                    }
                }

                if (File.Exists(itemDatabasePath) && (ItemDatabase == null || ItemDatabase.Count == 0))
                {
                    // Fallback to parsing the complex format
                    var allLines = File.ReadAllLines(itemDatabasePath);
                    ItemDatabase = new List<string>();

                    foreach (string line in allLines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // Look for lines that start with a number followed by a period (item entries)
                        // Format: "   123. Item Name Here"
                        var trimmedLine = line.Trim();
                        if (System.Text.RegularExpressions.Regex.IsMatch(trimmedLine, @"^\d+\.\s+"))
                        {
                            // Extract the item name (everything after the number and period)
                            var match = System.Text.RegularExpressions.Regex.Match(trimmedLine, @"^\d+\.\s+(.+)$");
                            if (match.Success)
                            {
                                string itemName = match.Groups[1].Value.Trim();

                                // Remove any quotes around the item name
                                if (itemName.StartsWith("\"") && itemName.Contains("\""))
                                {
                                    int endQuote = itemName.IndexOf("\"", 1);
                                    if (endQuote > 0)
                                    {
                                        itemName = itemName.Substring(1, endQuote - 1);
                                    }
                                }

                                if (!string.IsNullOrEmpty(itemName) && !ItemDatabase.Contains(itemName))
                                {
                                    ItemDatabase.Add(itemName);
                                }
                            }
                        }
                    }

                    // Sort the items alphabetically for better autocomplete experience
                    ItemDatabase.Sort();

                    Chat.WriteLine($"Loaded {ItemDatabase.Count} items from Item_Database_Complete_List.txt");
                    Chat.WriteLine("Item database autocomplete is now active");
                }

                if (ItemDatabase == null || ItemDatabase.Count == 0)
                {
                    Chat.WriteLine("Item database could not be loaded - autocomplete will not be available");
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error loading item database: {ex.Message}");
                Chat.WriteLine("Autocomplete functionality will not be available");
            }
        }

        private void CreateDefaultItemDatabase(string filePath)
        {
            try
            {
                Chat.WriteLine("Creating default ItemNames.txt file...");

                // Create a comprehensive list of common AO items
                var defaultItems = new List<string>
                {
                    // Monster-related items
                    "Monster Parts",
                    "Monster Parts with Ivory",
                    "Pelted Monster Parts",
                    "Pelted Monster Parts with Ivory",
                    "Monster Egg Containment Field",
                    "Monster Sunglasses",

                    // Basic materials
                    "Basic Armor",
                    "Basic Boots",
                    "Basic Gloves",
                    "Basic Helmet",
                    "Basic Pants",
                    "Basic Sleeves",
                    "Basic Weapon",
                    "Biomaterial",
                    "Bioplastic",
                    "Carbon Fiber",
                    "Ceramic Armor Plate",
                    "Cloth",
                    "Concrete Cushion",
                    "Crystalline Carbon",
                    "Elastomer",
                    "Exoskeleton",
                    "Fiberplastic",
                    "Flexsteel",
                    "Hardened Aluminum",
                    "Iron",
                    "Kevlar",
                    "Liquid Metal",
                    "Metaplast",
                    "Nano Crystal",
                    "Nano Formula",
                    "Nano Programming Interface",
                    "Notum",
                    "Organic Goo",
                    "Plasteel",
                    "Polymer",
                    "Quantum Processor",
                    "Reinforced Kevlar",
                    "Rubber",
                    "Silicon",
                    "Steel",
                    "Superconductor",
                    "Titanium",
                    "Tungsten",
                    "Viral Serum",

                    // Common weapons and upgrades
                    "Weapon Upgrade",
                    "Weapon Upgrade Device",
                    "Weapon Upgrade Kit",
                    "Weapon Upgrade Module",
                    "Weapon Upgrade Package",
                    "Weapon Upgrade Platform",
                    "Weapon Upgrade System",
                    "Weapon Upgrade Tool",
                    "Weapon Upgrade Unit",
                    "Weapon Upgrade Utility",

                    // Memory and NCU items
                    "100 NCU Memory",
                    "25 NCU Memory",
                    "35 NCU Memory",
                    "45 NCU Memory",
                    "55 NCU Memory",
                    "64 NCU Memory",
                    "8 NCU Memory",
                    "3 NCU Memory",

                    // Common quest items
                    "A bag",
                    "A Beer Jug",
                    "A cup.",
                    "A Jar",
                    "A nice Glass",
                    "A Very Nice Glass",
                    "A Silver Platter",

                    // Clusters and components
                    "1h Blunt Weapon Base",
                    "2H Blunt Weapon Base",
                    "2H Edged Weapon Base",
                    "Curved Blade",
                    "Curved Handle",
                    "Curved Hilt",
                    "Curved Shaft",
                    "Curved Stock",
                    "Curved Trigger",
                    "Curved Tube"
                };

                // Sort alphabetically
                defaultItems.Sort();

                // Write to file
                File.WriteAllLines(filePath, defaultItems);

                Chat.WriteLine($"Created ItemNames.txt with {defaultItems.Count} default items");
                Chat.WriteLine("You can add more items to this file manually or it will be updated from the full database if available");
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error creating default item database: {ex.Message}");
            }
        }

        private void HandleItemNameAutocomplete()
        {
            try
            {
                // Check if database is loaded
                if (ItemDatabase == null || ItemDatabase.Count == 0)
                {
                    // Try to load the database if it's not loaded yet
                    if (ItemDatabase == null)
                    {
                        ItemDatabase = new List<string>();
                        LoadItemDatabase();
                    }
                    return; // Exit if still no database
                }

                if (SettingsController.settingsWindow.FindView("tivName", out TextInputView itemNameInput))
                {
                    CurrentItemNameInput = itemNameInput;

                    // Check if text has changed
                    string currentText = itemNameInput.Text ?? "";

                    if (currentText != LastSearchText)
                    {
                        LastSearchText = currentText;

                        if (string.IsNullOrWhiteSpace(currentText))
                        {
                            FilteredItems.Clear();
                            // Clear all suggestions display
                            if (SettingsController.settingsWindow.FindView("tvErr", out TextView errorText))
                            {
                                errorText.Text = "";
                            }
                            ClearSuggestionLines();
                        }
                        else
                        {
                            // Filter items that contain the search text (case insensitive)
                            FilteredItems = ItemDatabase
                                .Where(item => item.IndexOf(currentText, StringComparison.OrdinalIgnoreCase) >= 0)
                                .OrderBy(item => item.IndexOf(currentText, StringComparison.OrdinalIgnoreCase)) // Prioritize matches at the beginning
                                .ThenBy(item => item.Length) // Then by length
                                .Take(100) // Show more suggestions
                                .ToList();

                            // Show suggestions in the error text field
                            ShowSuggestions(currentText);
                        }
                    }

                    // Auto-complete with the first match if there's only one result
                    if (FilteredItems.Count == 1 && currentText.Length >= 3)
                    {
                        string suggestion = FilteredItems[0];
                        if (suggestion.StartsWith(currentText, StringComparison.OrdinalIgnoreCase))
                        {
                            // Auto-complete the text
                            itemNameInput.Text = suggestion;
                            LastSearchText = suggestion;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error in autocomplete: {ex.Message}");
            }
        }

        private void ShowSuggestions(string searchText)
        {
            try
            {
                // Clear all suggestion lines first
                ClearSuggestionLines();

                if (SettingsController.settingsWindow.FindView("tvErr", out TextView errorText))
                {
                    if (FilteredItems.Count == 0)
                    {
                        errorText.Text = $"No items found matching '{searchText}'";
                        return;
                    }
                    else if (FilteredItems.Count == 1)
                    {
                        errorText.Text = $"Found: {FilteredItems[0]}";
                        return;
                    }
                    else
                    {
                        errorText.Text = $"Found {FilteredItems.Count} items:";
                    }
                }

                // Display items in separate lines (4 items per line)
                const int itemsPerLine = 4;
                const int maxLines = 4; // Show up to 4 lines (16 items)

                for (int lineIndex = 0; lineIndex < maxLines; lineIndex++)
                {
                    string lineViewName = $"tvSuggestionLine{lineIndex + 1}";

                    if (SettingsController.settingsWindow.FindView(lineViewName, out TextView lineText))
                    {
                        var lineItems = new List<string>();
                        int startIndex = lineIndex * itemsPerLine;

                        // Get up to 4 items for this line
                        for (int i = 0; i < itemsPerLine && (startIndex + i) < FilteredItems.Count; i++)
                        {
                            int itemIndex = startIndex + i;
                            string itemName = FilteredItems[itemIndex];

                            // No truncation - keep full item names
                            lineItems.Add($"{itemIndex + 1}.{itemName}");
                        }

                        // Set the text for this line with 5 spaces between entries
                        lineText.Text = string.Join("     ", lineItems);
                    }
                }

                // Show "more items" indicator if needed
                const int maxItemsToShow = itemsPerLine * maxLines;
                if (FilteredItems.Count > maxItemsToShow)
                {
                    if (SettingsController.settingsWindow.FindView("tvSuggestionMore", out TextView moreText))
                    {
                        moreText.Text = $"+{FilteredItems.Count - maxItemsToShow} more items...";
                    }
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error showing suggestions: {ex.Message}");
            }
        }

        private void ClearSuggestionLines()
        {
            try
            {
                // Clear all suggestion line text views
                for (int i = 1; i <= 4; i++)
                {
                    string lineViewName = $"tvSuggestionLine{i}";
                    if (SettingsController.settingsWindow.FindView(lineViewName, out TextView lineText))
                    {
                        lineText.Text = "";
                    }
                }

                // Clear the "more items" text
                if (SettingsController.settingsWindow.FindView("tvSuggestionMore", out TextView moreText))
                {
                    moreText.Text = "";
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error clearing suggestion lines: {ex.Message}");
            }
        }

        private void LogLootAction(string itemName, string action, string reason = "")
        {
            try
            {
                // Use the same directory as LootManager settings
                string lootManagerDir = GetLootManagerDirectory();

                // Ensure directory exists
                Directory.CreateDirectory(lootManagerDir);

                string logFilePath = Path.Combine(lootManagerDir, "LootManager_Log.txt");

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string characterName = DynelManager.LocalPlayer.Name;
                string logEntry = $"[{timestamp}] [{characterName}] {itemName} - {action}";

                if (!string.IsNullOrEmpty(reason))
                {
                    logEntry += $" ({reason})";
                }

                logEntry += Environment.NewLine;

                // Append to log file (creates if doesn't exist)
                File.AppendAllText(logFilePath, logEntry);
            }
            catch (Exception ex)
            {
                // Don't spam chat with logging errors, just silently fail
                var errorMessage = "Logging error on line " + GetLineNumber(ex) + ": " + ex.Message;
                if (errorMessage != previousErrorMessage)
                {
                    Chat.WriteLine($"LootManager logging error: {ex.Message}");
                    previousErrorMessage = errorMessage;
                }
            }
        }

        public static int GetLineNumber(Exception ex)
        {
            var lineNumber = 0;

            var lineMatch = Regex.Match(ex.StackTrace ?? "", @":line (\d+)$", RegexOptions.Multiline);

            if (lineMatch.Success)
            {
                lineNumber = int.Parse(lineMatch.Groups[1].Value);
            }


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
