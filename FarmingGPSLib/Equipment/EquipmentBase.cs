using System;
using DotSpatial.Positioning;

namespace FarmingGPSLib.Equipment
{
    public class EquipmentBase: IEquipment
    {
        #region Private Variables

        protected Distance _width;

        protected Distance _distanceFromVechile;

        protected Distance _overlap;

        protected Angle _fromDirectionOfTravel;

        #endregion

        public EquipmentBase(Distance width, Distance distanceFromVechile, Angle fromDirectionOfTravel)
        {
            _width = width;
            _distanceFromVechile = distanceFromVechile;
            _fromDirectionOfTravel = fromDirectionOfTravel;
            _overlap = Distance.FromMeters(0);
        }

        public EquipmentBase(Distance width, Distance distanceFromVechile, Angle fromDirectionOfTravel, Distance overlap)
        {
            _width = width;
            _distanceFromVechile = distanceFromVechile;
            _fromDirectionOfTravel = fromDirectionOfTravel;
            if (overlap > width.Divide(2))
                _overlap = width.Divide(2);
            else
                _overlap = overlap;
        }

        #region IEquipment Implementation

        public Distance DistanceFromVechile
        {
            get { return _distanceFromVechile; }
        }

        public Angle FromDirectionOfTravel
        {
            get { return _fromDirectionOfTravel; }
        }

        public Distance Width
        {
            get { return _width; }
        }

        public Distance Overlap
        {
            get { return _overlap; }
        }

        public Distance CenterOfWidth
        {
            get
            {
                return _width.Divide(2);
            }
        }

        public Distance CenterToCenter
        {
            get
            {
                return _width.Subtract(_overlap);
            }
        }

        public Distance CenterOfWidthWithOverlap
        {
            get { return CenterOfWidth.Subtract(Overlap); }
        }

        #endregion
    }
}
