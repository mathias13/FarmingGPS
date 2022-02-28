using DotSpatial.NTSExtension;
using GeoAPI.Geometries;
using GpsUtilities.HelperClasses;
using NetTopologySuite.Geometries;
using System;

namespace FarmingGPSLib.FarmingModes.Tools
{
    public class TrackingLineStartStopEvent : TrackingLine, IFarmingEvent
    {
        internal struct EventEvaluation
        {
            public Angle Left;
            public Angle Right;
            public IGeometry LineToCross;
            public double LastDistance;
            public bool ToBeTriggered;
            public string Message;
        }

        public const string START_EVENT = "EQUIPMENT_START";

        public const string STOP_EVENT = "EQUIPMENT_STOP";

        private string _message = String.Empty;

        private EventEvaluation[] _events = new EventEvaluation[4];

        public TrackingLineStartStopEvent(ILineString line, double startEventDistance, double stopEventDistance) : base(line, false)
        {
            Angle angleLeft = new Angle(MainLine.Angle + (Math.PI / 2.0));
            Angle angleRight = new Angle(MainLine.Angle - (Math.PI / 2.0));
            _events[0].Left = new Angle(MainLine.Angle + (Math.PI / 3.0));
            _events[0].Right = new Angle(MainLine.Angle - (Math.PI / 3.0));
            Coordinate coord = HelperClassCoordinate.ComputePoint(MainLine.P0, MainLine.Angle, startEventDistance);
            _events[0].LineToCross = new LineString(new Coordinate[] { HelperClassCoordinate.ComputePoint(coord, angleLeft.Radians, 2.0), HelperClassCoordinate.ComputePoint(coord, angleRight.Radians, 2.0) });
            _events[0].LastDistance = double.NaN;
            _events[0].Message = START_EVENT;
            _events[1].Left = _events[0].Left.Copy();
            _events[1].Right = _events[0].Right.Copy();
            coord = HelperClassCoordinate.ComputePoint(MainLine.P1, MainLine.Angle, stopEventDistance * -1.0);
            _events[1].LineToCross = new LineString(new Coordinate[] { HelperClassCoordinate.ComputePoint(coord, angleLeft.Radians, 2.0), HelperClassCoordinate.ComputePoint(coord, angleRight.Radians, 2.0) });
            _events[1].LastDistance = double.NaN;
            _events[1].Message = STOP_EVENT;
            _events[2].Left = new Angle(MainLine.Angle - (Math.PI / 1.5));
            _events[2].Right = new Angle(MainLine.Angle + (Math.PI / 1.5));
            coord = HelperClassCoordinate.ComputePoint(MainLine.P0, MainLine.Angle, stopEventDistance);
            _events[2].LineToCross = new LineString(new Coordinate[] { HelperClassCoordinate.ComputePoint(coord, angleLeft.Radians, 2.0), HelperClassCoordinate.ComputePoint(coord, angleRight.Radians, 2.0) });
            _events[2].LastDistance = double.NaN;
            _events[2].Message = STOP_EVENT;
            _events[3].Left = _events[2].Left.Copy();
            _events[3].Right = _events[2].Right.Copy();
            coord = HelperClassCoordinate.ComputePoint(MainLine.P1, MainLine.Angle, startEventDistance * -1.0);
            _events[3].LineToCross = new LineString(new Coordinate[] { HelperClassCoordinate.ComputePoint(coord, angleLeft.Radians, 2.0), HelperClassCoordinate.ComputePoint(coord, angleRight.Radians, 2.0) });
            _events[3].LastDistance = double.NaN;
            _events[3].Message = START_EVENT;
        }

        public TrackingLineStartStopEvent(ILineString line, IGeometry startPoint, IGeometry endPoint, double startEventDistance, double stopEventDistance) : base(line, startPoint, endPoint, false)
        {
            Angle angleLeft = new Angle(MainLine.Angle + (Math.PI / 2.0));
            Angle angleRight = new Angle(MainLine.Angle - (Math.PI / 2.0));

            _events[0].Left = new Angle(MainLine.Angle + (Math.PI / 3.0));
            _events[0].Right = new Angle(MainLine.Angle - (Math.PI / 3.0));
            if (startPoint.GeometryType == "Point")
            {
                var coord = HelperClassCoordinate.ComputePoint(startPoint.Coordinates[0], MainLine.Angle, startEventDistance);
                _events[0].LineToCross = new LineString(new Coordinate[2] { HelperClassCoordinate.ComputePoint(coord, angleLeft.Radians, 2.5), HelperClassCoordinate.ComputePoint(coord, angleRight.Radians, 2.5) });
            }
            else
                _events[0].LineToCross = new LineString(HelperClassCoordinate.TranslateCoordinates(startPoint, MainLine.Angle, startEventDistance));

            _events[0].LastDistance = double.NaN;
            _events[0].Message = START_EVENT;

            _events[1].Left = _events[0].Left.Copy();
            _events[1].Right = _events[0].Right.Copy();
            if (endPoint.GeometryType == "Point")
            {
                var coord = HelperClassCoordinate.ComputePoint(endPoint.Coordinates[0], MainLine.Angle, stopEventDistance * -1.0);
                _events[1].LineToCross = new LineString(new Coordinate[2] { HelperClassCoordinate.ComputePoint(coord, angleLeft.Radians, 2.5), HelperClassCoordinate.ComputePoint(coord, angleRight.Radians, 2.5) });
            }
            else
                _events[1].LineToCross = new LineString(HelperClassCoordinate.TranslateCoordinates(endPoint, MainLine.Angle, stopEventDistance * -1.0));
            _events[1].LastDistance = double.NaN;
            _events[1].Message = STOP_EVENT;

            _events[2].Left = new Angle(MainLine.Angle - (Math.PI / 1.5));
            _events[2].Right = new Angle(MainLine.Angle + (Math.PI / 1.5));
            if (endPoint.GeometryType == "Point")
            {
                var coord = HelperClassCoordinate.ComputePoint(startPoint.Coordinates[0], MainLine.Angle, stopEventDistance);
                _events[2].LineToCross = new LineString(new Coordinate[2] { HelperClassCoordinate.ComputePoint(coord, angleLeft.Radians, 2.5), HelperClassCoordinate.ComputePoint(coord, angleRight.Radians, 2.5) });
            }
            else
                _events[2].LineToCross = new LineString(HelperClassCoordinate.TranslateCoordinates(startPoint, MainLine.Angle, stopEventDistance));

            _events[2].LastDistance = double.NaN;
            _events[2].Message = STOP_EVENT;

            _events[3].Left = _events[2].Left.Copy();
            _events[3].Right = _events[2].Right.Copy();
            if (endPoint.GeometryType == "Point")
            {
                var coord = HelperClassCoordinate.ComputePoint(endPoint.Coordinates[0], MainLine.Angle, startEventDistance * -1.0);
                _events[3].LineToCross = new LineString(new Coordinate[2] { HelperClassCoordinate.ComputePoint(coord, angleLeft.Radians, 2.5), HelperClassCoordinate.ComputePoint(coord, angleRight.Radians, 2.5) });
            }
            else
                _events[3].LineToCross = new LineString(HelperClassCoordinate.TranslateCoordinates(endPoint, MainLine.Angle, startEventDistance * -1.0));
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
            return false;
            //Angle angle = new Angle(directionOfTravel.ToRadians().Value);
            //angle.Radians = angle.Radians * -1.0;
            //angle.Radians += Math.PI / 2.0;
            //for (int i = 0; i < _events.Length; i++)
            //{
            //    if (HelperClassAngles.AngleBetween(angle, _events[i].Left, _events[i].Right))
            //    {
            //        double distanceToLine = CgAlgorithms.DistancePointLine(coord, _events[i].LineToCross.P0, _events[i].LineToCross.P1);
            //        if (double.IsNaN(_events[i].LastDistance))
            //            _events[i].LastDistance = distanceToLine;
            //        else
            //        {
            //            if (_events[i].ToBeTriggered)
            //            {
            //                if (_events[i].LastDistance < distanceToLine && distanceToLine < 3.0)
            //                {
            //                    _events[i].ToBeTriggered = false;
            //                    _message = _events[i].Message;
            //                    return true;
            //                }
            //                else
            //                    _events[i].LastDistance = distanceToLine;
            //            }
            //            else
            //            {
            //                _events[i].ToBeTriggered = _events[i].LastDistance >= distanceToLine;
            //                _events[i].LastDistance = distanceToLine;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        _events[i].LastDistance = double.NaN;
            //        _events[i].ToBeTriggered = false;
            //    }
            //}
            //return false;
        }

        public bool EventFired(DotSpatial.Positioning.Azimuth directionOfTravel, ILineString equipmentCoords)
        {
            Angle angle = new Angle(directionOfTravel.ToRadians().Value);
            angle.Radians = angle.Radians * -1.0;
            angle.Radians += Math.PI / 2.0;
            for (int i = 0; i < _events.Length; i++)
            {
                double distanceToLine = _events[i].LineToCross.Distance(equipmentCoords);
                if (HelperClassAngles.AngleBetween(angle, _events[i].Left, _events[i].Right) && distanceToLine < 4.0)
                {
                    if (double.IsNaN(_events[i].LastDistance))
                    {
                        _events[i].LastDistance = distanceToLine;
                    }
                    else
                    {
                        if (_events[i].ToBeTriggered)
                        {
                            if (_events[i].Message == START_EVENT && (distanceToLine < 0.01 || _events[i].LastDistance < distanceToLine))
                            {
                                _events[i].ToBeTriggered = false;
                                _message = _events[i].Message;
                                return true;
                            }
                            else if (_events[i].Message == STOP_EVENT && _events[i].LastDistance < distanceToLine && distanceToLine > 0.01)
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
                            _events[i].ToBeTriggered = _events[i].LastDistance > distanceToLine && distanceToLine > 0.5;
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
