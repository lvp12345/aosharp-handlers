using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Interfaces;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ResearchManager
{
    public class ResearchManager : AOPluginEntry
    {
        protected Settings _settings;
        public static string PluginDir;

        double lastUpdateTime;
        bool enabled => _settings["Toggle"].AsBool();
        bool includeApotheosis => _settings["IncludeApotheosis"].AsBool();
        ModeSelection mode => (ModeSelection)_settings["ModeSelection"].AsInt32();
        float updateInterval = 0;

        static List<int> apotheosis = Enumerable.Range(10002, 10).ToList();

        public override void Run(string pluginDir)
        {
            _settings = new Settings("Research");
            PluginDir = pluginDir;

            _settings.AddVariable("Toggle", false);
            _settings.AddVariable("IncludeApotheosis", false);
            _settings.AddVariable("ModeSelection", (int)ModeSelection.HighestFirst);
            _settings.AddVariable("UpdateInterval", 10);

            RegisterSettingsWindow("Research Manager", $"ResearchManagerSettingWindow.xml");

            Chat.WriteLine("Research Manager Loaded!");
            Chat.WriteLine("/researchmanager for settings.");
            Game.OnUpdate += OnUpdate;
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDir + "\\UI\\" + xmlName, _settings);
        }

        private void OnUpdate(object s, float deltaTime)
        {
            if (!enabled || Game.IsZoning || Time.NormalTime < lastUpdateTime + updateInterval)
                return;

            lastUpdateTime = Time.NormalTime;

            var availableGoals = Research.Goals.Where(goal => goal.Available && (includeApotheosis || !apotheosis.Contains(goal.ResearchId)));

            if (mode == ModeSelection.LowestFirst)
            {
                availableGoals = availableGoals.OrderBy(goal => GetPerkLevel(goal.ResearchId)).ThenByDescending(goal => N3EngineClientAnarchy.GetPerkProgress((uint)goal.ResearchId));
            }

            if (mode == ModeSelection.HighestFirst)
            {
                availableGoals = availableGoals.OrderByDescending(goal => GetPerkLevel(goal.ResearchId)).ThenByDescending(goal => N3EngineClientAnarchy.GetPerkProgress((uint)goal.ResearchId));
            }

            if (availableGoals.Count() > 0)
            {
                ResearchGoal goal = availableGoals.First();

                if (DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal) != goal.ResearchId)
                {
                    Research.Train(goal.ResearchId);
                }
            }

            return;
        }

        private int GetPerkLevel(int perkId)
        {
            if (apotheosis.Contains(perkId))
                return ((perkId - 2) % 10) + 1;
            else
                return (perkId % 10) + 1;
        }

        enum ModeSelection
        {
            LowestFirst,
            HighestFirst
        }
    }
}
