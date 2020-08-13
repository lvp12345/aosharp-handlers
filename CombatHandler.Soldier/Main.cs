using AOSharp.Core;
using System;
using AOSharp.Core.UI;

namespace Desu
{
    public class Main : IAOPluginEntry
    {
        public void Run(string pluginDir)
        {
            try
            {
                Chat.WriteLine("Soldier Combat Handler Loaded!");
                AOSharp.Core.Combat.CombatHandler.Set(new SoldCombathandler());
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
