using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;

namespace FarmingGPSLib.Settings.Vaderstad
{
    public class Controller : ConfigurationSection, ISettingsCollection
    {
        SettingsCollection _settings;

        private static ConfigurationProperty _comport =
            new ConfigurationProperty("COMPort", typeof(string), "COM1", ConfigurationPropertyOptions.IsRequired);

        private static ConfigurationProperty _readInterval =
            new ConfigurationProperty("ReadInterval", typeof(int), 1000, ConfigurationPropertyOptions.IsRequired);
        
        public Controller()
        {
            _settings = new SettingsCollection("Controller");
            _settings.Add(new Setting("COMPort", "COMPort", _comport.Type, COMPort));
            _settings.Add(new Setting("ReadInterval", "Läs intervall", _readInterval.Type, ReadInterval));
            foreach (ISetting setting in _settings)
                setting.SettingChanged += Setting_SettingChanged;
        }

        public Controller(Controller controllerSettings) : this()
        {
            COMPort = controllerSettings.COMPort;
            ReadInterval = controllerSettings.ReadInterval;
            _settings["COMPort"].Value = COMPort;
            _settings["ReadInterval"].Value = ReadInterval;
        }

        private void Setting_SettingChanged(object sender, EventArgs e)
        {
            ISetting setting = sender as ISetting;
            if (setting.Name == "COMPort")
                COMPort = (string)setting.Value;
            else if (setting.Name == "ReadInterval")
                ReadInterval = (int)setting.Value;
        }

        [ConfigurationProperty("COMPort", IsRequired = true)]
        public string COMPort
        {
            get { return (string)this[_comport]; }
            set { this[_comport] = value; }
        }

        [ConfigurationProperty("ReadInterval", IsRequired = true)]
        public int ReadInterval
        {
            get { return (int)this[_readInterval]; }
            set { this[_readInterval] = value; }
        }
        
        #region ISettingColletion

        ISetting ISettingsCollection.this[string name]
        {
            get
            {
                return _settings[name];
            }
        }

        public IList<ISettingsCollection> ChildSettings
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int Count
        {
            get
            {
                return _settings.Count;
            }
        }

        public string Name
        {
            get
            {
                return "Väderstad DS400";
            }
        }

        public ISettingsCollection ParentSetting { get; set; }

        public IEnumerator<ISetting> GetEnumerator()
        {
            return _settings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
