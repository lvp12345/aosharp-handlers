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

namespace CombatHandler.Engineer
{
    [AoContract((int)IPCOpcode.GlobalRez)]
    public class GlobalRezMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.GlobalRez;

        [AoMember(0)]
        public bool Switch { get; set; }
    }
}
