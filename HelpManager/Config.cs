using AOSharp.Core;
using AOSharp.Core.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace HelpManager
{
    public class Config
    {
        public Dictionary<string, CharacterSettings> CharSettings { get; set; }

        protected string _path;

        [JsonIgnore]
        public int IPCChannel => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].IPCChannel : 2;
        [JsonIgnore]
        public int KitHealthPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage : 66;
        [JsonIgnore]
        public int KitNanoPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage : 66;
 
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
                    CharSettings = new Dictionary<string, CharacterSettings>()
                    {
                        { DynelManager.LocalPlayer.Name, new CharacterSettings() }
                    }
                };

                config._path = path;

                config.Save();
            }

            return config;
        }

        public void Save()
        {
            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\HelpManager\\{DynelManager.LocalPlayer.Name}"))
            {
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\HelpManager\\{DynelManager.LocalPlayer.Name}");
            }

            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
        }
    }

    public class CharacterSettings
    {
        public event EventHandler<int> IPCChannelChangedEvent;

        private int _ipcChannel = 2;

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

        public event EventHandler<int> KitHealthPercentageChangedEvent;

        private int _kitHealthPercentage = 66;

        public int KitHealthPercentage
        {
            get
            {
                return _kitHealthPercentage;
            }
            set
            {
                if (_kitHealthPercentage != value)
                {
                    _kitHealthPercentage = value;
                    KitHealthPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> KitNanoPercentageChangedEvent;

        private int _kitNanoPercentage = 66;

        public int KitNanoPercentage
        {
            get
            {
                return _kitNanoPercentage;
            }
            set
            {
                if (_kitNanoPercentage != value)
                {
                    _kitNanoPercentage = value;
                    KitNanoPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
    }
}

