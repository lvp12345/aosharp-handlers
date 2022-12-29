using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace CombatHandler.Enf
{
    [AoContract((int)IPCOpcode.GlobalDebuffing)]
    public class GlobalDebuffingMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.GlobalDebuffing;

        [AoMember(0)]
        public bool Switch { get; set; }
    }
}
