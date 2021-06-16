using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using CombatHandler.Generic.IPCMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Character.State
{
    public class SpecialAttacks
    {
        public static SpecialAttacks FromMessage(CharacterSpecialsMessage message)
        {
            return new SpecialAttacks()
            {
                HasBurst = message.HasBurst,
                HasFlingShot = message.HasFlingShot,
                HasFullAuto = message.HasFullAuto,
                HasAimedShot = message.HasAimedShot,
                HasFastAttack = message.HasFastAttack,
                HasBrawl = message.HasBrawl,
                HasSneakAttack = message.HasSneakAttack,
                HasDimach = message.HasDimach,
            };
        }

        public bool HasBurst { get; set; }

        public bool HasFlingShot { get; set; }

        public bool HasFullAuto { get; set; }

        public bool HasAimedShot { get; set; }

        public bool HasFastAttack { get; set; }

        public bool HasBrawl { get; set; }

        public bool HasSneakAttack { get; set; }

        public bool HasDimach { get; set; }
    }
}
