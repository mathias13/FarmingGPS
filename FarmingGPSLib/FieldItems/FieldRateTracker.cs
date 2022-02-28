using DotSpatial.Data;
using FarmingGPSLib.Equipment;
using FarmingGPSLib.StateRecovery;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

namespace FarmingGPSLib.FieldItems
{
    public class FieldRateTracker : IStateObject
    {
        [Serializable]
        public struct FieldRateTrackerState
        {
            public double DefaultRate;

            public double MinimumChangeOfRate;

            public string ShapeFile;

            public int RateColumn;

            public string FieldProjection;
        }

        #region Private Variables

        private double _currentRate = Double.NaN;

        private double _defaultRate = 0.0;

        private double _minimumChangeOfRate = 5.0;

        private Shapefile _shapeFile;

        private int _rateColumn = -1;

        private IEquipmentControl _equipmentToControl;

        private bool _auto = false;

        #endregion

        public FieldRateTracker()
        { }

        public FieldRateTracker(double defaultRate, double minimumChangeOfRate, Shapefile shapeFile, int rateColumn, IField field)
        {
            _defaultRate = defaultRate;
            _currentRate = defaultRate;
            _minimumChangeOfRate = minimumChangeOfRate;
            _shapeFile = shapeFile;
            _rateColumn = rateColumn;
            _shapeFile.Reproject(field.Projection);
            HasChanged = true;
        }

        #region Public Properties

        public bool Auto
        {
            get { return _auto; }
            set
            {
                if(value)
                    _equipmentToControl.SetRate(_currentRate);
                _auto = value;
            }
        }

        #endregion

        #region Public Methods

        public void UpdatePosition(Coordinate leftPoint, Coordinate rightPoint)
        {
            if (_equipmentToControl == null)
                return;

            LineString line = new LineString(new Coordinate[] { leftPoint, rightPoint });
            List<double> rates = new List<double>();
            foreach (var feature in _shapeFile.Features)
            {
                if (feature.FeatureType == FeatureType.Polygon && feature.Geometry is IPolygon)
                {
                    IPolygon poly = (IPolygon)feature.Geometry;
                    if (line.Intersects(poly))
                        rates.Add((double)feature.DataRow[_rateColumn]);
                }
            }

            double rateToSet = 0.0;
            if (rates.Count > 0)
            {
                foreach (double rate in rates)
                    rateToSet += rate;
                rateToSet = rateToSet / (double)rates.Count;
            }
            else
                rateToSet = _defaultRate;
            
            if (rateToSet > _currentRate + _minimumChangeOfRate || rateToSet < _currentRate - _minimumChangeOfRate)
            {
                _currentRate = rateToSet;
                if(_auto)
                    _equipmentToControl.SetRate(_currentRate);
            }
        }
        
        public void RegisterEquipmentControl(IEquipmentControl equipment)
        {
            _equipmentToControl = equipment;
            _equipmentToControl.SetRate(_currentRate);
        }

        #endregion

        #region IStateObject

        public object StateObject
        {
            get
            {
                HasChanged = false;
                return new FieldRateTrackerState() { DefaultRate = _defaultRate, MinimumChangeOfRate = _minimumChangeOfRate, RateColumn = _rateColumn, ShapeFile = _shapeFile.Filename, FieldProjection = _shapeFile.Projection.ToProj4String() };
            }
        }

        public virtual bool HasChanged { get; private set; } = false;

        public Type StateType
        {
            get { return typeof(FieldRateTrackerState); }
        }


        public void RestoreObject(object restoredState)
        {
            var fieldRateTrackerState = (FieldRateTrackerState)restoredState;
            _defaultRate = fieldRateTrackerState.DefaultRate;
            _currentRate = _defaultRate;
            _minimumChangeOfRate = fieldRateTrackerState.MinimumChangeOfRate;
            _rateColumn = fieldRateTrackerState.RateColumn;
            _shapeFile = Shapefile.OpenFile(fieldRateTrackerState.ShapeFile);
            _shapeFile.Projection = DotSpatial.Projections.KnownCoordinateSystems.Geographic.World.WGS1984;
            _shapeFile.Reproject(DotSpatial.Projections.ProjectionInfo.FromProj4String(fieldRateTrackerState.FieldProjection));
        }

        #endregion

    }
}
