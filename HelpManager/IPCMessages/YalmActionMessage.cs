using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace HelpManager.IPCMessages
{
    [AoContract((int)IPCOpcode.YalmAction)]
    public class YalmActionMessage : IPCMessage
    {
        public enum ActionType
        {
            On,
            Use,
            Off
        }

        public override short Opcode => (short)IPCOpcode.YalmAction;

        [AoMember(0)]
        public ActionType Action { get; set; }

        [AoMember(1)]
        public int Spell { get; set; }

        [AoMember(2)]
        public int Item { get; set; }
    }
}
