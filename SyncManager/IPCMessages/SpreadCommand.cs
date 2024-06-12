using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.Spread)]
    public class SpreadCommand : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Spread;

        [AoMember(0)]
        public Vector3 Position { get; set; }
    }
}
