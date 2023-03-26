using AOSharp.Common.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarpManager
{
    public enum SimpleState
    {
        ENABLED,
        DISABLED
    }
    public static void SetDefaultState()
    {

        WM_ROTATION = new Quaternion();
        Player_Location = new Vector3();
        Target_Location = new Vector3();
        WM_PLAYER_ANGLE_TO_TARGET = 0;

        logger.debug($"ComponentManager initialized!");

    }

}
