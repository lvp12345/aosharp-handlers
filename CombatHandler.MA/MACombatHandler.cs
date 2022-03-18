using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using CombatHandler;
using CombatHandler.Generic;

namespace Desu
{
    public class MACombatHandler : GenericCombatHandler
    {
        public static string PluginDirectory;

        public static Window buffWindow;
        public static Window tauntWindow;
        public static Window healingWindow;

        public MACombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("SingleTaunt", false);

            settings.AddVariable("Heal", false);
            settings.AddVariable("OSHeal", false);

            settings.AddVariable("EvadesTeam", false);

            settings.AddVariable("Zazen", false);

            settings.AddVariable("ShortDamage", false);

            RegisterSettingsWindow("Martial-Artist Handler", "MASettingsView.xml");

            RegisterSettingsWindow("Buffs", "MABuffsView.xml");
            RegisterSettingsWindow("Healing", "MAHealingView.xml");
            RegisterSettingsWindow("Taunts", "MATauntsViewView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistDebilitatingStrike, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistAbsoluteFist, LEProc, CombatActionPriority.Low);

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.ReduceInertia, TeamBuffExcludeInnerSanctum);
            RegisterSpellProcessor(RelevantNanos.TeamCritBuffs, TeamCritBuff);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SingleTargetHealing).OrderByStackingOrder(), Healing, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TeamHealing).OrderByStackingOrder(), TeamHealing, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledDestructionBuff).Where(s => s.StackingOrder >= 19).OrderByStackingOrder(), ControlledDestructionNoShutdown);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledDestructionBuff).Where(s => s.StackingOrder < 19).OrderByStackingOrder(), ControlledDestructionWithShutdown);
            RegisterSpellProcessor(RelevantNanos.FistsOfTheWinterFlame, FistsOfTheWinterFlameNano);
            RegisterSpellProcessor(RelevantNanos.Taunts, SingleTargetTaunt, CombatActionPriority.High);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.LimboMastery, GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), EvadesTeam);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.BrawlBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ControlledRageBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RunspeedBuffs).OrderByStackingOrder(), RunSpeed);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.StrengthBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtsBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).Where(s => s.Identity.Instance != 28879).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RiposteBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtistZazenStance).OrderByStackingOrder(), ZazenStance);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalIncreaseBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), GenericBuff);


            //Items
            RegisterItemProcessor(RelevantItems.TheWizdomOfHuzzum, RelevantItems.TheWizdomOfHuzzum, MartialArtsTeamHealAttack);
            RegisterItemProcessor(RelevantItems.TouchOfSaiFung, RelevantItems.TouchOfSaiFung, TouchOfSaiFung);
            RegisterItemProcessor(RelevantItems.Sappo, RelevantItems.Sappo, Sappo);

            PluginDirectory = pluginDir;
        }

        private void BuffView(object s, ButtonBase button)
        {
            if (tauntWindow != null && tauntWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", tauntWindow);
            }
            else if (healingWindow != null && healingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", healingWindow);
            }
            else
            {
                buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\MABuffsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                buffWindow.Show(true);
            }
        }

        private void HealingView(object s, ButtonBase button)
        {
            if (buffWindow != null && buffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Healing", buffWindow);
            }
            else if (tauntWindow != null && tauntWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Healing", tauntWindow);
            }
            else
            {
                healingWindow = Window.CreateFromXml("Healing", PluginDirectory + "\\UI\\MAHealingView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                healingWindow.Show(true);
            }
        }

        private void TauntView(object s, ButtonBase button)
        {
            if (buffWindow != null && buffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Taunts", buffWindow);
            }
            else if (healingWindow != null && healingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Taunts", healingWindow);
            }
            else
            {
                tauntWindow = Window.CreateFromXml("Taunts", PluginDirectory + "\\UI\\MATauntsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                tauntWindow.Show(true);
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
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

                if (SettingsController.settingsWindow.FindView("TauntsView", out Button tauntView))
                {
                    tauntView.Tag = SettingsController.settingsWindow;
                    tauntView.Clicked = TauntView;
                }
            }

            base.OnUpdate(deltaTime);
        }

        private bool ZazenStance(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell) || !IsSettingEnabled("Zazen")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("SingleTaunt") || fightingTarget == null) { return false; }

            if (DynelManager.LocalPlayer.NanoPercent < 30) { return false; }

            //If our target has a different target than us we need to make sure we taunt
            if (IsNotFightingMe(fightingTarget))
            {
                actionTarget.Target = fightingTarget;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
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

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.MajorEvasionBuffs)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool RunSpeed(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.MajorEvasionBuffs)) { return false; }

            if (SpellChecksPlayer(spell))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
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

        protected bool TeamCritBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.CriticalIncreaseBuff)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => c.Identity != DynelManager.LocalPlayer.Identity)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
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
        private bool Sappo(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingtarget == null) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.MartialArts)) { return false; }

            return true;
        }

        private bool TouchOfSaiFung(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingtarget == null) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Dimach)) { return false; }

            return true;
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

            return FindMemberWithHealthBelow(85, ref actionTarget); ;
        }

        private static class RelevantNanos
        {
            public const int FistsOfTheWinterFlame = 269470;
            public const int LimboMastery = 28894;
            public const int ReduceInertia = 28903;
            public static int[] TeamCritBuffs = { 160574, 160575, 160576 };
            public static int[] Taunts = { 301936, 100214, 100216, 100215, 100217, 28866 };
        }

        private static class RelevantItems
        {
            public const int TheWizdomOfHuzzum = 303056;
            public const int TouchOfSaiFung = 275018;
            public const int Sappo = 267525;
            public const int TreeOfEnlightenment = 204607;
        }
    }
}
