using System;
using System.Collections;
using System.Collections.Generic;

namespace FarmingGPSLib.Settings
{
    public class SettingsCollection : ISettingsCollection
    {
        protected List<ISetting> _settings = new List<ISetting>();

        protected IList<ISettingsCollection> _childSettings = null;

        protected ISettingsCollection _parentSetting = null;

        public SettingsCollection(string name)
        {
            Name = name;
            _childSettings = new List<ISettingsCollection>();
        }
        
        public ISetting this[string name]
        {
            get
            {
                foreach (ISetting setting in _settings)
                    if (setting.Name.ToLower() == name.ToLower())
                        return setting;
                return null;
            }
        }

        public IList<ISettingsCollection> ChildSettings
        {
            get
            {
                return _childSettings;
            }

            set
            {
                _childSettings = value;
            }
        }

        public int Count
        {
            get
            {
                return _settings.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public string Name { get; } = String.Empty;

        public ISettingsCollection ParentSetting
        {
            get
            {
                return _parentSetting;
            }

            set
            {
                _parentSetting = value;
            }
        }

        public void Add(ISetting item)
        {
            _settings.Add(item);
        }

        public void Clear()
        {
            _settings.Clear();
        }

        public bool Contains(ISetting item)
        {
            return _settings.Contains(item);
        }

        public void CopyTo(ISetting[] array, int arrayIndex)
        {
            _settings.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ISetting> GetEnumerator()
        {
            return _settings.GetEnumerator();
        }

        public bool Remove(ISetting item)
        {
            return _settings.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _settings.GetEnumerator();
        }
    }
}
