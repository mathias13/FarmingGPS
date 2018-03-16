using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;

namespace FarmingGPSLib.Settings.Receiver
{
    public class SBPSerial : ConfigurationSection, ISettingsCollection
    {
        public enum BaudRate
        {
            B9600 = 9600,
            B19200 = 19200,
            B38400 = 38400,
            B57600 = 57600,
            B115200 = 115200
        }

        SettingsCollection _settings;

        ISettingsCollection _parent;
        
        private static ConfigurationProperty _comport =
            new ConfigurationProperty("COMPort", typeof(string), String.Empty, ConfigurationPropertyOptions.IsRequired);

        private static ConfigurationProperty _baudrate =
            new ConfigurationProperty("Baudrate", typeof(BaudRate), BaudRate.B115200, ConfigurationPropertyOptions.IsRequired);

        private static ConfigurationProperty _rtsCts =
            new ConfigurationProperty("RtsCts", typeof(bool), false, ConfigurationPropertyOptions.IsRequired);

        public SBPSerial()
        {
            _settings = new SettingsCollection("SBPSeriell");
            _settings.Add(new Setting("COMPort", _comport.Type, COMPort));
            _settings.Add(new Setting("Baudrate", _baudrate.Type, Baudrate));
            _settings.Add(new Setting("RtsCts", _rtsCts.Type, RtsCts));
            foreach(ISetting setting in _settings)
                setting.SettingChanged += Setting_SettingChanged;
        }

        public SBPSerial(SBPSerial sbpSettings)
        {
            COMPort = sbpSettings.COMPort;
            Baudrate = sbpSettings.Baudrate;
            RtsCts = sbpSettings.RtsCts;
            _settings["COMPort"].Value = COMPort;
            _settings["Baudrate"].Value = Baudrate;
            _settings["RtsCts"].Value = RtsCts;
        }

        private void Setting_SettingChanged(object sender, EventArgs e)
        {
            ISetting setting = sender as ISetting;
            if (setting.Name == "COMPort")
                COMPort = (string)setting.Value;
            else if (setting.Name == "Baudrate")
                Baudrate = (BaudRate)setting.Value;
            else if (setting.Name == "RtsCts")
                RtsCts = (bool)setting.Value;
        }
        
        [ConfigurationProperty("COMPort", IsRequired = true)]
        public string COMPort
        {
            get { return (string)this[_comport]; }
            set { this[_comport] = value; }
        }

        [ConfigurationProperty("Baudrate", IsRequired = true)]
        public BaudRate Baudrate
        {
            get { return (BaudRate)this[_baudrate]; }
            set { this[_baudrate] = value; }
        }

        [ConfigurationProperty("RtsCts", IsRequired = true)]
        public bool RtsCts
        {
            get { return (bool)this[_rtsCts]; }
            set { this[_rtsCts] = value; }
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
