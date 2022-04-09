using DotSpatial.Positioning;
using FarmingGPSLib.Equipment;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
using GeoAPI.Geometries;
using GpsUtilities.HelperClasses;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.GeometriesGraph;
using NetTopologySuite.Operation.Buffer;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FarmingGPSLib.FarmingModes
{
    public enum FarmingMode
    {
        Plowing,
        GeneralHarrowing,
        Fertilizing,

    }

    public abstract class FarmingModeBase : IFarmingMode
    {
        //TODO: Delete this struct if it's possible to serialize Coordinate
        [Serializable]
        public struct SimpleCoordinate
        {
            public double X;

            public double Y;

            public double Z;
        }

        [Serializable]
        public struct SimpleLine
        {
            public SimpleCoordinate[] Line;

            [System.Xml.Serialization.XmlIgnore]
            public Coordinate[] LineCoordinateArray
            {
                get
                {
                    var coords = new List<Coordinate>();
                    foreach (var coord in Line)
                        coords.Add(new Coordinate(coord.X, coord.Y, coord.Z));
                    return coords.ToArray();
                }
            }

            public SimpleLine(Coordinate[] line)
            {
                var coords = new List<SimpleCoordinate>();
                foreach (var coord in line)
                    coords.Add(new SimpleCoordinate() { X = coord.X, Y = coord.Y, Z = coord.Z });
                Line = coords.ToArray();
            }
        }

        [Serializable]
        public struct FarmingModeState
        {
            public List<SimpleLine> TrackingLines;

            public List<SimpleLine> TrackingLinesHeadLand;

            public bool TrackingLineBackwards;

            public FarmingModeState(List<SimpleLine> trackingLines, List<SimpleLine> trackingLinesHeadLand, bool trackingLineBackwards)
            {
                TrackingLines = trackingLines;
                TrackingLinesHeadLand = trackingLinesHeadLand;
                TrackingLineBackwards = trackingLineBackwards;
            }
        }

        #region Private Variables

        protected Polygon _fieldPolygon;

        protected IList<TrackingLine> _trackingLinesHeadland = new List<TrackingLine>();

        protected IList<TrackingLine> _trackingLines = new List<TrackingLine>();

        protected IEquipment _equipment;

        protected int _headlandTurns;

        protected bool _trackingLineBackwards = false;

        private bool _hasChanged = true;

        #endregion

        public FarmingModeBase()
        {
        }

        public FarmingModeBase(IField field)
        {
            _fieldPolygon = field.Polygon;
        }

        public FarmingModeBase(IField field, IEquipment equipment, int headLandTurns) : this(field)
        {
            _equipment = equipment;
            _headlandTurns = headLandTurns;
            CalculateHeadLand();
        }
        public FarmingModeBase(IField field, IEquipment equipment, Distance headlandDistance) : this(field)
        {
            double distance = headlandDistance.ToMeters().Value;
            double equipmentWidth = equipment.WidthOverlap.ToMeters().Value;
            int headLandTurns = 0;
            while ((headLandTurns * equipmentWidth) < distance)
                headLandTurns++;

            _equipment = equipment;
            _headlandTurns = headLandTurns;
            CalculateHeadLand();
        }

        #region private Methods

        protected virtual void CalculateHeadLand()
        {
        }

        protected virtual void AddTrackingLines(IList<LineString> trackingLines)
        {
            _trackingLines.Clear();
            foreach (LineString line in trackingLines)
                _trackingLines.Add(new TrackingLine(line, true));
        }

        protected IList<ILineString> GetHeadLandCoordinates(double distance, Coordinate[] ring)
        {
            OffsetCurveBuilder curveBuilder = new OffsetCurveBuilder(new PrecisionModel(PrecisionModels.Floating), new BufferParameters());
            Coordinate[] coords = curveBuilder.GetRingCurve(ring, Positions.Left, distance);
            LineString newRing = new LineString(coords);

            IList<ILineString> rings = TestRing(newRing);

            return rings;
        }

        protected IList<ILineString> GetHeadLandCoordinates(double distance)
        {
            return GetHeadLandCoordinates(distance, _fieldPolygon.Shell.Coordinates);
        }

        protected IList<ILineString> GetHeadLandCoordinates(double distance, DotSpatial.NTSExtension.Angle[] angleConstraintsLeft, DotSpatial.NTSExtension.Angle[] angleConstraintRight)
        {
            return GetHeadLandCoordinates(distance, _fieldPolygon.Shell.Coordinates, angleConstraintsLeft, angleConstraintRight);
        }

        protected IList<ILineString> GetHeadLandCoordinates(double distance, Coordinate[] ring, DotSpatial.NTSExtension.Angle[] angleConstraintsLeft, DotSpatial.NTSExtension.Angle[] angleConstraintRight)
        {
            var newCoords = new List<Coordinate>();
            var lines = HelperClassLines.CreateLines(ring);
            newCoords.Add(lines[0].P0);
            foreach (var line in lines)
            {
                bool offSegment = false;
                for (int i = 0; i < angleConstraintsLeft.Length; i++)
                {
                    if (HelperClassAngles.AngleBetween(new DotSpatial.NTSExtension.Angle(line.Angle), angleConstraintsLeft[i], angleConstraintRight[i]))
                    {
                        var newLine = HelperClassLines.ComputeOffsetSegment(line, Positions.Left, distance);
                        newCoords.Add(newLine.P0);
                        newCoords.Add(newLine.P1);
                        offSegment = true;
                        break;
                    }
                }
                if (!offSegment)
                {
                    if (line.P0 != newCoords[newCoords.Count - 1])
                        newCoords.Add(line.P0);
                    newCoords.Add(line.P1);
                }
            }

            if (newCoords[0] != newCoords[newCoords.Count - 1])
                newCoords.Add(newCoords[0]);

            
            return TestRing(new LineString(newCoords.ToArray()));
        }

        protected IList<LineString> GetHeadlandLines(double distance)
        {
            IList<ILineString> newRing = GetHeadLandCoordinates(distance);
            List<LineString> lines = new List<LineString>();

            foreach (ILineString ring in newRing)
            {
                IList<LineSegment> lineSegments = HelperClassLines.CreateLines(ring.Coordinates);
                List<Coordinate> coordinates = new List<Coordinate>();
                foreach(LineSegment segment in lineSegments)
                {
                    coordinates.Add(segment.P0);
                    if (segment.Length < 10.0)
                        continue;
                    coordinates.Add(segment.P1);
                    lines.Add(new LineString(coordinates.ToArray()));
                    coordinates.Clear();
                }
                if(coordinates.Count > 0)
                {
                    coordinates.Add(lineSegments[lineSegments.Count - 1].P1);
                    lines.Add(new LineString(coordinates.ToArray()));
                }
            }
            
            return lines;
        }

        protected IList<LineString> GetHeadlandLines(double distance, DotSpatial.NTSExtension.Angle[] angleConstraintsLeft, DotSpatial.NTSExtension.Angle[] angleConstraintRight)
        {
            IList<ILineString> newRing = GetHeadLandCoordinates(distance, angleConstraintsLeft, angleConstraintRight);
            List<LineString> lines = new List<LineString>();

            foreach (ILineString ring in newRing)
            {
                IList<LineSegment> lineSegments = HelperClassLines.CreateLines(ring.Coordinates);
                List<Coordinate> coordinates = new List<Coordinate>();
                foreach (LineSegment segment in lineSegments)
                {
                    bool angleOK = false;
                    for (int i = 0; i < angleConstraintsLeft.Length && !angleOK; i++)
                        angleOK = HelperClassAngles.AngleBetween(new DotSpatial.NTSExtension.Angle(segment.Angle), angleConstraintsLeft[i], angleConstraintRight[i]);

                    if (angleOK)
                    {
                        coordinates.Add(segment.P0);
                        if (segment.Length < 10.0)
                            continue;
                        coordinates.Add(segment.P1);
                        lines.Add(new LineString(coordinates.ToArray()));
                        coordinates.Clear();
                    }
                    else if(coordinates.Count > 0)
                    {
                        coordinates.Add(segment.P0);
                        lines.Add(new LineString(coordinates.ToArray()));
                        coordinates.Clear();
                    }
                }
                if (coordinates.Count > 0)
                {
                    coordinates.Add(lineSegments[lineSegments.Count - 1].P1);
                    lines.Add(new LineString(coordinates.ToArray()));
                }
            }

            return lines;
        }

        protected IList<ILineString> TestRing(LineString ring)
        {
            List<ILineString> rings = new List<ILineString>();
            if (ring.IsSimple && CGAlgorithms.IsCCW(ring.Coordinates))
                rings.Add(ring);
            else
            {
                IList<LineSegment> newLines = HelperClassLines.CreateLines(ring.Coordinates);
                for (int i = 0; i < newLines.Count; i++)
                {
                    for (int k = i + 2; k < newLines.Count; k++)
                    {
                        if (i == 0 && k == newLines.Count -1)
                            continue;

                        Coordinate intersection = newLines[i].Intersection(newLines[k]);
                        
                        if (intersection != null && !Double.IsNaN(intersection.X) && !Double.IsNaN(intersection.Y))
                        {
                            List<Coordinate> ring1 = new List<Coordinate>();
                            List<Coordinate> ring2 = new List<Coordinate>();

                            int first = i + 1;
                            int second = k + 1;

                            ring1.Add(intersection);
                            for (int l = first; l < second; l++)
                                ring1.Add(ring.Coordinates[l]);
                            ring1.Add(intersection);
                            rings.AddRange(TestRing(new LineString(ring1.ToArray())));

                            for (int l = 0; l < ring.Coordinates.Length; l++)
                            {
                                if (l < first || l >= second)
                                    ring2.Add(ring.Coordinates[l]);
                                else if (l == first)
                                    ring2.Add(intersection);
                            }
                            rings.AddRange(TestRing(new LineString(ring2.ToArray())));

                            return rings;
                        }
                    }
                }
            }

            return rings;
        }

        protected void OnFarmingEvent(string message)
        {
            if (FarmingEvent != null)
                FarmingEvent.Invoke(this, message);
        }

        #endregion

        #region IFarmingMode Implementation

        public IList<TrackingLine> TrackingLinesHeadland
        {
            get { return _trackingLinesHeadland; }
        }

        public IList<TrackingLine> TrackingLines
        {
            get { return _trackingLines; }
        }

        public bool EquipmentSideOutRight
        { 
            get { return _trackingLineBackwards; }
            set { _trackingLineBackwards = value; }
        }

        public virtual TrackingLine GetClosestLine(Coordinate position, Azimuth direction)
        {
            double distanceToLine = double.MaxValue;
            TrackingLine closestLine = null;
            foreach(TrackingLine trackingLine in _trackingLines)
            {
                if (trackingLine.Depleted)
                    continue;

                double tempDistance = trackingLine.GetDistanceToLine(position);
                if(tempDistance < distanceToLine)
                {
                    distanceToLine = tempDistance;
                    closestLine = trackingLine;
                }
            }

            foreach (TrackingLine trackingLine in _trackingLinesHeadland)
            {
                if (trackingLine.Depleted)
                    continue;

                double tempDistance = trackingLine.GetDistanceToLine(position);
                if (tempDistance < distanceToLine)
                {
                    distanceToLine = tempDistance;
                    closestLine = trackingLine;
                }
            }

            return closestLine;
        }

        public virtual void CreateTrackingLines(Coordinate aCoord, DotSpatial.NTSExtension.Angle direction)
        {
            _hasChanged = true;
        }

        public virtual void CreateTrackingLines(Coordinate aCoord, Coordinate bCoord)
        {
            _hasChanged = true;
        }

        public virtual void CreateTrackingLines(TrackingLine headLine)
        {
            _hasChanged = true;
        }

        public virtual void CreateTrackingLines(TrackingLine trackingLine, DotSpatial.NTSExtension.Angle directionFromLine)
        {
            _hasChanged = true;
        }

        public virtual void CreateTrackingLines(TrackingLine trackingLine, DotSpatial.NTSExtension.Angle directionFromLine, double offset)
        {
            _hasChanged = true;
        }

        public virtual void UpdateEvents(ILineString positionEquipment, DotSpatial.Positioning.Azimuth direction)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<string> FarmingEvent;

        #endregion

        #region IStateObjectImplementation
        
        public virtual object StateObject
        {
            get
            {
                List<SimpleLine> trackingLines = new List<SimpleLine>();
                foreach (TrackingLine trackingLine in _trackingLines)
                    trackingLines.Add(new SimpleLine(trackingLine.Line.Coordinates));
                List<SimpleLine> trackingLinesHeadland = new List<SimpleLine>();
                foreach (TrackingLine trackingLineHeadland in _trackingLinesHeadland)
                    trackingLinesHeadland.Add(new SimpleLine(trackingLineHeadland.Line.Coordinates));
                return new FarmingModeState(trackingLines, trackingLinesHeadland, _trackingLineBackwards);
            }
        }

        public virtual Type StateType
        {
            get { return typeof(FarmingModeState); }
        }

        public bool HasChanged
        {
            get { return _hasChanged; }
        }

        public virtual void RestoreObject(object restoredState)
        {
            FarmingModeState farmingModeState = (FarmingModeState)restoredState;
            foreach (SimpleLine line in farmingModeState.TrackingLines)
                _trackingLines.Add(new TrackingLine(new LineString(line.LineCoordinateArray), false));
            foreach (SimpleLine line in farmingModeState.TrackingLinesHeadLand)
                _trackingLinesHeadland.Add(new TrackingLine(new LineString(line.LineCoordinateArray), true));
            _trackingLineBackwards = farmingModeState.TrackingLineBackwards;
        }

        #endregion
    }
}
