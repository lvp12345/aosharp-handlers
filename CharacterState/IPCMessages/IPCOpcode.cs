using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatHandler.Generic.IPCMessages
{
    public enum IPCOpcode
    {
        CharacterState = 90,
        Disband = 91,
        CharacterSpecials = 92,
        UseGrid = 93,
        UseFGrid = 94
    }
}
