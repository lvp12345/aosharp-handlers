using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using System.Collections.Generic;
using System.Linq;

namespace CombatHandler.Generic
{
    public class TauntTools
    {
        private const int AGGRESSION_MULTIPLIER_LOW = 83920;
        private const int AGGRESSION_MULTIPLIER_HIGH = 83919;
        private const int AGGRESSION_MULTIPLIER_JEALOUSY = 152029;
        private const int CODEX_OF_INSULTING_EMERTO = 253186;
        private const int SCORPIOS_AIM_OF_ANGER = 244655;
        private static List<int> TAUNT_TOOL_BY_PRIORITY = new List<int>() { SCORPIOS_AIM_OF_ANGER, AGGRESSION_MULTIPLIER_JEALOUSY, CODEX_OF_INSULTING_EMERTO, AGGRESSION_MULTIPLIER_HIGH, AGGRESSION_MULTIPLIER_LOW };
        private static Item tauntTool = null;
        private static bool tauntToolSearched;

        public static Item GetBestTauntTool()
        {
            if (!tauntToolSearched)
            {
                int preferredTauntTool = TAUNT_TOOL_BY_PRIORITY.Where(itemId => Inventory.Find(itemId, out tauntTool)).FirstOrDefault();
                Inventory.Find(preferredTauntTool, out tauntTool);
                if (tauntTool == null)
                {
                    Chat.WriteLine("Failed to locate taunt tool. Will not pull from outside range.");
                }
                tauntToolSearched = true;
            }
            return tauntTool;
        }

        public static bool CanUseTauntTool()
        {
            return !Item.HasPendingUse && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Psychology) && GetBestTauntTool() != null;
        }
    }
}
