using System;

namespace FarmingGPS.Settings
{
    public interface ISetting
    {
        string Name
        {
            get;
        }

        public Type ValueType
        {
            get;
        }

        public object Value
        {
            get;
        }
    }
}
