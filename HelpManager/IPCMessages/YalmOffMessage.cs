using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpManager.IPCMessages
{
    [AoContract((int)IPCOpcode.YalmOff)]
    public class YalmOffMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.YalmOff;
    }
}
