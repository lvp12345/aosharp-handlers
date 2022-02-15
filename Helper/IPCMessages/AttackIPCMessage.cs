using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace Helper.IPCMessages
{
    [AoContract((int)IPCOpcode.Attack)]
    public class AttackIPCMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Attack;

        [AoMember(0)]
        public Identity Target { get; set; }
    }
}
