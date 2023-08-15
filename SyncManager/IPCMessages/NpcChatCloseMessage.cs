using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.NpcChatClose)]
    public class NpcChatCloseMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.NpcChatClose;

        [AoMember(0)]
        public Identity Target { get; set; }
    }
}
