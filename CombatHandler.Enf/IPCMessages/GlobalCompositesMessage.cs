using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace CombatHandler.Enf
{
    [AoContract((int)IPCOpcode.GlobalComposites)]
    public class GlobalCompositesMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.GlobalComposites;

        [AoMember(0)]
        public bool Switch { get; set; }
    }
}
