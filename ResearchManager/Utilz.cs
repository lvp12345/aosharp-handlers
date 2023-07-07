using System;

namespace ResearchManager
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
