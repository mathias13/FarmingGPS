using System;

namespace FarmingGPS.Database
{
    partial class Obstacle
    {
        public enum ObstacleType
        {
            ROCK = 0,
            WELL = 1
        }
    }

    partial class CropProductionPlan
    {
    }

    partial class CropYear
    {
        public override string ToString()
        {
            return Year.ToString();
        }
    }

    partial class EquipmentRateFile
    {
        public override string ToString()
        {
            return Added.ToString("yyyy-MM-dd");
        }
    }

    partial class Nutrient
    {
        public override string ToString()
        {
            return Name;
        }
    }

    partial class Drainage
    {
        public override string ToString()
        {
            return Name;
        }
    }

    partial class SeedType
    {
        public override string ToString()
        {
            return Name;
        }
    }

    partial class FertilizerType
    {
        public override string ToString()
        {
            return Name;
        }
    }

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

    partial class FieldCut
    {
        public override string ToString()
        {
            return Name;
        }
    }

    partial class GpsCoordinate
    {
        public override string ToString()
        {
            return String.Format("N{0}° E{1}° H{2}m", this.Latitude.ToString("0.00000"),
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