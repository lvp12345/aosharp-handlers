using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace MultiboxHelper.IPCMessages
{
    [AoContract((int)IPCOpcode.Use)]
    public class UseMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Use;

        [AoMember(0)]
        public Identity Target { get; set; }


        [AoMember(1)]
        public int PfId { get; set; }
    }
}
