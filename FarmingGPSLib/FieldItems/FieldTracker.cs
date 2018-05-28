using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.HelperClasses;
using log4net;
using System;
using System.Collections.Generic;
using FarmingGPSLib.StateRecovery;
using System.Xml.Serialization;

namespace FarmingGPSLib.FieldItems
{
    public class FieldTracker: IStateObject
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        [Serializable]
        public struct SimpleCoordinateArray
        {
            public int PolygonIndex;
            
            public List<Coordinate> Coordinates;

            public SimpleCoordinateArray(int polygonIndex, List<Coordinate> coordinates)
            {
                PolygonIndex = polygonIndex;
                Coordinates = coordinates;
            }

        }

        [Serializable]
        public struct FieldTrackerState
        {
            public List<SimpleCoordinateArray> Polygons;
            
            public List<SimpleCoordinateArray> Holes;
            

            public FieldTrackerState(List<SimpleCoordinateArray> polygons, List<SimpleCoordinateArray> holes)
            {
                Polygons = polygons;
                Holes = holes;
            }
        }

        #region Events

        public event EventHandler<PolygonUpdatedEventArgs> PolygonUpdated;

        public event EventHandler<PolygonDeletedEventArgs> PolygonDeleted;

        public event EventHandler<AreaChanged> AreaChanged;

        #endregion

        #region Consts

        private const int SIMPLIFIER_COUNT_LIMIT = 50;

        #endregion

        #region Private Variables

        private IField _fieldToCalculateAreaWithin = null;
        
        private IDictionary<int, Polygon> _polygons = new Dictionary<int, Polygon>();

        private IDictionary<int, int> _polygonSimplifierCount = new Dictionary<int, int>();
                
        private Coordinate _prevLeftPoint = Coordinate.Empty;

        private Coordinate _prevRightPoint = Coordinate.Empty;

        private int _currentPolygonIndex = -1;

        private object _syncObject = new object();

        private bool _hasChanged = true;

        #endregion

        public FieldTracker()
        {}

        public FieldTracker(Coordinate initLeftPoint, Coordinate initRightPoint)
        {
            InitTrack(initLeftPoint, initRightPoint);
        }

        #region Public Methods

        public void AddTrackPoint(Coordinate leftPoint, Coordinate rightPoint)
        {
            lock (_syncObject)
            {
                try
                {
                    if (_currentPolygonIndex > -1)
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

                            RobustLineIntersector lineIntersector = new RobustLineIntersector();
                            lineIntersector.ComputeIntersection(rightPoint, leftPoint, _prevLeftPoint, _prevRightPoint);
                            if (lineIntersector.HasIntersection)
                            {
                                List<Coordinate> leftTriangle = new List<Coordinate>(new Coordinate[] { _prevLeftPoint, lineIntersector.IntersectionPoints[0], leftPoint, _prevLeftPoint });

                                List<Coordinate> rightTriangle = new List<Coordinate>(new Coordinate[] { _prevRightPoint, rightPoint, lineIntersector.IntersectionPoints[0], _prevRightPoint });

                                if (CgAlgorithms.IsCounterClockwise(leftTriangle))
                                {
                                    newCoords = new List<Coordinate>(leftTriangle);
                                    newCoords.RemoveAt(0);
                                    newCoordinates = new LineString(newCoords);
                                    rectPolygon = new Polygon(new Coordinate[] { _prevLeftPoint, rightPoint, leftPoint, _prevLeftPoint });
                                }
                                else if (CgAlgorithms.IsCounterClockwise(rightTriangle))
                                {
                                    newCoords = new List<Coordinate>(rightTriangle);
                                    newCoords.RemoveAt(0);
                                    newCoordinates = new LineString(newCoords);
                                    rectPolygon = new Polygon(new Coordinate[] { _prevRightPoint, rightPoint, leftPoint, _prevRightPoint });
                                }
                                else
                                    throw new Exception("Intersection found but can't decide triangle");
                            }

                            bool insidePolygon = _polygons[_currentPolygonIndex].Contains(rectPolygon);

                            _polygons[_currentPolygonIndex] = (Polygon)_polygons[_currentPolygonIndex].Union(rectPolygon);

                            List<ILinearRing> holes = new List<ILinearRing>(_polygons[_currentPolygonIndex].Holes);
                            for (int i = 0; i < holes.Count; i++)
                            {
                                if (!CheckHoleValidity(holes[i]))
                                {
                                    holes.RemoveAt(i);
                                    i--;
                                }
                            }
                            _polygons[_currentPolygonIndex].Holes = holes.ToArray();

                            if (_polygons[_currentPolygonIndex].Coordinates.Count > _polygonSimplifierCount[_currentPolygonIndex])
                            {
                                IGeometry geometry = DotSpatial.Topology.Simplify.TopologyPreservingSimplifier.Simplify(_polygons[_currentPolygonIndex], 0.04);
                                if (geometry is Polygon)
                                {
                                    _polygons[_currentPolygonIndex] = geometry as Polygon;
                                    _polygonSimplifierCount[_currentPolygonIndex] = geometry.Coordinates.Count + SIMPLIFIER_COUNT_LIMIT;
                                }
                            }

                            for (int i = 0; i < _polygons.Count; i++)
                            {
                                if (i == _currentPolygonIndex || !_polygons.ContainsKey(i))
                                    continue;
                                if (_polygons[_currentPolygonIndex].Overlaps(_polygons[i]))
                                {
                                    _polygons[_currentPolygonIndex] = (Polygon)_polygons[_currentPolygonIndex].Union(_polygons[i]);
                                    _polygons.Remove(i);
                                    _polygonSimplifierCount.Remove(i);
                                    OnPolygonDeleted(i);
                                }
                            }
                            if (!insidePolygon)
                            {
                                OnAreaChanged();
                                OnPolygonUpdated(_currentPolygonIndex);
                            }
                        }
                    }
                    else
                    {
                        List<Coordinate> coords = new List<Coordinate>();
                        coords.Add(_prevRightPoint);
                        coords.Add(rightPoint);
                        coords.Add(leftPoint);
                        coords.Add(_prevLeftPoint);
                        coords.Add(_prevRightPoint);
                        LinearRing ring = new LinearRing(coords);
                        Polygon polygon = new Polygon(ring);
                        int id = 0;
                        while (_polygons.Keys.Contains(id))
                            id++;
                        _polygons.Add(id, polygon);
                        _polygonSimplifierCount.Add(id, SIMPLIFIER_COUNT_LIMIT);
                        _currentPolygonIndex = id;
                        OnPolygonUpdated(_currentPolygonIndex);
                        OnAreaChanged();
                    }
                    //Make sure we are a little bit behind and to the middle so that .Union doesn't throw an exception next update
                    LineSegment line = new LineSegment(leftPoint, rightPoint);
                    Angle angle = new Angle(line.Angle);
                    angle -= new Angle(Angle.PI / 4.0);
                    _prevLeftPoint = HelperClassCoordinate.ComputePoint(leftPoint, angle.Radians, 0.02);
                    angle -= new Angle(Angle.PI / 2.0);
                    _prevRightPoint = HelperClassCoordinate.ComputePoint(rightPoint, angle.Radians, 0.02);
                }
                catch (Exception e)
                {
                    Log.Error("Failed add new trackingpoint", e);
                }
            }
        }

        public void InitTrack(Coordinate initLeftPoint, Coordinate initRightPoint)
        {
            lock (_syncObject)
            {
                _prevLeftPoint = initLeftPoint;
                _prevRightPoint = initRightPoint;
            }
        }

        public void StopTrack(Coordinate leftPoint, Coordinate rightPoint)
        {
            lock (_syncObject)
            {
                AddTrackPoint(leftPoint, rightPoint);
                _prevLeftPoint = Coordinate.Empty;
                _prevRightPoint = Coordinate.Empty;
                _currentPolygonIndex = -1;
            }
        }

        public void ClearTrack()
        {
            lock(_syncObject)
            {
                _prevLeftPoint = Coordinate.Empty;
                _prevRightPoint = Coordinate.Empty;
                _currentPolygonIndex = -1;
                foreach (KeyValuePair<int, Polygon> polygon in _polygons)
                    OnPolygonDeleted(polygon.Key);

                _polygons.Clear();
                _polygonSimplifierCount.Clear();
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
                        remainsOfLine = new MultiLineString(new IBasicLineString[1] { (LineString)remains });
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

        public DotSpatial.Positioning.Area Area
        {
            get
            {
                lock (_syncObject)
                {
                    double area = 0.0;
                    foreach (Polygon polygon in _polygons.Values)
                    {
                        Polygon polygonToUse = polygon;
                        if(_fieldToCalculateAreaWithin != null)
                        {
                            //if (!polygon.Overlaps(_fieldToCalculateAreaWithin.Polygon))
                            //    continue;
                            IGeometry geometry = _fieldToCalculateAreaWithin.Polygon.Intersection(polygon);
                            if (geometry is Polygon)
                                polygonToUse = geometry as Polygon;
                        }
                        if (polygonToUse.IsValid)
                            area += polygonToUse.Area;
                    }
                    return new DotSpatial.Positioning.Area(area, DotSpatial.Positioning.AreaUnit.SquareMeters);
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

        public IField FieldToCalculateAreaWithin
        {
            set { _fieldToCalculateAreaWithin = value; }
            get { return _fieldToCalculateAreaWithin; }
        }

        #endregion

        #region Protected Methods

        protected void OnPolygonUpdated(int id)
        {
            _hasChanged = true;
            if (PolygonUpdated != null)
                PolygonUpdated.Invoke(this, new PolygonUpdatedEventArgs(id, _polygons[id]));
        }

        protected void OnPolygonDeleted(int id)
        {
            _hasChanged = true;
            if (PolygonDeleted != null)
                PolygonDeleted.Invoke(this, new PolygonDeletedEventArgs(id));
        }

        protected void OnAreaChanged()
        {
            if (AreaChanged != null)
                AreaChanged.Invoke(this, new AreaChanged(Area));
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

        private ILinearRing RoundCoordinates(ILinearRing ring)
        {
            List<Coordinate> coords = new List<Coordinate>();
            foreach (Coordinate coord in ring.Coordinates)
                coords.Add(HelperClassCoordinate.CoordinateRoundedmm(coord));
            return new LinearRing(coords);
        }

        #endregion

        #region IStateObject

        public void RestoreObject(object restoredState)
        {
            lock (_syncObject)
            {
                FieldTrackerState state = (FieldTrackerState)restoredState;
                foreach (SimpleCoordinateArray polygon in state.Polygons)
                {
                    List<ILinearRing> holes = new List<ILinearRing>();
                    foreach (SimpleCoordinateArray hole in state.Holes)
                        if (hole.PolygonIndex == polygon.PolygonIndex)
                            holes.Add(new LinearRing(hole.Coordinates));
                    Polygon newPolygon = new Polygon(new LinearRing(polygon.Coordinates), holes.ToArray());
                    _polygons.Add(polygon.PolygonIndex, newPolygon);
                }
                foreach (int index in _polygons.Keys)
                    OnPolygonUpdated(index);
                OnAreaChanged();
            }
        }

        public object StateObject
        {
            get
            {
                lock(_syncObject)
                {
                    _hasChanged = false;
                    List<SimpleCoordinateArray> polygons = new List<SimpleCoordinateArray>();
                    List<SimpleCoordinateArray> holes = new List<SimpleCoordinateArray>();
                    foreach (KeyValuePair<int, Polygon> polygon in _polygons)
                    {
                        polygons.Add(new SimpleCoordinateArray(polygon.Key, new List<Coordinate>(polygon.Value.Shell.Coordinates)));

                        foreach (ILinearRing hole in polygon.Value.Holes)
                        {
                            holes.Add(new SimpleCoordinateArray(polygon.Key, new List<Coordinate>(hole.Coordinates)));
                        }                        
                    }

                    FieldTrackerState state = new FieldTrackerState(polygons, holes);
                    return state;
                }
            }
        }

        public bool HasChanged
        {
            get { return _hasChanged; }
        }

        public Type StateType
        {
            get { return typeof(FieldTrackerState); }
        }

        #endregion
    }
}
