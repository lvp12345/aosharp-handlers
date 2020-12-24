using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;
using System.Collections.Generic;
using System.Linq;

namespace Desu
{
    class DocCombatHandler : GenericCombatHandler
    {
        private Menu _menu;

        private List<PerkHash> BattleGroupHeals = new List<PerkHash> {
            PerkHash.BattleGroupHeal1,
            PerkHash.BattleGroupHeal2,
            PerkHash.BattleGroupHeal3,
            PerkHash.BattleGroupHeal4,
        };
        public DocCombatHandler()
        {
            //Perks
            RegisterPerkProcessor(PerkHash.HostileTakeover, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.ChaoticAssumption, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.DazzleWithLights, StarfallPerk);
            RegisterPerkProcessor(PerkHash.Combust, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.ThermalDetonation, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.QuickShot, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.DoubleShot, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.Deadeye, TargetedDamagePerk);
            BattleGroupHeals.ForEach(p => RegisterPerkProcessor(p, MajorHealPerk));

            //Spells
            RegisterSpellProcessor(RelevantNanos.BODILY_INV, TeamHeal, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.IMPROVED_CH, SingleTargetHeal);
            RegisterSpellProcessor(RelevantNanos.IMPROVED_LC, TeamHeal, CombatActionPriority.Low);

            //This needs work 
            RegisterSpellProcessor(RelevantNanos.UBT, DebuffTarget);

            _menu = new Menu("CombatHandler.Doc", "CombatHandler.Doc");
            _menu.AddItem(new MenuBool("UseDebuff", "Doc Debuffing", true));
            OptionPanel.AddMenu(_menu);
        }

        private bool DebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            // Check if we are fighting and if debuffing is enabled
            if (fightingTarget == null || !_menu.GetBool("UseDebuff"))
                return false;

            //Check if the target has the ubt buff running
            foreach (Buff buff in fightingTarget.Buffs.AsEnumerable())
                if (buff.Name == spell.Name)
                    return false;

            //Check if you are low hp dont debuff
            if (DynelManager.LocalPlayer.HealthPercent <= 30)
            {
                //actionTarget.Target = DynelManager.LocalPlayer;
                return false;
            }

            //Check if we're in a team and someone is low hp , dont debuff
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent < 30)
                    .OrderByDescending(c => c.GetStat(Stat.NumFightingOpponents))
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    actionTarget.Target = dyingTeamMember;
                    return false;
                }
            }
            return true;
        }

        private bool MajorHealPerk(Perk perk, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            // Prioritize keeping ourself alive
            if (DynelManager.LocalPlayer.HealthPercent <= 30)
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent < 30)
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

        protected virtual bool StarfallPerk(Perk perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Perk.Find(PerkHash.Combust, out Perk combust) && !combust.IsAvailable)
                return false;

            return TargetedDamagePerk(perk, fightingTarget, ref actionTarget);
        }

        private bool TeamHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
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
        private bool SingleTargetHeal(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            actiontarget.Target = DynelManager.LocalPlayer;
            return DynelManager.LocalPlayer.MissingHealth > 3500;
        }
        private static class RelevantNanos
        {
            public const int OMNI_MED = 95709;
            public const int IMPROVED_LC = 275011;
            public const int IMPROVED_CH = 270747;
            public const int BODILY_INV = 223299;
            public const int UBT = 99577;
            public const int UBT_MONSTER = 301844;
            public const int UBT_HUMAN = 301843;
        }
    }
}
