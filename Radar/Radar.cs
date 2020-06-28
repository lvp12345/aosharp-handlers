using AOSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core.Inventory;
using AOSharp.Core.Movement;

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
        }

        private void DrawPlayers()
        {
            foreach (SimpleChar player in DynelManager.Players)
            {
                if (player.Identity == DynelManager.LocalPlayer.Identity)
                    continue;

                Vector3 debuggingColor = DebuggingColor.White;

                if (Playfield.IsBattleStation)
                {
                    int battlestationSide = DynelManager.LocalPlayer.GetStat(Stat.BattlestationSide);

                    if (battlestationSide != player.GetStat(Stat.BattlestationSide))
                    {
                        debuggingColor = DebuggingColor.Red;
                    }
                    else
                    {
                        debuggingColor = DebuggingColor.Green;  
                    }
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

                Debug.DrawSphere(player.Position, 1, debuggingColor);
                Debug.DrawLine(DynelManager.LocalPlayer.Position, player.Position, debuggingColor);
            }
        }
    }
}
