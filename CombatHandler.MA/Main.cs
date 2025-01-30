using AOSharp.Core;
using AOSharp.Core.UI;
using System;

namespace CombatHandler.MartialArtist
{
    public class Main : AOPluginEntry
    {
        public override void Run()
        {
            try
            {
                
                if (Game.IsNewEngine)
                {
                    Chat.WriteLine("Does not work on this engine!");
                }
                else
                {
                    Chat.WriteLine("MA Combat Handler Loaded!");
                    Chat.WriteLine("/handler for settings.");
                    AOSharp.Core.Combat.CombatHandler.Set(new MACombatHandler(PluginDirectory));
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
