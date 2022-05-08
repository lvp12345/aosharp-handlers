using AOSharp.Common.GameData;
using AOSharp.Core;
using CombatHandler.Generic;
using AOSharp.Core.UI;
using System.Linq;
using System.Collections.Generic;
using System;
using AOSharp.Common.GameData.UI;

namespace CombatHandler.Agent
{
    public class AgentCombatHandler : GenericCombatHandler
    {
        private double _lastSwitchedHealTime = 0;

        public static Window buffWindow;
        public static Window debuffWindow;
        public static Window procWindow;
        public static Window falseProfWindow;
        public static Window healingWindow;

        public static string PluginDirectory;

        public AgentCombatHandler(string pluginDir) : base(pluginDir)
        {
            _settings.AddVariable("DotStrainA", false);

            _settings.AddVariable("CritTeam", false);

            _settings.AddVariable("InitDebuff", false);
            _settings.AddVariable("OSInitDebuff", false);


            _settings.AddVariable("Heal", false);
            _settings.AddVariable("OSHeal", false);


            _settings.AddVariable("EvasionDebuff", false);


            _settings.AddVariable("Damage", false);
            _settings.AddVariable("Detaunt", false);


            _settings.AddVariable("LazerAim", false);
            _settings.AddVariable("NotumChargedRounds", false);

            _settings.AddVariable("Concentration", false);

            _settings.AddVariable("FalseProfSelection", (int)FalseProfSelection.None);


            RegisterSettingsWindow("Agent Handler", "AgentSettingsView.xml");

            RegisterSettingsWindow("Healing", "AgentHealingView.xml");
            RegisterSettingsWindow("Procs", "AgentProcView.xml");
            RegisterSettingsWindow("Buffs", "AgentDebuffsView.xml");
            RegisterSettingsWindow("Debuffs", "AgentBuffsView.xml");
            RegisterSettingsWindow("False Professions", "AgentFalseProfsView.xml");

            ////LE Procs
            RegisterPerkProcessor(PerkHash.LEProcAgentGrimReaper, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcAgentLaserAim, LaserAim);
            RegisterPerkProcessor(PerkHash.LEProcAgentNotumChargedRounds, NotumChargedRounds);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AgilityBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AimedShotBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ConcealmentBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalIncreaseBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ExecutionerBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RifleBuffs).OrderByStackingOrder(), RifleBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AgentProcBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ConcentrationCriticalLine).OrderByStackingOrder(), Concentration);

            RegisterSpellProcessor(RelevantNanos.HEALS, Healing, CombatActionPriority.High);
            //RegisterSpellProcessor(RelevantNanos.CH, CompleteHealing, CombatActionPriority.Medium);

            RegisterSpellProcessor(RelevantNanos.FalseProfDoc, FalseProfDoctor);
            RegisterSpellProcessor(RelevantNanos.FalseProfAdv, FalseProfAdventurer);
            RegisterSpellProcessor(RelevantNanos.FalseProfCrat, FalseProfBeauracrat);
            RegisterSpellProcessor(RelevantNanos.FalseProfEnf, FalseProfEnforcer);
            RegisterSpellProcessor(RelevantNanos.FalseProfEng, FalseProfEngineer);
            RegisterSpellProcessor(RelevantNanos.FalseProfFixer, FalseProfFixer);
            RegisterSpellProcessor(RelevantNanos.FalseProfMa, FalseProfMartialArtist);
            RegisterSpellProcessor(RelevantNanos.FalseProfMp, FalseProfMetaphysicist);
            RegisterSpellProcessor(RelevantNanos.FalseProfNt, FalseProfNanoTechnician);
            RegisterSpellProcessor(RelevantNanos.FalseProfSol, FalseProfSoldier);
            RegisterSpellProcessor(RelevantNanos.FalseProfTrader, FalseProfTrader);

            RegisterSpellProcessor(RelevantNanos.DetauntProcs, DetauntProc);
            RegisterSpellProcessor(RelevantNanos.DotProcs, DamageProc);

            //Team Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(), TeamBuff);
            RegisterSpellProcessor(RelevantNanos.TeamCritBuffs, TeamCrit);

            //Debuffs/DoTs
            RegisterSpellProcessor(RelevantNanos.InitDebuffs, InitDebuffTarget);
            RegisterSpellProcessor(RelevantNanos.InitDebuffs, OSInitDebuff, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTAgentStrainA).OrderByStackingOrder(), DotStrainA);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EvasionDebuffs_Agent), EvasionDebuff);

            PluginDirectory = pluginDir;
        }

        private void ProcView(object s, ButtonBase button)
        {
            if (buffWindow != null && buffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Procs", buffWindow);
            }
            else if (debuffWindow != null && debuffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Procs", debuffWindow);
            }
            else if (healingWindow != null && healingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Procs", healingWindow);
            }
            else
            {
                procWindow = Window.CreateFromXml("Procs", PluginDirectory + "\\UI\\AgentProcView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                procWindow.Show(true);
            }
        }

        private void FalseProfView(object s, ButtonBase button)
        {
            if (buffWindow != null && procWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("False Professions", procWindow);
            }
            else if (procWindow != null && procWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("False Professions", procWindow);
            }
            else if (debuffWindow != null && debuffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("False Professions", debuffWindow);
            }
            else if (healingWindow != null && healingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("False Professions", healingWindow);
            }
            else
            {
                falseProfWindow = Window.CreateFromXml("False Professions", PluginDirectory + "\\UI\\AgentFalseProfsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                falseProfWindow.Show(true);
            }
        }

        private void BuffView(object s, ButtonBase button)
        {
            if (procWindow != null && procWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", procWindow);
            }
            else if (debuffWindow != null && debuffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", debuffWindow);
            }
            else if (healingWindow != null && healingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", healingWindow);
            }
            else if (falseProfWindow != null && falseProfWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", falseProfWindow);
            }
            else
            {
                buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\AgentBuffsView.xml",
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
            else if (procWindow != null && procWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Debuffs", procWindow);
            }
            else if (healingWindow != null && healingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Debuffs", healingWindow);
            }
            else if (falseProfWindow != null && falseProfWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Debuffs", falseProfWindow);
            }
            else
            {
                debuffWindow = Window.CreateFromXml("Debuffs", PluginDirectory + "\\UI\\AgentDebuffsView.xml",
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
            else if (procWindow != null && procWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Healing", procWindow);
            }
            else if (falseProfWindow != null && falseProfWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Healing", falseProfWindow);
            }
            else
            {
                healingWindow = Window.CreateFromXml("Healing", PluginDirectory + "\\UI\\AgentHealingView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                healingWindow.Show(true);
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (CanLookupPetsAfterZone())
            {
                SynchronizePetCombatStateWithOwner();
                AssignTargetToHealPet();
            }

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

                if (SettingsController.settingsWindow.FindView("DebuffsView", out Button debuffView))
                {
                    debuffView.Tag = SettingsController.settingsWindow;
                    debuffView.Clicked = DebuffView;
                }

                if (SettingsController.settingsWindow.FindView("ProcView", out Button procView))
                {
                    procView.Tag = SettingsController.settingsWindow;
                    procView.Clicked = ProcView;
                }

                if (SettingsController.settingsWindow.FindView("FalseProfsView", out Button falseProfView))
                {
                    falseProfView.Tag = SettingsController.settingsWindow;
                    falseProfView.Clicked = FalseProfView;
                }
            }

            base.OnUpdate(deltaTime);

            if (IsSettingEnabled("Damage") && !IsSettingEnabled("Detaunt"))
            {
                CancelBuffs(RelevantNanos.DetauntProcs);
            }
            if (IsSettingEnabled("Detaunt") && !IsSettingEnabled("Damage"))
            {
                CancelBuffs(RelevantNanos.DotProcs);
            }
        }

        #region Healing

        private void AssignTargetToHealPet()
        {
            if (Time.NormalTime - _lastSwitchedHealTime > 5)
            {
                SimpleChar dyingTarget = GetTargetToHeal();
                if (dyingTarget != null)
                {
                    Pet healPet = DynelManager.LocalPlayer.Pets.Where(pet => pet.Type == PetType.Heal).FirstOrDefault();
                    if (healPet != null)
                    {
                        healPet.Heal(dyingTarget.Identity);
                        _lastSwitchedHealTime = Time.NormalTime;
                    }
                }
            }
        }

        private SimpleChar GetTargetToHeal()
        {
            if (DynelManager.LocalPlayer.HealthPercent < 90)
            {
                return DynelManager.LocalPlayer;
            }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar dyingTeamMember = DynelManager.Characters
                    .Where(c => c.IsAlive)
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent < 90)
                    .OrderByDescending(c => c.HealthPercent)
                    .FirstOrDefault();

                if (dyingTeamMember != null)
                {
                    return dyingTeamMember;
                }
            }

            Pet dyingPet = DynelManager.LocalPlayer.Pets
                 .Where(pet => pet.Type == PetType.Attack || pet.Type == PetType.Social)
                 .Where(pet => pet.Character.HealthPercent < 90)
                 .OrderByDescending(pet => pet.Character.HealthPercent)
                 .FirstOrDefault();

            if (dyingPet != null)
            {
                return dyingPet.Character;
            }

            return null;
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

        //private bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (!IsSettingEnabled("CH"))
        //    {
        //        return false;
        //    }

        //    return FindMemberWithHealthBelow(50, ref actionTarget);
        //}

        #endregion

        #region Instanced Logic

        //private bool FalseProfDoc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        //{
        //    if (!IsSettingEnabled("Heal") && !IsSettingEnabled("OSHeal")) { return false; }

        //    return GenericBuff(spell, fightingTarget, ref actionTarget);
        //}

        private bool FalseProfEnforcer(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Enforcer != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfEngineer(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Engineer != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfNanoTechnician(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.NanoTechnician != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfTrader(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Trader != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfBeauracrat(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Beauracrat != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfMartialArtist(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.MartialArtist != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfFixer(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Fixer != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfAdventurer(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Adventurer != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfDoctor(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Doctor != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool FalseProfSoldier(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Soldier != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool FalseProfMetaphysicist(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (FalseProfSelection.Metaphysicist != (FalseProfSelection)_settings["FalseProfSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool RifleBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.AssassinsAimedShot, out Buff AAS)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Find(RelevantNanos.SteadyNerves, out Buff SN)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool LaserAim(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("LazerAim")) { return false; }

            if (IsSettingEnabled("NotumChargedRounds") && IsSettingEnabled("LazerAim")) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool NotumChargedRounds(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("NotumChargedRounds")) { return false; }

            if (IsSettingEnabled("NotumChargedRounds") && IsSettingEnabled("LazerAim")) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool AgentToggledDebuffTarget(String settingName, Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SomeoneNeedsHealing()) { return false; }

            return ToggledDebuffTarget(settingName, spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool TeamCrit(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam)
            {
                if (!IsSettingEnabled("CritTeam")) { return false; }

                return TeamBuff(spell, fightingTarget, ref actionTarget);
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool InitDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SomeoneNeedsHealing()) { return false; }

            return AgentToggledDebuffTarget("InitDebuff", spell, fightingTarget, ref actionTarget);
        }

        private bool OSInitDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (SomeoneNeedsHealing()) { return false; }

            return ToggledDebuffOthersInCombat("OSInitDebuff", spell, fightingTarget, ref actionTarget);
        }


        private bool DetauntProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Detaunt")) { return false; }

            if (!IsSettingEnabled("Detaunt") && !IsSettingEnabled("Damage") || (IsSettingEnabled("Detaunt") && IsSettingEnabled("Damage"))) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DamageProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Damage")) { return false; }

            if (!IsSettingEnabled("Detaunt") && !IsSettingEnabled("Damage") || (IsSettingEnabled("Detaunt") && IsSettingEnabled("Damage"))) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool Concentration(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Concentration")) { return false; }

            return CombatBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DotStrainA(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("DotStrainA", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool EvasionDebuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("EvasionDebuff", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Misc

        public enum FalseProfSelection
        {
            None, Metaphysicist, Soldier, Enforcer, Engineer, Doctor, Fixer, Beauracrat, MartialArtist, NanoTechnician, Trader, Adventurer
        }


        private static class RelevantNanos
        {
            public static int[] DetauntProcs = { 226437, 226435, 226433, 226431, 226429, 226427 };
            public static int[] FalseProfDoc = { 117210, 117221, 32033 };
            public static int[] FalseProfEng = { 117213, 117224, 32034 };
            public static int[] FalseProfSol = { 117216, 117227, 32038 };
            public static int[] FalseProfCrat = { 117209, 117220, 32032 };
            public static int[] FalseProfTrader = { 117211, 117222, 32040 };
            public static int[] FalseProfAdv = { 117214, 117225, 32030 };
            public static int[] FalseProfMp = { 117210, 117221, 32033 };
            public static int[] FalseProfFixer = { 117212, 117223, 32039 };
            public static int[] FalseProfEnf = { 117217, 117228, 32041 };
            public static int[] FalseProfMa = { 117215, 117226, 32035 };
            public static int[] FalseProfNt = { 117207, 117218, 32037 };
            public static int[] DotProcs = { 226425, 226423, 226421, 226419, 226417, 226415, 226413, 226410 };
            public static int[] TeamCritBuffs = { 160791, 160789, 160787 };
            public static int AssassinsAimedShot = 275007;
            public static int SteadyNerves = 160795;
            public static int CH = 28650;
            public static int TeamCH = 42409; //Add logic later
            public const int TiredLimbs = 99578;
            public static readonly Spell[] InitDebuffs = Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder().Where(spell => spell.Identity.Instance != TiredLimbs).ToArray();
            public static int[] HEALS = new[] { 223299, 223297, 223295, 223293, 223291, 223289, 223287, 223285, 223281, 43878, 43881, 43886, 43885,
                43887, 43890, 43884, 43808, 43888, 43889, 43883, 43811, 43809, 43810, 28645, 43816, 43817, 43825, 43815,
                43814, 43821, 43820, 28648, 43812, 43824, 43822, 43819, 43818, 43823, 28677, 43813, 43826, 43838, 43835,
                28672, 43836, 28676, 43827, 43834, 28681, 43837, 43833, 43830, 43828, 28654, 43831, 43829, 43832, 28665 };
        }

        #endregion
    }
}
