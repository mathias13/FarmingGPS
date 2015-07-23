using System;
using System.Collections.Generic;
using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;
using DotSpatial.Topology.Precision;
using FarmingGPSLib.HelperClasses;

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
            _prevLeftPoint = initLeftPoint;
            _prevRightPoint = initRightPoint;
            _currentPolygonIndex = -1;
        }

        #region Public Methods

        public void AddTrackPoint(Coordinate leftPoint, Coordinate rightPoint)
        {
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

                        List<ILinearRing> newHoles = new List<ILinearRing>();
                        
                        IGeometry newGeometry = EnhancedPrecisionOp.Union(new Polygon(_polygons[_currentPolygonIndex].Shell), rectPolygon);
                        if (newGeometry is Polygon)
                        {
                            Polygon newPolygon = (Polygon)newGeometry;
                            _polygons[_currentPolygonIndex].Shell = newPolygon.Shell;
                            foreach(ILinearRing hole in newPolygon.Holes)
                            {
                                if (!CheckHoleValidity(hole))
                                    continue;
                                polygonHoleChanged = true;
                                if (!CgAlgorithms.IsCounterClockwise(hole.Coordinates))
                                {
                                    List<Coordinate> coords = new List<Coordinate>(hole.Coordinates);
                                    coords.Reverse();
                                    newHoles.Add(new LinearRing(coords));
                                }
                                else
                                    newHoles.Add(hole);
                            }
                        }
                        else
                            throw new Exception("Geometry is not a polygon after union");

                        List<ILinearRing> holes = new List<ILinearRing>(_polygons[_currentPolygonIndex].Holes);
                        for (int i = 0; i < holes.Count; i++ )
                        {
                            Polygon holeAsPolygon = new Polygon(holes[i]);
                            IGeometry newHoleGeometry = EnhancedPrecisionOp.Difference(new Polygon(holes[i]), rectPolygon);
                            if (!newHoleGeometry.Equals((IGeometry)holeAsPolygon))
                            {
                                if (newHoleGeometry is Polygon)
                                {
                                    if (!CheckHoleValidity(((Polygon)newHoleGeometry).Shell))
                                        continue;
                                    if (!CgAlgorithms.IsCounterClockwise(((Polygon)newHoleGeometry).Shell.Coordinates))
                                    {
                                        List<Coordinate> coords = new List<Coordinate>(((Polygon)newHoleGeometry).Shell.Coordinates);
                                        coords.Reverse();
                                        holes[i] = new LinearRing(coords);
                                    }
                                    else
                                        holes[i] = ((Polygon)newHoleGeometry).Shell;
                                }
                                else if (newHoleGeometry is MultiPolygon)
                                {
                                    holes.RemoveAt(i);
                                    i--;
                                    foreach (IGeometry geometry in (MultiPolygon)newHoleGeometry)
                                    {
                                        if (geometry is Polygon)
                                        {
                                            Polygon polygon = (Polygon)geometry;
                                            if (!CheckHoleValidity(polygon.Shell))
                                                continue;
                                            if (!CgAlgorithms.IsCounterClockwise(polygon.Shell.Coordinates))
                                            {
                                                List<Coordinate> coords = new List<Coordinate>(polygon.Shell.Coordinates);
                                                coords.Reverse();
                                                newHoles.Add(new LinearRing(coords));
                                            }
                                            else
                                                newHoles.Add(polygon.Shell);
                                        }
                                    }
                                }
                                else if (newHoleGeometry.IsEmpty)
                                {
                                    holes.RemoveAt(i);
                                    i--;
                                }
                                polygonHoleChanged = true;
                            }
                            else
                                polygonHoleChanged = false;
                        }

                        if(polygonHoleChanged)
                        {
                            holes.AddRange(newHoles);
                            _polygons[_currentPolygonIndex].Holes = holes.ToArray();
                        }

                        //TODO Remove this
                        polygonHoleChanged = true;
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

        //public void AddTrackPoint(Coordinate leftPoint, Coordinate rightPoint)
        //{
        //    lock (_syncObject)
        //    {
        //        if (_currentPolygonIndex > -1 && _currentPolygonIndex < _polygons.Count)
        //        {
        //            if (_polygons[_currentPolygonIndex].Shell.Coordinates.Count == 0)
        //            {
        //                _polygons[_currentPolygonIndex].Shell.Coordinates.Add(_prevRightPoint);
        //                _polygons[_currentPolygonIndex].Shell.Coordinates.Add(rightPoint);
        //                _polygons[_currentPolygonIndex].Shell.Coordinates.Add(leftPoint);
        //                _polygons[_currentPolygonIndex].Shell.Coordinates.Add(_prevLeftPoint);
        //                _polygons[_currentPolygonIndex].Shell.Coordinates.Add(_prevRightPoint);
        //            }
        //            else
        //            {
        //                ILineString rectangle = new LineString(new Coordinate[5]{_prevRightPoint,
        //                                                                    rightPoint,
        //                                                                    leftPoint,
        //                                                                    _prevLeftPoint,
        //                                                                    _prevRightPoint});

        //                bool polygonHoleChanged = false;
        //                if (rectangle.IsSimple)
        //                {
        //                    RobustLineIntersector lineIntersector = new RobustLineIntersector();

        //                    int prevRightPointIndex = _polygons[_currentPolygonIndex].Shell.Coordinates.IndexOf(_prevRightPoint);
        //                    int prevLeftPointIndex = _polygons[_currentPolygonIndex].Shell.Coordinates.IndexOf(_prevLeftPoint);

        //                    LineSegment rightLine = new LineSegment(_prevRightPoint, rightPoint);
        //                    LineSegment centerLine = new LineSegment(rightPoint, leftPoint);
        //                    LineSegment leftLine = new LineSegment(leftPoint, _prevLeftPoint);

        //                    List<CutInfo> cutInfos = new List<CutInfo>();

        //                    bool rightCutting = false;
        //                    bool leftCutting = false;

        //                    for (int i = 0; i < _polygons[_currentPolygonIndex].Shell.Coordinates.Count - 1; i++)
        //                    {
        //                        if (lineIntersector.ComputeIntersect(leftLine.P0, leftLine.P1, _polygons[_currentPolygonIndex].Shell.Coordinates[i], _polygons[_currentPolygonIndex].Shell.Coordinates[i + 1]) == IntersectionType.PointIntersection)
        //                        {
        //                            if (lineIntersector.IntersectionPoints[0] != leftLine.P1)
        //                            {
        //                                cutInfos.Add(new CutInfo(i, lineIntersector.IntersectionPoints[0], prevLeftPointIndex < 0, CutInfo.CutEnum.Left));
        //                                leftCutting = true;
        //                            }
        //                        }

        //                        if (lineIntersector.ComputeIntersect(centerLine.P0, centerLine.P1, _polygons[_currentPolygonIndex].Shell.Coordinates[i], _polygons[_currentPolygonIndex].Shell.Coordinates[i + 1]) == IntersectionType.PointIntersection)
        //                        {
        //                            cutInfos.Add(new CutInfo(i, lineIntersector.IntersectionPoints[0], prevLeftPointIndex < 0 || prevRightPointIndex < 0, CutInfo.CutEnum.Center));
        //                        }

        //                        if (lineIntersector.ComputeIntersect(rightLine.P0, rightLine.P1, _polygons[_currentPolygonIndex].Shell.Coordinates[i], _polygons[_currentPolygonIndex].Shell.Coordinates[i + 1]) == IntersectionType.PointIntersection)
        //                        {
        //                            if (lineIntersector.IntersectionPoints[0] != rightLine.P0)
        //                            {
        //                                cutInfos.Add(new CutInfo(i, lineIntersector.IntersectionPoints[0], prevLeftPointIndex < 0, CutInfo.CutEnum.Right));
        //                                rightCutting = true;
        //                            }
        //                        }
        //                    }

        //                    if (cutInfos.Count > 0)
        //                    {
        //                        CheckOrderIndexes(ref cutInfos, _polygons[_currentPolygonIndex].Shell.Coordinates.Count - 1);
        //                        polygonHoleChanged = true;
        //                        if (!rightCutting && prevRightPointIndex > -1)
        //                        {
        //                            _polygons[_currentPolygonIndex].Shell.Coordinates.Insert(prevRightPointIndex + 1, rightPoint);
        //                            prevRightPointIndex++;
        //                            if (prevLeftPointIndex > -1)
        //                                prevLeftPointIndex++;
        //                            for (int i = 0; i < cutInfos.Count; i++)
        //                            {
        //                                if (cutInfos[i].Index > prevLeftPointIndex)
        //                                {
        //                                    CutInfo cutInfo = cutInfos[i];
        //                                    cutInfo.Index++;
        //                                    cutInfos[i] = cutInfo;
        //                                }
        //                            }
        //                        }

        //                        if (!leftCutting && prevLeftPointIndex > -1)
        //                        {
        //                            _polygons[_currentPolygonIndex].Shell.Coordinates.Insert(prevLeftPointIndex, leftPoint);
        //                            for (int i = 0; i < cutInfos.Count; i++)
        //                            {
        //                                if (cutInfos[i].Index > prevLeftPointIndex)
        //                                {
        //                                    CutInfo cutInfo = cutInfos[i];
        //                                    cutInfo.Index++;
        //                                    cutInfos[i] = cutInfo;
        //                                }
        //                            }
        //                        }

        //                        if (cutInfos.Count > 1)
        //                        {
        //                            IList<Coordinate[]> holes = new List<Coordinate[]>();
        //                            IList<Coordinate> holeOrPolygon1 = null;
        //                            IList<Coordinate> holeOrPolygon2 = null;
        //                            int prevCutOutIndex = -1;

        //                            for (int i = 0; i < cutInfos.Count; i++)
        //                            {
        //                                if (cutInfos[i].CuttingOut)
        //                                {
        //                                    if (prevCutOutIndex < 0)
        //                                        prevCutOutIndex = cutInfos[i].Index;
        //                                    else
        //                                    {

        //                                    }

        //                                }
        //                                else
        //                                {
        //                                    if (i == 0)
        //                                    {
        //                                        if (prevLeftPointIndex > 0)
        //                                        {
        //                                            holeOrPolygon1 = GetCoordsPosDirection(_polygons[_currentPolygonIndex].Shell.Coordinates, prevLeftPointIndex, cutInfos[0].Index);
        //                                            holeOrPolygon1.Add(cutInfos[0].Coordinate);
        //                                            holeOrPolygon1.Insert(0, cutInfos[0].Coordinate);
        //                                        }
        //                                        else
        //                                        {
        //                                            _polygons[_currentPolygonIndex].Shell.Coordinates.Insert(cutInfos[0].Index, cutInfos[0].Coordinate);
        //                                        }
        //                                    }
        //                                    else if (i == cutInfos.Count - 1)
        //                                    {
        //                                        if (prevRightPointIndex > 0)
        //                                        {
        //                                            holeOrPolygon2 = GetCoordsNegDirection(_polygons[_currentPolygonIndex].Shell.Coordinates, prevRightPointIndex, cutInfos[cutInfos.Count - 1].Index);
        //                                            holeOrPolygon2.Add(cutInfos[cutInfos.Count - 1].Coordinate);
        //                                            holeOrPolygon2.Insert(0, cutInfos[cutInfos.Count - 1].Coordinate);
        //                                        }
        //                                    }
        //                                    else
        //                                    {
        //                                        List<Coordinate> holeCoords = GetCoordsPosDirection(_polygons[_currentPolygonIndex].Shell.Coordinates, cutInfos[i].Index + 1, cutInfos[i + 1].Index - 1);
        //                                        holeCoords.Add(cutInfos[i + 1].Coordinate);
        //                                        holeCoords.Add(cutInfos[cutInfos.Count - 1].Coordinate);
        //                                        holeCoords.Insert(0, cutInfos[cutInfos.Count - 1].Coordinate);
        //                                        holes.Add(holeCoords.ToArray());
        //                                    }
        //                                }
        //                            }

        //                            if (!(holeOrPolygon1 == null || holeOrPolygon2 == null))
        //                            {
        //                                bool number1IsHole = CgAlgorithms.IsCounterClockwise(holeOrPolygon1);

        //                                LinearRing linearRing1 = new LinearRing(holeOrPolygon1);
        //                                LinearRing linearRing2 = new LinearRing(holeOrPolygon2);
        //                                List<LinearRing> linearRingHoles = new List<LinearRing>();
        //                                foreach (Coordinate[] hole in holes)
        //                                    linearRingHoles.Add(new LinearRing(hole));

        //                                if (!number1IsHole)
        //                                {
        //                                    linearRingHoles.Add(linearRing1);
        //                                    _polygons[_currentPolygonIndex].Shell.Coordinates = linearRing2.Coordinates;
        //                                }
        //                                else
        //                                {
        //                                    linearRing1.Reverse();
        //                                    linearRing2.Reverse();
        //                                    foreach (LinearRing linearRing in linearRingHoles)
        //                                        linearRing.Reverse();

        //                                    linearRingHoles.Add(linearRing2);
        //                                    _polygons[_currentPolygonIndex].Shell.Coordinates = linearRing1.Coordinates;
        //                                }

        //                                List<ILinearRing> polygonHoles = new List<ILinearRing>(_polygons[_currentPolygonIndex].Holes);
        //                                polygonHoles.AddRange(linearRingHoles);
        //                                _polygons[_currentPolygonIndex].Holes = polygonHoles.ToArray();
        //                            }
        //                            else
        //                            {

        //                            }
        //                        }
        //                        else
        //                        {
        //                            if (prevLeftPointIndex < 0)
        //                            {
        //                                Coordinate whereToInsert = _polygons[_currentPolygonIndex].Shell.Coordinates[cutInfos[0].Index];
        //                                List<Coordinate> toKeep = GetCoordsPosDirection(_polygons[_currentPolygonIndex].Shell.Coordinates, prevRightPointIndex, cutInfos[0].Index);
        //                                toKeep.Insert(toKeep.IndexOf(whereToInsert) + 1, cutInfos[0].Coordinate);
        //                                _polygons[_currentPolygonIndex].Shell.Coordinates = toKeep;
        //                                if (_polygons[_currentPolygonIndex].Shell.Coordinates[0] != _polygons[_currentPolygonIndex].Shell.Coordinates[_polygons[_currentPolygonIndex].Shell.Coordinates.Count - 1])
        //                                    _polygons[_currentPolygonIndex].Shell.Coordinates.Add(_polygons[_currentPolygonIndex].Shell.Coordinates[0]);
        //                            }
        //                            else
        //                            {
        //                                Coordinate whereToInsert = _polygons[_currentPolygonIndex].Shell.Coordinates[cutInfos[0].Index];
        //                                List<Coordinate> toKeep = GetCoordsPosDirection(_polygons[_currentPolygonIndex].Shell.Coordinates, prevLeftPointIndex, cutInfos[0].Index + 1);
        //                                toKeep.Insert(toKeep.IndexOf(whereToInsert) + 1, cutInfos[0].Coordinate);
        //                                _polygons[_currentPolygonIndex].Shell.Coordinates = toKeep;
        //                                if (_polygons[_currentPolygonIndex].Shell.Coordinates[0] != _polygons[_currentPolygonIndex].Shell.Coordinates[_polygons[_currentPolygonIndex].Shell.Coordinates.Count - 1])
        //                                    _polygons[_currentPolygonIndex].Shell.Coordinates.Add(_polygons[_currentPolygonIndex].Shell.Coordinates[0]);
        //                            }

        //                            if (_polygons[_currentPolygonIndex].Shell.Coordinates[0] != _polygons[_currentPolygonIndex].Shell.Coordinates[_polygons[_currentPolygonIndex].Shell.Coordinates.Count - 1])
        //                                _polygons[_currentPolygonIndex].Shell.Coordinates.Add(_polygons[_currentPolygonIndex].Shell.Coordinates[0]);
        //                        }
        //                    }
        //                    else if (prevLeftPointIndex > -1 && prevRightPointIndex > -1)
        //                    {
        //                        _polygons[_currentPolygonIndex].Shell.Coordinates.Insert(prevLeftPointIndex, leftPoint);
        //                        _polygons[_currentPolygonIndex].Shell.Coordinates.Insert(prevLeftPointIndex, rightPoint);
        //                    }

        //                    List<Coordinate> coords = new List<Coordinate>();
        //                    coords.Add(_prevRightPoint);
        //                    coords.Add(rightPoint);
        //                    coords.Add(leftPoint);
        //                    coords.Add(_prevLeftPoint);
        //                    LineString newRectangle = new LineString(coords);
        //                    OnPolygonUpdated(_currentPolygonIndex, newRectangle, polygonHoleChanged);
        //                }
        //                //else
        //                //throw new Exception("turning around itself");
        //            }
        //        }
        //        else
        //        {
        //            List<Coordinate> coords = new List<Coordinate>();
        //            coords.Add(_prevRightPoint);
        //            coords.Add(rightPoint);
        //            coords.Add(leftPoint);
        //            coords.Add(_prevLeftPoint);
        //            LineString rectangle = new LineString(coords);
        //            coords.Add(_prevRightPoint);
        //            LinearRing ring = new LinearRing(coords);
        //            Polygon polygon = new Polygon(ring);
        //            int id = 0;
        //            while (_polygons.Keys.Contains(id))
        //                id++;
        //            _polygons.Add(id, polygon);
        //            _currentPolygonIndex = id;
        //            OnPolygonUpdated(_currentPolygonIndex, rectangle, false);
        //        }
        //        _prevLeftPoint = leftPoint;
        //        _prevRightPoint = rightPoint;
        //    }
        //}
        #region split
        #endregion
        //public void AddTrackPoint(Coordinate leftPoint, Coordinate rightPoint)
        //{
        //    lock (_syncObject)
        //    {
        //        if (_currentPolygonIndex > -1 && _currentPolygonIndex < _polygons.Count)
        //        {
        //            if (_polygons[_currentPolygonIndex].Shell.Coordinates.Count == 0)
        //            {
        //                _polygons[_currentPolygonIndex].Shell.Coordinates.Add(_prevRightPoint);
        //                _polygons[_currentPolygonIndex].Shell.Coordinates.Add(rightPoint);
        //                _polygons[_currentPolygonIndex].Shell.Coordinates.Add(leftPoint);
        //                _polygons[_currentPolygonIndex].Shell.Coordinates.Add(_prevLeftPoint);
        //                _polygons[_currentPolygonIndex].Shell.Coordinates.Add(_prevRightPoint);
        //            }
        //            else
        //            {
        //                bool polygonHoleChanged = false;
        //                int prevRightPointIndex = _polygons[_currentPolygonIndex].Shell.Coordinates.IndexOf(_prevRightPoint);
        //                int prevLeftPointIndex = _polygons[_currentPolygonIndex].Shell.Coordinates.IndexOf(_prevLeftPoint);

        //                ILineString rectangle = new LineString(new Coordinate[4]{_prevRightPoint,
        //                                                                    rightPoint,
        //                                                                    leftPoint,
        //                                                                    _prevLeftPoint});

        //                bool rightPointInside = CgAlgorithms.IsPointInRing(rightPoint, _polygons[_currentPolygonIndex].Shell.Coordinates);
        //                bool leftPointInside = CgAlgorithms.IsPointInRing(leftPoint, _polygons[_currentPolygonIndex].Shell.Coordinates);

        //                if (!(rightPointInside && _prevRightPointInside && leftPointInside && _prevLeftPointInside))
        //                {
        //                    if (!rightPointInside && !_prevRightPointInside && !leftPointInside && !_prevLeftPointInside)
        //                    {
        //                        _polygons[_currentPolygonIndex].Shell.Coordinates.Insert(prevLeftPointIndex, leftPoint);
        //                        _polygons[_currentPolygonIndex].Shell.Coordinates.Insert(prevLeftPointIndex, rightPoint);
        //                    }
        //                    else
        //                    {
        //                        polygonHoleChanged = true;
        //                        IGeometry intersectionsGeometry = _polygons[_currentPolygonIndex].Intersection(rectangle);
        //                        IList<Coordinate> intersections = new List<Coordinate>(intersectionsGeometry.Coordinates);
        //                        intersections.Remove(_prevRightPoint);
        //                        intersections.Remove(_prevLeftPoint);
        //                        intersections.Remove(leftPoint);
        //                        intersections.Remove(rightPoint);

        //                        if(intersections.Count % 2 > 0)
        //                            throw new Exception("Intersection count is odd");

        //                        if(intersections.Count < 2)
        //                            throw new Exception("Intersections is less than two");

        //                        //if (!_prevLeftPointInside && !leftPointInside)
        //                        //    _polygons[_currentPolygonIndex].Shell.Coordinates.Insert(prevLeftPointIndex, leftPoint);

        //                        //if (!_prevRightPointInside && !rightPointInside)
        //                        //    _polygons[_currentPolygonIndex].Shell.Coordinates.Insert(prevRightPointIndex + 1, rightPoint);

        //                        int firstindex = -1;
        //                        int secondindex = -1;
        //                        for(int i = 0; i < _polygons[_currentPolygonIndex].Shell.Coordinates.Count - 1; i++)
        //                        {
        //                            LineString line = new LineString(new Coordinate[] { _polygons[_currentPolygonIndex].Shell.Coordinates[i], _polygons[_currentPolygonIndex].Shell.Coordinates[i + 1] });
        //                            bool firstIntersect = line.Intersects(intersections[0].X, intersections[0].Y);
        //                            bool secondIntersect = line.Intersects(intersections[intersections.Count - 1].X, intersections[intersections.Count - 1].Y);
                                    
        //                            if(firstIntersect && firstindex > -1)
        //                                firstindex = i;
        //                            else if(firstIntersect)
        //                                secondindex = i;

        //                            if(secondIntersect && firstindex > -1)
        //                                firstindex = i;
        //                            else if(secondIntersect)
        //                                secondindex = i;

        //                            if(firstindex > -1 && secondindex > -1)
        //                            {
        //                                if(firstindex > secondindex)
        //                                {
        //                                    int tempIndex = secondindex;
        //                                    secondindex = firstindex;
        //                                    firstindex = tempIndex;
        //                                }
        //                                break;
        //                            }
        //                        }

        //                        IList<Coordinate> newShell;
        //                        IList<Coordinate> newHole;
        //                        if(firstindex > prevRightPointIndex)
        //                        {
        //                            newHole = GetCoordsNegDirection(_polygons[_currentPolygonIndex].Shell.Coordinates, firstindex, prevLeftPointIndex);
        //                            if(!_prevLeftPointInside && !leftPointInside)
        //                                newHole.Add(leftPoint);
        //                            newHole.Add(intersections[0]);

        //                            newShell = GetCoordsPosDirection(_polygons[_currentPolygonIndex].Shell.Coordinates, secondindex + 1, prevRightPointIndex);
        //                            if(!_prevRightPointInside && !rightPointInside)
        //                                newShell.Add(rightPoint);
        //                            newShell.Add(intersections[intersections.Count - 1]);
        //                        }
        //                        else
        //                        {
        //                            newHole = GetCoordsNegDirection(_polygons[_currentPolygonIndex].Shell.Coordinates, prevRightPointIndex, secondindex + 1);
        //                            if(!_prevRightPointInside && !rightPointInside)
        //                                newHole.Add(rightPoint);
        //                            newHole.Add(intersections[intersections.Count - 1]);

        //                            newShell = GetCoordsPosDirection(_polygons[_currentPolygonIndex].Shell.Coordinates, prevLeftPointIndex, firstindex);
        //                            if(!_prevLeftPointInside && !leftPointInside)
        //                                newShell.Add(leftPoint);
        //                            newShell.Add(intersections[0]);
        //                        }

        //                        if (newHole[0] != newHole[newHole.Count - 1])
        //                            newHole.Add(newHole[0]);

        //                        if (newShell[0] != newShell[newShell.Count - 1])
        //                            newShell.Add(newShell[0]);

        //                        _polygons[_currentPolygonIndex].Shell.Coordinates = newHole;
        //                        List<ILinearRing> holes = new List<ILinearRing>(_polygons[_currentPolygonIndex].Holes);
        //                        holes.Add(new LinearRing(newHole));
        //                        _polygons[_currentPolygonIndex].Holes = holes.ToArray();
        //                    }

        //                    List<Coordinate> coords = new List<Coordinate>();
        //                    coords.Add(_prevRightPoint);
        //                    coords.Add(rightPoint);
        //                    coords.Add(leftPoint);
        //                    coords.Add(_prevLeftPoint);
        //                    LineString newRectangle = new LineString(coords);
        //                    OnPolygonUpdated(_currentPolygonIndex, newRectangle, polygonHoleChanged);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            List<Coordinate> coords = new List<Coordinate>();
        //            coords.Add(_prevRightPoint);
        //            coords.Add(rightPoint);
        //            coords.Add(leftPoint);
        //            coords.Add(_prevLeftPoint);
        //            LineString rectangle = new LineString(coords);
        //            coords.Add(_prevRightPoint);
        //            LinearRing ring = new LinearRing(coords);
        //            Polygon polygon = new Polygon(ring);
        //            int id = 0;
        //            while (_polygons.Keys.Contains(id))
        //                id++;
        //            _polygons.Add(id, polygon);
        //            _currentPolygonIndex = id;
        //            OnPolygonUpdated(_currentPolygonIndex, rectangle, false);
        //        }
        //        _prevLeftPoint = leftPoint;
        //        _prevRightPoint = rightPoint;
        //    }
        //}

        public void InitTrack(Coordinate initLeftPoint, Coordinate initRightPoint)
        {
            lock (_syncObject)
            {
                _prevLeftPoint = initLeftPoint;
                _prevRightPoint = initRightPoint;
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
            if (hole.Coordinates.Count != 4)
                return true;

            if (HelperClassCoordinate.CoordinateEqualsRoundedmm(hole.Coordinates[0], hole.Coordinates[3]) && HelperClassCoordinate.CoordinateEqualsRoundedmm(hole.Coordinates[1], hole.Coordinates[2]))
                return false;

            if (hole.Area < 0.01)
                return false;

            return true;
        }

        private List<Coordinate> GetCoordsNegDirection(IList<Coordinate> coords, int startIndex, int endIndex)
        {
            int i = startIndex;
            List<Coordinate> newCoords = new List<Coordinate>();
            newCoords.Add(coords[i]);
            do
            {
                i--;
                if (i < 0)
                    i = coords.Count - 1;

                if (!newCoords.Contains(coords[i]))
                    newCoords.Add(coords[i]);
            } while (i != endIndex);

            return newCoords;
        }

        private List<Coordinate> GetCoordsPosDirection(IList<Coordinate> coords, int startIndex, int endIndex)
        {
            int i = startIndex;
            List<Coordinate> newCoords = new List<Coordinate>();
            newCoords.Add(coords[i]);
            do
            {
                i++;
                if (i > coords.Count - 1)
                    i = 0;

                if (!newCoords.Contains(coords[i]))
                    newCoords.Add(coords[i]);
            } while (i != endIndex);

            return newCoords;
        }

        private List<Coordinate> GetCoordsBothWay(IList<Coordinate> coords, int startIndex, int forwardCount, int backwardCount)
        {
            List<Coordinate> newList = new List<Coordinate>();
            int indexInList = startIndex - backwardCount;
            while(indexInList < 0)
                indexInList = coords.Count + indexInList;
            for(int i = 0; i < forwardCount + backwardCount + 1; i++)
            {
                if (indexInList > coords.Count - 1)
                    indexInList = 0;
                newList.Add(coords[indexInList]);
                indexInList++;
            }
            return newList;
        }
        
        #endregion
    }
}
