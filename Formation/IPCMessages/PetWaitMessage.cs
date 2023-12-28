using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace Formation.IPCMessages
{
    [AoContract((int)IPCOpcode.PetWait)]
    public class PetWaitMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.PetWait;
    }
}
