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
        private HashSet<Identity> _lootedCorpses = new HashSet<Identity>();
        private Dictionary<Identity, Corpse> _corpsesBeingLooted = new Dictionary<Identity, Corpse>();
        private double _lastCheckTime = Time.NormalTime;
        private Settings settings = new Settings("AutoLoot");
        private string _pluginBaseDirectory;

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
            if (_corpsesBeingLooted.ContainsKey(container.Identity))
            {
                if (container.Items.Count > 0)
                {
                    foreach (Item item in container.Items)
                    {
                        if (LootingRules.Apply(item))
                        {
                            item.MoveToInventory();
                        }
                    }
                    Corpse currentCorpse = _corpsesBeingLooted[container.Identity];
                    currentCorpse.Open();
                    _lootedCorpses.Add(currentCorpse.Identity);
                    _corpsesBeingLooted.Remove(container.Identity);
                }
            }
        }

        private void OnUpdate(object sender, float deltaTime)
        {
            try
            {
                if (Time.NormalTime - _lastCheckTime > 5)
                {
                    _lastCheckTime = Time.NormalTime;

                    FindNextCorpseToLoot();
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e);
            }
        }

        private void FindNextCorpseToLoot()
        {
            Corpse corpseToLoot = DynelManager.Corpses
                .OrderBy(corpse => corpse.DistanceFrom(DynelManager.LocalPlayer))
                .Where(corpse => corpse.DistanceFrom(DynelManager.LocalPlayer) < 5 && !_lootedCorpses.Contains(corpse.Container.Identity) && !_corpsesBeingLooted.ContainsKey(corpse.Container.Identity))
                .FirstOrDefault();
            if (corpseToLoot != null)
            {
                _corpsesBeingLooted.Add(corpseToLoot.Container.Identity, corpseToLoot);
                corpseToLoot.Open();
            }
        }

        public override void Teardown()
        {
            LootingRules.Save(_pluginBaseDirectory);
        }
    }
}
