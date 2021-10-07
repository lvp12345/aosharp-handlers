using AOSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using MultiboxHelper;

namespace AutoLoot
{
    public class Main : AOPluginEntry
    {
        //private HashSet<Identity> _lootedCorpses = new HashSet<Identity>();
        private Dictionary<Identity, Corpse> _corpsesBeingLooted = new Dictionary<Identity, Corpse>();
        private double _lastCheckTime = Time.NormalTime;
        private Settings settings = new Settings("AutoLoot");
        private string _pluginBaseDirectory;

        public static Container extrabag1 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra1");
        public static Container extrabag2 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra2");
        public static Container extrabag3 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra3");
        public static Container extrabag4 = Inventory.Backpacks.FirstOrDefault(x => x.Name == "extra4");
        public static Corpse corpseLooting;
        public static List<Corpse> corpsesToLoot;
        public static bool LootingCorpse = false;

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

        private void OnContainerOpened(object sender, Container container)
        {
            List<Backpack> bags = Inventory.Backpacks
                .Where(c => c.Identity == container.Identity)
                .ToList();

            if (container.Items.Count >= 0 && bags.Count == 0)
            {
                foreach (Item item in container.Items)
                {
                    if (LootingRules.Apply(item))
                    {
                        if (Inventory.NumFreeSlots >= 1)
                            item.MoveToInventory();

                        else if (extrabag1 != null && extrabag1.Items.Count < 21)
                        {
                            foreach (Item itemtomove in Inventory.Items)
                            {
                                if (LootingRules.Apply(itemtomove))
                                    itemtomove.MoveToContainer(extrabag1);
                            }
                            item.MoveToInventory();
                        }
                        else if (extrabag2 != null && extrabag2.Items.Count < 21)
                        {
                            foreach (Item itemtomove in Inventory.Items)
                            {
                                if (LootingRules.Apply(itemtomove))
                                    itemtomove.MoveToContainer(extrabag2);
                            }
                            item.MoveToInventory();
                        }
                        else if (extrabag3 != null && extrabag3.Items.Count < 21)
                        {
                            foreach (Item itemtomove in Inventory.Items)
                            {
                                if (LootingRules.Apply(itemtomove))
                                    itemtomove.MoveToContainer(extrabag3);
                            }
                            item.MoveToInventory();
                        }
                        else if (extrabag4 != null && extrabag4.Items.Count < 21)
                        {
                            foreach (Item itemtomove in Inventory.Items)
                            {
                                if (LootingRules.Apply(itemtomove))
                                    itemtomove.MoveToContainer(extrabag4);
                            }
                            item.MoveToInventory();
                        }
                    }
                    else
                        item.Delete();

                }
                LootingCorpse = false;
                if (corpseLooting != null)
                    corpseLooting.Open();

                //Chat.WriteLine($"Closed corpse {LootingCorpse}");
            }
        }

        private void OnUpdate(object sender, float deltaTime)
        {
            try
            {
                if (Time.NormalTime - _lastCheckTime > 5)
                {
                    _lastCheckTime = Time.NormalTime;

                    corpsesToLoot = DynelManager.Corpses
                        .Where(corpse => corpse.DistanceFrom(DynelManager.LocalPlayer) < 6)
                        .ToList();

                    //if (corpsesToLoot.Count >= 1)
                    //{
                    //    Chat.WriteLine($"Corpse able to loot {corpsesToLoot.FirstOrDefault().Name}");
                    //    Chat.WriteLine($"Setting {LootingCorpse}");
                    //}

                    if (LootingCorpse == false)
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
            foreach (Corpse corpsefirst in corpsesToLoot)
            {
                corpsefirst.Open();

                if (corpsefirst.IsOpen)
                {
                    corpseLooting = DynelManager.Corpses
                        .Where(corpse => corpse.IsOpen)
                        .FirstOrDefault();

                    LootingCorpse = true;
                    //Chat.WriteLine($"Open corpse {LootingCorpse}");
                }
            }
        }

        public override void Teardown()
        {
            LootingRules.Save(_pluginBaseDirectory);
        }
    }
}
