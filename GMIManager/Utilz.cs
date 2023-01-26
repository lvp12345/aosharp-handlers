using AOSharp.Common.GameData;
using AOSharp.Core.UI;
using AOSharp.Core;
using System;
using System.Collections.Generic;
using AOSharp.Common.GameData.UI;

namespace GMIManager
{
    public static class Utilz
    {
        public static int Last(int num)
        {
            string str = num.ToString();
            int pos = str.Length - 1;

            return Convert.ToInt32(str.Substring(pos));
        }
    }
}
