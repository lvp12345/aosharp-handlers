using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpManager.IPCMessages
{
    public enum IPCOpcode
    {
        Follow = 2200,
        NavFollow = 2201,
        Assist = 2203,
        YalmOn = 2204,
        YalmUse = 2205,
        YalmOff = 2206,
        ClearBuffs = 2207
    }
}
