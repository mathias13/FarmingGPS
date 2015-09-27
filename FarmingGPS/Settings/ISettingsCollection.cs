using System;
using System.Collections.Generic;

namespace FarmingGPS.Settings
{
    public interface ISettingsCollection : ICollection<ISetting>
    {
        string Name
        {
            get;
        }

        ISettingsCollection ChildSettings
        {
            get;
            set;
        }

        ISettingsCollection ParentSetting
        {
            get;
            set;
        }
    }
}
