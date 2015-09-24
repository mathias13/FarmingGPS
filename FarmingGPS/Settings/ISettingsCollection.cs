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
    }
}
