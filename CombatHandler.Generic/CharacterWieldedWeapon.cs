using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiboxHelper
{

    [Flags]
    public enum CharacterWieldedWeapon
    {
        Invalid = 0x0,            // 0x00000000000000000000b
        Fists = 0x01,             // 0x00000000000000000001b
        Melee = 0x02,             // 0x00000000000000000010b
        Ranged = 0x04,            // 0x00000000000000000100b
        Bow = 0x08,               // 0x00000000000000001000b
        Smg = 0x10,               // 0x00000000000000010000b
        Edged1H = 0x20,           // 0x00000000000000100000b
        Blunt1H = 0x40,           // 0x00000000000001000000b
        Edged2H = 0x80,           // 0x00000000000010000000b
        Blunt2H = 0x100,          // 0x00000000000100000000b
        Piercing = 0x200,         // 0x00000000001000000000b
        Pistol = 0x400,           // 0x00000000010000000000b
        AssaultRifle = 0x800,     // 0x00000000100000000000b
        Rifle = 0x1000,           // 0x00000001000000000000b
        Shotgun = 0x2000,         // 0x00000010000000000000b
        MartialArts = 0x4000,     // 0x00000100000000000000b
        MeleeEnergy = 0x8000,     // 0x00001000000000000000b 0x100000000000010b
        RangedEnergy = 0x10000,   // 0x00010000000000000000b
        Grenade = 0x20000,        // 0x00100000000000000000b
        HeavyWeapons = 0x40000,   // 0x01000000000000000000b
    }
}
