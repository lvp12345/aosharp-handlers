using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper.IPCMessages
{
    [AoContract((int)IPCOpcode.ChannelAll)]
    public class IPCChannelAllMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.ChannelAll;

        [AoMember(0)]
        public int Channel { get; set; }
    }
}
