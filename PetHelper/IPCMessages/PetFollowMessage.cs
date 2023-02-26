using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace PetManager.IPCMessages
{
    [AoContract((int)IPCOpcode.PetFollow)]
    public class PetFollowMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.PetFollow;
    }
}
