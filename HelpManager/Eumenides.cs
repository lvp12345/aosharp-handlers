using AOSharp.Core;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core.Movement;

namespace HelpManager
{
    internal class Eumenides
    {
        public static void HandleEumenides()
        {
            var localPlayer = DynelManager.LocalPlayer;

            PlayerPositions(localPlayer.Profession);
            HandleMorphs();
        }

        static void HandleMorphs()
        {
            var localPlayer = DynelManager.LocalPlayer;
            var morphs = localPlayer.Buffs;

            if (morphs.Contains(305861))
            {

            }
            if (morphs.Contains(305862))
            {

            }
            if (morphs.Contains(305863))
            {

            }
            if (morphs.Contains(305864))
            {

            }
            //Calia's Corruption = 305866 
            //Curse-Rotted Curator = 305861; nano dot and root
            //Treacherous Parrot = 305862, dmg debuff/ target heal, stop attacking
            //Decaying Pit Lizard = 305863, int/rs debuff
            //Frenzied Wolf = 305864 dot, mind cotrol/ lock ch
        }

        static void PlayerPositions(Profession profession)
        {
            var localPlayer = DynelManager.LocalPlayer;

            if (localPlayer != null)
            {
                if ((HelpManager.Positions)HelpManager._settings["Positions"].AsInt32() == HelpManager.Positions.Center)
                {
                    HandleMovement(91.0, 6.0, 282.4);
                }
                if ((HelpManager.Positions)HelpManager._settings["Positions"].AsInt32() == HelpManager.Positions.BackWall)
                {
                    HandleMovement(91.2, 6.0, 292.5);
                }
                if ((HelpManager.Positions)HelpManager._settings["Positions"].AsInt32() == HelpManager.Positions.BackLeft)
                {
                    HandleMovement(84.5, 6.0, 290.0);
                }
                if ((HelpManager.Positions)HelpManager._settings["Positions"].AsInt32() == HelpManager.Positions.BackRight)
                {
                    HandleMovement(97.5, 6.0, 290.0);
                }
                if ((HelpManager.Positions)HelpManager._settings["Positions"].AsInt32() == HelpManager.Positions.FrontLeft)
                {
                    HandleMovement(84.7, 6.0, 276.6);
                }
                if ((HelpManager.Positions)HelpManager._settings["Positions"].AsInt32() == HelpManager.Positions.FrontRight)
                {
                    HandleMovement(97.3, 6.0, 276.5);
                }
                if ((HelpManager.Positions)HelpManager._settings["Positions"].AsInt32() == HelpManager.Positions.Door)
                {
                    HandleMovement(91.1, 6.0, 267.7);
                }
            }
        }

        static void HandleMovement(double x, double z, double y)
        {
            if (DynelManager.LocalPlayer.Position.DistanceFrom(new Vector3(x, z, y)) > 1)
            {
                if (!MovementController.Instance.IsNavigating)
                {
                    MovementController.Instance.SetDestination(new Vector3(x, z, y));
                }
            }
        }

        #region names

        //Eliminator Shiro, Health: 5205000, Position: (145.4241, 156.145, 114.074)
        //Hacked Mechdog ignore
        //Neutrino Mine ignore
        //Personal Transporter, Health: 10, Position: (196, 150, 95)
        //Personal Transporter, Health: 10, Position: (79, 150, 123) eumenides
        //Eumenides, Health: 6966667, Room: Shopping Dead-end, Position: (91.93027, 6.02, 287.3834)

        #endregion
    }
}
