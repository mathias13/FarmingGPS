using System;

namespace FarmingGPS.Settings
{
    interface ISettingsChanged
    {
        event EventHandler<string> SettingChanged;
    }
}
