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
            settings.AddVariable("HealthDrain", false);

            settings.AddVariable("RKNanoDrain", false);
            settings.AddVariable("SLNanoDrain", false);

            settings.AddVariable("Heal", true);

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

            //Heals
            RegisterSpellProcessor(RelevantNanos.Heal, Healing); // Self
            RegisterSpellProcessor(RelevantNanos.TeamHeal, TeamHealing); // Team
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DrainHeal).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDrain_LineA).OrderByStackingOrder(), RKNanoDrain);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SLNanopointDrain).OrderByStackingOrder(), SLNanoDrain);
            RegisterSpellProcessor(RelevantNanos.HealthDrain, HealthDrain);

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
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Deprive).OrderByStackingOrder(), DepriveDrain);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Ransack).OrderByStackingOrder(), RansackDrain);

            //AC Drains/Debuffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderDebuffACNanos).OrderByStackingOrder(), TraderACDrain);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderACTransferTargetDebuff_Draw).OrderByStackingOrder(), TraderACDrain);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderACTransferTargetDebuff_Siphon).OrderByStackingOrder(), TraderACDrain);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DebuffNanoACHeavy).OrderByStackingOrder(), TraderACDrain);

        }

        protected override void OnUpdate(float deltaTime)
        {
            if (settings["PurpleHeart"].AsBool() && settings["Sacrifice"].AsBool())
            {
                settings["PurpleHeart"] = false;
                settings["Sacrifice"] = false;

                Chat.WriteLine("Only activate one Perk option.");
            }

            if (settings["RKNanoDrain"].AsBool() && settings["SLNanoDrain"].AsBool())
            {
                settings["RKNanoDrain"] = false;
                settings["SLNanoDrain"] = false;

                Chat.WriteLine("Only activate one Drain option.");
            }

            base.OnUpdate(deltaTime);
        }

        private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Heal") || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => Team.Members
                        .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
                            .Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 85 && c.HealthPercent >= 50)
                    .ToList();

                if (dyingTeamMember.Count < 4) { return false; }
            }

            return FindMemberWithHealthBelow(85, ref actionTarget);
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Heal")) { return false; }

            if (!CanCast(spell)) { return false; }

            // Try to keep our teammates alive if we're in a team
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                List<SimpleChar> dyingTeamMember = DynelManager.Characters
                    .Where(c => Team.Members
                        .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
                            .Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 85 && c.HealthPercent >= 50)
                    .ToList();

                if (dyingTeamMember.Count >= 4) { return false; }
            }

            return FindMemberWithHealthBelow(85, ref actionTarget);
        }

        private bool PurpleHeart(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("PurpleHeart")) { return false; }

            if (IsSettingEnabled("PurpleHeart") && IsSettingEnabled("Sacrifice")) { return false; }

            return PerkCondtionProcessors.HealPerk(perk, fightingTarget, ref actionTarget);
        }
        private bool Sacrifice(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Sacrifice")) { return false; }

            if (IsSettingEnabled("Sacrifice") && IsSettingEnabled("PurpleHeart")) { return false; }

            return PerkCondtionProcessors.DamagePerk(perk, fightingTarget, ref actionTarget);
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
                        //.Where(c => !c.Buffs.Contains(NanoLine.MajorEvasionBuffs))
                        .Where(c => SpellChecksOther(spell, spell.Nanoline, c))
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

        private bool SLNanoDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("SLNanoDrain", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool HealthDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("HealthDrain", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool RKNanoDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("RKNanoDrain", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool MyEnemy(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //if (!IsSettingEnabled("MyEnemy") || fightingTarget == null || fightingTarget.FightingTarget == DynelManager.LocalPlayer) { return false; }

            return ToggledDebuffTarget("MyEnemy", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool GrandTheftHumidity(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //if (!IsSettingEnabled("GTH") || fightingTarget == null) { return false; }

            return ToggledDebuffTarget("GTH", spell, spell.Nanoline, fightingTarget, ref actionTarget);
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

            //if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.TraderSkillTransferCasterBuff_Ransack)) { return false; }

            //if (fightingTarget.Buffs.Contains(NanoLine.TraderSkillTransferTargetDebuff_Ransack)) { return false; }

            //return ToggledDebuff("RansackDrain", spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);

            //return ToggledDebuffTarget("RansackDrain", spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);

            if (DynelManager.LocalPlayer.Level >= 150)
            {
                return ToggledDebuffTarget("RansackDrain", spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);
            }
            else
            {
                if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Ransack, out Buff buff))
                {
                    if (spell.StackingOrder <= buff.StackingOrder)
                    {
                        if (buff.RemainingTime > 150) { return false; }

                        if (!CanCast(spell) || fightingTarget == null) { return false; }

                        return true;
                    }
                }

                return ToggledDebuffTarget("RansackDrain", spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);
            }
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

            //if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.TraderSkillTransferCasterBuff_Deprive)) { return false; }

            //if (fightingTarget.Buffs.Contains(NanoLine.TraderSkillTransferTargetDebuff_Deprive)) { return false; }

            //return ToggledDebuff("DepriveDrain", spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);

            //return ToggledDebuffTarget("DepriveDrain", spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);


            if (DynelManager.LocalPlayer.Level >= 150)
            {
                return ToggledDebuffTarget("DepriveDrain", spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);
            }
            else
            {
                if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Deprive, out Buff buff))
                {
                    if (spell.StackingOrder <= buff.StackingOrder)
                    {
                        if (buff.RemainingTime > 150) { return false; }

                        if (!CanCast(spell) || fightingTarget == null) { return false; }

                        return true;
                    }
                }

                return ToggledDebuffTarget("DepriveDrain", spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);
            }
        }

        private bool DamageDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            //if (!IsSettingEnabled("DamageDrain") || fightingTarget == null) { return false; }

            //if (!DynelManager.LocalPlayer.Buffs.Find(NanoLine.DamageBuffs_LineA, out Buff buff))
            //{
            //    if (fightingTarget.Buffs.Contains(NanoLine.DamageBuffs_LineA))
            //    {
            //        actionTarget.ShouldSetTarget = true;
            //        return true;
            //    }
            //}

            //return ToggledDebuff("DamageDrain", spell, spell.Nanoline, fightingTarget, ref actionTarget);
            return ToggledDebuffTarget("DamageDrain", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool AAODrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AAODrain") || fightingTarget == null) { return false; }

            //if (fightingTarget.Buffs.Contains(NanoLine.TraderNanoTheft2)) { return false; }

            //if (!DynelManager.LocalPlayer.Buffs.Find(NanoLine.AAOBuffs, out Buff buff))
            //{
            //    if (fightingTarget.Buffs.Contains(NanoLine.TraderNanoTheft1))
            //    {
            //        actionTarget.ShouldSetTarget = true;
            //        return true;
            //    }
            //}
            return ToggledDebuffTarget("AAODrain", spell, NanoLine.TraderNanoTheft1, fightingTarget, ref actionTarget);
            //return ToggledDebuff("AAODrain", spell, NanoLine.TraderNanoTheft1, fightingTarget, ref actionTarget);
        }

        private bool AADDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AADDrain") || fightingTarget == null) { return false; }

            if (fightingTarget.Buffs.Contains(NanoLine.TraderNanoTheft2)) { return false; }

            //if (!DynelManager.LocalPlayer.Buffs.Find(NanoLine.AADBuffs, out Buff buff))
            //{
            //    if (fightingTarget.Buffs.Contains(NanoLine.TraderNanoTheft2))
            //    {
            //        actionTarget.ShouldSetTarget = true;
            //        return true;
            //    }
            //}

            return ToggledDebuffTarget("AADDrain", spell, NanoLine.TraderNanoTheft2, fightingTarget, ref actionTarget);
            //return ToggledDebuff("AADDrain", spell, NanoLine.TraderNanoTheft2, fightingTarget, ref actionTarget);
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
            //if (!IsSettingEnabled("ACDrains") || fightingTarget == null) { return false; }

            //if (!DynelManager.LocalPlayer.Buffs.Find(spell.Nanoline, out Buff buff))
            //{
            //    if (fightingTarget.Buffs.Contains(spell.Nanoline))
            //    {
            //        actionTarget.ShouldSetTarget = true;
            //        return true;
            //    }
            //}

            return ToggledDebuffTarget("ACDrains", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        //private bool ToggledDebuff(string settingName, Spell spell, NanoLine spellNanoLine , SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (!IsSettingEnabled(settingName) || fightingTarget == null) { return false; }

        //    return !fightingTarget.Buffs
        //        .Where(buff => buff.Nanoline == spellNanoLine) //Same nanoline as the spell nanoline
        //        .Where(buff => buff.RemainingTime > 3) //Remaining time on buff > 1 second
        //        .Any(); ;
        //}

        private static class RelevantNanos
        {
            public const int QuantumUncertanity = 30745;
            public const int ImprovedQuantumUncertanity = 270808;
            public const int UnstoppableKiller = 275846;
            public const int DivestDamage = 273407;
            public const int UmbralWranglerPremium = 235291;
            public const int MyEnemiesEnemyIsMyFriend = 270714;
            public static int[] GrandThefts = { 269842, 280050 };
            public static int[] HealthDrain = { 270357, 77195, 76478, 76475, 76487, 76481,
                76484, 76491, 76494, 76499, 76571, 76503, 76651, 76614, 76656,
                76653, 76679, 76681, 76684, 76686, 76691, 76688, 76717, 76715,
                76720, 76722, 76724, 76727, 76729, 76732, 76742};
            public static int[] Heal = { 273410, 252155, 121496, 121500, 121501, 121499,
                121502, 121495, 121492, 121506, 121494, 121493, 121504, 121498, 121503,
                76653, 76679, 76681, 76684, 76686, 76691, 76688, 76717, 76715,
                121497, 121505};
            public static int[] TeamHeal = { 118245, 118230, 118232, 118231, 118235, 118233,
                118234, 118238, 118236, 118237, 118241, 118239, 118240, 118243, 118244,
                118242, 43374};
        }
    }
}
