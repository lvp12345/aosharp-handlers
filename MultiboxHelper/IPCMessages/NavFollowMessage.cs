using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiboxHelper.IPCMessages
{
    [AoContract((int)IPCOpcode.NavFollow)]
    public class NavFollowMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.NavFollow;

        [AoMember(0)]
        public Identity Target { get; set; }
    }
}
