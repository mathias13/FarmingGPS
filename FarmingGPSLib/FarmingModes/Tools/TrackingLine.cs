using DotSpatial.NTSExtension;
using GeoAPI.Geometries;
using GpsUtilities.HelperClasses;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

namespace FarmingGPSLib.FarmingModes.Tools
{
    public delegate void DepletedChangedEventHandler(object sender, bool depleted);

    public delegate void ActiveChangedEventHandler(object sender, bool active);

    public class TrackingLine
    {

        #region Event

        public event DepletedChangedEventHandler DepletedChanged;

        public event ActiveChangedEventHandler ActiveChanged;

        #endregion

        #region Private Variables

        private bool _depleted = false;

        private bool _active = false;

        private bool _headland;

        private IGeometry _startPoint = null;

        private IGeometry _endPoint = null;

        protected ILineString _line;

        protected LineSegment _extendedLine = null;

        #endregion

        public TrackingLine(ILineString line, bool headland)
        {
            _depleted = false;
            _active = false;
            _line = line;
            _headland = headland;
            if (!headland)
            {
                _extendedLine = new LineSegment(line.Coordinates[0], line.Coordinates[1]);
                Coordinate p0 = HelperClassCoordinate.ComputePoint(line.Coordinates[0], _extendedLine.Angle, -40.0);
                Coordinate p1 = HelperClassCoordinate.ComputePoint(line.Coordinates[1], _extendedLine.Angle, 40.0);
                _extendedLine = new LineSegment(p0, p1);
            }
        }

        public TrackingLine(ILineString line, IGeometry startPoint, IGeometry endPoint, bool headland) : this(line, headland)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
        }

        public OrientationToLine GetOrientationToLine(Coordinate point, DotSpatial.Positioning.Azimuth directionOfTravel)
        {
            Coordinate p0 = new Coordinate();
            Coordinate p1 = new Coordinate();
            bool trackingBackwards;
            double distance = double.MaxValue;
            if (_extendedLine != null  && !_headland)
            {
                p0 = _extendedLine.P0;
                p1 = _extendedLine.P1;
                distance = CGAlgorithms.DistancePointLine(point, _extendedLine.P0, _extendedLine.P1);
            }
            else
            {
                for (int i = 0; i < _line.Coordinates.Length - 1; i++)
                {
                    double tempDistance = CGAlgorithms.DistancePointLine(point, _line.Coordinates[i], _line.Coordinates[i + 1]);
                    if (tempDistance < distance)
                    {
                        p0 = _line.Coordinates[i];
                        p1 = _line.Coordinates[i + 1];
                        distance = tempDistance;
                    }
                }
            }
            LineSegment line = new LineSegment(p0, p1);
            directionOfTravel =  DotSpatial.Positioning.Azimuth.Maximum - directionOfTravel.Subtract(90.0).DecimalDegrees;
            Angle angleDiff = new Angle(line.Angle - directionOfTravel.ToRadians().Value);
            if (angleDiff.Radians > Math.PI)
                angleDiff = new Angle(angleDiff.Radians - Math.PI * 2);
            else if (angleDiff.Radians < Math.PI * -1)
                angleDiff = new Angle(angleDiff.Radians + Math.PI * 2);
            OrientationToLine.Side side = (CGAlgorithms.OrientationIndex(p0, p1, point) > 0) ? OrientationToLine.Side.Left : OrientationToLine.Side.Right;
            if(!(angleDiff.Degrees < 90 && angleDiff.Degrees > -90))
            {
                trackingBackwards = true;
                if(side == OrientationToLine.Side.Right)
                    side = OrientationToLine.Side.Left;
                else if(side == OrientationToLine.Side.Left)
                    side = OrientationToLine.Side.Right;
            }
            else
                trackingBackwards = false;
            return new OrientationToLine(side, distance, trackingBackwards);
        }
        
        public double GetDistanceToLine(Coordinate point)
        {
            double tempDistance = 0.0;
            double distance = double.MaxValue;
            if (_extendedLine != null && !_headland)
                distance = CGAlgorithms.DistancePointLine(point, _extendedLine.P0, _extendedLine.P1);
            else
            {
                for (int i = 0; i < _line.Coordinates.Length - 1; i++)
                {
                    tempDistance = CGAlgorithms.DistancePointLine(point, _line.Coordinates[i], _line.Coordinates[i + 1]);
                    if (tempDistance < distance)
                        distance = tempDistance;
                }
            }
            return distance;
        }

        public bool Depleted
        {
            get { return _depleted; }
            set
            {
                bool changed = false;
                if (_depleted != value)
                {
                    if (value && Active)
                        Active = false;
                    changed = true;
                }
                _depleted = value;

                if (changed)
                {
                    DepletedChangedEventHandler handler = DepletedChanged;
                    if (handler != null)
                        handler.Invoke(this, value);
                }
            }
        }

        public bool Active
        {
            get { return _active; }
            set
            {
                bool changed = false;
                if(_active != value)
                {
                    if (value && Depleted)
                        Depleted = false;
                    changed = true;
                }
                _active = value;

                if (changed)
                {
                    ActiveChangedEventHandler handler = ActiveChanged;
                    if (handler != null)
                        handler.Invoke(this, value);
                }
            }
        }

        public IList<Coordinate> Points
        {
            get
            {
                return _line.Coordinates;
            }
        }

        public double Length
        {
            get
            {
                return _line.Length;
            }
        }

        public double Angle
        {
            get
            {
                Angle angle = new Angle(MainLine.Angle);
                return angle.DegreesPos;
            }
        }

        public LineSegment MainLine
        {
            get
            {
                return new LineSegment(_line.Coordinates[_line.Coordinates.Length - 2], _line.Coordinates[_line.Coordinates.Length - 1]);
            }
        }

        public ILineString Line
        {
            get { return _line; }
        }

        public IGeometry StartPoint
        {
            get { return _startPoint; }
        }

        public IGeometry EndPoint
        {
            get { return _endPoint; }
        }

        public override bool Equals(object obj)
        {
            if (obj is TrackingLine)
            {
                TrackingLine trackingLine = (TrackingLine)obj;
                return trackingLine.Line.Equals(this.Line);
            }
            else
                return false;
        }

        public override int GetHashCode()
        {
            return this.Line.GetHashCode();
        }
    }
}
