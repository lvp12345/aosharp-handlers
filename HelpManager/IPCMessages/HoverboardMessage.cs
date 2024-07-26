using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace HelpManager.IPCMessages
{
    [AoContract((int)IPCOpcode.Hoverboard)]
    public class HoverboardMessage : IPCMessage
    {
        public enum HoverboardAction
        {
            On,
            Off
        }

        public override short Opcode => (short)IPCOpcode.Hoverboard;

        [AoMember(0)]
        public HoverboardAction Action { get; set; }

        [AoMember(1)]
        public int Spell { get; set; }
    }
}
