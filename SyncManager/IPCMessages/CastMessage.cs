using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.Cast)]
    public class CastMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Cast;

        [AoMember(0)]
        public int NanoId { get; set; }

        [AoMember(1)]
        public Identity Target { get; set; }
    }
}
