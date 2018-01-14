using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace FarmingGPS.Settings
{
    public class SettingGroup : IEnumerable
    {
        private string _name = String.Empty;

        private ReadOnlyCollection<SettingGroup> _items;

        private UserControl _settingControl;

        public SettingGroup(string name, SettingGroup[] childs, UserControl settingControl)
        {
            List<SettingGroup> items = new List<SettingGroup>();
            if (childs != null)
            {
                foreach (SettingGroup setting in childs)
                    items.Add(setting);
            }
            _items = new ReadOnlyCollection<SettingGroup>(items);
            _name = name;
            _settingControl = settingControl;
        }

        public string Name
        {
            get { return _name; }
        }

        public UserControl SettingControl
        {
            get { return _settingControl; }
        }

        public ReadOnlyCollection<SettingGroup> Items
        {
            get { return _items; }
        }

        public IEnumerator GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }
}
