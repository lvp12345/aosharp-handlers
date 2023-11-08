using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.NpcChat)]
    public class NpcChatIPCMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.NpcChat;

        [AoMember(0)]
        public Identity Target { get; set; }

        [AoMember(1)]
        public bool? OpenClose { get; set; } 

        [AoMember(2)]
        public int? Answer { get; set; } 
    }
}
