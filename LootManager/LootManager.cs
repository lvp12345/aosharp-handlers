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
using Newtonsoft.Json;
using System.Data;
using System.IO;
using AOSharp.Core.Movement;
using System.Net.Http;
using AOSharp.Common.Unmanaged.Interfaces;
using System.Text.RegularExpressions;

namespace LootManager
{
    public class LootManager : AOPluginEntry
    {
        private double _moveLootDelay = Time.NormalTime;
        private double _closeBag;

        //public static List<MultiListViewItem> MultiListViewItemList = new List<MultiListViewItem>();
        //public static Dictionary<ItemModel, MultiListViewItem> PreItemList = new Dictionary<ItemModel, MultiListViewItem>();

        private string previousErrorMessage = string.Empty;

        protected double _lastZonedTime = Time.NormalTime;

        private static int MinQlValue;
        private static int MaxQlValue;
        private static int ItemIdValue;
        private static string ItemNameValue;

        public static List<Rule> Rules;

        protected Settings _settings;
        public static Settings _settingsItems;
        private static bool _initiliaseBags = false;

        private static bool Looting = false;
        private static bool Delete = false;

        bool _lootingCorpse;
        double _lootingTimer;

        private Window _infoWindow;

        public static string PluginDir;
        private static bool _toggle = false;

        public override void Run(string pluginDir)
        {
            try
            {
                _settings = new Settings("LootManager");
                PluginDir = pluginDir;

                Game.OnUpdate += OnUpdate;

                RegisterSettingsWindow("Loot Manager", "LootManagerSettingWindow.xml");

                LoadRules();

                Chat.RegisterCommand("leaveopen", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    _toggle = !_toggle;

                    Chat.WriteLine("Leaving loot open now.");
                });

                Chat.RegisterCommand("lm", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Looting = !Looting;
                    Chat.WriteLine($"Looting : {Looting}.");
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
            SaveRules();
            SettingsController.CleanUp();
        }

        private void FindBagWithSpace()
        {
            foreach (Backpack backpack in Inventory.Backpacks)
            {
                // For some reason <container>.ItemsCount is returning 0 on initially starting the handler or on zoning
                if (backpack.Items.Count == 0)
                {
                    //In this case backpacks must be opened and closed first to set the item count
                    //As we cant identify a specific bag by name (yet) for the Use method, open them all .. this should only happen once
                    List<Item> bags = Inventory.Items.Where(c => c.UniqueIdentity.Type == IdentityType.Container).ToList();
                    foreach (Item bag in bags)
                    {
                        bag.Use();
                        bag.Use();
                    }
                    _initiliaseBags = true;
                }
            }
        }

        private static Backpack BagWithSpace()
        {
            foreach (Backpack backpack in Inventory.Backpacks.Where(c => c.Name.Contains("loot")))
            {
                if (backpack.Items.Count < 21)
                    return backpack;
            }
            return null;
        }

        private void OnUpdate(object sender, float deltaTime)
        {
            if (Game.IsZoning)
                return;
            if (Looting)
            {
                if (!Delete)
                {
                    List<Vector3> openedCorpses = new List<Vector3>();

                    foreach (Corpse corpse in DynelManager.Corpses.Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 6))
                    {
                        if (openedCorpses.Contains(corpse.Position))
                        {
                            continue;
                        }

                        if (Spell.List.Any(c => c.IsReady) && !Spell.HasPendingCast && !corpse.IsOpen)
                        {
                            corpse.Open();
                        }

                        if (corpse.IsOpen)
                        {
                            openedCorpses.Add(corpse.Position);
                            //Chat.WriteLine($"{corpse.Position} added.");
                        }

                        // Process the items within the corpse's container
                        foreach (Item item in corpse.Container.Items)
                        {
                            if (Inventory.NumFreeSlots > 1 && CheckRules(item))
                            {
                                item.MoveToInventory();
                            }
                        }
                    }

                    List<Vector3> positionsToRemove = new List<Vector3>();

                    foreach (Vector3 position in openedCorpses.ToList())
                    {
                        if (DynelManager.Corpses.FirstOrDefault(c => c.Position == position) == null)
                        {
                            positionsToRemove.Add(position);
                            //Chat.WriteLine($"Position {position} removed from the list.");
                        }
                    }

                    foreach (Vector3 position in positionsToRemove)
                    {
                        openedCorpses.Remove(position);
                    }
                    MoveItemsToBag();
                }


                if (Delete)
                {
                    Corpse corpse = DynelManager.Corpses.Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 6).FirstOrDefault();

                    if (corpse != null)
                    {
                        if (Spell.List.Any(c => c.IsReady) && !Spell.HasPendingCast)
                        {
                            if (Time.NormalTime > _lootingTimer + 4)
                            {
                                corpse.Open();
                                _lootingTimer = Time.NormalTime;
                            }
                        }
                        //foreach (Item item in container.Items)
                        foreach (Item item in corpse.Container.Items)
                        {
                            if (Inventory.NumFreeSlots > 1)
                            {
                                if (CheckRules(item))
                                    item.MoveToInventory();
                                else
                                    item.Delete();
                            }
                        }
                    }
                    MoveItemsToBag();
                }
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                if (SettingsController.settingsWindow.FindView("chkOnOff", out Checkbox chkOnOff))
                {
                    chkOnOff.SetValue(Looting);
                    if (chkOnOff.Toggled == null)
                        chkOnOff.Toggled += chkOnOff_Toggled;
                }

                if (SettingsController.settingsWindow.FindView("chkDel", out Checkbox chkDel))
                {
                    chkDel.SetValue(Delete);
                    if (chkDel.Toggled == null)
                        chkDel.Toggled += chkDel_Toggled;
                }

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
        }

        private void chkDel_Toggled(object sender, bool e)
        {
            Checkbox chk = (Checkbox)sender;
            Delete = e;
        }

        private void chkOnOff_Toggled(object sender, bool e)
        {
            Checkbox chk = (Checkbox)sender;
            Looting = e;
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


            Rules.Add(new Rule(_itemName.Text, _itemMinQL.Text, _itemMaxQL.Text, GlobalScope));

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

                Chat.WriteLine(ex.Message);
            }
        }

        private void MoveItemsToBag()
        {
            foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot.Type == IdentityType.Inventory))
            {
                // Only check to move if there is something to move
                if (CheckRules(itemtomove))
                {
                    if (!_initiliaseBags)
                    {
                        // Initialise bags again if the flag is false (after injecting or zoning)
                        FindBagWithSpace();
                    }
                    // Don't move if no eligible bag (name or space)
                    Backpack _bag = BagWithSpace();
                    if (_bag == null) { return; }
                    else if (Time.NormalTime > _moveLootDelay + 2)
                    {
                        itemtomove.MoveToContainer(_bag);
                    }
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

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp");

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods");

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\LootManager"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\LootManager");

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\LootManager\\{DynelManager.LocalPlayer.Name}"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\LootManager\\{DynelManager.LocalPlayer.Name}");

            string filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\LootManager\\Global.json";
            if (File.Exists(filename))
            {
                string rulesJson = File.ReadAllText(filename);
                Rules = JsonConvert.DeserializeObject<List<Rule>>(rulesJson);
                foreach (Rule rule in Rules)
                    rule.Global = true;
            }


            filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\LootManager\\{DynelManager.LocalPlayer.Name}\\Rules.json";
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

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp");

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods");

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\LootManager"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\LootManager");

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\LootManager\\{DynelManager.LocalPlayer.Name}"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\LootManager\\{DynelManager.LocalPlayer.Name}");

            string filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\LootManager\\Global.json";

            GlobalRules = Rules.Where(o => o.Global == true).ToList();
            ScopeRules = Rules.Where(o => o.Global == false).ToList();

            string rulesJson = JsonConvert.SerializeObject(GlobalRules);
            File.WriteAllText(filename, rulesJson);

            filename = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\KnowsMods\\LootManager\\{DynelManager.LocalPlayer.Name}\\Rules.json";
            rulesJson = JsonConvert.SerializeObject(ScopeRules);
            File.WriteAllText(filename, rulesJson);
        }

        private int GetLineNumber(Exception ex)
        {
            var lineNumber = 0;

            var lineMatch = Regex.Match(ex.StackTrace ?? "", @":line (\d+)$", RegexOptions.Multiline);

            if (lineMatch.Success)
                lineNumber = int.Parse(lineMatch.Groups[1].Value);

            return lineNumber;
        }

        public bool CheckRules(Item item)
        {
            foreach (Rule rule in Rules)
            {
                if (
                    item.Name.ToUpper().Contains(rule.Name.ToUpper()) &&
                    item.QualityLevel >= Convert.ToInt32(rule.Lql) &&
                    item.QualityLevel <= Convert.ToInt32(rule.Hql))
                    return true;

            }
            return false;
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
        //public int Ql;
    }
}
