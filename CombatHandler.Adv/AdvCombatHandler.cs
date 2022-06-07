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
    public class AdvCombatHandler : GenericCombatHandler
    {
        public static IPCChannel IPCChannel;

        public static string PluginDirectory;

        public static Window morphWindow;
        public static Window healingWindow;

        private static double _ncuUpdateTime;

        public AdvCombatHandler(string pluginDir) : base(pluginDir)
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

            _settings.AddVariable("HealSelection", (int)HealSelection.None);

            _settings.AddVariable("DragonMorph", false);
            _settings.AddVariable("LeetMorph", false);
            _settings.AddVariable("SaberMorph", false);
            _settings.AddVariable("WolfMorph", false);

            _settings.AddVariable("ArmorBuff", false);

            _settings.AddVariable("CH", false);

            RegisterSettingsWindow("Adventurer Handler", "AdvSettingsView.xml");

            RegisterSettingsWindow("Healing", "AdvHealingView.xml");
            RegisterSettingsWindow("Morphs", "AdvMorphView.xml");

            //LE Procs
            RegisterPerkProcessor(PerkHash.LEProcAdventurerMacheteFlurry, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcAdventurerCombustion, LEProc);

            //Spells
            RegisterSpellProcessor(RelevantNanos.HEALS, Healing, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.CompleteHealingLine).OrderByStackingOrder(), CompleteHealing, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TeamHealing).OrderByStackingOrder(), TeamHealing, CombatActionPriority.High);

            //Buffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.General1HEdgedBuff).OrderByStackingOrder(), MeleeBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), RangedBuff);
            //RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ArmorBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShieldUpgrades).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageShields).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MultiwieldBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(RelevantNanos.ArmorBuffs, ArmorBuff);

            //Morphs
            RegisterSpellProcessor(RelevantNanos.DragonMorph, DragonMorph);
            RegisterSpellProcessor(RelevantNanos.LeetMorph, LeetMorph);
            RegisterSpellProcessor(RelevantNanos.WolfMorph, WolfMorph);
            RegisterSpellProcessor(RelevantNanos.SaberMorph, SaberMorph);

            RegisterSpellProcessor(RelevantNanos.DragonScales, DragonScales);
            RegisterSpellProcessor(RelevantNanos.LeetCrit, LeetCrit);
            RegisterSpellProcessor(RelevantNanos.WolfAgility, WolfAgility);
            RegisterSpellProcessor(RelevantNanos.SaberDamage, SaberDamage);

            PluginDirectory = pluginDir;

            //Items
            //RegisterItemProcessor(RelevantItems.TheWizdomOfHuzzum, RelevantItems.TheWizdomOfHuzzum, MartialArtsTeamHealAttack);

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

            if (_settings["DragonMorph"].AsBool() && _settings["LeetMorph"].AsBool())
            {
                _settings["DragonMorph"] = false;
                _settings["LeetMorph"] = false;

                Chat.WriteLine("Only activate one Morph option.");
            }
            if (_settings["DragonMorph"].AsBool() && _settings["SaberMorph"].AsBool())
            {
                _settings["DragonMorph"] = false;
                _settings["SaberMorph"] = false;

                Chat.WriteLine("Only activate one Morph option.");
            }
            if (_settings["DragonMorph"].AsBool() && _settings["WolfMorph"].AsBool())
            {
                _settings["DragonMorph"] = false;
                _settings["WolfMorph"] = false;

                Chat.WriteLine("Only activate one Morph option.");
            }
            if (_settings["SaberMorph"].AsBool() && _settings["LeetMorph"].AsBool())
            {
                _settings["SaberMorph"] = false;
                _settings["LeetMorph"] = false;

                Chat.WriteLine("Only activate one Morph option.");
            }
            if (_settings["SaberMorph"].AsBool() && _settings["WolfMorph"].AsBool())
            {
                _settings["SaberMorph"] = false;
                _settings["WolfMorph"] = false;

                Chat.WriteLine("Only activate one Morph option.");
            }
            if (_settings["LeetMorph"].AsBool() && _settings["WolfMorph"].AsBool())
            {
                _settings["LeetMorph"] = false;
                _settings["WolfMorph"] = false;

                Chat.WriteLine("Only activate one Morph option.");
            }

            if (!_settings["DragonMorph"].AsBool())
            {
                CancelBuffs(RelevantNanos.DragonMorph);
            }
            if (!_settings["LeetMorph"].AsBool())
            {
                CancelBuffs(RelevantNanos.LeetMorph);
            }
            if (!_settings["SaberMorph"].AsBool())
            {
                CancelBuffs(RelevantNanos.SaberMorph);
            }
            if (!_settings["WolfMorph"].AsBool())
            {
                CancelBuffs(RelevantNanos.WolfMorph);
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

                if (SettingsController.settingsWindow.FindView("MorphView", out Button morphView))
                {
                    morphView.Tag = SettingsController.settingsWindow;
                    morphView.Clicked = MorphView;
                }
            }

            if (SettingsController.CombatHandlerChannel == String.Empty)
            {
                SettingsController.CombatHandlerChannel = Config.IPCChannel.ToString();
            }
            if (SettingsController.AdvCompleteHealPercentage == String.Empty)
            {
                SettingsController.AdvCompleteHealPercentage = Config.AdvCompleteHealPercentage.ToString();
            }

            if (SettingsController.AdvHealPercentage == String.Empty)
            {
                SettingsController.AdvHealPercentage = Config.AdvHealPercentage.ToString();
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


        private void HealingView(object s, ButtonBase button)
        {
            if (morphWindow != null && morphWindow.IsValid)
            {
                healingWindow.FindView("HealPercentageBox", out TextInputView textinput1);
                healingWindow.FindView("CompleteHealPercentageBox", out TextInputView textinput2);

                if (SettingsController.AdvHealPercentage != String.Empty)
                {
                    if (textinput1 != null)
                        textinput1.Text = SettingsController.AdvHealPercentage;
                }

                if (SettingsController.AdvCompleteHealPercentage != String.Empty)
                {
                    if (textinput2 != null)
                        textinput2.Text = SettingsController.AdvCompleteHealPercentage;
                }

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (int.TryParse(textinput1.Text, out int healValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].AdvHealPercentage != healValue)
                        {
                            Config.CharSettings[Game.ClientInst].AdvHealPercentage = healValue;
                            SettingsController.AdvHealPercentage = healValue.ToString();
                            Config.Save();
                        }
                    }
                }
                if (textinput2 != null && textinput2.Text != String.Empty)
                {
                    if (int.TryParse(textinput2.Text, out int chhealValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].AdvCompleteHealPercentage != chhealValue)
                        {
                            Config.CharSettings[Game.ClientInst].AdvCompleteHealPercentage = chhealValue;
                            SettingsController.AdvCompleteHealPercentage = chhealValue.ToString();
                            Config.Save();
                        }
                    }
                }

                SettingsController.AppendSettingsTab("Healing", morphWindow);
            }
            else
            {
                healingWindow = Window.CreateFromXml("Healing", PluginDirectory + "\\UI\\AdvHealingView.xml",
                     windowSize: new Rect(0, 0, 240, 345),
                     windowStyle: WindowStyle.Default,
                     windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);
                healingWindow.FindView("HealPercentageBox", out TextInputView textinput1);
                healingWindow.FindView("CompleteHealPercentageBox", out TextInputView textinput2);

                if (SettingsController.AdvHealPercentage != String.Empty)
                {
                    if (textinput1 != null)
                        textinput1.Text = SettingsController.AdvHealPercentage;
                }

                if (SettingsController.AdvCompleteHealPercentage != String.Empty)
                {
                    if (textinput2 != null)
                        textinput2.Text = SettingsController.AdvCompleteHealPercentage;
                }

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (int.TryParse(textinput1.Text, out int healValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].AdvHealPercentage != healValue)
                        {
                            Config.CharSettings[Game.ClientInst].AdvHealPercentage = healValue;
                            SettingsController.AdvHealPercentage = healValue.ToString();
                            Config.Save();
                        }
                    }
                }
                if (textinput2 != null && textinput2.Text != String.Empty)
                {
                    if (int.TryParse(textinput2.Text, out int chhealValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].AdvCompleteHealPercentage != chhealValue)
                        {
                            Config.CharSettings[Game.ClientInst].AdvCompleteHealPercentage = chhealValue;
                            SettingsController.AdvCompleteHealPercentage = chhealValue.ToString();
                            Config.Save();
                        }
                    }
                }

                healingWindow.Show(true);
            }
        }

        private void MorphView(object s, ButtonBase button)
        {
            if (healingWindow != null && healingWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Morphs", healingWindow);
            }
            else
            {
                morphWindow = Window.CreateFromXml("Morphs", PluginDirectory + "\\UI\\AdvMorphView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                morphWindow.Show(true);
            }
        }

        private bool ArmorBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.DragonMorph)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        #region Morphs

        private bool DragonMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("DragonMorph")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool LeetMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("LeetMorph")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool WolfMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("WolfMorph")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool SaberMorph(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("SaberMorph")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool WolfAgility(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("WolfMorph")) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.WolfMorph)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool SaberDamage(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("SaberMorph")) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.SaberMorph)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool LeetCrit(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("LeetMorph")) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.LeetMorph)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }
        private bool DragonScales(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("DragonMorph")) { return false; }

            if (!DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.DragonMorph)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Healing

        private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell) || HealSelection.SingleTeam != (HealSelection)_settings["HealSelection"].AsInt32()) { return false; }

            return FindMemberWithHealthBelow(Convert.ToInt32(SettingsController.AdvHealPercentage), ref actionTarget);
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell) || SettingsController.AdvHealPercentage == string.Empty) { return false; }

            if (HealSelection.SingleTeam == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                return FindMemberWithHealthBelow(Convert.ToInt32(SettingsController.AdvHealPercentage), ref actionTarget);
            }
            else if (HealSelection.SingleOS == (HealSelection)_settings["HealSelection"].AsInt32())
            {
                return FindPlayerWithHealthBelow(Convert.ToInt32(SettingsController.AdvHealPercentage), ref actionTarget);
            }

            return false;
        }

        private bool CompleteHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("CH") || !CanCast(spell)
                || SettingsController.DocCompleteHealPercentage == string.Empty) { return false; }

            return FindMemberWithHealthBelow(Convert.ToInt32(SettingsController.AdvCompleteHealPercentage), ref actionTarget);
        }

        #endregion

        #region Misc

        public enum HealSelection
        {
            None, SingleTeam, SingleOS
        }
        private static class RelevantNanos
        {
            public static int[] HEALS = new[] { 223167, 252008, 252006, 136674, 136673, 143908, 82059, 136675, 136676, 82060, 136677,
                136678, 136679, 136682, 82061, 136681, 136680, 136683, 136684, 136685, 82062, 136686, 136689, 82063, 136688, 136687,
                82064, 26695 };

            public static readonly int[] ArmorBuffs = { 74173, 74174, 74175 , 74176, 74177, 74178 };
            public static readonly int[] DragonMorph = { 217670, 25994 };
            public static readonly int[] LeetMorph = { 263278, 82834 };
            public static readonly int[] WolfMorph = { 275005, 85062 };
            public static readonly int[] SaberMorph = { 217680, 85070 };
            public static readonly int[] DragonScales = { 302217, 302214 };
            public static readonly int[] WolfAgility = { 302235, 302232 };
            public static readonly int[] LeetCrit = { 302229, 302226 };
            public static readonly int[] SaberDamage = { 302243, 302240 };

        }

        private static class RelevantItems
        {

        }

        #endregion
    }
}
