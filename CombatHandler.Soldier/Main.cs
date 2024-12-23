using AOSharp.Core;
using AOSharp.Core.UI;
using System;

namespace CombatHandler.Soldier
{
    public class Main : AOPluginEntry
    {
        public override void Run()
        {
            try
            {
                base.Run();
                Chat.WriteLine("Soldier Combat Handler Loaded!");
                Chat.WriteLine("/handler for settings.");
                AOSharp.Core.Combat.CombatHandler.Set(new SoldCombathandler(PluginDirectory));
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
