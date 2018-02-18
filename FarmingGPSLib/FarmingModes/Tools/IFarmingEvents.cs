using System;
using System.Collections.Generic;

namespace FarmingGPSLib.FarmingModes.Tools
{
    public interface IFarmingEvents
    {
        IList<IFarmingEvent> FarmingEvents
        {
            get;
        }
    }
}
