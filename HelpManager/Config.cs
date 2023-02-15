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
        public int SitPercentage => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].SitPercentage : 65;

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
        private int _ipcChannel = 2;

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

        public event EventHandler<int> SitPercentageChangedEvent;
        private int _sitPercentage = 65;
        public int SitPercentage
        {
            get
            {
                return _sitPercentage;
            }
            set
            {
                if (_sitPercentage != value)
                {
                    _sitPercentage = value;
                    SitPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
    }
}

