using AOSharp.Common.GameData;
using AOSharp.Core;
using CombatHandler.Generic;
using AOSharp.Core.UI;
using System.Linq;
using System;
using AOSharp.Common.GameData.UI;
using AOSharp.Core.IPC;
using System.Threading.Tasks;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System.Threading;
using SmokeLounge.AOtomation.Messaging.Messages;
using CombatHandler;
using System.Collections.Generic;
using AOSharp.Core.Inventory;

namespace Desu
{
    public class NTCombatHandler : GenericCombatHandler
    {
        public static IPCChannel IPCChannel;

        public static string PluginDirectory;

        public static Window buffWindow;

        private static double _ncuUpdateTime;

        public NTCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[Game.ClientInst].IPCChannel));

            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            IPCChannel.RegisterCallback((int)IPCOpcode.Attack, OnAttackMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.StopAttack, OnStopAttackMessage);

            IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

            Chat.RegisterCommand("channel", (string command, string[] param, ChatWindow chatWindow) =>
            {
                Chat.WriteLine($"Channel set : {param[0]}");
                IPCChannel.SetChannelId(Convert.ToByte(param[0]));
                Config.CharSettings[Game.ClientInst].IPCChannel = Convert.ToByte(param[0]);
                Config.Save();

            });

            Network.N3MessageSent += Network_N3MessageSent;
            Team.TeamRequest += Team_TeamRequest;

            Chat.RegisterCommand("reform", ReformCommand);
            Chat.RegisterCommand("form", FormCommand);
            Chat.RegisterCommand("disband", DisbandCommand);
            Chat.RegisterCommand("convert", RaidCommand);

            _settings.AddVariable("AIDot", true);

            _settings.AddVariable("Pierce", false);

            _settings.AddVariable("AoeBlind", false);

            _settings.AddVariable("AOE", false);
            _settings.AddVariable("VE", false);

            _settings.AddVariable("NanoHoTTeam", false);
            _settings.AddVariable("CostTeam", false);

            RegisterSettingsWindow("Nano-Technician Handler", "NTSettingsView.xml");

            RegisterSettingsWindow("Buffs", "NTBuffsView.xml");

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NullitySphereNano).OrderByStackingOrder(), NullitySphere, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.NanobotAegis, NanobotAegis);
            RegisterSpellProcessor(RelevantNanos.IzgimmersWealth, IzgimmersWealth);

            RegisterSpellProcessor(RelevantNanos.NanobotShelter, GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Psy_IntBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDamageMultiplierBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NFRangeBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MatCreaBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), GenericBuffExcludeInnerSanctum);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.Fortify).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoOverTime_LineA).OrderByStackingOrder(), NanoHoT);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NPCostBuff).OrderByStackingOrder(), Cost);

            if (Spell.Find(RelevantNanos.SuperiorFleetingImmunity, out Spell immunity))
            {
                RegisterSpellProcessor(immunity, GenericBuff);
            }

            //Team buffs
            RegisterSpellProcessor(RelevantNanos.AbsortAcTargetBuffs, GenericBuff);

            //Nukes and DoTs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTNanotechnicianStrainA).OrderByStackingOrder(), AiDotNuke, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.Garuk, SingleTargetNuke, CombatActionPriority.Medium);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTNanotechnicianStrainB).OrderByStackingOrder(), PierceNuke, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.SingleTargetNukes, SingleTargetNuke, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.AOENukes, AOENuke);
            RegisterSpellProcessor(RelevantNanos.VolcanicEruption, VolcanicEruption);

            //Debuffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AAODebuffs).OrderByStackingOrder(), SingleBlind);
            RegisterSpellProcessor(RelevantNanos.AoeBlinds, AoeBlind);

            PluginDirectory = pluginDir;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView textinput1);

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (int.TryParse(textinput1.Text, out int channelValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].IPCChannel != channelValue)
                        {
                            IPCChannel.SetChannelId(Convert.ToByte(channelValue));
                            Config.CharSettings[Game.ClientInst].IPCChannel = Convert.ToByte(channelValue);
                            SettingsController.CombatHandlerChannel = channelValue.ToString();
                            Config.Save();
                        }
                    }
                }

                if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                {
                    buffView.Tag = SettingsController.settingsWindow;
                    buffView.Clicked = BuffView;
                }
            }

            if (SettingsController.CombatHandlerChannel == String.Empty)
            {
                SettingsController.CombatHandlerChannel = Config.IPCChannel.ToString();
            }

            if (Time.NormalTime > _ncuUpdateTime + 0.5f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }

            base.OnUpdate(deltaTime);
        }

        public static bool IsRaidEnabled(string[] param)
        {
            return param.Length > 0 && "raid".Equals(param[0]);
        }

        public static Identity[] GetRegisteredCharactersInvite()
        {
            Identity[] registeredCharacters = SettingsController.GetRegisteredCharacters();
            int firstTeamCount = registeredCharacters.Length > 6 ? 6 : registeredCharacters.Length;
            Identity[] firstTeamCharacters = new Identity[firstTeamCount];
            Array.Copy(registeredCharacters, firstTeamCharacters, firstTeamCount);
            return firstTeamCharacters;
        }

        public static Identity[] GetRemainingRegisteredCharacters()
        {
            Identity[] registeredCharacters = SettingsController.GetRegisteredCharacters();
            int characterCount = registeredCharacters.Length - 6;
            Identity[] remainingCharacters = new Identity[characterCount];
            if (characterCount > 0)
            {
                Array.Copy(registeredCharacters, 6, remainingCharacters, 0, characterCount);
            }
            return remainingCharacters;
        }

        public static void SendTeamInvite(Identity[] targets)
        {
            foreach (Identity target in targets)
            {
                if (target != DynelManager.LocalPlayer.Identity)
                    Team.Invite(target);
            }
        }

        public static void Team_TeamRequest(object s, TeamRequestEventArgs e)
        {
            if (SettingsController.IsCharacterRegistered(e.Requester))
            {
                e.Accept();
            }
        }

        public static void Network_N3MessageSent(object s, N3Message n3Msg)
        {
            if (!IsActiveWindow || n3Msg.Identity != DynelManager.LocalPlayer.Identity) { return; }

            //Chat.WriteLine($"{n3Msg.Identity != DynelManager.LocalPlayer.Identity}");

            if (n3Msg.N3MessageType == N3MessageType.LookAt)
            {
                LookAtMessage lookAtMsg = (LookAtMessage)n3Msg;
                IPCChannel.Broadcast(new TargetMessage()
                {
                    Target = lookAtMsg.Target
                });
            }
            else if (n3Msg.N3MessageType == N3MessageType.Attack)
            {
                AttackMessage attackMsg = (AttackMessage)n3Msg;
                IPCChannel.Broadcast(new AttackIPCMessage()
                {
                    Target = attackMsg.Target
                });
            }
            else if (n3Msg.N3MessageType == N3MessageType.StopFight)
            {
                StopFightMessage stopAttackMsg = (StopFightMessage)n3Msg;
                IPCChannel.Broadcast(new StopAttackIPCMessage());
            }
        }

        public static void OnDisband(int sender, IPCMessage msg)
        {
            Team.Leave();
        }


        public static void OnStopAttackMessage(int sender, IPCMessage msg)
        {
            if (IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            DynelManager.LocalPlayer.StopAttack();
        }

        public static void DisbandCommand(string command, string[] param, ChatWindow chatWindow)
        {
            Team.Disband();
            IPCChannel.Broadcast(new DisbandMessage());
        }

        public static void RaidCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (Team.IsLeader)
                Team.ConvertToRaid();
            else
                Chat.WriteLine("Needs to be used from leader.");
        }

        public static void ReformCommand(string command, string[] param, ChatWindow chatWindow)
        {
            Team.Disband();
            IPCChannel.Broadcast(new DisbandMessage());
            Task task = new Task(() =>
            {
                Thread.Sleep(1000);
                FormCommand("form", param, chatWindow);
            });
            task.Start();
        }

        public static void FormCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (!DynelManager.LocalPlayer.IsInTeam())
            {
                SendTeamInvite(GetRegisteredCharactersInvite());

                if (IsRaidEnabled(param))
                {
                    Task task = new Task(() =>
                    {
                        Thread.Sleep(1000);
                        Team.ConvertToRaid();
                        Thread.Sleep(1000);
                        SendTeamInvite(GetRemainingRegisteredCharacters());
                    });
                    task.Start();
                }
            }
            else
            {
                Chat.WriteLine("Cannot form a team. Character already in team. Disband first.");
            }
        }

        public static void OnTargetMessage(int sender, IPCMessage msg)
        {
            if (IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            TargetMessage targetMsg = (TargetMessage)msg;
            Targeting.SetTarget(targetMsg.Target);
        }

        public static void OnAttackMessage(int sender, IPCMessage msg)
        {
            if (IsActiveWindow)
                return;

            if (Game.IsZoning)
                return;

            AttackIPCMessage attackMsg = (AttackIPCMessage)msg;
            Dynel targetDynel = DynelManager.GetDynel(attackMsg.Target);
            DynelManager.LocalPlayer.Attack(targetDynel, true);
        }

        public static void OnRemainingNCUMessage(int sender, IPCMessage msg)
        {
            try
            {
                if (Game.IsZoning)
                    return;

                RemainingNCUMessage ncuMessage = (RemainingNCUMessage)msg;
                SettingsController.RemainingNCU[ncuMessage.Character] = ncuMessage.RemainingNCU;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e);
            }
        }


        private void BuffView(object s, ButtonBase button)
        {
            buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\NTBuffsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            buffWindow.Show(true);
        }

        private bool AoeBlind(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AoeBlind") || fightingTarget == null) { return false; }

            return !fightingTarget.Buffs.Contains(NanoLine.AAODebuffs);
        }

        private bool SingleBlind(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("AoeBlind") || fightingTarget == null) { return false; }

            return !fightingTarget.Buffs.Contains(NanoLine.AAODebuffs);
        }

        private bool VolcanicEruption(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("VE"))
            {
                if (fightingTarget == null || !CanCast(spell)) { return false; }

                actionTarget.ShouldSetTarget = false;
                return true;
            }

            return false;
        }

        private bool NanoHoT(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("NanoHoTTeam"))
            {
                if (Team.IsInTeam)
                    return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);

                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool Cost(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("CostTeam"))
            {
                if (Team.IsInTeam)
                    return CheckNotProfsBeforeCast(spell, fightingTarget, ref actionTarget);

                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool NanobotAegis(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = false;
            return DynelManager.LocalPlayer.HealthPercent < 50 && !DynelManager.LocalPlayer.Buffs.Contains(NanoLine.NullitySphereNano);
        }

        private bool NullitySphere(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            actionTarget.ShouldSetTarget = false;
            return DynelManager.LocalPlayer.HealthPercent < 50 && !DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.NanobotAegis);
        }

        private bool SingleTargetNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("AOE") || fightingTarget == null) { return false; }

            return true;
        }

        private bool PierceNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Pierce") || fightingTarget == null) { return false; }

            return true;
        }

        private bool AOENuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AOE") || fightingTarget == null) { return false; }

            return true;
        }

        private bool IzgimmersWealth(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            if (DynelManager.LocalPlayer.NanoPercent > 25) { return false; }

            actionTarget.ShouldSetTarget = false;
            return true;
        }

        private bool AiDotNuke(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AIDot") || fightingTarget == null) { return false; }

            if (fightingTarget.Health < 80000) { return false; }

            if (fightingTarget.Buffs.Find(spell.Identity.Instance, out Buff buff) && buff.RemainingTime > 5) { return false; }

            return true;
        }

        private static class RelevantNanos
        {
            public const int NanobotAegis = 302074;
            public const int IzgimmersWealth = 275024;
            public const int IzgimmersUltimatum = 218168;
            public const int Garuk = 275692;
            public const int PierceReflect = 266287;
            public const int VolcanicEruption = 28638;
            public static readonly int[] AOENukes = { 266293, 28638,
                266297, 28637, 28594, 45922, 45906, 45884, 28635, 266298, 28593, 45925, 45940, 45900,28629,
                45917, 45937, 28599, 45894, 45943, 28633, 28631 };
            public const int SuperiorFleetingImmunity = 273386;
            public static readonly Spell[] AbsortAcTargetBuffs = Spell.GetSpellsForNanoline(NanoLine.AbsorbACBuff).OrderByStackingOrder().Where(spell => spell.Identity.Instance != SuperiorFleetingImmunity).ToArray();
            public static readonly int[] AoeBlinds = { 83959, 83960, 83961, 83962, 83963, 83964 };
            public static readonly int[] SingleTargetNukes = { 218168, 218164, 218162, 218160, 218158, 218156, 218154, 218152, 218150, 
                218148, 218146, 218144, 218142, 218140, 218138, 218136, 269473, 218134, 201935, 202262, 201933, 218132, 28618, 218124, 218130, 
                218122, 218120, 218128, 218118, 218126, 45226, 45192, 28619, 45230, 28623, 28604, 28616, 218116, 28597, 45210, 45236, 45197, 
                45233, 45247, 45199, 45235, 45234, 218114, 45258, 45217, 28600, 45198, 28613, 45919, 45195, 45225, 45260, 45891, 45254, 45890, 
                45213, 218112, 45215, 45915, 218104, 45252, 45214, 45251, 45929, 45220, 45920, 45222, 218102, 28598, 45911, 45237, 45216, 
                218110, 45913, 45901, 45212, 45206, 45912, 45883, 45245, 45140, 45904, 45218, 28626, 218108, 45261, 218100, 45909, 45203, 
                45228, 45903, 45200, 45939, 28592, 45242, 218098, 218106, 45885, 45926, 45241, 44538, 45908, 45250, 45934, 45138, 45932, 
                28632, 45205, 28609, 45209, 45246, 45935, 45921, 45227, 45207, 45942, 45191, 45924, 218096, 28610, 45914, 45208, 45893, 
                28621, 45211, 45916, 45933, 218094, 45240, 45259, 45941, 45910, 45253, 28614, 218092, 45221, 45204, 28634, 45196, 45886, 
                45201, 45928, 45193, 45323, 45244, 45889, 45895, 28605, 45219, 45223, 45938, 28628, 45232, 45248, 45898, 45202, 45923, 
                45229, 45907, 45139, 45887, 45231, 45882, 28627, 45936, 45194, 28639, 45243, 45931, 28630, 45137, 28607, 45257, 45880, 
                45256, 45249, 45888, 45255, 45881, 42543, 45927, 45902, 42540, 42541, 45899, 45905, 28611, 45897, 28601, 42542, 28608, 
                45918, 42539, 45892, 45930, 45879, 45896, 28612 };
            public static readonly int[] NanobotShelter = { 273388, 263265 };
            public static readonly int CompositeAttribute = 223372;
            public static readonly int CompositeNano = 223380;
        }
    }
}
