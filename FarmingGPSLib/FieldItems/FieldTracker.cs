using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.StateRecovery;
using GeoAPI.Geometries;
using GpsUtilities.HelperClasses;
using log4net;
using DotSpatial.NTSExtension;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Simplify;
using System;
using System.Collections.Generic;

namespace FarmingGPSLib.FieldItems
{
    public class FieldTracker: IStateObject
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //TODO: Delete this if it's possible to serialize Coordinate
        [Serializable]
        public struct SimpleCoordinate
        {
            public double X;

            public double Y;

            public double Z;
        }

        [Serializable]
        public struct SimpleCoordinateArray
        {
            public int PolygonIndex;

            public SimpleCoordinate[] Coordinates;

            [System.Xml.Serialization.XmlIgnore]
            public Coordinate[] CoordinateArray
            {
                get
                {
                    var coords = new List<Coordinate>();
                    foreach (var coord in Coordinates)
                        coords.Add(new Coordinate(coord.X, coord.Y, coord.Z));
                    return coords.ToArray();
                }
            }
               
            public SimpleCoordinateArray(int polygonIndex, Coordinate[] coordinates)
            {
                PolygonIndex = polygonIndex;
                var coords = new List<SimpleCoordinate>();
                foreach(var coord in coordinates)
                    coords.Add(new SimpleCoordinate() { X = coord.X, Y = coord.Y, Z = coord.Z });
                Coordinates = coords.ToArray();
            }

        }

        [Serializable]
        public struct FieldTrackerState
        {
            public SimpleCoordinateArray[] Polygons;
            
            public SimpleCoordinateArray[] Holes;

            public bool AutoStartStop;
            
            public FieldTrackerState(SimpleCoordinateArray[] polygons, SimpleCoordinateArray[] holes, bool autoStartStop)
            {
                Polygons = polygons;
                Holes = holes;
                AutoStartStop = autoStartStop;
            }
        }

        public struct TrackPoint
        {
            public TrackPoint(Coordinate leftPoint, Coordinate rightPoint)
            {
                LineSegment line = new LineSegment(leftPoint, rightPoint);
                LeftPoint = HelperClassCoordinate.ComputePoint(leftPoint, line.Angle + HelperClassAngles.DEGREE_180_RAD, 0.10);
                RightPoint = HelperClassCoordinate.ComputePoint(rightPoint, line.Angle, 0.10);
            }

            public Coordinate LeftPoint;

            public Coordinate RightPoint;
        }

        #region Events

        public event EventHandler<PolygonUpdatedEventArgs> PolygonUpdated;

        public event EventHandler<PolygonDeletedEventArgs> PolygonDeleted;

        public event EventHandler<AreaChanged> AreaChanged;

        #endregion

        #region Consts

        #endregion

        #region Private Variables

        private IField _fieldToCalculateAreaWithin = null;
        
        private IDictionary<int, Polygon> _polygons = new Dictionary<int, Polygon>();

        private Coordinate _prevLeftPoint = new Coordinate(double.NaN, double.NaN);

        private Coordinate _prevRightPoint = new Coordinate(double.NaN, double.NaN);

        private int _currentPolygonIndex = -1;

        private int _areaChangedLimitCounter = 0;

        private object _syncObject = new object();

        private bool _hasChanged = true;

        #endregion

        public FieldTracker()
        {}

        public FieldTracker(TrackPoint trackPoint)
        {
            InitTrack(trackPoint);
        }

        #region Public Methods

        public void AddTrackPoints(TrackPoint[] trackPoints)
        {
            lock (_syncObject)
            {
                List<int> polygonsUpdated = new List<int>();
                List<int> polygonsDeleted = new List<int>();
                var polygonIDs = new int[_polygons.Keys.Count];
                _polygons.Keys.CopyTo(polygonIDs, 0);
                try
                {
                    foreach (var trackPoint in trackPoints)
                    {
                        if (_currentPolygonIndex > -1)
                        {
                            List<Coordinate> newCoords = new List<Coordinate>();
                            newCoords.Add(_prevRightPoint);
                            newCoords.Add(trackPoint.RightPoint);
                            newCoords.Add(trackPoint.LeftPoint);
                            newCoords.Add(_prevLeftPoint);
                            LineString newCoordinates = new LineString(newCoords.ToArray());
                            List<Coordinate> newRectangleCoords = new List<Coordinate>(newCoords);
                            newRectangleCoords.Add(_prevRightPoint);
                            ILineString rectangle = new LineString(newRectangleCoords.ToArray());

                            IGeometry rectPolygon = new Polygon(new LinearRing(rectangle.Coordinates));

                            RobustLineIntersector lineIntersector = new RobustLineIntersector();
                            lineIntersector.ComputeIntersection(trackPoint.RightPoint, trackPoint.LeftPoint, _prevLeftPoint, _prevRightPoint);
                            if (lineIntersector.HasIntersection)
                            {
                                List<Coordinate> leftTriangle = new List<Coordinate>(new Coordinate[] { _prevLeftPoint, lineIntersector.GetIntersection(0), trackPoint.LeftPoint, _prevLeftPoint });

                                List<Coordinate> rightTriangle = new List<Coordinate>(new Coordinate[] { _prevRightPoint, trackPoint.RightPoint, lineIntersector.GetIntersection(0), _prevRightPoint });

                                if (CGAlgorithms.IsCCW(leftTriangle.ToArray()))
                                {
                                    newCoords = new List<Coordinate>(leftTriangle);
                                    newCoords.RemoveAt(0);
                                    newCoordinates = new LineString(newCoords.ToArray());
                                    rectPolygon = new Polygon(new LinearRing(new Coordinate[] { _prevLeftPoint, trackPoint.RightPoint, trackPoint.LeftPoint, _prevLeftPoint }));
                                }
                                else if (CGAlgorithms.IsCCW(rightTriangle.ToArray()))
                                {
                                    newCoords = new List<Coordinate>(rightTriangle);
                                    newCoords.RemoveAt(0);
                                    newCoordinates = new LineString(newCoords.ToArray());
                                    rectPolygon = new Polygon(new LinearRing(new Coordinate[] { _prevRightPoint, trackPoint.RightPoint, trackPoint.LeftPoint, _prevRightPoint }));
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
                            _polygons[_currentPolygonIndex] = new Polygon(_polygons[_currentPolygonIndex].Shell, holes.ToArray());

                            foreach (int i in polygonIDs)
                            {
                                if (i == _currentPolygonIndex)
                                    continue;
                                if (_polygons[_currentPolygonIndex].Overlaps(_polygons[i]))
                                {
                                    _polygons[_currentPolygonIndex] = (Polygon)_polygons[_currentPolygonIndex].Union(_polygons[i]);
                                    _polygons.Remove(i);
                                    if (!polygonsDeleted.Contains(i))
                                        polygonsDeleted.Add(i);
                                }
                            }
                            if (!insidePolygon)
                            {
                                if (_areaChangedLimitCounter > 15)
                                {
                                    OnAreaChanged();
                                    _areaChangedLimitCounter = 0;
                                }
                                _areaChangedLimitCounter++;
                                if (!polygonsUpdated.Contains(_currentPolygonIndex))
                                    polygonsUpdated.Add(_currentPolygonIndex);
                            }
                        }
                        else
                        {
                            List<Coordinate> coords = new List<Coordinate>();
                            coords.Add(_prevRightPoint);
                            coords.Add(trackPoint.RightPoint);
                            coords.Add(trackPoint.LeftPoint);
                            coords.Add(_prevLeftPoint);
                            coords.Add(_prevRightPoint);
                            LinearRing ring = new LinearRing(coords.ToArray());
                            Polygon polygon = new Polygon(ring);
                            if (ring.IsSimple && CGAlgorithms.IsCCW(polygon.Coordinates))
                            {
                                int id = 0;
                                while (_polygons.Keys.Contains(id))
                                    id++;
                                _polygons.Add(id, polygon);
                                _currentPolygonIndex = id;
                                if (!polygonsUpdated.Contains(_currentPolygonIndex))
                                    polygonsUpdated.Add(_currentPolygonIndex);
                                OnAreaChanged();
                            }
                        }
                        _prevLeftPoint = trackPoint.LeftPoint;
                        _prevRightPoint = trackPoint.RightPoint;
                    }

                    foreach (var polygonDeleted in polygonsDeleted)
                        if (polygonsUpdated.Contains(polygonDeleted))
                            polygonsUpdated.Remove(polygonDeleted);

                    foreach (var updated in polygonsUpdated)
                        OnPolygonUpdated(updated);

                    foreach (var deleted in polygonsDeleted)
                        OnPolygonDeleted(deleted);
                }
                catch (Exception e)
                {
                    Log.Warn("Failed to add new trackingpoints", e);
                }
            }
        }

        public void InitTrack(TrackPoint trackPoint)
        {
            lock (_syncObject)
            {
                _prevLeftPoint = trackPoint.LeftPoint;
                _prevRightPoint = trackPoint.RightPoint;
            }
        }

        public void StopTrack()
        {
            lock (_syncObject)
            {
                _prevLeftPoint = new Coordinate(double.NaN, double.NaN);
                _prevRightPoint = new Coordinate(double.NaN, double.NaN);
                _currentPolygonIndex = -1;
            }
        }

        public void StopTrack(TrackPoint trackPoint)
        {
            lock (_syncObject)
            {
                AddTrackPoints(new TrackPoint[1] { trackPoint });
                _prevLeftPoint = new Coordinate(double.NaN, double.NaN);
                _prevRightPoint = new Coordinate(double.NaN, double.NaN);
                _currentPolygonIndex = -1;
            }
        }

        public void ClearTrack()
        {
            lock(_syncObject)
            {
                _prevLeftPoint = new Coordinate(double.NaN, double.NaN);
                _prevRightPoint = new Coordinate(double.NaN, double.NaN);
                _currentPolygonIndex = -1;
                foreach (KeyValuePair<int, Polygon> polygon in _polygons)
                    OnPolygonDeleted(polygon.Key);

                _polygons.Clear();
            }
        }

        public double GetTrackingLineCoverage(TrackingLine trackingLine)
        {
            lock(_syncObject)
            {
                IMultiLineString remainsOfLine = new MultiLineString(new LineString[1] { new LineString(trackingLine.Line.Coordinates) });
                foreach(Polygon polygon in _polygons.Values)
                {
                    IGeometry remains = remainsOfLine.Difference(polygon);
                    if (remains is MultiLineString)
                        remainsOfLine = (MultiLineString)remains;
                    else if (remains is LineString)
                        remainsOfLine = new MultiLineString(new LineString[1] { (LineString)remains });
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

        public bool AutoStartStop
        {
            get;
            set;
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
            double area = Math.Abs(CGAlgorithms.SignedArea(hole.Coordinates));
            if (area < 4.0)
                return false;

            return true;
        }

        private ILinearRing RoundCoordinates(ILinearRing ring)
        {
            List<Coordinate> coords = new List<Coordinate>();
            foreach (Coordinate coord in ring.Coordinates)
                coords.Add(HelperClassCoordinate.CoordinateRoundedmm(coord));
            return new LinearRing(coords.ToArray());
        }

        #endregion

        #region IStateObject

        public void RestoreObject(object restoredState)
        {
            lock (_syncObject)
            {
                FieldTrackerState state = (FieldTrackerState)restoredState;
                AutoStartStop = state.AutoStartStop;
                foreach (SimpleCoordinateArray polygon in state.Polygons)
                {
                    List<ILinearRing> holes = new List<ILinearRing>();
                    foreach (SimpleCoordinateArray hole in state.Holes)
                        if (hole.PolygonIndex == polygon.PolygonIndex)
                            holes.Add(new LinearRing(hole.CoordinateArray));
                    Polygon newPolygon = new Polygon(new LinearRing(polygon.CoordinateArray), holes.ToArray());
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
                Dictionary<int, IGeometry> simplifiedPolygons = new Dictionary<int, IGeometry>();
                _hasChanged = false;
                List<SimpleCoordinateArray> polygons = new List<SimpleCoordinateArray>();
                List<SimpleCoordinateArray> holes = new List<SimpleCoordinateArray>();
                lock (_syncObject)
                {
                    foreach (KeyValuePair<int, Polygon> polygon in _polygons)
                        simplifiedPolygons.Add(polygon.Key, TopologyPreservingSimplifier.Simplify(polygon.Value, 0.04));
                }

                foreach (KeyValuePair<int, IGeometry> simplifiedPolygon in simplifiedPolygons)
                {
                    if (simplifiedPolygon.Value is IPolygon)
                    {
                        IPolygon polygon = simplifiedPolygon.Value as IPolygon;
                        polygons.Add(new SimpleCoordinateArray(simplifiedPolygon.Key, polygon.Shell.Coordinates));

                        foreach (ILinearRing hole in polygon.Holes)
                        {
                            holes.Add(new SimpleCoordinateArray(simplifiedPolygon.Key, hole.Coordinates));
                        }
                    }
                }

                FieldTrackerState state = new FieldTrackerState(polygons.ToArray(), holes.ToArray(), AutoStartStop);
                return state;
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
