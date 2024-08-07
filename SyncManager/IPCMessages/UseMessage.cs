using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;
using static SyncManager.SyncManager;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.Use)]
    public class UseMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Use;

        [AoMember(0)]
        public int ItemId { get; set; }

        [AoMember(2)]
        public Identity Target { get; set; }

        [AoMember(3)]
        public RingName RingName { get; set; }

        [AoMember(4)]
        public int PfId { get; set; }
    }
}
