using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core.Inventory;
using AOSharp.Core;

namespace CombatHandler.Generic
{
    public static class Utilz
    {
        public static void AddRandomness(this ref Vector3 pos, int entropy)
        {
            pos.X += Next(-entropy, entropy);
            pos.Z += Next(-entropy, entropy);
        }

        public static int Next(int min, int max)
        {
            if (min >= max)
            {
                throw new ArgumentException("Min value is greater or equals than Max value.");
            }

            byte[] intBytes = new byte[4];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetNonZeroBytes(intBytes);
            }

            return min + Math.Abs(BitConverter.ToInt32(intBytes, 0)) % (max - min + 1);
        }
    }
}
