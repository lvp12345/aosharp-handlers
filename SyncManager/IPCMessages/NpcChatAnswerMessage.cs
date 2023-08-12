using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.NpcChatAnswer)]
    public class NpcChatAnswerMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.NpcChatAnswer;

        [AoMember(0)]
        public Identity Target { get; set; }

        [AoMember(1)]
        public int Answer { get; set; }
    }
}
