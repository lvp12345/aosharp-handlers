using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using HelpManager.IPCMessages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace HelpManager
{
    public class HelpManager : AOPluginEntry
    {
        private static IPCChannel IPCChannel;

        public static Config Config { get; private set; }

        private static Pet healpet;

        public static string PluginDirectory;

        private static int SitPercentage;

        private Stopwatch _kitTimer = new Stopwatch();

        private static bool _init = false;

        private static double _updateTick;
        private static double _sitUpdateTimer;
        private static double _sitPetUpdateTimer;
        private static double _sitPetUsedTimer;
        private static double _shapeUsedTimer;
        private static double _morphPathingTimer;
        private static double _bellyPathingTimer;
        private static double _zixMorphTimer;

        private static double _uiDelay;

        public static bool Sitting = false;
        public static bool HealingPet = false;

        public static Window _followWindow;
        public static Window _assistWindow;
        public static Window _infoWindow;

        public static View _followView;
        public static View _assistView;
        public static View _infoView;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        protected Settings _settings;

        public static string PluginDir;

        List<Vector3> MorphBird = new List<Vector3>
        {
            new Vector3(75.5, 29.0, 58.6),
            new Vector3(37.3, 29.0, 59.0),
            new Vector3(35.6, 29.3, 30.5),
            new Vector3(37.3, 29.0, 59.0),
            new Vector3(75.5, 29.0, 58.6),
            new Vector3(75.5, 29.0, 58.6)
            //new Vector3(76.1, 29.0, 28.3)
        };

        List<Vector3> BellyPath = new List<Vector3>
        {
            new Vector3(143.1f, 90.0f, 108.2f),
            new Vector3(156.1f, 90.0f, 102.3f),
            new Vector3(178.0f, 90.0f, 97.6f)
        };

        List<Vector3> OutBellyPath = new List<Vector3>
        {
            new Vector3(214.8f, 100.6f, 126.5f),
            new Vector3(211.0f, 100.3f, 135.1f)
        };

        List<Vector3> MorphHorse = new List<Vector3>
        {
            new Vector3(128.4, 29.0, 59.6),
            new Vector3(161.9, 29.0, 59.5),
            new Vector3(163.9, 29.4, 29.6),
            new Vector3(161.9, 29.0, 59.5),
            new Vector3(128.4, 29.0, 59.6),
            new Vector3(128.4, 29.0, 59.6)
            //new Vector3(76.1, 29.0, 28.3)
        };

        private bool IsActiveWindow => GetForegroundWindow() == Process.GetCurrentProcess().MainWindowHandle;

        public override void Run(string pluginDir)
        {

            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\HelpManager\\{DynelManager.LocalPlayer.Name}\\Config.json");
            IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel));

            PluginDir = pluginDir;

            _settings = new Settings("HelpManager");

            IPCChannel.RegisterCallback((int)IPCOpcode.YalmOn, OnYalmCast);
            IPCChannel.RegisterCallback((int)IPCOpcode.YalmUse, OnYalmUse);
            IPCChannel.RegisterCallback((int)IPCOpcode.YalmOff, OnYalmCancel);

            Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannelChangedEvent += IPCChannel_Changed;
            Config.CharSettings[DynelManager.LocalPlayer.Name].SitPercentageChangedEvent += SitPercentage_Changed;

            RegisterSettingsWindow("Help Manager", "HelpManagerSettingWindow.xml");

            Game.OnUpdate += OnUpdate;

            _settings.AddVariable("AutoSit", true);

            _settings.AddVariable("MorphPathing", false);
            _settings.AddVariable("BellyPathing", false);
            _settings.AddVariable("Db3Shapes", false);

            Chat.RegisterCommand("autosit", AutoSitSwitch);

            Chat.RegisterCommand("yalm", YalmCommand);

            Chat.WriteLine("HelpManager Loaded!");
            Chat.WriteLine("/helpmanager for settings.");

            PluginDirectory = pluginDir;

            SitPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].SitPercentage;
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        public Window[] _windows => new Window[] { _assistWindow, _followWindow };

        public static void IPCChannel_Changed(object s, int e)
        {
            IPCChannel.SetChannelId(Convert.ToByte(e));
            Config.Save();
        }
        public static void SitPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].SitPercentage = e;
            Config.Save();
        }

        private void InfoView(object s, ButtonBase button)
        {
            _infoWindow = Window.CreateFromXml("Info", PluginDirectory + "\\UI\\HelpManagerInfoView.xml",
                windowSize: new Rect(0, 0, 440, 510),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            _infoWindow.Show(true);
        }

        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDir + "\\UI\\" + xmlName, _settings);
        }

        private void OnUpdate(object s, float deltaTime)
        {

            SitAndUseKit();

            if (Time.NormalTime > _zixMorphTimer + 3)
            {
                if (DynelManager.LocalPlayer.Buffs.Contains(288532) || DynelManager.LocalPlayer.Buffs.Contains(302212))
                {
                    CancelBuffs(RelevantNanos.ZixMorph);
                }

                _zixMorphTimer = Time.NormalTime;
            }

            if (Time.NormalTime > _sitPetUpdateTimer + 2)
            {
                if (DynelManager.LocalPlayer.Profession == Profession.Metaphysicist)
                    ListenerPetSit();

                _sitPetUpdateTimer = Time.NormalTime;
            }

            if (_settings["BellyPathing"].AsBool() && Time.NormalTime > _bellyPathingTimer + 1)
            {
                Dynel Pustule = DynelManager.AllDynels
                    .Where(x => x.Identity.Type == IdentityType.Terminal && DynelManager.LocalPlayer.DistanceFrom(x) < 7f
                        && x.Name == "Glowing Pustule")
                    .FirstOrDefault();

                if (Pustule != null)
                {
                    Pustule.Use();
                }

                if (DynelManager.LocalPlayer.Position.DistanceFrom(new Vector3(132.0f, 90.0f, 117.0f)) < 2f
                    && !MovementController.Instance.IsNavigating)
                {
                    MovementController.Instance.SetPath(BellyPath);
                }

                if (DynelManager.LocalPlayer.Position.DistanceFrom(new Vector3(217.0f, 94.0f, 148.0f)) < 2f
                    && !MovementController.Instance.IsNavigating)
                {
                    MovementController.Instance.SetPath(OutBellyPath);
                }

                _bellyPathingTimer = Time.NormalTime;
            }

            if (_settings["MorphPathing"].AsBool() && Time.NormalTime > _morphPathingTimer + 2)
            {
                if (!MovementController.Instance.IsNavigating && DynelManager.LocalPlayer.Buffs.Contains(281109))
                {
                    Vector3 curr = DynelManager.LocalPlayer.Position;

                    MovementController.Instance.SetPath(MorphBird);
                    MovementController.Instance.AppendDestination(curr);
                }

                if (!MovementController.Instance.IsNavigating && DynelManager.LocalPlayer.Buffs.Contains(281108))
                {
                    Vector3 curr = DynelManager.LocalPlayer.Position;

                    MovementController.Instance.SetPath(MorphHorse);
                    MovementController.Instance.AppendDestination(curr);
                }

                _morphPathingTimer = Time.NormalTime;
            }

            if (_settings["Db3Shapes"].AsBool() && Time.NormalTime > _shapeUsedTimer + 0.5)
            {
                Dynel shape = DynelManager.AllDynels
                    .Where(x => x.Identity.Type == IdentityType.Terminal && DynelManager.LocalPlayer.DistanceFrom(x) < 5f
                        && (x.Name == "Triangle of Nano Power" || x.Name == "Cylinder of Speed"
                    || x.Name == "Torus of Aim" || x.Name == "Square of Attack Power"))
                    .FirstOrDefault();

                if (shape != null)
                {
                    shape.Use();
                }

                _shapeUsedTimer = Time.NormalTime;
            }

            #region UI

            if (Time.NormalTime > _uiDelay + 1.0)
            {
                if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
                {
                    SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView channelInput);
                    SettingsController.settingsWindow.FindView("SitPercentageBox", out TextInputView sitPercentageInput);

                    if (channelInput != null && !string.IsNullOrEmpty(channelInput.Text))
                    {
                        if (int.TryParse(channelInput.Text, out int channelValue)
                            && Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel != channelValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel = channelValue;
                        }
                    }

                    if (sitPercentageInput != null && !string.IsNullOrEmpty(sitPercentageInput.Text))
                    {
                        if (int.TryParse(sitPercentageInput.Text, out int sitPercentageValue)
                            && Config.CharSettings[DynelManager.LocalPlayer.Name].SitPercentage != sitPercentageValue)
                        {
                            Config.CharSettings[DynelManager.LocalPlayer.Name].SitPercentage = sitPercentageValue;
                        }
                    }

                    if (SettingsController.settingsWindow.FindView("HelpManagerInfoView", out Button infoView))
                    {
                        infoView.Tag = SettingsController.settingsWindow;
                        infoView.Clicked = InfoView;
                    }
                }
                _uiDelay = Time.NormalTime;
            }
            #endregion
        }

        private void OnYalmCast(int sender, IPCMessage msg)
        {
            YalmOnMessage yalmMsg = (YalmOnMessage)msg;

            Spell yalm = Spell.List.FirstOrDefault(x => x.Id == yalmMsg.Spell);

            Spell yalm2 = Spell.List.FirstOrDefault(x => RelevantNanos.Yalms.Contains(x.Id));

            if (yalm != null)
            {
                yalm.Cast(false);
            }
            else if (yalm2 != null)
            {
                yalm2.Cast(false);
            }
            else
            {
                Item yalm3 = Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.Inventory).FirstOrDefault();

                if (yalm3 != null)
                    yalm3.Equip(EquipSlot.Weap_Hud1);
            }
        }

        private void OnYalmUse(int sender, IPCMessage msg)
        {
            YalmUseMessage yalmMsg = (YalmUseMessage)msg;

            Item yalm = Inventory.Items.FirstOrDefault(x => x.HighId == yalmMsg.Item);

            Item yalm2 = Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.Inventory).FirstOrDefault();

            if (yalm != null)
            {
                yalm.Equip(EquipSlot.Weap_Hud1);
            }
            else if (yalm2 != null)
            {
                yalm2.Equip(EquipSlot.Weap_Hud1);
            }
            else
            {
                Spell yalm3 = Spell.List.FirstOrDefault(x => RelevantNanos.Yalms.Contains(x.Id));

                if (yalm3 != null)
                    yalm3.Cast(false);
            }
        }

        private void OnYalmCancel(int sender, IPCMessage msg)
        {
            if (Inventory.Items.Where(x => x.Name.Contains("Yalm")).Where(x => x.Slot.Type == IdentityType.WeaponPage).Any())
            {
                Item yalm = Inventory.Items.Where(x => x.Name.Contains("Yalm")).Where(x => x.Slot.Type == IdentityType.WeaponPage).FirstOrDefault();

                if (yalm != null)
                    yalm.MoveToInventory();
            }
            else
                CancelBuffs(RelevantNanos.Yalms);
        }

        private void AutoSitSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["AutoSit"] = !_settings["AutoSit"].AsBool();
                Chat.WriteLine($"Auto sit : {_settings["AutoSit"].AsBool()}");
            }
        }

        private void YalmCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.Yalms))
            {
                CancelBuffs(RelevantNanos.Yalms);
                IPCChannel.Broadcast(new YalmOffMessage());
            }
            else if (Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.WeaponPage).Any())
            {
                Item yalm = Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.WeaponPage).FirstOrDefault();

                if (yalm != null)
                {
                    yalm.MoveToInventory();

                    IPCChannel.Broadcast(new YalmOffMessage());
                }
            }
            else if (Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.Inventory).Any())
            {
                Item yalm = Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.Inventory).FirstOrDefault();

                if (yalm != null)
                {
                    yalm.Equip(EquipSlot.Weap_Hud1);

                    IPCChannel.Broadcast(new YalmUseMessage()
                    {
                        Item = yalm.HighId
                    });
                }
            }
            else
            {
                Spell yalmbuff = Spell.List.FirstOrDefault(x => RelevantNanos.Yalms.Contains(x.Id));

                if (yalmbuff != null)
                {
                    yalmbuff.Cast(false);

                    IPCChannel.Broadcast(new YalmOnMessage()
                    {
                        Spell = yalmbuff.Id
                    });
                }
            }
        }

        private void ListenerPetSit()
        {
            healpet = DynelManager.LocalPlayer.Pets.Where(x => x.Type == PetType.Heal).FirstOrDefault();

            Item kit = Inventory.Items.Where(x => RelevantItems.Kits.Contains(x.Id)).FirstOrDefault();

            if (healpet == null || kit == null) { return; }

            if (_settings["AutoSit"].AsBool())
            {
                if (CanUseSitKit() && Time.NormalTime > _sitPetUsedTimer + 16
                    && DynelManager.LocalPlayer.DistanceFrom(healpet.Character) < 10f && healpet.Character.IsInLineOfSight)
                {
                    if (healpet.Character.Nano == 10) { return; }

                    if (healpet.Character.Nano / PetMaxNanoPool() * 100 > 55) { return; }

                    MovementController.Instance.SetMovement(MovementAction.SwitchToSit);

                    if (DynelManager.LocalPlayer.MovementState == MovementState.Sit
                        && DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Treatment))
                    {

                        MovementController.Instance.SetMovement(MovementAction.LeaveSit);

                        _sitPetUsedTimer = Time.NormalTime;
                    }
                }
            }
        }

        private void SitAndUseKit()
        {
            Spell spell = Spell.List.FirstOrDefault(x => x.IsReady);

            Item kit = Inventory.Items.FirstOrDefault(x => RelevantItems.Kits.Contains(x.Id));

            if (kit == null || spell == null)
            {
                return;
            }

            if (!DynelManager.LocalPlayer.Buffs.Contains(280488) && CanUseSitKit())
            {
                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Treatment) &&
                    DynelManager.LocalPlayer.MovementState != MovementState.Sit)
                {
                    if (DynelManager.LocalPlayer.NanoPercent < 66 || DynelManager.LocalPlayer.HealthPercent < 66)
                    {
                        // Switch to sitting
                        MovementController.Instance.SetMovement(MovementAction.SwitchToSit);
                    }
                }
            }

            if (DynelManager.LocalPlayer.MovementState == MovementState.Sit
           && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Treatment))
            {
                // If the Stopwatch hasn't been started or 2 seconds have elapsed
                if (!_kitTimer.IsRunning || _kitTimer.ElapsedMilliseconds >= 2000)
                {
                    if (DynelManager.LocalPlayer.NanoPercent < 90 || DynelManager.LocalPlayer.HealthPercent < 90)
                    {
                        // Use the kit
                        kit.Use(DynelManager.LocalPlayer, true);

                        // Reset and start the Stopwatch
                        _kitTimer.Restart();
                    }
                }
            }

            if (DynelManager.LocalPlayer.MovementState == MovementState.Sit
            && DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Treatment))
            {
                if (DynelManager.LocalPlayer.NanoPercent > 66 || DynelManager.LocalPlayer.HealthPercent > 66)
                {
                    // Leave sitting if treatment cooldown is active
                    MovementController.Instance.SetMovement(MovementAction.LeaveSit);
                }
            }
        }

        public static void CancelBuffs(int[] buffsToCancel)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs)
            {
                if (buffsToCancel.Contains(buff.Id))
                    buff.Remove();
            }
        }

        private bool BeingAttacked()
        {
            if (Team.IsInTeam)
            {
                return DynelManager.Characters
                    .Any(c => c.FightingTarget != null
                        && Team.Members.Select(m => m.Name).Contains(c.FightingTarget.Name));
            }

            return DynelManager.Characters
                    .Any(c => c.FightingTarget != null
                        && c.FightingTarget.Name == DynelManager.LocalPlayer.Name);
        }

        private bool CanUseSitKit()
        {
            List<Item> sitKits = Inventory.FindAll("Health and Nano Recharger").Where(c => c.Id != 297274).ToList();

            if (Inventory.Find(297274, out Item premSitKit))
            {
                if (DynelManager.LocalPlayer.IsAlive && !BeingAttacked() && DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0
                    && !Team.IsInCombat() && DynelManager.LocalPlayer.FightingTarget == null
                    && !DynelManager.LocalPlayer.IsMoving && !Game.IsZoning) { return true; }
            }

            if (!sitKits.Any()) { return false; }

            if (DynelManager.LocalPlayer.IsAlive && !BeingAttacked() && DynelManager.LocalPlayer.GetStat(Stat.NumFightingOpponents) == 0
                    && !Team.IsInCombat() && DynelManager.LocalPlayer.FightingTarget == null
                    && !DynelManager.LocalPlayer.IsMoving && !Game.IsZoning)
            {
                foreach (Item sitKit in sitKits.OrderBy(x => x.QualityLevel))
                {
                    int skillReq = (sitKit.QualityLevel > 200 ? (sitKit.QualityLevel % 200 * 3) + 1501 : (int)(sitKit.QualityLevel * 7.5f));

                    if (DynelManager.LocalPlayer.GetStat(Stat.FirstAid) >= skillReq || DynelManager.LocalPlayer.GetStat(Stat.Treatment) >= skillReq)
                        return true;
                }
            }

            return false;
        }

        private float PetMaxNanoPool()
        {
            if (healpet.Character.Level == 215)
                return 5803;
            else if (healpet.Character.Level == 192)
                return 13310;
            else if (healpet.Character.Level == 169)
                return 11231;
            else if (healpet.Character.Level == 146)
                return 9153;
            else if (healpet.Character.Level == 123)
                return 7169;
            else if (healpet.Character.Level == 99)
                return 5327;
            else if (healpet.Character.Level == 77)
                return 3807;
            else if (healpet.Character.Level == 55)
                return 2404;
            else if (healpet.Character.Level == 33)
                return 1234;
            else if (healpet.Character.Level == 14)
                return 414;

            return 0;
        }

        private static class RelevantNanos
        {
            public static readonly int[] ZixMorph = { 288532, 302212 };
            public static readonly int[] Yalms = {
                290473, 281569, 301672, 270984, 270991, 273468, 288795, 270993, 270995, 270986, 270982,
                296034, 296669, 304437, 270884, 270941, 270836, 287285, 288816, 270943, 270939, 270945,
                270711, 270731, 270645, 284061, 288802, 270764, 277426, 288799, 270738, 270779, 293619,
                294781, 301669, 301700, 301670, 120499, 82835
            };
        }

        private static class RelevantItems
        {
            public static readonly int[] Kits = {
                297274, 293296, 291084, 291083, 291082
            };
        }
    }
}
