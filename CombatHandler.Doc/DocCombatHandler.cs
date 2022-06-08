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
    class DocCombatHandler : GenericCombatHandler
    {
        public static IPCChannel IPCChannel;

        public static string PluginDirectory;

        public static Window buffWindow;
        public static Window debuffWindow;
        public static Window healingWindow;

        private static double _ncuUpdateTime;

        public DocCombatHandler(String pluginDir) : base(pluginDir)
        {
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[Game.ClientInst].IPCChannel));

            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            IPCChannel.RegisterCallback((int)IPCOpcode.Attack, OnAttackMessage);
            IPCChannel.RegisterCallback((int)IPCOpcode.StopAttack, OnStopAttackMessage);

            IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

            Network.N3MessageSent += Network_N3MessageSent;
            Team.TeamRequest += Team_TeamRequest;

            Chat.RegisterCommand("reform", ReformCommand);
            Chat.RegisterCommand("form", FormCommand);
            Chat.RegisterCommand("disband", DisbandCommand);
            Chat.RegisterCommand("convert", RaidCommand);

            _settings.AddVariable("InitDebuffSelection", (int)InitDebuffSelection.None);
            _settings.AddVariable("HealSelection", (int)HealSelection.None);

            _settings.AddVariable("DotA", false);
            _settings.AddVariable("DotB", false);
            _settings.AddVariable("DotC", false);


            _settings.AddVariable("NanoResistTeam", false);

            _settings.AddVariable("ShortHpSelection", (int)ShortHpSelection.None);
            _settings.AddVariable("ShortHoTSelection", (int)ShortHoTSelection.None);

            _settings.AddVariable("CH", true);

            _settings.AddVariable("LockCH", false);

            RegisterSettingsWindow("Doctor Handler", "DocSettingsView.xml");

            RegisterSettingsWindow("Healing", "DocHealingView.xml");
            RegisterSettingsWindow("Buffs", "DocBuffsView.xml");
            RegisterSettingsWindow("Debuffs", "DocDebuffsView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcDoctorAstringent, LEProc, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcDoctorMuscleMemory, LEProc, CombatActionPriority.Low);

            //Healing
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CompleteHealingLine).OrderByStackingOrder(), CompleteHealing, CombatActionPriority.High);

            RegisterSpellProcessor(RelevantNanos.Heals, Healing, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.TeamHeals, TeamHealing, CombatActionPriority.Medium);

            RegisterSpellProcessor(RelevantNanos.AlphaAndOmega, LockCH, CombatActionPriority.High);

            //Buffs
            RegisterSpellProcessor(RelevantNanos.HPBuffs, HPBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolMasteryBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealDeltaBuff).OrderByStackingOrder(), TeamBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoResistanceBuffs).OrderByStackingOrder(), NanoResistanceBuff);

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.TeamDeathlessBlessing, TeamDeathlessBlessing);
            RegisterSpellProcessor(RelevantNanos.IndividualShortHoT, ShortHoTBuff);

            RegisterSpellProcessor(RelevantNanos.ImprovedLC, ImprovedLifeChanneler);
            RegisterSpellProcessor(RelevantNanos.IndividualShortHP, ShortHPBuff);

            //Debuffs
            RegisterSpellProcessor(RelevantNanos.InitDebuffs, InitDebuffs, CombatActionPriority.Low);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineA).OrderByStackingOrder(), DOTADebuffTarget);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOT_LineB).OrderByStackingOrder(), DOTBDebuffTarget);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DOTStrainC).OrderByStackingOrder(), DOTCDebuffTarget);

            PluginDirectory = pluginDir;
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



        protected override void OnUpdate(float deltaTime)
        {
            if (Time.NormalTime > _ncuUpdateTime + 0.5f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }

            if (healingWindow != null && healingWindow.IsValid)
            {
                healingWindow.FindView("HealPercentageBox", out TextInputView textinput1);
                healingWindow.FindView("CompleteHealPercentageBox", out TextInputView textinput2);

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (int.TryParse(textinput1.Text, out int healValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].DocHealPercentage != healValue)
                        {
                            Config.CharSettings[Game.ClientInst].DocHealPercentage = healValue;
                            SettingsController.DocHealPercentage = healValue.ToString();
                            Config.Save();
                        }
                    }
                }
                if (textinput2 != null && textinput2.Text != String.Empty)
                {
                    if (int.TryParse(textinput2.Text, out int chhealValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].DocCompleteHealPercentage != chhealValue)
                        {
                            Config.CharSettings[Game.ClientInst].DocCompleteHealPercentage = chhealValue;
                            SettingsController.DocCompleteHealPercentage = chhealValue.ToString();
                            Config.Save();
                        }
                    }
                }
            }

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

                if (SettingsController.settingsWindow.FindView("HealingView", out Button healingView))
                {
                    healingView.Tag = SettingsController.settingsWindow;
                    healingView.Clicked = HealingView;
                }

                if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                {
                    buffView.Tag = SettingsController.settingsWindow;
                    buffView.Clicked = BuffView;
                }

                if (SettingsController.settingsWindow.FindView("DebuffsView", out Button debuffView))
                {
                    debuffView.Tag = SettingsController.settingsWindow;
                    debuffView.Clicked = DebuffView;
                }
            }

            if (SettingsController.CombatHandlerChannel == String.Empty)
            {
                SettingsController.CombatHandlerChannel = Config.IPCChannel.ToString();
            }

            if (SettingsController.DocCompleteHealPercentage == String.Empty)
            {
                SettingsController.DocCompleteHealPercentage = Config.DocCompleteHealPercentage.ToString();
            }

            if (SettingsController.DocHealPercentage == String.Empty)
            {
                SettingsController.DocHealPercentage = Config.DocHealPercentage.ToString();
            }

            base.OnUpdate(deltaTime);
        }

        #region Healing

        private bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("CH") || !CanCast(spell)
                || SettingsController.DocCompleteHealPercentage == string.Empty) { return false; }

            return FindMemberWithHealthBelow(Convert.ToInt32(SettingsController.DocCompleteHealPercentage), ref actionTarget);
        }

        private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell) || SettingsController.DocHealPercentage == string.Empty) { return false; }

            if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32()
                || HealSelection.Team == (HealSelection)_settings["HealSelection"].AsInt32())
            {

                if (DynelManager.LocalPlayer.IsInTeam())
                {
                    List<SimpleChar> dyingTeamMember = DynelManager.Characters
                        .Where(c => Team.Members
                            .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
                                .Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                        .Where(c => c.HealthPercent <= 85 && c.HealthPercent >= 50)
                        .ToList();

                    if (dyingTeamMember.Count < 4) { return false; }
                }

                return FindMemberWithHealthBelow(Convert.ToInt32(SettingsController.DocHealPercentage), ref actionTarget);
            }

            return false;
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell) || SettingsController.DocHealPercentage == string.Empty) { return false; }

            if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                if (DynelManager.LocalPlayer.IsInTeam())
                {
                    List<SimpleChar> dyingTeamMember = DynelManager.Characters
                        .Where(c => Team.Members
                            .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
                                .Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                        .Where(c => c.HealthPercent <= 85 && c.HealthPercent >= 50)
                        .ToList();

                    if (dyingTeamMember.Count >= 4) { return false; }
                }

                return FindMemberWithHealthBelow(Convert.ToInt32(SettingsController.DocHealPercentage), ref actionTarget);
            }
            else if (HealSelection.SingleOS == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                return FindPlayerWithHealthBelow(Convert.ToInt32(SettingsController.DocHealPercentage), ref actionTarget);
            }

            return false;
        }

        #endregion

        #region Buffs

        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget != null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => SpellChecksOther(spell, spell.Nanoline, c))
                    .Where(c => c.Profession == Profession.Doctor || c.Profession == Profession.NanoTechnician)
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool ImprovedLifeChanneler(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell) || SettingsController.DocHealPercentage == string.Empty) { return false; }

            if (HealSelection.ImprovedLifeChanneler == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                return FindMemberWithHealthBelow(Convert.ToInt32(SettingsController.DocHealPercentage), ref actionTarget);
            }

            if (HasBuffNanoLine(NanoLine.DoctorShortHPBuffs, DynelManager.LocalPlayer)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool NanoResistanceBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("NanoResistTeam"))
            {
                return AllTeamBuff(spell, fightingTarget, ref actionTarget);
            }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool HPBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (HasBuffNanoLine(NanoLine.DoctorHPBuffs, DynelManager.LocalPlayer)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool ShortHoTBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ShortHoTSelection.Self == (ShortHoTSelection)_settings["ShortHoTSelection"].AsInt32())
            {
                return AllBuff(spell, fightingTarget, ref actionTarget);
            }
            if (ShortHoTSelection.Team == (ShortHoTSelection)_settings["ShortHoTSelection"].AsInt32())
            {
                return AllTeamBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool ShortHPBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ShortHpSelection.Self == (ShortHpSelection)_settings["ShortHpSelection"].AsInt32())
            {
                return AllBuff(spell, fightingTarget, ref actionTarget);
            }
            if (ShortHpSelection.Team == (ShortHpSelection)_settings["ShortHpSelection"].AsInt32())
            {
                return AllTeamBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool TeamDeathlessBlessing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ShortHoTSelection.TeamDeathlessBlessing != (ShortHoTSelection)_settings["ShortHoTSelection"].AsInt32()) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.IndividualShortHoTs))
            {
                CancelBuffs(RelevantNanos.IndividualShortHoTs);
            }

            return AllBuff(spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Debuffs

        private bool InitDebuffs(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell)) { return false; }

            if (InitDebuffSelection.OS == (InitDebuffSelection)_settings["InitDebuffSelection"].AsInt32())
            {
                SimpleChar debuffTarget = DynelManager.NPCs
                    .Where(c => !debuffOSTargetsToIgnore.Contains(c.Name)) //Is not a quest target etc
                    .Where(c => c.FightingTarget != null) //Is in combat
                    .Where(c => !c.Buffs.Contains(301844)) // doesn't have ubt in ncu
                    .Where(c => c.IsInLineOfSight)
                    .Where(c => !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz))
                    .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f) //Is in range for debuff (we assume weapon range == debuff range)
                    .Where(c => SpellChecksOther(spell, spell.Nanoline, c)) //Needs debuff refreshed
                    .OrderBy(c => c.MaxHealth)
                    .FirstOrDefault();

                if (debuffTarget != null)
                {
                    actionTarget = (debuffTarget, true);
                    return true;
                }

                return false;
            }

            if (InitDebuffSelection.Target == (InitDebuffSelection)_settings["InitDebuffSelection"].AsInt32()
                && fightingTarget != null)
            {
                if (debuffTargetsToIgnore.Contains(fightingTarget.Name)) { return false; }

                return DebuffTarget(spell, spell.Nanoline, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool DOTADebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("DotA", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool DOTBDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("DotB", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool DOTCDebuffTarget(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("DotC", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Misc

        private bool LockCH(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("LockCH"))
            {
                actionTarget.ShouldSetTarget = true;
                actionTarget.Target = DynelManager.LocalPlayer;
                return true;
            }

            return false;
        }

        private void BuffView(object s, ButtonBase button)
        {
            if (healingWindow != null && healingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", healingWindow);
            }
            else if (debuffWindow != null && debuffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", debuffWindow);
            }
            else
            {
                buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\DocBuffsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                buffWindow.Show(true);
            }
        }

        private void DebuffView(object s, ButtonBase button)
        {
            if (healingWindow != null && healingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Debuffs", healingWindow);
            }
            else if (buffWindow != null && buffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Debuffs", buffWindow);
            }
            else
            {
                debuffWindow = Window.CreateFromXml("Debuffs", PluginDirectory + "\\UI\\DocDebuffsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                debuffWindow.Show(true);
            }
        }

        private void HealingView(object s, ButtonBase button)
        {
            if (buffWindow != null && buffWindow.IsValid)
            {
                buffWindow.FindView("HealPercentageBox", out TextInputView textinput1);
                buffWindow.FindView("CompleteHealPercentageBox", out TextInputView textinput2);

                if (SettingsController.DocHealPercentage != String.Empty)
                {
                    if (textinput1 != null)
                        textinput1.Text = SettingsController.DocHealPercentage;
                }

                if (SettingsController.DocCompleteHealPercentage != String.Empty)
                {
                    if (textinput2 != null)
                        textinput2.Text = SettingsController.DocCompleteHealPercentage;
                }

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (int.TryParse(textinput1.Text, out int healValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].DocHealPercentage != healValue)
                        {
                            Config.CharSettings[Game.ClientInst].DocHealPercentage = healValue;
                            SettingsController.DocHealPercentage = healValue.ToString();
                            Config.Save();
                        }
                    }
                }
                if (textinput2 != null && textinput2.Text != String.Empty)
                {
                    if (int.TryParse(textinput2.Text, out int chhealValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].DocCompleteHealPercentage != chhealValue)
                        {
                            Config.CharSettings[Game.ClientInst].DocCompleteHealPercentage = chhealValue;
                            SettingsController.DocCompleteHealPercentage = chhealValue.ToString();
                            Config.Save();
                        }
                    }
                }

                SettingsController.AppendSettingsTab("Healing", buffWindow);
            }
            else if (debuffWindow != null && debuffWindow.IsValid)
            {
                debuffWindow.FindView("HealPercentageBox", out TextInputView textinput1);
                debuffWindow.FindView("CompleteHealPercentageBox", out TextInputView textinput2);

                if (SettingsController.DocHealPercentage != String.Empty)
                {
                    if (textinput1 != null)
                        textinput1.Text = SettingsController.DocHealPercentage;
                }

                if (SettingsController.DocCompleteHealPercentage != String.Empty)
                {
                    if (textinput2 != null)
                        textinput2.Text = SettingsController.DocCompleteHealPercentage;
                }

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (int.TryParse(textinput1.Text, out int healValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].DocHealPercentage != healValue)
                        {
                            Config.CharSettings[Game.ClientInst].DocHealPercentage = healValue;
                            SettingsController.DocHealPercentage = healValue.ToString();
                            Config.Save();
                        }
                    }
                }
                if (textinput2 != null && textinput2.Text != String.Empty)
                {
                    if (int.TryParse(textinput2.Text, out int chhealValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].DocCompleteHealPercentage != chhealValue)
                        {
                            Config.CharSettings[Game.ClientInst].DocCompleteHealPercentage = chhealValue;
                            SettingsController.DocCompleteHealPercentage = chhealValue.ToString();
                            Config.Save();
                        }
                    }
                }

                SettingsController.AppendSettingsTab("Healing", debuffWindow);
            }
            else
            {
                healingWindow = Window.CreateFromXml("Healing", PluginDirectory + "\\UI\\DocHealingView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                healingWindow.FindView("HealPercentageBox", out TextInputView textinput1);
                healingWindow.FindView("CompleteHealPercentageBox", out TextInputView textinput2);

                if (SettingsController.DocHealPercentage != String.Empty)
                {
                    if (textinput1 != null)
                        textinput1.Text = SettingsController.DocHealPercentage;
                }

                if (SettingsController.DocCompleteHealPercentage != String.Empty)
                {
                    if (textinput2 != null)
                        textinput2.Text = SettingsController.DocCompleteHealPercentage;
                }

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (int.TryParse(textinput1.Text, out int healValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].DocHealPercentage != healValue)
                        {
                            Config.CharSettings[Game.ClientInst].DocHealPercentage = healValue;
                            SettingsController.DocHealPercentage = healValue.ToString();
                            Config.Save();
                        }
                    }
                }
                if (textinput2 != null && textinput2.Text != String.Empty)
                {
                    if (int.TryParse(textinput2.Text, out int chhealValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].DocCompleteHealPercentage != chhealValue)
                        {
                            Config.CharSettings[Game.ClientInst].DocCompleteHealPercentage = chhealValue;
                            SettingsController.DocCompleteHealPercentage = chhealValue.ToString();
                            Config.Save();
                        }
                    }
                }

                healingWindow.Show(true);
            }
        }

        public enum HealSelection
        {
            None, SingleTeam, SingleOS, Team, ImprovedLifeChanneler
        }
        public enum InitDebuffSelection
        {
            None, Target, OS
        }
        public enum ShortHpSelection
        {
            None, Self, Team
        }
        public enum ShortHoTSelection
        {
            None, Self, Team, TeamDeathlessBlessing
        }

        private static class RelevantNanos
        {
            public const int TeamDeathlessBlessing = 269455;
            public static readonly Spell[] IndividualShortHoT = Spell.GetSpellsForNanoline(NanoLine.HealOverTime).OrderByStackingOrder()
                .Where(spell => spell.Id != TeamDeathlessBlessing).ToArray();
            public static int[] IndividualShortHoTs = new[] { 43852, 43868, 43870, 43872, 43873, 43871, 42396, 43869, 43867, 43877, 43876, 43875, 43879,
                42399, 43882, 43874, 43880, 42401 };

            public const int ImprovedLC = 275011;
            public static readonly Spell[] IndividualShortHP = Spell.GetSpellsForNanoline(NanoLine.DoctorShortHPBuffs).OrderByStackingOrder()
                .Where(spell => spell.Id != ImprovedLC).ToArray();

            public const int TiredLimbs = 99578;
            public static readonly Spell[] InitDebuffs = Spell.GetSpellsForNanoline(NanoLine.InitiativeDebuffs).OrderByStackingOrder()
                .Where(spell => spell.Id != TiredLimbs).ToArray();

            public static int[] HPBuffs = new[] { 95709, 28662, 95720, 95712, 95710, 95711, 28649, 95713, 28660, 95715, 95714, 95718, 95716, 95717, 95719, 42397 };

            public const int AlphaAndOmega = 42409;
            public static int[] Heals = new[] { 223299, 223297, 223295, 223293, 223291, 223289, 223287, 223285, 223281, 43878, 43881, 43886, 43885,
                43887, 43890, 43884, 43808, 43888, 43889, 43883, 43811, 43809, 43810, 28645, 43816, 43817, 43825, 43815,
                43814, 43821, 43820, 28648, 43812, 43824, 43822, 43819, 43818, 43823, 28677, 43813, 43826, 43838, 43835,
                28672, 43836, 28676, 43827, 43834, 28681, 43837, 43833, 43830, 43828, 28654, 43831, 43829, 43832, 28665 };
            public static int[] TeamHeals = new[] { 273312, 273315, 270349, 43891, 223291, 43892, 43893, 43894, 43895, 43896, 43897, 43898, 43899,
                43900, 43901, 43903, 43902, 42404, 43905, 43904, 42395, 43907, 43908, 43906, 42398, 43910, 43909, 42402,
                43911, 43913, 42405, 43912, 43914, 43915, 27804, 43916, 43917, 42403, 42408 };
        }

        #endregion
    }
}