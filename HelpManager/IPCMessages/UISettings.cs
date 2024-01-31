using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace HelpManager.IPCMessages
{
    [AoContract((int)IPCOpcode.UISettings)]
    internal class UISettings : IPCMessage
    {
        [AoMember(0)]
        public bool BroadcastSettings { get; set; }
        [AoMember(1)]
        public bool AutoSit { get; set; }
        [AoMember(2)]
        public bool MorphPathing { get; set; }
        [AoMember(3)]
        public bool BellyPathing { get; set; }
        [AoMember(4)]
        public bool Eumenides { get; set; }
        [AoMember(5)]
        public bool Db3Shapes { get; set; }
    }
}
