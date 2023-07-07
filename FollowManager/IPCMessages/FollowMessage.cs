using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace FollowManager.IPCMessages
{
    [AoContract((int)IPCOpcode.Follow)]
    public class FollowMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Follow;

        [AoMember(0)]
        public Identity Target { get; set; }
    }
}
