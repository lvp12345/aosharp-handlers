using AOSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using SettingsCore;

namespace AutoLoot
{
    public class Main : AOPluginEntry
    {
        //private HashSet<Identity> _lootedCorpses = new HashSet<Identity>();
        //private Dictionary<Identity, Corpse> _corpsesBeingLooted = new Dictionary<Identity, Corpse>();

        private double _lastCheckTime = Time.NormalTime;
        private AOSharp.Core.Settings settings = new AOSharp.Core.Settings("AutoLoot");
        private string _pluginBaseDirectory;

        public static Container extrabag1 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra1");
        public static Container extrabag2 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra2");
        public static Container extrabag3 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra3");
        public static Container extrabag4 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra4");

        public static List<Identity> corpseLootingIdentity = new List<Identity>();
        public static List<Corpse> corpseLooting = new List<Corpse>();
        public static List<Corpse> corpsesToLoot = new List<Corpse>();
        public static List<Identity> lootedCorpses = new List<Identity>();

        public static bool LootingCorpse = false;
        public static bool Single = false;
        public static bool Start = true;

        public override void Run(string pluginDir)
        {
            _pluginBaseDirectory = pluginDir;
            LootingRules.Load(_pluginBaseDirectory);
            settings.AddVariable("Radius", 5);
            SettingsController.RegisterSettingsWindow("Auto loot", pluginDir + "\\UI\\AutoLootSettingsView.xml", settings);
            Game.OnUpdate += OnUpdate;
            Inventory.ContainerOpened = OnContainerOpened;
            Chat.RegisterCommand("autoloot", AutoLootCommand.OnAutoLootCommand);
            Chat.WriteLine("AutoLoot loaded!");
            Chat.WriteLine("Type /autoloot help for usage instructions");
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
            if (container.Identity.Type == IdentityType.Corpse && container.Items.Count >= 0)
            {
                foreach (Item item in container.Items)
                {
                    if (LootingRules.Apply(item))
                    {
                        if (Single == true && !ItemExists(item))
                            continue;

                        if (Inventory.NumFreeSlots >= 1)
                            item.MoveToInventory();
                        else
                        {
                            if (extrabag1 != null && extrabag1.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory))
                                {
                                    if (LootingRules.Apply(itemtomove))
                                        itemtomove.MoveToContainer(extrabag1);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag2 != null && extrabag2.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory))
                                {
                                    if (LootingRules.Apply(itemtomove))
                                        itemtomove.MoveToContainer(extrabag2);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag3 != null && extrabag3.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory))
                                {
                                    if (LootingRules.Apply(itemtomove))
                                        itemtomove.MoveToContainer(extrabag3);
                                }
                                item.MoveToInventory();
                            }
                            else if (extrabag4 != null && extrabag4.Items.Count < 21)
                            {
                                foreach (Item itemtomove in Inventory.Items.Where(c => c.Slot == IdentityType.Inventory))
                                {
                                    if (LootingRules.Apply(itemtomove))
                                        itemtomove.MoveToContainer(extrabag4);
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

                if (corpseLootingIdentity.Count >= 1)
                {
                    corpseLooting = DynelManager.Corpses
                        .Where(corpse => corpseLootingIdentity.Contains(corpse.Identity))
                        .Where(corpse => corpse.DistanceFrom(DynelManager.LocalPlayer) < 5)
                        .ToList();

                    corpseLooting.FirstOrDefault().Open();
                    corpseLootingIdentity.Remove(corpseLooting.FirstOrDefault().Identity);
                }
            }
        }

        private void OnUpdate(object sender, float deltaTime)
        {
            try
            {
                if (Time.NormalTime - _lastCheckTime > 6)
                {
                    _lastCheckTime = Time.NormalTime;

                    corpsesToLoot = DynelManager.Corpses
                        .Where(corpse => !lootedCorpses.Contains(corpse.Identity))
                        .Where(corpse => corpse.DistanceFrom(DynelManager.LocalPlayer) < 5)
                        .ToList();

                    if (corpsesToLoot.Count >= 1)
                    {
                        FindNextCorpseToLoot();
                    }
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e);
            }
        }

        private void FindNextCorpseToLoot()
        {
            foreach (Corpse corpse in corpsesToLoot)
            {
                corpse.Open();

                if (corpse.IsOpen)
                {
                    corpseLootingIdentity.Add(corpse.Identity);
                    lootedCorpses.Add(corpse.Identity);
                }
            }
        }

        public override void Teardown()
        {
            LootingRules.Save(_pluginBaseDirectory);
        }
    }
}
