using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace CombatHandler.Bureaucrat
{
    [AoContract((int)IPCOpcode.PetSyncOn)]
    public class PetSyncOnMessag : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.PetSyncOn;
    }
}
