using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System.Linq;
using System;
using AOSharp.Common.GameData.UI;
using AOSharp.Core.IPC;
using System.Threading.Tasks;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System.Threading;
using SmokeLounge.AOtomation.Messaging.Messages;
using System.Collections.Generic;
using AOSharp.Core.Inventory;
using CombatHandler.Generic;

namespace CombatHandler.Fixer
{
    public class FixerCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private static Window _buffWindow;
        private static Window _debuffWindow;
        private static Window _procWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _procView;

        private double _lastBackArmorCheckTime = Time.NormalTime;

        private static double _ncuUpdateTime;

        public FixerCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("RKRunspeed", false);
            _settings.AddVariable("RKRunspeedTeam", false);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.LucksCalamity);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.BootlegRemedies);

            _settings.AddVariable("SLRunspeed", false);

            _settings.AddVariable("EvasionDebuff", false);

            _settings.AddVariable("ShadowwebSpinner", false);
            _settings.AddVariable("GridArmor", false);

            _settings.AddVariable("LongHoT", false);
            _settings.AddVariable("ShortHoT", false);
            _settings.AddVariable("LongHoTTeam", false);
            _settings.AddVariable("ShortHoTTeam", false);

            RegisterSettingsWindow("Fixer Handler", "FixerSettingsView.xml");

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcFixerLucksCalamity, LucksCalamity, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerDirtyTricks, DirtyTricks, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerEscapeTheSystem, EscapeTheSystem, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerIntenseMetabolism, IntenseMetabolism, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerFishInABarrel, FishInABarrel, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcFixerBootlegRemedies, BootlegRemedies, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerSlipThemAMickey, SlipThemAMickey, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerBendingTheRules, BendingTheRules, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerBackyardBandages, BackyardBandages, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerFightingChance, FightingChance, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerContaminatedBullets, ContaminatedBullets, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcFixerUndergroundSutures, UndergroundSutures, CombatActionPriority.Low);

            ////Luck's Calamity is missing from PerkHash list
            //PerkAction lucksCalamity = PerkAction.List.Where(action => action.Name.Equals("Luck's Calamity")).FirstOrDefault();
            //if (lucksCalamity != null) {
            //    RegisterPerkProcessor(lucksCalamity.Hash, LEProc);
            //}
            
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
        public Window[] _windows => new Window[] { _buffWindow, _debuffWindow, _procWindow };

        public static void OnRemainingNCUMessage(int sender, IPCMessage msg)
        {
            try
            {
                if (Game.IsZoning)
                    return;

                RemainingNCUMessage ncuMessage = (RemainingNCUMessage)msg;
                SettingsController.RemainingNCU[ncuMessage.Character] = ncuMessage.RemainingNCU;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e);
            }
        }

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //Cannot stop Multi-Tabs. Easy fix would be correct naming of views to reference against WindowOptions - options.Name
                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\FixerBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "FixerBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "FixerBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }

        private void HandleDebuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //Cannot stop Multi-Tabs. Easy fix would be correct naming of views to reference against WindowOptions - options.Name
                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\FixerDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "FixerDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "FixerDebuffsView" }, _debuffView, out var container);
                _debuffWindow = container;
            }
        }
        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\FixerProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "FixerProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "FixerProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            EquipBackArmor();

            if (Time.NormalTime > _ncuUpdateTime + 0.5f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }

            if (_settings["RKRunspeed"].AsBool() && _settings["SLRunspeed"].AsBool())
            {
                _settings["RKRunspeed"] = false;
                _settings["SLRunspeed"] = false;

                Chat.WriteLine("Only activate one Runspeed option.");
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                {
                    buffView.Tag = SettingsController.settingsWindow;
                    buffView.Clicked = HandleBuffViewClick;
                }

                if (SettingsController.settingsWindow.FindView("DebuffsView", out Button debuffView))
                {
                    debuffView.Tag = SettingsController.settingsWindow;
                    debuffView.Clicked = HandleDebuffViewClick;
                }

                if (SettingsController.settingsWindow.FindView("ProcsView", out Button procView))
                {
                    procView.Tag = SettingsController.settingsWindow;
                    procView.Clicked = HandleProcViewClick;
                }
            }
        }

        #region Perks


        private bool LucksCalamity(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.LucksCalamity != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool IntenseMetabolism(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.IntenseMetabolism != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool FishInABarrel(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.FishInABarrel != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool EscapeTheSystem(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.EscapeTheSystem != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool DirtyTricks(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.DirtyTricks != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool BootlegRemedies(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.BootlegRemedies != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool BendingTheRules(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.BendingTheRules != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool BackyardBandages(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.BackyardBandages != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool ContaminatedBullets(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.ContaminatedBullets != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool FightingChance(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.FightingChance != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool SlipThemAMickey(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.SlipThemAMickey != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool UndergroundSutures(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.UndergroundSutures != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }


        #endregion

        private bool NCUBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HasBuffNanoLine(NanoLine.FixerNCUBuff, DynelManager.LocalPlayer)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool LongHotBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")
                || fightingTarget != null || !CanCast(spell) || GenericCombatHandler.RelevantNanos.IgnoreNanos.Contains(spell.Id)) { return false; }

            if (IsSettingEnabled("LongHoTTeam"))
            {
                //idk
                //if (fightingTarget != null || !CanCast(spell) || GenericCombatHandler.RelevantNanos.IgnoreNanos.Contains(spell.Id)) { return false; }

                if (DynelManager.LocalPlayer.IsInTeam())
                {
                    if (DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                                && SpellChecksOther(spell, spell.Nanoline, c))
                        .Any())
                    {
                        actionTarget.Target = DynelManager.Characters
                            .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance)
                                && SpellChecksOther(spell, spell.Nanoline, c))
                            .FirstOrDefault();

                        if (actionTarget.Target != null)
                        {
                            actionTarget.ShouldSetTarget = true;
                            return true;
                        }
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
            if (!IsSettingEnabled("Buffing")
                || fightingTarget != null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                actionTarget.Target = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .FirstOrDefault();

                if (actionTarget.Target != null && SpellChecksOther(spell, spell.Nanoline, actionTarget.Target))
                {
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        private bool GsfBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") || IsInsideInnerSanctum()) { return false; }

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
            if (!IsSettingEnabled("Buffing") || IsInsideInnerSanctum()) { return false; }

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
            if (!IsSettingEnabled("Buffing") 
                || !IsSettingEnabled("GridArmor") || !CanCast(spell)) { return false; }

            return !Inventory.Items.Any(x => RelevantItems.GRID_ARMORS.Contains(x.HighId));
        }

        private bool ShadowwebSpinner(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing") 
                || !IsSettingEnabled("ShadowwebSpinner") || !CanCast(spell)) { return false; }

            return !Inventory.Items.Any(x => RelevantItems.SHADOWWEB_SPINNERS.Contains(x.HighId));
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
            public static readonly Spell[] TEAM_LONG_HOTS = Spell.GetSpellsForNanoline(NanoLine.FixerLongHoT).OrderByStackingOrder().Where(spell => spell.Id != GREATER_PRESERVATION_MATRIX).ToArray();
        }

        private static class RelevantItems
        {
            public static readonly int[] GRID_ARMORS = { 155172, 155173, 155174, 155150 };
            public static readonly int[] SHADOWWEB_SPINNERS = { 273350, 224400, 224399, 224398, 224397, 224396, 224395, 224394, 224393, 224392, 224390 };
        }

        public enum ProcType1Selection
        {
            LucksCalamity, DirtyTricks, EscapeTheSystem, IntenseMetabolism, FishInABarrel
        }

        public enum ProcType2Selection
        {
            BootlegRemedies, SlipThemAMickey, BendingTheRules, BackyardBandages, FightingChance, ContaminatedBullets, UndergroundSutures
        }
    }
}
