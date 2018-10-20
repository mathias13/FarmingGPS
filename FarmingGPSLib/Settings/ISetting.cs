using System;

namespace FarmingGPSLib.Settings
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
