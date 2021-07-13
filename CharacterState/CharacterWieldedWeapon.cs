using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Character.State
{
    public enum CharacterWieldedWeapon
    {
        Bandaid = 1337,
        Invalid = 0,
        Fists = 1,
        Bow = 12,
        Smg = 20,
        Edged1H = 34,
        Blunt1H = 66,
        Edged2H = 130,
        Blunt2H = 258,
        Piercing = 0x0200,
        Pistol = 1028,
        AssaultRifle = 2052,
        Rifle = 4100,
        Shotgun = 8196,
        Grenade = 0x20000,
        PistolAndShotgun = 9220,
        HeavyWeapons = 0x40000,
        PistolAndAssaultRifle = 3076,
    }
}
