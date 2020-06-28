using AOSharp.Core;
using System;

namespace Desu
{
    public class Main : IAOPluginEntry
    {
        public void Run(string pluginDir)
        {
            try
            {
                Chat.WriteLine("Shade Combat Handler Loaded!");
                AOSharp.Core.Combat.CombatHandler.Set(new ShadeCombatHandler());
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
