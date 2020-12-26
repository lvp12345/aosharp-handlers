using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CombatHandler.Engi
{
    class EngiCombatHandler : GenericCombatHandler
    {
        private const float PostZonePetCheckBuffer = 5;
        private Menu _menu;
        private double _lastZonedTime = 0;

        public EngiCombatHandler()
        {
            //Perks
            RegisterPerkProcessor(PerkHash.BioRejuvenation, TeamHealPerk);
            RegisterPerkProcessor(PerkHash.BioRegrowth, TeamHealPerk);
            //RegisterPerkProcessor(PerkHash.BioShield, SelfBuffPerk);
            RegisterPerkProcessor(PerkHash.BioCocoon, SelfHealPerk);

            RegisterPerkProcessor(PerkHash.Energize, DamagePerk);
            RegisterPerkProcessor(PerkHash.PowerVolley, DamagePerk);
            RegisterPerkProcessor(PerkHash.PowerShock, DamagePerk);
            RegisterPerkProcessor(PerkHash.PowerBlast, DamagePerk);
            RegisterPerkProcessor(PerkHash.PowerCombo, DamagePerk);
            RegisterPerkProcessor(PerkHash.LegShot, DamagePerk);
            RegisterPerkProcessor(PerkHash.EasyShot, DamagePerk);
            RegisterPerkProcessor(PerkHash.PointBlank, DamagePerk);
            RegisterPerkProcessor(PerkHash.QuickShot, DamagePerk);
            RegisterPerkProcessor(PerkHash.DoubleShot, DamagePerk);
            RegisterPerkProcessor(PerkHash.Deadeye, DamagePerk);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.CompositeAttribute, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeNano, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeUtility, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeRanged, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.CompositeRangedSpec, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.SympatheticReactiveCocoon, GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.GrenadeBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadowlandReflectBase).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SpecialAttackAbsorberBase).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerSpecialAttackAbsorber).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(), GenericBuff);

            //Pet Spawners
            RegisterSpellProcessor(RelevantNanos.Pets.Where(x => x.Value.PetType == PetType.Attack).Select(x => x.Key).ToArray(), PetSpawner);
            RegisterSpellProcessor(RelevantNanos.Pets.Where(x => x.Value.PetType == PetType.Support).Select(x => x.Key).ToArray(), PetSpawner);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetShortTermDamageBuffs).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PetDefensiveNanos).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MPPetInitiativeBuffs).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EngineerMiniaturization).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(), PetTargetBuff);
            RegisterSpellProcessor(RelevantNanos.ShieldOfTheObedientServant, ShieldOfTheObedientServant);

            //Pet Shells
            foreach(int shellId in RelevantNanos.Pets.Values.Select(x => x.ShellId))
                RegisterItemProcessor(shellId, shellId, PetSpawnerItem);

            _menu = new Menu("CombatHandler.Engi", "CombatHandler.Engi");
            _menu.AddItem(new MenuBool("SpawnPets", "Spawn Pets?", true));
            _menu.AddItem(new MenuBool("BuffPets", "Buff Pets?", true));
            OptionPanel.AddMenu(_menu);

            Game.TeleportEnded += OnZoned;
        }

        private void OnZoned(object s, EventArgs e)
        {
            _lastZonedTime = Time.NormalTime;
        }

        protected bool PetSpawner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_menu.GetBool("SpawnPets"))
                return false;

            if (Time.NormalTime < _lastZonedTime + PostZonePetCheckBuffer)
                return false;

            //Do not attempt any pet spawns if we have a pet not loaded as it could be the pet we think we need to replace.
            if (DynelManager.LocalPlayer.Pets.Any(x => x.Type == PetType.Unknown))
                return false;

            if (!RelevantNanos.Pets.ContainsKey(spell.Identity.Instance))
                return false;

            //Ignore spell if we already have this type of pet out
            if (DynelManager.LocalPlayer.Pets.Any(x => x.Type == RelevantNanos.Pets[spell.Identity.Instance].PetType))
                return false;

            //Ignore spell if we already have the shell in our inventory
            if (Inventory.Find(RelevantNanos.Pets[spell.Identity.Instance].ShellId, out Item shell))
                return false;

            actionTarget.ShouldSetTarget = false;
            return true;
        }

        protected virtual bool PetSpawnerItem(Item item, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_menu.GetBool("SpawnPets"))
                return false;

            if (Time.NormalTime < _lastZonedTime + PostZonePetCheckBuffer)
                return false;

            if (!RelevantNanos.Pets.Values.Any(x => (x.ShellId == item.LowId || x.ShellId == item.HighId) && !DynelManager.LocalPlayer.Pets.Any(p => p.Type == x.PetType)))
                return false;

            actionTarget.ShouldSetTarget = false;
            return true;
        }

        protected bool PetTargetBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_menu.GetBool("BuffPets"))
                return false;

            if (Time.NormalTime < _lastZonedTime + PostZonePetCheckBuffer)
                return false;

            bool petsNeedBuff = false;

            foreach (Pet pet in DynelManager.LocalPlayer.Pets.Where(x => x.Character != null && (x.Type == PetType.Attack || x.Type == PetType.Support)))
            {
                if (pet.Character.Buffs.Find(spell.Nanoline, out Buff buff))
                {
                    //Don't cast if weaker than existing
                    if (spell.StackingOrder < buff.StackingOrder)
                        continue;

                    //Don't cast if greater than 10% time remaining
                    if (spell.Nanoline == buff.Nanoline && buff.RemainingTime / buff.TotalTime > 0.1)
                        continue;
                }

                actionTarget.Target = pet.Character;
                petsNeedBuff = true;
                break;
            }

            if (!petsNeedBuff)
                return false;

            actionTarget.ShouldSetTarget = true;
            return true;
        }

        protected bool ShieldOfTheObedientServant(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_menu.GetBool("BuffPets"))
                return false;

            if (!DynelManager.LocalPlayer.Pets.Where(x => x.Character != null)
                                            .Where(x => x.Type == PetType.Attack || x.Type == PetType.Support)
                                            .Any(x => !x.Character.Buffs.Find(spell.Identity.Instance == 270790 ? 285699 : 285698, out _)))
                return false;

            actionTarget.ShouldSetTarget = false;
            return true;
        }

        private bool SelfHealPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking)
                return false;

            // Prioritize keeping ourself alive
            if (DynelManager.LocalPlayer.HealthPercent <= 35)//We should consider making this a slider value in the options
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }
            return false;
        }

        private bool TeamHealPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            if (!DynelManager.LocalPlayer.IsAttacking)
                return false;

            // Prioritize keeping ourself alive
            if (DynelManager.LocalPlayer.HealthPercent <= 60)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 60)
                    .OrderByDescending(c => c.GetStat(Stat.NumFightingOpponents))
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    actionTarget.Target = dyingTeamMember;
                    return true;
                }
            }
            return false;
        }

        private static class RelevantNanos
        {
            public const int CompositeAttribute = 223372;
            public const int CompositeNano = 223380;
            public const int CompositeUtility = 287046;
            public const int CompositeRanged = 223348;
            public const int CompositeRangedSpec = 223364;
            public const int SympatheticReactiveCocoon = 154550;
            public static int[] ShieldOfTheObedientServant = { 270790, 202260 };

            public static Dictionary<int, PetSpellData> Pets = new Dictionary<int, PetSpellData>
            { 
                { 223323, new PetSpellData(217994, PetType.Attack) },
                { 275815, new PetSpellData(275816, PetType.Support) }
            };

            //Doggos 275815, 223337, 223335, 223333, 223331, 223329, 223327, 301855, 223325 
        }

        private class PetSpellData
        {
            public int ShellId;
            public PetType PetType;

            public PetSpellData(int shellId, PetType petType)
            {
                ShellId = shellId;
                PetType = petType;
            }
        }
    }
}
