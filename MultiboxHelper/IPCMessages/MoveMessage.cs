using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace MultiboxHelper.IPCMessages
{
    [AoContract((int)IPCOpcode.Move)]
    public class MoveMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Move;

        [AoMember(0)]
        public MovementAction MoveType { get; set; }

        [AoMember(1)]
        public int PlayfieldId { get; set; }

        [AoMember(2)]
        public Vector3 Position { get; set; }

        [AoMember(3)]
        public Quaternion Rotation { get; set; }
    }
}
