using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace HelpManager.IPCMessages
{
    [AoContract((int)IPCOpcode.YalmOff)]
    public class YalmOffMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.YalmOff;
    }
}
