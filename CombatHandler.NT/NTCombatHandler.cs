using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Combat;

namespace Desu
{
    public class NTCombatHandler : CombatHandler
    {
        public NTCombatHandler() : base()
        {
            //Perks
            RegisterPerkProcessor(PerkHash.NanoFeast, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.BotConfinement, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.HostileTakeover, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.ChaoticAssumption, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.ProgramOverload, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.FlimFocus, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Utilize, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.DazzleWithLights, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Combust, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.ThermalDetonation, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.Supernova, GenericDamagePerk);
            RegisterPerkProcessor(PerkHash.BreachDefenses, GenericDamagePerk);

            //Spells
            RegisterSpellProcessor(275692, SingleTargetNuke);                                                                           //Garuk's Improved Viral Assault
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(Nanoline.DOTNanotechnicianStrainA).OrderByStackingOrder(), AIDotNuke);    //AI Dot
            RegisterSpellProcessor(218168, SingleTargetNuke);                                                                           //IU for now.. but once i'm not lazy, more nukes.
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;
            if (fightingTarget == null)
                return false;

            return true;
        }

        private bool AIDotNuke(Spell spell, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = null;
            if (fightingTarget == null)
                return false;

            Buff buff;
            if (fightingTarget.Buffs.Find(spell.Identity.Instance, out buff) && buff.RemainingTime < 5)
                return false;

            target = fightingTarget;
            return true;
        }

        private bool GenericDamagePerk(Perk perk, SimpleChar fightingTarget, out SimpleChar target)
        {
            target = fightingTarget;

            if (fightingTarget == null || fightingTarget.HealthPercent < 5)
                return false;

            return true;
        }
    }
}
