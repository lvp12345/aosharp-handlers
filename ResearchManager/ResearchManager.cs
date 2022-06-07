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

        public static List<ResearchGoal> _researchGoals = Research.Goals;
        public static List<ResearchGoal> _researchGoalsActive = new List<ResearchGoal>();


        //public static List<string> _researchGoalsActive = new List<string>();
        public static List<uint> _researchGoalsCompleted = new List<uint>();
        public static List<string> _researchGoalsActiveStr = new List<string>();
        public static List<string> _researchGoalsWholeStr = new List<string>();
        public static List<uint> _completedResearchGoals = new List<uint>();

        public static List<ResearchGoal> _goalListFinished;
        public static List<ResearchGoal> _trueGoalPrepList;
        public static List<ResearchGoal> _trueGoalList;
        public static List<ResearchGoal> _goalList;

        public static string _currentLineName;
        public static int _currentLineHash;

        public static uint _currentScapeGoat;

        public int _goal = 0;
        public uint _currentLine = 0;

        public uint _currentGoal = 0;

        public double _timerWorker;
        public double _timerPopList;

        public static bool _switch = false;

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

            //Init to get completed not available
            foreach (ResearchGoal goal in Research.Goals.Where(c => !c.Available))
            {
                if (!_completedResearchGoals.Contains((uint)goal.ResearchId))
                {
                    _completedResearchGoals.Add((uint)goal.ResearchId);

                    if (_settings[$"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}"].AsBool())
                    {
                        _settings[$"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}"] = false;
                    }

                    Chat.WriteLine($"Finished - {N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}");
                }
            }

            //Init to get completed
            foreach (uint goal in Research.Completed)
            {
                ResearchGoal _goal = Research.Goals.Where(c => N3EngineClientAnarchy.GetPerkName(c.ResearchId) == N3EngineClientAnarchy.GetPerkName((int)goal)).FirstOrDefault();

                if (_goal.ResearchId == 0 && !_completedResearchGoals.Contains(goal))
                {
                    _completedResearchGoals.Add(goal);

                    if (_settings[$"{N3EngineClientAnarchy.GetPerkName(_goal.ResearchId)}"].AsBool())
                    {
                        _settings[$"{N3EngineClientAnarchy.GetPerkName(_goal.ResearchId)}"] = false;
                    }

                    Chat.WriteLine($"Finished - {N3EngineClientAnarchy.GetPerkName((int)goal)}");
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
            if (_settings["Toggle"].AsBool() && !Game.IsZoning
                && Time.NormalTime > _timerPopList + 1)
            {
                foreach (ResearchGoal goal in Research.Goals)
                {
                    if (_settings[$"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}"].AsBool() && !_completedResearchGoals.Contains((uint)goal.ResearchId)
                        && goal.ResearchId != 0)
                    {
                        if (!_researchGoalsActive.Contains(goal))
                        {
                            _researchGoalsActive.Add(goal);
                            Chat.WriteLine($"Adding Active - {N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}");
                        }
                    }

                    if (!_settings[$"{N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}"].AsBool())
                    {
                        if (_researchGoalsActive.Contains(goal))
                        {
                            _researchGoalsActive.Remove(goal);
                            Chat.WriteLine($"Removing Active - {N3EngineClientAnarchy.GetPerkName(goal.ResearchId)}");
                        }
                    }
                }
                _timerPopList = Time.NormalTime;
            }

            //Tick the brain
            if (_settings["Toggle"].AsBool() && !Game.IsZoning
                && _researchGoalsActive.Count >= 1
                && Time.NormalTime > _timerWorker + 5)
            {
                //List<ResearchGoal> _goalNotAvail = Research.Goals.Where(c => !c.Available && c.ResearchId == DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal)
                //                                    && !_completedResearchGoals.Contains((uint)c.ResearchId)).ToList();

                _goalListFinished = _researchGoalsActive.Where(c => Research.Completed.Contains((uint)c.ResearchId)).ToList();

                _goalList = Research.Goals.Where(c => !Research.Completed.Contains((uint)c.ResearchId)).ToList();

                ResearchGoal _currentGoal = _researchGoalsActive.Where(c => c.ResearchId != 0 && N3EngineClientAnarchy.GetPerkName(c.ResearchId)
                    == N3EngineClientAnarchy.GetPerkName((int)DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal)))
                    .FirstOrDefault();

                if (_currentGoal.ResearchId != 0)
                {
                    _currentLineName = N3EngineClientAnarchy.GetPerkName(_currentGoal.ResearchId);
                    _currentLineHash = _currentGoal.ResearchId;
                }

                List<ResearchGoal> _researchGoalsActiveList = _researchGoalsActive
                    .Where(c => !_completedResearchGoals.Contains((uint)c.ResearchId))
                    .ToList();

                //if (_goalNotAvail.Count >= 1)
                //{
                //    _completedResearchGoals
                //        .Add((uint)_goalNotAvail.FirstOrDefault().ResearchId);
                //}

                if (!_currentGoal.Available)
                {
                    _settings[$"{_currentLineName}"] = false;

                    ResearchGoal _goal = _researchGoalsActiveList.FirstOrDefault(c => c.ResearchId == _currentLineHash);
                    
                    if (!_completedResearchGoals.Contains((uint)_currentLineHash))
                    {
                        _completedResearchGoals
                            .Add((uint)_currentLineHash);
                    }

                    if (_researchGoalsActive.Contains(_goal))
                    {
                        _researchGoalsActive.Remove(_goal);
                        Chat.WriteLine($"Finished - {_currentLineName}");
                    }

                    foreach (ResearchGoal _goalNew in _researchGoalsActive.Where(c => !_completedResearchGoals.Contains((uint)c.ResearchId)).Take(1))
                    {
                        if (_researchGoalsActive.Count >= 1)
                        {
                            Research.Train(_goalNew.ResearchId);
                            Chat.WriteLine($"Starting - {N3EngineClientAnarchy.GetPerkName(_goalNew.ResearchId)}");
                            return;
                        }
                    }
                }

                _timerWorker = Time.NormalTime;
            }
        }
    }
}
