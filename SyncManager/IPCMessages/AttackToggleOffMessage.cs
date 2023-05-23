using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.AttackToggleOff)]
    public class AttackToggleOffMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.AttackToggleOff;
    }
}
