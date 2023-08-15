using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace CombatHandler.Doctor
{
    [AoContract((int)IPCOpcode.GlobalRez)]
    public class GlobalRezMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.GlobalRez;

        [AoMember(0)]
        public bool Switch { get; set; }
    }
}
