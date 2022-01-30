using AOSharp.Common.GameData;
using AOSharp.Core;
using CombatHandler.Generic;
using System.Linq;
using System.Collections.Generic;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using System;

namespace Desu
{
    public class TraderCombatHandler : GenericCombatHandler
    {
        public TraderCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("DamageDrain", true);

            settings.AddVariable("AAODrain", true);
            settings.AddVariable("AADDrain", true);

            settings.AddVariable("MyEnemy", true);

            settings.AddVariable("RansackDrain", true);
            settings.AddVariable("DepriveDrain", true);

            settings.AddVariable("ACDrains", true);

            settings.AddVariable("GTH", true);

            settings.AddVariable("Sacrifice", false);
            settings.AddVariable("PurpleHeart", false);

            settings.AddVariable("NanoHealTeam", false);
            settings.AddVariable("EvadesTeam", false);

            RegisterSettingsWindow("Trader Handler", "TraderSettingsView.xml");

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcTraderRigidLiquidation, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcTraderDebtCollection, LEProc);

            RegisterPerkProcessor(PerkHash.PurpleHeart, PurpleHeart);
            RegisterPerkProcessor(PerkHash.Sacrifice, Sacrifice);

            //Self Buffs
            RegisterSpellProcessor(RelevantNanos.ImprovedQuantumUncertanity, EvadesTeam);
            RegisterSpellProcessor(RelevantNanos.UnstoppableKiller, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.UmbralWranglerPremium, GenericBuff);

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.QuantumUncertanity, EvadesTeam);

            //Team Nano heal (Rouse Outfit nanoline)
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoPointHeals).OrderByStackingOrder(), TeamNanoHeal);

            //GTH/Your Enemy Drains
            RegisterSpellProcessor(RelevantNanos.GrandThefts, GrandTheftHumidity);
            RegisterSpellProcessor(RelevantNanos.MyEnemiesEnemyIsMyFriend, MyEnemy);

            //AAO/AAD/Damage Drains
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderAADDrain).OrderByStackingOrder(), AADDrain);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderAAODrain).OrderByStackingOrder(), AAODrain);
            RegisterSpellProcessor(RelevantNanos.DivestDamage, DamageDrain);

            //Deprive/Ransack Drains
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Deprive), DepriveDrain);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Ransack), RansackDrain);

            //AC Drains/Debuffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderDebuffACNanos).OrderByStackingOrder(), TraderACDrain);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderACTransferTargetDebuff_Draw).OrderByStackingOrder(), TraderACDrain);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderACTransferTargetDebuff_Siphon).OrderByStackingOrder(), TraderACDrain);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DebuffNanoACHeavy).OrderByStackingOrder(), TraderACDrain);

        }

        private bool PurpleHeart(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {

            if (!IsSettingEnabled("PurpleHeart")) { return false; }

            if (IsSettingEnabled("PurpleHeart") && IsSettingEnabled("Sacrifice")) { return false; }

            return PerkCondtionProcessors.HealPerk(perk, fightingTarget, ref actionTarget);
        }

        protected bool NanoHealTeam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (IsSettingEnabled("EvadesTeam"))
            {
                if (fightingTarget != null || !CanCast(spell)) { return false; }

                if (DynelManager.LocalPlayer.IsInTeam())
                {
                    SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                        .Where(c => !c.Buffs.Contains(NanoLine.MajorEvasionBuffs))
                        .Where(c => SpellChecksOther(spell, c))
                        .FirstOrDefault();

                    if (teamMemberWithoutBuff != null)
                    {
                        actionTarget.Target = teamMemberWithoutBuff;
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool EvadesTeam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (IsSettingEnabled("EvadesTeam"))
            {
                if (fightingTarget != null || !CanCast(spell)) { return false; }

                if (DynelManager.LocalPlayer.IsInTeam())
                {
                    SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                        .Where(c => !c.Buffs.Contains(NanoLine.MajorEvasionBuffs))
                        .Where(c => SpellChecksOther(spell, c))
                        .FirstOrDefault();

                    if (teamMemberWithoutBuff != null)
                    {
                        actionTarget.Target = teamMemberWithoutBuff;
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool Sacrifice(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Sacrifice")) { return false; }

            if (IsSettingEnabled("Sacrifice") && IsSettingEnabled("PurpleHeart")) { return false; }

            return PerkCondtionProcessors.DamagePerk(perk, fightingTarget, ref actionTarget);
        }

        private bool MyEnemy(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("MyEnemy") || fightingTarget == null || fightingTarget.FightingTarget == DynelManager.LocalPlayer) { return false; }

            return true;
        }

        private bool GrandTheftHumidity(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("GTH") || fightingTarget == null) { return false; }

            return true;
        }

        private bool RansackDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //if (!IsSettingEnabled("RansackDrain") || fightingTarget == null) { return false; }

            //if (!DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Ransack, out Buff buff))
            //{
            //    if (fightingTarget.Buffs.Contains(NanoLine.TraderSkillTransferTargetDebuff_Ransack))
            //    {
            //        actionTarget.ShouldSetTarget = true;
            //        return true;
            //    }
            //}

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.TraderSkillTransferCasterBuff_Ransack)) { return false; }

            return ToggledDebuff("RansackDrain", spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);
        }

        private bool DepriveDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //if (!IsSettingEnabled("DepriveDrain") || fightingTarget == null) { return false; }

            //if (!DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Deprive, out Buff buff))
            //{
            //    if (fightingTarget.Buffs.Contains(NanoLine.TraderSkillTransferTargetDebuff_Deprive))
            //    {
            //        actionTarget.ShouldSetTarget = true;
            //        return true;
            //    }
            //}

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.TraderSkillTransferCasterBuff_Deprive)) { return false; }

            return ToggledDebuff("DepriveDrain", spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);
        }

        private bool DamageDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("DamageDrain") || fightingTarget == null) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Find(NanoLine.DamageBuffs_LineA, out Buff buff))
            {
                if (fightingTarget.Buffs.Contains(NanoLine.DamageBuffs_LineA))
                {
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return ToggledDebuff("DamageDrain", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool AAODrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AAODrain") || fightingTarget == null) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Find(NanoLine.AAOBuffs, out Buff buff))
            {
                if (fightingTarget.Buffs.Contains(NanoLine.TraderNanoTheft1))
                {
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return ToggledDebuff("AAODrain", spell, NanoLine.TraderNanoTheft1, fightingTarget, ref actionTarget);
        }

        private bool AADDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AADDrain") || fightingTarget == null) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Find(NanoLine.AADBuffs, out Buff buff))
            {
                if (fightingTarget.Buffs.Contains(NanoLine.TraderNanoTheft2))
                {
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return ToggledDebuff("AADDrain", spell, NanoLine.TraderNanoTheft2, fightingTarget, ref actionTarget);
        }

        private bool TeamNanoHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.NanoPointHeals)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar lowNanoTeamMember = DynelManager.Characters
                    .Where(c => Team.Members
                        .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
                            .Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.NanoPercent <= 80)
                    .FirstOrDefault();

                if (lowNanoTeamMember != null)
                {
                    actionTarget.Target = lowNanoTeamMember;
                    return true;
                }
            }

            return false;
        }

        private bool TraderACDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("ACDrains") || fightingTarget == null) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Find(spell.Nanoline, out Buff buff))
            {
                if (fightingTarget.Buffs.Contains(spell.Nanoline))
                {
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return ToggledDebuff("ACDrains", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool ToggledDebuff(string settingName, Spell spell, NanoLine spellNanoLine , SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled(settingName) || fightingTarget == null) { return false; }

            return !fightingTarget.Buffs
                .Where(buff => buff.Nanoline == spellNanoLine) //Same nanoline as the spell nanoline
                .Where(buff => buff.RemainingTime > 3) //Remaining time on buff > 1 second
                .Any(); ;
        }

        private static class RelevantNanos
        {
            public const int QuantumUncertanity = 30745;
            public const int ImprovedQuantumUncertanity = 270808;
            public const int UnstoppableKiller = 275846;
            public const int DivestDamage = 273407;
            public const int UmbralWranglerPremium = 235291;
            public const int MyEnemiesEnemyIsMyFriend = 270714;
            public static int[] GrandThefts = { 269842, 280050 };
            //public static Dictionary<NanoLine, NanoLine> DebuffToDrainLine = new Dictionary<NanoLine, NanoLine>()
            //{
            //    {NanoLine.TraderAADDrain, NanoLine.TraderNanoTheft2},
            //    {NanoLine.TraderAAODrain, NanoLine.TraderNanoTheft1},
            //    {NanoLine.NanoDrain_LineB, NanoLine.NanoOverTime_LineB}
            //};
        }
    }
}
