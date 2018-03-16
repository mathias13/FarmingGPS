using System;

namespace FarmingGPSLib.Settings
{
    public interface ISettingsChanged
    {
        event EventHandler<string> SettingChanged;
    }
}
