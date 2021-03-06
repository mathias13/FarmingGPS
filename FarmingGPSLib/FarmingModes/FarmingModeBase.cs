﻿using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;
using DotSpatial.Topology.GeometriesGraph;
using DotSpatial.Topology.Operation.Buffer;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
using GpsUtilities.HelperClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using FarmingGPSLib.StateRecovery;

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
        [Serializable]
        public struct SimpleLine
        {
            public List<Coordinate> Line;

            public SimpleLine(List<Coordinate> line)
            {
                Line = line;
            }
        }

        [Serializable]
        public struct FarmingModeState
        {
            public List<SimpleLine> TrackingLines;

            public List<SimpleLine> TrackingLinesHeadLand;

            public FarmingModeState(List<SimpleLine> trackingLines, List<SimpleLine> trackingLinesHeadLand)
            {
                TrackingLines = trackingLines;
                TrackingLinesHeadLand = trackingLinesHeadLand;
            }
        }

        #region Private Variables

        protected Polygon _fieldPolygon;

        protected IList<TrackingLine> _trackingLinesHeadland = new List<TrackingLine>();

        protected IList<TrackingLine> _trackingLines = new List<TrackingLine>();

        private bool _hasChanged = true;

        #endregion

        public FarmingModeBase()
        {
        }

        public FarmingModeBase(IField field)
        {
            _fieldPolygon = field.Polygon;
        }

        #region private Methods

        protected IList<ILineString> GetHeadLandCoordinates(double distance)
        {
            LineString newRing = new LineString(GetHeadlandAroundPoints(distance));

            IList<ILineString> rings = TestRing(newRing);

            return rings;
        }
        
        protected IList<LineString> GetHeadlandLines(double distance)
        {
            IList<ILineString> newRing = GetHeadLandCoordinates(distance);
            List<LineString> lines = new List<LineString>();

            foreach (ILineString ring in newRing)
            {
                IList<LineSegment> lineSegments = HelperClassLines.CreateLines(ring.Coordinates);
                IList<Coordinate> coordinates = new List<Coordinate>();
                foreach(LineSegment segment in lineSegments)
                {
                    coordinates.Add(segment.P0);
                    if (segment.Length < 10.0)
                        continue;
                    coordinates.Add(segment.P1);
                    lines.Add(new LineString(coordinates.CloneList()));
                    coordinates.Clear();
                }
                if(coordinates.Count > 0)
                {
                    coordinates.Add(lineSegments[lineSegments.Count - 1].P1);
                    lines.Add(new LineString(coordinates.CloneList()));
                }
            }
            
            return lines;
        }

        protected IList<Coordinate> GetHeadlandAroundPoints(double distance)
        {
            OffsetCurveBuilder curveBuilder = new OffsetCurveBuilder(new PrecisionModel(PrecisionModelType.Floating));
            IList list = curveBuilder.GetRingCurve(_fieldPolygon.Shell.Coordinates, PositionType.Left, distance);
            return (IList<Coordinate>)list[0];
        }

        protected IList<ILineString> TestRing(LineString ring)
        {
            List<ILineString> rings = new List<ILineString>();
            if (ring.IsSimple && CgAlgorithms.IsCounterClockwise(ring.Coordinates))
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
                        
                        if (intersection != null)
                        {
                            List<Coordinate> ring1 = new List<Coordinate>();
                            List<Coordinate> ring2 = new List<Coordinate>();

                            int first = i + 1;
                            int second = k + 1;

                            ring1.Add(intersection);
                            for (int l = first; l < second; l++)
                                ring1.Add(ring.Coordinates[l]);
                            ring1.Add(intersection);
                            rings.AddRange(TestRing(new LineString(ring1)));

                            for (int l = 0; l < ring.Coordinates.Count; l++)
                            {
                                if (l < first || l >= second)
                                    ring2.Add(ring.Coordinates[l]);
                                else if (l == first)
                                    ring2.Add(intersection);
                            }
                            rings.AddRange(TestRing(new LineString(ring2)));

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

        public TrackingLine GetClosestLine(Coordinate position)
        {
            double distanceToLine = double.MaxValue;
            TrackingLine closestLine = null;
            foreach(TrackingLine trackingLine in _trackingLines)
            {
                if (trackingLine.Depleted)
                    continue;

                double tempDistance = trackingLine.GetDistanceToLine(position, false);
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

                double tempDistance = trackingLine.GetDistanceToLine(position, false);
                if (tempDistance < distanceToLine)
                {
                    distanceToLine = tempDistance;
                    closestLine = trackingLine;
                }
            }

            return closestLine;
        }

        public virtual void CreateTrackingLines(Coordinate aCoord, Angle direction)
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

        public virtual void CreateTrackingLines(TrackingLine trackingLine, Angle directionFromLine)
        {
            _hasChanged = true;
        }

        public virtual void UpdateEvents(Coordinate position, DotSpatial.Positioning.Azimuth direction)
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
                    trackingLines.Add(new SimpleLine(new List<Coordinate>(trackingLine.Line.Coordinates)));
                List<SimpleLine> trackingLinesHeadland = new List<SimpleLine>();
                foreach (TrackingLine trackingLineHeadland in _trackingLinesHeadland)
                    trackingLinesHeadland.Add(new SimpleLine(new List<Coordinate>(trackingLineHeadland.Line.Coordinates)));
                return new FarmingModeState(trackingLines, trackingLinesHeadland);
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
                _trackingLines.Add(new TrackingLine(new LineString(line.Line)));
            foreach (SimpleLine line in farmingModeState.TrackingLinesHeadLand)
                _trackingLinesHeadland.Add(new TrackingLine(new LineString(line.Line)));
        }

        #endregion
    }
}
