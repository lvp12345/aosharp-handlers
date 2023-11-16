using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.GameData;
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
        public bool OpenClose { get; set; }

        [AoMember(2)]
        public int Answer { get; set; }

        [AoMember(3)]
        public bool IsStartTrade { get; set; }
        [AoMember(4)]
        public bool IsTrade { get; set; }
        [AoMember(5)]
        public bool IsFinishTrade { get; set; }
        [AoMember(6)]
        public Identity Container { get; set; }
        [AoMember(7)]
        public int NumberOfItemSlotsInTradeWindow { get; set; }
        [AoMember(8)]
        public int Decline { get; set; }
        [AoMember(9)]
        public int Amount { get; set; }

    }
}
