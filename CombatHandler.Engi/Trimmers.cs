using AOSharp.Common.GameData;
using AOSharp.Core.Inventory;
using AOSharp.Core;
using System.Collections.Generic;
using System.Linq;
using static CombatHandler.Engineer.EngiCombatHandler;
using static CombatHandler.Generic.GenericCombatHandler;
using System;

namespace CombatHandler.Engineer
{
    internal class Trimmers
    {
        public static double LastTrimTime;
        public static double AGGAttackDelay = 1.0;
        public static double AGGSupportDelay = 2.0;
        public static double MEAttackDelay = 3.0;
        public static double MESupportDelay = 4.0;
        public static double EEAttackDelay = 5.0;
        public static double EESupportDelay = 6.0;
        public static double AggDefAttackDelay = 7.0;
        public static double AggDefSupportDelay = 9.0;

        public const float DelayBetweenFiveMinTrims = 310;
        public const float DelayBetweenActuatorTrims = 3610;

        public static Dictionary<PetType, bool> petTrimmedAggressive = new Dictionary<PetType, bool>()
        {
            {PetType.Attack, false }, {PetType.Support, false}
        };
        public static Dictionary<PetType, bool> petTrimmedAggDef = new Dictionary<PetType, bool>()
        {
            {PetType.Attack, false }, {PetType.Support, false}
        };
        public static Dictionary<PetType,Tuple< bool, double>> petMEtrimmed = new Dictionary<PetType, Tuple< bool, double>>()
        {
            {PetType.Attack, Tuple.Create(false, 0.0)}, {PetType.Support, Tuple.Create(false, 0.0)}
        };

        public static Dictionary<PetType, Tuple<bool, double>> petEEtrimmed = new Dictionary<PetType, Tuple<bool, double>>()
        {
            {PetType.Attack, Tuple.Create(false, 0.0)}, {PetType.Support, Tuple.Create(false, 0.0)}
        };

        public static bool IncreaseAggressivenessTrimmer(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (item == null) { return false; }
            if (Item.HasPendingUse) { return false; }

            if (_settings["IncreaseAggressivenessTrimmer"].AsBool())
            {
                if (Time.AONormalTime < LastTrimTime + AGGAttackDelay) { return false; }

                Pet _attackPet = DynelManager.LocalPlayer.Pets
                    .FirstOrDefault(c => c.Character != null && c.Type == PetType.Attack && !petTrimmedAggressive[c.Type]);
                if (_attackPet != null)
                {
                    //Chat.WriteLine($"Using {item.Name} on {_attackPet.Character.Name}.");
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = _attackPet.Character;
                    petTrimmedAggressive[PetType.Attack] = true;
                    return true;
                }
            }

            if (_settings["SupportIncreaseAggressivenessTrimmer"].AsBool())
            {
                if (Time.AONormalTime < LastTrimTime + AGGSupportDelay) { return false; }

                Pet _supportPet = DynelManager.LocalPlayer.Pets
                    .FirstOrDefault(c => c.Character != null && c.Type == PetType.Support && !petTrimmedAggressive[c.Type]);

                if (_supportPet != null)
                {
                    //Chat.WriteLine($"Using {item.Name}  on  {_supportPet.Character.Name}.");
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = _supportPet.Character;
                    petTrimmedAggressive[PetType.Support] = true;
                    return true;
                }
            }
            return false;
        }
        public static bool DivertEnergyToDefense(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (item == null) { return false; }
            if (Item.HasPendingUse) { return false; }

            if (MechEngiSelection.DivertEnergyToDefense ==
                (MechEngiSelection)_settings["MechEngiSelection"].AsInt32())
            {
                if (MechEngieAttack(item, ref actionTarget))
                {
                    return true;
                }
            }

            if (SupportMechEngiSelection.DivertEnergyToDefense ==
                (SupportMechEngiSelection)_settings["SupportMechEngiSelection"].AsInt32())
            {
                if (MechEngieSupport(item, ref actionTarget))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool DivertEnergyToOffense(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (item == null) { return false; }
            if (Item.HasPendingUse) { return false; }

            if (MechEngiSelection.DivertEnergyToOffense ==
                (MechEngiSelection)_settings["MechEngiSelection"].AsInt32())
            {
                if (MechEngieAttack(item, ref actionTarget))
                {
                    return true;
                }
            }

            if (SupportMechEngiSelection.DivertEnergyToOffense ==
               (SupportMechEngiSelection)_settings["SupportMechEngiSelection"].AsInt32())
            {
                if (MechEngieSupport(item, ref actionTarget))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool ColdDamageModifier(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (item == null) { return false; }
            if (Item.HasPendingUse) { return false; }

            if (MechEngiSelection.ColdDamageModifier ==
                (MechEngiSelection)_settings["MechEngiSelection"].AsInt32())
            {
                if (MechEngieAttack(item, ref actionTarget))
                {
                    return true;
                }
            }

            if (SupportMechEngiSelection.ColdDamageModifier ==
               (SupportMechEngiSelection)_settings["SupportMechEngiSelection"].AsInt32())
            {
                if (MechEngieSupport(item, ref actionTarget))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool FireDamageModifier(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (item == null) { return false; }
            if (Item.HasPendingUse) { return false; }

            if (MechEngiSelection.FireDamageModifier ==
                (MechEngiSelection)_settings["MechEngiSelection"].AsInt32())
            {
                if (MechEngieAttack(item, ref actionTarget))
                {
                    return true;
                }
            }

            if (SupportMechEngiSelection.FireDamageModifier ==
               (SupportMechEngiSelection)_settings["SupportMechEngiSelection"].AsInt32())
            {
                if (MechEngieSupport(item, ref actionTarget))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool EnergyDamageModifier(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (item == null) { return false; }
            if (Item.HasPendingUse) { return false; }

            if (MechEngiSelection.EnergyDamageModifier ==
                (MechEngiSelection)_settings["MechEngiSelection"].AsInt32())
            {
                if (MechEngieAttack(item, ref actionTarget))
                {
                    return true;
                }
            }

            if (SupportMechEngiSelection.EnergyDamageModifier ==
               (SupportMechEngiSelection)_settings["SupportMechEngiSelection"].AsInt32())
            {
                if (MechEngieSupport(item, ref actionTarget))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool ImproveActuators(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (item == null) { return false; }
            if (Item.HasPendingUse) { return false; }

            if (MechEngiSelection.ImproveActuators ==
                (MechEngiSelection)_settings["MechEngiSelection"].AsInt32())
            {
                if (MechEngieAttack(item, ref actionTarget))
                {
                    return true;
                }
            }

            if (SupportMechEngiSelection.ImproveActuators ==
               (SupportMechEngiSelection)_settings["SupportMechEngiSelection"].AsInt32())
            {
                if (MechEngieSupport(item, ref actionTarget))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool DivertEnergyToAvoidance(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (item == null) { return false; }
            if (Item.HasPendingUse) { return false; }

            if (ElecEngiSelection.DivertEnergyToAvoidance ==
                (ElecEngiSelection)_settings["ElecEngiSelection"].AsInt32())
            {
                if (ElecEngieAttack(item, ref actionTarget))
                {
                    return true;
                }
            }

            if (SupportElecEngiSelection.DivertEnergyToAvoidance ==
                (SupportElecEngiSelection)_settings["SupportElecEngiSelection"].AsInt32())
            {
                if (ElecEngieSupport(item, ref actionTarget))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool DivertEnergyToHitpoints(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (item == null) { return false; }
            if (Item.HasPendingUse) { return false; }

            if (ElecEngiSelection.DivertEnergyToHitpoints ==
                (ElecEngiSelection)_settings["ElecEngiSelection"].AsInt32())
            {
                if (ElecEngieAttack(item, ref actionTarget))
                {
                    return true;
                }
            }

            if (SupportElecEngiSelection.DivertEnergyToHitpoints ==
                (SupportElecEngiSelection)_settings["SupportElecEngiSelection"].AsInt32())
            {
                if (ElecEngieSupport(item, ref actionTarget))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool NegativeAggressiveDefensive(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (item == null) { return false; }
            if (Item.HasPendingUse) { return false; }

            if (AggressiveDefensiveSelection.NegativeAggressiveDefensive ==
                (AggressiveDefensiveSelection)_settings["AggressiveDefensiveSelection"].AsInt32())
            {
                if (AggDefAttack(item, ref actionTarget))
                {
                    return true;
                }
            }

            if (SupportAggressiveDefensiveSelection.NegativeAggressiveDefensive ==
                (SupportAggressiveDefensiveSelection)_settings["SupportAggressiveDefensiveSelection"].AsInt32())
            {
                if (AggDefSupport(item, ref actionTarget))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool PositiveAggressiveDefensive(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (item == null) { return false; }
            if (Item.HasPendingUse) { return false; }

            if (AggressiveDefensiveSelection.PositiveAggressiveDefensive ==
                (AggressiveDefensiveSelection)_settings["AggressiveDefensiveSelection"].AsInt32())
            {
                if (AggDefAttack(item, ref actionTarget))
                {
                    return true;
                }
            }

            if (SupportAggressiveDefensiveSelection.PositiveAggressiveDefensive ==
               (SupportAggressiveDefensiveSelection)_settings["SupportAggressiveDefensiveSelection"].AsInt32())
            {
                if (AggDefSupport(item, ref actionTarget))
                {
                    return true;
                }
            }
            return false;
        }
        static bool MechEngieAttack(Item item, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Time.AONormalTime < LastTrimTime + MEAttackDelay) { return false; }

            Pet _attackPet = DynelManager.LocalPlayer.Pets.FirstOrDefault(c => c.Character != null &&
                     c.Type == PetType.Attack && (!petMEtrimmed[c.Type].Item1 ||
                     petMEtrimmed[c.Type].Item2 <= Time.AONormalTime));

            if (_attackPet != null)
            {
                //Chat.WriteLine($"Using {item.Name} on {_attackPet.Character.Name}.");
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _attackPet.Character;
                petMEtrimmed[PetType.Attack] = Tuple.Create(true, Time.AONormalTime + DelayBetweenFiveMinTrims);
                return true;
            }

            return false;
        }
        static bool MechEngieSupport(Item item, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Time.AONormalTime < LastTrimTime + MESupportDelay) { return false; }

            Pet _supportPet = DynelManager.LocalPlayer.Pets.FirstOrDefault(c => c.Character != null &&
                     c.Type == PetType.Support && (!petMEtrimmed[c.Type].Item1 ||
                     petMEtrimmed[c.Type].Item2 <= Time.AONormalTime));

            if (_supportPet != null)
            {
                //Chat.WriteLine($"Using {item.Name} on {_supportPet.Character.Name}.");
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _supportPet.Character;
                petMEtrimmed[PetType.Support] = Tuple.Create(true, Time.AONormalTime + DelayBetweenFiveMinTrims);
                return true;
            }

            return false;
        }

        static bool ElecEngieAttack(Item item, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Time.AONormalTime < LastTrimTime + EEAttackDelay) { return false; }

            Pet _attackPet = DynelManager.LocalPlayer.Pets.FirstOrDefault(c => c.Character != null &&
                     c.Type == PetType.Attack && (!petEEtrimmed[c.Type].Item1 ||
                     petEEtrimmed[c.Type].Item2 <= Time.AONormalTime));

            if (_attackPet != null)
            {
                //Chat.WriteLine($"Using {item.Name} on {_attackPet.Character.Name}.");
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _attackPet.Character;
                petEEtrimmed[PetType.Attack] = Tuple.Create(true, Time.AONormalTime + DelayBetweenFiveMinTrims);
                return true;
            }

            return false;
        }

        static bool ElecEngieSupport(Item item, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Time.AONormalTime < LastTrimTime + EESupportDelay) { return false; }

            Pet _supportPet = DynelManager.LocalPlayer.Pets.FirstOrDefault(c => c.Character != null &&
                     c.Type == PetType.Support && (!petEEtrimmed[c.Type].Item1 ||
                     petEEtrimmed[c.Type].Item2 <= Time.AONormalTime));

            if (_supportPet != null)
            {
                //Chat.WriteLine($"Using {item.Name} on {_supportPet.Character.Name}.");
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _supportPet.Character;
                petEEtrimmed[PetType.Support] = Tuple.Create(true, Time.AONormalTime + DelayBetweenFiveMinTrims);
                return true;
            }

            return false;
        }

        private static bool AggDefAttack(Item item, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Time.AONormalTime < LastTrimTime + AggDefAttackDelay) { return false; }

            Pet _attackPet = DynelManager.LocalPlayer.Pets
                .FirstOrDefault(c => c.Character != null && c.Type == PetType.Attack && !petTrimmedAggDef[c.Type]);

            if (_attackPet != null)
            {
                //Chat.WriteLine($"Using {item.Name} on {_attackPet.Character.Name}.");
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _attackPet.Character;
                petTrimmedAggDef[PetType.Attack] = true;
                return true;
            }

            return false;
        }

        private static bool AggDefSupport(Item item, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Time.AONormalTime < LastTrimTime + AggDefSupportDelay) { return false; }

            Pet _supportPet = DynelManager.LocalPlayer.Pets
                .FirstOrDefault(c => c.Character != null && c.Type == PetType.Support && !petTrimmedAggDef[c.Type]);

            if (_supportPet != null)
            {
                //Chat.WriteLine($"Using {item.Name} on {_supportPet.Character.Name}.");
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = _supportPet.Character;
                petTrimmedAggDef[PetType.Support] = true;
                return true;
            }

            return false;
        }
        public static void ResetTrimmers()
        {
            petTrimmedAggressive[PetType.Attack] = false;
            petTrimmedAggressive[PetType.Support] = false;
            petTrimmedAggDef[PetType.Attack] = false;
            petTrimmedAggDef[PetType.Support] = false;
        }
    }
}
