using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace CombatHandler.Shade
{
    [AoContract((int)IPCOpcode.Disband)]
    public class DisbandMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Disband;
    }
}
