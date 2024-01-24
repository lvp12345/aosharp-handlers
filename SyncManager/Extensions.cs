using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using System;
using System.Runtime.InteropServices;

namespace SyncManager
{
    internal class Extensions
    {

        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_NPCChatAddTradeItem@n3EngineClientAnarchy_t@@QAEXABVIdentity_t@@00@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void NPCChatAddTradeItem(IntPtr pEngine, ref Identity self, ref Identity npc, ref Identity slot);

        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_NPCChatStartTrade@n3EngineClientAnarchy_t@@QAEXABVIdentity_t@@0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void NPCChatStartTrade(IntPtr pEngine, ref Identity self, ref Identity npc);

        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_NPCChatEndTrade@n3EngineClientAnarchy_t@@QAEXABVIdentity_t@@0H_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void NPCChatEndTrade(IntPtr pEngine, ref Identity self, ref Identity npc, int credits, bool decline);


        public static void NPCChatStartTrade(Identity self, Identity npc)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine != IntPtr.Zero)
            {
                NPCChatStartTrade(pEngine, ref self, ref npc);
            }
        }

        public static void NPCChatAddTradeItem(Identity self, Identity npc, Identity item)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine != IntPtr.Zero)
            {
                NPCChatAddTradeItem(pEngine, ref self, ref npc, ref item);
            }
        }

        public static void NPCChatEndTrade(Identity self, Identity npc, int credits = 0, bool accept = true)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine != IntPtr.Zero)
            {
                NPCChatEndTrade(pEngine, ref self, ref npc, credits, accept);
            }
        }

    }
}
