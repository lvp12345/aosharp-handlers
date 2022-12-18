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
        private static Window _procWindow;

        private static View _buffView;
        private static View _debuffView;
        private static View _procView;

        private static double _ncuUpdateTime;

        public ShadeCombatHandler(string pluginDir) : base(pluginDir)
        {
            IPCChannel.RegisterCallback((int)IPCOpcode.RemainingNCU, OnRemainingNCUMessage);

            _settings.AddVariable("Buffing", true);
            _settings.AddVariable("Composites", true);

            //LE Proc
            _settings.AddVariable("ProcType1Selection", (int)ProcType1Selection.BlackenedLegacy);
            _settings.AddVariable("ProcType2Selection", (int)ProcType2Selection.Blackheart);

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
            _settings.AddVariable("DOTProc", false);
            _settings.AddVariable("StunProc", false);

            _settings.AddVariable("HealthDrain", false);
            _settings.AddVariable("SpiritSiphon", false);

            RegisterSettingsWindow("Shade Handler", "ShadeSettingsView.xml");

            //LE Proc
            RegisterPerkProcessor(PerkHash.LEProcShadeBlackenedLegacy, BlackenedLegacy, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcShadeSiphonBeing, SiphonBeing, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcShadeShadowedGift, ShadowedGift, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcShadeDrainEssence, DrainEssence, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcShadeElusiveSpirit, ElusiveSpirit, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcShadeToxicConfusion, ToxicConfusion, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcShadeSapLife, SapLife, CombatActionPriority.Low);

            RegisterPerkProcessor(PerkHash.LEProcShadeBlackheart, Blackheart, CombatActionPriority.Low);;
            RegisterPerkProcessor(PerkHash.LEProcShadeTwistedCaress, TwistedCaress, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcShadeConcealedSurprise, ConcealedSurprise, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcShadeMisdirection, Misdirection, CombatActionPriority.Low);
            RegisterPerkProcessor(PerkHash.LEProcShadeDeviousSpirit, DeviousSpirit, CombatActionPriority.Low);

            //Perks
            RelevantPerks.SpiritPhylactery.ForEach(p => RegisterPerkProcessor(p, SpiritPhylacteryPerk));
            RelevantPerks.TotemicRites.ForEach(p => RegisterPerkProcessor(p, TotemicRitesPerk));
            RelevantPerks.PiercingMastery.ForEach(p => RegisterPerkProcessor(p, PiercingMasteryPerk));

            //Spells
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.EmergencySneak).OrderByStackingOrder(), SmokeBombNano, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.NemesisNanoPrograms).OrderByStackingOrder(), ShadesCaressNano, CombatActionPriority.High);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.HealthDrain).OrderByStackingOrder(), HealthDrainNano);
            RegisterSpellProcessor(RelevantNanos.SpiritDrain, SpiritSiphonNano);

            //Items
            RegisterItemProcessor(RelevantItems.Tattoo, RelevantItems.Tattoo, TattooItem, CombatActionPriority.High);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AgilityBuff).OrderByStackingOrder(), Agility);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ConcealmentBuff).OrderByStackingOrder(), Concealment);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.FastAttackBuffs).OrderByStackingOrder(), FastAttack);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MultiwieldBuff).OrderByStackingOrder(), MultiWield);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.MartialArtsBuff).OrderByStackingOrder(), MartialArts);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.ShadePiercingBuff).OrderByStackingOrder(), Piercing);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.SneakAttackBuffs).OrderByStackingOrder(), SneakAttack);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.WeaponEffectAdd_On2).OrderByStackingOrder(), WeaponEffect);
            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.AADBuffs).OrderByStackingOrder(), AAD);

            RegisterSpellProcessor(RelevantNanos.ShadeDmgProc, DamageProc);
            RegisterSpellProcessor(RelevantNanos.ShadeStunProc, StunProc);
            RegisterSpellProcessor(RelevantNanos.ShadeInitDebuffProc, InitDebuffProc);
            RegisterSpellProcessor(RelevantNanos.ShadeDOTProc, DOTProc);

            RegisterSpellProcessor(Spell.GetSpellsForNanoline(NanoLine.RunspeedBuffs).OrderByStackingOrder(), FasterThanYourShadow);

            RegisterItemProcessor(RelevantItems.Sappo, RelevantItems.Sappo, Sappo);

            PluginDirectory = pluginDir;
        }

        public Window[] _windows => new Window[] { _buffWindow, _debuffWindow, _procWindow };

        #region Callbacks

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

        #endregion

        #region Handles

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

        private void HandleProcViewClick(object s, ButtonBase button)
        {
            Window window = _windows.Where(c => c != null && c.IsValid).FirstOrDefault();
            if (window != null)
            {
                //Cannot re-use the view, as crashes client. I don't know why.

                if (window.Views.Contains(_procView)) { return; }

                _procView = View.CreateFromXml(PluginDirectory + "\\UI\\ShadeProcsView.xml");
                SettingsController.AppendSettingsTab(window, new WindowOptions() { Name = "Procs", XmlViewName = "ShadeProcsView" }, _procView);
            }
            else if (_procWindow == null || (_procWindow != null && !_procWindow.IsValid))
            {
                SettingsController.CreateSettingsTab(_procWindow, PluginDir, new WindowOptions() { Name = "Procs", XmlViewName = "ShadeProcsView" }, _procView, out var container);
                _procWindow = container;
            }
        }

        #endregion

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

                if (SettingsController.settingsWindow.FindView("ProcsView", out Button procView))
                {
                    procView.Tag = SettingsController.settingsWindow;
                    procView.Clicked = HandleProcViewClick;
                }
            }

            if (_settings["InitDebuffProc"].AsBool() && _settings["DamageProc"].AsBool())
            {
                _settings["InitDebuffProc"] = false;
                _settings["DamageProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }
            if (_settings["InitDebuffProc"].AsBool() && _settings["DOTProc"].AsBool())
            {
                _settings["InitDebuffProc"] = false;
                _settings["DOTProc"] = false;

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
            if (_settings["DamageProc"].AsBool() && _settings["DOTProc"].AsBool())
            {
                _settings["DamageProc"] = false;
                _settings["DOTProc"] = false;

                Chat.WriteLine("Only activate one Proc option.");
            }
            if (_settings["StunProc"].AsBool() && _settings["DOTProc"].AsBool())
            {
                _settings["StunProc"] = false;
                _settings["DOTProc"] = false;

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
            if (!IsSettingEnabled("DOTProc"))
            {
                CancelBuffs(RelevantNanos.ShadeDOTProc);
            }
            if (!IsSettingEnabled("StunProc"))
            {
                CancelBuffs(RelevantNanos.ShadeStunProc);
            }
        }

        #region LE Procs

        private bool BlackenedLegacy(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.BlackenedLegacy != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DrainEssence(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.DrainEssence != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool ElusiveSpirit(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.ElusiveSpirit != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool SapLife(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.SapLife != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool ShadowedGift(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.ShadowedGift != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool SiphonBeing(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.SiphonBeing != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool ToxicConfusion(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType1Selection.ToxicConfusion != (ProcType1Selection)_settings["ProcType1Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool Blackheart(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Blackheart != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool ConcealedSurprise(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.ConcealedSurprise != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool DeviousSpirit(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.DeviousSpirit != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        private bool Misdirection(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.Misdirection != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }
        private bool TwistedCaress(PerkAction perk, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (ProcType2Selection.TwistedCaress != (ProcType2Selection)_settings["ProcType2Selection"].AsInt32()) { return false; }

            return LEProc(perk, fightingTarget, ref actionTarget);
        }

        #endregion

        #region Items

        private bool Sappo(Item item, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingtarget == null) { return false; }

            if (DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.MartialArts)) { return false; }

            return true;
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

        #endregion

        #region Perks
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

        #endregion

        #region Buffs

        #region Procs

        private bool InitDebuffProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("InitDebuffProc")) { return false; }

            return Buff(spell, spell.Nanoline, fightingtarget, ref actiontarget);
        }

        private bool DamageProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("DamageProc")) { return false; }

            return Buff(spell, spell.Nanoline, fightingtarget, ref actiontarget);
        }
        private bool DOTProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("DOTProc")) { return false; }

            return Buff(spell, spell.Nanoline, fightingtarget, ref actiontarget);
        }
        private bool StunProc(Spell spell, SimpleChar fightingtarget, ref (SimpleChar Target, bool ShouldSetTarget) actiontarget)
        {
            if (!IsSettingEnabled("StunProc")) { return false; }

            return Buff(spell, spell.Nanoline, fightingtarget, ref actiontarget);
        }

        #endregion

        //TODO: Delete other FTYS and rename... maybe this works

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
        protected bool FTYSTeamBuff(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (Team.IsInTeam)
            {
                SimpleChar target = DynelManager.Players
                    .Where(c => c.IsInLineOfSight
                        && c.DistanceFrom(DynelManager.LocalPlayer) < 30f
                        && c.Health > 0
                        & SpellChecksOther(spell, spell.Nanoline, c))
                    .FirstOrDefault();

                if (target != null)
                {
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = target;
                    return true;
                }
            }

            if (fightingTarget == null) { return false; }

            return GenericBuff(spell, fightingTarget, ref actionTarget);
        }

        protected bool Agility(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        protected bool Concealment(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        protected bool FastAttack(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        protected bool SneakAttack(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        protected bool MartialArts(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        protected bool WeaponEffect(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        protected bool AAD(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        protected bool Piercing(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }
        protected bool MultiWield(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            return Buff(spell, spell.Nanoline, fightingTarget, ref actionTarget);
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

            if (fightingTarget != null && fightingTarget.HealthPercent <= 21)
            {
                if (!_shadeSiphon)
                {
                    _shadeSiphon = true;
                    spell.Cast(fightingTarget);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Debuffs

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
                    actionTarget.ShouldSetTarget = true;
                    actionTarget.Target = fightingTarget;
                    return true;
                }
            }

            if (DynelManager.LocalPlayer.HealthPercent <= 80 && fightingTarget.HealthPercent > 5) { return true; }

            return false;
        }
        private bool HealthDrainNano(Spell spell, SimpleChar fightingTarget, ref (SimpleChar Target, bool ShouldSetTarget) actionTarget)
        {
            if (fightingTarget == null) { return false; }

            if (fightingTarget.Buffs.Contains(273390)) { return false; }

            if (DynelManager.LocalPlayer.NanoPercent > 80) { return true; }

            // Otherwise save it for if our health starts to drop
            if (DynelManager.LocalPlayer.HealthPercent >= 85) { return false; }

            return ToggledTargetDebuff("HealthDrain", spell, spell.Nanoline, fightingTarget, ref actionTarget);
        }


        #endregion

        #region Misc

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
            public const int SpiritDrain = 297342;
            public static readonly int[] FasterThanYourShadow = { 272371 };
            public static readonly int[] EVASION_BUFFS = { 275844, 29247, 28903, 28878, 28872, 218070, 218068, 218066,
            218064, 218062, 218060, 272371, 270808, 30745, 302188, 29272, 270802, 28603, 223125, 223131, 223129, 215718,
            223127, 272416, 272415, 272414, 272413, 272412};
            public static readonly int[] RK_RUN_BUFFS = { 93132, 93126, 93127, 93128, 93129, 93130, 93131, 93125 };
            public static readonly int[] ShadeDmgProc = { 224167, 224165, 224163, 210371, 210369, 210367, 210365, 210363, 210361, 210359, 210357, 210355, 210353 };
            public static readonly int[] ShadeStunProc = { 224171, 224169, 210380, 210378, 210376 };
            public static readonly int[] ShadeInitDebuffProc = { 224177, 210407, 210401 };
            public static readonly int[] ShadeDOTProc = { 224161, 224159, 210395, 210393, 210391, 210389, 210387 };
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

        public enum ProcType1Selection
        {
            BlackenedLegacy, SiphonBeing, ShadowedGift, DrainEssence, ElusiveSpirit, ToxicConfusion, SapLife
        }

        public enum ProcType2Selection
        {
            Blackheart, TwistedCaress, ConcealedSurprise, Misdirection, DeviousSpirit
        }

        #endregion
    }
}
