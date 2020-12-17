using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Desu
{
    class EnfCombatHandler : GenericCombatHandler
    {
        private Menu _menu;

        public EnfCombatHandler()
        {
            List<PerkHash> RebuffPerks = new List<PerkHash>
            {
                PerkHash.ViolationBuffer,
            };

            //Perks
            RegisterPerkProcessor(PerkHash.Taunt, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.Charge, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.Headbutt, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.Hatred, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.GroinKick, TargetedDamagePerk);

            RegisterPerkProcessor(PerkHash.HammerAndAnvil, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.Pulverize, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.OverwhelmingMight, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.SeismicSmash, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.Deadeye, TargetedDamagePerk);

            RegisterPerkProcessor(PerkHash.BioRejuvenation, TeamHealPerk);
            RegisterPerkProcessor(PerkHash.BioRegrowth, TeamHealPerk);
            RegisterPerkProcessor(PerkHash.BioShield, SelfBuffPerk, CombatActionPriority.High);
            RegisterPerkProcessor(PerkHash.BioCocoon, SelfHealPerk);

            //LE Procs Support for these needs to re worked
            //RegisterPerkProcessor(PerkHash.ShieldOfTheOgre, SelfBuffPerk, CombatActionPriority.Medium);//Type1
            //RegisterPerkProcessor(PerkHash.ViolationBuffer, SelfBuffPerk, CombatActionPriority.Low);//Type2

            //Spells (Im not sure the spell lines are up to date to support the full line of SL mongos)
            RegisterSpellProcessor(RelevantNanos.MONGO_KRAKEN, MajorHpBuff, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.MongoBuff).OrderByStackingOrder(), AoeTaunt);
            RegisterSpellProcessor(RelevantNanos.ELEMENT_OF_MALICE, SingleTargetTaunt, CombatActionPriority.High);


            _menu = new Menu("CombatHandler.Enf", "CombatHandler.Enf");
            _menu.AddItem(new MenuBool("UseAOEMongo", "Use Mongo?", true));
            //Setting this to default to false for now, as I do not currently have the nano ( Will by EOD so expect a patch )
            _menu.AddItem(new MenuBool("UseSingleTaunt", "Use IMalice?", false));
            OptionPanel.AddMenu(_menu);
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_menu.GetBool("UseSingleTaunt") || !DynelManager.LocalPlayer.IsAttacking)
                return false;

            if (fightingTarget == null)
                return false;

            //If our target has a different target than us we need to make sure we taunt
            if (fightingTarget.FightingTarget != null && (fightingTarget.FightingTarget.Identity != DynelManager.LocalPlayer.Identity))
            {
                return true;
            }

            if (DynelManager.LocalPlayer.NanoPercent < 30)
                return false;

            return true;
        }

        private bool AoeTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!_menu.GetBool("UseAOEMongo"))
                return false;

            if (fightingTarget == null)
                return false;

            //If our target has a different target than us we need to make sure we taunt
            if (fightingTarget.FightingTarget != null && (fightingTarget.FightingTarget.Identity != DynelManager.LocalPlayer.Identity))
                return true;

            //Check if our team members are being attacked first
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => c.GetStat(Stat.NumFightingOpponents) > 0)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .OrderByDescending(c => c.GetStat(Stat.NumFightingOpponents))
                    .FirstOrDefault();
                return true;
            }

            //Check if we have tanking enabled & have more than one enemy
            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) < 2) 
            { 
                return false;
            }

            //Check if we still have the mongo hot 
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if ((buff.Name == spell.Name && buff.RemainingTime < 5))
                    return true;
            }

            //Make sure we have plenty of nano for spamming mongo
            if (DynelManager.LocalPlayer.NanoPercent < 30)
                return false;

            return false;
        }

        private bool MajorHpBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //Check if we have Kraken in our ncu at all times, if not we refresh it.
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable()) 
            {
                if (buff.Identity == spell.Identity)
                    return false;
            }

            if(DynelManager.LocalPlayer.NanoPercent < 30)
                return false;

            return true;
        }

        private bool SelfBuffPerk(Perk perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs.AsEnumerable())
            {
                if (buff.Name == perk.Name) 
                {
                    //Chat.WriteLine(buff.Name+" "+perk.Name);
                    return false;
                }
            }
            return true;
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
           public const int MONGO_KRAKEN = 273322;
           public const int MONGO_DEMOLISH = 270786;
           public const int ELEMENT_OF_MALICE = 275014;
        }
    }
}
