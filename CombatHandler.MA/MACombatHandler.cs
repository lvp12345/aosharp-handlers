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
            settings.AddVariable("Heal", false);
            settings.AddVariable("OSHeal", false);

            settings.AddVariable("ShortDamage", false);

            RegisterSettingsWindow("Martial-Artist Handler", "MASettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistDebilitatingStrike, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistAbsoluteFist, LEProc, CombatActionPriority.Low);
            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.ReduceInertia, TeamBuffExcludeInnerSanctum);
            RegisterSpellProcessor(RelevantNanos.TeamCritBuffs, TeamBuff);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SingleTargetHealing).OrderByStackingOrder(), Healing, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TeamHealing).OrderByStackingOrder(), TeamHealing, CombatActionPriority.High);
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
            if (fightingTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ControlledDestructionBuff)) { return false; }

            if (DynelManager.LocalPlayer.NanoPercent < 30) { return false; }

            return true;
        }

        private bool ControlledDestructionWithShutdown(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("ShortDamage")) { return false; }

            if (fightingTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.ControlledDestructionBuff)) { return false; }

            if (DynelManager.LocalPlayer.NanoPercent < 30) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent < 100) { return false; }

            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) > 1) { return false; }

            return true;
        }

        protected bool TeamBuffSpecialCheck(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell)) { return false; }

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

        private bool MartialArtsTeamHealAttack(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingtarget == null) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Dimach)) { return false; }

            return FindMemberWithHealthBelow(85, ref actionTarget);

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

        private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Heal") || !CanCast(spell)) { return false; }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 80)
                    .Where(c => c.HealthPercent >= 50)
                    .ToList();

                if (dyingTeamMember.Count < 4)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal")) { return false; }

            if (!CanCast(spell)) { return false; }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam() && IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal"))
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 80)
                    .Where(c => c.HealthPercent >= 50)
                    .ToList();

                if (dyingTeamMember.Count >= 4)
                {
                    return false;
                }
                else
                {
                    return FindMemberWithHealthBelow(85, ref actionTarget);
                }
            }
            if (IsSettingEnabled("OSHeal") && !IsSettingEnabled("Heal"))
            {
                return FindPlayerWithHealthBelow(85, ref actionTarget);
            }
            else
            {
                return FindMemberWithHealthBelow(85, ref actionTarget);
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
