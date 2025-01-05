using AOSharp.Core;
using AOSharp.Core.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace AssistManager
{
    public class Config
    {
        public Dictionary<string, CharacterSettings> CharSettings { get; set; }

        protected string _path;

        [JsonIgnore]
        public string AssistPlayer => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].AssistPlayer : string.Empty;
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
            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\AssistManager\\{DynelManager.LocalPlayer.Name}"))
            {
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\AssistManager\\{DynelManager.LocalPlayer.Name}");
            } 

            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
        }
    }

    public class CharacterSettings
    {
        public event EventHandler<string> AssistPlayerChangedEvent;
        private string _assistPlayer = string.Empty;

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
    }
}

