using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.IPC;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using AOSharp.Pathfinding;
using HelpManager.IPCMessages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static HelpManager.IPCMessages.HoverboardMessage;
using Debug = AOSharp.Core.Debug;


namespace HelpManager
{
    public class HelpManager : AOPluginEntry
    {
        private static IPCChannel IPCChannel;
        public static string previousErrorMessage = string.Empty;

        public static Config Config { get; private set; }

        public static SMovementController SMovementController { get; set; }

        private static double _sitPetUpdateTimer;

        private static double _shapeUsedTimer;
        private static double _morphPathingTimer;
        private static double _bellyPathingTimer;
        private static double _zixMorphTimer;

        public static int KitHealthPercentage = 0;
        public static int KitNanoPercentage = 0;

        private static double _uiDelay;

        public static bool Sitting = false;
        public static bool HealingPet = false;

        public static Window _followWindow;
        public static Window _assistWindow;
        public static Window _infoWindow;
        public static Window _eumenidesWindow;
        public static Window _pohWindow;

        public static View _followView;
        public static View _assistView;
        public static View _infoView;
        public static View _eumenidesView;
        public static View _pohView;
        public static Vector3 returnPosition;
        public static bool InPos;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public static Settings _settings;

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
            new Vector3(178.0f, 90.0f, 97.6f),
            new Vector3(132.0f, 90.0f, 117.0f),
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

        public override void Run()
        {
            try
            {
                _settings = new Settings("HelpManager");

                

                Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\HelpManager\\{DynelManager.LocalPlayer.Name}\\Config.json");

                SMovementControllerSettings mSettings = new SMovementControllerSettings
                {
                    NavMeshSettings = new SNavMeshSettings { DrawNavMesh = false, DrawDistance = 30 },

                    PathSettings = new SPathSettings
                    {
                        DrawPath = true,
                        MinRotSpeed = 10,
                        MaxRotSpeed = 30,
                        UnstuckUpdate = 5000,
                        UnstuckThreshold = 2f,
                        RotUpdate = 10,
                        MovementUpdate = 200,
                        PathRadius = 0.26f,
                        Extents = new Vector3(1.0f, 0.1f, 1.0f)
                    }
                };

                SMovementController.Set(mSettings);
                SMovementController.AutoLoadNavmeshes($"{PluginDirectory}\\Meshes");

                IPCChannel = new IPCChannel(Convert.ToByte(Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel));
                KitHealthPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage;
                KitNanoPercentage = Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage;

                IPCChannel.RegisterCallback((int)IPCOpcode.YalmAction, OnYalmAction);
                IPCChannel.RegisterCallback((int)IPCOpcode.Hoverboard, OnHoverboardAction);

                IPCChannel.RegisterCallback((int)IPCOpcode.UISettings, BroadcastSettingsReceived);
                IPCChannel.RegisterCallback((int)IPCOpcode.POHPathing, POHPathingReceived);
                IPCChannel.RegisterCallback((int)IPCOpcode.POHBool, POHBoolReceived);

                Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannelChangedEvent += IPCChannel_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentageChangedEvent += KitHealthPercentage_Changed;
                Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentageChangedEvent += KitNanoPercentage_Changed;

                RegisterSettingsWindow("Help Manager", "HelpManagerSettingWindow.xml");

                Game.OnUpdate += OnUpdate;

                _settings.AddVariable("AutoSit", false);
                _settings.AddVariable("Traps", false);
                _settings.AddVariable("MorphPathing", false);
                _settings.AddVariable("BellyPathing", false);
                _settings.AddVariable("Eumenides", false);
                _settings.AddVariable("Db3Shapes", false);
                _settings.AddVariable("Rift", false);

                _settings.AddVariable("Positions", (int)Positions.Center);
                _settings.AddVariable("POHPositions", (int)POHPositions.None);

                Chat.RegisterCommand("autosit", AutoSitSwitch);

                Chat.RegisterCommand("yalm", YalmCommand);

                Chat.RegisterCommand("hoverboard", HoverboardCommand);

                if (Game.IsNewEngine)
                {
                    Chat.WriteLine("Does not work on this engine!");
                }
                else
                {
                    Chat.WriteLine("HelpManager Loaded!");
                    Chat.WriteLine("/helpmanager for settings.");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = "An error occurred on line " + GetLineNumber(ex) + ": " + ex.Message;

                if (errorMessage != previousErrorMessage)
                {
                    Chat.WriteLine(errorMessage);
                    Chat.WriteLine("Stack Trace: " + ex.StackTrace);
                    previousErrorMessage = errorMessage;
                }
            }
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        public Window[] _windows => new Window[] { _assistWindow, _followWindow, _infoWindow, _eumenidesWindow, _pohWindow };

        public static void IPCChannel_Changed(object s, int e)
        {
            IPCChannel.SetChannelId(Convert.ToByte(e));
            Config.Save();
        }

        public static void KitHealthPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage = e;
            KitHealthPercentage = e;
            Config.Save();
        }
        public static void KitNanoPercentage_Changed(object s, int e)
        {
            Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage = e;
            KitNanoPercentage = e;
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

        private void EumenidesView(object s, ButtonBase button)
        {
            _eumenidesWindow = Window.CreateFromXml("Eumenides", PluginDirectory + "\\UI\\HelpManagerEumenidesView.xml",
                windowSize: new Rect(0, 0, 440, 510),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            _eumenidesWindow.Show(true);
        }

        private void POHView(object s, ButtonBase button)
        {
            _pohWindow = Window.CreateFromXml("POH", PluginDirectory + "\\UI\\HelpManagerPOHView.xml",
                windowSize: new Rect(0, 0, 440, 510),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            _pohWindow.Show(true);
        }

        private void POHPathButtonClicked(object s, ButtonBase button)
        {
            if (Playfield.ModelIdentity.Instance != 8020) return;

            IPCChannel.Broadcast(new POHPathing
            {
                Position = DynelManager.LocalPlayer.Position,
            });

            if (returnPosition == Vector3.Zero)
            {
                returnPosition = DynelManager.LocalPlayer.Position;
            }

            POH.HandlePathingToPOS();
        }

        private void UISettingsButtonClicked(object s, ButtonBase button)
        {
            IPCChannel.Broadcast(new UISettings()
            {
                AutoSit = _settings["AutoSit"].AsBool(),
                MorphPathing = _settings["MorphPathing"].AsBool(),
                BellyPathing = _settings["BellyPathing"].AsBool(),
                Eumenides = _settings["Eumenides"].AsBool(),
                Db3Shapes = _settings["Db3Shapes"].AsBool(),
            });
        }

        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDirectory + "\\UI\\" + xmlName, _settings);
        }

        private void BroadcastSettingsReceived(int arg1, IPCMessage message)
        {
            if (message is UISettings uISettings)
            {
                _settings["AutoSit"] = uISettings.AutoSit;
                _settings["MorphPathing"] = uISettings.MorphPathing;
                _settings["BellyPathing"] = uISettings.BellyPathing;
                _settings["Eumenides"] = uISettings.Eumenides;
                _settings["Db3Shapes"] = uISettings.Db3Shapes;
            }
        }

        private void POHPathingReceived(int arg1, IPCMessage message)
        {
            if (Playfield.ModelIdentity.Instance != 8020) return;

            POHPathing Pos = message as POHPathing;

            if (returnPosition == Vector3.Zero)
            {
                returnPosition = Pos.Position;
            }

            POH.HandlePathingToPOS();
        }

        private void POHBoolReceived(int arg1, IPCMessage message)
        {
            POHBool pos = message as POHBool;

            if (pos.AtPOS1)
            {
                Chat.WriteLine("Pos 1 ready");
            }
            if (pos.AtPOS2)
            {
                Chat.WriteLine("Pos 2 ready");
            }
            if (pos.AtPOS3)
            {
                Chat.WriteLine("Pos 3 ready");
            }
            if (pos.AtPOS4)
            {
                Chat.WriteLine("Pos 4 ready");
            }
        }

        private void OnUpdate(object s, float deltaTime)
        {
            try
            {

                if (_settings["AutoSit"].AsBool())
                {
                    Kits kitsInstance = new Kits();

                    kitsInstance.SitAndUseKit();

                    if (Time.AONormalTime > _sitPetUpdateTimer + 2)
                    {
                        if (DynelManager.LocalPlayer.Profession == Profession.Metaphysicist)
                        {
                            PetSitKit();
                        }
                        _sitPetUpdateTimer = Time.AONormalTime;
                    }
                }

                if (_settings["Traps"].AsBool())
                {
                    foreach (Dynel dynel in DynelManager.AllDynels.Where(d => DynelManager.LocalPlayer.Position.DistanceFrom(d.Position) < 60))
                    {
                        if (dynel.Name.Contains("Mine") || dynel.Name.Contains("Trap") || dynel.Name.Contains("Collision Spawn"))
                        {
                            var rad = dynel.Radius;

                            if (rad > 1)
                            {
                                Debug.DrawSphere(dynel.Position, rad, DebuggingColor.Red);
                            }
                            else
                            {
                                Debug.DrawSphere(dynel.Position, 1, DebuggingColor.Red);
                            }
                        }
                    }
                }

                if (Playfield.ModelIdentity.Instance == 8020)
                {
                    AtPos();

                    if (DynelManager.LocalPlayer.Position.DistanceFrom(returnPosition) < 1)
                    {
                        if (!SMovementController.IsNavigating())
                        {
                            returnPosition = Vector3.Zero;
                            InPos = false;
                        }
                    }
                }

                if (Playfield.ModelIdentity.Instance == 8050)
                {
                    if (_settings["Rift"].AsBool())
                    {
                        foreach (var rift in DynelManager.AllDynels.Where(d => d.Name == "Unstable Rift" && d.Identity.Type == IdentityType.Terminal))
                        {
                            var timeremaining = rift.GetStat(Stat.TimeExist);

                            if (timeremaining < 1000)
                            {
                                Debug.DrawSphere(rift.Position, 5, DebuggingColor.Red);
                            }
                            else if (timeremaining < 2000)
                            {
                                Debug.DrawSphere(rift.Position, 10, DebuggingColor.Yellow);
                            }
                            else if (timeremaining < 3000)
                            {
                                Debug.DrawSphere(rift.Position, 20, DebuggingColor.Green);
                            }
                        }
                    }
                }

                if (Playfield.ModelIdentity.Instance == 9070)
                {
                    if (_settings["Eumenides"].AsBool())
                    {
                        var _eumenides = DynelManager.NPCs.Where(c => c.Name == "Eumenides").FirstOrDefault();

                        if (DynelManager.LocalPlayer.Room.Name == "Shopping Dead-end")
                        {
                            if (_eumenides != null)
                            {
                                Eumenides.HandleEumenides();
                            }
                        }
                    }

                    if (_settings["BellyPathing"].AsBool() && Time.AONormalTime > _bellyPathingTimer)
                    {
                        var Pustule = DynelManager.AllDynels
                        .Where(x => x.Name == "Glowing Pustule")
                        .FirstOrDefault();

                        var loaclPlayerPosition = DynelManager.LocalPlayer.Position;

                        var bellyRoom = Playfield.Rooms.FirstOrDefault(c => c.Name == "Abmouth's Stomach");
                        var abbyRoom = Playfield.Rooms.FirstOrDefault(c => c.Name == "Abmouth Showdown");
                        var playerRoom = DynelManager.LocalPlayer.Room;

                        if (playerRoom == bellyRoom)
                        {
                            if (Pustule != null)
                            {
                                if (loaclPlayerPosition.DistanceFrom(Pustule.Position) > 5)
                                {
                                    if (!MovementController.Instance.IsNavigating)
                                    {
                                        if (loaclPlayerPosition.DistanceFrom(new Vector3(133.3458f, 90.01f, 118.7395f)) < 4f)
                                        {
                                            MovementController.Instance.SetDestination(new Vector3(131.9f, 90.0f, 104.8f));
                                        }
                                        else
                                        {
                                            MovementController.Instance.SetDestination(Pustule.Position);
                                        }
                                    }
                                }
                                else
                                {
                                    if (MovementController.Instance.IsNavigating)
                                    {
                                        MovementController.Instance.Halt();
                                    }
                                    else
                                    {
                                        Pustule.Use();
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (playerRoom == abbyRoom)
                            {
                                if (!MovementController.Instance.IsNavigating)
                                {
                                    if (loaclPlayerPosition.DistanceFrom(new Vector3(217.0f, 94.0f, 148.0f)) < 2f)
                                    {
                                        MovementController.Instance.SetPath(OutBellyPath);
                                    }
                                }
                            }
                        }

                        _bellyPathingTimer = Time.AONormalTime + 1;
                    }
                }

                if (Playfield.ModelIdentity.Instance == 4021)
                {
                    if (_settings["Db3Shapes"].AsBool() && Time.AONormalTime > _shapeUsedTimer + 0.5)
                    {
                        Dynel shape = DynelManager.AllDynels
                            .Where(x => x.Identity.Type == IdentityType.Terminal && DynelManager.LocalPlayer.DistanceFrom(x) < 5f
                                && (x.Name == "Triangle of Nano Power" || x.Name == "Cylinder of Speed"
                            || x.Name == "Torus of Aim" || x.Name == "Square of Attack Power"))
                            .FirstOrDefault();

                        shape?.Use();

                        _shapeUsedTimer = Time.AONormalTime;
                    }
                }

                if (Playfield.ModelIdentity.Instance == 6015)
                {
                    if (_settings["MorphPathing"].AsBool() && Time.AONormalTime > _morphPathingTimer + 2)
                    {
                        if (!MovementController.Instance.IsNavigating)
                        {
                            if (DynelManager.LocalPlayer.Buffs.Contains(281109))
                            {
                                Vector3 curr = DynelManager.LocalPlayer.Position;

                                MovementController.Instance.SetPath(MorphBird);
                                MovementController.Instance.AppendDestination(curr);
                            }

                            if (DynelManager.LocalPlayer.Buffs.Contains(281108))
                            {
                                Vector3 curr = DynelManager.LocalPlayer.Position;

                                MovementController.Instance.SetPath(MorphHorse);
                                MovementController.Instance.AppendDestination(curr);
                            }
                        }
                        _morphPathingTimer = Time.AONormalTime;
                    }
                }

                if (Time.AONormalTime > _zixMorphTimer + 3)
                {
                    if (DynelManager.LocalPlayer.Buffs.Contains(288532) || DynelManager.LocalPlayer.Buffs.Contains(302212))
                    {
                        CancelBuffs(RelevantNanos.ZixMorph);
                    }

                    _zixMorphTimer = Time.AONormalTime;
                }

                #region UI

                if (Time.AONormalTime > _uiDelay + 1.0)
                {
                    if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
                    {
                        SettingsController.settingsWindow.FindView("ChannelBox", out TextInputView channelInput);
                        SettingsController.settingsWindow.FindView("KitHealthPercentageBox", out TextInputView kitHealthInput);
                        SettingsController.settingsWindow.FindView("KitNanoPercentageBox", out TextInputView kitNanoInput);

                        if (channelInput != null && !string.IsNullOrEmpty(channelInput.Text))
                        {
                            if (int.TryParse(channelInput.Text, out int channelValue)
                                && Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel != channelValue)
                            {
                                Config.CharSettings[DynelManager.LocalPlayer.Name].IPCChannel = channelValue;
                            }
                        }

                        if (kitHealthInput != null && !string.IsNullOrEmpty(kitHealthInput.Text))
                        {
                            if (int.TryParse(kitHealthInput.Text, out int kitHealthValue))
                            {
                                if (Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage != kitHealthValue)
                                {
                                    Config.CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage = kitHealthValue;
                                }
                            }
                        }

                        if (kitNanoInput != null && !string.IsNullOrEmpty(kitNanoInput.Text))
                        {
                            if (int.TryParse(kitNanoInput.Text, out int kitNanoValue))
                            {
                                if (Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage != kitNanoValue)
                                {
                                    Config.CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage = kitNanoValue;
                                }
                            }
                        }

                        if (SettingsController.settingsWindow.FindView("HelpManagerInfoView", out Button infoView))
                        {
                            infoView.Tag = SettingsController.settingsWindow;
                            infoView.Clicked = InfoView;
                        }
                        if (SettingsController.settingsWindow.FindView("BroadcastSettingsView", out Button settingsButton))
                        {
                            settingsButton.Tag = SettingsController.settingsWindow;
                            settingsButton.Clicked = UISettingsButtonClicked;
                        }
                        if (SettingsController.settingsWindow.FindView("POHPathButton", out Button pathButton))
                        {
                            pathButton.Tag = SettingsController.settingsWindow;
                            pathButton.Clicked = POHPathButtonClicked;
                        }
                        if (SettingsController.settingsWindow.FindView("EumenidesPositionsView", out Button eumenidesView))
                        {
                            eumenidesView.Tag = SettingsController.settingsWindow;
                            eumenidesView.Clicked = EumenidesView;
                        }
                        if (SettingsController.settingsWindow.FindView("POHView", out Button pohView))
                        {
                            pohView.Tag = SettingsController.settingsWindow;
                            pohView.Clicked = POHView;
                        }
                    }

                    _uiDelay = Time.AONormalTime;
                }
                #endregion

            }
            catch (Exception ex)
            {
                var errorMessage = "An error occurred on line " + GetLineNumber(ex) + ": " + ex.Message;

                if (errorMessage != previousErrorMessage)
                {
                    Chat.WriteLine(errorMessage);
                    Chat.WriteLine("Stack Trace: " + ex.StackTrace);
                    previousErrorMessage = errorMessage;
                }
            }
        }

        void AtPos()
        {
            //floor 0ne
            var pos1 = new Vector3(204.1, 7.8, 99.1);
            var pos2 = new Vector3(228.2, 7.8, 112.3);

            //floor two
            var pos3 = new Vector3(71.2, 6.0, 117.2);
            var pos4 = new Vector3(50.2, 6.0, 48.9);
            var pos5 = new Vector3(122.8, 6.0, 37.1);

            //floor three
            var pos6 = new Vector3(291.5, 9.0, 223.1);
            var pos7 = new Vector3(275.1, 6.0, 180.9);
            var pos8 = new Vector3(336.6, 6.0, 134.1);
            var pos9 = new Vector3(357.0, 6.0, 30.2);

            var playerPos = DynelManager.LocalPlayer.Position;

            if (!InPos)
            {
                if (playerPos.DistanceFrom(pos1) < 1 || playerPos.DistanceFrom(pos3) < 1 || playerPos.DistanceFrom(pos6) < 1)
                {
                    Chat.WriteLine("Pos 1 ready");
                    IPCChannel.Broadcast(new POHBool
                    {
                        AtPOS1 = true,

                    });
                    InPos = true;
                }
                else if (playerPos.DistanceFrom(pos2) < 1 || playerPos.DistanceFrom(pos4) < 1 || playerPos.DistanceFrom(pos7) < 1)
                {
                    Chat.WriteLine("Pos 2 ready");
                    IPCChannel.Broadcast(new POHBool
                    {
                        AtPOS2 = true,
                    });
                    InPos = true;
                }
                else if (playerPos.DistanceFrom(pos5) < 1 || playerPos.DistanceFrom(pos8) < 1)
                {
                    Chat.WriteLine("Pos 3 ready");
                    IPCChannel.Broadcast(new POHBool
                    {
                        AtPOS3 = true,
                    });
                    InPos = true;
                }
                else if (playerPos.DistanceFrom(pos9) < 1)
                {
                    Chat.WriteLine("Pos 4 ready");
                    IPCChannel.Broadcast(new POHBool
                    {
                        AtPOS4 = true,
                    });
                    InPos = true;
                }
            }
        }

        private void OnYalmAction(int sender, IPCMessage msg)
        {
            YalmActionMessage yalmMsg = (YalmActionMessage)msg;

            switch (yalmMsg.Action)
            {
                case YalmActionMessage.ActionType.On:
                    OnYalmCast(yalmMsg.Spell);
                    break;
                case YalmActionMessage.ActionType.Use:
                    OnYalmUse(yalmMsg.Item);
                    break;
                case YalmActionMessage.ActionType.Off:
                    OnYalmCancel();
                    break;
            }
        }

        private void OnYalmCast(int spellId)
        {
            Spell yalm = Spell.List.FirstOrDefault(x => x.Id == spellId);
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
                yalm3?.Equip(EquipSlot.Weap_Hud1);
            }
        }

        private void OnYalmUse(int itemId)
        {
            Item yalm = Inventory.Items.FirstOrDefault(x => x.HighId == itemId);
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
                yalm3?.Cast(false);
            }
        }

        private void OnYalmCancel()
        {
            if (Inventory.Items.Where(x => x.Name.Contains("Yalm")).Where(x => x.Slot.Type == IdentityType.WeaponPage).Any())
            {
                Item yalm = Inventory.Items.Where(x => x.Name.Contains("Yalm")).Where(x => x.Slot.Type == IdentityType.WeaponPage).FirstOrDefault();
                yalm?.MoveToInventory();
            }
            else
            {
                CancelBuffs(RelevantNanos.Yalms);
            }
        }

        private void YalmCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.Yalms))
            {
                CancelBuffs(RelevantNanos.Yalms);
                IPCChannel.Broadcast(new YalmActionMessage { Action = YalmActionMessage.ActionType.Off });
            }
            else if (Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.WeaponPage).Any())
            {
                Item yalm = Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.WeaponPage).FirstOrDefault();
                if (yalm != null)
                {
                    yalm.MoveToInventory();
                    IPCChannel.Broadcast(new YalmActionMessage { Action = YalmActionMessage.ActionType.Off });
                }
            }
            else if (Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.Inventory).Any())
            {
                Item yalm = Inventory.Items.Where(x => x.Name.Contains("Yalm") || x.Name.Contains("Ganimedes")).Where(x => x.Slot.Type == IdentityType.Inventory).FirstOrDefault();
                if (yalm != null)
                {
                    yalm.Equip(EquipSlot.Weap_Hud1);
                    IPCChannel.Broadcast(new YalmActionMessage { Action = YalmActionMessage.ActionType.Use, Item = yalm.HighId });
                }
            }
            else
            {
                Spell yalmbuff = Spell.List.FirstOrDefault(x => RelevantNanos.Yalms.Contains(x.Id));
                if (yalmbuff != null)
                {
                    yalmbuff.Cast(false);
                    IPCChannel.Broadcast(new YalmActionMessage { Action = YalmActionMessage.ActionType.On, Spell = yalmbuff.Id });
                }
            }
        }

        private void OnHoverboardAction(int sender, IPCMessage msg)
        {
            HoverboardMessage hoverboardMsg = (HoverboardMessage)msg;

            switch (hoverboardMsg.Action)
            {
                case HoverboardAction.On:
                    Spell hoverboardOn = Spell.List.FirstOrDefault(x => RelevantNanos.Hoverboards.Contains(x.Id));
                    hoverboardOn?.Cast(false);
                    break;
                case HoverboardAction.Off:
                    CancelBuffs(RelevantNanos.Hoverboards);
                    break;
            }
        }

        private void HoverboardCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(RelevantNanos.Hoverboards))
            {
                CancelBuffs(RelevantNanos.Hoverboards);
                IPCChannel.Broadcast(new HoverboardMessage()
                {
                    Action = HoverboardAction.Off
                });
            }
            else
            {
                Spell hoverboardbuff = Spell.List.FirstOrDefault(x => RelevantNanos.Hoverboards.Contains(x.Id));

                if (hoverboardbuff != null)
                {
                    hoverboardbuff.Cast(false);

                    IPCChannel.Broadcast(new HoverboardMessage()
                    {
                        Action = HoverboardAction.On,
                        Spell = hoverboardbuff.Id
                    });
                }
            }
        }

        private void AutoSitSwitch(string command, string[] param, ChatWindow chatWindow)
        {
            if (param.Length == 0)
            {
                _settings["AutoSit"] = !_settings["AutoSit"].AsBool();
                Chat.WriteLine($"Auto sit : {_settings["AutoSit"].AsBool()}");
            }
        }

        private void PetSitKit()
        {
            Kits kits = new Kits();
            var healpet = DynelManager.LocalPlayer.Pets.Where(x => x.Type == PetType.Heal).FirstOrDefault();
            var kit = Inventory.Items.Where(x => KitItems.Kits.Contains(x.Id)).FirstOrDefault();

            if (healpet == null || kit == null) { return; }

            if (kits.CanUseSitKit() && DynelManager.LocalPlayer.DistanceFrom(healpet.Character) < 10f
                && healpet.Character.IsInLineOfSight)
            {
                if (healpet.Character.NanoPercent <= 75)
                {
                    if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Treatment))
                    {
                        if (DynelManager.LocalPlayer.MovementState != MovementState.Sit)
                        {
                            MovementController.Instance.SetMovement(MovementAction.SwitchToSit);
                        }
                        else
                        {

                            kit.Use(healpet.Character, true);
                        }
                    }
                    else
                    {
                        if (DynelManager.LocalPlayer.MovementState == MovementState.Sit)
                        {
                            MovementController.Instance.SetMovement(MovementAction.LeaveSit);
                        }
                    }
                }
            }
        }

        public static void CancelBuffs(int[] buffsToCancel)
        {
            foreach (Buff buff in DynelManager.LocalPlayer.Buffs)
            {
                if (buffsToCancel.Contains(buff.Id))
                {
                    buff.Remove();
                }
            }
        }

        private static class RelevantNanos
        {
            public static readonly int[] ZixMorph = { 288532, 302212 };

            public static readonly int[] Yalms = {
                82835, 290473, 281569, 301672, 270984, 270991, 273468, 288795, 270993, 270995, 270986, 270982,
                296034, 296669, 304437, 270884, 270941, 270836, 287285, 288816, 270943, 270939, 270945,
                270711, 270731, 270645, 284061, 288802, 270764, 277426, 288799, 270738, 270779, 293619,
                294781, 301669, 301700, 301670, 120499, 
            };

            public static readonly int[] Hoverboards = {
                270634, 270632, 270636, 270327, 277712, 288804, 270643, 270641, 270431, 270540, 270542, 274272,
                288808, 281684, 288814, 270538, 281668, 288812, 270544, 270546, 
            };
        }

        private static class HelpManagerItems
        {
            public static readonly int[] GraftSparrowFlight =
            {
                128345, 128346, 128344, 128343, 94577, 94799, 94049, 93648, 94798, 94048
            };
        }

        public enum Positions
        {
            Center,
            BackWall,
            BackLeft,
            BackRight,
            FrontLeft,
            FrontRight,
            Door,
        }

        public enum POHPositions
        {
            None,
            PortalGuardian1,
            PortalGuardian2,
            PortalGuardian3,
            PortalGuardian4,
        }

        public static int GetLineNumber(Exception ex)
        {
            var lineNumber = 0;

            var lineMatch = Regex.Match(ex.StackTrace ?? "", @":line (\d+)$", RegexOptions.Multiline);

            if (lineMatch.Success)
            {
                lineNumber = int.Parse(lineMatch.Groups[1].Value);
            }

            return lineNumber;
        }
    }
}
