using AOSharp.Core;
using AOSharp.Core.Combat;
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
                Chat.WriteLine("Adv Combat Handler Loaded!");
                AOSharp.Core.Combat.CombatHandler.Set(new AdvCombatHandler(pluginDir));
            }
            catch(Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
