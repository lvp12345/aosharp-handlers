using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.StartStop)]
    public class StartStopIPCMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.StartStop;

        [AoMember(0)]
        public bool IsStarting { get; set; }
    }
}
