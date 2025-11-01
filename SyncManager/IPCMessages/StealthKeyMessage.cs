using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.StealthKey)]
    public class StealthActionMessage : IPCMessage
    {
        [AoMember(0)]
        public StealthActionType ActionType { get; set; }

        [AoMember(1)]
        public bool Activate { get; set; }
    }

    public enum StealthActionType
    {
        Stealth = 0,      // H key - stealth/sneak
        AimedShot = 1,    // O key - aimed shot special
        FlingShot = 2,    // L key - fling shot special
        Burst = 3,        // , key - burst special
        FullAuto = 4      // . key - full auto special
    }
}
