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
        Use = 2403,
        NpcChatOpen = 2404,
        NpcChatClose = 2405,
        NpcChatAnswer = 2406,
        UseItem = 2407,
        Trade = 2408,
    }
}
