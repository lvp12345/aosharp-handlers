using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace PetManager.IPCMessages
{
    [AoContract((int)IPCOpcode.PetAttack)]
    public class PetAttackMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.PetAttack;

        [AoMember(0)]
        public Identity Target { get; set; }
    }
}
