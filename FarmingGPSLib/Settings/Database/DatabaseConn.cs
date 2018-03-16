using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;

namespace FarmingGPSLib.Settings.Database
{
    public class DatabaseConn : ConfigurationSection, ISettingsCollection
    {
        SettingsCollection _settings;

        ISettingsCollection _parent;
        
        private static ConfigurationProperty _encrypt =
            new ConfigurationProperty("Encrypt", typeof(bool), false, ConfigurationPropertyOptions.IsRequired);

        private static ConfigurationProperty _trustServerCertificate =
            new ConfigurationProperty("TrustServerCertificate", typeof(bool), false, ConfigurationPropertyOptions.IsRequired);

        private static ConfigurationProperty _integratedSecurity =
            new ConfigurationProperty("IntegratedSecurity", typeof(bool), false, ConfigurationPropertyOptions.IsRequired);

        private static ConfigurationProperty _userName =
            new ConfigurationProperty("UserName", typeof(string), String.Empty, ConfigurationPropertyOptions.IsRequired);
        
        private static ConfigurationProperty _url =
            new ConfigurationProperty("Url", typeof(string), String.Empty, ConfigurationPropertyOptions.IsRequired);

        private static ConfigurationProperty _databaseName =
            new ConfigurationProperty("DatabaseName", typeof(string), String.Empty, ConfigurationPropertyOptions.IsRequired);

        public DatabaseConn()
        {
            _settings = new SettingsCollection("Databas");
            _settings.Add(new Setting("Kryptering", _encrypt.Type, Encrypt));
            _settings.Add(new Setting("Betrott certifikat", _trustServerCertificate.Type, TrustServerCertificate));
            _settings.Add(new Setting("Windows inloggning", _integratedSecurity.Type, IntegratedSecurity));
            _settings.Add(new Setting("Användarnamn", _userName.Type, UserName));
            _settings.Add(new Setting("URL", _url.Type, Url));
            _settings.Add(new Setting("Databas", _databaseName.Type, DatabaseName));
            foreach(ISetting setting in _settings)
                setting.SettingChanged += Setting_SettingChanged;

        }

        public DatabaseConn(DatabaseConn databaseSettings) : this()
        {
            Encrypt = databaseSettings.Encrypt;
            TrustServerCertificate = databaseSettings.TrustServerCertificate;
            IntegratedSecurity = databaseSettings.IntegratedSecurity;
            UserName = databaseSettings.UserName;
            Url = databaseSettings.Url;
            DatabaseName = databaseSettings.DatabaseName;
            _settings["Kryptering"].Value = Encrypt;
            _settings["Betrott certifikat"].Value = TrustServerCertificate;
            _settings["Windows inloggning"].Value = IntegratedSecurity;
            _settings["Användarnamn"].Value = UserName;
            _settings["URL"].Value = Url;
            _settings["Databas"].Value = DatabaseName;
        }

        private void Setting_SettingChanged(object sender, EventArgs e)
        {
            ISetting setting = sender as ISetting;
            if (setting.Name == "Kryptering")
                Encrypt = (bool)setting.Value;
            else if (setting.Name == "Betrott certifikat")
                TrustServerCertificate = (bool)setting.Value;
            else if (setting.Name == "Windows inloggning")
                IntegratedSecurity = (bool)setting.Value;
            else if (setting.Name == "Användarnamn")
                UserName = (string)setting.Value;
            else if (setting.Name == "URL")
                Url = (string)setting.Value;
            else if (setting.Name == "Databas")
                DatabaseName = (string)setting.Value;
        }
        
        [ConfigurationProperty("Encrypt", IsRequired = true)]
        public bool Encrypt
        {
            get { return (bool)this[_encrypt]; }
            set { this[_encrypt] = value; }
        }

        [ConfigurationProperty("TrustServerCertificate", IsRequired = true)]
        public bool TrustServerCertificate
        {
            get { return (bool)this[_trustServerCertificate]; }
            set { this[_trustServerCertificate] = value; }
        }

        [ConfigurationProperty("IntegratedSecurity", IsRequired = true)]
        public bool IntegratedSecurity
        {
            get { return (bool)this[_integratedSecurity]; }
            set { this[_integratedSecurity] = value; }
        }

        [ConfigurationProperty("UserName", IsRequired = true)]
        public string UserName
        {
            get { return (string)this[_userName]; }
            set { this[_userName] = value; }
        }

        [ConfigurationProperty("Url", IsRequired = true)]
        public string Url
        {
            get { return (string)this[_url]; }
            set { this[_url] = value; }
        }

        [ConfigurationProperty("DatabaseName", IsRequired = true)]
        public string DatabaseName
        {
            get { return (string)this[_databaseName]; }
            set { this[_databaseName] = value; }
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
