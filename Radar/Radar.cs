using AOSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing;
using System.Linq;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Core.Inventory;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;

namespace Radar
{
    public class Main : IAOPluginEntry
    {
        private List<string> _trackedNames;

        public void Run(string pluginDir)
        {
            try
            {
                Chat.WriteLine("Radar loaded!");
                _trackedNames = new List<string>();
                Chat.RegisterCommand("track", TrackCallback);
                Chat.RegisterCommand("untrack", UntrackCallback);
                Game.OnUpdate += OnUpdate;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void UntrackCallback(string command, string[] args, ChatWindow window)
        {
            if (args.Length > 0)
            {
                string name = string.Join(" ", args);
                if (_trackedNames.Contains(name))
                {
                    _trackedNames.Remove(name);
                    window.WriteLine($"Removed \"{name}\" from tracking list");
                }
                else
                {
                    window.WriteLine($"Not tracking \"{name}\"");
                }
            }
            else
            {
                window.WriteLine("Please specify a name");
            }
        }

        private void TrackCallback(string command, string[] args, ChatWindow window)
        {
            if (args.Length > 0)
            {
                string name = string.Join(" ", args);
                _trackedNames.Add(name);
                window.WriteLine($"Added \"{name}\" to tracking list");
            }
            else
            {
                window.WriteLine("Please specify a name");
            }
        }

        private void OnUpdate(object sender, float e)
        {
            DrawPlayers();
            DrawBots();
            DrawLifts();
            DrawTracked();
        }

        private void DrawTracked()
        {
            foreach (SimpleChar character in DynelManager.Characters)
            {
                if (_trackedNames.Contains(character.Name))
                {
                    Debug.DrawSphere(character.Position, 1, DebuggingColor.Green);
                    Debug.DrawLine(DynelManager.LocalPlayer.Position, character.Position, DebuggingColor.Green);
                }
            }
        }

        private void DrawPlayers()
        {
            int time = (int) Time.NormalTime;

            foreach (SimpleChar player in DynelManager.Players)
            {
                if (player.Identity == DynelManager.LocalPlayer.Identity)
                    continue;

                Vector3 debuggingColor = DebuggingColor.White;

                if (Playfield.IsBattleStation)
                {
                    debuggingColor = DynelManager.LocalPlayer.GetStat(Stat.BattlestationSide) != player.GetStat(Stat.BattlestationSide) ? DebuggingColor.Red : DebuggingColor.Green;
                }
                else
                {
                    if(player.Buffs.Contains(new [] {216382, 284620, 202732, 214879 }) && time % 2 == 0)
                    {
                        debuggingColor = DebuggingColor.Red;
                    }
                    else
                    {
                        switch (player.Side)
                        {
                            case Side.Clan:
                                debuggingColor = DebuggingColor.Yellow;
                                break;
                            case Side.OmniTek:
                                debuggingColor = DebuggingColor.LightBlue;
                                break;
                            case Side.Neutral:
                                debuggingColor = DebuggingColor.Purple;
                                break;
                        }
                    }
                }

                Debug.DrawSphere(player.Position, 1, debuggingColor);
                Debug.DrawLine(DynelManager.LocalPlayer.Position, player.Position, debuggingColor);
            }
        }

        private void DrawLifts()
        {
            foreach (Dynel terminal in DynelManager.AllDynels.Where(t => t.Identity.Type == IdentityType.Terminal))
            {
                if (!terminal.Name.Contains("Button"))
                    continue;

                Debug.DrawSphere(terminal.Position, 1, DebuggingColor.White);
                Debug.DrawLine(DynelManager.LocalPlayer.Position, terminal.Position, DebuggingColor.White);
            }
        }

        private void DrawBots()
        {
            foreach (SimpleChar player in DynelManager.Players.Where(p => p.GetStat(Stat.InPlay) == 0))
            {
                Debug.DrawSphere(player.Position, 1, DebuggingColor.Blue);
                Debug.DrawLine(DynelManager.LocalPlayer.Position, player.Position, DebuggingColor.Blue);
            }
        }
    }
}
