using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using FarmingGPSLib.Settings;

namespace FarmingGPS.Visualization.Settings
{
    public class LightBar : ConfigurationSection, ISettingsCollection
    {
        SettingsCollection _settings;

        ISettingsCollection _parent;

        private static ConfigurationProperty _tolerance =
            new ConfigurationProperty("Tolerance", typeof(int), 20, ConfigurationPropertyOptions.IsRequired);
                
        public LightBar()
        {
            _settings = new SettingsCollection("Lightbar");
            _settings.Add(new Setting("Tolerance", "Tolerans", typeof(int), Tolerance));
            foreach (ISetting setting in _settings)
                setting.SettingChanged += Setting_SettingChanged;
        }

        private void Setting_SettingChanged(object sender, EventArgs e)
        {
            ISetting setting = sender as ISetting;
            if (setting.Name == "Tolerance")
                Tolerance = (int)setting.Value;
        }
        
        [ConfigurationProperty("Tolerance", IsRequired = true)]
        public int Tolerance
        {
            get { return (int)this[_tolerance]; }
            set { this[_tolerance] = value; }
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

        public ISettingsCollection ParentSetting
        {
            get
            {
                return _parent;
            }

            set
            {
                _parent = value;
            }
        }

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
