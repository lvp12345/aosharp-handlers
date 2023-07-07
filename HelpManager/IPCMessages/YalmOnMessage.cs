using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace HelpManager.IPCMessages
{
    [AoContract((int)IPCOpcode.YalmOn)]
    public class YalmOnMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.YalmOn;

        [AoMember(0)]
        public int Spell { get; set; }

        [AoMember(1)]
        public int Item { get; set; }
    }
}
