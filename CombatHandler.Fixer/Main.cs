using AOSharp.Core;
using AOSharp.Core.UI;
using System;

namespace CombatHandler.Fixer
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
                    Chat.WriteLine("Fixer Combat Handler Loaded!");
                    Chat.WriteLine("/handler for settings.");
                    AOSharp.Core.Combat.CombatHandler.Set(new FixerCombatHandler(PluginDirectory));
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
