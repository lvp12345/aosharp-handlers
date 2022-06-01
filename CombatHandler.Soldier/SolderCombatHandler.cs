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
    public class SoldCombathandler : GenericCombatHandler
    {
        public static IPCChannel IPCChannel;

        public static double _singleTauntTick;
        public static double _singleTaunt;

        public static Window buffWindow;
        public static Window tauntWindow;

        private static double _ncuUpdateTime;

        public static string PluginDirectory;

        public SoldCombathandler(string pluginDir) : base(pluginDir)
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

            _settings.AddVariable("SingleTaunt", false);
            _settings.AddVariable("OSTaunt", false);

            _settings.AddVariable("Burst", false);
            _settings.AddVariable("BurstTeam", false);

            _settings.AddVariable("AAOTeam", false);

            _settings.AddVariable("Init", false);
            _settings.AddVariable("InitTeam", false);

            _settings.AddVariable("NotumGrenades", false);

            _settings.AddVariable("LegShot", false);

            //settings.AddVariable("DamageTeam", false);

            RegisterSettingsWindow("Soldier Handler", "SoldierSettingsView.xml");

            RegisterSettingsWindow("Buffs", "SoldierBuffsView.xml");
            RegisterSettingsWindow("Taunts", "SoldierTauntsView.xml");

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcSoldierGrazeJugularVein, LEProc);
            RegisterPerkProcessor(PerkHash.LEProcSoldierFuriousAmmunition, LEProc);

            //Leg Shot
            RegisterPerkProcessor(PerkHash.LegShot, LegShot);

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ReflectShield).OrderByStackingOrder(), AugmentedMirrorShieldMKV);
            RegisterSpellProcessor(RelevantNanos.SolDrainHeal, SolDrainHeal);
            RegisterSpellProcessor(RelevantNanos.TauntBuffs, SingleTargetTaunt, CombatActionPriority.High);
            //RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.DamageBuffs_LineA).OrderByStackingOrder(), DamageTeam);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HPBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SiphonBox683).OrderByStackingOrder(), NotumGrenades);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MajorEvasionBuffs).OrderByStackingOrder(), GenericBuffExcludeInnerSanctum);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadowlandReflectBase).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierFullAutoBuff).OrderByStackingOrder(), GenericBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.TotalFocus).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierShotgunBuff).OrderByStackingOrder(), ShotgunBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HeavyWeaponsBuffs).OrderByStackingOrder(), HeavyWeaponBuff);

            RegisterSpellProcessor(RelevantNanos.ArBuffs, ARBuff);
            RegisterSpellProcessor(RelevantNanos.HeavyComp, HeavyCompBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SoldierDamageBase).OrderByStackingOrder(), GenericBuff);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AAOBuffs).OrderByStackingOrder(), AAOBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.PistolBuff).OrderByStackingOrder(), PistolMasteryBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.BurstBuff).OrderByStackingOrder(), BurstBuff);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.InitiativeBuffs).OrderByStackingOrder(), InitBuff);

            // Needs work for 2nd tanking Abmouth and Ayjous
            //if (TauntTools.CanTauntTool())
            //{
            //    Item tauntTool = TauntTools.GetBestTauntTool();
            //    RegisterItemProcessor(tauntTool.LowId, tauntTool.HighId, TauntTool);
            //}

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
            if (tauntWindow != null && tauntWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Buffs", tauntWindow);
            }
            else
            {
                buffWindow = Window.CreateFromXml("Buffs", PluginDirectory + "\\UI\\SoldierBuffsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                buffWindow.Show(true);
            }
        }

        private void TauntView(object s, ButtonBase button)
        {
            if (buffWindow != null && buffWindow.IsValid)
            {
                SettingsController.AppendSettingsTab("Taunts", buffWindow);
            }
            else
            {
                tauntWindow = Window.CreateFromXml("Taunts", PluginDirectory + "\\UI\\SoldierTauntsView.xml",
                    windowSize: new Rect(0, 0, 240, 345),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                tauntWindow.Show(true);
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

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                if (SettingsController.settingsWindow.FindView("BuffsView", out Button buffView))
                {
                    buffView.Tag = SettingsController.settingsWindow;
                    buffView.Clicked = BuffView;
                }

                if (SettingsController.settingsWindow.FindView("TauntsView", out Button tauntView))
                {
                    tauntView.Tag = SettingsController.settingsWindow;
                    tauntView.Clicked = TauntView;
                }
            }

            base.OnUpdate(deltaTime);
        }

        #region Instanced Logic

        private bool AAOBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("AAOTeam"))
            {
                if (DynelManager.LocalPlayer.IsInTeam())
                {
                    SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => !c.Buffs.Contains(NanoLine.AAOBuffs))
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

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool BurstBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("BurstTeam"))
            {
                if (DynelManager.LocalPlayer.IsInTeam())
                {
                    SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => SpellChecksOther(spell, spell.Nanoline, c))
                    .Where(c => c.SpecialAttacks.Contains(SpecialAttack.Burst))
                    .FirstOrDefault();

                    if (teamMemberWithoutBuff != null)
                    {
                        actionTarget.Target = teamMemberWithoutBuff;
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            if (!IsSettingEnabled("Burst")) { return false; }

            if (!DynelManager.LocalPlayer.SpecialAttacks.Contains(SpecialAttack.Burst)) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool HeavyCompBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                    .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                    .Where(c => SpellChecksOther(spell, spell.Nanoline, c))
                    .Where(c => GetWieldedWeapons(c).HasFlag(CharacterWieldedWeapon.Grenade) && c.Profession != Profession.Engineer)
                    .Where(c => GetWieldedWeapons(c).HasFlag(CharacterWieldedWeapon.Smg))
                    .Where(c => GetWieldedWeapons(c).HasFlag(CharacterWieldedWeapon.Shotgun))
                    .Where(c => GetWieldedWeapons(c).HasFlag(CharacterWieldedWeapon.AssaultRifle))
                    .Where(c => !c.Buffs.Contains(NanoLine.FixerSuppressorBuff))
                    .Where(c => !c.Buffs.Contains(NanoLine.AssaultRifleBuffs))
                    .FirstOrDefault();

                if (teamMemberWithoutBuff != null)
                {
                    if (teamMemberWithoutBuff.Identity == DynelManager.LocalPlayer.Identity
                        && GetWieldedWeapons(DynelManager.LocalPlayer).HasFlag(CharacterWieldedWeapon.AssaultRifle)) { return false; }

                    actionTarget.Target = teamMemberWithoutBuff;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Smg)
                                || BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Shotgun)
                                || BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Grenade);
        }

        private bool HeavyWeaponBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.HeavyWeapons);
        }

        private bool ShotgunBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.Shotgun);
        }

        private bool ARBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return BuffWeaponType(spell, fightingTarget, ref actionTarget, CharacterWieldedWeapon.AssaultRifle);
        }

        private bool LegShot(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("LegShot")) { return false; }

            return LegShotPerk(perk, fightingTarget, ref actionTarget);
        }

        private bool NotumGrenades(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (!IsSettingEnabled("NotumGrenades")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        private bool SingleTargetTaunt(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("OSTaunt") && Time.NormalTime > _singleTauntTick + 1)
            {
                List<SimpleChar> mobs = DynelManager.NPCs
                    .Where(c => c.IsAttacking && c.FightingTarget != null
                        && c.IsInLineOfSight
                        && !debuffTargetsToIgnore.Contains(c.Name)
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && IsNotFightingMe(c)
                        && IsAttackingUs(c)
                        && (c.FightingTarget.Profession != Profession.Enforcer
                                || c.FightingTarget.Profession != Profession.Soldier
                                || c.FightingTarget.Profession != Profession.MartialArtist))
                    .ToList();

                foreach (SimpleChar mob in mobs)
                {
                    if (mob != null)
                    {
                        _singleTauntTick = Time.NormalTime;
                        actionTarget.Target = mob;
                        actionTarget.ShouldSetTarget = true;
                        return true;
                    }
                }
            }

            if (!IsSettingEnabled("SingleTaunt")) { return false; }

            if (DynelManager.LocalPlayer.FightingTarget != null
                && (DynelManager.LocalPlayer.FightingTarget.Name == "Technomaster Sinuh"
                || DynelManager.LocalPlayer.FightingTarget.Name == "Collector"))
            {
                return true;
            }

            if (Time.NormalTime > _singleTaunt + 9)
            {
                if (fightingTarget != null)
                {
                    _singleTaunt = Time.NormalTime;
                    actionTarget.Target = fightingTarget;
                    actionTarget.ShouldSetTarget = true;
                    return true;
                }
            }

            return false;
        }

        private bool SolDrainHeal(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (DynelManager.LocalPlayer.FightingTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent <= 40) { return true; }

            return false;
        }

        private bool AugmentedMirrorShieldMKV(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (DynelManager.LocalPlayer.FightingTarget == null || !CanCast(spell)) { return false; }

            if (DynelManager.LocalPlayer.HealthPercent <= 85) { return true; }

            return false;
        }

        private bool InitBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (IsSettingEnabled("InitTeam"))
            {
                if (DynelManager.LocalPlayer.IsInTeam())
                {
                    SimpleChar teamMemberWithoutBuff = DynelManager.Characters
                        .Where(c => Team.Members.Select(t => t.Identity.Instance).Contains(c.Identity.Instance))
                        .Where(c => c.Profession != Profession.Doctor && c.Profession != Profession.NanoTechnician)
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

            if (!IsSettingEnabled("Init")) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Misc

        private static class RelevantNanos
        {
            public static readonly int[] SolDrainHeal = { 29241, 301897 };
            public static readonly int[] TauntBuffs = { 223209, 223207, 223205, 223203, 223201, 29242, 100207,
            29218, 100205, 100206, 100208, 29228};
            public const int HeavyComp = 269482;
            public static readonly int[] ArBuffs = { 275027, 203119, 29220, 203121 };
        }

        private static class RelevantItems
        {
            public const int DreadlochEnduranceBooster = 267168;
            public const int DreadlochEnduranceBoosterNanomageEdition = 267167;
        }

        #endregion
    }
}
