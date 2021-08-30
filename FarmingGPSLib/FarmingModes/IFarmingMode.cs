using System;
using System.Collections.Generic;
using DotSpatial.Topology;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.StateRecovery;

namespace FarmingGPSLib.FarmingModes
{
    public interface IFarmingMode : IStateObject
    {
        void CreateTrackingLines(Coordinate aCoord, Angle direction);

        void CreateTrackingLines(Coordinate aCoord, Coordinate bCoord);

        void CreateTrackingLines(TrackingLine trackingLine);

        void CreateTrackingLines(TrackingLine trackingLine, Angle directionFromLine);

        void CreateTrackingLines(TrackingLine trackingLine, Angle directionFromLine, double offset);

        void UpdateEvents(ILineString positionEquipment, DotSpatial.Positioning.Azimuth direction);

        IList<TrackingLine> TrackingLinesHeadland
        {
            get;
        }

        IList<TrackingLine> TrackingLines
        {
            get;
        }

        bool EquipmentSideOutRight
        {
            get;
            set;
        }

        TrackingLine GetClosestLine(Coordinate position, DotSpatial.Positioning.Azimuth direction);

        event EventHandler<string> FarmingEvent;
    }
}
