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
        protected Settings _settings;

        public static List<ResearchGoal> _researchGoalsActive = new List<ResearchGoal>();
        public static List<ResearchGoal> _currentGoalFinished;

        public static List<string> _researchGoalsActiveStr = new List<string>();
        public static List<string> _researchGoalsWholeStr = new List<string>();

        public static bool _asyncToggle = false;

        private static double _tick;
        private static double _tickActive;


        public static string PluginDir;

        public override void Run(string pluginDir)
        {
            _settings = new Settings("Research");
            PluginDir = pluginDir;

            Game.OnUpdate += OnUpdate;

            _settings.AddVariable("Toggle", false);

            RegisterSettingsWindow("Research Manager", $"Research{DynelManager.LocalPlayer.Profession}View.xml");

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

            foreach (ResearchGoal goal in Research.Goals)
            {
                if (_settings[$"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}"].AsBool() && !_researchGoalsActive.Contains(goal))
                {
                    //if (!goal.Available)
                    //{
                    //    _settings[$"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}"] = false;
                    //    Chat.WriteLine($"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)} Line not available.");
                    //    return;
                    //}

                    _researchGoalsActive.Add(goal);
                    //Chat.WriteLine($"Adding Active - {N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}");
                }
            }

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
            //Tick add active and remove active
            if (_settings["Toggle"].AsBool() && !Game.IsZoning
                && Time.NormalTime > _tickActive + 2.0f)
            {
                foreach (ResearchGoal goal in Research.Goals)
                {
                    if (!_settings[$"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}"].AsBool() && _researchGoalsActive.Contains(goal))
                    {
                        _researchGoalsActive.Remove(goal);
                        Chat.WriteLine($"Removing Active - {N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}");
                    }
                }

                foreach (ResearchGoal goal in Research.Goals)
                {
                    if (_settings[$"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}"].AsBool() && !_researchGoalsActive.Contains(goal))
                    {
                        if (!goal.Available)
                        {
                            _settings[$"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}"] = false;
                            Chat.WriteLine($"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)} Line not available.");
                            return;
                        }

                        //_researchGoalsActive.Remove(goal);
                        _researchGoalsActive.Add(goal);
                        //Chat.WriteLine($"Adding Active - {N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}");
                    }
                }

                _tickActive = Time.NormalTime;
            }

            //Tick the brain
            if (_settings["Toggle"].AsBool() && !Game.IsZoning
                && _researchGoalsActive.Count >= 1
                && Time.NormalTime > _tick + 3.0f)
            {
                _currentGoalFinished = _researchGoalsActive.Where(c => N3EngineClientAnarchy.GetPerkName(c.ResearchId)
                    == N3EngineClientAnarchy.GetPerkName((int)DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal))
                    && (!c.Available || !Research.Goals.Where(d => N3EngineClientAnarchy.GetPerkName(d.ResearchId)
                       == N3EngineClientAnarchy.GetPerkName(c.ResearchId)).Any()))
                    .ToList();

                if (_currentGoalFinished.Count >= 1)
                {
                    if (_asyncToggle == false)
                    {
                        Task.Factory.StartNew(
                            async () =>
                            {
                                _asyncToggle = true;

                                await Task.Delay(200);
                                _settings[$"{N3EngineClientAnarchy.GetPerkName((int)DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal))}"] = false;
                                await Task.Delay(400);
                                Chat.WriteLine($"Finished - {N3EngineClientAnarchy.GetPerkName((int)DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal))}");
                                _researchGoalsActive.Remove(_researchGoalsActive.Where(c => N3EngineClientAnarchy.GetPerkName(c.ResearchId)
                                    == N3EngineClientAnarchy.GetPerkName((int)DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal)))
                                    .FirstOrDefault());
                                await Task.Delay(200);

                                foreach (ResearchGoal _currentGoal in Research.Goals.Where(c => N3EngineClientAnarchy.GetPerkName(c.ResearchId)
                                        != N3EngineClientAnarchy.GetPerkName((int)DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal))
                                        && _researchGoalsActive.Contains(c)
                                        && c.Available).Take(1))
                                {
                                    await Task.Delay(200);
                                    Research.Train(_currentGoal.ResearchId);
                                    Chat.WriteLine($"Starting - {N3EngineClientAnarchy.GetPerkName(_currentGoal.ResearchId)}");
                                    await Task.Delay(200);
                                }

                                _asyncToggle = false;
                            });
                    }
                }
                _tick = Time.NormalTime;
            }
        }
    }
}
