using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncManager.IPCMessages
{
    public enum IPCOpcode
    {
        Move = 200,
        Jump = 201,
        Target = 202,
        Attack = 203,
        StopAttack = 204,
        Use = 205,
        NpcChatOpen = 206,
        NpcChatClose = 207,
        NpcChatAnswer = 208,
        UseItem = 209,
        Trade = 210,
    }
}
