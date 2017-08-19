using System;
using System.Collections.Generic;
using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;

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

        private bool _depleted;

        private bool _active;

        protected ILineString _line;

        #endregion

        public TrackingLine(ILineString line)
        {
            _depleted = false;
            _active = false;
            _line = line;
        }

        public OrientationToLine GetOrientationToLine(Coordinate point, DotSpatial.Positioning.Azimuth directionOfTravel)
        {
            Coordinate p0 = Coordinate.Empty;
            Coordinate p1 = Coordinate.Empty;
            double tempDistance = 0.0;
            double distance = double.MaxValue;
            for (int i = 0; i < _line.Coordinates.Count - 1; i++)
            {
                tempDistance = CgAlgorithms.DistancePointLine(point, _line.Coordinates[i], _line.Coordinates[i + 1]);
                if (tempDistance < distance)
                {
                    p0 = _line.Coordinates[i];
                    p1 = _line.Coordinates[i+1];
                    distance = tempDistance;
                }
            }
            LineSegment line = new LineSegment(p0, p1);
            directionOfTravel =  DotSpatial.Positioning.Azimuth.Maximum - directionOfTravel.Subtract(90.0).DecimalDegrees;
            Angle angleDiff = new Angle(line.Angle - directionOfTravel.ToRadians().Value);
            if (angleDiff.Radians > Math.PI)
                angleDiff = new Angle(angleDiff.Radians - Math.PI * 2);
            else if (angleDiff.Radians < Math.PI * -1)
                angleDiff = new Angle(angleDiff.Radians + Math.PI * 2);
            OrientationToLine.Side side = (CgAlgorithms.OrientationIndex(p0, p1, point) > 0) ? OrientationToLine.Side.Left : OrientationToLine.Side.Right;
            if(!(angleDiff.Degrees < 90 && angleDiff.Degrees > -90))
            {
                if(side == OrientationToLine.Side.Right)
                    side = OrientationToLine.Side.Left;
                else if(side == OrientationToLine.Side.Left)
                    side = OrientationToLine.Side.Right;
            }
            return new OrientationToLine(side, distance);
        }
        
        public double GetDistanceToLine(Coordinate point)
        {
            double tempDistance = 0.0;
            double distance = double.MaxValue;
            for (int i = 0; i < _line.Coordinates.Count - 1; i++)
            {
                tempDistance = CgAlgorithms.DistancePointLine(point, _line.Coordinates[i], _line.Coordinates[i + 1]);
                if (tempDistance < distance)
                    distance = tempDistance;
            }
            return distance;
        }

        public bool Depleted
        {
            get { return _depleted; }
            set 
            { 
                if(_depleted != value)
                {
                    DepletedChangedEventHandler handler = DepletedChanged;
                    if (handler != null)
                        handler.Invoke(this, value);
                }
                _depleted = value;
            }
        }

        public bool Active
        {
            get { return _active; }
            set
            {
                if(_active != value)
                {
                    ActiveChangedEventHandler handler = ActiveChanged;
                    if (handler != null)
                        handler.Invoke(this, value);
                }
                _active = value;
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

        public ILineSegment MainLine
        {
            get
            {
                return new LineSegment(_line.Coordinates[_line.Coordinates.Count - 2], _line.Coordinates[_line.Coordinates.Count - 1]);
            }
        }

        public ILineString Line
        {
            get { return _line; }
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
