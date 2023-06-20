using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatHandler.Metaphysicist
{
    public enum IPCOpcode
    {
        RemainingNCU = 2000,
        GlobalBuffing = 2001,
        GlobalComposites = 2002,
        GlobalRez = 2003,
        PetAttack = 2344,
        PetWait = 2345,
        PetWarp = 2346,
        PetFollow = 2347,
        PetSyncOn = 2348,
        PetSyncOff = 2349,
        Disband = 2100,
        ClearBuffs = 2101
    }
}
