using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace CombatHandler.Bureaucrat
{
    [AoContract((int)IPCOpcode.PetSyncOff)]
    public class PetSyncOffMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.PetSyncOff;
    }
}
