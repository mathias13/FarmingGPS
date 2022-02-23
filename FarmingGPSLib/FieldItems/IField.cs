using DotSpatial.Positioning;
using DotSpatial.Projections;
using FarmingGPSLib.StateRecovery;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using System.Collections.Generic;

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
