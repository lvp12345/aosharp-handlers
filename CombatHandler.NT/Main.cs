using AOSharp.Core;
using AOSharp.Core.UI;
using System;

namespace CombatHandler.NanoTechnician
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
                    Chat.WriteLine("NT Combat Handler Loaded!");
                    Chat.WriteLine("/handler for settings.");
                    AOSharp.Core.Combat.CombatHandler.Set(new NTCombatHandler(PluginDirectory));
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
