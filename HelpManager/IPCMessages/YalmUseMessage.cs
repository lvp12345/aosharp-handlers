using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace HelpManager.IPCMessages
{
    [AoContract((int)IPCOpcode.YalmUse)]
    public class YalmUseMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.YalmUse;

        [AoMember(0)]
        public int Item { get; set; }
    }
}
