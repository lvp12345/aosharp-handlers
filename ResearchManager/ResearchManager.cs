using System;
using System.Diagnostics;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using AOSharp.Core.Inventory;
using AOSharp.Common.Unmanaged.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace ResearchManager
{
    public class ResearchManager : AOPluginEntry
    {
        public static string PluginDirectory;

        private static Settings _settings = new Settings("Research");

        public static List<ResearchGoal> _researchGoalsActive = new List<ResearchGoal>();

        public static List<string> _researchGoalsActiveStr = new List<string>();
        public static List<string> _researchGoalsWholeStr = new List<string>();

        public override void Run(string pluginDir)
        {
            PluginDirectory = pluginDir;

            SettingsController.RegisterSettingsWindow("Research Manager", pluginDir + $"\\UI\\Research{DynelManager.LocalPlayer.Profession}View.xml", _settings);

            _settings.AddVariable("Toggle", false);

            Chat.WriteLine("Research Manager Loaded!");
            Chat.WriteLine("/research for settings.");

            //Init to add settings
            foreach (int goal in Research.Completed)
            {
                if (!_researchGoalsWholeStr.Contains($"{N3EngineClientAnarchy.GetPerkName(goal)}"))
                    _researchGoalsWholeStr.Add($"{N3EngineClientAnarchy.GetPerkName(goal)}");

                if (_researchGoalsWholeStr.Count == 8)
                {
                    foreach (string str in _researchGoalsWholeStr)
                    {
                        _settings.AddVariable($"{str}", false);
                    }
                }
            }

            //Init to add settings
            foreach (ResearchGoal goal in Research.Goals)
            {
                if (!_researchGoalsWholeStr.Contains($"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}"))
                    _researchGoalsWholeStr.Add($"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}");

                if (_researchGoalsWholeStr.Count == 8)
                {
                    foreach (string str in _researchGoalsWholeStr)
                    {
                        _settings.AddVariable($"{str}", false);
                    }
                }
            }

            Game.OnUpdate += OnUpdate;
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        private void OnUpdate(object s, float deltaTime)
        {
            //Tick add active and remove active
            if (_settings["Toggle"].AsBool() && !Game.IsZoning)
            {
                foreach (ResearchGoal goal in Research.Goals)
                {
                    if (_settings[$"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}"].AsBool())
                    {
                        if (!goal.Available)
                        {
                            _settings[$"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}"] = false;
                            Chat.WriteLine($"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)} Line not available.");
                            return;
                        }

                        if (!_researchGoalsActive.Contains(goal))
                        {
                            _researchGoalsActive.Add(goal);
                            //Chat.WriteLine($"Adding Active - {N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}");
                        }
                    }
                }
            }

            //Tick the brain
            if (_settings["Toggle"].AsBool() && !Game.IsZoning
                && _researchGoalsActive.Count >= 1)
            {
                List<ResearchGoal> _currentGoalFinished = Research.Goals.Where(c => N3EngineClientAnarchy.GetPerkName(c.ResearchId)
                    == N3EngineClientAnarchy.GetPerkName((int)DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal))
                    && (!c.Available || c.ResearchId == 0))
                    .ToList();

                if (_currentGoalFinished.Count >= 1)
                {
                    //Chat.WriteLine($"Finished - {N3EngineClientAnarchy.GetPerkName(_currentGoalFinished.FirstOrDefault().ResearchId)}");
                    _researchGoalsActive.Remove(_currentGoalFinished.FirstOrDefault());
                    _settings[$"{N3EngineClientAnarchy.GetPerkName(_currentGoalFinished.FirstOrDefault().ResearchId)}"] = false;
                    //Chat.WriteLine($"Removing Active - {N3EngineClientAnarchy.GetPerkName(_currentGoalFinished.FirstOrDefault().ResearchId)}");

                    foreach (ResearchGoal _currentGoal in _researchGoalsActive.Where(c => N3EngineClientAnarchy.GetPerkName(c.ResearchId)
                            != N3EngineClientAnarchy.GetPerkName((int)DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal))
                            && c.Available).Take(1))
                    {
                        Research.Train(_currentGoal.ResearchId);
                        Chat.WriteLine($"Starting - {N3EngineClientAnarchy.GetPerkName(_currentGoal.ResearchId)}");
                        return;
                    }
                }
            }
        }
    }
}
