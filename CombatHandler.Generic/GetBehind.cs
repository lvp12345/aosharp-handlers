using AOSharp.Common.GameData;
using AOSharp.Core.Movement;
using AOSharp.Core;

namespace CombatHandler.Generic
{
    internal class GetBehind
    {
        public const float BehindDistOffset = 1.25f;
        public const float RaycastYOffset = 0.1f;


        public void MoveBehindFightingtarget()
        {
            // Do nothing if we aren't attacking or our target is missing
            if (!DynelManager.LocalPlayer.IsAttacking || DynelManager.LocalPlayer.FightingTarget == null)
                return;

            // Do nothing if our fighting target is fighting us
            if (DynelManager.LocalPlayer.FightingTarget.FightingTarget == null ||
                DynelManager.LocalPlayer.FightingTarget.FightingTarget.Identity == DynelManager.LocalPlayer.Identity)
                return;

            // Do nothing if we are navigating
            if (MovementController.Instance.IsNavigating)
                return;

            Vector3 posBehindTarget = GetPositionBehindTarget(DynelManager.LocalPlayer.FightingTarget);

            if (DynelManager.LocalPlayer.Position.DistanceFrom(posBehindTarget) < BehindDistOffset)
                return;

            MovementController.Instance.SetDestination(posBehindTarget);
        }

        private Vector3 GetPositionBehindTarget(SimpleChar target)
        {
            Vector3 posBehind = target.Position + Quaternion.AngleAxis(-180, target.Rotation.Forward).VectorRepresentation() * BehindDistOffset;

            return Playfield.Raycast(target.Position + Vector3.Up * RaycastYOffset, posBehind + Vector3.Up * RaycastYOffset, out Vector3 hitPos, out _) ? hitPos : posBehind;
        }
    }    
}
