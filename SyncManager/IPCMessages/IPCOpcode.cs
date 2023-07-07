namespace SyncManager.IPCMessages
{
    public enum IPCOpcode
    {
        Move = 200,
        Jump = 201,
        Target = 202,
        Attack = 203,
        StopAttack = 204,
        Use = 205,
        NpcChatOpen = 206,
        NpcChatClose = 207,
        NpcChatAnswer = 208,
        UseItem = 209,
        Trade = 210,
        Start = 211,
        Stop = 212,
        AttackToggleOn = 213,
        AttackToggleOff = 214
    }
}
