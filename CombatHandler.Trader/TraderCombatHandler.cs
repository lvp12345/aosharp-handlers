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
    public class TraderCombatHandler : GenericCombatHandler
    {
        public static IPCChannel IPCChannel;

        public static Window buffWindow;
        public static Window debuffWindow;
        public static Window healingWindow;

        private static double _ncuUpdateTime;
        private static double _drainTick;

        public static string PluginDirectory;

        public static SimpleChar _drainTarget;

        public TraderCombatHandler(string pluginDir) : base(pluginDir)
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

            _settings.AddVariable("DamageDrain", true);
            _settings.AddVariable("HealthDrain", false);

            //_settings.AddVariable("RKNanoDrain", false);
            //_settings.AddVariable("SLNanoDrain", false);

            //_settings.AddVariable("Heal", true);

            _settings.AddVariable("AAODrain", true);
            _settings.AddVariable("AADDrain", true);

            _settings.AddVariable("MyEnemy", true);

            _settings.AddVariable("ACDrains", true);

            _settings.AddVariable("GTH", true);

            _settings.AddVariable("Sacrifice", false);
            _settings.AddVariable("PurpleHeart", false);

            _settings.AddVariable("NanoHealTeam", false);
            _settings.AddVariable("EvadesTeam", false);

            _settings.AddVariable("LegShot", false);
            _settings.AddVariable("PerkSelection", (int)PerkSelection.Sacrifice);
            _settings.AddVariable("HealSelection", (int)HealSelection.None);
            _settings.AddVariable("DepriveSelection", (int)DepriveSelection.Target);
            _settings.AddVariable("RansackSelection", (int)RansackSelection.Target);
            _settings.AddVariable("NanoDrainSelection", (int)NanoDrainSelection.None);

            RegisterSettingsWindow("Trader Handler", "TraderSettingsView.xml");

            RegisterSettingsWindow("Buffs", "TraderBuffsView.xml");
            RegisterSettingsWindow("Debuffs", "TraderDebuffsView.xml");
            RegisterSettingsWindow("Healing", "TraderHealingView.xml");

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcTraderRigidLiquidation, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcTraderDebtCollection, LEProc);

            //Leg Shot
            RegisterPerkProcessor(PerkHash.LegShot, LegShot);

            //Heals
            RegisterSpellProcessor(RelevantNanos.Heal, Healing); // Self
            RegisterSpellProcessor(RelevantNanos.TeamHeal, TeamHealing); // Team
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DrainHeal).OrderByStackingOrder(), LEHeal);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoDrain_LineA).OrderByStackingOrder(), RKNanoDrain);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SLNanopointDrain).OrderByStackingOrder(), SLNanoDrain);
            RegisterSpellProcessor(RelevantNanos.HealthDrain, HealthDrain);

            //Self Buffs
            RegisterSpellProcessor(RelevantNanos.ImprovedQuantumUncertanity, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.UnstoppableKiller, GenericBuff);
            RegisterSpellProcessor(RelevantNanos.UmbralWranglerPremium, GenericBuff);

            //Team Buffs
            RegisterSpellProcessor(RelevantNanos.QuantumUncertanity, EvadesTeam);

            //Team Nano heal (Rouse Outfit nanoline)
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NanoPointHeals).OrderByStackingOrder(), TeamNanoHeal);

            //GTH/Your Enemy Drains
            RegisterSpellProcessor(RelevantNanos.GrandThefts, GrandTheftHumidity, CombatActionPriority.High);
            RegisterSpellProcessor(RelevantNanos.MyEnemiesEnemyIsMyFriend, MyEnemy);

            //AAO/AAD/Damage Drains
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderAADDrain).OrderByStackingOrder(), AADDrain, CombatActionPriority.Medium);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderAAODrain).OrderByStackingOrder(), AAODrain, CombatActionPriority.Medium);
            RegisterSpellProcessor(RelevantNanos.DivestDamage, DamageDrain, CombatActionPriority.Medium);

            //Deprive/Ransack Drains
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Deprive).OrderByStackingOrder(), DepriveDrain, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Ransack).OrderByStackingOrder(), RansackDrain, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Deprive).OrderByStackingOrder(), OSDepriveDrain, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderSkillTransferTargetDebuff_Ransack).OrderByStackingOrder(), OSRansackDrain, CombatActionPriority.High);

            //AC Drains/Debuffs
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderDebuffACNanos).OrderByStackingOrder(), TraderACDrain, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderACTransferTargetDebuff_Draw).OrderByStackingOrder(), TraderACDrain, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TraderACTransferTargetDebuff_Siphon).OrderByStackingOrder(), TraderACDrain, CombatActionPriority.Low);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DebuffNanoACHeavy).OrderByStackingOrder(), TraderACDrain, CombatActionPriority.Low);

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
                buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\TraderBuffsView.xml",
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
                debuffWindow = Window.CreateFromXml("Debuffs", PluginDirectory + "\\UI\\TraderDebuffsView.xml",
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

                if (SettingsController.TraderHealPercentage != String.Empty)
                {
                    if (textinput1 != null)
                        textinput1.Text = SettingsController.TraderHealPercentage;
                }

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (int.TryParse(textinput1.Text, out int healValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].TraderHealPercentage != healValue)
                        {
                            Config.CharSettings[Game.ClientInst].TraderHealPercentage = healValue;
                            SettingsController.TraderHealPercentage = healValue.ToString();
                            Config.Save();
                        }
                    }
                }

                SettingsController.AppendSettingsTab("Healing", buffWindow);
            }
            else if (debuffWindow != null && debuffWindow.IsValid)
            {
                debuffWindow.FindView("HealPercentageBox", out TextInputView textinput1);

                if (SettingsController.TraderHealPercentage != String.Empty)
                {
                    if (textinput1 != null)
                        textinput1.Text = SettingsController.TraderHealPercentage;
                }

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (int.TryParse(textinput1.Text, out int healValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].TraderHealPercentage != healValue)
                        {
                            Config.CharSettings[Game.ClientInst].TraderHealPercentage = healValue;
                            SettingsController.TraderHealPercentage = healValue.ToString();
                            Config.Save();
                        }
                    }
                }

                SettingsController.AppendSettingsTab("Healing", debuffWindow);
            }
            else
            {
                healingWindow = Window.CreateFromXml("Healing", PluginDirectory + "\\UI\\TraderHealingView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                healingWindow.FindView("HealPercentageBox", out TextInputView textinput1);

                if (SettingsController.TraderHealPercentage != String.Empty)
                {
                    if (textinput1 != null)
                        textinput1.Text = SettingsController.TraderHealPercentage;
                }

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (int.TryParse(textinput1.Text, out int healValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].TraderHealPercentage != healValue)
                        {
                            Config.CharSettings[Game.ClientInst].TraderHealPercentage = healValue;
                            SettingsController.TraderHealPercentage = healValue.ToString();
                            Config.Save();
                        }
                    }
                }

                healingWindow.Show(true);
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

                if (textinput1 != null && textinput1.Text != String.Empty)
                {
                    if (int.TryParse(textinput1.Text, out int healValue))
                    {
                        if (Config.CharSettings[Game.ClientInst].TraderHealPercentage != healValue)
                        {
                            Config.CharSettings[Game.ClientInst].TraderHealPercentage = healValue;
                            SettingsController.TraderHealPercentage = healValue.ToString();
                            Config.Save();
                        }
                    }
                }
            }


            if ((RansackSelection.OS == (RansackSelection)_settings["RansackSelection"].AsInt32()
                || DepriveSelection.OS == (DepriveSelection)_settings["DepriveSelection"].AsInt32())
                && Time.NormalTime > _drainTick + 1)
            {
                _drainTarget = DynelManager.NPCs
                    .Where(c => !debuffOSTargetsToIgnore.Contains(c.Name))
                    .Where(c => c.IsInLineOfSight)
                    .Where(c => !c.Buffs.Contains(NanoLine.Mezz) && !c.Buffs.Contains(NanoLine.AOEMezz))
                    .Where(c => c.DistanceFrom(DynelManager.LocalPlayer) < 30f)
                    .OrderBy(c => c.DistanceFrom(DynelManager.LocalPlayer))
                    .FirstOrDefault();

                _drainTick = Time.NormalTime;
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

            if (SettingsController.TraderHealPercentage == String.Empty)
            {
                SettingsController.TraderHealPercentage = Config.TraderHealPercentage.ToString();
            }

            base.OnUpdate(deltaTime);
        }

        private bool TeamHealing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Heal") || !CanCast(spell)) { return false; }

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

            return FindMemberWithHealthBelow(85, ref actionTarget);
        }

        private bool LEHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell)) { return false; }

            return FindMemberWithHealthBelow(60, ref actionTarget);
        }

        private bool Healing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!CanCast(spell) || SettingsController.TraderHealPercentage == string.Empty) { return false; }

            if (HealSelection.SingleTeam != (HealSelection)_settings["HealSelection"].AsInt32())
            {
                return FindMemberWithHealthBelow(Convert.ToInt32(SettingsController.TraderHealPercentage), ref actionTarget);
            }

            if (HealSelection.SingleOS != (HealSelection)_settings["HealSelection"].AsInt32())
            {
                return FindPlayerWithHealthBelow(Convert.ToInt32(SettingsController.TraderHealPercentage), ref actionTarget);
            }

            if (HealSelection.Team != (HealSelection)_settings["HealSelection"].AsInt32())
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

                return FindMemberWithHealthBelow(Convert.ToInt32(SettingsController.TraderHealPercentage), ref actionTarget);
            }

            return false;
        }

        protected bool EvadesTeam(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (IsSettingEnabled("EvadesTeam"))
            {
                if (DynelManager.LocalPlayer.IsInTeam())
                {
                    SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                        //.Where(c => !c.Buffs.Contains(NanoLine.MajorEvasionBuffs))
                        .Where(c => SpellChecksOther(spell, spell.Nanoline, c))
                        .FirstOrDefault();

                    if (teamMemberWithoutBuff != null)
                    {
                        actionTarget.Target = teamMemberWithoutBuff;
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            if (fightingTarget != null || !CanCast(spell)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool LegShot(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("LegShot")) { return false; }

            return LegShotPerk(perk, fightingTarget, ref actionTarget);
        }
        private bool Sacrifice(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PerkSelection.Sacrifice != (PerkSelection)_settings["PerkSelection"].AsInt32()) { return false; }

            return Sacrifice(perk, fightingTarget, ref actionTarget);
        }
        private bool PurpleHeart(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (PerkSelection.PurpleHeart != (PerkSelection)_settings["PerkSelection"].AsInt32()) { return false; }

            return PurpleHeart(perk, fightingTarget, ref actionTarget);
        }

        private bool SLNanoDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (NanoDrainSelection.Shadowlands != (NanoDrainSelection)_settings["NanoDrainSelection"].AsInt32()) { return false; }

            return DebuffTarget(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        private bool HealthDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("HealthDrain", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool RKNanoDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (NanoDrainSelection.RubiKa != (NanoDrainSelection)_settings["NanoDrainSelection"].AsInt32()) { return false; }

            return DebuffTarget(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool MyEnemy(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("MyEnemy", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool GrandTheftHumidity(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("GTH", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool OSRansackDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (RansackSelection.OS != (RansackSelection)_settings["RansackSelection"].AsInt32()) { return false; }

            if (_drainTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Ransack, out Buff buff))
            {
                if (spell.StackingOrder <= buff.StackingOrder)
                {
                    if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU)) { return false; }

                    if (buff.RemainingTime > 40) { return false; }

                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = _drainTarget;
                    return true;
                }

                return false;
            }

            if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = _drainTarget;
            return true;
        }

        private bool RansackDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (RansackSelection.Target != (RansackSelection)_settings["RansackSelection"].AsInt32()) { return false; }

            if (fightingTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU) { return false; }

            return DebuffTarget(spell, NanoLine.TraderSkillTransferTargetDebuff_Ransack, fightingTarget, ref actionTarget);
        }

        private bool OSDepriveDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DepriveSelection.OS != (DepriveSelection)_settings["DepriveSelection"].AsInt32()) { return false; }

            if (_drainTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.Buffs.Find(NanoLine.TraderSkillTransferCasterBuff_Deprive, out Buff buff))
            {
                if (spell.StackingOrder <= buff.StackingOrder)
                {
                    if (DynelManager.LocalPlayer.RemainingNCU < Math.Abs(spell.NCU - buff.NCU)) { return false; }

                    if (buff.RemainingTime > 40) { return false; }

                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = _drainTarget;
                    return true;
                }

                return false;
            }

            if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU) { return false; }

            actionTarget.ShouldSetTarget = true;
            actionTarget.Target = _drainTarget;
            return true;
        }

        private bool DepriveDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DepriveSelection.Target != (DepriveSelection)_settings["DepriveSelection"].AsInt32()) { return false; }

            if (fightingTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.RemainingNCU < spell.NCU) { return false; }

            return DebuffTarget(spell, NanoLine.TraderSkillTransferTargetDebuff_Deprive, fightingTarget, ref actionTarget);
        }

        private bool DamageDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("DamageDrain", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool AAODrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AAODrain") || fightingTarget == null) { return false; }

            return ToggledDebuffTarget("AAODrain", spell, NanoLine.TraderNanoTheft1, fightingTarget, ref actionTarget);
        }

        private bool AADDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("AADDrain") || fightingTarget == null) { return false; }

            //if (fightingTarget.Buffs.Contains(NanoLine.TraderNanoTheft2)) { return false; }

            return ToggledDebuffTarget("AADDrain", spell, NanoLine.TraderNanoTheft2, fightingTarget, ref actionTarget);
            //return ToggledDebuff("AADDrain", spell, NanoLine.TraderNanoTheft2, fightingTarget, ref actionTarget);
        }

        private bool TeamNanoHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.NanoPointHeals)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar lowNanoTeamMember = DynelManager.Characters
                    .Where(c => Team.Members
                        .Where(m => m.TeamIndex == Team.Members.FirstOrDefault(n => n.Identity == DynelManager.LocalPlayer.Identity).TeamIndex)
                            .Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.NanoPercent <= 80)
                    .FirstOrDefault();

                if (lowNanoTeamMember != null)
                {
                    actionTarget.Target = lowNanoTeamMember;
                    return true;
                }
            }

            return false;
        }

        private bool TraderACDrain(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return ToggledDebuffTarget("ACDrains", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        public enum PerkSelection
        {
            Sacrifice, PurpleHeart
        }
        public enum HealSelection
        {
            None, SingleTeam, SingleOS, Team
        }
        public enum NanoDrainSelection
        {
            None, RubiKa, Shadowlands
        }
        public enum RansackSelection
        {
            Target, OS
        }
        public enum DepriveSelection
        {
            Target, OS
        }

        private static class RelevantNanos
        {
            public const int QuantumUncertanity = 30745;
            public const int ImprovedQuantumUncertanity = 270808;
            public const int UnstoppableKiller = 275846;
            public const int DivestDamage = 273407;
            public const int UmbralWranglerPremium = 235291;
            public const int MyEnemiesEnemyIsMyFriend = 270714;
            public static int[] GrandThefts = { 269842, 280050 };
            public static int[] HealthDrain = { 270357, 77195, 76478, 76475, 76487, 76481,
                76484, 76491, 76494, 76499, 76571, 76503, 76651, 76614, 76656,
                76653, 76679, 76681, 76684, 76686, 76691, 76688, 76717, 76715,
                76720, 76722, 76724, 76727, 76729, 76732, 76742};
            public static int[] Heal = { 273410, 252155, 121496, 121500, 121501, 121499,
                121502, 121495, 121492, 121506, 121494, 121493, 121504, 121498, 121503,
                76653, 76679, 76681, 76684, 76686, 76691, 76688, 76717, 76715,
                121497, 121505};
            public static int[] TeamHeal = { 118245, 118230, 118232, 118231, 118235, 118233,
                118234, 118238, 118236, 118237, 118241, 118239, 118240, 118243, 118244,
                118242, 43374};
        }
    }
}
