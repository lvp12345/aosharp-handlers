using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using System.Collections.Generic;
using AOSharp.Common.Unmanaged.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AOSharp.Common.GameData.UI;

namespace ResearchManager
{
    public class ResearchManager : AOPluginEntry
    {
        protected Settings _settings;

        public static bool _asyncToggle = false;

        private static double _tick;

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
                && Time.NormalTime > _tick + 3f)
            {
                _tick = Time.NormalTime;

                if (_asyncToggle == false && (DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal) == 0
                    || !Research.Goals.Where(c => N3EngineClientAnarchy.GetPerkName(c.ResearchId)
                        == N3EngineClientAnarchy.GetPerkName(DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal)))
                        .FirstOrDefault().Available))
                {
                    Task.Factory.StartNew(
                        async () =>
                        {
                            _asyncToggle = true;

                            ResearchGoal _next = Research.Goals.Where(c => N3EngineClientAnarchy.GetPerkName(c.ResearchId)
                                != N3EngineClientAnarchy.GetPerkName(DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal))
                                && c.Available)
                                .FirstOrDefault();

                            await Task.Delay(200);
                            Research.Train(_next.ResearchId);
                            //Chat.WriteLine($"Changing to line - {N3EngineClientAnarchy.GetPerkName(_next.ResearchId)} [{_next.ResearchId}]");
                            await Task.Delay(200);

                            _asyncToggle = false;
                        });
                }

            }
        }
    }
}
