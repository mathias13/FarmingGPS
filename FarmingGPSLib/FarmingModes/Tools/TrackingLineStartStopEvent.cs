using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;
using FarmingGPSLib.HelperClasses;
using System;

namespace FarmingGPSLib.FarmingModes.Tools
{
    public class TrackingLineStartStopEvent : TrackingLine, IFarmingEvent
    {
        internal struct EventEvaluation
        {
            public Angle Left;
            public Angle Right;
            public LineSegment LineToCross;
            public double LastDistance;
            public bool ToBeTriggered;
            public string Message;
        }

        public const string START_EVENT = "EQUIPMENT_START";

        public const string STOP_EVENT = "EQUIPMENT_STOP";

        private string _message = String.Empty;

        private EventEvaluation[] _events = new EventEvaluation[4];

        public TrackingLineStartStopEvent(ILineString line, double startEventDistance, double stopEventDistance) : base(line)
        {
            Angle angleLeft = new Angle(MainLine.Angle + (Math.PI / 2.0));
            Angle angleRight = new Angle(MainLine.Angle - (Math.PI / 2.0));
            _events[0].Left = new Angle(MainLine.Angle + (Math.PI / 3.0));
            _events[0].Right = new Angle(MainLine.Angle - (Math.PI / 3.0));
            Coordinate coord = HelperClassCoordinate.ComputePoint(MainLine.P0, MainLine.Angle, startEventDistance);
            _events[0].LineToCross = new LineSegment(HelperClassCoordinate.ComputePoint(coord, angleLeft.Radians, 2.0), HelperClassCoordinate.ComputePoint(coord, angleRight.Radians, 1.0));
            _events[0].LastDistance = double.NaN;
            _events[0].Message = START_EVENT;
            _events[1].Left = _events[0].Left.Copy();
            _events[1].Right = _events[0].Right.Copy();
            coord = HelperClassCoordinate.ComputePoint(MainLine.P1, MainLine.Angle, stopEventDistance * -1.0);
            _events[1].LineToCross = new LineSegment(HelperClassCoordinate.ComputePoint(coord, angleLeft.Radians, 2.0), HelperClassCoordinate.ComputePoint(coord, angleRight.Radians, 1.0));
            _events[1].LastDistance = double.NaN;
            _events[1].Message = STOP_EVENT;
            _events[2].Left = new Angle(MainLine.Angle - (Math.PI / 1.5));
            _events[2].Right = new Angle(MainLine.Angle + (Math.PI / 1.5));
            coord = HelperClassCoordinate.ComputePoint(MainLine.P0, MainLine.Angle, stopEventDistance);
            _events[2].LineToCross = new LineSegment(HelperClassCoordinate.ComputePoint(coord, angleLeft.Radians, 2.0), HelperClassCoordinate.ComputePoint(coord, angleRight.Radians, 1.0));
            _events[2].LastDistance = double.NaN;
            _events[2].Message = STOP_EVENT;
            _events[3].Left = _events[2].Left.Copy();
            _events[3].Right = _events[2].Right.Copy();
            coord = HelperClassCoordinate.ComputePoint(MainLine.P1, MainLine.Angle, startEventDistance * -1.0);
            _events[3].LineToCross = new LineSegment(HelperClassCoordinate.ComputePoint(coord, angleLeft.Radians, 2.0), HelperClassCoordinate.ComputePoint(coord, angleRight.Radians, 1.0));
            _events[3].LastDistance = double.NaN;
            _events[3].Message = START_EVENT;
        }

        #region IFarmingEvent

        public string Message
        {
            get
            {
                return _message;
            }
        }

        public bool EventFired(DotSpatial.Positioning.Azimuth directionOfTravel, Coordinate coord)
        {
            Angle angle = new Angle(directionOfTravel.ToRadians().Value);
            angle.Radians = angle.Radians * -1.0;
            angle.Radians += Math.PI / 2.0;
            for (int i = 0; i < _events.Length; i++)
            {
                if (HelperClassAngles.AngleBetween(angle, _events[i].Left, _events[i].Right))
                {
                    double distanceToLine = CgAlgorithms.DistancePointLine(coord, _events[i].LineToCross.P0, _events[i].LineToCross.P1);
                    if (double.IsNaN(_events[i].LastDistance))
                        _events[i].LastDistance = distanceToLine;
                    else
                    {
                        if (_events[i].ToBeTriggered)
                        {
                            if (_events[i].LastDistance < distanceToLine && distanceToLine < 1.0)
                            {
                                _events[i].ToBeTriggered = false;
                                _message = _events[i].Message;
                                return true;
                            }
                            else
                                _events[i].LastDistance = distanceToLine;
                        }
                        else
                        {
                            _events[i].ToBeTriggered = _events[i].LastDistance >= distanceToLine;
                            _events[i].LastDistance = distanceToLine;
                        }
                    }
                }
                else
                {
                    _events[i].LastDistance = double.NaN;
                    _events[i].ToBeTriggered = false;
                }
            }
            return false;
        }
        
        #endregion
    }
}
