using DotSpatial.Data;
using DotSpatial.Topology;
using FarmingGPSLib.Equipment;
using FarmingGPSLib.FieldItems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FarmingGPSLib.FieldItems
{
    public class FieldRateTracker
    {
        #region Private Variables

        private double _currentRate = Double.NaN;

        private double _defaultRate = 0.0;

        private double _minimumChangeOfRate = 5.0;

        private Shapefile _shapeFile;

        private int _rateColumn = -1;

        private IEquipmentControl _equipmentToControl;

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
        }

        #region Public Methods

        public void UpdatePosition(Coordinate leftPoint, Coordinate rightPoint)
        {
            if (_equipmentToControl == null)
                return;

            LineString line = new LineString(new Coordinate[] { leftPoint, rightPoint });
            List<double> rates = new List<double>();
            foreach (var feature in _shapeFile.Features)
            {
                if (feature.FeatureType == FeatureType.Polygon && feature.BasicGeometry is IPolygon)
                {
                    IPolygon poly = (IPolygon)feature.BasicGeometry;
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
                _equipmentToControl.SetRate(_currentRate);
            }
        }
        
        public void RegisterEquipmentControl(IEquipmentControl equipment)
        {
            _equipmentToControl = equipment;
            _equipmentToControl.SetRate(_currentRate);
        }

        #endregion
    }
}
