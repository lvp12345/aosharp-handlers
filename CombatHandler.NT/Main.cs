using AOSharp.Core;
using AOSharp.Core.Combat;
using System;

namespace Desu
{
    public class Main : IAOPluginEntry
    {
        public void Run(string pluginDir)
        {
            try
            {
                Chat.WriteLine("NT Combat Handler Loaded!");
                CombatHandler.Set(new NTCombatHandler());
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
