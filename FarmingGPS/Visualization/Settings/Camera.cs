using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using FarmingGPSLib.Settings;

namespace FarmingGPS.Visualization.Settings
{
    public enum CameraTypes
    {
        Axis,
        GarminVirb
    }

    public class Camera : ConfigurationSection, ISettingsCollection
    {
        SettingsCollection _settings;

        private static ConfigurationProperty _address =
            new ConfigurationProperty("Address", typeof(string), String.Empty, ConfigurationPropertyOptions.IsRequired);

        private static ConfigurationProperty _cameraType =
            new ConfigurationProperty("CameraType", typeof(CameraTypes), CameraTypes.Axis, ConfigurationPropertyOptions.IsRequired);

        public Camera()
        {
            _settings = new SettingsCollection("Camera");
            _settings.Add(new Setting("CameraType", "CameraType", typeof(CameraTypes), CameraType));
            _settings.Add(new Setting("Address", "Address", typeof(string), Address));
            foreach (ISetting setting in _settings)
                setting.SettingChanged += Setting_SettingChanged;
        }

        public Camera(Camera cameraSettings) : this()
        {
            Address = cameraSettings.Address;
            CameraType = cameraSettings.CameraType;
            _settings["Address"].Value = Address;
            _settings["CameraType"].Value = CameraType;
        }

        private void Setting_SettingChanged(object sender, EventArgs e)
        {
            ISetting setting = sender as ISetting;
            if (setting.Name == "Address")
                Address = (string)setting.Value;
            if (setting.Name == "CameraType")
                CameraType = (CameraTypes)setting.Value;
        }

        [ConfigurationProperty("Address", IsRequired = true)]
        public string Address
        {
            get { return (string)this[_address]; }
            set { this[_address] = value; }
        }

        [ConfigurationProperty("CameraType", IsRequired = true)]
        public CameraTypes CameraType
        {
            get { return (CameraTypes)this[_cameraType]; }
            set { this[_cameraType] = value; }
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
