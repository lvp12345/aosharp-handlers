using AOSharp.Core;
using System;
using AOSharp.Core.UI;

namespace CombatHandler.Agent
{
    public class Main : AOPluginEntry
    {
        public override void Run(string pluginDir)
        {
            try
            {
                Chat.WriteLine("Agent Combat Handler Loaded!");
                AOSharp.Core.Combat.CombatHandler.Set(new AgentCombatHandler(pluginDir));
            }
            catch(Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
