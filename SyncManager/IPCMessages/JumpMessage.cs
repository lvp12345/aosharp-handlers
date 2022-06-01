using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SyncManager.IPCMessages
{
    [AoContract((int)IPCOpcode.Jump)]
    public class JumpMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Jump;

        [AoMember(0)]
        public MovementAction MoveType { get; set; }

        [AoMember(1)]
        public int PlayfieldId { get; set; }
    }
}
