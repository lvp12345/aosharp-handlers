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

        public static List<ResearchGoal> _finishedGoals = new List<ResearchGoal>();

        public static bool _asyncToggle = false;

        private static double _tick;
        private static int _currentEnd = 0;

        public static string PluginDir;

        public override void Run(string pluginDir)
        {
            _settings = new Settings("Research");
            PluginDir = pluginDir;

            Game.OnUpdate += OnUpdate;

            _settings.AddVariable("Toggle", false);

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
            if (_settings["Toggle"].AsBool() && !Game.IsZoning
                && Time.NormalTime > _tick + 0.5f)
            {
                if (DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal) >= 1 
                    || (DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal) == 0 && Research.Completed.Contains((uint)_currentEnd))) 
                {
                    ResearchGoal _current = Research.Goals.Where(c => !_finishedGoals.Contains(c) && N3EngineClientAnarchy.GetPerkName(c.ResearchId)
                        == N3EngineClientAnarchy.GetPerkName(DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal)))
                        .FirstOrDefault();

                    if (_currentEnd == 0)
                    {
                        if (Utilz.Last(_current.ResearchId) == 9)
                            _currentEnd = _current.ResearchId + 1;
                        else if (Utilz.Last(_current.ResearchId) == 0)
                            _currentEnd = _current.ResearchId;
                    }

                    if (_current.Available) { return; }

                    if (_asyncToggle == false)
                    {
                        Task.Factory.StartNew(
                            async () =>
                            {
                                _asyncToggle = true;

                                _finishedGoals.Add(_current);

                                ResearchGoal _next = Research.Goals.Where(c => N3EngineClientAnarchy.GetPerkName(c.ResearchId)
                                    != N3EngineClientAnarchy.GetPerkName(DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal)))
                                    .FirstOrDefault();

                                await Task.Delay(200);
                                _currentEnd = 0;
                                await Task.Delay(200);
                                Research.Train(_next.ResearchId);
                                await Task.Delay(200);

                                _asyncToggle = false;
                            });
                    }
                }

                _tick = Time.NormalTime;
            }
        }
    }
}
