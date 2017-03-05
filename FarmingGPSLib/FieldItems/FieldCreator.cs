using DotSpatial.Topology;
using DotSpatial.Positioning;
using DotSpatial.Projections;
using System;
using System.Collections.Generic;
using GpsUtilities.Reciever;
using FarmingGPSLib.Equipment;

namespace FarmingGPSLib.FieldItems
{
    public class FieldCreator
    {
        public enum Orientation
        {
            Lefthand,
            Righthand
        }

        #region Events

        public event EventHandler<FieldBoundaryUpdatedEventArgs> FieldBoundaryUpdated;

        #endregion

        #region Private Variables

        protected const double MINIMUM_DISTANCE_BETWEEN_POINTS = 0.3;

        protected const double MAXIMUM_DISTANCE_BETWEEN_POINTS = 30.0;

        protected const double MINIMUM_CHANGE_DIRECTION = 3.0;

        protected const double MAXIMUM_CHANGE_DIRECTION = 10.0;

        private Orientation _orientation;

        private ProjectionInfo _projectionInfo;

        private IEquipment _equipment;

        private List<Position> _track = new List<Position>();

        private Field _field;

        private FieldTracker _fieldTracker;

        private Azimuth _previousHeading;

        private Position _previousPosition;

        #endregion

        public FieldCreator(ProjectionInfo projectionInfo, Orientation orientation, IReceiver receiver, IEquipment equipment)
        {
            _orientation = orientation;
            _projectionInfo = projectionInfo;
            _equipment = equipment;
            
            List<Position> fieldPoints = new List<Position>();
            fieldPoints.Add(receiver.CurrentPosition.TranslateTo(Azimuth.Northwest, Distance.FromMeters(1.0)));
            fieldPoints.Add(receiver.CurrentPosition.TranslateTo(Azimuth.Southwest, Distance.FromMeters(1.0)));
            fieldPoints.Add(receiver.CurrentPosition.TranslateTo(Azimuth.Southeast, Distance.FromMeters(1.0)));
            fieldPoints.Add(receiver.CurrentPosition.TranslateTo(Azimuth.Northeast, Distance.FromMeters(1.0)));
            fieldPoints.Add(receiver.CurrentPosition.TranslateTo(Azimuth.Northwest, Distance.FromMeters(1.0)));
            _previousHeading = receiver.CurrentBearing;
            _previousPosition = receiver.CurrentPosition;
            _fieldTracker = new FieldTracker();
            _field = new Field(fieldPoints, DotSpatial.Projections.KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33N);
            receiver.PositionUpdate += Receiver_PositionUpdate;
        }
        
        private void Receiver_PositionUpdate(object sender, Position actualPosition)
        {
            IReceiver receiver = sender as IReceiver;
            Position correctPosition = _equipment.GetCenter(actualPosition, receiver.CurrentBearing);
            if (CheckDistanceFromPreviousPoint(correctPosition, receiver.CurrentBearing))
            {
                AddPoint(correctPosition, receiver.CurrentBearing);
                if (_track.Count > 3)
                    OnFieldBoundaryUpdated(_track);
            }
        }

        private void AddPoint(Position actualPosition, Azimuth heading)
        {
            Position leftTip = _equipment.GetLeftTip(actualPosition, heading);
            Position rightTip = _equipment.GetLeftTip(actualPosition, heading);
            
            if(_fieldTracker.IsTracking)
            {
                _track.Insert(_track.Count - 2, _orientation == Orientation.Lefthand ? rightTip : leftTip);
                _fieldTracker.AddTrackPoint(_field.GetPositionInField(leftTip), _field.GetPositionInField(rightTip));
            }
            else
            {
                _track.Add(_orientation == Orientation.Lefthand ? rightTip : leftTip);
                _track.Add(_track[0]);
                _fieldTracker.InitTrack(_field.GetPositionInField(leftTip), _field.GetPositionInField(rightTip));
            }

        }

        private bool CheckDistanceFromPreviousPoint(Position position, Azimuth heading)
        {
            double changeOfDirection = Math.Abs(heading.DecimalDegrees - _previousHeading.DecimalDegrees);
            double limit = MAXIMUM_DISTANCE_BETWEEN_POINTS - (((MAXIMUM_DISTANCE_BETWEEN_POINTS - MINIMUM_DISTANCE_BETWEEN_POINTS) / (MAXIMUM_CHANGE_DIRECTION - MINIMUM_CHANGE_DIRECTION)) * changeOfDirection);
            if (Distance.FromMeters(limit) < _previousPosition.DistanceTo(position))
            {
                _previousPosition = position;
                _previousHeading = heading;
                return true;
            }
            else
                return false;
        }

        #region Public Methods
        
        public Field GetField()
        {
            return _field;
        }

        public FieldTracker GetFieldTracker()
        {
            return null;
        }

        #endregion

        #region Protected Methods

        protected void OnFieldBoundaryUpdated(List<Position> fieldBoundary)
        {
            if (FieldBoundaryUpdated != null)
                FieldBoundaryUpdated.Invoke(this, new FieldBoundaryUpdatedEventArgs(fieldBoundary));
        }
        #endregion

    }
}
