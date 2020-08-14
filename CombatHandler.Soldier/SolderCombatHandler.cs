using AOSharp.Common.GameData;
using AOSharp.Core;
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
            RegisterPerkProcessor(PerkHash.ClipFever, DamagePerk);
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
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.TotalMirrorShield).OrderByStackingOrder(), AugmentedMirrorShieldMKV);
            RegisterSpellProcessor(RelevantNanos.AdrenalineRush, AdrenalineRush);
            RegisterSpellProcessor(RelevantNanos.Distinctvictim, SingleTargetTaunt);//TODO: Generate soldier taunt line to support lower ql taunt use

            _menu = new Menu("CombatHandler.Sold", "CombatHandler.Sold");
            _menu.AddItem(new MenuBool("useTaunt", "Use Taunt", true));
            OptionPanel.AddMenu(_menu);
        }

        private bool DmgBuffPerk(Perk perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking || fightingTarget == null)
                return false;
            return true;
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!_menu.GetBool("useTaunt") || !DynelManager.LocalPlayer.IsAttacking || fightingtarget == null || DynelManager.LocalPlayer.Nano < spell.Cost)
                return false;

            return true;
        }

        private bool AdrenalineRush(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking || fightingtarget == null)
                return false;

            if (DynelManager.LocalPlayer.HealthPercent <= 30 && fightingtarget.HealthPercent > 1)
                return true;

            return false;
        }

        private bool AugmentedMirrorShieldMKV(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking || fightingtarget == null)
                return false;

            if (DynelManager.LocalPlayer.IsAlive && fightingtarget.IsAlive)
                return true;

            return false;
        }

        private static class RelevantNanos
        {
            public const int AdrenalineRush = 301897;
            public const int Distinctvictim = 223205;
        }
    }
}
