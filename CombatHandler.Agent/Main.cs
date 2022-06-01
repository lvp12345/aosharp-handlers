using AOSharp.Core;
using System;
using AOSharp.Core.UI;

namespace Desu
{
    public class Main : AOPluginEntry
    {
        public override void Run(string pluginDir)
        {
            try
            {
                Chat.WriteLine("Agent Combat Handler Loaded!");
                Chat.WriteLine("/handler for settings.");
                AOSharp.Core.Combat.CombatHandler.Set(new AgentCombatHandler(pluginDir));
            }
            catch(Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
