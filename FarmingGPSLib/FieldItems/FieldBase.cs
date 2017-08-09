using System;
using System.Collections.Generic;
using DotSpatial.Projections;
using DotSpatial.Positioning;
using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;

namespace FarmingGPSLib.FieldItems
{
    public class FieldBase :IField
    {
        #region Private Variables

        protected IList<Position> _positions;

        protected Polygon _polygon;

        protected ProjectionInfo _proj;

        protected object _syncObject = new object();

        #endregion

        #region Constructors

        public FieldBase()
        {
        }

        public FieldBase(IList<Position> positions, ProjectionInfo proj)
        {
            _positions = positions;
            _proj = proj;
            ReloadPolygon();
        }

        #endregion

        #region Public Methods

        #endregion

        #region Private Methods

        protected void ReloadPolygon()
        {
            lock (_syncObject)
            {
                if (_positions.Count > 2)
                {
                    IList<Coordinate> coords = ReprojectBoundary();
                    LineString lineString = new LineString(coords);
                    //Make sure we have a left hand orientation to make correct offsetcurves
                    if (!CgAlgorithms.IsCounterClockwise(coords))
                        lineString.Reverse();
                    if (!lineString.IsRing)
                        throw new InvalidOperationException("Field is selfintersecting or is not closed");
                    _polygon = new Polygon(new LinearRing(lineString));
                }
                else
                    _polygon = null;
            }
        }

        protected IList<Coordinate> ReprojectBoundary()
        {
            IList<Coordinate> points = new List<Coordinate>();
            List<double> xy = new List<double>();
            List<double> z = new List<double>();
            double[] xyArray;
            double[] zArray;
            lock (_syncObject)
            {
                foreach (Position position in _positions)
                {
                    xy.Add(position.Longitude.DecimalDegrees);
                    xy.Add(position.Latitude.DecimalDegrees);
                    z.Add(Distance.EarthsAverageRadius.ToMeters().Value);
                }
            }

            xyArray = xy.ToArray();
            zArray = z.ToArray();
            Reproject.ReprojectPoints(xyArray, zArray, KnownCoordinateSystems.Geographic.World.WGS1984, _proj, 0, zArray.Length);
            for (int i = 0; i < zArray.Length; i++)
                points.Add(new Coordinate(xyArray[i * 2] * _proj.Unit.Meters, xyArray[i * 2 + 1] * _proj.Unit.Meters));
            return points;
        }

        #endregion

        #region Properties

        #endregion

        #region IField Implementation

        public bool IsPointInField(Coordinate pointToCheck)
        {
            lock (_syncObject)
            {
                if (_polygon == null)
                    return false;
                else
                    return SimplePointInAreaLocator.ContainsPointInPolygon(pointToCheck, _polygon);
            }
        }

        public Coordinate GetPositionInField(Position position)
        {
            double[] xyArray = new double[2] { position.Longitude.DecimalDegrees, position.Latitude.DecimalDegrees };
            double[] zArray = new double[1] { Distance.EarthsAverageRadius.ToMeters().Value };
            Reproject.ReprojectPoints(xyArray, zArray, KnownCoordinateSystems.Geographic.World.WGS1984, _proj, 0, zArray.Length);
            return new Coordinate(xyArray[0], xyArray[1]);
        }

        public Area FieldArea
        {
            get
            {
                lock (_syncObject)
                {
                    if (_polygon == null)
                        return Area.Empty;
                    else
                        return new Area(_polygon.Area, AreaUnit.SquareMeters);
                }
            }
        }

        public IList<Position> BoundaryPositions
        {
            get
            {
                List<Position> positions = new List<Position>();
                lock (_syncObject)
                    foreach (Position position in _positions)
                        positions.Add(position.Clone());
                return positions;

            }
        }

        public Polygon Polygon
        {
            get
            {
                lock (_syncObject)
                {
                    if (_polygon == null)
                        return null;
                    else
                        return new Polygon(_polygon.Shell.Clone() as ILinearRing, _polygon.Holes.Clone() as ILinearRing[]);
                }
            }
        }

        public ProjectionInfo Projection
        {
            get { return _proj; }
        }

        #endregion
    }
}
