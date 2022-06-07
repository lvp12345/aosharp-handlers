using System;
using System.Collections.Generic;
using System.IO;
using AOSharp.Core;
using AOSharp.Core.UI;
using System.Xml;
using Newtonsoft.Json;

namespace CombatHandler
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
        public int IPCChannel { get; set; } = 0;
        public int DocHealPercentage { get; set; } = 90;
        public int DocCompleteHealPercentage { get; set; } = 20;
    }
}

