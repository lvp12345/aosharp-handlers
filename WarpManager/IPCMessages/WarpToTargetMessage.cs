using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace WarpManager.IPCMessages
{
    [AoContract((int)IPCOpcode.WarpToTarget)]
    public class WarpToTargetMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.WarpToTarget;
    }
}
