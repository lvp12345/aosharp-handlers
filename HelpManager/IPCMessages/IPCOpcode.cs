using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpManager.IPCMessages
{
    public enum IPCOpcode
    {
        YalmOn = 300,
        YalmUse = 301,
        YalmOff = 302,
        ClearBuffs = 303
    }
}
