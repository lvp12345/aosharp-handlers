using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.IPC;
using Character.State;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;

namespace CombatHandler.Generic.IPCMessages
{
    [AoContract((int)IPCOpcode.CharacterState)]
    public class CharacterStateMessage : IPCMessage
    {
        public static CharacterStateMessage ForLocalPlayer()
        {
            return new CharacterStateMessage()
            {
                Character = DynelManager.LocalPlayer.Identity,
                RemainingNCU = DynelManager.LocalPlayer.RemainingNCU,
                WeaponType = GetLocalWeaponType(),
            };
        }

        public override short Opcode => (short)IPCOpcode.CharacterState;

        [AoMember(0)]
        public Identity Character { get; set; }

        [AoMember(1)]
        public int RemainingNCU { get; set; }

        [AoMember(2)]
        public CharacterWeaponType WeaponType { get; set; }

        private static bool IsCharacterMelee()
        {
            return DynelManager.LocalPlayer.SpecialAttacks.Contains(SpecialAttack.FastAttack)
                || DynelManager.LocalPlayer.SpecialAttacks.Contains(SpecialAttack.Brawl)
                || DynelManager.LocalPlayer.SpecialAttacks.Contains(SpecialAttack.SneakAttack)
                || DynelManager.LocalPlayer.SpecialAttacks.Contains(SpecialAttack.Backstab);
        }

        private static bool IsCharacterRanged()
        {
            return DynelManager.LocalPlayer.SpecialAttacks.Contains(SpecialAttack.FlingShot)
                || DynelManager.LocalPlayer.SpecialAttacks.Contains(SpecialAttack.Burst)
                || DynelManager.LocalPlayer.SpecialAttacks.Contains(SpecialAttack.FullAuto)
                || DynelManager.LocalPlayer.SpecialAttacks.Contains(SpecialAttack.AimedShot);
        }

        private static CharacterWeaponType GetLocalWeaponType()
        {
            if (IsCharacterMelee())
            {
                return CharacterWeaponType.MELEE;
            }

            if (IsCharacterRanged())
            {
                return CharacterWeaponType.RANGED;
            }

            return CharacterWeaponType.UNAVAILABLE;
        }
    }
}
