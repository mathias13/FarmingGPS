using System;

namespace FarmingGPS.StateRecovery
{
    public interface IStateObject
    {
        void RestoreObjects(object[] objects);

        object[] StateObjects
        {
            get;
        }
    }
}
