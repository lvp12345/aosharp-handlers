using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper.IPCMessages
{
    [AoContract((int)IPCOpcode.NpcChatAnswer)]
    public class NpcChatAnswerMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.NpcChatAnswer;

        [AoMember(0)]
        public Identity Target { get; set; }

        [AoMember(1)]
        public int Answer { get; set; }
    }
}
