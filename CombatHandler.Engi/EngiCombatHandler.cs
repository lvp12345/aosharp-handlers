using AOSharp.Common.GameData;
using AOSharp.Core;
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
        private Menu _menu;

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
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.PistolBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.GrenadeBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.InitiativeBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.ShadowlandReflectBase).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.SpecialAttackAbsorberBase).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.EngineerSpecialAttackAbsorber).OrderByStackingOrder(), GenericBuff);




            _menu = new Menu("CombatHandler.Engi", "CombatHandler.Engi");
            _menu.AddItem(new MenuBool("BuffPets", "Buff Pets?", true));
            //Setting this to default to false for now, as I do not currently have the nano ( Will by EOD so expect a patch )
            _menu.AddItem(new MenuBool("UseSingleTaunt", "Use IMalice?", false));
            OptionPanel.AddMenu(_menu);
        }



        private bool SelfHealPerk(Perk perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
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

        private bool TeamHealPerk(Perk perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
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
        }
    }
}
