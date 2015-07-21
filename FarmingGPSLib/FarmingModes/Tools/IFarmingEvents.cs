using System;
using System.Collections.Generic;

namespace FarmingGPSLib.FarmingModes.Tools
{
    interface IFarmingEvents
    {
        IList<FarmingEventBase> FarmingEvents
        {
            get;
        }
    }
}
