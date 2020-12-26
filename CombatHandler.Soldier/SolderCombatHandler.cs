using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;
using System;

namespace Desu
{
    public class SoldCombathandler : GenericCombatHandler
    {
        private Menu _menu;
        public SoldCombathandler()
        {

            //DmgPerks
            RegisterPerkProcessor(PerkHash.SupressiveHorde, DmgBuffPerk);
            RegisterPerkProcessor(PerkHash.Energize, DmgBuffPerk);
            RegisterPerkProcessor(PerkHash.ReinforceSlugs, DmgBuffPerk);

            //Debuffs
            RegisterPerkProcessor(PerkHash.Tracer, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.TriangulateTarget, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.LaserPaintTarget, TargetedDamagePerk);

            //AI Perks
            RegisterPerkProcessor(PerkHash.LaserPaintTarget, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.Fuzz, DamagePerk);
            RegisterPerkProcessor(PerkHash.FireFrenzy, DamagePerk);
            RegisterPerkProcessor(PerkHash.Clipfever, DamagePerk);
            RegisterPerkProcessor(PerkHash.MuzzleOverload, DamagePerk);
            RegisterPerkProcessor(PerkHash.BotConfinement, DamagePerk);
            RegisterPerkProcessor(PerkHash.BotConfinement, DamagePerk);
            RegisterPerkProcessor(PerkHash.NanoFeast, DamagePerk);

            //Shadow Dmg
            RegisterPerkProcessor(PerkHash.WeaponBash, DamagePerk);
            RegisterPerkProcessor(PerkHash.NapalmSpray, DamagePerk);
            RegisterPerkProcessor(PerkHash.ContainedBurst, DamagePerk);
            RegisterPerkProcessor(PerkHash.PowerVolley, DamagePerk);
            RegisterPerkProcessor(PerkHash.PowerShock, DamagePerk);
            RegisterPerkProcessor(PerkHash.PowerBlast, DamagePerk);
            RegisterPerkProcessor(PerkHash.PowerCombo, DamagePerk);
            RegisterPerkProcessor(PerkHash.JarringBurst, DamagePerk);
            RegisterPerkProcessor(PerkHash.SolidSlug, DamagePerk);
            RegisterPerkProcessor(PerkHash.NeutroniumSlug, DamagePerk);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TotalMirrorShield).OrderByStackingOrder(), AugmentedMirrorShieldMKV);
            RegisterSpellProcessor(RelevantNanos.AdrenalineRush, AdrenalineRush);
            RegisterSpellProcessor(RelevantNanos.Distinctvictim, SingleTargetTaunt);//TODO: Generate soldier taunt line to support lower ql taunt use

            //Items
            RegisterItemProcessor(RelevantItems.DreadlochEnduranceBoosterNanomageEdition, RelevantItems.DreadlochEnduranceBoosterNanomageEdition, EnduranceBooster, CombatActionPriority.High);

            _menu = new Menu("CombatHandler.Sold", "CombatHandler.Sold");
            _menu.AddItem(new MenuBool("useTaunt", "Use Taunt", true));
            OptionPanel.AddMenu(_menu);
        }

        private bool EnduranceBooster(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            // don't use if skill is locked (we will add this dynamically later)
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Strength))
                return false;

            // don't use if we're above 40%
            if (DynelManager.LocalPlayer.HealthPercent > 40)
                return false;

            // don't use if nothing is fighting us
            //if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0)
            //    return false;

            // don't use if we have another major absorb running
            // we could check remaining absorb stat to be slightly more effective
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon))
                return false;

            return true;
        }

        private bool DmgBuffPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking || fightingTarget == null)
                return false;
            return true;
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_menu.GetBool("useTaunt") || !DynelManager.LocalPlayer.IsAttacking || fightingTarget == null || DynelManager.LocalPlayer.Nano < spell.Cost)
                return false;

            return true;
        }

        private bool AdrenalineRush(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //if (!DynelManager.LocalPlayer.IsAttacking || fightingtarget == null)
            //    return false;

            if (DynelManager.LocalPlayer.HealthPercent <= 30)
                return true;

            return false;
        }

        private bool AugmentedMirrorShieldMKV(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking || fightingtarget == null)
                return false;

            if (DynelManager.LocalPlayer.HealthPercent <= 25 && spell.IsReady)
                return true;

            return false;
        }

        private static class RelevantNanos
        {
            public const int AdrenalineRush = 301897;
            public const int Distinctvictim = 223205;
        }

        private static class RelevantItems
        {
            public const int DreadlochEnduranceBooster = 267168;
            public const int DreadlochEnduranceBoosterNanomageEdition = 267167;
        }
    }
}
