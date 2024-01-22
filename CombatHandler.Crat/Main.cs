using AOSharp.Core;
using AOSharp.Core.UI;
using System;

namespace CombatHandler.Bureaucrat
{
    public class Main : AOPluginEntry
    {
        public override void Run(string pluginDir)
        {
            try
            {
                if (Game.IsNewEngine)
                {
                    Chat.WriteLine("Does not work on this engine!");
                }
                else
                {
                    Chat.WriteLine("Crat Combat Handler Loaded!");
                    Chat.WriteLine("/handler for settings.");
                    AOSharp.Core.Combat.CombatHandler.Set(new CratCombatHandler(pluginDir));
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
