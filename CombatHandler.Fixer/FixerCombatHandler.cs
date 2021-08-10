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
            settings.AddVariable("UseRKRunspeed", false);
            settings.AddVariable("UseEvasionDebuff", false);
            settings.AddVariable("SummonBackArmor", false);
            settings.AddVariable("BackArmorType", (int)BackItemType.SHADOWWEB_SPINNER);
            settings.AddVariable("UseLongHoT", true);
            settings.AddVariable("UseShortHoT", false);

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
            RegisterSpellProcessor(RelevantNanos.SUMMON_GRID_ARMOR, SummonGridArmor);
            RegisterSpellProcessor(RelevantNanos.SUMMON_SHADOWWEB_SPINNER, SummonShadowwebSpinner);
        }

        private bool NCUBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(HasBuffNanoLine(NanoLine.FixerNCUBuff, DynelManager.LocalPlayer))
            {
                return false;
            }
            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool LongHotBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledBuff("UseLongHoT", spell, fightingTarget, ref actionTarget);
        }

        private bool ShortHotBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return HealOverTimeBuff("UseShortHoT", spell, fightingTarget, ref actionTarget);
        }

        private bool SummonShadowwebSpinner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return SummonBackArmor(BackItemType.SHADOWWEB_SPINNER, spell, fightingTarget, ref actionTarget);
        }

        private bool SummonGridArmor(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return SummonBackArmor(BackItemType.GRID_ARMOR, spell, fightingTarget, ref actionTarget);
        }

        private bool EvasionDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("UseEvasionDebuff", spell, fightingTarget, ref actionTarget);
        }

        private bool GsfBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(IsInsideInnerSanctum())
            {
                return false;
            }
            return ToggledBuff("UseRKRunspeed", spell, fightingTarget, ref actionTarget);
        }

        private bool ShadowlandsSpeedBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(IsInsideInnerSanctum() || IsSettingEnabled("UseRKRunspeed"))
            {
                return false;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool SummonBackArmor(BackItemType backItemType, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("SummonBackArmor") || HasBackItemEquipped() || GetSelectedBackItem() != backItemType || FindBackItem(backItemType) != null)
            {
                return false;
            }

            return spell.Cost < DynelManager.LocalPlayer.Nano;
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            CancelBuffs(IsSettingEnabled("UseRKRunspeed") ? RelevantNanos.SL_RUN_BUFFS : RelevantNanos.RK_RUN_BUFFS);
            EquipBackArmor();
        }

        private void EquipBackArmor()
        {
            if (IsSettingEnabled("SummonBackArmor") && !HasBackItemEquipped() && Time.NormalTime - _lastBackArmorCheckTime > 6)
            {
                _lastBackArmorCheckTime = Time.NormalTime;
                Item backArmor = FindBackItem(GetSelectedBackItem());
                if (backArmor != null)
                {
                    backArmor.Equip(EquipSlot.Cloth_Back);
                }
            }
        }

        private Item FindBackItem(BackItemType backItemType)
        {
            int[] itemIds = backItemType == BackItemType.SHADOWWEB_SPINNER ? RelevantItems.SHADOWWEB_SPINNERS : RelevantItems.GRID_ARMORS;
            foreach (int itemId in itemIds)
            {
                if (Inventory.Find(itemId, out Item item))
                {
                    return item;
                }
            }
            return null;
        }

        private bool HasBackItemEquipped()
        {
            return Inventory.Items.Any(itemCandidate => itemCandidate.Slot.Instance == (int)EquipSlot.Cloth_Back);
        }

        private BackItemType GetSelectedBackItem()
        {
            return (BackItemType)settings["BackArmorType"].AsInt32();
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

        private enum BackItemType
        {
            SHADOWWEB_SPINNER = 0,
            GRID_ARMOR = 1
        }
    }
}
