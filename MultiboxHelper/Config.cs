using System;
using System.Collections.Generic;
using System.IO;
using AOSharp.Core;
using AOSharp.Core.UI;
using System.Xml;
using Newtonsoft.Json;

namespace MultiboxHelper
{
    public class Config
    {
        public Dictionary<int, CharacterSettings> CharSettings { get; set; }

        protected string _path;

        [JsonIgnore]
        public int IPCChannel => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].IPCChannel : 0;
        [JsonIgnore]
        public string AssistPlayer => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].AssistPlayer : String.Empty;
        [JsonIgnore]
        public string FollowPlayer => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].FollowPlayer : String.Empty;
        [JsonIgnore]
        public string NavFollowPlayer => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].NavFollowPlayer : String.Empty;
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

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\MultiboxHelper"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\MultiboxHelper");

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\MultiboxHelper\\{Game.ClientInst}"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\MultiboxHelper\\{Game.ClientInst}");

            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
        }
    }

    public class CharacterSettings
    {
        public int IPCChannel { get; set; } = 0;
        public string AssistPlayer { get; set; } = String.Empty;
        public string FollowPlayer { get; set; } = String.Empty;
        public string NavFollowPlayer { get; set; } = String.Empty;
        public int NavFollowDistance { get; set; } = 15;
    }
}

