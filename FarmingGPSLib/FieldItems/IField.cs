using System;
using System.Collections.Generic;
using DotSpatial.Positioning;
using DotSpatial.Topology;
using DotSpatial.Projections;

namespace FarmingGPSLib.FieldItems
{
    public interface IField
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
