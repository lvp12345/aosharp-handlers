using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace CombatHandler.Generic.IPCMessages
{
    [AoContract((int)IPCOpcode.Kitting)]
    public class KittingMessage : IPCMessage
    {
        [AoMember(1)]
        public bool IsReady { get; set; }
        public override short Opcode => (short)IPCOpcode.Kitting;
    }
}
