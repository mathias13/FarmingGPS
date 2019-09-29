using System;

namespace FarmingGPSLib.Settings
{
    public interface ISetting
    {
        string Name
        {
            get;
        }

        string DisplayName
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
