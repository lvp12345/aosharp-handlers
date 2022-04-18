using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using AOSharp.Core.UI.Options;
using CombatHandler;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Desu
{
    class EnfCombatHandler : GenericCombatHandler
    {
        public static double _absorbs;
        public static double _aoeTaunt;
        public static double _aoeTauntOst;
        public static double _singleTaunt;
        public static double _singleTauntTick;

        public static Window buffWindow;
        public static Window tauntWindow;

        public static string PluginDirectory;

        public EnfCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("SingleTaunt", false);
            settings.AddVariable("OSTaunt", false);
            settings.AddVariable("AOETaunt", false);
            settings.AddVariable("Absorbs", false);
            //settings.AddVariable("UseTauntTool", true);

            settings.AddVariable("OST", false);

            RegisterSettingsWindow("Enforcer Handler", "EnforcerSettingsView.xml");

            RegisterSettingsWindow("Taunts", "EnforcerTauntsView.xml");
            RegisterSettingsWindow("Buffs", "EnforcerBuffsView.xml");


            //-------------LE procs-------------
            RegisterPerkProcessor(PerkHash.LEProcEnforcerVortexOfHate, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcEnforcerInspireIre, LEProc, CombatActionPriority.Low);

            //Spells (Im not sure the spell lines are up to date to support the full line of SL mongos)
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HPBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MongoBuff).OrderByStackingOrder(), AoeTaunt, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.SingleTargetTaunt, SingleTargetTaunt, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageChangeBuffs).OrderByStackingOrder(), DamageChangeBuff);
            RegisterSpellProcessor(RelevantNanos.FortifyBuffs, Fortify, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EnforcerTauntProcs).OrderByStackingOrder(), GenericBuff);
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

        private void BuffView(object s, ButtonBase button)
        {
            if (tauntWindow != null && tauntWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", tauntWindow);
            }
            else
            {
                buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\EnforcerBuffsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                buffWindow.Show(true);
            }
        }

        private void TauntView(object s, ButtonBase button)
        {
            if (buffWindow != null && buffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Taunts", buffWindow);
            }
            else
            {
                tauntWindow = Window.CreateFromXml("Taunts", PluginDirectory + "\\UI\\EnforcerTauntsView.xml",
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
        private bool DamageChangeBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if(HasBuffNanoLine(NanoLine.DamageChangeBuffs, DynelManager.LocalPlayer)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("OSTaunt") && Time.NormalTime > _singleTauntTick + 1)
            {
                List<SimpleChar> mobs = DynelManager.NPCs
                    .Where(c => c.IsAttacking && c.FightingTarget != null
                        && c.IsInLineOfSight
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

            if (IsSettingEnabled("AOETaunt") && Time.NormalTime > _singleTaunt + 9)
            {
                if (fightingTarget != null)
                {
                    _singleTaunt = Time.NormalTime;
                    actionTarget.Target = fightingTarget;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            if (!IsSettingEnabled("AOETaunt"))
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
            if (!CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.FightingTarget != null 
                && DynelManager.LocalPlayer.FightingTarget.Name == "Technomaster Sinuh") { return false; }

            if (DynelManager.LocalPlayer.Buffs.Any(Buff => Buff.Identity.Instance == RelevantNanos.BIO_COCOON_BUFF)) { return false; }

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

        private bool AoeTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.FightingTarget != null 
                && DynelManager.LocalPlayer.FightingTarget.Name == "Technomaster Sinuh") { return false; }

            if (!CanCast(spell)) { return false; }

            if (IsSettingEnabled("OST"))
            {
                if (Time.NormalTime > _aoeTauntOst + 10)
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
            public static readonly Spell[] TargetedDamageShields = Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder().Where(spell => spell.Identity.Instance != ICE_BURN).ToArray();
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