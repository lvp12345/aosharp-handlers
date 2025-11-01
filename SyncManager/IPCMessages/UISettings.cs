using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;
using static SyncManager.SyncManager;
namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.UISettings)]
    internal class UISettings : IPCMessage
    {
        [AoMember(0)]
        public bool BroadcastSettings { get; set; }

        [AoMember(1)]
        public bool Use { get; set; }

        [AoMember(2)]
        public bool Bags { get; set; }

        [AoMember(3)]
        public bool Chat { get; set; }

        [AoMember(4)]
        public bool NpcTrade { get; set; }

        [AoMember(5)]
        public bool Trade { get; set; }

        [AoMember(6)]
        public bool Attack { get; set; }

        [AoMember(7)]
        public bool Stealth { get; set; }

        [AoMember(8)]
        public bool Target { get; set; }

        [AoMember(9)]
        public bool Nanos { get; set; }
    }
}
