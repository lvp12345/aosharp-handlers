using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatHandler.Soldier
{
    public enum IPCOpcode
    {
        RemainingNCU = 2000,
        GlobalBuffing = 2001,
        GlobalComposites = 2002,
        GlobalRez = 2003,
        Disband = 2100,
        ClearBuffs = 2101
    }
}
