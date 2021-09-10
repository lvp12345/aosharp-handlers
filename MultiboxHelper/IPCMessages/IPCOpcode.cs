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
        Jump,
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
        NavFollow,
        Assist,
        UseItem,
        SetStat,
        Trade,
        RemainingNCU,
        Disband,
        ChannelAll,
        YalmOn,
        YalmOff
    }
}
