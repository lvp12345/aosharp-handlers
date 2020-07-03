using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Combat;
using AOSharp.Core.GameData;
using AOSharp.Core.UI.Options;
using CombatHandler.Generic;

namespace Desu
{
    public class NTCombatHandler : GenericCombatHandler
    {
        private Menu _menu;

        public NTCombatHandler() : base()
        {
            //Perks
            RegisterPerkProcessor(PerkHash.HostileTakeover, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.ChaoticAssumption, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.ProgramOverload, DamagePerk);
            RegisterPerkProcessor(PerkHash.FlimFocus, DamagePerk);
            RegisterPerkProcessor(PerkHash.Utilize, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.DazzleWithLights, StarfallPerk);
            RegisterPerkProcessor(PerkHash.Combust, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.ThermalDetonation, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.Supernova, TargetedDamagePerk);
            RegisterPerkProcessor(PerkHash.BreachDefenses, TargetedDamagePerk);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.NullitySphereNano).OrderByStackingOrder(), NullitySphere, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.NanobotAegis, NanobotAegis);
            RegisterSpellProcessor(RelevantNanos.IzgimmersWealth, IzgimmersWealth);
            RegisterSpellProcessor(RelevantNanos.IzgimmersUltimatum, SingleTargetNuke);                                                                           //Garuk's Improved Viral Assault
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.DOTNanotechnicianStrainA).OrderByStackingOrder(), AIDotNuke);          //AI Dot
            RegisterSpellProcessor(RelevantNanos.Garuk, SingleTargetNuke);                                                                           //IU for now.. but once i'm not lazy, more nukes.

            _menu = new Menu("CombatHandler.NT", "CombatHandler.NT");
            _menu.AddItem(new MenuBool("UseAIDot", "Use AI DoT", true));
            OptionPanel.AddMenu(_menu);
        }

        protected virtual bool StarfallPerk(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = null;

            if (Perk.Find(PerkHash.Combust, out Perk combust) && !combust.IsAvailable)
                return false;

            return TargetedDamagePerk(perk, fightingTarget, out target);
        }

        private bool NanobotAegis(Spell spell, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = null;
            return DynelManager.LocalPlayer.HealthPercent < 50 && !DynelManager.LocalPlayer.Buffs.Contains(Nanoline.NullitySphereNano);
        }

        private bool NullitySphere(Spell spell, SimpleChar fightingtarget, out SimpleChar target)
        {
            target = null;
            return DynelManager.LocalPlayer.HealthPercent < 50 && !DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.NanobotAegis);
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;
            if (fightingTarget == null)
                return false;

            return true;
        }

        private bool IzgimmersWealth(Spell spell, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;

            if (fightingTarget == null)
                return false;

            if (DynelManager.LocalPlayer.MissingNano < 20000 && DynelManager.LocalPlayer.NanoPercent > 5)
                return false;

            return true;
        }

        private bool AIDotNuke(Spell spell, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;

            if (!_menu.GetBool("UseAIDot"))
                return false;

            if (fightingTarget == null)
                return false;

            if (fightingTarget.Health < 80000)
                return false;

            if (fightingTarget.Buffs.Find(spell.Identity.Instance, out Buff buff) && buff.RemainingTime > 5)
                return false;

            return true;
        }

        private static class RelevantNanos
        {
            public const int NanobotAegis = 302074;
            public const int IzgimmersWealth = 275024;
            public const int IzgimmersUltimatum = 275692;
            public const int Garuk = 218168;
        }
    }
}
