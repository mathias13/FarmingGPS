using System;
using DotSpatial.Positioning;
using GpsUtilities.Reciever;
using FarmingGPSLib.StateRecovery;

namespace FarmingGPSLib.Vechile
{
    public interface IVechile: IStateObject
    {
        Azimuth OffsetDirection { get; }

        Distance OffsetDistance { get; }

        IReceiver Receiver { get; set; }
    }
}
