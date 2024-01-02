using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.Trade)]
    public class TradeHandleMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Trade;

        [AoMember(0)]
        public int Unknown1 { get; set; }

        [AoMember(1)]
        public TradeAction Action { get; set; }

        [AoMember(2)]
        public int Param1 { get; set; }

        [AoMember(3)]
        public int Param2 { get; set; }

        [AoMember(4)]
        public int Param3 { get; set; }

        [AoMember(5)]
        public int Param4 { get; set; }
        public Identity Target { get; set; }

        [AoMember(6)]
        public Identity Container { get; set; }
    }
}
