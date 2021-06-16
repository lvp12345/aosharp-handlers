using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using System;
using System.Linq;

namespace AutoLoot
{
    public class AutoLootCommand
    {
        public static void OnAutoLootCommand(string command, string[] param, ChatWindow chatWindow)
        {
            try
            {
                HandleCommand(command, param, chatWindow);
            } catch(Exception e)
            {
                Chat.WriteLine(e);
            }
        }

        private static void HandleCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                return;
            }
            switch(param[0])
            {
                case "help":
                    PrintHelp(chatWindow);
                    break;
                case "rules":
                    HandleRulesCommand(command, param, chatWindow);
                    break;
                default:
                    chatWindow.WriteLine("Unsupported autoloot subcommand " + param[0]);
                    break;

            }
        }

        private static void HandleRulesCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length >= 2)
            {
                switch (param[1])
                {
                    case "list":
                        ListRules(chatWindow);
                        break;
                    case "remove":
                        RemoveRule(param, chatWindow);
                        break;
                    case "add":
                        AddRule(param, chatWindow);
                        break;
                    default:
                        chatWindow.WriteLine("Unknown parameter: " + param[1]);
                        PrintHelp(chatWindow);
                        break;
                }
            }
        }

        private static void AddRule(string[] param, ChatWindow chatWindow)
        {
            if (param.Length < 3)
            {
                chatWindow.WriteLine("Incorrect paramters passed to /autoloot rules. Use /autoloot help for more information.");
                return;
            }
            int currentLocation = 2;
            int minQl = 1;
            int maxQl = 500;
            while (currentLocation < param.Length && param[currentLocation] == "min" || param[currentLocation] == "max")
            {
                if (param[currentLocation] == "min")
                {
                    if (param.Length < currentLocation + 1)
                    {
                        chatWindow.WriteLine("Incorrect paramters passed to /autoloot rules. Use /autoloot help for more information.");
                        return;
                    }
                    currentLocation++;
                    minQl = Int32.Parse(param[currentLocation]);
                    currentLocation++;
                }
                if (param[currentLocation] == "max")
                {
                    if (param.Length < currentLocation + 1)
                    {
                        chatWindow.WriteLine("Incorrect paramters passed to /autoloot rules. Use /autoloot help for more information.");
                        return;
                    }
                    currentLocation++;
                    maxQl = Int32.Parse(param[currentLocation]);
                    currentLocation++;
                }
            }
            LootingRules.Add(ExtractItemName(param, currentLocation), minQl, maxQl);
        }

        private static string ExtractItemName(string[] param, int startingLocation)
        {
            string[] nameArray = new ArraySegment<string>(param, startingLocation, param.Length - startingLocation).ToArray<string>();
            return string.Join(" ", nameArray);
        }

        private static void RemoveRule(string[] param, ChatWindow chatWindow)
        {
            if (param.Length != 3)
            {
                chatWindow.WriteLine("/autoloot rules remove only takes on additional parameter - the ID of the rule to remove as seen in /autoloot rules list.");
                return;
            }
            LootingRules.Remove(Int32.Parse(param[2]));
        }

        private static void ListRules(ChatWindow chatWindow)
        {
            chatWindow.WriteLine("Looting rules:");
            LootingRules.DumpToChat(chatWindow);
        }

        private static void PrintHelp(ChatWindow chatWindow)
        {
            chatWindow.WriteLine("This command allows you to configure auto looting.");
            chatWindow.WriteLine("Usage: ");
            chatWindow.WriteLine("     /autoloot rules list - lists all currently active looting rules.");
            chatWindow.WriteLine("     /autoloot rules remove - removes a rule with given ID (this WILL SHIFT the IDs of remaining rules to compensate).");
            chatWindow.WriteLine("     /autoloot rules add <qualifiers> <name>  - adds a rule to loot items with a given name and (optional) qualifiers.");
            chatWindow.WriteLine("        <name> - full or partial name of the item to loot");
            chatWindow.WriteLine("        <qualifiers> - zero or more qualifiers for the items to loot.");
            chatWindow.WriteLine("           Available Qualifiers:");
            chatWindow.WriteLine("           * min_ql <ql> - minimum QL of the item to loot (default is 1).");
            chatWindow.WriteLine("           * max_ql <ql> - maximum QL of the item to loot (default is 500).");
        }
    }
}
