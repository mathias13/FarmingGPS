using System;
using System.Collections.Generic;
using DotSpatial.Positioning;
using DotSpatial.Topology;
using DotSpatial.Projections;
using FarmingGPSLib.StateRecovery;

namespace FarmingGPSLib.FieldItems
{
    public interface IField: IStateObject
    {
        bool IsPointInField(Coordinate pointToCheck);

        Coordinate GetPositionInField(Position position);

        Area FieldArea
        {
            get;
        }

        IList<Position> BoundaryPositions
        {
            get;
        }

        Polygon Polygon
        {
            get;
        }

        ProjectionInfo Projection
        {
            get;
        }
    }
}
