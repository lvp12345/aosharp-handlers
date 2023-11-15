using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.UseItem)]
    public class UsableMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.UseItem;

        [AoMember(0)]
        public int ItemId { get; set; }

        [AoMember(1)]
        public int ItemHighId { get; set; }

        [AoMember(2)]
        public Identity Target { get; set; }

        [AoMember(3)]
        public String Name { get; set; }

        [AoMember(4)]
        public int PfId { get; set; }
    }
}
