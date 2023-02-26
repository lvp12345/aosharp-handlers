using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetManager.IPCMessages
{
    public enum IPCOpcode
    {
        PetWait = 2345,
        PetWarp = 2346,
        PetFollow = 2347
    }
}
