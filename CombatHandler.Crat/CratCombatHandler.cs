using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;

namespace Desu
{
    public class CratCombatHandler : GenericCombatHandler
    {
        private Menu _menu;

        public CratCombatHandler()
        {
            //Perks
            RegisterPerkProcessor(PerkHash.DazzleWithLights, StarfallPerk);
            RegisterPerkProcessor(PerkHash.Combust, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.ThermalDetonation, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.Supernova, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.QuickShot, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.DoubleShot, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.Deadeye, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.Antitrust, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.NanoFeast, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.BotConfinement, TargetedDamagePerk);

            //Spells
            RegisterSpellProcessor(RelevantNanos.PinkSlip, SingleTargetNuke);
            RegisterSpellProcessor(RelevantNanos.WorkplaceDepression, SingleTargetNuke);

            _menu = new Menu("CombatHandler.Crat", "CombatHandler.Crat");
            //_menu.AddItem(new MenuBool("UseDebuff", "Crat Debuffing", true));

            OptionPanel.AddMenu(_menu);
        }

        protected virtual bool StarfallPerk(Perk perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Perk.Find(PerkHash.Combust, out Perk combust) && !combust.IsAvailable)
                return false;

            return TargetedDamagePerk(perk, fightingTarget, ref actionTarget);
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null)
                return false;

            return true;
        }


        private static class RelevantNanos
        {
            public const int PinkSlip = 273307;
            public const int WorkplaceDepression = 273631;
        }
    }
}
