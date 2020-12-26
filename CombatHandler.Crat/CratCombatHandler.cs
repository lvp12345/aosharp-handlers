using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;
using System;
using System.Linq;

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
            RegisterSpellProcessor(RelevantNanos.PinkSlip, SingleTargetNuke, CombatActionPriority.Low);
            RegisterSpellProcessor(RelevantNanos.WorkplaceDepression, CratDebuff, CombatActionPriority.Low);

            RegisterSpellProcessor(RelevantNanos.MalaiseOfZeal, CratDebuff, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.WastefulArmMovements, CratDebuff, CombatActionPriority.High);

            _menu = new Menu("CombatHandler.Crat", "CombatHandler.Crat");
            _menu.AddItem(new MenuBool("UseDebuff", "Crat Debuffing", true));

            OptionPanel.AddMenu(_menu);
        }

        private bool CratDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            // Check if we are fighting and if debuffing is enabled
            if (fightingTarget == null || !_menu.GetBool("UseDebuff"))
                return false;

            //Check the remaining time on debuffs. On the enemy target
            foreach (Buff buff in fightingTarget.Buffs.AsEnumerable())
            { 
                //Chat.WriteLine(buff.Name);
                if (buff.Name == spell.Name && buff.RemainingTime > 1)
                    return false;
            }

            return true;
        }

        protected virtual bool StarfallPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PerkAction.Find(PerkHash.Combust, out PerkAction combust) && !combust.IsAvailable)
                return false;

            return TargetedDamagePerk(perkAction, fightingTarget, ref actionTarget);
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
            public const int MalaiseOfZeal = 275824;
            public const int WastefulArmMovements = 302150;
        }
    }
}
