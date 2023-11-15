using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Interfaces;
using AOSharp.Core;
using AOSharp.Core.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ResearchManager
{
    public class ResearchManager : AOPluginEntry
    {
        protected Settings _settings;

        public static bool _asyncToggle = false;

        private static double _tick;

        public static string PluginDir;

        private bool enabled => _settings["Toggle"].AsBool();
        private bool includeApotheosis => _settings["IncludeApotheosis"].AsBool();
        private ModeSelection mode => (ModeSelection)_settings["ModeSelection"].AsInt32();
        private float updateInterval = 10;

        public static List<int> apotheosis = Enumerable.Range(10002, 10).ToList();

        public override void Run(string pluginDir)
        {
            _settings = new Settings("Research");
            PluginDir = pluginDir;

            Game.OnUpdate += OnUpdate;

            _settings.AddVariable("Toggle", false);
            _settings.AddVariable("IncludeApotheosis", false);
            _settings.AddVariable("ModeSelection", (int)ModeSelection.HighestFirst);
            _settings.AddVariable("UpdateInterval", 10);

            RegisterSettingsWindow("Research Manager", $"ResearchManagerSettingWindow.xml");

            Chat.WriteLine("Research Manager Loaded!");
            Chat.WriteLine("/researchmanager for settings.");
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
            if (!enabled || Game.IsZoning || Time.NormalTime < _tick + updateInterval)
                return;

            _tick = Time.NormalTime;

            var availableGoals = Research.Goals.Where(goal => goal.Available && (includeApotheosis || !apotheosis.Contains(goal.ResearchId)));
            var researchPerks = Perk.GetByInstance(availableGoals.Select(goal => goal.ResearchId)).ToDictionary(perk => perk.Instance, perk => perk);

            if (mode == ModeSelection.LowestFirst)
            {
                availableGoals = availableGoals.OrderBy(goal => researchPerks[goal.ResearchId].Level).ThenByDescending(goal => N3EngineClientAnarchy.GetPerkProgress((uint)goal.ResearchId));
            }

            if (mode == ModeSelection.HighestFirst)
            {
                availableGoals = availableGoals.OrderByDescending(goal => researchPerks[goal.ResearchId].Level).ThenByDescending(goal => N3EngineClientAnarchy.GetPerkProgress((uint)goal.ResearchId));
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

        enum ModeSelection
        {
            LowestFirst,
            HighestFirst
        }
    }
}
