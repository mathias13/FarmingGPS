using System;

namespace FarmingGPSLib.Settings
{
    public class Setting : ISetting
    {
        private string _name = String.Empty;

        private Type _valueType = typeof(string);

        private object _value = String.Empty;

        public Setting()
        {
        }

        public Setting(string name, Type valueType, object value)
        {
            _name = name;
            _valueType = valueType;
            _value = value;
        }

        #region ISetting

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public Type ValueType
        {
            get { return _valueType; }
            set { _valueType = value; }
        }

        public object Value
        {
            get { return _value; }
            set
            {
                _value = value;
                if (SettingChanged != null)
                    SettingChanged.Invoke(this, new EventArgs());
            }
        }

        public event EventHandler SettingChanged;

        #endregion
    }
}
