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
    [AoContract((int)IPCOpcode.NpcChatOpen)]
    public class NpcChatOpenMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.NpcChatOpen;

        [AoMember(0)]
        public Identity Target { get; set; }
    }
}
