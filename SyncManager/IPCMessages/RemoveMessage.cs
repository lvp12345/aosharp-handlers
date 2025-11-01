using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.Remove)]
    public class RemoveMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Remove;

        [AoMember(0)]
        public int NanoId { get; set; }
    }
}
