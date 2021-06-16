using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace AutoLoot
{
    public class LootingRules
    {
        private static LootingRulesConfig _rulesConfig = new LootingRulesConfig();

        public static void Load(string pluginBaseDirectory)
        {
            string configPath = pluginBaseDirectory + "\\looting_rules.json";
            if (File.Exists(configPath))
            {
                string lootingRulesJson = File.ReadAllText(configPath);
                _rulesConfig = JsonConvert.DeserializeObject<LootingRulesConfig>(lootingRulesJson);
            }
        }

        public static void Save(string pluginBaseDirectory)
        {
            try
            {
                string configPath = pluginBaseDirectory + "\\looting_rules.json";
                string lootingRulesJson = JsonConvert.SerializeObject(_rulesConfig);
                File.WriteAllText(configPath, lootingRulesJson);
            } catch(Exception e)
            {
                Chat.WriteLine(e);
            }
        }

        public static void Add(string name, int minQl, int maxQl)
        {
            ItemRule newRule = new ItemRule(name, minQl, maxQl);
            if(!_rulesConfig.Rules.Contains(newRule))
            {
                _rulesConfig.Rules.Add(newRule);
            }
        }

        public static void Remove(int index)
        {
            _rulesConfig.Rules.RemoveAt(index);
        }

        public static bool Apply(Item item)
        {
            return _rulesConfig.Rules.Find(rules => rules.Apply(item)) != null;
        }

        internal static void DumpToChat(ChatWindow chatWindow)
        {
            int index = 0;
            _rulesConfig.Rules.ForEach(rule => rule.DumpToChat(chatWindow, index++));
        }
    }

    class LootingRulesConfig
    {
        public readonly List<ItemRule> Rules = new List<ItemRule>();
    }

    public class ItemRule 
    {
        public string name;
        public int minQl;
        public int maxQl;

        public ItemRule(string name, int minQl, int maxQl)
        {
            this.name = name;
            this.minQl = minQl;
            this.maxQl = maxQl;
        }

        public bool Apply(Item item)
        {
            return item.Name.Contains(name) && item.QualityLevel >= minQl && item.QualityLevel <= maxQl;
        }

        public void DumpToChat(ChatWindow chatWindow, int index)
        {
            chatWindow.WriteLine(index + " - " + name + " QL" + minQl + "-" + maxQl);
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || !(obj is ItemRule))
            {
                return false;
            }
            else
            {
                ItemRule otherRule = (ItemRule)obj;
                return name == otherRule.name && minQl == otherRule.minQl && maxQl == otherRule.maxQl;
            }
        }

        public override int GetHashCode()
        {
            return minQl * 100 + maxQl * 10 + name.GetHashCode(); ;
        }
    }
}
