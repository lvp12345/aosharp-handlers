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
            _perkRules.Add(214399, Limber);
            _perkRules.Add(211453, DanceOfFools);
        }

        private bool Limber(Perk perk, SimpleChar fightingTarget, out (CombatActionType actionType, SimpleChar target) actionUsageInfo)
        {
            actionUsageInfo = (CombatActionType.Buff, null);

            Buff dof;
            if ((dof = DynelManager.LocalPlayer.Buffs.FirstOrDefault(x => x.Identity.Instance == DOF_BUFF)) != null && dof.RemainingTime > 12.5f)
                return false;

            return true;
        }

        private bool DanceOfFools(Perk perk, SimpleChar fightingTarget, out (CombatActionType actionType, SimpleChar target) actionUsageInfo)
        {
            actionUsageInfo = (CombatActionType.Buff, null);

            Buff limber;
            if ((limber = DynelManager.LocalPlayer.Buffs.FirstOrDefault(x => x.Identity.Instance == LIMBER_BUFF)) == null || limber.RemainingTime > 12.5f)
                return false;

            return true;
        }
    }
}
