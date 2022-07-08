using System;
using System.Collections.Generic;
using System.IO;
using AOSharp.Core;
using AOSharp.Core.UI;
using System.Xml;
using Newtonsoft.Json;

namespace HelpManager
{
    public class Config
    {
        public Dictionary<int, CharacterSettings> CharSettings { get; set; }

        protected string _path;

        [JsonIgnore]
        public int IPCChannel => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].IPCChannel : 0;
        [JsonIgnore]
        public string AssistPlayer => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].AssistPlayer : string.Empty;
        [JsonIgnore]
        public string FollowPlayer => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].FollowPlayer : string.Empty;
        [JsonIgnore]
        public string NavFollowPlayer => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].NavFollowIdentity : string.Empty;
        [JsonIgnore]
        public int NavFollowDistance => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].NavFollowDistance : 15;

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
            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp");

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\HelpManager"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\HelpManager");

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\HelpManager\\{Game.ClientInst}"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\HelpManager\\{Game.ClientInst}");

            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
        }
    }

    public class CharacterSettings
    {
        public event EventHandler<int> IPCChannelChangedEvent;
        private int _ipcChannel = 0;

        //Breaking out auto-property
        public int IPCChannel
        {
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
        public event EventHandler<string> AssistPlayerChangedEvent;
        private string _assistPlayer = string.Empty;

        //Breaking out auto-property
        public string AssistPlayer
        {
            get
            {
                return _assistPlayer;
            }
            set
            {
                if (_assistPlayer != value)
                {
                    _assistPlayer = value;
                    AssistPlayerChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<string> FollowPlayerChangedEvent;
        private string _followPlayer = string.Empty;

        //Breaking out auto-property
        public string FollowPlayer
        {
            get
            {
                return _followPlayer;
            }
            set
            {
                if (_followPlayer != value)
                {
                    _followPlayer = value;
                    FollowPlayerChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<string> NavFollowIdentityChangedEvent;
        private string _navFollowPlayer = string.Empty;

        //Breaking out auto-property
        public string NavFollowIdentity
        {
            get
            {
                return _navFollowPlayer;
            }
            set
            {
                if (_navFollowPlayer != value)
                {
                    _navFollowPlayer = value;
                    NavFollowIdentityChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> NavFollowDistanceChangedEvent;
        private int _navFollowDistance = 0;

        //Breaking out auto-property
        public int NavFollowDistance
        {
            get
            {
                return _navFollowDistance;
            }
            set
            {
                if (_navFollowDistance != value)
                {
                    _navFollowDistance = value;
                    NavFollowDistanceChangedEvent?.Invoke(this, value);
                }
            }
        }
    }
}

