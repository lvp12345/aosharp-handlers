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

namespace ResearchManager
{
    public class ResearchManager : AOPluginEntry
    {
        public static string PluginDirectory;

        private static Settings _settings = new Settings("Research");

        public static List<ResearchGoal> _researchGoals;
        public static List<int> _researchGoalsActive = new List<int>();
        public static List<uint> _completedResearchGoals = new List<uint>();

        public int _goal = 0;

        public override void Run(string pluginDir)
        {
            PluginDirectory = pluginDir;

            SettingsController.RegisterSettingsWindow("Research", pluginDir + $"\\UI\\Research{DynelManager.LocalPlayer.Profession}View.xml", _settings);

            _settings.AddVariable("Toggle", false);

            _settings.AddVariable("Line1", false);
            _settings.AddVariable("Line2", false);
            _settings.AddVariable("Line3", false);
            _settings.AddVariable("Line4", false);
            _settings.AddVariable("Line5", false);
            _settings.AddVariable("Line6", false);
            _settings.AddVariable("Line7", false);
            _settings.AddVariable("Line8", false);

            Game.OnUpdate += OnUpdate;
            //Network.PacketReceived += Network_PacketReceived;


            Chat.WriteLine("ResearchManager Loaded!");
            Chat.WriteLine("/research for settings.");
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        // attach to event handler ??
        // packet - Research Update? triggers when level up, can be used to attach event handler ??

        //private void Network_PacketReceived(object s, byte[] packet)
        //{
        //    N3MessageType msgType = (N3MessageType)((packet[16] << 24) + (packet[17] << 16) + (packet[18] << 8) + packet[19]);
        //    //Chat.WriteLine($"{msgType}");

        //    if (msgType == N3MessageType.ResearchUpdate)
        //        Chat.WriteLine(BitConverter.ToString(packet).Replace("-", ""));
        //}

        private void OnUpdate(object s, float deltaTime)
        {
            if (_settings["Toggle"].AsBool() && !Game.IsZoning)
            {
                _researchGoals = Research.Goals
                    .Where(c => c.Available == true)
                    .ToList();

                _completedResearchGoals = Research.Completed;

                InitRemoveResearch();
                InitAddResearch();

                if (_completedResearchGoals.Contains((uint)DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal)))
                {
                    Chat.WriteLine($"Research line finished - {N3EngineClientAnarchy.GetPerkName(_researchGoals[_goal].ResearchId)}");
                    _researchGoalsActive.Remove(_goal + 1);

                    _goal++;

                    Research.Train(_researchGoals[_goal].ResearchId);
                    //DynelManager.LocalPlayer.SetStat(Stat.PersonalResearchGoal, _researchGoals[_goal].ResearchId);
                }
            }
        }

        public static void InitAddResearch()
        {
            if (_settings["Line1"].AsBool())
            {
                if (!_researchGoalsActive.Contains(1))
                    _researchGoalsActive.Add(1);
            }
            if (_settings["Line2"].AsBool())
            {
                if (!_researchGoalsActive.Contains(2))
                    _researchGoalsActive.Add(2);
            }
            if (_settings["Line3"].AsBool())
            {
                if (!_researchGoalsActive.Contains(3))
                    _researchGoalsActive.Add(3);
            }
            if (_settings["Line4"].AsBool())
            {
                if (!_researchGoalsActive.Contains(4))
                    _researchGoalsActive.Add(4);
            }
            if (_settings["Line5"].AsBool())
            {
                if (!_researchGoalsActive.Contains(5))
                    _researchGoalsActive.Add(5);
            }
            if (_settings["Line6"].AsBool())
            {
                if (!_researchGoalsActive.Contains(6))
                    _researchGoalsActive.Add(6);
            }
            if (_settings["Line7"].AsBool())
            {
                if (!_researchGoalsActive.Contains(7))
                    _researchGoalsActive.Add(7);
            }
            if (_settings["Line8"].AsBool())
            {
                if (!_researchGoalsActive.Contains(8))
                    _researchGoalsActive.Add(8);
            }
        }

        public static void InitRemoveResearch()
        {
            if (!_settings["Line1"].AsBool())
            {
                if (_researchGoalsActive.Contains(1))
                    _researchGoalsActive.Remove(1);
            }
            if (!_settings["Line2"].AsBool())
            {
                if (_researchGoalsActive.Contains(2))
                    _researchGoalsActive.Remove(2);
            }
            if (!_settings["Line3"].AsBool())
            {
                if (_researchGoalsActive.Contains(3))
                    _researchGoalsActive.Remove(3);
            }
            if (!_settings["Line4"].AsBool())
            {
                if (_researchGoalsActive.Contains(4))
                    _researchGoalsActive.Remove(4);
            }
            if (!_settings["Line5"].AsBool())
            {
                if (_researchGoalsActive.Contains(5))
                    _researchGoalsActive.Remove(5);
            }
            if (!_settings["Line6"].AsBool())
            {
                if (_researchGoalsActive.Contains(6))
                    _researchGoalsActive.Remove(6);
            }
            if (!_settings["Line7"].AsBool())
            {
                if (_researchGoalsActive.Contains(7))
                    _researchGoalsActive.Remove(7);
            }
            if (!_settings["Line8"].AsBool())
            {
                if (_researchGoalsActive.Contains(8))
                    _researchGoalsActive.Remove(8);
            }
        }
    }
}
