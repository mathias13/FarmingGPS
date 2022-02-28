using DotSpatial.Positioning;
using DotSpatial.Projections;
using GeoAPI.Geometries;
using GpsUtilities.Filter;
using NetTopologySuite.Geometries;
using NetTopologySuite.Simplify;
using System;
using System.Collections.Generic;

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

        protected const double DISTANCE_TO_START_FIELD_FINISHED = 10.0;

        private Orientation _orientation;

        private ProjectionInfo _projectionInfo;

        protected ProjectionInfo _projWGS84 = KnownCoordinateSystems.Geographic.World.WGS1984;

        //private IEquipment _equipment;

        private List<Coordinate> _track = new List<Coordinate>();

        private Field _field;

        private DistanceTrigger _distanceTrigger;

        private bool _waitForFinish = false;

        private bool _fieldFinished = false;

        #endregion

        public FieldCreator(ProjectionInfo projectionInfo, Orientation orientation, Position currentPosition)
        {
            _orientation = orientation;
            _projectionInfo = projectionInfo;
            
            List<Position> fieldPoints = new List<Position>();
            fieldPoints.Add(currentPosition.TranslateTo(Azimuth.Northwest, Distance.FromMeters(1.0)));
            fieldPoints.Add(currentPosition.TranslateTo(Azimuth.Southwest, Distance.FromMeters(1.0)));
            fieldPoints.Add(currentPosition.TranslateTo(Azimuth.Southeast, Distance.FromMeters(1.0)));
            fieldPoints.Add(currentPosition.TranslateTo(Azimuth.Northeast, Distance.FromMeters(1.0)));
            fieldPoints.Add(currentPosition.TranslateTo(Azimuth.Northwest, Distance.FromMeters(1.0)));
            _distanceTrigger = new DistanceTrigger(MINIMUM_DISTANCE_BETWEEN_POINTS, MAXIMUM_DISTANCE_BETWEEN_POINTS, MINIMUM_CHANGE_DIRECTION, MAXIMUM_CHANGE_DIRECTION);
            _field = new Field(fieldPoints, projectionInfo);
        }
        
        private void AddPoint(Coordinate actualPosition)
        {
            if (_track.Count > 0)
                _track.Insert(_track.Count - 1, actualPosition);
            else
            {
                _track.Add(actualPosition);
                _track.Add(_track[0]);
            }

        }

        private bool CheckFinishedField(Coordinate actualPosition)
        {
            double distance = new LineSegment(actualPosition, _track[0]).Length;
            if (!_waitForFinish)
                _waitForFinish =  distance > Distance.FromMeters(DISTANCE_TO_START_FIELD_FINISHED + 1.0).Value;
            else
            {
                if (distance < Distance.FromMeters(DISTANCE_TO_START_FIELD_FINISHED).Value)
                {
                    double[] xyArray = new double[2];
                    double[] zArray = new double[1];
                    List<Position> coordinates = new List<Position>();
                    var polygon = (Polygon)TopologyPreservingSimplifier.Simplify(new Polygon(new LinearRing(_track.ToArray())), 0.5);
                    foreach (Coordinate coord in polygon.Coordinates)
                    {
                        xyArray[0] = coord.X;
                        xyArray[1] = coord.Y;
                        zArray = new double[1] { Distance.EarthsAverageRadius.ToMeters().Value };
                        Reproject.ReprojectPoints(xyArray, zArray, _projectionInfo, _projWGS84, 0, zArray.Length);
                        coordinates.Add(new Position(new Longitude(xyArray[0]), new Latitude(xyArray[1])));
                    }

                    _field = new Field(coordinates, _field.Projection);                    
                    OnFieldCreated(_field);
                    return true;
                }
            }
            return false;
        }

        #region Public Methods

        public void UpdatePosition(Coordinate leftTip, Coordinate rightTip, Azimuth heading)
        {
            var correctPosition = leftTip;
            if (_orientation == Orientation.Lefthand)
                correctPosition = rightTip;

            if (_track.Count < 2)
            {
                AddPoint(correctPosition);
                _distanceTrigger.Init(correctPosition, heading);
            }
            else if (CheckFinishedField(correctPosition))
                _fieldFinished = true;
            else
            {
                if (_distanceTrigger.CheckDistance(correctPosition, heading))
                {
                    AddPoint(correctPosition);
                    if (_track.Count > 3)
                        OnFieldBoundaryUpdated(_track);
                }
            }
        }

        public Field GetField()
        {
            return _field;
        }

        #endregion

        #region Public Properties

        public bool FieldFinished
        {
            get { return _fieldFinished; }
        }

        #endregion

        #region Protected Methods

        protected void OnFieldBoundaryUpdated(List<Coordinate> fieldBoundary)
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
