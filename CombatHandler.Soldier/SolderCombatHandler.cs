using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using CombatHandler.Generic;
using AOSharp.Core.UI;
using System.Linq;
using CombatHandler;
using AOSharp.Common.GameData.UI;

namespace Desu
{
    public class SoldCombathandler : GenericCombatHandler
    {
        public const double singletauntrefresh = 8f;
        private double _singletauntused;

        public static Window buffWindow;
        public static Window tauntWindow;

        public static string PluginDirectory;

        public SoldCombathandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("SingleTaunt", false);

            settings.AddVariable("Burst", false);
            settings.AddVariable("BurstTeam", false);

            settings.AddVariable("Init", false);
            settings.AddVariable("InitTeam", false);

            settings.AddVariable("NotumGrenades", false);

            //settings.AddVariable("DamageTeam", false);

            RegisterSettingsWindow("Soldier Handler", "SoldierSettingsView.xml");

            RegisterSettingsWindow("Buffs", "SoldierBuffsView.xml");
            RegisterSettingsWindow("Taunts", "SoldierTauntsView.xml");

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcSoldierGrazeJugularVein, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcSoldierFuriousAmmunition, LEProc);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ReflectShield).OrderByStackingOrder(), AugmentedMirrorShieldMKV);
            RegisterSpellProcessor(RelevantNanos.SolDrainHeal, SolDrainHeal);
            RegisterSpellProcessor(RelevantNanos.TauntBuffs, SingleTargetTaunt);
            //RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(), DamageTeam);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HPBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SiphonBox683).OrderByStackingOrder(), NotumGrenades);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadowlandReflectBase).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierFullAutoBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TotalFocus).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierShotgunBuff).OrderByStackingOrder(), ShotgunBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HeavyWeaponsBuffs).OrderByStackingOrder(), HeavyWeaponBuff);

            RegisterSpellProcessor(RelevantNanos.ArBuffs, ARBuff);
            RegisterSpellProcessor(RelevantNanos.HeavyComp, HeavyCompBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierDamageBase).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AAOBuffs).OrderByStackingOrder(), TeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolMasteryBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.BurstBuff).OrderByStackingOrder(), BurstBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);

            // Needs work for 2nd tanking Abmouth and Ayjous
            //if (TauntTools.CanTauntTool())
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
                buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\SoldierBuffsView.xml",
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
                tauntWindow = Window.CreateFromXml("Taunts", PluginDirectory + "\\UI\\SoldierTauntsView.xml",
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

        #region Instanced Logic

        private bool BurstBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("BurstTeam"))
            {
                if (DynelManager.LocalPlayer.IsInTeam())
                {
                    SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => SpellChecksOther(spell, spell.Nanoline, c))
                    .Where(c => c.SpecialAttacks.Contains(SpecialAttack.Burst))
                    .FirstOrDefault();

                    if (teamMemberWithoutBuff != null)
                    {
                        actionTarget.Target = teamMemberWithoutBuff;
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            if (!IsSettingEnabled("Burst")) { return false; }

            if (!DynelManager.LocalPlayer.SpecialAttacks.Contains(SpecialAttack.Burst)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool HeavyCompBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => SpellChecksOther(spell, spell.Nanoline, c))
                    .Where(c => GetWieldedWeapons(c).HasFlag(CharacterWieldedWeapon.Grenade) && c.Profession != Profession.Engineer)
                    .Where(c => GetWieldedWeapons(c).HasFlag(CharacterWieldedWeapon.Smg))
                    .Where(c => GetWieldedWeapons(c).HasFlag(CharacterWieldedWeapon.Shotgun))
                    .Where(c => GetWieldedWeapons(c).HasFlag(CharacterWieldedWeapon.AssaultRifle))
                    .Where(c => !c.Buffs.Contains(NanoLine.FixerSuppressorBuff))
                    .Where(c => !c.Buffs.Contains(NanoLine.AssaultRifleBuffs))
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    if (teamMemberWithoutBuff.Identity == DynelManager.LocalPlayer.Identity
                        && GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(CharacterWieldedWeapon.AssaultRifle)) { return false; }

                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Smg)
                                || BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Shotgun)
                                || BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade);
        }

        private bool HeavyWeaponBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.HeavyWeapons);
        }

        private bool ShotgunBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Shotgun);
        }

        private bool ARBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.AssaultRifle);
        }

        private bool NotumGrenades(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("NotumGrenades")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("SingleTaunt")) { return false; }

            if (DynelManager.LocalPlayer.FightingTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.NanoPercent < 30) { return false; }

            if (fightingTarget?.FightingTarget != null && fightingTarget?.FightingTarget.Profession == Profession.Enforcer) { return false; }

            if (IsNotFightingMe(fightingTarget))
            {
                actionTarget.Target = fightingTarget;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            if (Time.NormalTime > _singletauntused + singletauntrefresh)
            {
                _singletauntused = Time.NormalTime;
                actionTarget.Target = fightingTarget;
                actionTarget.ShouldSetTarget = true;
                return true;
            }

            return false;
        }

        private bool SolDrainHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.FightingTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent <= 40) { return true; }

            return false;
        }

        private bool AugmentedMirrorShieldMKV(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.FightingTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent <= 85) { return true; }

            return false;
        }

        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("InitTeam"))
            {
                if (DynelManager.LocalPlayer.IsInTeam())
                {
                    SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                        .Where(c => c.Profession != Profession.Doctor && c.Profession != Profession.NanoTechnician)
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

            if (!IsSettingEnabled("Init")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Misc

        private static class RelevantNanos
        {
            public static readonly int[] SolDrainHeal = { 29241, 301897 };
            public static readonly int[] TauntBuffs = { 223209, 223207, 223205, 223203, 223201, 29242, 100207,
            29218, 100205, 100206, 100208, 29228};
            public const int HeavyComp = 269482;
            public static readonly int[] ArBuffs = { 275027, 203119, 29220, 203121 };
        }

        private static class RelevantItems
        {
            public const int DreadlochEnduranceBooster = 267168;
            public const int DreadlochEnduranceBoosterNanomageEdition = 267167;
        }

        #endregion
    }
}
