using AOSharp.Core;
using System;
using AOSharp.Core.UI;

namespace CombatHandler.Engi
{
    public class Main : AOPluginEntry
    {
        public override void Run(string pluginDir)
        {
            try
            {
                Chat.WriteLine("Engi Combat Handler Loaded!");
                AOSharp.Core.Combat.CombatHandler.Set(new EngiCombatHandler(pluginDir));
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
