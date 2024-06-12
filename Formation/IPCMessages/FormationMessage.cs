using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace Formation.IPCMessages
{
    [AoContract((int)IPCOpcode.Formation)]
    public class FormationMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Formation;

        [AoMember(0)]
        public Vector3 Position { get; set; }
    }
}
