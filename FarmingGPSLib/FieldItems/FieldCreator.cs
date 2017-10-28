using DotSpatial.Topology;
using DotSpatial.Positioning;
using DotSpatial.Projections;
using System;
using System.Collections.Generic;
using GpsUtilities.Reciever;
using FarmingGPSLib.Equipment;
using GpsUtilities.Filter;

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

        private DistanceTrigger _distanceTrigger;

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
            _distanceTrigger = new DistanceTrigger(MINIMUM_DISTANCE_BETWEEN_POINTS, MAXIMUM_DISTANCE_BETWEEN_POINTS, MINIMUM_CHANGE_DIRECTION, MAXIMUM_CHANGE_DIRECTION);
            _distanceTrigger.Init(receiver.CurrentPosition, receiver.CurrentBearing);
            _field = new Field(fieldPoints, DotSpatial.Projections.KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33N);
            Position leftTip = _equipment.GetLeftTip(receiver.CurrentPosition, receiver.CurrentBearing);
            Position rightTip = _equipment.GetRightTip(receiver.CurrentPosition, receiver.CurrentBearing);
            AddPoint(_orientation == Orientation.Lefthand ? rightTip : leftTip);
            receiver.PositionUpdate += Receiver_PositionUpdate;
        }
        
        private void Receiver_PositionUpdate(object sender, Position actualPosition)
        {
            IReceiver receiver = sender as IReceiver;
            Position correctPosition = _equipment.GetCenter(actualPosition, receiver.CurrentBearing);
            if(_orientation == Orientation.Lefthand)
                correctPosition = _equipment.GetRightTip(actualPosition, receiver.CurrentBearing);
            else
                correctPosition = _equipment.GetLeftTip(actualPosition, receiver.CurrentBearing);

            if (CheckFinishedField(correctPosition))
                receiver.PositionUpdate -= Receiver_PositionUpdate;
            else
            {
                if (_distanceTrigger.CheckDistance(correctPosition, receiver.CurrentBearing))
                {
                    AddPoint(correctPosition);
                    if (_track.Count > 3)
                        OnFieldBoundaryUpdated(_track);
                }
            }
        }

        private void AddPoint(Position actualPosition)
        {

            if (_track.Count > 0)
                _track.Insert(_track.Count - 1, actualPosition);
            else
            {
                _track.Add(actualPosition);
                _track.Add(_track[0]);
            }

        }

        private bool CheckFinishedField(Position actualPosition)
        {
            if (!_waitForFinish)
                _waitForFinish = _track[0].DistanceTo(actualPosition) > Distance.FromMeters(DISTANCE_TO_START_FIELD_FINISHED + 1.0);
            else
            {
                Distance distance = _track[0].DistanceTo(actualPosition);
                if (distance < Distance.FromMeters(DISTANCE_TO_START_FIELD_FINISHED))
                {
                    _field = new Field(_track, _field.Projection);                    
                    OnFieldCreated(_field);
                    return true;
                }
            }
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

        protected void OnFieldCreated(Field field)
        {
            if (FieldCreated != null)
                FieldCreated.Invoke(this, new FieldCreatedEventArgs(field));
        }

        #endregion

    }
}
