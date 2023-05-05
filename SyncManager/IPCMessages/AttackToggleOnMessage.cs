using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.AttackToggleOn)]
    public class AttackToggleOnMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.AttackToggleOn;
    }
}
