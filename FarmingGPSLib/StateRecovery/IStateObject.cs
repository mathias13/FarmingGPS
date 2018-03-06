using System;

namespace FarmingGPSLib.StateRecovery
{
    public interface IStateObject
    {
        void RestoreObject(object restoredState);

        object StateObject
        {
            get;
        }

        Type StateType
        {
            get;
        }        
    }
}
