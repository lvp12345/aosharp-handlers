using AOSharp.Core;
using AOSharp.Core.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace GMIManager
{
    public class Config
    {
        public Dictionary<int, CharacterSettings> CharSettings { get; set; }

        protected string _path;

        [JsonIgnore]
        public int GMIWithdrawAmount => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].GMIWithdrawAmount : 0;
        [JsonIgnore]
        public string GMIBuyOrderName => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].GMIBuyOrderName : string.Empty;
        [JsonIgnore]
        public long GMIBuyOrderEndPrice => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].GMIBuyOrderEndPrice : 0;
        [JsonIgnore]
        public string GMIItemName => CharSettings != null && CharSettings.ContainsKey(Game.ClientInst) ? CharSettings[Game.ClientInst].GMIItemName : string.Empty;

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

            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\AOSP\\GMIManager\\{Game.ClientInst}"))
            {
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\AOSP\\GMIManager\\{Game.ClientInst}");
            }

            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
        }
    }

    public class CharacterSettings
    {
        public event EventHandler<string> GMIBuyOrderNameChangedEventChangedEvent;
        private string _gMIBuyOrderName = string.Empty;

        public string GMIBuyOrderName
        {
            get
            {
                return _gMIBuyOrderName;
            }
            set
            {
                if (_gMIBuyOrderName != value)
                {
                    _gMIBuyOrderName = value;
                    GMIBuyOrderNameChangedEventChangedEvent?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<int> GMIWithdrawAmountChangedEvent;
        private int _gMIWithdrawAmount = 0;

        public int GMIWithdrawAmount
        {
            get
            {
                return _gMIWithdrawAmount;
            }
            set
            {
                if (_gMIWithdrawAmount != value)
                {
                    _gMIWithdrawAmount = value;
                    GMIWithdrawAmountChangedEvent?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<long> GMIBuyOrderEndPriceChangedEvent;
        private long _gMIBuyOrderEndPrice = 0;

        public long GMIBuyOrderEndPrice
        {
            get
            {
                return _gMIBuyOrderEndPrice;
            }
            set
            {
                if (_gMIBuyOrderEndPrice != value)
                {
                    _gMIBuyOrderEndPrice = value;
                    GMIBuyOrderEndPriceChangedEvent?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<string> GMIItemNameChangedEventChangedEvent;
        private string _gMIItemName = string.Empty;

        public string GMIItemName
        {
            get
            {
                return _gMIItemName;
            }
            set
            {
                if (_gMIItemName != value)
                {
                    _gMIItemName = value;
                    GMIItemNameChangedEventChangedEvent?.Invoke(this, value);
                }
            }
        }
    }
}

