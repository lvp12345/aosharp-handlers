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

namespace CombatHandler.Enf
{
    class EnfCombatHandler : GenericCombatHandler
    { 
        private static string PluginDirectory;

        private static Window _buffWindow;
        private static Window _tauntWindow;
        private static Window _procWindow;

        private static View _buffView;
        private static View _tauntView;
        private static View _procView;

        private static double _absorbs;
        private static double _aoeTaunt;
        private static double _aoeTauntOst;
        private static double _singleTaunt;
        private static double _singleTauntTick;

        private static double _ncuUpdateTime;

        public EnfCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.RagingBlow);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.ViolationBuffer);

            _settings.AddVariable("SingleTauntsSelection", (int)SingleTauntsSelection.None);

            _settings.AddVariable("RaidTaunt", false);
            _settings.AddVariable("AOETaunt", false);
            _settings.AddVariable("Absorbs", false);
            _settings.AddVariable("TauntProc", false);

            _settings.AddVariable("TrollForm", false);

            _settings.AddVariable("OST", false);

            RegisterSettingsWindow("Enforcer Handler", "EnforcerSettingsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcEnforcerAirOfHatred, AirOfHatred, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerBustKneecaps, BustKneecaps, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerIgnorePain, IgnorePain, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerInspireIre, InspireIre, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerInspireRage, InspireRage, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcEnforcerRagingBlow, RagingBlow, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerShieldOfTheOgre, ShieldOfTheOgre, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerShrugOffHits, ShrugOffHits, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerTearLigaments, TearLigaments, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerVileRage, VileRage, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerViolationBuffer, ViolationBuffer, CombatActionPriority.Low);

            //Troll Form
            RegisterPerkProcessor(PerkHash.TrollForm, TrollForm);

            //Spells (Im not sure the spell lines are up to date to support the full line of SL mongos)
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HPBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MongoBuff).OrderByStackingOrder(), AoeTaunt, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.SingleTargetTaunt, SingleTargetTaunt, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageChangeBuffs).OrderByStackingOrder(), DamageChangeBuff);
            RegisterSpellProcessor(RelevantNanos.FortifyBuffs, Fortify, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EnforcerTauntProcs).OrderByStackingOrder(), TauntProc);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(RelevantNanos.Melee1HB, Melee1HBBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.Melee1HE, Melee1HEBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.Melee2HE, Melee2HEBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.Melee2HB, Melee2HBBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.MeleePierce, MeleePierceBuffWeapon);
            RegisterSpellProcessor(RelevantNanos.MeleeEnergy, MeleeEnergyBuffWeapon);

            //Team buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), MeleeBuff);
            RegisterSpellProcessor(RelevantNanos.TargetedDamageShields, TeamBuff);
            RegisterSpellProcessor(RelevantNanos.TargetedHpBuff, TeamBuff);
            RegisterSpellProcessor(RelevantNanos.FOCUSED_ANGER, GenericBuff);

            //if (TauntTools.CanUseTauntTool())
            //{
            //    Item tauntTool = TauntTools.GetBestTauntTool();
            //    RegisterItemProcessor(tauntTool.LowId, tauntTool.HighId, TauntTool);
            //}

            PluginDirectory = pluginDir;
        }

        public Window[] _windows => new Window[] { _buffWindow, _tauntWindow, _procWindow };

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
        private void BuffView(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //Cannot stop Multi-Tabs. Easy fix would be correct naming of views to reference against WindowOptions - options.Name
                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\EnforcerBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "EnforcerBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "EnforcerBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }

        private void TauntView(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //Cannot stop Multi-Tabs. Easy fix would be correct naming of views to reference against WindowOptions - options.Name
                _tauntView = View.CreateFromXml(PluginDirectory + "\\UI\\EnforcerTauntsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Taunts", XmlViewName = "EnforcerTauntsView" }, _tauntView);
            }
            else if (_tauntWindow == null || (_tauntWindow != null && !_tauntWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_tauntWindow, PluginDir, new WindowOptions() { Name = "Taunts", XmlViewName = "EnforcerTauntsView" }, _tauntView, out var container);
                _tauntWindow = container;
            }
        }
        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\EnforcerProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "EnforcerProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "EnforcerProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            if (Time.NormalTime > _ncuUpdateTime + 0.5f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
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
                    procView.Clicked = HandleProcViewClick;
                }
            }
        }


        #region Perks

        private bool InspireRage(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.InspireRage != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool RagingBlow(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.RagingBlow != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool ShieldOfTheOgre(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.Shieldoftheogre != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool TearLigaments(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.TearLigaments != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool VileRage(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.VileRage != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool VortexOfHate(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.VortexofHate != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool AirOfHatred(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Airofhatred != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool BustKneecaps(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.BustKneecaps != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool IgnorePain(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.IgnorePain != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool InspireIre(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.InspireIre != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool ShrugOffHits(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.ShrugOffHits != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool ViolationBuffer(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.ViolationBuffer != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion


        private bool DamageChangeBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(HasBuffNanoLine(NanoLine.DamageChangeBuffs, DynelManager.LocalPlayer)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (SingleTauntsSelection.OS == (SingleTauntsSelection)_settings["SingleTauntsSelection"].AsInt32() && Time.NormalTime > _singleTauntTick + 1)
            {
                List<SimpleChar> mobs = DynelManager.NPCs
                    .Where(c => c.IsAttacking && c.FightingTarget != null
                        && c.IsInLineOfSight
                        && !debuffOSTargetsToIgnore.Contains(c.Name)
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

            if (IsSettingEnabled("RaidTaunt"))
            {
                SimpleChar mob = DynelManager.NPCs
                    .Where(c => c.Name == "Azdaja the Joyous" 
                    || c.Name == "The Awoken Nightmare, Phobettor"
                    || c.Name == "Abmouth Supremus"
                    || c.Name == "Technomaster Sinuh"
                    || c.Name == "Collector")
                    .FirstOrDefault();

                if (mob != null)
                {
                    actionTarget.Target = mob;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }


            if (IsSettingEnabled("AOETaunt") && Time.NormalTime > _singleTaunt + 6)
            {
                if (fightingTarget != null)
                {
                    _singleTaunt = Time.NormalTime;
                    actionTarget.Target = fightingTarget;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            if (!IsSettingEnabled("AOETaunt") && SingleTauntsSelection.Single == (SingleTauntsSelection)_settings["SingleTauntsSelection"].AsInt32())
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

        private bool TrollForm(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("TrollForm")) { return false; }

            return TrollFormPerk(perk, fightingTarget, ref actionTarget);
        }

        private bool Melee1HEBuffWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Edged1H);
        }

        private bool Melee1HBBuffWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Blunt1H);
        }

        private bool Melee2HEBuffWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Edged2H);
        }

        private bool Melee2HBBuffWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Blunt2H);
        }

        private bool MeleePierceBuffWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Piercing);
        }

        private bool MeleeEnergyBuffWeapon(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.MeleeEnergy);
        }

        private bool Fortify(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (!CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.FightingTarget != null 
                && DynelManager.LocalPlayer.FightingTarget.Name == "Technomaster Sinuh") { return false; }

            if (DynelManager.LocalPlayer.Buffs.Any(Buff => Buff.Id == RelevantNanos.BIO_COCOON_BUFF)) { return false; }

            List<Spell> mongobuff = Spell.List.Where(x => x.Nanoline == NanoLine.MongoBuff).OrderBy(x => x.StackingOrder).ToList();

            if (IsSettingEnabled("OST") && !mongobuff.FirstOrDefault().IsReady && Time.NormalTime > _absorbs + 8)
            {
                _absorbs = Time.NormalTime;
                return true;
            }

            if (!IsSettingEnabled("Absorbs")) { return false; }

            if (!IsSettingEnabled("OST") && fightingTarget != null && Time.NormalTime > _absorbs + 15)
            {
                _absorbs = Time.NormalTime;
                return true;
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool TauntProc(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell) || !IsSettingEnabled("TauntProc")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool AoeTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (DynelManager.LocalPlayer.FightingTarget != null 
                && DynelManager.LocalPlayer.FightingTarget.Name == "Technomaster Sinuh") { return false; }

            if (!CanCast(spell)) { return false; }

            if (IsSettingEnabled("OST"))
            {
                if (Time.NormalTime > _aoeTauntOst + 7) // 10
                {
                    _aoeTauntOst = Time.NormalTime;
                    return true;
                }
            }

            if (!IsSettingEnabled("AOETaunt") || fightingTarget == null) { return false; }

            if (!IsSettingEnabled("Absorbs") && Time.NormalTime > _aoeTaunt + 9)
            {
                _aoeTaunt = Time.NormalTime;
                return true;
            }

            List<Spell> absorbbuff = Spell.List.Where(x => x.Nanoline == NanoLine.AbsorbACBuff).OrderBy(x => x.StackingOrder).ToList();

            if (IsSettingEnabled("Absorbs") && absorbbuff.FirstOrDefault() != null && Time.NormalTime > _aoeTaunt + 9)
            {
                _aoeTaunt = Time.NormalTime;
                return true;
            }

            return false;
        }

        private bool ShouldBeTaunted(SimpleChar target)
        {
            return !target.IsPlayer && !target.IsPet && target.IsValid && target.IsInLineOfSight;
        }

        public enum ProcType1Selection
        {
            VortexofHate, RagingBlow, Shieldoftheogre, InspireRage, TearLigaments, VileRage
        }

        public enum ProcType2Selection
        {
            ViolationBuffer, InspireIre, Airofhatred, ShrugOffHits, BustKneecaps, IgnorePain
        }
        public enum SingleTauntsSelection
        {
            None, Single, OS
        }

        private static class RelevantNanos
        {
            public static readonly int[] SingleTargetTaunt = { 275014, 223123, 223121, 223119, 223117, 223115, 100209, 100210, 100212, 100211, 100213 };
            public static readonly int[] Melee1HB = { 202846, 202844, 202842, 29630, 202840, 29644 };
            public static readonly int[] Melee2HB = { 202856, 202854, 202852, 29630, 202850, 29644, 202848 };
            public static readonly int[] Melee1HE = { 202818, 202816, 202793, 202791, 202774, 202739, 202776 };
            public static readonly int[] Melee2HE = { 202838, 202836, 202834, 202832, 202830, 202828, 202826 };
            public static readonly int[] MeleePierce = { 202858, 202860, 202862, 202864, 202866, 202868, 202870 };
            public static readonly int[] MeleeEnergy = { 203215, 203207, 203209, 203211, 203213 };
            public static readonly int[] TargetedHpBuff = { 273629, 95708, 95700, 95701, 95702, 95704, 95706, 95707 };
            public static readonly int[] FortifyBuffs = { 273320, 270350, 117686, 117688, 117682, 117687, 117685, 117684, 117683, 117680, 117681 };
            public static readonly Spell[] TargetedDamageShields = Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder().Where(spell => spell.Id != ICE_BURN).ToArray();
            public const int MONGO_KRAKEN = 273322;
            public const int MONGO_DEMOLISH = 270786;
            public const int FOCUSED_ANGER = 29641;
            public const int IMPROVED_ESSENCE_OF_BEHEMOTH = 273629;
            public const int CORUSCATING_SCREEN = 55751;
            public const int ICE_BURN = 269460;
            public const int BIO_COCOON_BUFF = 209802;
        }
    }
}