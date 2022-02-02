using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using CombatHandler.Generic;

namespace Desu
{
    public class ShadeCombatHandler : GenericCombatHandler
    {
        private const int MissingHealthCombatAbortPercentage = 30;

        private static bool ShadeSiphon;

        public ShadeCombatHandler(string pluginDir) : base(pluginDir)
        {
            settings.AddVariable("Runspeed", false);
            settings.AddVariable("RunspeedTeam", false);

            settings.AddVariable("InitDebuffProc", false);
            settings.AddVariable("DamageProc", false);
            settings.AddVariable("DoTProc", false);
            settings.AddVariable("StunProc", false);

            settings.AddVariable("HealthDrain", false);
            settings.AddVariable("SpiritSiphon", false);

            RegisterSettingsWindow("Shade Handler", "ShadeSettingsView.xml");

            RegisterPerkProcessor(PerkHash.LEProcShadeSiphonBeing, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcShadeBlackheart, LEProc);

            //Perks
            RelevantPerks.SpiritPhylactery.ForEach(p => RegisterPerkProcessor(p, SpiritPhylacteryPerk));
            RelevantPerks.TotemicRites.ForEach(p => RegisterPerkProcessor(p, TotemicRitesPerk));
            RelevantPerks.PiercingMastery.ForEach(p => RegisterPerkProcessor(p, PiercingMasteryPerk));

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EmergencySneak).OrderByStackingOrder(), SmokeBombNano, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NemesisNanoPrograms).OrderByStackingOrder(), ShadesCaressNano, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealthDrain).OrderByStackingOrder(), HealthDrainNano);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SpiritDrain).OrderByStackingOrder(), SpiritSiphonNano);

            //Items
            RegisterItemProcessor(RelevantItems.Tattoo, RelevantItems.Tattoo, TattooItem, CombatActionPriority.High);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AgilityBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ConcealmentBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MultiwieldBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtsBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadePiercingBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SneakAttackBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.WeaponEffectAdd_On2).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AADBuffs).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(RelevantNanos.ShadeDmgProc, DamageProc);
            RegisterSpellProcessor(RelevantNanos.ShadeStunProc, StunProc);
            RegisterSpellProcessor(RelevantNanos.ShadeInitDebuffProc, InitDebuffProc);
            RegisterSpellProcessor(RelevantNanos.ShadeDotProc, DoTProc);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RunspeedBuffs).OrderByStackingOrder(), FasterThanYourShadow);
        }

        private bool InitDebuffProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return ToggledBuff("InitDebuffProc", spell, fightingtarget, ref actiontarget);
        }

        private bool DamageProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return ToggledBuff("DamageProc", spell, fightingtarget, ref actiontarget);
        }
        private bool DoTProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return ToggledBuff("DoTProc", spell, fightingtarget, ref actiontarget);
        }
        private bool StunProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return ToggledBuff("StunProc", spell, fightingtarget, ref actiontarget);
        }

        private bool ShadesCaressNano(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!DynelManager.LocalPlayer.IsAttacking || fightingtarget == null) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent <= 50 && fightingtarget.HealthPercent > 5) { return true; }

            return false;
        }

        protected bool FTYSTeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => !c.Buffs.Contains(RelevantNanos.EVASION_BUFFS))
                    .Where(c => SpellChecksOther(spell, c))
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

        private bool FasterThanYourShadow(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (IsSettingEnabled("RunspeedTeam"))
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.RK_RUN_BUFFS))
                {
                    CancelBuffs(RelevantNanos.RK_RUN_BUFFS);
                }

                return FTYSTeamBuff(spell, fightingTarget, ref actionTarget);
            }

            if (IsSettingEnabled("Runspeed"))
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.RK_RUN_BUFFS))
                {
                    CancelBuffs(RelevantNanos.RK_RUN_BUFFS);
                }

                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool TattooItem(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            // don't use if BM is locked (we will add this dynamically later)
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BiologicalMetamorphosis)) { return false; }

            // don't use if we're above 40%
            if (DynelManager.LocalPlayer.HealthPercent > 40) { return false; }

            // don't use if nothing is fighting us
            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            // don't use if we have another major absorb (example: nanomage booster) running
            // we could check remaining absorb stat to be slightly more effective
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)) { return false; }

            // don't use if our fighting target has caress running
            if (fightingtarget.Buffs.Contains(275242)) { return false; }

            return true;
        }

        private bool SmokeBombNano(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = false;

            if (DynelManager.LocalPlayer.HealthPercent <= MissingHealthCombatAbortPercentage) { return true; }

            return false;
        }

        private bool SpiritSiphonNano(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("SpiritSiphon")) { return false; }

            if (fightingtarget == null && ShadeSiphon)
            {
                ShadeSiphon = false;
            }

            if (!DynelManager.LocalPlayer.IsAttacking) { return false; }

            if (DynelManager.LocalPlayer.Nano < spell.Cost) { return false; }

            if (fightingtarget != null && DynelManager.LocalPlayer.HealthPercent <= 20)
            {
                if (!ShadeSiphon)
                {
                    ShadeSiphon = true;
                    return true;
                }
            }

            return false;
        }

        private bool HealthDrainNano(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Nano < spell.Cost) { return false; }

            if (!DynelManager.LocalPlayer.IsAttacking) { return false; }

            // if we have caress, save enough nano to use it
            if (Spell.Find(RelevantNanos.ShadesCaress, out Spell caress))
            {
                if (DynelManager.LocalPlayer.Nano - spell.Cost < caress.Cost) { return false; }
            }

            // only use it for dps if we have plenty of nano
            if (IsSettingEnabled("HealthDrain") && DynelManager.LocalPlayer.NanoPercent > 80) { return true; }

            // otherwise save it for if our health starts to drop
            if (DynelManager.LocalPlayer.HealthPercent >= 85) { return false; }

            return true;
        }

        private bool PiercingMasteryPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            //Don't PM if there are TR/SP chains in progress
            if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (RelevantPerks.TotemicRites.Contains(action.Hash) || RelevantPerks.SpiritPhylactery.Contains(action.Hash)))) { return false; }

            if (!(PerkAction.Find(PerkHash.Stab, out PerkAction stab) && PerkAction.Find(PerkHash.DoubleStab, out PerkAction doubleStab)))
                return true;

            if (perkAction.Hash == PerkHash.Perforate)
            {
                if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (action == stab || action == doubleStab))) { return false; }
            }

            if (!(PerkAction.Find(PerkHash.Stab, out PerkAction perforate) && PerkAction.Find(PerkHash.DoubleStab, out PerkAction lacerate))) { return true; }

            if (perkAction.Hash == PerkHash.Impale)
            {
                if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (action == stab || action == doubleStab || action == perforate || action == lacerate))) { return false; }
            }

            return true;
        }

        private bool SpiritPhylacteryPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            //Don't SP if there are TR/PM chains in progress
            if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (RelevantPerks.TotemicRites.Contains(action.Hash) || RelevantPerks.PiercingMastery.Contains(action.Hash)))) { return false; }

            return true;
        }

        private bool TotemicRitesPerk(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            //Don't TR if there are SP/PM chains in progress
            if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (RelevantPerks.SpiritPhylactery.Contains(action.Hash) || RelevantPerks.PiercingMastery.Contains(action.Hash)))) { return false; }

            return true;
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            if (settings["InitDebuffProc"].AsBool() && settings["DamageProc"].AsBool())
            {
                settings["InitDebuffProc"] = false;
                settings["DamageProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }
            if (settings["InitDebuffProc"].AsBool() && settings["DoTProc"].AsBool())
            {
                settings["InitDebuffProc"] = false;
                settings["DoTProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }
            if (settings["InitDebuffProc"].AsBool() && settings["StunProc"].AsBool())
            {
                settings["InitDebuffProc"] = false;
                settings["StunProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }
            if (settings["DamageProc"].AsBool() && settings["StunProc"].AsBool())
            {
                settings["DamageProc"] = false;
                settings["StunProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }
            if (settings["DamageProc"].AsBool() && settings["DoTProc"].AsBool())
            {
                settings["DamageProc"] = false;
                settings["DoTProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }
            if (settings["StunProc"].AsBool() && settings["DoTProc"].AsBool())
            {
                settings["StunProc"] = false;
                settings["DoTProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }

            if (!IsSettingEnabled("Runspeed") && !IsSettingEnabled("RunspeedTeam"))
            {
                CancelBuffs(RelevantNanos.FasterThanYourShadow);
            }
            if (!IsSettingEnabled("InitDebuffProc"))
            {
                CancelBuffs(RelevantNanos.ShadeInitDebuffProc);
            }
            if (!IsSettingEnabled("DamageProc"))
            {
                CancelBuffs(RelevantNanos.ShadeDmgProc);
            }
            if (!IsSettingEnabled("DoTProc"))
            {
                CancelBuffs(RelevantNanos.ShadeDotProc);
            }
            if (!IsSettingEnabled("StunProc"))
            {
                CancelBuffs(RelevantNanos.ShadeStunProc);
            }
        }

        private class RelevantItems {
            public const int Tattoo = 269511;
        }

        private class RelevantNanos
        {
            public const int ShadesCaress = 266300;
            public const int CompositeAttribute = 223372;
            public const int CompositeNano = 223380;
            public const int CompositeMelee = 223360;
            public const int CompositeMeleeSpec = 215264;
            public static readonly int[] FasterThanYourShadow = { 272371 };
            public static readonly int[] EVASION_BUFFS = { 275844, 29247, 28903, 28878, 28872, 218070, 218068, 218066,
            218064, 218062, 218060, 272371, 270808, 30745, 302188, 29272, 270802, 28603, 223125, 223131, 223129, 215718,
            223127, 272416, 272415, 272414, 272413, 272412};
            public static readonly int[] RK_RUN_BUFFS = { 93132, 93126, 93127, 93128, 93129, 93130, 93131, 93125 };
            public static readonly int[] ShadeDmgProc = { 224167, 224165, 224163, 210371, 210369, 210367, 210365, 210363, 210361, 210359, 210357, 210355, 210353 };
            public static readonly int[] ShadeStunProc = { 224171, 224169, 210380, 210378, 210376 };
            public static readonly int[] ShadeInitDebuffProc = { 224177, 210407, 210401 };
            public static readonly int[] ShadeDotProc = { 224161, 224159, 210395, 210393, 210391, 210389, 210387 };
        }

        private class RelevantPerks
        {
            public static readonly List<PerkHash> TotemicRites = new List<PerkHash>
            {
                PerkHash.RitualOfDevotion,
                PerkHash.DevourVigor,
                PerkHash.RitualOfZeal,
                PerkHash.DevourEssence,
                PerkHash.RitualOfSpirit,
                PerkHash.DevourVitality,
                PerkHash.RitualOfBlood
            };

            public static readonly List<PerkHash> PiercingMastery = new List<PerkHash>
            {
                PerkHash.Stab,
                PerkHash.DoubleStab,
                PerkHash.Perforate,
                PerkHash.Lacerate,
                PerkHash.Impale,
                PerkHash.Gore,
                PerkHash.Hecatomb
            };

            public static readonly List<PerkHash> SpiritPhylactery = new List<PerkHash>
            {
                PerkHash.CaptureVigor,
                PerkHash.UnsealedBlight,
                PerkHash.CaptureEssence,
                PerkHash.UnsealedPestilence,
                PerkHash.CaptureSpirit,
                PerkHash.UnsealedContagion,
                PerkHash.CaptureVitality
            };
        }
    }
}
