using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace HelpManager.IPCMessages
{
    [AoContract((int)IPCOpcode.POHBool)]
    internal class POHBool : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.POHBool;

        [AoMember(0)]
        public bool AtPOS1 { get; set; }

        [AoMember(1)]
        public bool AtPOS2 { get; set; }

        [AoMember(2)]
        public bool AtPOS3 { get; set; }

        [AoMember(3)]
        public bool AtPOS4 { get; set; }
    }
}
