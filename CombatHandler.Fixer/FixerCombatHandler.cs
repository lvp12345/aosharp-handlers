using AOSharp.Common.GameData;
using AOSharp.Core;
using CombatHandler.Generic;
using System.Linq;
using System.Collections.Generic;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using System;
using CombatHandler;
using AOSharp.Common.GameData.UI;

namespace Desu
{
    public class FixerCombatHandler : GenericCombatHandler
    {
        private double _lastBackArmorCheckTime = Time.NormalTime;

        public static string PluginDirectory;

        public static Window buffWindow;
        public static Window debuffWindow;

        public FixerCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("RKRunspeed", false);
            settings.AddVariable("RKRunspeedTeam", false);

            settings.AddVariable("SLRunspeed", false);

            settings.AddVariable("EvasionDebuff", false);

            settings.AddVariable("ShadowwebSpinner", false);
            settings.AddVariable("GridArmor", false);

            settings.AddVariable("LongHoT", false);
            settings.AddVariable("ShortHoT", false);
            settings.AddVariable("LongHoTTeam", false);
            settings.AddVariable("ShortHoTTeam", false);

            RegisterSettingsWindow("Fixer Handler", "FixerSettingsView.xml");

            RegisterSettingsWindow("Buffs", "FixerBuffsView.xml");
            RegisterSettingsWindow("Debuffs", "FixerDebuffsView.xml");

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcFixerBootlegRemedies, LEProc);

            RegisterPerkProcessor(PerkHash.NCUBooster, LEProc);

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
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealOverTime).OrderByStackingOrder(), TeamShortHotBuff);
            RegisterSpellProcessor(RelevantNanos.RK_RUN_BUFFS, GsfBuff);
            RegisterSpellProcessor(RelevantNanos.SL_RUN_BUFFS, ShadowlandsSpeedBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EvasionDebuffs).OrderByStackingOrder(), EvasionDebuff);
            RegisterSpellProcessor(RelevantNanos.SUMMON_GRID_ARMOR, GridArmor);
            RegisterSpellProcessor(RelevantNanos.SUMMON_SHADOWWEB_SPINNER, ShadowwebSpinner);

            PluginDirectory = pluginDir;
        }

        private void BuffView(object s, ButtonBase button)
        {
            if (debuffWindow != null && debuffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", debuffWindow);
            }
            else
            {
                buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\FixerBuffsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                buffWindow.Show(true);
            }
        }

        private void DebuffView(object s, ButtonBase button)
        {
            if (buffWindow != null && buffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Debuffs", buffWindow);
            }
            else
            {
                debuffWindow = Window.CreateFromXml("Debuffs", PluginDirectory + "\\UI\\FixerDebuffsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                debuffWindow.Show(true);
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            if (settings["RKRunspeed"].AsBool() && settings["SLRunspeed"].AsBool())
            {
                settings["RKRunspeed"] = false;
                settings["SLRunspeed"] = false;

                Chat.WriteLine("Only activate one Runspeed option.");
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                if (SettingsController.settingsWindow != null)
                {
                    if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                    {
                        buffView.Tag = SettingsController.settingsWindow;
                        buffView.Clicked = BuffView;
                    }

                    if (SettingsController.settingsWindow.FindView("DebuffsView", out Button debuffView))
                    {
                        debuffView.Tag = SettingsController.settingsWindow;
                        debuffView.Clicked = DebuffView;
                    }
                }
            }

            EquipBackArmor();
        }

        private bool NCUBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HasBuffNanoLine(NanoLine.FixerNCUBuff, DynelManager.LocalPlayer)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool LongHotBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("LongHoTTeam"))
            {
                if (fightingTarget != null || !CanCast(spell) || spell.Name.Contains("Veteran")) { return false; }

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

                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool TeamShortHotBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("ShortHoTTeam")) { return false; }

            return AllTeamBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool ShortHotBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("ShortHoT")) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool EvasionDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("EvasionDebuff", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        protected bool GSFTeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    //.Where(c => !c.Buffs.Contains(RelevantNanos.EVASION_BUFFS))
                    .Where(c => SpellChecksOther(spell, spell.Nanoline, c))
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

        private bool GsfBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (IsSettingEnabled("RKRunspeedTeam"))
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.SL_RUN_BUFFS))
                {
                    CancelBuffs(RelevantNanos.SL_RUN_BUFFS);
                }

                return GSFTeamBuff(spell, fightingTarget, ref actionTarget);
            }

            if (IsSettingEnabled("RKRunspeed"))
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.SL_RUN_BUFFS))
                {
                    CancelBuffs(RelevantNanos.SL_RUN_BUFFS);
                }

                return ToggledBuff("RKRunspeed", spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool ShadowlandsSpeedBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (IsSettingEnabled("SLRunspeed"))
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.RK_RUN_BUFFS))
                {
                    CancelBuffs(RelevantNanos.RK_RUN_BUFFS);
                }

                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
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
            public const int SuperiorInsuranceHack = 273352;
            public static readonly int[] SL_RUN_BUFFS = { 223125, 223131, 223129, 215718, 223127, 272416, 272415, 272414, 272413, 272412 };
            public static readonly int[] RK_RUN_BUFFS = { 93132, 93126, 93127, 93128, 93129, 93130, 93131, 93125 };
            public static readonly int[] EVASION_BUFFS = { 275844, 29247, 28903, 28878, 28872, 218070, 218068, 218066,
            218064, 218062, 218060, 272371, 270808, 30745, 302188, 29272, 270802, 28603, 223125, 223131, 223129, 215718,
            223127, 272416, 272415, 272414, 272413, 272412};
            public static readonly int[] SUMMON_GRID_ARMOR = { 155189, 155187, 155188, 155186 };
            public static readonly int[] SUMMON_SHADOWWEB_SPINNER = { 273349, 224422, 224420, 224418, 224416, 224414, 224412, 224410, 224408, 224405, 224403 };
            public static readonly int[] NCU_BUFFS = { 275043, 163095, 163094, 163087, 163085, 163083, 163081, 163079, 162995 };
            //public static readonly Spell[] TeamShortHoTs = Spell.GetSpellsForNanoline(NanoLine.HealOverTime).OrderByStackingOrder().Where(spell => spell.Identity.Instance != SuperiorInsuranceHack).ToArray();
            public static readonly Spell[] TEAM_LONG_HOTS = Spell.GetSpellsForNanoline(NanoLine.FixerLongHoT).OrderByStackingOrder().Where(spell => spell.Identity.Instance != GREATER_PRESERVATION_MATRIX).ToArray();
        }

        private static class RelevantItems
        {
            public static readonly int[] GRID_ARMORS = { 155172, 155173, 155174, 155150 };
            public static readonly int[] SHADOWWEB_SPINNERS = { 273350, 224400, 224399, 224398, 224397, 224396, 224395, 224394, 224393, 224392, 224390 };
        }
    }
}
