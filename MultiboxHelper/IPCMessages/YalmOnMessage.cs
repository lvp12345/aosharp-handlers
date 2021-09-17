using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiboxHelper.IPCMessages
{
    [AoContract((int)IPCOpcode.YalmOn)]
    public class YalmOnMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.YalmOn;

        [AoMember(0)]
        public int spell { get; set; }
    }
}
