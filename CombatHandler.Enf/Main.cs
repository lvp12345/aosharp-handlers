using AOSharp.Core;
using AOSharp.Core.UI;
using System;

namespace CombatHandler.Enf
{
    public class Main : AOPluginEntry
    {
        public override void Run()
        {
            try
            {
                base.Run();
                if (Game.IsNewEngine)
                {
                    Chat.WriteLine("Does not work on this engine!");
                }
                else
                {
                    Chat.WriteLine("Enforcer Combat Handler Loaded!");
                    Chat.WriteLine("/handler for settings.");
                    AOSharp.Core.Combat.CombatHandler.Set(new EnfCombatHandler(PluginDirectory));
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
