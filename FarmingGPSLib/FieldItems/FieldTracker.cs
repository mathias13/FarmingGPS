using System;
using System.Collections.Generic;
using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;
using DotSpatial.Topology.Precision;
using FarmingGPSLib.HelperClasses;
using FarmingGPSLib.FarmingModes.Tools;

namespace FarmingGPSLib.FieldItems
{
    //internal struct IntersectionInfo
    //{
    //    internal enum CutEnum
    //    {
    //        Right,
    //        Center,
    //        Left
    //    }

    //    internal IntersectionInfo(int index, Coordinate coord, CutEnum cutSide)
    //    {
    //        Index = index;
    //        IntersectCoordinate = coord;
    //        CutSide = cutSide;
    //    }

    //    internal int Index;

    //    internal Coordinate IntersectCoordinate;
        
    //    internal CutEnum CutSide;

    //}

    internal class CSVCoordinates
    {
        private IEnumerable<Coordinate> _coords;

        public CSVCoordinates(IEnumerable<Coordinate> coords)
        {
            _coords = coords;
        }

        public override string ToString()
        {
            string csvString = String.Empty;
            foreach (Coordinate coord in _coords)
                csvString += coord.X.ToString() + ";" + coord.Y.ToString() + Environment.NewLine;
            return csvString;
        }
    }

    public class FieldTracker
    {
        #region Events

        public event EventHandler<PolygonUpdatedEventArgs> PolygonUpdated;

        public event EventHandler<PolygonDeletedEventArgs> PolygonDeleted;

        #endregion

        #region Private Variables

        private IDictionary<int, Polygon> _polygons = new Dictionary<int, Polygon>();
                
        private Coordinate _prevLeftPoint = Coordinate.Empty;

        private Coordinate _prevRightPoint = Coordinate.Empty;

        private int _currentPolygonIndex = -1;

        private object _syncObject = new object();

        #endregion

        public FieldTracker()
        {}

        public FieldTracker(Coordinate initLeftPoint, Coordinate initRightPoint)
        {
            _prevLeftPoint = HelperClassCoordinate.CoordinateRoundedmm(initLeftPoint);
            _prevRightPoint = HelperClassCoordinate.CoordinateRoundedmm(initRightPoint);
            _currentPolygonIndex = -1;
        }

        #region Public Methods

        public void AddTrackPoint(Coordinate leftPoint, Coordinate rightPoint)
        {
            leftPoint = HelperClassCoordinate.CoordinateRoundedmm(leftPoint);
            rightPoint = HelperClassCoordinate.CoordinateRoundedmm(rightPoint);
            lock (_syncObject)
            {
                if (_currentPolygonIndex > -1 && _currentPolygonIndex < _polygons.Count)
                {
                    if (_polygons[_currentPolygonIndex].Shell.Coordinates.Count == 0)
                    {
                        _polygons[_currentPolygonIndex].Shell.Coordinates.Add(_prevRightPoint);
                        _polygons[_currentPolygonIndex].Shell.Coordinates.Add(rightPoint);
                        _polygons[_currentPolygonIndex].Shell.Coordinates.Add(leftPoint);
                        _polygons[_currentPolygonIndex].Shell.Coordinates.Add(_prevLeftPoint);
                        _polygons[_currentPolygonIndex].Shell.Coordinates.Add(_prevRightPoint);
                    }
                    else
                    {
                        bool polygonHoleChanged = false;

                        List<Coordinate> newCoords = new List<Coordinate>();
                        newCoords.Add(_prevRightPoint);
                        newCoords.Add(rightPoint);
                        newCoords.Add(leftPoint);
                        newCoords.Add(_prevLeftPoint);
                        LineString newCoordinates = new LineString(newCoords);
                        List<Coordinate> newRectangleCoords = new List<Coordinate>(newCoords);
                        newRectangleCoords.Add(_prevRightPoint);
                        ILineString rectangle = new LineString(newRectangleCoords);

                        IGeometry rectPolygon = new Polygon(rectangle.Coordinates);

                        if (!rectangle.IsSimple)
                        {
                            RobustLineIntersector lineIntersector = new RobustLineIntersector();
                            lineIntersector.ComputeIntersection(rightPoint, leftPoint, _prevLeftPoint, _prevRightPoint);
                            if (!lineIntersector.HasIntersection)
                                throw new Exception("No instersection point found but turning around itself");

                            List<Coordinate> leftTriangle = new List<Coordinate>();
                            leftTriangle.Add(_prevLeftPoint);
                            leftTriangle.Add(lineIntersector.IntersectionPoints[0]);
                            leftTriangle.Add(leftPoint);
                            leftTriangle.Add(_prevLeftPoint);

                            List<Coordinate> rightTriangle = new List<Coordinate>();
                            rightTriangle.Add(_prevRightPoint);
                            rightTriangle.Add(rightPoint);
                            rightTriangle.Add(lineIntersector.IntersectionPoints[0]);
                            rightTriangle.Add(_prevRightPoint);

                            if (CgAlgorithms.IsCounterClockwise(leftTriangle))
                            {
                                newCoords = new List<Coordinate>(leftTriangle);
                                newCoords.RemoveAt(0);
                                newCoordinates = new LineString(newCoords);
                                leftTriangle.RemoveAt(1);
                                leftTriangle.Insert(1, rightPoint);
                                rectPolygon = new Polygon(leftTriangle);
                            }
                            else if(CgAlgorithms.IsCounterClockwise(rightTriangle))
                            {
                                newCoords = new List<Coordinate>(rightTriangle);
                                newCoords.RemoveAt(0);
                                newCoordinates = new LineString(newCoords);
                                rightTriangle.RemoveAt(2);
                                rightTriangle.Insert(2, leftPoint);
                                rectPolygon = new Polygon(rightTriangle);
                            }
                            else
                                throw new Exception("No instersection point found but turning around itself");
                        }

                        int holeHash = _polygons[_currentPolygonIndex].Holes.GetHashCode();

                        _polygons[_currentPolygonIndex] = (Polygon)_polygons[_currentPolygonIndex].Union(rectPolygon);

                        if (holeHash != _polygons[_currentPolygonIndex].Holes.GetHashCode())
                        {
                            polygonHoleChanged = true;
                            List<ILinearRing> holes = new List<ILinearRing>(_polygons[_currentPolygonIndex].Holes);
                            for (int i = 0; i < holes.Count; i++)
                            {
                                if (!CheckHoleValidity(holes[i]))
                                {
                                    holes.RemoveAt(i);
                                    i--;
                                }
                                else
                                    holes[i] = RoundHoleCoordinates(holes[i]);

                            }
                            _polygons[_currentPolygonIndex].Holes = holes.ToArray();
                        }

                        OnPolygonUpdated(_currentPolygonIndex, newCoordinates, polygonHoleChanged);
                    }
                }
                else
                {
                    List<Coordinate> coords = new List<Coordinate>();
                    coords.Add(_prevRightPoint);
                    coords.Add(rightPoint);
                    coords.Add(leftPoint);
                    coords.Add(_prevLeftPoint);
                    LineString newCoordinates = new LineString(coords);
                    coords.Add(_prevRightPoint);
                    LinearRing ring = new LinearRing(coords);
                    Polygon polygon = new Polygon(ring);
                    int id = 0;
                    while (_polygons.Keys.Contains(id))
                        id++;
                    _polygons.Add(id, polygon);
                    _currentPolygonIndex = id;
                    OnPolygonUpdated(_currentPolygonIndex, newCoordinates, false);
                }
                _prevLeftPoint = leftPoint;
                _prevRightPoint = rightPoint;
            }
        }

        public void InitTrack(Coordinate initLeftPoint, Coordinate initRightPoint)
        {
            lock (_syncObject)
            {
                _prevLeftPoint = HelperClassCoordinate.CoordinateRoundedmm(initLeftPoint);
                _prevRightPoint = HelperClassCoordinate.CoordinateRoundedmm(initRightPoint);
                _currentPolygonIndex = -1;
            }
        }

        public void StopTrack()
        {
            lock (_syncObject)
            {
                _prevLeftPoint = Coordinate.Empty;
                _prevRightPoint = Coordinate.Empty;
            }
        }

        public double GetTrackingLineCoverage(TrackingLine trackingLine)
        {
            lock(_syncObject)
            {
                IMultiLineString remainsOfLine = new MultiLineString(new IBasicLineString[1] { trackingLine.Line });
                foreach(Polygon polygon in _polygons.Values)
                {
                    IGeometry remains = remainsOfLine.Difference(polygon);
                    if (remains is MultiLineString)
                        remainsOfLine = (MultiLineString)remains;
                    else if (remains is LineString)
                        new MultiLineString(new IBasicLineString[1] { (LineString)remains });
                    else if (remains.IsEmpty)
                    {
                        remainsOfLine = MultiLineString.Empty;
                        break;
                    }
                    else
                    {
                        remainsOfLine = MultiLineString.Empty;
                        break;
                    }
                }

                if (remainsOfLine == MultiLineString.Empty)
                    return 1.0;
                else
                    return 1.0 - (remainsOfLine.Length / trackingLine.Length);
            }
        }

        #endregion

        #region Public Properties

        public double Area
        {
            get
            {
                lock (_syncObject)
                {
                    double area = 0.0;
                    foreach (Polygon polygon in _polygons.Values)
                        if (polygon.IsValid)
                            area += polygon.Area;
                    return area;
                }
            }
        }
        
        public bool IsTracking
        {
            get
            {
                lock(_syncObject)
                    return !_prevLeftPoint.IsEmpty() && !_prevRightPoint.IsEmpty();
            }
        }

        public Polygon this[int id]
        {
            get
            {
                lock (_syncObject)
                {
                    if (_polygons.Keys.Contains(id))
                        return _polygons[id];
                    else
                        return null;
                }
            }
        }
        
        #endregion

        #region Protected Methods

        protected void OnPolygonUpdated(int id, ILineString newRectangle, bool redrawPolygon)
        {
            if (PolygonUpdated != null)
                PolygonUpdated.Invoke(this, new PolygonUpdatedEventArgs(id, _polygons[id], newRectangle, redrawPolygon));
        }

        protected void OnPolygonDeleted(int id)
        {
            if (PolygonDeleted != null)
                PolygonDeleted.Invoke(this, new PolygonDeletedEventArgs(id));
        }

        protected void OnAreaChanged()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        private bool CheckHoleValidity(ILinearRing hole)
        {
            double area = Math.Abs(CgAlgorithms.SignedArea(hole.Coordinates));
            if (area < 0.1)
                return false;

            return true;
        }

        private ILinearRing RoundHoleCoordinates(ILinearRing hole)
        {
            List<Coordinate> coords = new List<Coordinate>();
            foreach (Coordinate coord in hole.Coordinates)
                coords.Add(HelperClassCoordinate.CoordinateRoundedmm(coord));
            return new LinearRing(coords);
        }

        #endregion
    }
}
