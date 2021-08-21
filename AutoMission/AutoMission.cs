using AOSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;

namespace AutoMission
{
    public class Main : AOPluginEntry
    {
        public static bool OmniEasy = false;
        public static bool OmniMedium = false;
        public static bool OmniHard = false;

        public static bool ClanEasy = false;
        public static bool ClanMedium = false;
        public static bool ClanHard = false;

        public static bool Started = false;

        public override void Run(string pluginDir)
        {
            Chat.RegisterCommand("automission", AutoMissionBotCommand);

            Game.OnUpdate += OnUpdate;
            NpcDialog.AnswerListChanged += NpcDialog_AnswerListChanged;
        }

        private void OnUpdate(object sender, float deltaTime)
        {
            try
            {
                if (Mission.List.Exists(x => x.DisplayName.Contains("Go into Trial-Area at 2833.7...")))
                {
                    Started = false;
                    return;
                }

                if (Started)
                {
                    Dynel questGiver = DynelManager.Characters.FirstOrDefault(x => x.Name == "Soul Devourer" && !x.IsPet);

                    foreach (Mission mission in Mission.List)
                    {
                        if (mission.DisplayName.Contains("Go into Notum Mine at 3192.8..."))
                            mission.Delete();
                        if (mission.DisplayName.Contains("Go into Cave of the Enlighte..."))
                            mission.Delete();
                        if (mission.DisplayName.Contains("Go into Stonecrown Dungeon a..."))
                            mission.Delete();
                    }

                    if (questGiver != null)
                    {
                        if (!Mission.List.Exists(x => x.DisplayName.Contains("Go into Notum Mine at 3192.8...")) &&
                            !Mission.List.Exists(x => x.DisplayName.Contains("Go into Cave of the Enlighte...")) &&
                            !Mission.List.Exists(x => x.DisplayName.Contains("Go into Stonecrown Dungeon a...")))
                            NpcDialog.Open(questGiver);
                    }
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e);
            }
        }

        private void NpcDialog_AnswerListChanged(object s, Dictionary<int, string> options)
        {
            SimpleChar dialogNpc = DynelManager.GetDynel((Identity)s).Cast<SimpleChar>();

            if (dialogNpc.Name == "Soul Devourer")
            {
                foreach (KeyValuePair<int, string> option in options)
                {
                    if (option.Value == "Yes I have." ||
                        option.Value == "My talents are at your disposal." ||
                        (option.Value == "I think I'll start off slowly and work my way up." && (ClanEasy || OmniEasy)) ||
                        (option.Value == "I'm pretty confident I can do what you ask!" && (ClanMedium || OmniMedium)) ||
                        (option.Value == "I will undertake the greatest challenge." && (ClanHard || OmniHard)) ||
                        option.Value == "Goodbye")
                        NpcDialog.SelectAnswer(dialogNpc.Identity, option.Key);
                }
            }
        }

        private void AutoMissionBotCommand(string command, string[] param, ChatWindow chatWindow)
        {
            try
            {
                if (param.Length < 1)
                {
                    PrintHelp(chatWindow);
                    return;
                }

                switch (param[0].ToLower())
                {
                    case "help":
                        PrintHelp(chatWindow);
                        break;

                    case "start":
                        Started = true;
                        break;

                    case "omni":
                        if (param[1].ToLower() == "easy")
                        {
                            if (!OmniEasy)
                            {
                                Chat.WriteLine("Omni Easy selected.");
                                OmniEasy = true;
                            }
                            else
                            {
                                Chat.WriteLine("Omni Easy de-selected.");
                                OmniEasy = false;
                            }
                        }
                        if (param[1].ToLower() == "medium")
                        {
                            if (!OmniMedium)
                            {
                                Chat.WriteLine("Omni Medium selected.");
                                OmniMedium = true;
                            }
                            else
                            {
                                Chat.WriteLine("Omni Medium de-selected.");
                                OmniMedium = false;
                            }
                        }
                        if (param[1].ToLower() == "hard")
                        {
                            if (!OmniHard)
                            {
                                Chat.WriteLine("Omni Hard selected.");
                                OmniHard = true;
                            }
                            else
                            {
                                Chat.WriteLine("Omni Hard de-selected.");
                                OmniHard = false;
                            }
                        }
                        break;
                    case "clan":
                        if (param[1].ToLower() == "easy")
                        {
                            if (!ClanEasy)
                            {
                                Chat.WriteLine("Clan Easy selected.");
                                ClanEasy = true;
                            }
                            else
                            {
                                Chat.WriteLine("Clan Easy de-selected.");
                                ClanEasy = false;
                            }
                        }
                        if (param[1].ToLower() == "medium")
                        {
                            if (!ClanMedium)
                            {
                                Chat.WriteLine("Clan Medium selected.");
                                ClanMedium = true;
                            }
                            else
                            {
                                Chat.WriteLine("Clan Medium de-selected.");
                                ClanMedium = false;
                            }
                        }
                        if (param[1].ToLower() == "hard")
                        {
                            if (!ClanHard)
                            {
                                Chat.WriteLine("Clan Hard selected.");
                                ClanHard = true;
                            }
                            else
                            {
                                Chat.WriteLine("Clan Hard de-selected.");
                                ClanHard = false;
                            }
                        }
                        break;
                    default:
                        return;
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void PrintHelp(ChatWindow chatWindow)
        {
            string help = "For starting omni;\n" +
                            "\n" +
                            "/automission omni difficulty\n" +
                            "\n" +
                            "\n" +
                            "For starting clan (Not implemented yet);\n" +
                            "\n" +
                            "/automission clan difficulty\n" +
                            "\n" +
                            "\n" +
                            "For starting;\n" +
                            "\n" +
                            "/automission start after selecting difficulty.\n"; ;

            chatWindow.WriteLine(help, ChatColor.LightBlue);
        }
    }
}
