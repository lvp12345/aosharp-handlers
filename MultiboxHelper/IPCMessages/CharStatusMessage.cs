using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace MultiboxHelper.IPCMessages
{
    [AoContract((int)IPCOpcode.CharStatus)]
    public class CharStatusMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.CharStatus;

        [AoMember(0, SerializeSize = ArraySizeType.Byte)]
        public string Name { get; set; }

        [AoMember(1)]
        public int Health { get; set; }

        [AoMember(2)]
        public int MaxHealth { get; set; }

        [AoMember(3)]
        public int Nano { get; set; }

        [AoMember(4)]
        public int MaxNano { get; set; }
    }
}
