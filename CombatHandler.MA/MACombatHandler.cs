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

        public static double _singleTauntTick;
        public static double _singleTaunt;

        public static Window _buffWindow;
        public static Window _tauntWindow;
        public static Window _healingWindow;
        public static Window _procWindow;

        public static View _buffView;
        public static View _tauntView;
        public static View _healingView;
        public static View _procView;

        private static Settings buff = new Settings("Buffs");
        private static Settings taunt = new Settings("Taunts");
        private static Settings healing = new Settings("Healing");
        private static Settings proc = new Settings("Procs");

        public MACombatHandler(string pluginDir) : base(pluginDir)
        {
            _settings.AddVariable("SingleTaunt", false);
            _settings.AddVariable("OSTaunt", false);

            _settings.AddVariable("Heal", false);
            _settings.AddVariable("OSHeal", false);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.AbsoluteFist);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.SelfReconstruction);

            _settings.AddVariable("DamageTypeSelection", (int)DamageTypeSelection.Melee);

            _settings.AddVariable("EvadesTeam", false);

            _settings.AddVariable("Zazen", false);

            _settings.AddVariable("ShortDamage", false);

            RegisterSettingsWindow("Martial-Artist Handler", "MASettingsView.xml");

            SettingsController.RegisterSettingsWindow("Buffs", pluginDir + "\\UI\\MABuffsView.xml", buff);
            SettingsController.RegisterSettingsWindow("Healing", pluginDir + "\\UI\\MAHealingView.xml", healing);
            SettingsController.RegisterSettingsWindow("Taunts", pluginDir + "\\UI\\MATauntsView.xml", taunt);
            SettingsController.RegisterSettingsWindow("Procs", pluginDir + "\\UI\\MAProcsView.xml", proc);

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistAbsoluteFist, AbsoluteFist, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistStrengthenKi, StrengthenKi, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistDisruptKi, DisruptKi, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistSmashingFist, SmashingFist, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistStrengthenSpirit, StrengthenSpirit, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistStingingFist, StingingFist, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcMartialArtistSelfReconstruction, SelfReconstruction, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistDebilitatingStrike, DebilitatingStrike, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistHealingMeditation, HealingMeditation, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistAttackLigaments, AttackLigaments, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcMartialArtistMedicinalRemedy, MedicinalRemedy, CombatActionPriority.Low);

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

            RegisterSpellProcessor(RelevantNanos.DamageTypeMelee, DamageTypeMelee);
            RegisterSpellProcessor(RelevantNanos.DamageTypeFire, DamageTypeFire);
            RegisterSpellProcessor(RelevantNanos.DamageTypeEnergy, DamageTypeEnergy);
            RegisterSpellProcessor(RelevantNanos.DamageTypeChemical, DamageTypeChemical);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CriticalIncreaseBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), GenericBuff);


            //Items
            RegisterItemProcessor(RelevantItems.TheWizdomOfHuzzum, RelevantItems.TheWizdomOfHuzzum, MartialArtsTeamHealAttack);
            RegisterItemProcessor(RelevantItems.TouchOfSaiFung, RelevantItems.TouchOfSaiFung, TouchOfSaiFung);
            RegisterItemProcessor(RelevantItems.Sappo, RelevantItems.Sappo, Sappo);

            PluginDirectory = pluginDir;
        }

        private void ProcView(object s, ButtonBase button)
        {
            if (_healingWindow != null && _healingWindow.IsValid)
            {
                if (_procView == null)
                    _procView = View.CreateFromXml(PluginDirectory + "\\UI\\MAProcsView.xml");

                if (!_healingWindow.Views.Contains(_procView))
                {
                    _healingWindow.AppendTab("Procs", _procView);
                }
            }
            else if (_buffWindow != null && _healingWindow.IsValid)
            {
                if (_procView == null)
                    _procView = View.CreateFromXml(PluginDirectory + "\\UI\\MAProcsView.xml");

                if (!_procWindow.Views.Contains(_procView))
                {
                    _procWindow.AppendTab("Procs", _procView);
                }
            }
            else if (_tauntWindow != null && _procWindow.IsValid)
            {
                if (_procView == null)
                    _procView = View.CreateFromXml(PluginDirectory + "\\UI\\MAProcsView.xml");

                if (!_tauntWindow.Views.Contains(_procView))
                {
                    _tauntWindow.AppendTab("Procs", _procView);
                }
            }
            else
            {
                _procWindow = Window.CreateFromXml("Procs", PluginDirectory + "\\UI\\MAProcsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                _procWindow.Show(true);
            }
        }

        private void BuffView(object s, ButtonBase button)
        {
            if (_healingWindow != null && _healingWindow.IsValid)
            {
                if (_buffView == null)
                    _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\MABuffsView.xml");

                if (!_healingWindow.Views.Contains(_buffView))
                {
                    _healingWindow.AppendTab("Buffs", _buffView);
                }
            }
            else if (_procWindow != null && _procWindow.IsValid)
            {
                if (_buffView == null)
                    _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\MABuffsView.xml");

                if (!_procWindow.Views.Contains(_buffView))
                {
                    _procWindow.AppendTab("Buffs", _buffView);
                }
            }
            else if (_tauntWindow != null && _procWindow.IsValid)
            {
                if (_buffView == null)
                    _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\MABuffsView.xml");

                if (!_tauntWindow.Views.Contains(_buffView))
                {
                    _tauntWindow.AppendTab("Buffs", _buffView);
                }
            }
            else
            {
                _buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\MABuffsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                _buffWindow.Show(true);
            }
        }

        private void HealingView(object s, ButtonBase button)
        {
            if (_buffWindow != null && _buffWindow.IsValid)
            {
                if (_healingView == null)
                    _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\MAHealingView.xml");

                if (!_buffWindow.Views.Contains(_healingView))
                {
                    _buffWindow.AppendTab("Healing", _healingView);
                }
            }
            else if (_procWindow != null && _procWindow.IsValid)
            {
                if (_healingView == null)
                    _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\MAHealingView.xml");

                if (!_procWindow.Views.Contains(_healingView))
                {
                    _procWindow.AppendTab("Healing", _healingView);
                }
            }
            else if (_tauntWindow != null && _tauntWindow.IsValid)
            {
                if (_healingView == null)
                    _healingView = View.CreateFromXml(PluginDirectory + "\\UI\\MAHealingView.xml");

                if (!_tauntWindow.Views.Contains(_healingView))
                {
                    _tauntWindow.AppendTab("Healing", _healingView);
                }
            }
            else
            {
                _healingWindow = Window.CreateFromXml("Healing", PluginDirectory + "\\UI\\MAHealingView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                _healingWindow.Show(true);
            }
        }

        private void TauntView(object s, ButtonBase button)
        {
            if (_buffWindow != null && _buffWindow.IsValid)
            {
                if (_tauntView == null)
                    _tauntView = View.CreateFromXml(PluginDirectory + "\\UI\\MATauntsView.xml");

                if (!_buffWindow.Views.Contains(_tauntView))
                {
                    _buffWindow.AppendTab("Taunts", _tauntView);
                }
            }
            else if (_healingWindow != null && _healingWindow.IsValid)
            {
                if (_tauntView == null)
                    _tauntView = View.CreateFromXml(PluginDirectory + "\\UI\\MATauntsView.xml");

                if (!_healingWindow.Views.Contains(_tauntView))
                {
                    _healingWindow.AppendTab("Taunts", _tauntView);
                }
            }
            else if (_procWindow != null && _procWindow.IsValid)
            {
                if (_tauntView == null)
                    _tauntView = View.CreateFromXml(PluginDirectory + "\\UI\\MATauntsView.xml");

                if (!_procWindow.Views.Contains(_tauntView))
                {
                    _procWindow.AppendTab("Taunts", _tauntView);
                }
            }
            else
            {
                _tauntWindow = Window.CreateFromXml("Taunts", PluginDirectory + "\\UI\\MATauntsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                _tauntWindow.Show(true);
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

                if (SettingsController.settingsWindow.FindView("ProcsView", out Button procView))
                {
                    procView.Tag = SettingsController.settingsWindow;
                    procView.Clicked = ProcView;
                }
            }

            base.OnUpdate(deltaTime);
        }

        private bool ZazenStance(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell) || !IsSettingEnabled("Zazen")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool DamageTypeEnergy(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DamageTypeSelection.Energy != (DamageTypeSelection)_settings["DamageTypeSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool DamageTypeFire(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DamageTypeSelection.Fire != (DamageTypeSelection)_settings["DamageTypeSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool DamageTypeMelee(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DamageTypeSelection.Melee != (DamageTypeSelection)_settings["DamageTypeSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool DamageTypeChemical(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DamageTypeSelection.Chemical != (DamageTypeSelection)_settings["DamageTypeSelection"].AsInt32()) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("OSTaunt") && Time.NormalTime > _singleTauntTick + 1)
            {
                List<SimpleChar> mobs = DynelManager.NPCs
                    .Where(c => c.IsAttacking && c.FightingTarget != null
                        && c.IsInLineOfSight
                        && !debuffTargetsToIgnore.Contains(c.Name)
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && IsNotFightingMe(c)
                        && IsAttackingUs(c)
                        && (c.FightingTarget.Profession != Profession.Enforcer
                                || c.FightingTarget.Profession != Profession.Soldier
                                || c.FightingTarget.Profession != Profession.MartialArtist))
                    .ToList();

                foreach (SimpleChar mob in mobs)
                {
                    if (mob != null)
                    {
                        _singleTauntTick = Time.NormalTime;
                        actionTarget.Target = mob;
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            if (!IsSettingEnabled("SingleTaunt")) { return false; }

            if (DynelManager.LocalPlayer.FightingTarget != null
                && (DynelManager.LocalPlayer.FightingTarget.Name == "Technomaster Sinuh"
                || DynelManager.LocalPlayer.FightingTarget.Name == "Collector"))
            {
                return true;
            }

            if (Time.NormalTime > _singleTaunt + 9)
            {
                if (fightingTarget != null)
                {
                    _singleTaunt = Time.NormalTime;
                    actionTarget.Target = fightingTarget;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
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

        #region Perks

        private bool AbsoluteFist(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.AbsoluteFist != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DisruptKi(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.DisruptKi != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool SmashingFist(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.SmashingFist != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool StingingFist(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.StingingFist != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool StrengthenKi(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.StrengthenKi != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool StrengthenSpirit(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.StrengthenSpirit != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool AttackLigaments(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.AttackLigaments != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DebilitatingStrike(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.DebilitatingStrike != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool HealingMeditation(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.HealingMeditation != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool MedicinalRemedy(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.MedicinalRemedy != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool SelfReconstruction(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.SelfReconstruction != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        public enum ProcType1Selection
        {
            AbsoluteFist, StrengthenKi, DisruptKi, SmashingFist, StrengthenSpirit, StingingFist
        }

        public enum ProcType2Selection
        {
            SelfReconstruction, DebilitatingStrike, HealingMeditation, AttackLigaments, MedicinalRemedy
        }
        public enum DamageTypeSelection
        {
            Melee, Fire, Energy, Chemical
        }

        private static class RelevantNanos
        {
            public const int FistsOfTheWinterFlame = 269470;
            public const int LimboMastery = 28894;
            public const int ReduceInertia = 28903;
            public static int[] TeamCritBuffs = { 160574, 160575, 160576 };
            public static int[] Taunts = { 301936, 100214, 100216, 100215, 100217, 28866 };
            public static int[] DamageTypeMelee = { 270798, 28892 };
            public static int[] DamageTypeFire = { 81827, 81824, 28876 };
            public static int[] DamageTypeEnergy = { 81825, 81823, 81826, 81829 };
            public static int[] DamageTypeChemical = { 81822, 81830 };
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
