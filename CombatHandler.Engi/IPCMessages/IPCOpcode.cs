using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Desu
{
    public enum IPCOpcode
    {
        RemainingNCU = 2000,
        Disband = 2001,
        Target = 2002,
        Attack = 2003,
        StopAttack = 2004,
    }
}
