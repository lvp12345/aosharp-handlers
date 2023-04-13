using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace CombatHandler.Metaphysicist
{
    [AoContract((int)IPCOpcode.PetFollow)]
    public class PetFollowMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.PetFollow;
    }
}
