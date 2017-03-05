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

        public event EventHandler<FieldCreatedEventArgs> FieldCreated;

        #endregion

        #region Private Variables

        protected const double MINIMUM_DISTANCE_BETWEEN_POINTS = 0.3;

        protected const double MAXIMUM_DISTANCE_BETWEEN_POINTS = 30.0;

        protected const double MINIMUM_CHANGE_DIRECTION = 3.0;

        protected const double MAXIMUM_CHANGE_DIRECTION = 10.0;

        protected const double DISTANCE_TO_START_FIELD_FINISHED = 5.0;

        private Orientation _orientation;

        private ProjectionInfo _projectionInfo;

        private IEquipment _equipment;

        private List<Position> _track = new List<Position>();

        private Field _field;

        private Azimuth _previousHeading;

        private Position _previousPosition;

        private bool _waitForFinish = false;

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
            _field = new Field(fieldPoints, DotSpatial.Projections.KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33N);
            AddPoint(receiver.CurrentPosition, receiver.CurrentBearing);
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
            Position rightTip = _equipment.GetRightTip(actualPosition, heading);

            if (_track.Count > 0)
                _track.Insert(_track.Count - 2, _orientation == Orientation.Lefthand ? rightTip : leftTip);
            else
            {
                _track.Add(_orientation == Orientation.Lefthand ? rightTip : leftTip);
                _track.Add(_track[0]);
            }

        }

        private void CheckFinishedField(Position actualPosition)
        {
            if (!_waitForFinish)
                _waitForFinish = _track[0].DistanceTo(actualPosition) > Distance.FromMeters(DISTANCE_TO_START_FIELD_FINISHED + 1.0);
            else
            {
                if(_track[0].DistanceTo(actualPosition) < Distance.FromMeters(DISTANCE_TO_START_FIELD_FINISHED))
                {
                    _field = new Field(_track, _field.Projection);
                }
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
        
        #endregion

        #region Protected Methods

        protected void OnFieldBoundaryUpdated(List<Position> fieldBoundary)
        {
            if (FieldBoundaryUpdated != null)
                FieldBoundaryUpdated.Invoke(this, new FieldBoundaryUpdatedEventArgs(fieldBoundary));
        }

        protected void OnFieldCreated(List<Position> fieldBoundary)
        {
            if (FieldCreated != null)
                FieldCreated.Invoke(this, new FieldCreatedEventArgs(_field));
        }

        #endregion

    }
}
