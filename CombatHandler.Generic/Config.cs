using System;
using System.Collections.Generic;
using System.IO;
using AOSharp.Core;
using AOSharp.Core.UI;
using System.Xml;
using Newtonsoft.Json;

namespace CombatHandler.Generic
{
    public class Config
    {
        public Dictionary<int, CharacterSettings> CharSettings { get; set; }

        protected string _path;

        [JsonIgnore]
        public int IPCChannel => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].IPCChannel : 0;
        [JsonIgnore]
        public int DocHealPercentage => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].DocHealPercentage : 90;
        [JsonIgnore]
        public int DocCompleteHealPercentage => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].DocCompleteHealPercentage : 20;
        [JsonIgnore]
        public int TraderHealPercentage => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].TraderHealPercentage : 90;
        [JsonIgnore]
        public int AgentHealPercentage => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].AgentHealPercentage : 90;
        [JsonIgnore]
        public int AgentCompleteHealPercentage => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].AgentCompleteHealPercentage : 20;
        [JsonIgnore]
        public int MAHealPercentage => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].MAHealPercentage : 90;
        [JsonIgnore]
        public int AdvHealPercentage => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].AdvHealPercentage : 90;
        [JsonIgnore]
        public int AdvCompleteHealPercentage => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].AdvCompleteHealPercentage : 20;
        [JsonIgnore]
        public int EnfTauntDelaySingle => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].EnfTauntDelaySingle : 1;
        [JsonIgnore]
        public int EnfTauntDelayArea => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].EnfTauntDelayArea : 1;
        [JsonIgnore]
        public int EnfCycleAbsorbsDelay => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].EnfCycleAbsorbsDelay : 1;
        [JsonIgnore]
        public int EngiBioCocoonPercentage => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].EngiBioCocoonPercentage : 65;

        public static Config Load(string path)
        {
            Config config;

            try
            {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));

                config._path = path;
            }
            catch
            {
                Chat.WriteLine($"No config file found.");
                Chat.WriteLine($"Using default settings");

                config = new Config
                {
                    CharSettings = new Dictionary<int, CharacterSettings>()
                    {
                        { Game.ClientInst, new CharacterSettings() }
                    }
                };

                config._path = path;

                config.Save();
            }

            return config;
        }

        public void Save()
        {
            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Generic"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Generic");

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\Generic"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\Generic");

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\Generic\\{Game.ClientInst}"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\Generic\\{Game.ClientInst}");

            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
        }
    }

    public class CharacterSettings
    {
        public event EventHandler<int> IPCChannelChangedEvent;
        private int _ipcChannel = 0;

        //Breaking out auto-property
        public int IPCChannel {
            get
            {
                return _ipcChannel;
            }
            set
            {
                if (_ipcChannel != value)
                {
                    _ipcChannel = value;
                    IPCChannelChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> TraderHealPercentageChangedEvent;
        private int _traderHealPercentage = 90;
        public int TraderHealPercentage
        {
            get
            {
                return _traderHealPercentage;
            }
            set
            {
                if (_traderHealPercentage != value)
                {
                    _traderHealPercentage = value;
                    TraderHealPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> MAHealPercentageChangedEvent;
        private int _maHealPercentage = 90;
        public int MAHealPercentage
        {
            get
            {
                return _maHealPercentage;
            }
            set
            {
                if (_maHealPercentage != value)
                {
                    _maHealPercentage = value;
                    MAHealPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> DocHealPercentageChangedEvent;
        private int _docHealPercentage = 90;
        public int DocHealPercentage
        {
            get
            {
                return _docHealPercentage;
            }
            set
            {
                if (_docHealPercentage != value)
                {
                    _docHealPercentage = value;
                    DocHealPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> DocCompleteHealPercentageChangedEvent;
        private int _docCompleteHealPercentage = 20;
        public int DocCompleteHealPercentage
        {
            get
            {
                return _docCompleteHealPercentage;
            }
            set
            {
                if (_docCompleteHealPercentage != value)
                {
                    _docCompleteHealPercentage = value;
                    DocCompleteHealPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> AgentHealPercentageChangedEvent;
        private int _agentHealPercentage = 90;
        public int AgentHealPercentage
        {
            get
            {
                return _agentHealPercentage;
            }
            set
            {
                if (_agentHealPercentage != value)
                {
                    _agentHealPercentage = value;
                    AgentHealPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> AgentCompleteHealPercentageChangedEvent;
        private int _agentCompleteHealPercentage = 20;
        public int AgentCompleteHealPercentage
        {
            get
            {
                return _agentCompleteHealPercentage;
            }
            set
            {
                if (_agentCompleteHealPercentage != value)
                {
                    _agentCompleteHealPercentage = value;
                    AgentCompleteHealPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> AdvHealPercentageChangedEvent;
        private int _advHealPercentage = 90;
        public int AdvHealPercentage
        {
            get
            {
                return _advHealPercentage;
            }
            set
            {
                if (_advHealPercentage != value)
                {
                    _advHealPercentage = value;
                    AdvHealPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> AdvCompleteHealPercentageChangedEvent;
        private int _advCompleteHealPercentage = 20;
        public int AdvCompleteHealPercentage
        {
            get
            {
                return _advCompleteHealPercentage;
            }
            set
            {
                if (_advCompleteHealPercentage != value)
                {
                    _advCompleteHealPercentage = value;
                    AdvCompleteHealPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> EnfTauntDelayAreaChangedEvent;
        private int _enfTauntDelayArea = 1;
        public int EnfTauntDelayArea
        {
            get
            {
                return _enfTauntDelayArea;
            }
            set
            {
                if (_enfTauntDelayArea != value)
                {
                    _enfTauntDelayArea = value;
                    EnfTauntDelayAreaChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> EnfTauntDelaySingleChangedEvent;
        private int _enfTauntDelaySingle = 1;
        public int EnfTauntDelaySingle
        {
            get
            {
                return _enfTauntDelaySingle;
            }
            set
            {
                if (_enfTauntDelaySingle != value)
                {
                    _enfTauntDelaySingle = value;
                    EnfTauntDelaySingleChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> EnfCycleAbsorbsDelayChangedEvent;
        private int _enfCycleAbsorbsDelay = 1;
        public int EnfCycleAbsorbsDelay
        {
            get
            {
                return _enfCycleAbsorbsDelay;
            }
            set
            {
                if (_enfCycleAbsorbsDelay != value)
                {
                    _enfCycleAbsorbsDelay = value;
                    EnfCycleAbsorbsDelayChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> EngiBioCocoonPercentageChangedEvent;
        private int _engiBioCocoonPercentage = 65;
        public int EngiBioCocoonPercentage
        {
            get
            {
                return _engiBioCocoonPercentage;
            }
            set
            {
                if (_engiBioCocoonPercentage != value)
                {
                    _engiBioCocoonPercentage = value;
                    EngiBioCocoonPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
    }
}

