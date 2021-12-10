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
    public class FixerCombatHandler : GenericCombatHandler
    {
        private double _lastBackArmorCheckTime = Time.NormalTime;

        public FixerCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("RKRunspeed", false);
            settings.AddVariable("SLRunspeed", false);
            settings.AddVariable("EvasionDebuff", false);
            settings.AddVariable("ShadowwebSpinner", false);
            settings.AddVariable("GridArmor", false);
            settings.AddVariable("LongHoT", false);
            settings.AddVariable("ShortHoT", false);
            settings.AddVariable("LongHoTOther", false);
            settings.AddVariable("ShortHoTOther", false);

            RegisterSettingsWindow("Fixer Handler", "FixerSettingsView.xml");

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcFixerBootlegRemedies, LEProc);

            //Luck's Calamity is missing from PerkHash list
            PerkAction lucksCalamity = PerkAction.List.Where(action => action.Name.Equals("Luck's Calamity")).FirstOrDefault();
            if (lucksCalamity != null) {
                RegisterPerkProcessor(lucksCalamity.Hash, LEProc);
            }
            
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FixerDodgeBuffLine).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FixerSuppressorBuff).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(RelevantNanos.NCU_BUFFS, NCUBuff);
            RegisterSpellProcessor(RelevantNanos.GREATER_PRESERVATION_MATRIX, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.TEAM_LONG_HOTS, LongHotBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealOverTime).OrderByStackingOrder(), ShortHotBuff);
            RegisterSpellProcessor(RelevantNanos.RK_RUN_BUFFS, GsfBuff);
            RegisterSpellProcessor(RelevantNanos.SL_RUN_BUFFS, ShadowlandsSpeedBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EvasionDebuffs).OrderByStackingOrder(), EvasionDebuff);
            RegisterSpellProcessor(RelevantNanos.SUMMON_GRID_ARMOR, GridArmor);
            RegisterSpellProcessor(RelevantNanos.SUMMON_SHADOWWEB_SPINNER, ShadowwebSpinner);
        }

        private bool NCUBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HasBuffNanoLine(NanoLine.FixerNCUBuff, DynelManager.LocalPlayer)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool LongHotBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("LongHoTOther"))
            {
                return TeamBuff(spell, fightingTarget, ref actionTarget);
            }

            return ToggledBuff("LongHoT", spell, fightingTarget, ref actionTarget);
        }

        private bool ShortHotBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("ShortHoTOther"))
            {
                return TeamBuff(spell, fightingTarget, ref actionTarget);
            }

            return ToggledBuff("ShortHoT", spell, fightingTarget, ref actionTarget);
        }

        private bool EvasionDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("EvasionDebuff", spell, fightingTarget, ref actionTarget);
        }

        private bool GsfBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.RunspeedBuffs)) { return false; }

            return ToggledBuff("RKRunspeed", spell, fightingTarget, ref actionTarget);
        }

        private bool ShadowlandsSpeedBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.RunspeedBuffs)) { return false; }

            return ToggledBuff("SLRunspeed", spell, fightingTarget, ref actionTarget);
        }

        private bool GridArmor(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("GridArmor") || !CanCast(spell)) { return false; }

            if (Inventory.Items.FirstOrDefault(x => RelevantItems.GRID_ARMORS.Contains(x.HighId)) != null)
            {
                return false;
            }

            return true;
        }

        private bool ShadowwebSpinner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("ShadowwebSpinner") || !CanCast(spell)) { return false; }

            if (Inventory.Items.FirstOrDefault(x => RelevantItems.SHADOWWEB_SPINNERS.Contains(x.HighId)) != null)
            {
                return false;
            }

            return true;
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            if (settings["RKRunspeed"].AsBool() && settings["SLRunspeed"].AsBool())
            {
                settings["RKRunspeed"] = false;
                settings["SLRunspeed"] = false;

                Chat.WriteLine("Can only activate one.");
            }

            CancelBuffs(IsSettingEnabled("RKRunspeed") ? RelevantNanos.RK_RUN_BUFFS : RelevantNanos.SL_RUN_BUFFS);
            CancelBuffs(IsSettingEnabled("SLRunspeed") ? RelevantNanos.SL_RUN_BUFFS : RelevantNanos.RK_RUN_BUFFS);
            EquipBackArmor();
        }

        private void EquipBackArmor()
        {
            if (IsSettingEnabled("GridArmor") && !HasBackItemEquipped() && Time.NormalTime - _lastBackArmorCheckTime > 6)
            {
                _lastBackArmorCheckTime = Time.NormalTime;
                Item backArmor = Inventory.Items.FirstOrDefault(x => RelevantItems.GRID_ARMORS.Contains(x.HighId));
                if (backArmor != null)
                {
                    backArmor.Equip(EquipSlot.Cloth_Back);
                }
            }

            if (IsSettingEnabled("ShadowwebSpinner") && !HasBackItemEquipped() && Time.NormalTime - _lastBackArmorCheckTime > 6)
            {
                _lastBackArmorCheckTime = Time.NormalTime;
                Item backArmor = Inventory.Items.FirstOrDefault(x => RelevantItems.SHADOWWEB_SPINNERS.Contains(x.HighId));
                if (backArmor != null)
                {
                    backArmor.Equip(EquipSlot.Cloth_Back);
                }
            }
        }

        private bool HasBackItemEquipped()
        {
            return Inventory.Items.Any(itemCandidate => itemCandidate.Slot.Instance == (int)EquipSlot.Cloth_Back);
        }

        private static class RelevantNanos
        {
            public const int GREATER_PRESERVATION_MATRIX = 275679;
            public static readonly int[] SL_RUN_BUFFS = { 223125, 223131, 223129, 215718, 223127, 272416, 272415, 272414, 272413, 272412 };
            public static readonly int[] RK_RUN_BUFFS = { 93132, 93126, 93127, 93128, 93129, 93130, 93131, 93125 };
            public static readonly int[] SUMMON_GRID_ARMOR = { 155189, 155187, 155188, 155186 };
            public static readonly int[] SUMMON_SHADOWWEB_SPINNER = { 273349, 224422, 224420, 224418, 224416, 224414, 224412, 224410, 224408, 224405, 224403 };
            public static readonly int[] NCU_BUFFS = { 275043, 163095, 163094, 163087, 163085, 163083, 163081, 163079, 162995 };
            public static readonly Spell[] TEAM_LONG_HOTS = Spell.GetSpellsForNanoline(NanoLine.FixerLongHoT).OrderByStackingOrder().Where(spell => spell.Identity.Instance != GREATER_PRESERVATION_MATRIX).ToArray();
        }

        private static class RelevantItems
        {
            public static readonly int[] GRID_ARMORS = { 155172, 155173, 155174, 155150 };
            public static readonly int[] SHADOWWEB_SPINNERS = { 273350, 224400, 224399, 224398, 224397, 224396, 224395, 224394, 224393, 224392, 224390 };
        }
    }
}
