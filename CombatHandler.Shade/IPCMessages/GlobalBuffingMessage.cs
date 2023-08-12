using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace CombatHandler.Shade
{
    [AoContract((int)IPCOpcode.GlobalBuffing)]
    public class GlobalBuffingMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.GlobalBuffing;

        [AoMember(0)]
        public bool Switch { get; set; }
    }
}
