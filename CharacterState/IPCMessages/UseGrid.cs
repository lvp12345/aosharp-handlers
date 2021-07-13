using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatHandler.Generic.IPCMessages
{
    [AoContract((int)IPCOpcode.UseGrid)]
    public class UseGrid : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.UseGrid;
    }
}
