using AOSharp.Core;
using AOSharp.Core.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace CombatHandler.Generic
{
    public class Config
    {
        public Dictionary<string, CharacterSettings> CharSettings { get; set; }

        protected string _path;

        #region Json

        [JsonIgnore]
        public int IPCChannel => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].IPCChannel : 0;
        [JsonIgnore]
        public string StimTargetName => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].StimTargetName : string.Empty;
        [JsonIgnore]
        public int StimHealthPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].StimHealthPercentage : 66;
        [JsonIgnore]
        public int StimNanoPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].StimNanoPercentage : 66;
        [JsonIgnore]
        public int KitHealthPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].KitHealthPercentage : 66;
        [JsonIgnore]
        public int KitNanoPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].KitNanoPercentage : 66;
        [JsonIgnore]
        public int HealthDrainPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].HealthDrainPercentage : 90;
       
        [JsonIgnore]
        public int TargetHealPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].TargetHealPercentage : 70;
        
        [JsonIgnore]
        public int CompleteHealPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].CompleteHealPercentage : 35;
        
        [JsonIgnore]
        public int FountainOfLifeHealPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].FountainOfLifeHealPercentage : 30;

        [JsonIgnore]
        public int TeamHealPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].TeamHealPercentage : 90;

        [JsonIgnore]
        public int CompleteTeamHealPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].CompleteTeamHealPercentage : 35;

        [JsonIgnore]
        public int SingleTauntDelay => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].SingleTauntDelay : 1;
        
        [JsonIgnore]
        public int MongoDelay => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].MongoDelay : 1;
        [JsonIgnore]
        public int CycleAbsorbsDelay => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].CycleAbsorbsDelay : 20;
        [JsonIgnore]
        public int CycleChallengerDelay => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].CycleChallengerDelay : 1;
        [JsonIgnore]
        public int CycleRageDelay => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].CycleRageDelay : 1;
        [JsonIgnore]
        public int CycleXpPerksDelay => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].CycleXpPerksDelay : 1;
        [JsonIgnore]
        public int BioCocoonPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].BioCocoonPercentage : 65;
        [JsonIgnore]
        public int NanoAegisPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].NanoAegisPercentage : 70;
        [JsonIgnore]
        public int NullitySpherePercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].NullitySpherePercentage : 35;
        [JsonIgnore]
        public int IzgimmersWealthPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].IzgimmersWealthPercentage : 25;
        [JsonIgnore]
        public int CycleSpherePerkDelay => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].CycleSpherePerkDelay : 1;
        [JsonIgnore]
        public int CycleWitOfTheAtroxPerkDelay => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].CycleWitOfTheAtroxPerkDelay : 1;
        [JsonIgnore]
        public int SelfHealPerkPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].SelfHealPerkPercentage : 20;
        [JsonIgnore]
        public int SelfNanoPerkPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].SelfNanoPerkPercentage : 20;
        [JsonIgnore]
        public int TeamHealPerkPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].TeamHealPerkPercentage : 20;
        [JsonIgnore]
        public int TeamNanoPerkPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].TeamNanoPerkPercentage :20;
        [JsonIgnore]
        public int BattleGroupHeal1Percentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal1Percentage : 60;
        [JsonIgnore]
        public int BattleGroupHeal2Percentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal2Percentage : 50;
        [JsonIgnore]
        public int BattleGroupHeal3Percentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal3Percentage : 40;
        [JsonIgnore]
        public int BattleGroupHeal4Percentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].BattleGroupHeal4Percentage : 30;
        [JsonIgnore]
        public int DuckAbsorbsItemPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].DuckAbsorbsItemPercentage : 40;
        [JsonIgnore]
        public int BodyDevAbsorbsItemPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].BodyDevAbsorbsItemPercentage : 55;
        [JsonIgnore]
        public int StrengthAbsorbsItemPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].StrengthAbsorbsItemPercentage : 70;

        [JsonIgnore]
        public int StaminaAbsorbsItemPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].StaminaAbsorbsItemPercentage : 50;

        [JsonIgnore]
        public int CycleBioRegrowthDelay => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].CycleBioRegrowthPerkDelay : 1;
        [JsonIgnore]
        public int BioRegrowthPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].BioRegrowthPercentage : 70;

        [JsonIgnore]
        public int TOTWPercentage => CharSettings != null && CharSettings.ContainsKey(DynelManager.LocalPlayer.Name) ? CharSettings[DynelManager.LocalPlayer.Name].TOTWPercentage : 35;


        #endregion

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
            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\Generic\\{DynelManager.LocalPlayer.Name}"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{CommonParameters.BasePath}\\{CommonParameters.AppPath}\\Generic\\{DynelManager.LocalPlayer.Name}");

            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
        }
    }

    public class CharacterSettings
    {
        #region Breaking out Auto-Properties

        public event EventHandler<int> IPCChannelChangedEvent;
        private int _ipcChannel = 0;

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
        public event EventHandler<string> StimTargetNameChangedEvent;
        private string _stimTargetName = string.Empty;
        public string StimTargetName
        {
            get
            {
                return _stimTargetName;
            }
            set
            {
                if (_stimTargetName != value)
                {
                    _stimTargetName = value;
                    StimTargetNameChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> StimHealthPercentageChangedEvent;
        private int _stimHealthPercentage = 66;
        public int StimHealthPercentage
        {
            get
            {
                return _stimHealthPercentage;
            }
            set
            {
                if (_stimHealthPercentage != value)
                {
                    _stimHealthPercentage = value;
                    StimHealthPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> StimNanoPercentageChangedEvent;
        private int _stimNanoPercentage = 66;
        public int StimNanoPercentage
        {
            get
            {
                return _stimNanoPercentage;
            }
            set
            {
                if (_stimNanoPercentage != value)
                {
                    _stimNanoPercentage = value;
                    StimNanoPercentageChangedEvent?.Invoke(this, value);
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
        public event EventHandler<int> SelfHealPerkPercentageChangedEvent;
        private int _selfHealPerkPercentage = 20;
        public int SelfHealPerkPercentage
        {
            get
            {
                return _selfHealPerkPercentage;
            }
            set
            {
                if (_selfHealPerkPercentage != value)
                {
                    _selfHealPerkPercentage = value;
                    SelfHealPerkPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> SelfNanoPerkPercentageChangedEvent;
        private int _selfNanoPerkPercentage = 20;
        public int SelfNanoPerkPercentage
        {
            get
            {
                return _selfNanoPerkPercentage;
            }
            set
            {
                if (_selfNanoPerkPercentage != value)
                {
                    _selfNanoPerkPercentage = value;
                    SelfNanoPerkPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> TeamHealPerkPercentageChangedEvent;
        private int _teamHealPerkPercentage = 20;
        public int TeamHealPerkPercentage
        {
            get
            {
                return _teamHealPerkPercentage;
            }
            set
            {
                if (_teamHealPerkPercentage != value)
                {
                    _teamHealPerkPercentage = value;
                    TeamHealPerkPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> TeamNanoPerkPercentageChangedEvent;
        private int _teamNanoPerkPercentage = 20;
        public int TeamNanoPerkPercentage
        {
            get
            {
                return _teamNanoPerkPercentage;
            }
            set
            {
                if (_teamNanoPerkPercentage != value)
                {
                    _teamNanoPerkPercentage = value;
                    TeamNanoPerkPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> HealthDrainPercentageChangedEvent;
        private int _healthDrainPercentage = 90;
        public int HealthDrainPercentage
        {
            get
            {
                return _healthDrainPercentage;
            }
            set
            {
                if (_healthDrainPercentage != value)
                {
                    _healthDrainPercentage = value;
                    HealthDrainPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<int> TargetHealPercentageChangedEvent;

        private int _targetHealPercentage = 70;
        public int TargetHealPercentage
        {
            get
            {
                return _targetHealPercentage;
            }
            set
            {
                if (_targetHealPercentage != value)
                {
                    _targetHealPercentage = value;
                    TargetHealPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<int> CompleteHealPercentageChangedEvent;

        private int _completeHealPercentage = 35;
        public int CompleteHealPercentage
        {
            get
            {
                return _completeHealPercentage;
            }
            set
            {
                if (_completeHealPercentage != value)
                {
                    _completeHealPercentage = value;
                    CompleteHealPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<int> FountainOfLifeHealPercentageChangedEvent;

        private int _fountainOfLifeHealPercentage = 30;
        public int FountainOfLifeHealPercentage
        {
            get
            {
                return _fountainOfLifeHealPercentage;
            }
            set
            {
                if (_fountainOfLifeHealPercentage != value)
                {
                    _fountainOfLifeHealPercentage = value;
                    FountainOfLifeHealPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<int> TeamHealPercentageChangedEvent;

        private int _teamHealPercentage = 90;
        public int TeamHealPercentage
        {
            get
            {
                return _teamHealPercentage;
            }
            set
            {
                if (_teamHealPercentage != value)
                {
                    _teamHealPercentage = value;
                    TeamHealPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<int> CompleteTeamHealPercentageChangedEvent;

        private int _completeTeamHealPercentage = 35;
        public int CompleteTeamHealPercentage
        {
            get
            {
                return _completeTeamHealPercentage;
            }
            set
            {
                if (_completeTeamHealPercentage != value)
                {
                    _completeTeamHealPercentage = value;
                    CompleteTeamHealPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<int> BattleGroupHeal1PercentageChangedEvent;
        private int _battleGroupHeal1Percentage = 60;
        public int BattleGroupHeal1Percentage
        {
            get
            {
                return _battleGroupHeal1Percentage;
            }
            set
            {
                if (_battleGroupHeal1Percentage != value)
                {
                    _battleGroupHeal1Percentage = value;
                    BattleGroupHeal1PercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> BattleGroupHeal2PercentageChangedEvent;
        private int _battleGroupHeal2Percentage = 50;
        public int BattleGroupHeal2Percentage
        {
            get
            {
                return _battleGroupHeal2Percentage;
            }
            set
            {
                if (_battleGroupHeal2Percentage != value)
                {
                    _battleGroupHeal2Percentage = value;
                    BattleGroupHeal2PercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> BattleGroupHeal3PercentageChangedEvent;
        private int _battleGroupHeal3Percentage = 40;
        public int BattleGroupHeal3Percentage
        {
            get
            {
                return _battleGroupHeal3Percentage;
            }
            set
            {
                if (_battleGroupHeal3Percentage != value)
                {
                    _battleGroupHeal3Percentage = value;
                    BattleGroupHeal3PercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> BattleGroupHeal4PercentageChangedEvent;
        private int _battleGroupHeal4Percentage = 30;
        public int BattleGroupHeal4Percentage
        {
            get
            {
                return _battleGroupHeal4Percentage;
            }
            set
            {
                if (_battleGroupHeal4Percentage != value)
                {
                    _battleGroupHeal4Percentage = value;
                    BattleGroupHeal4PercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> DuckAbsorbsItemPercentageChangedEvent;
        private int _duckAbsorbsItemPercentage = 70;
        public int DuckAbsorbsItemPercentage
        {
            get
            {
                return _duckAbsorbsItemPercentage;
            }
            set
            {
                if (_duckAbsorbsItemPercentage != value)
                {
                    _duckAbsorbsItemPercentage = value;
                    DuckAbsorbsItemPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> BodyDevAbsorbsItemPercentageChangedEvent;
        private int _bodyDevAbsorbsItemPercentage = 65;
        public int BodyDevAbsorbsItemPercentage
        {
            get
            {
                return _bodyDevAbsorbsItemPercentage;
            }
            set
            {
                if (_bodyDevAbsorbsItemPercentage != value)
                {
                    _bodyDevAbsorbsItemPercentage = value;
                    BodyDevAbsorbsItemPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> StrengthAbsorbsItemPercentageChangedEvent;
        private int _strengthAbsorbsItemPercentage = 85;
        public int StrengthAbsorbsItemPercentage
        {
            get
            {
                return _strengthAbsorbsItemPercentage;
            }
            set
            {
                if (_strengthAbsorbsItemPercentage != value)
                {
                    _strengthAbsorbsItemPercentage = value;
                    StrengthAbsorbsItemPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> StaminaAbsorbsItemPercentageChangedEvent;
        private int _staminaAbsorbsItemPercentage = 50;
        public int StaminaAbsorbsItemPercentage
        {
            get
            {
                return _staminaAbsorbsItemPercentage;
            }
            set
            {
                if (_staminaAbsorbsItemPercentage != value)
                {
                    _staminaAbsorbsItemPercentage = value;
                    StaminaAbsorbsItemPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> MongoDelayChangedEvent;
        private int _mongoDelay = 1;
        public int MongoDelay
        {
            get
            {
                
                return _mongoDelay;
            }
            set
            {
                if (_mongoDelay != value)
                {
                    _mongoDelay = value;
                    MongoDelayChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> SingleTauntDelayChangedEvent;
        private int _SingleTauntDelay = 1;
        public int SingleTauntDelay
        {
            get
            {
                return _SingleTauntDelay;
            }
            set
            {
                if (_SingleTauntDelay != value)
                {
                    _SingleTauntDelay = value;
                    SingleTauntDelayChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> TimedTauntDelayChangedEvent;
        private int _TimedTauntDelay = 1;
        public int TimedTauntDelay
        {
            get
            {
                return _TimedTauntDelay;
            }
            set
            {
                if (_TimedTauntDelay != value)
                {
                    _TimedTauntDelay = value;
                    TimedTauntDelayChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> CycleAbsorbsDelayChangedEvent;
        private int _cycleAbsorbsDelay = 20;
        public int CycleAbsorbsDelay
        {
            get
            {
                return _cycleAbsorbsDelay;
            }
            set
            {
                if (_cycleAbsorbsDelay != value)
                {
                    _cycleAbsorbsDelay = value;
                    CycleAbsorbsDelayChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> CycleChallengerDelayChangedEvent;
        private int _cycleChallengerDelay = 1;
        public int CycleChallengerDelay
        {
            get
            {
                return _cycleChallengerDelay;
            }
            set
            {
                if (_cycleChallengerDelay != value)
                {
                    _cycleChallengerDelay = value;
                    CycleChallengerDelayChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> CycleRageDelayChangedEvent;
        private int _cycleRageDelay = 1;
        public int CycleRageDelay
        {
            get
            {
                return _cycleRageDelay;
            }
            set
            {
                if (_cycleRageDelay != value)
                {
                    _cycleRageDelay = value;
                    CycleRageDelayChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> CycleXpPerksDelayChangedEvent;
        private int _cycleXpPerksDelay = 1;
        public int CycleXpPerksDelay
        {
            get
            {
                return _cycleXpPerksDelay;
            }
            set
            {
                if (_cycleXpPerksDelay != value)
                {
                    _cycleXpPerksDelay = value;
                    CycleXpPerksDelayChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> BioCocoonPercentageChangedEvent;
        private int _bioCocoonPercentage = 65;
        public int BioCocoonPercentage
        {
            get
            {
                return _bioCocoonPercentage;
            }
            set
            {
                if (_bioCocoonPercentage != value)
                {
                    _bioCocoonPercentage = value;
                    BioCocoonPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> NanoAegisPercentageChangedEvent;
        private int _nanoAegisPercentage = 70;
        public int NanoAegisPercentage
        {
            get
            {
                return _nanoAegisPercentage;
            }
            set
            {
                if (_nanoAegisPercentage != value)
                {
                    _nanoAegisPercentage = value;
                    NanoAegisPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> NullitySpherePercentageChangedEvent;
        private int _nullitySpherePercentage = 35;
        public int NullitySpherePercentage
        {
            get
            {
                return _nullitySpherePercentage;
            }
            set
            {
                if (_nullitySpherePercentage != value)
                {
                    _nullitySpherePercentage = value;
                    NullitySpherePercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> IzgimmersWealthPercentageChangedEvent;
        private int _izgimmersWealthPercentage = 35;
        public int IzgimmersWealthPercentage
        {
            get
            {
                return _izgimmersWealthPercentage;
            }
            set
            {
                if (_izgimmersWealthPercentage != value)
                {
                    _izgimmersWealthPercentage = value;
                    IzgimmersWealthPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> CycleSpherePerkDelayChangedEvent;
        private int _cycleSpherePerkDelay = 1;
        public int CycleSpherePerkDelay
        {
            get
            {
                return _cycleSpherePerkDelay;
            }
            set
            {
                if (_cycleSpherePerkDelay != value)
                {
                    _cycleSpherePerkDelay = value;
                    CycleSpherePerkDelayChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> CycleWitOfTheAtroxPerkDelayChangedEvent;
        private int _cycleWitOfTheAtroxPerkDelay = 1;
        public int CycleWitOfTheAtroxPerkDelay
        {
            get
            {
                return _cycleWitOfTheAtroxPerkDelay;
            }
            set
            {
                if (_cycleWitOfTheAtroxPerkDelay != value)
                {
                    _cycleWitOfTheAtroxPerkDelay = value;
                    CycleWitOfTheAtroxPerkDelayChangedEvent?.Invoke(this, value);
                }
            }
        }


        public event EventHandler<int> CycleBioRegrowthPerkDelayChangedEvent;
        private int _cycleBioRegrowthPerkDelay = 1;
        public int CycleBioRegrowthPerkDelay
        {
            get
            {
                return _cycleBioRegrowthPerkDelay;
            }
            set
            {
                if (_cycleBioRegrowthPerkDelay != value)
                {
                    _cycleBioRegrowthPerkDelay = value;
                    CycleBioRegrowthPerkDelayChangedEvent?.Invoke(this, value);
                }
            }
        }
        public event EventHandler<int> BioRegrowthPercentageChangedEvent;
        private int _bioRegrowthPercentage = 70;
        public int BioRegrowthPercentage
        {
            get
            {
                return _bioRegrowthPercentage;
            }
            set
            {
                if (_bioRegrowthPercentage != value)
                {
                    _bioRegrowthPercentage = value;
                    BioRegrowthPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<int> TOTWPercentageChangedEvent;
        private int _tOTWPercentage = 35;
        public int TOTWPercentage
        {
            get
            {
                return _tOTWPercentage;
            }
            set
            {
                if (_tOTWPercentage != value)
                {
                    _tOTWPercentage = value;
                    TOTWPercentageChangedEvent?.Invoke(this, value);
                }
            }
        }
        #endregion
    }
}

