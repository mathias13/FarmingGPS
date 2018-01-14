using System;

namespace FarmingGPS.Settings
{
    public interface ISetting
    {
        string Name
        {
            get;
        }

        Type ValueType
        {
            get;
        }

        object Value
        {
            get;
            set;
        }

        event EventHandler SettingChanged;
    }
}
