using System;

namespace FarmingGPS.Database
{
    partial class Equipment
    {
        public override string ToString()
        {
            return Name;
        }
    }

    partial class VechileAttach
    {
        public override string ToString()
        {
            return Name;
        }
    }

    partial class Vechile
    {
        public override string ToString()
        {
            return String.Format("{0} {1}", Manufacturer, Model);
        }
    }

    partial class SubFieldIntersect
    {
        public override string ToString()
        {
            return String.Format("{0} {1}", GpsCoordinateFirst, GpsCoordinateSecond);
        }
    }

    partial class GpsCoordinate
    {
        public override string ToString()
        {
            return String.Format("N{0}� E{1}� H{2}m", this.Latitude.ToString("0.00000"),
                                                    this.Longitude.ToString("0.00000"),
                                                    this.Altitude.ToString("0.00"));
        }
    }

    partial class Field
    {
        public override string ToString()
        {
            return this.FieldName;
        }
    }
}