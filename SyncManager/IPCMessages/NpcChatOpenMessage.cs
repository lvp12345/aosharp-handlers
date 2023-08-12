using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.NpcChatOpen)]
    public class NpcChatOpenMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.NpcChatOpen;

        [AoMember(0)]
        public Identity Target { get; set; }
    }
}
