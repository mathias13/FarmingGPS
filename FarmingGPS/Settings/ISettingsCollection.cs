using System;
using System.Collections.Generic;

namespace FarmingGPS.Settings
{
    public interface ISettingsCollection : IEnumerable<ISetting>
    {
        int Count
        {
            get;
        }

        string Name
        {
            get;
        }

        ISetting this[string name]
        {
            get;
        }

        IList<ISettingsCollection> ChildSettings
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
