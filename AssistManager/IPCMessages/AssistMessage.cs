using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssistManager.IPCMessages
{
    [AoContract((int)IPCOpcode.Assist)]
    public class AssistMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Assist;

        [AoMember(0)]
        public Identity Target { get; set; }
    }
}
