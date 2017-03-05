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
            //TODO fix this with general function to calculate tip position and check orientation
            _track.Add(receiver.CurrentPosition.TranslateTo(receiver.CurrentBearing.Subtract(equipment.FromDirectionOfTravel.DecimalDegrees - 90.0), equipment.CenterToTip));
            _track.Add(_track[0]);
            
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
            //TODO fix this with general function to calculate tip position and check orientation
            if (CheckDistanceFromPreviousPoint(actualPosition, receiver.CurrentBearing))
            {
                _track.Insert(_track.Count - 2, actualPosition);
                //_track.Insert(_track.Count - 1, receiver.CurrentPosition.TranslateTo(receiver.CurrentBearing.Subtract(_equipment.FromDirectionOfTravel.DecimalDegrees - 90.0), _equipment.CenterToTip));
                if (_track.Count > 3)
                    OnFieldBoundaryUpdated(_track);
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
