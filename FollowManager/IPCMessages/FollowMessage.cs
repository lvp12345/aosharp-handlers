using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FollowManager.IPCMessages
{
    [AoContract((int)IPCOpcode.Follow)]
    public class FollowMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Follow;

        [AoMember(0)]
        public Identity Target { get; set; }
    }
}
