using AOSharp.Core;
using AOSharp.Common.GameData;
using AOSharp.Pathfinding;
using System.Linq;
using AOSharp.Core.UI;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using AOSharp.Core.IPC;
using HelpManager.IPCMessages;

namespace HelpManager
{
    internal class POH
    {
        public static void HandlePathingToPOS()
        {
            var playerFloor = DynelManager.LocalPlayer.Room.Instance;

            switch (playerFloor)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                    if ((HelpManager.POHPositions)HelpManager._settings["POHPositions"].AsInt32() == HelpManager.POHPositions.PortalGuardian1)
                    {
                        HandleMovement(204.1, 7.8, 99.1);
                    }
                    else if ((HelpManager.POHPositions)HelpManager._settings["POHPositions"].AsInt32() == HelpManager.POHPositions.PortalGuardian2)
                    {
                        HandleMovement(228.2, 7.8, 112.3);
                    }
                    else if ((HelpManager.POHPositions)HelpManager._settings["POHPositions"].AsInt32() == HelpManager.POHPositions.PortalGuardian3)
                    {
                        HelpManager.returnPosition = Vector3.Zero;
                    }
                    else if ((HelpManager.POHPositions)HelpManager._settings["POHPositions"].AsInt32() == HelpManager.POHPositions.PortalGuardian4)
                    {
                        HelpManager.returnPosition = Vector3.Zero;
                    }
                    break;
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 17:
                case 18:
                case 19:
                    if ((HelpManager.POHPositions)HelpManager._settings["POHPositions"].AsInt32() == HelpManager.POHPositions.PortalGuardian1)
                    {
                        HandleMovement(71.2, 6.0, 117.2);
                    }
                    else if ((HelpManager.POHPositions)HelpManager._settings["POHPositions"].AsInt32() == HelpManager.POHPositions.PortalGuardian2)
                    {
                        HandleMovement(50.2, 6.0, 48.9);
                    }
                    else if ((HelpManager.POHPositions)HelpManager._settings["POHPositions"].AsInt32() == HelpManager.POHPositions.PortalGuardian3)
                    {
                        HandleMovement(122.8, 6.0, 37.1);
                    }
                    else if ((HelpManager.POHPositions)HelpManager._settings["POHPositions"].AsInt32() == HelpManager.POHPositions.PortalGuardian4)
                    {
                        HelpManager.returnPosition = Vector3.Zero;
                    }
                    break;

                case 20:
                case 21:
                case 22:
                case 23:
                case 24:
                case 25:
                case 26:
                case 27:
                case 28:
                case 29:
                case 30:
                case 31:
                case 32:
                case 34:
                case 35:
                case 36:
                case 37:
                case 39:
                case 40:
                case 42:
                case 43:
                case 44:
                    if ((HelpManager.POHPositions)HelpManager._settings["POHPositions"].AsInt32() == HelpManager.POHPositions.PortalGuardian1)
                    {
                        HandleMovement(291.5, 9.0, 223.1);
                    }
                    else if ((HelpManager.POHPositions)HelpManager._settings["POHPositions"].AsInt32() == HelpManager.POHPositions.PortalGuardian2)
                    {
                        HandleMovement(275.1, 6.0, 180.9);
                    }
                    else if ((HelpManager.POHPositions)HelpManager._settings["POHPositions"].AsInt32() == HelpManager.POHPositions.PortalGuardian3)
                    {
                        HandleMovement(336.6, 6.0, 134.1);
                    }
                    else if ((HelpManager.POHPositions)HelpManager._settings["POHPositions"].AsInt32() == HelpManager.POHPositions.PortalGuardian4)
                    {
                        HandleMovement(357.0, 6.0, 30.2);
                    }
                    break;

                default:
                    break;
            }
        }

        static void HandleMovement(double x, double z, double y)
        {
            var portalKeepers = DynelManager.Characters.Where(c => c.Name.Contains("Portal") && c.Health > 0 && c.IsInLineOfSight).FirstOrDefault();

            if (DynelManager.LocalPlayer.Position.DistanceFrom(new Vector3(x, z, y)) > 1)
            {
                if (!SMovementController.IsNavigating())
                {
                    Network.Send(new SocialActionCmdMessage { Unknown5 = 0x3E, Unknown = 1, Action = SocialAction.DanceFlamenco });
                    SMovementController.SetNavDestination(new Vector3(x, z, y));
                }
            }
            else
            {
                if (portalKeepers == null)
                {
                    if (DynelManager.LocalPlayer.Position.DistanceFrom(HelpManager.returnPosition) > 1)
                    {
                        if (!SMovementController.IsNavigating())
                        {
                            Network.Send(new SocialActionCmdMessage { Unknown5 = 0x3E, Unknown = 1, Action = SocialAction.DanceYmca });
                            SMovementController.SetNavDestination(HelpManager.returnPosition);
                        }
                    }
                }
            }
        }
    }
}
