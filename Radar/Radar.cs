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
        public void Run(string pluginDir)
        {
            try
            {
                Chat.WriteLine("Radar loaded!");
                Game.OnUpdate += OnUpdate;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void OnUpdate(object sender, float e)
        {
            DrawPlayers();
            DrawBots();
            DrawLifts();
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
