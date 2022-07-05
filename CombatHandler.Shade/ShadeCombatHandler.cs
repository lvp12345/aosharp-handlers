using System.Collections.Generic;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System.Linq;
using System;
using AOSharp.Common.GameData.UI;
using AOSharp.Core.IPC;
using System.Threading.Tasks;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System.Threading;
using SmokeLounge.AOtomation.Messaging.Messages;
using AOSharp.Core.Inventory;
using CombatHandler.Generic;

namespace CombatHandler.Shade
{
    public class ShadeCombatHandler : GenericCombatHandler
    {
        private static string PluginDirectory;

        private const int MissingHealthAbortCombatPercentage = 30;

        private static bool _shadeSiphon;

        private static Window _buffWindow;
        private static Window _debuffWindow;

        private static View _buffView;
        private static View _debuffView;

        private static double _ncuUpdateTime;

        public ShadeCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[Game.ClientInst].IPCChannel));
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            //IPCChannel.RegisterCallback((int)IPCOpcode.Attack, OnAttackMessage);
            //IPCChannel.RegisterCallback((int)IPCOpcode.StopAttack, OnStopAttackMessage);

            //IPCChannel.RegisterCallback((int)IPCOpcode.Disband, OnDisband);

            //Network.N3MessageSent += Network_N3MessageSent;
            //Team.TeamRequest += Team_TeamRequest;

            //Chat.RegisterCommand("reform", ReformCommand);
            //Chat.RegisterCommand("form", FormCommand);
            //Chat.RegisterCommand("disband", DisbandCommand);
            //Chat.RegisterCommand("convert", RaidCommand);

            _settings.AddVariable("Runspeed", false);
            _settings.AddVariable("RunspeedTeam", false);

            _settings.AddVariable("InitDebuffProc", false);
            _settings.AddVariable("DamageProc", false);
            _settings.AddVariable("DoTProc", false);
            _settings.AddVariable("StunProc", false);

            _settings.AddVariable("HealthDrain", false);
            _settings.AddVariable("SpiritSiphon", false);

            RegisterSettingsWindow("Shade Handler", "ShadeSettingsView.xml");

            RegisterPerkProcessor(PerkHash.LEProcShadeSiphonBeing, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcShadeBlackheart, LEProc);

            //Perks
            RelevantPerks.SpiritPhylactery.ForEach(p => RegisterPerkProcessor(p, SpiritPhylacteryPerk));
            RelevantPerks.TotemicRites.ForEach(p => RegisterPerkProcessor(p, TotemicRitesPerk));
            RelevantPerks.PiercingMastery.ForEach(p => RegisterPerkProcessor(p, PiercingMasteryPerk));

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EmergencySneak).OrderByStackingOrder(), SmokeBombNano, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NemesisNanoPrograms).OrderByStackingOrder(), ShadesCaressNano, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealthDrain).OrderByStackingOrder(), HealthDrainNano);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SpiritDrain).OrderByStackingOrder(), SpiritSiphonNano);

            //Items
            RegisterItemProcessor(RelevantItems.Tattoo, RelevantItems.Tattoo, TattooItem, CombatActionPriority.High);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AgilityBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ConcealmentBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MultiwieldBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtsBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadePiercingBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SneakAttackBuffs).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.WeaponEffectAdd_On2).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AADBuffs).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(RelevantNanos.ShadeDmgProc, DamageProc);
            RegisterSpellProcessor(RelevantNanos.ShadeStunProc, StunProc);
            RegisterSpellProcessor(RelevantNanos.ShadeInitDebuffProc, InitDebuffProc);
            RegisterSpellProcessor(RelevantNanos.ShadeDotProc, DoTProc);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RunspeedBuffs).OrderByStackingOrder(), FasterThanYourShadow);

            RegisterItemProcessor(RelevantItems.Sappo, RelevantItems.Sappo, Sappo);

            PluginDirectory = pluginDir;
        }
        public Window[] _windows => new Window[] { _buffWindow, _debuffWindow };

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

        //public static bool IsRaidEnabled(string[] param)
        //{
        //    return param.Length > 0 && "raid".Equals(param[0]);
        //}

        //public static Identity[] GetRegisteredCharactersInvite()
        //{
        //    Identity[] registeredCharacters = SettingsController.GetRegisteredCharacters();
        //    int firstTeamCount = registeredCharacters.Length > 6 ? 6 : registeredCharacters.Length;
        //    Identity[] firstTeamCharacters = new Identity[firstTeamCount];
        //    Array.Copy(registeredCharacters, firstTeamCharacters, firstTeamCount);
        //    return firstTeamCharacters;
        //}

        //public static Identity[] GetRemainingRegisteredCharacters()
        //{
        //    Identity[] registeredCharacters = SettingsController.GetRegisteredCharacters();
        //    int characterCount = registeredCharacters.Length - 6;
        //    Identity[] remainingCharacters = new Identity[characterCount];
        //    if (characterCount > 0)
        //    {
        //        Array.Copy(registeredCharacters, 6, remainingCharacters, 0, characterCount);
        //    }
        //    return remainingCharacters;
        //}

        //public static void SendTeamInvite(Identity[] targets)
        //{
        //    foreach (Identity target in targets)
        //    {
        //        if (target != DynelManager.LocalPlayer.Identity)
        //            Team.Invite(target);
        //    }
        //}

        //public static void Team_TeamRequest(object s, TeamRequestEventArgs e)
        //{
        //    if (SettingsController.IsCharacterRegistered(e.Requester))
        //    {
        //        e.Accept();
        //    }
        //}

        //public static void Network_N3MessageSent(object s, N3Message n3Msg)
        //{
        //    if (!IsActiveWindow || n3Msg.Identity != DynelManager.LocalPlayer.Identity) { return; }

        //    //Chat.WriteLine($"{n3Msg.Identity != DynelManager.LocalPlayer.Identity}");

        //    if (n3Msg.N3MessageType == N3MessageType.LookAt)
        //    {
        //        LookAtMessage lookAtMsg = (LookAtMessage)n3Msg;
        //        IPCChannel.Broadcast(new TargetMessage()
        //        {
        //            Target = lookAtMsg.Target
        //        });
        //    }
        //    else if (n3Msg.N3MessageType == N3MessageType.Attack)
        //    {
        //        AttackMessage attackMsg = (AttackMessage)n3Msg;
        //        IPCChannel.Broadcast(new AttackIPCMessage()
        //        {
        //            Target = attackMsg.Target
        //        });
        //    }
        //    else if (n3Msg.N3MessageType == N3MessageType.StopFight)
        //    {
        //        StopFightMessage stopAttackMsg = (StopFightMessage)n3Msg;
        //        IPCChannel.Broadcast(new StopAttackIPCMessage());
        //    }
        //}

        //public static void OnDisband(int sender, IPCMessage msg)
        //{
        //    Team.Leave();
        //}


        //public static void OnStopAttackMessage(int sender, IPCMessage msg)
        //{
        //    if (IsActiveWindow)
        //        return;

        //    if (Game.IsZoning)
        //        return;

        //    DynelManager.LocalPlayer.StopAttack();
        //}

        //public static void DisbandCommand(string command, string[] param, ChatWindow chatWindow)
        //{
        //    Team.Disband();
        //    IPCChannel.Broadcast(new DisbandMessage());
        //}

        //public static void RaidCommand(string command, string[] param, ChatWindow chatWindow)
        //{
        //    if (Team.IsLeader)
        //        Team.ConvertToRaid();
        //    else
        //        Chat.WriteLine("Needs to be used from leader.");
        //}

        //public static void ReformCommand(string command, string[] param, ChatWindow chatWindow)
        //{
        //    Team.Disband();
        //    IPCChannel.Broadcast(new DisbandMessage());
        //    Task task = new Task(() =>
        //    {
        //        Thread.Sleep(1000);
        //        FormCommand("form", param, chatWindow);
        //    });
        //    task.Start();
        //}

        //public static void FormCommand(string command, string[] param, ChatWindow chatWindow)
        //{
        //    if (!DynelManager.LocalPlayer.IsInTeam())
        //    {
        //        SendTeamInvite(GetRegisteredCharactersInvite());

        //        if (IsRaidEnabled(param))
        //        {
        //            Task task = new Task(() =>
        //            {
        //                Thread.Sleep(1000);
        //                Team.ConvertToRaid();
        //                Thread.Sleep(1000);
        //                SendTeamInvite(GetRemainingRegisteredCharacters());
        //            });
        //            task.Start();
        //        }
        //    }
        //    else
        //    {
        //        Chat.WriteLine("Cannot form a team. Character already in team. Disband first.");
        //    }
        //}

        //public static void OnTargetMessage(int sender, IPCMessage msg)
        //{
        //    if (IsActiveWindow)
        //        return;

        //    if (Game.IsZoning)
        //        return;

        //    TargetMessage targetMsg = (TargetMessage)msg;
        //    Targeting.SetTarget(targetMsg.Target);
        //}

        //public static void OnAttackMessage(int sender, IPCMessage msg)
        //{
        //    if (IsActiveWindow)
        //        return;

        //    if (Game.IsZoning)
        //        return;

        //    AttackIPCMessage attackMsg = (AttackIPCMessage)msg;
        //    Dynel targetDynel = DynelManager.GetDynel(attackMsg.Target);
        //    DynelManager.LocalPlayer.Attack(targetDynel, true);
        //}

        private void HandleBuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //Cannot stop Multi-Tabs. Easy fix would be correct naming of views to reference against WindowOptions - options.Name
                _buffView = View.CreateFromXml(PluginDirectory + "\\UI\\ShadeBuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Buffs", XmlViewName = "ShadeBuffsView" }, _buffView);
            }
            else if (_buffWindow == null || (_buffWindow != null && !_buffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_buffWindow, PluginDir, new WindowOptions() { Name = "Buffs", XmlViewName = "ShadeBuffsView" }, _buffView, out var container);
                _buffWindow = container;
            }
        }

        private void HandleDebuffViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.
                //Cannot stop Multi-Tabs. Easy fix would be correct naming of views to reference against WindowOptions - options.Name
                _debuffView = View.CreateFromXml(PluginDirectory + "\\UI\\ShadeDebuffsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Debuffs", XmlViewName = "ShadeDebuffsView" }, _debuffView);
            }
            else if (_debuffWindow == null || (_debuffWindow != null && !_debuffWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_debuffWindow, PluginDir, new WindowOptions() { Name = "Debuffs", XmlViewName = "ShadeDebuffsView" }, _debuffView, out var container);
                _debuffWindow = container;
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            if (Time.NormalTime > _ncuUpdateTime + 0.5f)
            {
                RemainingNCUMessage ncuMessage = RemainingNCUMessage.ForLocalPlayer();

                IPCChannel.Broadcast(ncuMessage);

                OnRemainingNCUMessage(0, ncuMessage);

                _ncuUpdateTime = Time.NormalTime;
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                {
                    buffView.Tag = SettingsController.settingsWindow;
                    buffView.Clicked = HandleBuffViewClick;
                }

                if (SettingsController.settingsWindow.FindView("DebuffsView", out Button debuffView))
                {
                    debuffView.Tag = SettingsController.settingsWindow;
                    debuffView.Clicked = HandleDebuffViewClick;
                }
            }

            if (_settings["InitDebuffProc"].AsBool() && _settings["DamageProc"].AsBool())
            {
                _settings["InitDebuffProc"] = false;
                _settings["DamageProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }
            if (_settings["InitDebuffProc"].AsBool() && _settings["DoTProc"].AsBool())
            {
                _settings["InitDebuffProc"] = false;
                _settings["DoTProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }
            if (_settings["InitDebuffProc"].AsBool() && _settings["StunProc"].AsBool())
            {
                _settings["InitDebuffProc"] = false;
                _settings["StunProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }
            if (_settings["DamageProc"].AsBool() && _settings["StunProc"].AsBool())
            {
                _settings["DamageProc"] = false;
                _settings["StunProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }
            if (_settings["DamageProc"].AsBool() && _settings["DoTProc"].AsBool())
            {
                _settings["DamageProc"] = false;
                _settings["DoTProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }
            if (_settings["StunProc"].AsBool() && _settings["DoTProc"].AsBool())
            {
                _settings["StunProc"] = false;
                _settings["DoTProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }

            if (!IsSettingEnabled("Runspeed") && !IsSettingEnabled("RunspeedTeam"))
            {
                CancelBuffs(RelevantNanos.FasterThanYourShadow);
            }
            if (!IsSettingEnabled("InitDebuffProc"))
            {
                CancelBuffs(RelevantNanos.ShadeInitDebuffProc);
            }
            if (!IsSettingEnabled("DamageProc"))
            {
                CancelBuffs(RelevantNanos.ShadeDmgProc);
            }
            if (!IsSettingEnabled("DoTProc"))
            {
                CancelBuffs(RelevantNanos.ShadeDotProc);
            }
            if (!IsSettingEnabled("StunProc"))
            {
                CancelBuffs(RelevantNanos.ShadeStunProc);
            }
        }

        private bool Sappo(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingtarget == null) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.MartialArts)) { return false; }

            return true;
        }

        private bool InitDebuffProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return ToggledBuff("InitDebuffProc", spell, fightingtarget, ref actiontarget);
        }

        private bool DamageProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return ToggledBuff("DamageProc", spell, fightingtarget, ref actiontarget);
        }
        private bool DoTProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return ToggledBuff("DoTProc", spell, fightingtarget, ref actiontarget);
        }
        private bool StunProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            return ToggledBuff("StunProc", spell, fightingtarget, ref actiontarget);
        }

        private bool ShadesCaressNano(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (!DynelManager.LocalPlayer.IsAttacking || fightingTarget == null
                 || !CanCast(spell)) { return false; }

            if (fightingTarget.HealthPercent < 5) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                List<SimpleChar> teamMembersLowHp = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => c.HealthPercent <= 80)
                    .ToList();

                if (teamMembersLowHp.Count >= 3)
                {
                    actionTarget.Target = fightingTarget;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            if (DynelManager.LocalPlayer.HealthPercent <= 80 && fightingTarget.HealthPercent > 5) { return true; }

            return false;
        }

        protected bool FTYSTeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            if (fightingTarget != null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    //.Where(c => !c.Buffs.Contains(RelevantNanos.EVASION_BUFFS))
                    .Where(c => SpellChecksOther(spell, spell.Nanoline, c))
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        private bool FasterThanYourShadow(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsInsideInnerSanctum()) { return false; }

            if (IsSettingEnabled("RunspeedTeam"))
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.RK_RUN_BUFFS))
                {
                    CancelBuffs(RelevantNanos.RK_RUN_BUFFS);
                }

                return FTYSTeamBuff(spell, fightingTarget, ref actionTarget);
            }

            if (IsSettingEnabled("Runspeed"))
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.RK_RUN_BUFFS))
                {
                    CancelBuffs(RelevantNanos.RK_RUN_BUFFS);
                }

                return GenericBuff(spell, fightingTarget, ref actionTarget);
            }

            return false;
        }

        private bool TattooItem(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            // don't use if BM is locked (we will add this dynamically later)
            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.BiologicalMetamorphosis)) { return false; }

            // don't use if we're above 40%
            if (DynelManager.LocalPlayer.HealthPercent > 40) { return false; }

            // don't use if nothing is fighting us
            if (DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0) { return false; }

            // don't use if we have another major absorb (example: nanomage booster) running
            // we could check remaining absorb stat to be slightly more effective
            if (DynelManager.LocalPlayer.Buffs.Contains(NanoLine.BioCocoon)) { return false; }

            // don't use if our fighting target has caress running
            if (fightingtarget.Buffs.Contains(275242)) { return false; }

            return true;
        }

        private bool SmokeBombNano(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("Buffing")) { return false; }

            actionTarget.ShouldSetTarget = false;

            if (DynelManager.LocalPlayer.HealthPercent <= MissingHealthAbortCombatPercentage) { return true; }

            return false;
        }

        private bool SpiritSiphonNano(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("SpiritSiphon")) { return false; }

            if (fightingTarget == null && _shadeSiphon)
            {
                _shadeSiphon = false;
            }

            if (!DynelManager.LocalPlayer.IsAttacking) { return false; }

            if (DynelManager.LocalPlayer.Nano < spell.Cost) { return false; }

            if (fightingTarget != null && DynelManager.LocalPlayer.HealthPercent <= 20)
            {
                if (!_shadeSiphon)
                {
                    _shadeSiphon = true;
                    return true;
                }
            }

            return false;
        }

        private bool HealthDrainNano(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            if (fightingTarget.Buffs.Contains(273390)) { return false; }

            if (DynelManager.LocalPlayer.NanoPercent > 80) { return true; }

            // Otherwise save it for if our health starts to drop
            if (DynelManager.LocalPlayer.HealthPercent >= 85) { return false; }

            return ToggledDebuffTarget("HealthDrain", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }

        private bool PiercingMasteryPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            // Don't PM if there are TR/SP chains in progress
            if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (RelevantPerks.TotemicRites.Contains(action.Hash) || RelevantPerks.SpiritPhylactery.Contains(action.Hash)))) { return false; }

            if (!(PerkAction.Find(PerkHash.Stab, out PerkAction stab) && PerkAction.Find(PerkHash.DoubleStab, out PerkAction doubleStab)))
                return true;

            if (perkAction.Hash == PerkHash.Perforate)
            {
                if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (action == stab || action == doubleStab))) { return false; }
            }

            if (!(PerkAction.Find(PerkHash.Stab, out PerkAction perforate) && PerkAction.Find(PerkHash.DoubleStab, out PerkAction lacerate))) { return true; }

            if (perkAction.Hash == PerkHash.Impale)
            {
                if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (action == stab || action == doubleStab || action == perforate || action == lacerate))) { return false; }
            }

            return true;
        }

        private bool SpiritPhylacteryPerk(PerkAction perkAction, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            //Don't SP if there are TR/PM chains in progress
            if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (RelevantPerks.TotemicRites.Contains(action.Hash) || RelevantPerks.PiercingMastery.Contains(action.Hash)))) { return false; }

            return true;
        }

        private bool TotemicRitesPerk(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            //Don't TR if there are SP/PM chains in progress
            if (_actionQueue.Any(x => x.CombatAction is PerkAction action && (RelevantPerks.SpiritPhylactery.Contains(action.Hash) || RelevantPerks.PiercingMastery.Contains(action.Hash)))) { return false; }

            return true;
        }

        private class RelevantItems 
        {
            public const int Sappo = 267525;
            public const int Tattoo = 269511;
        }

        private class RelevantNanos
        {
            public const int ShadesCaress = 266300;
            public const int CompositeAttribute = 223372;
            public const int CompositeNano = 223380;
            public const int CompositeMelee = 223360;
            public const int CompositeMeleeSpec = 215264;
            public static readonly int[] FasterThanYourShadow = { 272371 };
            public static readonly int[] EVASION_BUFFS = { 275844, 29247, 28903, 28878, 28872, 218070, 218068, 218066,
            218064, 218062, 218060, 272371, 270808, 30745, 302188, 29272, 270802, 28603, 223125, 223131, 223129, 215718,
            223127, 272416, 272415, 272414, 272413, 272412};
            public static readonly int[] RK_RUN_BUFFS = { 93132, 93126, 93127, 93128, 93129, 93130, 93131, 93125 };
            public static readonly int[] ShadeDmgProc = { 224167, 224165, 224163, 210371, 210369, 210367, 210365, 210363, 210361, 210359, 210357, 210355, 210353 };
            public static readonly int[] ShadeStunProc = { 224171, 224169, 210380, 210378, 210376 };
            public static readonly int[] ShadeInitDebuffProc = { 224177, 210407, 210401 };
            public static readonly int[] ShadeDotProc = { 224161, 224159, 210395, 210393, 210391, 210389, 210387 };
        }

        private class RelevantPerks
        {
            public static readonly List<PerkHash> TotemicRites = new List<PerkHash>
            {
                PerkHash.RitualOfDevotion,
                PerkHash.DevourVigor,
                PerkHash.RitualOfZeal,
                PerkHash.DevourEssence,
                PerkHash.RitualOfSpirit,
                PerkHash.DevourVitality,
                PerkHash.RitualOfBlood
            };

            public static readonly List<PerkHash> PiercingMastery = new List<PerkHash>
            {
                PerkHash.Stab,
                PerkHash.DoubleStab,
                PerkHash.Perforate,
                PerkHash.Lacerate,
                PerkHash.Impale,
                PerkHash.Gore,
                PerkHash.Hecatomb
            };

            public static readonly List<PerkHash> SpiritPhylactery = new List<PerkHash>
            {
                PerkHash.CaptureVigor,
                PerkHash.UnsealedBlight,
                PerkHash.CaptureEssence,
                PerkHash.UnsealedPestilence,
                PerkHash.CaptureSpirit,
                PerkHash.UnsealedContagion,
                PerkHash.CaptureVitality
            };
        }
    }
}
