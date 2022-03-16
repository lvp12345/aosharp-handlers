using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.UI;
using CombatHandler;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Desu
{
    class DocCombatHandler : GenericCombatHandler
    {
        public static string PluginDirectory;

        public static Window buffWindow;
        public static Window debuffWindow;
        public static Window healingWindow;
        public static Window aidingWindow;

        public DocCombatHandler(String pluginDir) : base(pluginDir)
        {
            settings.AddVariable("InitDebuff", false);
            settings.AddVariable("OSInitDebuff", false);

            settings.AddVariable("DotA", false);
            settings.AddVariable("DotB", false);
            settings.AddVariable("DotC", false);


            settings.AddVariable("ShortHoT", false);
            settings.AddVariable("ShortHoTTeam", false);
            settings.AddVariable("Deathless", false);

            settings.AddVariable("ShortHP", false);
            settings.AddVariable("ShortHPTeam", false);

            settings.AddVariable("Heal", true);
            settings.AddVariable("OSHeal", false);
            settings.AddVariable("CH", true);

            settings.AddVariable("LockCH", false);

            RegisterSettingsWindow("Doctor Handler", "DocSettingsView.xml");

            RegisterSettingsWindow("Healing", "DocHealingView.xml");
            RegisterSettingsWindow("Buffs", "DocBuffsView.xml");
            RegisterSettingsWindow("Debuffs", "DocDebuffsView.xml");
            RegisterSettingsWindow("Aiding", "DocAidingView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcDoctorAstringent, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorMuscleMemory, LEProc, CombatActionPriority.Low);

            //Healing
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CompleteHealingLine).OrderByStackingOrder(), CompleteHealing, CombatActionPriority.High);

            RegisterSpellProcessor(RelevantNanos.Heals, Healing, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.ImprovedLC, TeamHealing, CombatActionPriority.Medium);

            RegisterSpellProcessor(RelevantNanos.AlphaAndOmega, LockCH, CombatActionPriority.High);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.HPBuffs, HPBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolMasteryBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealDeltaBuff).OrderByStackingOrder(), TeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceBuffs).OrderByStackingOrder(), NanoResistanceBuff);

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.TeamDeathlessBlessing, TeamDeathlessBlessing);
            RegisterSpellProcessor(RelevantNanos.IndividualShortHoT, ShortHoTBuff);

            RegisterSpellProcessor(RelevantNanos.ImprovedLC, LifeChanneler);
            RegisterSpellProcessor(RelevantNanos.IndividualShortHP, ShortHPBuff);

            //Debuffs
            RegisterSpellProcessor(RelevantNanos.InitDebuffs, InitDebuffTarget);
            RegisterSpellProcessor(RelevantNanos.InitDebuffs, OSInitDebuff, CombatActionPriority.Low);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineA).OrderByStackingOrder(), DOTADebuffTarget);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineB).OrderByStackingOrder(), DOTBDebuffTarget);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTStrainC).OrderByStackingOrder(), DOTCDebuffTarget);

            PluginDirectory = pluginDir;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (IsSettingEnabled("Deathless") && IsSettingEnabled("HoT"))
            {
                settings["Deathless"] = false;
                settings["ShortHoT"] = false;

                Chat.WriteLine($"Can only have one Short HoT active.");
            }

            if (IsSettingEnabled("Deathless") && IsSettingEnabled("ShortHoTTeam"))
            {
                settings["Deathless"] = false;
                settings["ShortHoTTeam"] = false;

                Chat.WriteLine($"Can only have one Short HoT active.");
            }

            if (IsSettingEnabled("ShortHoT") && IsSettingEnabled("ShortHoTTeam"))
            {
                settings["ShortHot"] = false;
                settings["ShortHoTTeam"] = false;

                Chat.WriteLine($"Can only have one Short HoT active.");
            }

            if (IsSettingEnabled("ShortHP") && IsSettingEnabled("ShortHPTeam"))
            {
                settings["ShortHP"] = false;
                settings["ShortHPTeam"] = false;

                Chat.WriteLine($"Can only have one Short Hp active.");
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                if (SettingsController.settingsWindow != null)
                {
                    if (SettingsController.settingsWindow.FindView("AidingView", out Button helpView))
                    {
                        helpView.Tag = SettingsController.settingsWindow;
                        helpView.Clicked = AidingView;
                    }

                    if (SettingsController.settingsWindow.FindView("HealingView", out Button healingView))
                    {
                        healingView.Tag = SettingsController.settingsWindow;
                        healingView.Clicked = HealingView;
                    }

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

            base.OnUpdate(deltaTime);
        }

        #region Healing

        private bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("CH") || !CanCast(spell)) { return false; }

            return FindMemberWithHealthBelow(50, ref actionTarget);
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
            if (!IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal")) { return false; }

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

            if (IsSettingEnabled("OSHeal") && !IsSettingEnabled("Heal"))
            {
                return FindPlayerWithHealthBelow(85, ref actionTarget);
            }

            return FindMemberWithHealthBelow(85, ref actionTarget);
        }

        #endregion

        #region Buffs

        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => SpellChecksOther(spell, spell.Nanoline, c))
                    .Where(c => c.Profession == Profession.Doctor || c.Profession == Profession.NanoTechnician)
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

        private bool LifeChanneler(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HasBuffNanoLine(NanoLine.DoctorShortHPBuffs, DynelManager.LocalPlayer)) { return false; }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool NanoResistanceBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool HPBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HasBuffNanoLine(NanoLine.DoctorHPBuffs, DynelManager.LocalPlayer)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool ShortHoTBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("ShortHoT"))
            {
                return AllBuff(spell, fightingTarget, ref actionTarget);
            }

            if (IsSettingEnabled("ShortHoTTeam"))
            {
                return AllTeamBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool ShortHPBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("ShortHP"))
            {
                return AllBuff(spell, fightingTarget, ref actionTarget);
            }

            if (IsSettingEnabled("ShortHPTeam"))
            {
                return AllTeamBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool TeamDeathlessBlessing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Deathless")) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.IndividualShortHoTs))
            {
                CancelBuffs(RelevantNanos.IndividualShortHoTs);
            }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Debuffs

        private bool DocToggledDebuffTarget(String settingName, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SomeoneNeedsHealing()) { return false; }

            return ToggledDebuffTarget(settingName, spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool InitDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return DocToggledDebuffTarget("InitDebuff", spell, fightingTarget, ref actionTarget);
        }

        private bool DOTADebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return DocToggledDebuffTarget("DotA", spell, fightingTarget, ref actionTarget);
        }

        private bool DOTBDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return DocToggledDebuffTarget("DotB", spell, fightingTarget, ref actionTarget);
        }

        private bool DOTCDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return DocToggledDebuffTarget("DotC", spell, fightingTarget, ref actionTarget);
        }

        private bool OSInitDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SomeoneNeedsHealing()) { return false; }

            return ToggledDebuffOthersInCombat("OSInitDebuff", spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Misc

        private bool LockCH(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("LockCH"))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        private void BuffView(object s, ButtonBase button)
        {
            if (healingWindow != null && healingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", healingWindow);
            }
            else if (debuffWindow != null && debuffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", debuffWindow);
            }
            else if (aidingWindow != null && aidingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", aidingWindow);
            }
            else
            {
                buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\DocBuffsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                buffWindow.Show(true);
            }
        }

        private void DebuffView(object s, ButtonBase button)
        {
            if (healingWindow != null && healingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Debuffs", healingWindow);
            }
            else if (buffWindow != null && buffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Debuffs", buffWindow);
            }
            else if (aidingWindow != null && aidingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Debuffs", aidingWindow);
            }
            else
            {
                debuffWindow = Window.CreateFromXml("Debuffs", PluginDirectory + "\\UI\\DocDebuffsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                debuffWindow.Show(true);
            }
        }

        private void HealingView(object s, ButtonBase button)
        {
            if (buffWindow != null && buffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Healing", buffWindow);
            }
            else if (debuffWindow != null && debuffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Healing", debuffWindow);
            }
            else if (aidingWindow != null && aidingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Healing", aidingWindow);
            }
            else
            {
                healingWindow = Window.CreateFromXml("Healing", PluginDirectory + "\\UI\\DocHealingView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                healingWindow.Show(true);
            }
        }

        private void AidingView(object s, ButtonBase button)
        {
            if (healingWindow != null && healingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Aiding", healingWindow);
            }
            else if (buffWindow != null && buffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Aiding", buffWindow);
            }
            else if (debuffWindow != null && debuffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Aiding", debuffWindow);
            }
            else
            {
                aidingWindow = Window.CreateFromXml("Aiding", PluginDirectory + "\\UI\\DocAidingView.xml",
                    windowSize: new Rect(0, 0, 270, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                aidingWindow.Show(true);
            }
        }

        private static class RelevantNanos
        {
            public const int TeamDeathlessBlessing = 269455;
            public static readonly Spell[] IndividualShortHoT = Spell.GetSpellsForNanoline(NanoLine.HealOverTime).OrderByStackingOrder()
                .Where(spell => spell.Identity.Instance != TeamDeathlessBlessing).ToArray();
            public static int[] IndividualShortHoTs = new[] { 43852, 43868, 43870, 43872, 43873, 43871, 42396, 43869, 43867, 43877, 43876, 43875, 43879,
                42399, 43882, 43874, 43880, 42401 };

            public const int ImprovedLC = 275011;
            public static readonly Spell[] IndividualShortHP = Spell.GetSpellsForNanoline(NanoLine.DoctorShortHPBuffs).OrderByStackingOrder()
                .Where(spell => spell.Identity.Instance != ImprovedLC).ToArray();

            public const int TiredLimbs = 99578;
            public static readonly Spell[] InitDebuffs = Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder()
                .Where(spell => spell.Identity.Instance != TiredLimbs).ToArray();

            public static int[] HPBuffs = new[] { 95709, 28662, 95720, 95712, 95710, 95711, 28649, 95713, 28660, 95715, 95714, 95718, 95716, 95717, 95719, 42397 };

            public const int AlphaAndOmega = 42409;
            public static int[] Heals = new[] { 223299, 223297, 223295, 223293, 223291, 223289, 223287, 223285, 223281, 43878, 43881, 43886, 43885,
                43887, 43890, 43884, 43808, 43888, 43889, 43883, 43811, 43809, 43810, 28645, 43816, 43817, 43825, 43815,
                43814, 43821, 43820, 28648, 43812, 43824, 43822, 43819, 43818, 43823, 28677, 43813, 43826, 43838, 43835,
                28672, 43836, 28676, 43827, 43834, 28681, 43837, 43833, 43830, 43828, 28654, 43831, 43829, 43832, 28665 };
        }

        #endregion
    }
}