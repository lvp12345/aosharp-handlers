using AOSharp.Common.GameData;
using AOSharp.Core.Inventory;
using AOSharp.Core;
using System.Linq;
using AOSharp.Core.UI;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.GameData;
using System;
using System.Collections.Generic;

namespace CombatHandler.Generic
{
    internal class Ammo
    {
        public static Dictionary<EquipSlot, Tuple<string, int>> WeaponAmmo = new Dictionary<EquipSlot, Tuple<string, int>>();
        public static double delay;

        public static void CrateOfAmmo()
        {
            try
            {
                var RightHand = DynelManager.LocalPlayer.GetWeapon((int)EquipSlot.Weap_RightHand);
                var LeftHand = DynelManager.LocalPlayer.GetWeapon((int)EquipSlot.Weap_LeftHand);

                if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.WeaponSmithing)) { return; }
                if (Item.HasPendingUse) { return; }

                foreach (var weaponItem in DynelManager.LocalPlayer.Weapons)
                {
                    GetAmmoType(weaponItem.Key, weaponItem.Value);
                }

                foreach (var ammoType in WeaponAmmo)
                {
                    if (Inventory.NumFreeSlots >= 2)
                    {
                        var anyCrate = Inventory.Items.Where(c => c.Name.Contains($"Crate of ")).FirstOrDefault();

                        if (anyCrate != null)
                        {
                            if (ammoType.Value.Item2 > 0)
                            {
                                var boxOfAmmo = Inventory.Items.Where(c => c.Name == ammoType.Value.Item1).FirstOrDefault();

                                if (boxOfAmmo == null)
                                {
                                    if (anyCrate.HighId == ammoType.Value.Item2)
                                    {
                                        anyCrate.Use();
                                    }
                                    else
                                    {
                                        var ScrewDriver = Inventory.Items.Where(c => c.HighId == 150922).FirstOrDefault();

                                        if (ScrewDriver != null)
                                        {
                                            if (Time.AONormalTime > delay)
                                            {
                                                Network.Send(new CharacterActionMessage
                                                {
                                                    Action = CharacterActionType.UseItemOnItem,
                                                    Target = ScrewDriver.Slot,
                                                    Parameter1 = (int)IdentityType.Inventory,
                                                    Parameter2 = anyCrate.Slot.Instance,
                                                });

                                                delay = Time.AONormalTime + 1.0;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = "An error occurred on line " + GenericCombatHandler.GetLineNumber(ex) + ": " + ex.Message;

                if (errorMessage != GenericCombatHandler.previousErrorMessage)
                {
                    Chat.WriteLine(errorMessage);
                    Chat.WriteLine("Stack Trace: " + ex.StackTrace);
                    GenericCombatHandler.previousErrorMessage = errorMessage;
                }
            }
        }

        private static void GetAmmoType(EquipSlot slot, WeaponItem weapon)
        {
            if (weapon != null)
            {
                var ammoInt = weapon.GetStat(Stat.AmmoType);

                if (ammoInt == 1)
                {
                    if (!WeaponAmmo.ContainsKey(slot))
                    {
                        WeaponAmmo[slot] = new Tuple<string, int>("Ammo: Box of Energy Weapon Ammo", 303138);
                    }
                }
                else if (ammoInt == 2)
                {
                    if (!WeaponAmmo.ContainsKey(slot))
                    {
                        WeaponAmmo[slot] = new Tuple<string, int>("Ammo: Box of Bullets", 303137);
                    }
                }
                else if (ammoInt == 3)
                {
                    if (!WeaponAmmo.ContainsKey(slot))
                    {
                        WeaponAmmo[slot] = new Tuple<string, int>("Ammo: Box of Flamethrower Ammo", 303139);
                    }
                }
                else if (ammoInt == 4)
                {
                    if (!WeaponAmmo.ContainsKey(slot))
                    {
                        WeaponAmmo[slot] = new Tuple<string, int>("Ammo: Box of Shotgun Shells", 303141);
                    }
                }
                else if (ammoInt == 5)
                {
                    if (!WeaponAmmo.ContainsKey(slot))
                    {
                        WeaponAmmo[slot] = new Tuple<string, int>("Ammo: Box of Arrows", 303136);
                    }
                }
                else if (ammoInt == 6)
                {
                    if (!WeaponAmmo.ContainsKey(slot))
                    {
                        WeaponAmmo[slot] = new Tuple<string, int>("Ammo: Box of Launcher Grenades", 303140);
                    }
                }
            }
        }
    }
}
