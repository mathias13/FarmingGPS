using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;
using DotSpatial.Topology.GeometriesGraph;
using DotSpatial.Topology.Operation.Buffer;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
using FarmingGPSLib.HelperClasses;
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

        protected IList<LineString> GetHeadlandAround(double distance)
        {
            List<LineString> lines = new List<LineString>();
            LineString newRing = new LineString(GetHeadlandAroundPoints(distance));
            IList<LineSegment> oldLines = HelperClassLines.CreateLines(_fieldPolygon.Shell.Coordinates);
            IList<LineSegment> newLines = HelperClassLines.CreateLines(newRing.Coordinates);
            if (!newRing.IsSimple)
            {
                List<int> linesAlreadyFinished = new List<int>();

                for (int i = 0; i < newLines.Count; i++)
                {
                    for (int k = 0; k < newLines.Count; k++)
                    {
                        if (linesAlreadyFinished.Contains(i) || linesAlreadyFinished.Contains(k))
                            continue;
                        if (i == k)
                            continue;
                        Coordinate intersection = newLines[i].Intersection(newLines[k]);
                        if (intersection == newLines[i].P0 || intersection == newLines[i].P1 ||
                            intersection == newLines[k].P0 || intersection == newLines[k].P1)
                            continue;
                        if (intersection != null)
                        {
                            int first = newRing.Coordinates.IndexOf(newLines[i].P1);
                            int second = newRing.Coordinates.IndexOf(newLines[k].P1);
                            int indexToRemove = first;
                            while (first < second)
                            {
                                newRing.Coordinates.RemoveAt(indexToRemove);
                                first++;
                            }
                            newRing.Coordinates.Insert(indexToRemove, intersection);
                            int firstLine = i;
                            while (firstLine <= k)
                            {
                                linesAlreadyFinished.Add(firstLine);
                                firstLine++;
                            }
                        }
                    }
                }
                newLines = HelperClassLines.CreateLines(newRing.Coordinates);
                //throw new InvalidOperationException("Headland has an selfintersection");
            }

            int lastNewLine = 0;
            for (int i = 0; i < oldLines.Count; i++)
            {
                bool exist = false;
                for (int j = lastNewLine; j < newLines.Count; j++)
                    if (Math.Round(oldLines[i].Angle, 4) == Math.Round(newLines[j].Angle, 4))
                    {
                        exist = true;
                        lastNewLine = j + 1;
                        break;
                    }
                if (exist)
                    continue;
                oldLines.RemoveAt(i);
                i--;
            }

            List<Coordinate> coordNextResultLine = new List<Coordinate>();
            coordNextResultLine.Add(newLines[0].P0);
            int lastOldLine = 0;
            for (int i = 0; i < newLines.Count; i++)
            {
                coordNextResultLine.Add(newLines[i].P1);
                for (int j = lastOldLine; j < oldLines.Count; j++)
                {
                    if (Math.Round(newLines[i].Angle, 4) == Math.Round(oldLines[j].Angle, 4))
                    {
                        lines.Add(new LineString(coordNextResultLine.CloneList()));
                        coordNextResultLine.Clear();
                        coordNextResultLine.Add(newLines[i].P1);
                        lastOldLine = j + 1;
                        break;
                    }
                }
            }
            if (coordNextResultLine.Count > 1)
                lines.Add(new LineString(coordNextResultLine.CloneList()));
            return lines;
        }

        protected IList<Coordinate> GetHeadlandAroundPoints(double distance)
        {
            OffsetCurveBuilder curveBuilder = new OffsetCurveBuilder(new PrecisionModel(PrecisionModelType.Floating));
            IList list = curveBuilder.GetRingCurve(_fieldPolygon.Shell.Coordinates, PositionType.Left, distance);
            return (IList<Coordinate>)list[0];
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

        public virtual void UpdateEvents(Coordinate position, DotSpatial.Positioning.Azimuth direction)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<string> FarmingEvent;

        #endregion

        #region IStateObjectImplementation
        
        public object StateObject
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

        public Type StateType
        {
            get { return typeof(FarmingModeState); }
        }

        public bool HasChanged
        {
            get { return _hasChanged; }
        }

        public void RestoreObject(object restoredState)
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
