using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncManager.IPCMessages
{
    public enum IPCOpcode
    {
        Move = 2400,
        Jump = 2401,
        Target = 2402,
        Attack = 2403,
        StopAttack = 2404,
        Use = 2405,
        NpcChatOpen = 2406,
        NpcChatClose = 2407,
        NpcChatAnswer = 2408,
        UseItem = 2409,
        Trade = 2410,
    }
}
