using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiboxHelper.IPCMessages
{
    public enum IPCOpcode
    {
        Move,
        Target,
        Attack,
        StopAttack,
        Use,
        CharStatus,
        CharLeft,
        NpcChatOpen,
        NpcChatClose,
        NpcChatAnswer,
        Follow,
        UseItem,
        SetStat,
        Trade,
    }
}
