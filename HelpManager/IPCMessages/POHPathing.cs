using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace HelpManager.IPCMessages
{
    [AoContract((int)IPCOpcode.POHPathing)]
    internal class POHPathing : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.POHPathing;

        [AoMember(0)]
        public Vector3 Position { get; set; }
    }
}
