using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatHandler.Doctor
{
    [AoContract((int)IPCOpcode.Disband)]
    public class DisbandMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Disband;
    }
}
