using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpManager.IPCMessages
{
    public enum IPCOpcode
    {
        Follow = 1800,
        NavFollow = 1801,
        Assist = 1802,
        YalmOn = 1803,
        YalmUse = 1804,
        YalmOff = 1805,
        ClearBuffs = 1806
    }
}
