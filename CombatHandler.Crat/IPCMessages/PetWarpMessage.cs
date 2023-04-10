using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace CombatHandler.Bureaucrat
{
    [AoContract((int)IPCOpcode.PetWarp)]
    public class PetWarpMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.PetWarp;
    }
}
