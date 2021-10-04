using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using CombatHandler.Generic;

namespace Desu
{
    public class MACombatHandler : GenericCombatHandler
    {
        public MACombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("HealTeammates", true);
            settings.AddVariable("UseShortDamageBuffNanoShutdown", false);
            RegisterSettingsWindow("Martial-Artist Handler", "MASettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistDebilitatingStrike, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistAbsoluteFist, LEProc, CombatActionPriority.Low);
            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.ReduceInertia, TeamBuffExcludeInnerSanctum);
            RegisterSpellProcessor(RelevantNanos.TeamCritBuffs, TeamBuff);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SingleTargetHealing).OrderByStackingOrder(), SingleTargetHeal, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TeamHealing).OrderByStackingOrder(), TeamHeal, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledDestructionBuff).Where(s => s.StackingOrder >= 19).OrderByStackingOrder(), ControlledDestructionNoShutdown);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledDestructionBuff).Where(s => s.StackingOrder < 19).OrderByStackingOrder(), ControlledDestructionWithShutdown);
            RegisterSpellProcessor(RelevantNanos.FistsOfTheWinterFlame, FistsOfTheWinterFlameNano);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.LimboMastery, GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), GenericBuffExcludeInnerSanctum);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.BrawlBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledRageBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RunspeedBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.StrengthBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtsBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).Where(s => s.Identity.Instance != 28879).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RiposteBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistBuff).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalIncreaseBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), GenericBuff);


            //Items
            RegisterItemProcessor(RelevantItems.TheWizdomOfHuzzum, RelevantItems.TheWizdomOfHuzzum, MartialArtsTeamHealAttack);

        }

        private bool ControlledDestructionNoShutdown(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ControlledDestructionBuff))
                return false;

            if (fightingTarget == null)
                return false;

            if (DynelManager.LocalPlayer.NanoPercent < 30)
                return false;

            return true;
        }

        protected bool TeamBuffSpecialCheck(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell))
            {
                return false;
            }

            if (SpellChecksPlayer(spell))
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => SpellChecksOther(spell, c))
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        private bool ControlledDestructionWithShutdown(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("UseShortDamageBuffNanoShutdown"))
                return false;

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ControlledDestructionBuff))
                return false;

            if (DynelManager.LocalPlayer.HealthPercent < 100)
                return false;

            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 1)
                return false;

            if (fightingTarget == null)
                return false;

            if (DynelManager.LocalPlayer.NanoPercent < 30)
                return false;

            return true;
        }

        private bool MartialArtsTeamHealAttack(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingtarget == null)
                return false;

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(GetSkillLockStat(item)))
                return false;

            int healingReceived = item.LowId == RelevantItems.TreeOfEnlightenment ? 290 : 1200;

            if (DynelManager.LocalPlayer.MissingHealth > healingReceived * 2)
                return true;

            if (IsSettingEnabled("HealTeammates"))
            {
                SimpleChar dyingTeammate = GetTeammatesInNeedOfHealing(healingReceived * 2)
                    .FirstOrDefault();

                if (dyingTeammate != null)
                    return true;
            }

            return false;
        }

        protected override bool ShouldUseSpecialAttack(SpecialAttack specialAttack)
        {
            return specialAttack != SpecialAttack.Dimach;
        }

        private bool FistsOfTheWinterFlameNano(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            actiontarget.ShouldSetTarget = false;
            return fightingtarget != null && fightingtarget.HealthPercent > 50;
        }

        private bool SingleTargetHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("HealTeammates"))
            {
                return false;
            }

            actionTarget.ShouldSetTarget = true;

            if (DynelManager.LocalPlayer.MissingHealth > 800) //TODO: Some kind of healing check to calc an optimal missing health value
            {
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            if (IsSettingEnabled("HealTeammates"))
            {
                SimpleChar dyingTeammate = GetTeammatesInNeedOfHealing(800)
                    .Where(target => target.IsInLineOfSight)
                    .FirstOrDefault(target => target.Position.DistanceFrom(DynelManager.LocalPlayer.Position) < 20); //TODO: make this dynamic when possible

                if (dyingTeammate != null)
                {
                    actionTarget.Target = dyingTeammate;
                    return true;
                }
            }

            return false;
        }

        private bool TeamHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("HealTeammates"))
            {
                return false;
            }

            actiontarget.ShouldSetTarget = false;

            if (DynelManager.LocalPlayer.MissingHealth > 1500) //TODO: Some kind of healing check to calc an optimal missing health value
                return true;

            if (IsSettingEnabled("HealTeammates"))
            {
                SimpleChar dyingTeammate = GetTeammatesInNeedOfHealing(1500)
                    .FirstOrDefault(target => target.Position.DistanceFrom(DynelManager.LocalPlayer.Position) < spell.AttackRange); //TODO: factor in nano range increase

                if (dyingTeammate != null)
                    return true;
            }

            return false;
        }

        private IEnumerable<SimpleChar> GetTeammatesInNeedOfHealing(int missingHealthThreshold)
        {
            return DynelManager.Characters
                .Where(c => c.IsAlive)
                .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                .Where(c => c.MissingHealth >= missingHealthThreshold);
        }

        private Stat GetSkillLockStat(Item item)
        {
            switch (item.HighId)
            {
                case RelevantItems.TheWizdomOfHuzzum:
                case RelevantItems.TreeOfEnlightenment:
                    return Stat.Dimach;
                default:
                    throw new Exception($"No skill lock stat defined for item id {item.HighId}");
            }
        }

        private static class RelevantNanos
        {
            public const int FistsOfTheWinterFlame = 269470;
            public const int LimboMastery = 28894;
            public const int ReduceInertia = 28903;
            public static int[] TeamCritBuffs = { 160574, 160575, 160576 };
        }

        private static class RelevantItems
        {
            public const int TheWizdomOfHuzzum = 303056;
            public const int TreeOfEnlightenment = 204607;
        }
    }
}
