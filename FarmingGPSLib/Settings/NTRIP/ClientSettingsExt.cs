using System;
using NTRIP.Settings;
using System.Collections;
using System.Collections.Generic;

namespace FarmingGPSLib.Settings.NTRIP
{
    public class ClientSettingsExt : ClientSettings, ISettingsCollection
    {
        SettingsCollection _settings;

        ISettingsCollection _parent;

        public ClientSettingsExt() : base()
        {
            _settings = new SettingsCollection("NTRIP");
            _settings.Add(new Setting("Url", IPorHost.GetType(), IPorHost));
            _settings.Add(new Setting("Port nummer", PortNumber.GetType(), PortNumber));
            _settings.Add(new Setting("Användarnamn", NTRIPUser.UserName.GetType(), NTRIPUser.UserName));
            _settings.Add(new Setting("Lösenord", NTRIPUser.UserPassword.GetType(), NTRIPUser.UserPassword));
            _settings.Add(new Setting("Antenn", NTRIPMountPoint.GetType(), NTRIPMountPoint));
            foreach (ISetting setting in _settings)
                setting.SettingChanged += Setting_SettingChanged;
        }
                    
        public ClientSettingsExt(ClientSettings clientSettings) : this()
        {
            IPorHost = clientSettings.IPorHost;
            PortNumber = clientSettings.PortNumber;
            NTRIPUser = clientSettings.NTRIPUser;
            NTRIPMountPoint = clientSettings.NTRIPMountPoint;
            _settings["Url"].Value = IPorHost;
            _settings["Port nummer"].Value = PortNumber;
            _settings["Användarnamn"].Value = NTRIPUser.UserName;
            _settings["Lösenord"].Value = NTRIPUser.UserPassword;
            _settings["Antenn"].Value = NTRIPMountPoint;
        }

        private void Setting_SettingChanged(object sender, EventArgs e)
        {
            ISetting setting = sender as ISetting;
            if (setting.Name == "Url")
                IPorHost = (string)setting.Value;
            else if (setting.Name == "Port nummer")
                PortNumber = (int)setting.Value;
            else if (setting.Name == "Användarnamn")
                NTRIPUser.UserName = (string)setting.Value;
            else if (setting.Name == "Lösenord")
                NTRIPUser.UserPassword = (string)setting.Value;
            else if (setting.Name == "Antenn")
                NTRIPMountPoint = (string)setting.Value;
        }

        ISetting ISettingsCollection.this[string name]
        {
            get { return _settings[name]; }
        }

        public IList<ISettingsCollection> ChildSettings
        {
            get { return null; }
            set
            {
                throw new NotSupportedException("Can't set Childsettings");
            }
        }

        public int Count
        {
            get { return _settings.Count; }
        }

        public string Name
        {
            get { return _settings.Name; }
        }

        public ISettingsCollection ParentSetting
        {
            get { return _parent; }

            set { _parent = value; }
        }
        
        public IEnumerator<ISetting> GetEnumerator()
        {
            return _settings.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _settings.GetEnumerator();
        }
    }
}
