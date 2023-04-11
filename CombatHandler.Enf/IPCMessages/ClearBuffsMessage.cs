using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatHandler.Enf
{
    [AoContract((int)IPCOpcode.ClearBuffs)]
    public class ClearBuffsMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.ClearBuffs;
    }
}
