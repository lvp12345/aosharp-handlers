using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.NPCFinishTrade)]
    public class NpcFinishTradeMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.NPCFinishTrade;

        [AoMember(0)]
        public Identity Target { get; set; }

        [AoMember(1)]
        public int Amount { get; set; }

        [AoMember(8)]
        public int Decline { get; set; }
    }
}
