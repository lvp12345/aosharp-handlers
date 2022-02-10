using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using CombatHandler.Generic;

namespace Desu
{
    public class AdvCombatHandler : GenericCombatHandler
    {
        public AdvCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("Heal", true);
            settings.AddVariable("OSHeal", false);

            settings.AddVariable("DragonMorph", false);
            settings.AddVariable("LeetMorph", false);
            settings.AddVariable("SaberMorph", false);
            settings.AddVariable("WolfMorph", false);

            settings.AddVariable("ArmorBuff", false);

            //settings.AddVariable("Heal", true); // Morph leet and crit?
            //settings.AddVariable("Heal", true); // Morph sabre and damage?
            //settings.AddVariable("OSHeal", false); // Morph healing?

            RegisterSettingsWindow("Adventurer Handler", "AdvSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcAdventurerMacheteFlurry, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerCombustion, LEProc);

            //Spells
            RegisterSpellProcessor(RelevantNanos.HEALS, Healing, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CompleteHealingLine).OrderByStackingOrder(), CompleteHealing, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TeamHealing).OrderByStackingOrder(), TeamHealing, CombatActionPriority.High);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.General1HEdgedBuff).OrderByStackingOrder(), MeleeBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), RangedBuff);
            //RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShieldUpgrades).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MultiwieldBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(RelevantNanos.ArmorBuffs, ArmorBuff);

            //Morphs
            RegisterSpellProcessor(RelevantNanos.DragonMorph, DragonMorph);
            RegisterSpellProcessor(RelevantNanos.LeetMorph, LeetMorph);
            RegisterSpellProcessor(RelevantNanos.WolfMorph, WolfMorph);
            RegisterSpellProcessor(RelevantNanos.SaberMorph, SaberMorph);

            RegisterSpellProcessor(RelevantNanos.DragonScales, DragonScales);
            RegisterSpellProcessor(RelevantNanos.LeetCrit, LeetCrit);
            RegisterSpellProcessor(RelevantNanos.WolfAgility, WolfAgility);
            RegisterSpellProcessor(RelevantNanos.SaberDamage, SaberDamage);

            //Items
            //RegisterItemProcessor(RelevantItems.TheWizdomOfHuzzum, RelevantItems.TheWizdomOfHuzzum, MartialArtsTeamHealAttack);

        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            if (settings["DragonMorph"].AsBool() && settings["LeetMorph"].AsBool())
            {
                settings["DragonMorph"] = false;
                settings["LeetMorph"] = false;

                Chat.WriteLine("Only activate one Morph option.");
            }
            if (settings["DragonMorph"].AsBool() && settings["SaberMorph"].AsBool())
            {
                settings["DragonMorph"] = false;
                settings["SaberMorph"] = false;

                Chat.WriteLine("Only activate one Morph option.");
            }
            if (settings["DragonMorph"].AsBool() && settings["WolfMorph"].AsBool())
            {
                settings["DragonMorph"] = false;
                settings["WolfMorph"] = false;

                Chat.WriteLine("Only activate one Morph option.");
            }
            if (settings["SaberMorph"].AsBool() && settings["LeetMorph"].AsBool())
            {
                settings["SaberMorph"] = false;
                settings["LeetMorph"] = false;

                Chat.WriteLine("Only activate one Morph option.");
            }
            if (settings["SaberMorph"].AsBool() && settings["WolfMorph"].AsBool())
            {
                settings["SaberMorph"] = false;
                settings["WolfMorph"] = false;

                Chat.WriteLine("Only activate one Morph option.");
            }
            if (settings["LeetMorph"].AsBool() && settings["WolfMorph"].AsBool())
            {
                settings["LeetMorph"] = false;
                settings["WolfMorph"] = false;

                Chat.WriteLine("Only activate one Morph option.");
            }

            if (!settings["DragonMorph"].AsBool())
            {
                CancelBuffs(RelevantNanos.DragonMorph);
            }
            if (!settings["LeetMorph"].AsBool())
            {
                CancelBuffs(RelevantNanos.LeetMorph);
            }
            if (!settings["SaberMorph"].AsBool())
            {
                CancelBuffs(RelevantNanos.SaberMorph);
            }
            if (!settings["WolfMorph"].AsBool())
            {
                CancelBuffs(RelevantNanos.WolfMorph);
            }
        }

        private bool ArmorBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.DragonMorph)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        #region Morphs

        private bool DragonMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("DragonMorph")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool LeetMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("LeetMorph")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool WolfMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("WolfMorph")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool SaberMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("SaberMorph")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool WolfAgility(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("WolfMorph")) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.WolfMorph)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool SaberDamage(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("SaberMorph")) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.SaberMorph)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool LeetCrit(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("LeetMorph")) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.LeetMorph)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool DragonScales(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("DragonMorph")) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.DragonMorph)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Healing

        private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Heal") || !CanCast(spell)) { return false; }

            return FindMemberWithHealthBelow(85, ref actionTarget);
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal")) { return false; }

            if (!CanCast(spell)) { return false; }

            if (IsSettingEnabled("OSHeal") && !IsSettingEnabled("Heal"))
            {
                return FindPlayerWithHealthBelow(85, ref actionTarget);
            }

            return FindMemberWithHealthBelow(85, ref actionTarget);
        }

        private bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return FindMemberWithHealthBelow(30, ref actionTarget);
        }

        //private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (!IsSettingEnabled("Heal") || !CanCast(spell)) { return false; }

        //    // Try to keep our teammates alive if we're in a team
        //    if (DynelManager.LocalPlayer.IsInTeam())
        //    {
        //        List<SimpleChar> dyingTeamMember = DynelManager.Characters
        //            .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
        //            .Where(c => c.HealthPercent <= 80)
        //            .Where(c => c.HealthPercent >= 50)
        //            .ToList();

        //        if (dyingTeamMember.Count < 4)
        //        {
        //            return false;
        //        }
        //        else
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        //private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (!IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal")) { return false; }

        //    if (!CanCast(spell)) { return false; }

        //    // Try to keep our teammates alive if we're in a team
        //    if (DynelManager.LocalPlayer.IsInTeam() && IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal"))
        //    {
        //        List<SimpleChar> dyingTeamMember = DynelManager.Characters
        //            .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
        //            .Where(c => c.HealthPercent <= 80)
        //            .Where(c => c.HealthPercent >= 50)
        //            .ToList();

        //        if (dyingTeamMember.Count >= 4)
        //        {
        //            return false;
        //        }
        //        else
        //        {
        //            return FindMemberWithHealthBelow(85, ref actionTarget);
        //        }
        //    }
        //    if (IsSettingEnabled("OSHeal") && !IsSettingEnabled("Heal"))
        //    {
        //        return FindPlayerWithHealthBelow(85, ref actionTarget);
        //    }
        //    else
        //    {
        //        return FindMemberWithHealthBelow(85, ref actionTarget);
        //    }
        //}

        #endregion

        #region Misc
        private static class RelevantNanos
        {
            public static int[] HEALS = new[] { 223167, 252008, 252006, 136674, 136673, 143908, 82059, 136675, 136676, 82060, 136677,
                136678, 136679, 136682, 82061, 136681, 136680, 136683, 136684, 136685, 82062, 136686, 136689, 82063, 136688, 136687,
                82064, 26695 };

            public static readonly int[] ArmorBuffs = { 74173, 74174, 74175 , 74176, 74177, 74178 };
            public static readonly int[] DragonMorph = { 217670, 25994 };
            public static readonly int[] LeetMorph = { 263278, 82834 };
            public static readonly int[] WolfMorph = { 275005, 85062 };
            public static readonly int[] SaberMorph = { 217680, 85070 };
            public static readonly int[] DragonScales = { 302217, 302214 };
            public static readonly int[] WolfAgility = { 302235, 302232 };
            public static readonly int[] LeetCrit = { 302229, 302226 };
            public static readonly int[] SaberDamage = { 302243, 302240 };

        }

        private static class RelevantItems
        {

        }

        #endregion
    }
}
