using System;
using System.Collections.Generic;
using DotSpatial.Projections;
using DotSpatial.Projections.Transforms;
using DotSpatial.Positioning;
using DotSpatial.Topology;

namespace FarmingGPSLib.Positioning
{
    public class Boundary
    {
        #region Private Variables

        Polygon _polygon;

        Position _southWest = Position.Maximum;

        Position _east1m;

        Position _north1m;

        Distance _averageHeight;

        #endregion

        public Boundary(IList<Position> positions, Distance averageHeight)
        {
            List<double> xy = new List<double>();
            List<double> z = new List<double>();
            double[] xyArray;
            double[] zArray;
            
            foreach (Position position in positions)
            {
                xy.Add(position.Longitude.DecimalDegrees);
                xy.Add(position.Latitude.DecimalDegrees);
                z.Add(averageHeight.ToMeters().Value);
                if (position.Latitude < _southWest.Latitude)
                    _southWest = new Position(position.Latitude, _southWest.Longitude);
                if (position.Longitude < _southWest.Longitude)
                    _southWest = new Position(_southWest.Latitude, position.Longitude);
            }
            xyArray = xy.ToArray();
            zArray = z.ToArray();
            if (KnownCoordinateSystems.Geographic.World.WGS1984.IsGeocentric)
                throw new Exception("");
            Reproject.ReprojectPoints(xyArray, zArray, KnownCoordinateSystems.Geographic.World.WGS1984, KnownCoordinateSystems.Projected.NationalGridsSweden.SWEREF991330, 0, z.Count);
            List<Coordinate> coords = new List<Coordinate>();
            for (int i = 0; i < zArray.Length; i++)
                coords.Add(new Coordinate(xyArray[i * 2], xyArray[i * 2 + 1]));
            LinearRing ring = new LinearRing(coords);
            Polygon polygon = new Polygon(ring);
            Area area = new Area(polygon.Area, AreaUnit.SquareMeters);

        }

        #region Public Methods

        #endregion

        #region Public Properties

        public Area Area
        {
            get
            {
                return new Area(_polygon.Area, AreaUnit.SquareMeters);
            }
        }

        #endregion

        #region Private Methods

        #endregion

        /*
        #region Private Variables

        IList<Coordinate> _points = new List<Coordinate>();

        IList<TriangleExt> _triangles = new List<TriangleExt>();

        Extent _extent;

        #endregion

        public Boundary(IList<Coordinate> points)
        {
            _points = points;

            List<LineSegment> lines = new List<LineSegment>();
            for (int i = 0; i < _points.Count - 1; i++)
            {
                LineSegment newLine = new LineSegment(_points[i], _points[i + 1]);
                foreach (LineSegment line in lines)
                {
                    Coordinate intersection = line.Intersection(newLine);
                    if (intersection != null)
                        if (!((!intersection.Equals(newLine.P0)) ^ (!intersection.Equals(newLine.P1))))
                            throw new InvalidOperationException("Boundary is selfintersecting");
                }
                lines.Add(newLine);
            }

            //Check for right or lefthand direction
            double angleSum = 0.0;

            for (int i = 0; i < lines.Count - 1; i++)
                angleSum += HelperClass.GetAngleBetweenLines(lines[i], lines[i + 1]).Value;

            //Make sure we have left hand direction
            if (angleSum < 0)
            {
                IList<Coordinate> newPoints = new List<Coordinate>();
                for (int i = _points.Count - 1; i >= 0; i++)
                    newPoints.Add(_points[i]);
                _points = newPoints;
            }

            Coordinate p0 = null;
            Coordinate p1 = null;
            Coordinate p2 = null;

            for (int i = 0; i < _points.Count; i++)
            {
                if (p0 == null)
                {
                    p0 = _points[i];
                    continue;
                }
                else if (p1 == null)
                {
                    p1 = _points[i];
                    continue;
                }
                else if (p2 == null)
                    p2 = _points[i];

                if (HelperClass.GetAngleBetweenLines(p0,p1,p1,p2).Value > 0)
                {
                    _triangles.Add(new TriangleExt(p0, p1, p2));
                    p0 = null;
                    p1 = null;
                    p2 = null;
                }
                else
                {
                    for (int j = _triangles.Count - 1; j != 0; j--)
                    {
                        p2 = _triangles[j].P0;
                        TriangleExt newTriangle = new TriangleExt(p0, p1, p2);
                        foreach (TriangleExt triangle in _triangles)
                            if (triangle.TriangleLinesInterset(newTriangle))
                                continue;
                        _triangles.Add(newTriangle);
                    }
                    p0 = null;
                    p1 = null;
                    p2 = null;
                    i -= 2;
                }
            }
        }

        public Boundary(IList<Position> points)
        {
            if (points.Count < 3)
                throw new InvalidOperationException("Boundary needs at least 3 points to be valid");
            else if (points[0] == points[points.Count - 1])
                throw new InvalidOperationException("Boundary doesn't form a closed loop");

            Position southWest = points[0];
            Position northEast = points[0];
            for (int i = 1; i < points.Count; i++)
            {
                if(southWest.Latitude > points[i].Latitude)
                    southWest = new Position(points[i].Latitude, southWest.Longitude);
                if(southWest.Longitude > points[i].Longitude)
                    southWest = new Position(southWest.Latitude, points[i].Longitude);
                if(northEast.Latitude < points[i].Latitude)
                    northEast = new Position(points[i].Latitude, northEast.Longitude);
                if(northEast.Longitude < points[i].Longitude)
                    northEast = new Position(northEast.Latitude, points[i].Longitude);
            }

            _extent = new Extent(southWest, northEast);
            foreach (Position point in points)
                _points.Add(_extent.GetPointInDrawingSpace(point));
                     
        }

        #region Public Methods

        #endregion

        #region Public Properties

        public Area Area
        {
            get
            {
                double area = 0;
                foreach (TriangleExt triangle in _triangles)
                    area += triangle.Area;
                return new Area(area, AreaUnit.SquareMeters);
            }
        }

        #endregion

        #region Private Methods
        
        #endregion*/
    }
}
