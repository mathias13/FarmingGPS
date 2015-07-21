using System;
using System.Collections.Generic;
using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;

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
                        ILineString rectangle = new LineString(new Coordinate[5]{_prevRightPoint,
                                                                            rightPoint,
                                                                            leftPoint,
                                                                            _prevLeftPoint,
                                                                            _prevRightPoint});

                        bool polygonHoleChanged = false;
                        int prevRightPointIndex = _polygons[_currentPolygonIndex].Shell.Coordinates.IndexOf(_prevRightPoint);
                        int prevLeftPointIndex = _polygons[_currentPolygonIndex].Shell.Coordinates.IndexOf(_prevLeftPoint);

                        IGeometry rectPolygon = new Polygon(rectangle.Coordinates);
                        if(_polygons[_currentPolygonIndex].Holes.Length > 0)
                        {
                            List<ILinearRing> holes = new List<ILinearRing>(_polygons[_currentPolygonIndex].Holes);
                            List<ILinearRing> holesToAdd = new List<ILinearRing>();
                            for (int i = 0; i < holes.Count; i++)
                            {
                                Polygon hole = new Polygon(holes[i]);
                                if (!rectPolygon.Intersects(hole))
                                    continue;
                                IGeometry diff = hole.Difference(rectPolygon);
                                if (diff is MultiPolygon)
                                {
                                    holes.RemoveAt(i);
                                    i--;
                                    MultiPolygon multiPoly = diff as MultiPolygon;
                                    holesToAdd.Add((multiPoly[0] as Polygon).Shell);
                                    holesToAdd.Add((multiPoly[1] as Polygon).Shell);
                                }
                                else if (diff is Polygon)
                                    holes[i] = new LinearRing((diff as Polygon).Shell);
                                else if (diff.IsEmpty)
                                {
                                    holes.RemoveAt(i);
                                    i--;
                                }
                                else
                                    throw new Exception("Invalid hole");
                            }
                            holes.AddRange(holesToAdd);
                            _polygons[_currentPolygonIndex].Holes = holes.ToArray();
                            //TODO Remove polygonHoleChanged
                            polygonHoleChanged = true;
                            OnPolygonUpdated(_currentPolygonIndex, new LineString(new Coordinate[0]), polygonHoleChanged);
                        }
                        if (!rectangle.IsSimple)
                        {
                            SimplePointInRing pointInRing = new SimplePointInRing(_polygons[_currentPolygonIndex].Shell);
                            if (!pointInRing.IsInside(leftPoint))
                            {
                                List<Coordinate> lineString = new List<Coordinate>();
                                lineString.Add(rightPoint);
                                lineString.Add(leftPoint);
                                lineString.Add(_prevLeftPoint);
                                rectangle = new LineString(lineString);
                            }
                            else if (!pointInRing.IsInside(rightPoint))
                            {
                                List<Coordinate> lineString = new List<Coordinate>();
                                lineString.Add(_prevRightPoint);
                                lineString.Add(rightPoint);
                                lineString.Add(leftPoint);
                                rectangle = new LineString(lineString);
                            }
                            rectPolygon = new Polygon(rectangle.Coordinates);
                        }                            
                        
                        IGeometry rectDiffGeometry = (IGeometry)rectPolygon.Difference(new Polygon(_polygons[_currentPolygonIndex].Shell));

                        if (rectDiffGeometry.Coordinates.Count < 1)
                            return;

                        List<Coordinate> coords = new List<Coordinate>(rectDiffGeometry.Coordinates);
                        if (coords[0] == coords[coords.Count - 1])
                            coords.RemoveAt(0);

                        List<Coordinate> rectDiff = new List<Coordinate>(rectDiffGeometry.Coordinates);
                        List<Coordinate> rightPoly = new List<Coordinate>();
                        List<Coordinate> leftPoly = new List<Coordinate>();
                        if(rectDiffGeometry is MultiPolygon)
                        {
                            MultiPolygon multiPoly = rectDiffGeometry as MultiPolygon;
                            if(multiPoly[0].Coordinates.Contains(rightPoint))
                            {
                                rightPoly.AddRange(multiPoly[0].Coordinates);
                                leftPoly.AddRange(multiPoly[1].Coordinates);
                            }
                            else
                            {
                                rightPoly.AddRange(multiPoly[1].Coordinates);
                                leftPoly.AddRange(multiPoly[0].Coordinates);
                            }
                            if (!CgAlgorithms.IsCounterClockwise(rightPoly))
                                rightPoly.Reverse();
                            if (!CgAlgorithms.IsCounterClockwise(leftPoly))
                                leftPoly.Reverse();
                        }

                        rectDiff.RemoveAt(0);

                        List<Coordinate> intersectionCoords = new List<Coordinate>(rectDiff);
                        intersectionCoords.Remove(_prevRightPoint);
                        intersectionCoords.Remove(rightPoint);
                        intersectionCoords.Remove(leftPoint);
                        intersectionCoords.Remove(_prevLeftPoint);

                        int intersections = intersectionCoords.Count;

                        if (intersections > 0)
                        {
                            if (!CgAlgorithms.IsCounterClockwise(rectDiff))
                                rectDiff.Reverse();
                            IGeometry rectPolygonGeometry = _polygons[_currentPolygonIndex].Shell.Difference(rectPolygon);
                            List<Coordinate> polygonDiff = new List<Coordinate>(rectPolygonGeometry.Coordinates);
                            if (!CgAlgorithms.IsCounterClockwise(polygonDiff))
                                polygonDiff.Reverse();
                            int prevRightPointIndexPoly = polygonDiff.IndexOf(_prevRightPoint);
                            int prevLeftPointIndexPoly = polygonDiff.IndexOf(_prevLeftPoint);
                            int prevRightPointIndexRect = rectDiff.IndexOf(_prevRightPoint);
                            int prevLeftPointIndexRect = rectDiff.IndexOf(_prevLeftPoint);
                            int rightPointIndecRect = rectDiff.IndexOf(rightPoint);
                            int leftPointIndexRect = rectDiff.IndexOf(leftPoint);
                            if (prevLeftPointIndexRect != -1 && prevRightPointIndexRect != -1 && (rightPointIndecRect == -1 || leftPointIndexRect == -1)) //Check if we are previously outside and are going to create a hole
                            {
                                polygonHoleChanged = true;
                                List<Coordinate> rightCoords = new List<Coordinate>();
                                Coordinate rightCut = Coordinate.Empty;
                                List<Coordinate> leftCoords = new List<Coordinate>();
                                Coordinate leftCut = Coordinate.Empty;
                                for (int i = prevRightPointIndexRect + 1; i != prevRightPointIndexRect; i++)
                                {
                                    if (i > rectDiff.Count - 1)
                                        i = 0;

                                    if (rectDiff[i] != rightPoint && rectDiff[i] != leftPoint)
                                    {
                                        rightCut = rectDiff[i];
                                        break;
                                    }
                                    rightCoords.Add(rectDiff[i]);
                                }
                                for (int i = prevLeftPointIndexRect - 1; i != prevLeftPointIndexRect; i--)
                                {
                                    if (i < 0)
                                        i = rectDiff.Count - 1;

                                    if (rectDiff[i] != rightPoint && rectDiff[i] != leftPoint)
                                    {
                                        leftCut = rectDiff[i];
                                        break;
                                    }
                                    leftCoords.Add(rectDiff[i]);
                                }

                                List<Coordinate> rightPolygon = GetCoordsNegDirection(polygonDiff,
                                                                                        polygonDiff.IndexOf(_prevRightPoint),
                                                                                        polygonDiff.IndexOf(rightCut));

                                List<Coordinate> leftPolygon = GetCoordsPosDirection(polygonDiff,
                                                                                        polygonDiff.IndexOf(_prevLeftPoint),
                                                                                        polygonDiff.IndexOf(leftCut));

                                bool rightIsHole = true;
                                if (!CgAlgorithms.IsCounterClockwise(rightPolygon))
                                {
                                    rightIsHole = false;
                                    rightPolygon.Reverse();
                                    leftPolygon.Reverse();
                                    rightCoords.Reverse();
                                    leftCoords.Reverse();
                                }

                                for (int i = 0; i < rightCoords.Count; i++)
                                    rightPolygon.Insert(0, rightCoords[i]);

                                for (int i = 0; i < leftCoords.Count; i++)
                                    leftPolygon.Insert(0, leftCoords[i]);

                                if (rightPolygon[0] != rightPolygon[rightPolygon.Count - 1])
                                    rightPolygon.Add(rightPolygon[0]);

                                if (leftPolygon[0] != leftPolygon[leftPolygon.Count - 1])
                                    leftPolygon.Add(leftPolygon[0]);

                                if (rightIsHole)
                                {
                                    _polygons[_currentPolygonIndex].Shell = new LinearRing(leftPolygon);
                                    List<ILinearRing> holes = new List<ILinearRing>(_polygons[_currentPolygonIndex].Holes);
                                    holes.Add(new LinearRing(rightPolygon));
                                    _polygons[_currentPolygonIndex].Holes = holes.ToArray();
                                }
                                else
                                {
                                    _polygons[_currentPolygonIndex].Shell = new LinearRing(rightPolygon);
                                    List<ILinearRing> holes = new List<ILinearRing>(_polygons[_currentPolygonIndex].Holes);
                                    holes.Add(new LinearRing(leftPolygon));
                                    _polygons[_currentPolygonIndex].Holes = holes.ToArray();
                                }

                                CSVCoordinates csvRectDiff = new CSVCoordinates(rectDiff);
                                CSVCoordinates csvLeftPolygon = new CSVCoordinates(leftPolygon);
                                CSVCoordinates csvRightPolygon = new CSVCoordinates(rightPolygon);
                                CSVCoordinates csvPolygonDiff = new CSVCoordinates(polygonDiff);
                                System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(@"C:\Binaries\positions.csv");
                                streamWriter.Write(DateTime.Now.ToString("hh:mm:ss") + Environment.NewLine);
                                streamWriter.Write(csvRectDiff.ToString());
                                streamWriter.Write("1" + Environment.NewLine);
                                streamWriter.Write(csvLeftPolygon.ToString());
                                streamWriter.Write("2" + Environment.NewLine);
                                streamWriter.Write(csvRightPolygon.ToString());
                                streamWriter.Write("3" + Environment.NewLine);
                                streamWriter.Write(csvPolygonDiff.ToString());
                                streamWriter.Flush();
                                streamWriter.Dispose();
                            }
                            else if (prevLeftPointIndexRect != -1 && prevRightPointIndexRect == -1)
                            {
                                List<Coordinate> leftCoords = new List<Coordinate>();
                                Coordinate leftCut = Coordinate.Empty;
                                for (int i = prevLeftPointIndexRect - 1; i != prevLeftPointIndexRect; i--)
                                {
                                    if (i < 0)
                                        i = rectDiff.Count - 1;

                                    if (rectDiff[i] != leftPoint && rectDiff[i] != rightPoint)
                                    {
                                        leftCut = rectDiff[i];
                                        break;
                                    }
                                    leftCoords.Add(rectDiff[i]);
                                }
                                List<Coordinate> leftPolygon = GetCoordsPosDirection(polygonDiff,
                                                                                        polygonDiff.IndexOf(_prevLeftPoint),
                                                                                        polygonDiff.IndexOf(leftCut));

                                if (!CgAlgorithms.IsCounterClockwise(leftPolygon))
                                {
                                    leftPolygon.Reverse();
                                    leftCoords.Reverse();
                                }

                                for (int i = 0; i < leftCoords.Count; i++)
                                    leftPolygon.Insert(0, leftCoords[i]);

                                if (leftPolygon[0] != leftPolygon[leftPolygon.Count - 1])
                                    leftPolygon.Add(leftPolygon[0]);

                                _polygons[_currentPolygonIndex].Shell = new LinearRing(leftPolygon);

                                CSVCoordinates csvRectDiff = new CSVCoordinates(rectDiff);
                                CSVCoordinates csvLeftPolygon = new CSVCoordinates(leftPolygon);
                                CSVCoordinates csvPolygonDiff = new CSVCoordinates(polygonDiff);
                                System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(@"C:\Binaries\positions.csv");
                                streamWriter.Write(DateTime.Now.ToString("hh:mm:ss") + Environment.NewLine);
                                streamWriter.Write(csvRectDiff.ToString());
                                streamWriter.Write("1" + Environment.NewLine);
                                streamWriter.Write(csvLeftPolygon.ToString());
                                streamWriter.Write("2" + Environment.NewLine);
                                streamWriter.Write(csvPolygonDiff.ToString());
                                streamWriter.Flush();
                                streamWriter.Dispose();
                            }
                            else if (prevRightPointIndexRect != -1 && prevLeftPointIndexRect == 1)
                            {
                                List<Coordinate> rightCoords = new List<Coordinate>();
                                Coordinate rightCut = Coordinate.Empty;
                                for (int i = prevRightPointIndexRect + 1; i != prevRightPointIndexRect; i++)
                                {
                                    if (i > rectDiff.Count - 1)
                                        i = 0;

                                    if (rectDiff[i] != rightPoint && rectDiff[i] != leftPoint)
                                    {
                                        rightCut = rectDiff[i];
                                        break;
                                    }
                                    rightCoords.Add(rectDiff[i]);
                                }
                                List<Coordinate> rightPolygon = GetCoordsNegDirection(polygonDiff,
                                                                                        polygonDiff.IndexOf(_prevRightPoint),
                                                                                        polygonDiff.IndexOf(rightCut));

                                if (!CgAlgorithms.IsCounterClockwise(rightPolygon))
                                {
                                    rightPolygon.Reverse();
                                    rightCoords.Reverse();
                                }

                                for (int i = 0; i < rightCoords.Count; i++)
                                    rightPolygon.Insert(0, rightCoords[i]);

                                if (rightPolygon[0] != rightPolygon[rightPolygon.Count - 1])
                                    rightPolygon.Add(rightPolygon[0]);

                                _polygons[_currentPolygonIndex].Shell = new LinearRing(rightPolygon);

                                CSVCoordinates csvRectDiff = new CSVCoordinates(rectDiff);
                                CSVCoordinates csvRightPolygon = new CSVCoordinates(rightPolygon);
                                CSVCoordinates csvPolygonDiff = new CSVCoordinates(polygonDiff);
                                System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(@"C:\Binaries\positions.csv");
                                streamWriter.Write(DateTime.Now.ToString("hh:mm:ss") + Environment.NewLine);
                                streamWriter.Write(csvRectDiff.ToString());
                                streamWriter.Write("1" + Environment.NewLine);
                                streamWriter.Write(csvRightPolygon.ToString());
                                streamWriter.Write("2" + Environment.NewLine);
                                streamWriter.Write(csvPolygonDiff.ToString());
                                streamWriter.Flush();
                                streamWriter.Dispose();
                            }
                            else
                            {
                                int rightPointIndex = rightPoly.Count > 0 ? rightPoly.IndexOf(rightPoint) : rectDiff.IndexOf(rightPoint);
                                int leftPointIndex = leftPoly.Count > 0 ? leftPoly.IndexOf(leftPoint) : rectDiff.IndexOf(leftPoint);
                                List<Coordinate> rightCoords = new List<Coordinate>();
                                List<Coordinate> leftCoords = new List<Coordinate>();
                                if (rightPointIndex == leftPointIndex - 1 || (leftPointIndex == 0 && rightPointIndex == rectDiff.Count - 1))
                                {
                                    rightCoords = GetCoordsBothWay(rectDiff, rightPointIndex, 2, 1);
                                }
                                else
                                {
                                    if (rightPointIndex != -1)
                                        rightCoords = GetCoordsBothWay(rightPoly.Count > 0 ? rightPoly : rectDiff, rightPointIndex, 1, 1);

                                    if (leftPointIndex != -1)
                                        leftCoords = GetCoordsBothWay(leftPoly.Count > 0 ? leftPoly : rectDiff, leftPointIndex, 1, 1);
                                }

                                List<Coordinate> newPoly = new List<Coordinate>();
                                if (rightCoords.Count > 0 && leftCoords.Count > 0)
                                {
                                    List<Coordinate> bigRing = GetCoordsPosDirection(polygonDiff, polygonDiff.IndexOf(leftCoords[leftCoords.Count - 1]), polygonDiff.IndexOf(rightCoords[0]));
                                    List<Coordinate> smallRing = GetCoordsPosDirection(polygonDiff, polygonDiff.IndexOf(rightCoords[rightCoords.Count - 1]), polygonDiff.IndexOf(leftCoords[0]));
                                    newPoly.AddRange(bigRing);
                                    newPoly.Add(rightPoint);
                                    newPoly.AddRange(smallRing);
                                    newPoly.Add(leftPoint);
                                    CSVCoordinates csvBigRing = new CSVCoordinates(bigRing);
                                    CSVCoordinates csvRightPoint = new CSVCoordinates(new Coordinate[] { rightPoint });
                                    CSVCoordinates csvSmallRing = new CSVCoordinates(smallRing);
                                    CSVCoordinates csvLeftPoint = new CSVCoordinates(new Coordinate[] { leftPoint });
                                    System.IO.StreamWriter streamWriter1 = new System.IO.StreamWriter(@"C:\Binaries\positions.csv");
                                    streamWriter1.Write(DateTime.Now.ToString("hh:mm:ss") + Environment.NewLine);
                                    streamWriter1.Write(csvBigRing.ToString());
                                    streamWriter1.Write("1" + Environment.NewLine);
                                    streamWriter1.Write(csvRightPoint.ToString());
                                    streamWriter1.Write("2" + Environment.NewLine);
                                    streamWriter1.Write(csvSmallRing.ToString());
                                    streamWriter1.Write("3" + Environment.NewLine);
                                    streamWriter1.Write(csvLeftPoint.ToString());
                                    streamWriter1.Flush();
                                    streamWriter1.Dispose();
                                }
                                else if (rightCoords.Count > 0)
                                {
                                    newPoly = GetCoordsPosDirection(polygonDiff, polygonDiff.IndexOf(rightCoords[rightCoords.Count - 1]), polygonDiff.IndexOf(rightCoords[0]));
                                    newPoly.Add(rightPoint);
                                    if (leftPointIndex != -1)
                                        newPoly.Add(leftPoint);
                                }
                                else if (leftCoords.Count > 0)
                                {
                                    newPoly = GetCoordsPosDirection(polygonDiff, polygonDiff.IndexOf(leftCoords[leftCoords.Count - 1]), polygonDiff.IndexOf(leftCoords[0]));
                                    newPoly.Add(leftPoint);
                                }

                                if (newPoly[0] != newPoly[newPoly.Count - 1])
                                    newPoly.Add(newPoly[0]);

                                _polygons[_currentPolygonIndex].Shell = new LinearRing(newPoly);
                                
                                CSVCoordinates csvNewPoly = new CSVCoordinates(newPoly);
                                CSVCoordinates csvLeftCoords = new CSVCoordinates(leftCoords);
                                CSVCoordinates csvRightCoords = new CSVCoordinates(rightCoords);
                                System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(@"C:\Binaries\positions.csv");
                                streamWriter.Write(DateTime.Now.ToString("hh:mm:ss") + Environment.NewLine);
                                streamWriter.Write(csvNewPoly.ToString());
                                streamWriter.Write("1" + Environment.NewLine);
                                streamWriter.Write(csvLeftCoords.ToString());
                                streamWriter.Write("2" + Environment.NewLine);
                                streamWriter.Write(csvRightCoords.ToString());
                                streamWriter.Flush();
                                streamWriter.Dispose();
                            }
                        }
                        else if (prevLeftPointIndex == -1 && prevRightPointIndex == -1)
                            return;
                        else
                        {
                            _polygons[_currentPolygonIndex].Shell.Coordinates.Insert(prevLeftPointIndex, leftPoint);
                            _polygons[_currentPolygonIndex].Shell.Coordinates.Insert(prevLeftPointIndex, rightPoint);
                        }

                        //TODO Remove this
                        polygonHoleChanged = true;
                        OnPolygonUpdated(_currentPolygonIndex, new LineString(coords), polygonHoleChanged);
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
