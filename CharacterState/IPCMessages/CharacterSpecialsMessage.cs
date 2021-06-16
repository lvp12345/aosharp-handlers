using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System.Collections.Generic;

namespace CombatHandler.Generic.IPCMessages
{
    [AoContract((int)IPCOpcode.CharacterSpecials)]
    public class CharacterSpecialsMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.CharacterSpecials;

        public static readonly CharacterSpecialsMessage NO_SPECIALS = new CharacterSpecialsMessage();

        public static CharacterSpecialsMessage ForLocalPlayer()
        {
            HashSet<SpecialAttack> specialsSet = DynelManager.LocalPlayer.SpecialAttacks;
            return new CharacterSpecialsMessage()
            {
                Character = DynelManager.LocalPlayer.Identity,
                HasBurst = specialsSet.Contains(SpecialAttack.Burst),
                HasFlingShot = specialsSet.Contains(SpecialAttack.FlingShot),
                HasFullAuto = specialsSet.Contains(SpecialAttack.FullAuto),
                HasAimedShot = specialsSet.Contains(SpecialAttack.AimedShot),
                HasFastAttack = specialsSet.Contains(SpecialAttack.FastAttack),
                HasBrawl = specialsSet.Contains(SpecialAttack.Brawl),
                HasSneakAttack = specialsSet.Contains(SpecialAttack.SneakAttack),
                HasDimach = specialsSet.Contains(SpecialAttack.Dimach),
            };
        }

        [AoMember(0)]
        public Identity Character { get; set; }

        [AoMember(1)]
        public bool HasBurst { get; set; }

        [AoMember(2)]
        public bool HasFlingShot { get; set; }

        [AoMember(3)]
        public bool HasFullAuto { get; set; }

        [AoMember(4)]
        public bool HasAimedShot { get; set; }

        [AoMember(5)]
        public bool HasFastAttack { get; set; }

        [AoMember(6)]
        public bool HasBrawl { get; set; }

        [AoMember(7)]
        public bool HasSneakAttack { get; set; }

        [AoMember(8)]
        public bool HasDimach { get; set; }
    }
}
