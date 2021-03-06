﻿using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;
using FarmingGPSLib.FarmingModes.Tools;
using GpsUtilities.HelperClasses;
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

        public struct TrackPoint
        {
            public Coordinate LeftPoint;

            public Coordinate RightPoint;
        }

        #region Events

        public event EventHandler<PolygonUpdatedEventArgs> PolygonUpdated;

        public event EventHandler<PolygonDeletedEventArgs> PolygonDeleted;

        public event EventHandler<AreaChanged> AreaChanged;

        #endregion

        #region Consts

        private const int SIMPLIFIER_COUNT_LIMIT = 200;

        #endregion

        #region Private Variables

        private IField _fieldToCalculateAreaWithin = null;
        
        private IDictionary<int, Polygon> _polygons = new Dictionary<int, Polygon>();

        private IDictionary<int, int> _polygonSimplifierCount = new Dictionary<int, int>();

        private Coordinate _prevLeftPoint = Coordinate.Empty;

        private Coordinate _prevRightPoint = Coordinate.Empty;

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
                            LineString newCoordinates = new LineString(newCoords);
                            List<Coordinate> newRectangleCoords = new List<Coordinate>(newCoords);
                            newRectangleCoords.Add(_prevRightPoint);
                            ILineString rectangle = new LineString(newRectangleCoords);

                            IGeometry rectPolygon = new Polygon(rectangle.Coordinates);

                            RobustLineIntersector lineIntersector = new RobustLineIntersector();
                            lineIntersector.ComputeIntersection(trackPoint.RightPoint, trackPoint.LeftPoint, _prevLeftPoint, _prevRightPoint);
                            if (lineIntersector.HasIntersection)
                            {
                                List<Coordinate> leftTriangle = new List<Coordinate>(new Coordinate[] { _prevLeftPoint, lineIntersector.IntersectionPoints[0], trackPoint.LeftPoint, _prevLeftPoint });

                                List<Coordinate> rightTriangle = new List<Coordinate>(new Coordinate[] { _prevRightPoint, trackPoint.RightPoint, lineIntersector.IntersectionPoints[0], _prevRightPoint });

                                if (CgAlgorithms.IsCounterClockwise(leftTriangle))
                                {
                                    newCoords = new List<Coordinate>(leftTriangle);
                                    newCoords.RemoveAt(0);
                                    newCoordinates = new LineString(newCoords);
                                    rectPolygon = new Polygon(new Coordinate[] { _prevLeftPoint, trackPoint.RightPoint, trackPoint.LeftPoint, _prevLeftPoint });
                                }
                                else if (CgAlgorithms.IsCounterClockwise(rightTriangle))
                                {
                                    newCoords = new List<Coordinate>(rightTriangle);
                                    newCoords.RemoveAt(0);
                                    newCoordinates = new LineString(newCoords);
                                    rectPolygon = new Polygon(new Coordinate[] { _prevRightPoint, trackPoint.RightPoint, trackPoint.LeftPoint, _prevRightPoint });
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
                            LinearRing ring = new LinearRing(coords);
                            Polygon polygon = new Polygon(ring);
                            if (ring.IsSimple && CgAlgorithms.IsCounterClockwise(polygon.Coordinates))
                            {
                                int id = 0;
                                while (_polygons.Keys.Contains(id))
                                    id++;
                                _polygons.Add(id, polygon);
                                _currentPolygonIndex = id;
                                _polygonSimplifierCount.Add(id, SIMPLIFIER_COUNT_LIMIT);
                                if (!polygonsUpdated.Contains(_currentPolygonIndex))
                                    polygonsUpdated.Add(_currentPolygonIndex);
                                OnAreaChanged();
                            }
                        }
                        //Make sure we are a little bit behind and to the middle so that .Union doesn't throw an exception next update
                        Angle angle = new Angle(new LineSegment(trackPoint.LeftPoint, trackPoint.RightPoint).Angle);
                        angle -= new Angle(Angle.PI * 0.45);
                        _prevLeftPoint = HelperClassCoordinate.ComputePoint(trackPoint.LeftPoint, angle.Radians, 0.05);
                        angle = new Angle(new LineSegment(trackPoint.RightPoint, trackPoint.LeftPoint).Angle);
                        angle += new Angle(Angle.PI * 0.45);
                        _prevRightPoint = HelperClassCoordinate.ComputePoint(trackPoint.RightPoint, angle.Radians, 0.05);
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
                    Log.Error("Failed add new trackingpoints", e);
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
                _prevLeftPoint = Coordinate.Empty;
                _prevRightPoint = Coordinate.Empty;
                _currentPolygonIndex = -1;
            }
        }

        public void StopTrack(TrackPoint trackPoint)
        {
            lock (_syncObject)
            {
                AddTrackPoints(new TrackPoint[1] { trackPoint });
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
            if (area < 0.4)
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
                Dictionary<int, IGeometry> simplifiedPolygons = new Dictionary<int, IGeometry>();
                _hasChanged = false;
                List<SimpleCoordinateArray> polygons = new List<SimpleCoordinateArray>();
                List<SimpleCoordinateArray> holes = new List<SimpleCoordinateArray>();
                lock (_syncObject)
                {
                    foreach (KeyValuePair<int, Polygon> polygon in _polygons)
                        simplifiedPolygons.Add(polygon.Key, DotSpatial.Topology.Simplify.TopologyPreservingSimplifier.Simplify(polygon.Value, 0.04));
                }

                foreach (KeyValuePair<int, IGeometry> simplifiedPolygon in simplifiedPolygons)
                {
                    if (simplifiedPolygon.Value is IPolygon)
                    {
                        IPolygon polygon = simplifiedPolygon.Value as IPolygon;
                        polygons.Add(new SimpleCoordinateArray(simplifiedPolygon.Key, new List<Coordinate>(polygon.Shell.Coordinates)));

                        foreach (ILinearRing hole in polygon.Holes)
                        {
                            holes.Add(new SimpleCoordinateArray(simplifiedPolygon.Key, new List<Coordinate>(hole.Coordinates)));
                        }
                    }
                }

                FieldTrackerState state = new FieldTrackerState(polygons, holes);
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
