using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using FarmingGPSLib.Settings;

namespace FarmingGPS.Visualization.Settings
{
    class FieldTracker : ConfigurationSection, ISettingsCollection
    {
        SettingsCollection _settings;

        private static ConfigurationProperty _autoStartStop =
            new ConfigurationProperty("AutoStartStop", typeof(bool), false, ConfigurationPropertyOptions.IsRequired);

        public FieldTracker()
        {
            _settings = new SettingsCollection("Fieldtracker");
            _settings.Add(new Setting("Auto start/stop", "Auto start/stopp", typeof(bool), AutoStartStop));
            foreach (ISetting setting in _settings)
                setting.SettingChanged += Setting_SettingChanged;
        }

        private void Setting_SettingChanged(object sender, EventArgs e)
        {
            ISetting setting = sender as ISetting;
            if (setting.Name == "Auto start/stop")
                AutoStartStop = (bool)setting.Value;
        }

        [ConfigurationProperty("AutoStartStop", IsRequired = true)]
        public bool AutoStartStop
        {
            get { return (bool)this[_autoStartStop]; }
            set { this[_autoStartStop] = value; }
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
                return _settings.Name;
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
