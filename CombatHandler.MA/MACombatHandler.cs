using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Core;
using AOSharp.Core.Combat;

namespace Desu
{
    public class MACombatHandler : CombatHandler
    {
        private const int DOF_BUFF = 210159;
        private const int LIMBER_BUFF = 210158;

        public MACombatHandler() : base()
        {
            _perkRules.Add(214399, Limber);                 //Limber
            _perkRules.Add(211453, DanceOfFools);           //Dance of Fools
            _perkRules.Add(248727, Moonmist);               //Moonmist

            for (int i = 226177; i <= 226182; i++)          //Dragonfire
                _perkRules.Add(i, GenericDamagePerk);

            _perkRules.Add(226170, GenericDamagePerk);      //Chi Conductor
            _perkRules.Add(226169, GenericDamagePerk);      //Incapacitate

            for (int i = 226797; i <= 226802; i++)          //Tremor Hand
                _perkRules.Add(i, GenericDamagePerk);

            _perkRules.Add(226162, GenericDamagePerk);      //Flesh Quiver

            for (int i = 226155; i <= 226160; i++)          //Obliterate
                _perkRules.Add(i, Obliterate);

            _perkRules.Add(253078, GenericDamagePerk);      //Bore
            _perkRules.Add(253075, GenericDamagePerk);      //Crave
            _perkRules.Add(253119, GenericDamagePerk);      //Nano Feast
            _perkRules.Add(253122, GenericDamagePerk);      //Bot Confinement

            _spellRules.Add(275698, MatrixOfKa);
        }

        private bool MatrixOfKa(Spell spell, SimpleChar fightingTarget, out SimpleChar target)
        {
            if (DynelManager.LocalPlayer.MissingHealth > 2000)
            {
                target = DynelManager.LocalPlayer;
                return true;
            }

            target = null;
            return false;
        }

        private bool Limber(Perk perk, SimpleChar fightingTarget, out (CombatActionType actionType, SimpleChar target) actionUsageInfo)
        {
            actionUsageInfo = (CombatActionType.Buff, null);

            Buff dof;
            if (DynelManager.LocalPlayer.Buffs.Find(DOF_BUFF, out dof) && dof.RemainingTime > 12.5f)
                return false;

            return true;
        }

        private bool DanceOfFools(Perk perk, SimpleChar fightingTarget, out (CombatActionType actionType, SimpleChar target) actionUsageInfo)
        {
            actionUsageInfo = (CombatActionType.Buff, null);

            Buff limber;
            if (!DynelManager.LocalPlayer.Buffs.Find(LIMBER_BUFF, out limber) || limber.RemainingTime > 12.5f)
                return false;

            return true;
        }

        private bool Moonmist(Perk perk, SimpleChar fightingTarget, out (CombatActionType actionType, SimpleChar target) actionUsageInfo)
        {
            actionUsageInfo = (CombatActionType.Buff, null);

            if (fightingTarget == null || fightingTarget.HealthPercent < 90)
                return false;

            return true;
        }

        private bool GenericDamagePerk(Perk perk, SimpleChar fightingTarget, out (CombatActionType actionType, SimpleChar target) actionUsageInfo)
        {
            actionUsageInfo = (CombatActionType.Damage, fightingTarget);

            if (fightingTarget == null || fightingTarget.HealthPercent < 5)
                return false;

            return true;
        }

        private bool Obliterate(Perk perk, SimpleChar fightingTarget, out (CombatActionType actionType, SimpleChar target) actionUsageInfo)
        {
            actionUsageInfo = (CombatActionType.Damage, fightingTarget);

            if (fightingTarget == null || fightingTarget.HealthPercent > 15)
                return false;

            return true;
        }
    }
}
