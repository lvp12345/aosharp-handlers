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

        List<int> ourMobs = new List<int>();//mob instance
        Dictionary<int, int> ourCorpses = new Dictionary<int, int>(); // corpse instance and mob instance
        List<int> openedCorpses = new List<int>();//corpse instance
        //int corpseInstance;
        public static string previousErrorMessage = string.Empty;
        bool isBackpackInfoInitialized = false;
        double openDelay;
        double moveDelay;

        [Obsolete]
        public override void Run(string pluginDir)
        {
            try
            {
                _settings = new Settings("LootManager");

                PluginDir = pluginDir;

                Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\LootManager\\{DynelManager.LocalPlayer.Name}\\Config.json");

                Game.OnUpdate += OnUpdate;
                Network.N3MessageReceived += N3MessageReceived;
                Inventory.ContainerOpened += ContainerOpened;

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

                Chat.RegisterCommand("printlm", (string command, string[] param, ChatWindow chatWindow) =>
                {

                    if (ourMobs.Count > 1)
                    {
                        foreach (var mob in ourMobs)
                        {
                            Chat.WriteLine($"{mob}");
                        }
                    }
                    else { Chat.WriteLine("ourMobs.Count = 0"); }

                    if (ourCorpses.Count > 1)
                    {
                        foreach (var corpse in ourCorpses)
                        {
                            Chat.WriteLine($"{corpse.Key}, {corpse.Value}");
                        }
                    }
                    else { Chat.WriteLine("ourCorpses.Count = 0"); }

                    if (openedCorpses.Count > 1)
                    {
                        foreach (var corpse in openedCorpses)
                        {
                            Chat.WriteLine($"{corpse}");
                        }
                    }
                    else { Chat.WriteLine("openedCorpses.Count = 0"); }
                });

                if (!Game.IsNewEngine)
                {
                    Chat.WriteLine("Loot Manager loaded!");
                    Chat.WriteLine("/lootmanager for settings. /lm to enable/disable");

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

        private void N3MessageReceived(object sender, N3Message e)
        {
            switch (e.N3MessageType)
            {
                case N3MessageType.CorpseFullUpdate:
                    var corpsemsg = (CorpseFullUpdateMessage)e;
                    //var containerDyel = DynelManager.AllDynels.FirstOrDefault(x => x.Identity.Instance == corpsemsg.Identity.Instance);
                    //Chat.WriteLine($"{containerDyel?.Name}, corpse instance = {corpsemsg.Identity.Instance}, mob instance = {corpsemsg.UnknownIdentity.Instance}");
                    if (!ourMobs.Contains(corpsemsg.UnknownIdentity.Instance)) { return; }
                    ourCorpses.Add(corpsemsg.Identity.Instance, corpsemsg.UnknownIdentity.Instance);
                    break;
                case N3MessageType.Despawn:
                    var despawnMsg = (DespawnMessage)e;
                    if (!ourCorpses.ContainsKey(despawnMsg.Identity.Instance)) { return; }
                    var mobToRemove = ourCorpses[despawnMsg.Identity.Instance];
                    ourMobs.Remove(mobToRemove);
                    ourCorpses.Remove(despawnMsg.Identity.Instance);
                    openedCorpses.Remove(despawnMsg.Identity.Instance);
                    break;
                case N3MessageType.ContainerAddItem:
                    var addItemMsg = (ContainerAddItem)e;
                    if (addItemMsg.Identity != DynelManager.LocalPlayer.Identity) { return; }

                    switch (addItemMsg.Target.Type)
                    {
                        case IdentityType.Inventory:
                            break;
                        case IdentityType.Container:
                            break;
                    }
                    break;
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

                        if (!ourCorpses.ContainsKey(container.Identity.Instance)) { return; }

                        openedCorpses.Add(container.Identity.Instance);

                        foreach (var item in container.Items)
                        {
                            if (Inventory.NumFreeSlots <= 1) { return; }

                            //Chat.WriteLine(item.Name);

                            if (CheckRules(item, true))
                            {
                                item.MoveToInventory();
                            }
                            else
                            {
                                if (_settings["Delete"].AsBool())
                                {
                                    item?.Delete();
                                }
                            }
                        }

                        if (!_settings["Delete"].AsBool())
                        {
                            if (container.Items.Any(item => !CheckRules(item)))
                            {
                                var corpse = DynelManager.Corpses.FirstOrDefault(c => c.Identity.Instance == container.Identity.Instance);

                                if (openedCorpses.Contains(container.Identity.Instance))
                                {
                                    if (ourCorpses.ContainsKey(container.Identity.Instance))
                                    {
                                        var mobToRemove = ourCorpses[container.Identity.Instance]; //get value from ourcorpse key
                                        ourMobs.Remove(mobToRemove);
                                        ourCorpses.Remove(container.Identity.Instance);
                                        openedCorpses.Remove(container.Identity.Instance);
                                        openDelay = 0;
                                        //Chat.WriteLine($"Closing {corpse.Name}");
                                        corpse.Open();//close corpse
                                    }
                                }
                            }
                        }
                        break;
                        //case IdentityType.Container:
                        //    //Chat.WriteLine($"{container.Identity.Type} opened");
                        //    var backpack = Inventory.Backpacks.FirstOrDefault(c => c.Identity.Instance == container.Identity.Instance);
                        //    if (backpack == null || !backpack.Name.Contains("loot")) { return; }

                        //    //if (!backpackDictionary.ContainsKey(container.Identity.Instance))
                        //    //{
                        //    //    backpackDictionary[container.Identity.Instance] = backpack;
                        //    //}

                        //    //Chat.WriteLine($"{backpack.Name} opened, instance = {container.Identity.Instance}");
                        //    break;
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
            Config.Save();
            SaveRules();
            SettingsController.CleanUp();
        }

        private void MoveItemsToBag()
        {

            var backpack = Inventory.Backpacks.Where(a => a.Name.Contains("loot")).OrderBy(b => b.Name).FirstOrDefault(c => c.Items.Count >= 0 && c.Items.Count < 21);

            if (Time.AONormalTime < moveDelay) { return; }

            foreach (var itemtomove in Inventory.Items.Where(c => c.Slot.Type == IdentityType.Inventory))
            {
                if (backpack == null) { return; }

                if (CheckRules(itemtomove))
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

            foreach (var item in Inventory.Items)
            {
                if (lootBags.Any(bag => bag.Identity.Instance == item.UniqueIdentity.Instance))
                {
                    item?.Use(); // Open
                    item?.Use(); // Close
                }
            }

            isBackpackInfoInitialized = true;
        }

        public void ProcessCorpses()
        {
            var corpses = DynelManager.Corpses.Where(c => ourCorpses.ContainsKey(c.Identity.Instance) && !openedCorpses.Contains(c.Identity.Instance)
            && DynelManager.LocalPlayer.Position.DistanceFrom(c.Position) < 5).ToList();

            if (corpses.Count == 0) { return; }
            if (Time.AONormalTime < openDelay) { return; }
            if (Spell.HasPendingCast) { return; }
            if (Item.HasPendingUse) { return; }
            if (PerkAction.List.Any(perk => perk.IsExecuting)) { return; }

            foreach (var corpse in corpses)
            {
                //Chat.WriteLine($"Opening {corpse.Name}");
                corpse.Open();//open corpse
            }

            openDelay = Time.AONormalTime + 1.0;
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
                }

                #endregion

                if (_settings["Enabled"].AsBool())
                {
                    InitializeBackpackInfo();

                    if (Team.IsInTeam)
                    {
                        foreach (var teamMember in Team.Members)
                        {
                            var character = teamMember.Character;

                            if (character != null && character.IsAttacking)
                            {
                                var fightingTarget = character.FightingTarget;

                                if (fightingTarget != null && !ourMobs.Contains(fightingTarget.Identity.Instance))
                                {
                                    //Chat.WriteLine($"Adding {fightingTarget.Identity.Instance} to ourMobs");
                                    ourMobs.Add(fightingTarget.Identity.Instance);
                                }
                            }
                        }


                        //var teamCharacter = Team.Members.FirstOrDefault(t => t.Character != null && t.Character.IsAttacking
                        //&& !ourMobs.Contains(t.Character.FightingTarget.Identity.Instance))?.Character;

                        //if (teamCharacter != null)
                        //{
                        //    Chat.WriteLine($"Adding {teamCharacter.FightingTarget?.Identity.Instance} to ourMobs");
                        //    ourMobs.Add(teamCharacter.FightingTarget.Identity.Instance);
                        //}
                    }
                    else if (player.FightingTarget != null && !ourMobs.Contains(player.FightingTarget.Identity.Instance))
                    {
                        ourMobs.Add(player.FightingTarget.Identity.Instance);
                    }

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
            _infoWindow = Window.CreateFromXml("Info", PluginDir + "\\UI\\LootManagerInfoView.xml",
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
                    string globalscope = r.Global ? "G" : "L";
                    _textView.Text = $"{iEntry + 1} - {globalscope} - [ {r.Lql.PadLeft(3, ' ')} - {r.Hql.PadLeft(3, ' ')} ] - {r.Name} - {r.Quantity} - {r.BagName}";
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
            SettingsController.RegisterSettingsWindow(settingsName, PluginDir + "\\UI\\" + xmlName, _settings);
        }

        private void LoadRules()
        {
            try
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

                string filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\LootManager\\Global.json";

                GlobalRules = Rules.Where(o => o.Global == true).ToList();
                ScopeRules = Rules.Where(o => o.Global == false).ToList();

                string rulesJson = JsonConvert.SerializeObject(GlobalRules);
                File.WriteAllText(filename, rulesJson);

                filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\LootManager\\{DynelManager.LocalPlayer.Name}\\Rules.json";
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
                if (_settings["Exact"].AsBool())
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
