using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace CombatHandler.Shade
{
    [AoContract((int)IPCOpcode.RemainingNCU)]
    public class RemainingNCUMessage : IPCMessage
    {
        public static RemainingNCUMessage ForLocalPlayer()
        {
            return new RemainingNCUMessage()
            {
                Character = DynelManager.LocalPlayer.Identity,
                RemainingNCU = DynelManager.LocalPlayer.RemainingNCU,
            };
        }

        public override short Opcode => (short)IPCOpcode.RemainingNCU;

        [AoMember(0)]
        public Identity Character { get; set; }

        [AoMember(1)]
        public int RemainingNCU { get; set; }
    }
}
