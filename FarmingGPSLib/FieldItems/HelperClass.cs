using DotSpatial.Projections;
using DotSpatial.Positioning;
using System;


namespace FarmingGPSLib.FieldItems
{
    public class HelperClass
    {
        public static ProjectionInfo GetUtmProjectionZone(Position3D position)
        { 
            return GetUtmProjectionZone(new Position(position.Latitude, position.Longitude)); 
        }

        public static ProjectionInfo GetUtmProjectionZone(Position postion)
        {
            int utmZone = (int)Math.Ceiling((postion.Longitude.DecimalDegrees - -180.0) / 6.0);
            bool north = postion.Latitude >= 0.0;
            switch(utmZone)
            {
                case 1:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone1N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone1S;

                case 2:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone2N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone2S;

                case 3:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone3N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone3S;

                case 4:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone4N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone4S;

                case 5:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone5N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone5S;

                case 6:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone6N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone6S;

                case 7:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone7N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone7S;

                case 8:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone8N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone8S;

                case 9:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone9N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone9S;

                case 10:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone10N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone10S;

                case 11:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone11N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone11S;

                case 12:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone12N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone12S;

                case 13:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone13N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone13S;

                case 14:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone14N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone14S;

                case 15:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone15N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone15S;

                case 16:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone16N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone16S;

                case 17:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone17N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone17S;

                case 18:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone18N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone18S;

                case 19:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone19N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone19S;

                case 20:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone20N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone20S;

                case 21:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone21N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone21S;

                case 22:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone22N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone22S;

                case 23:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone23N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone23S;

                case 24:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone24N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone24S;

                case 25:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone25N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone25S;

                case 26:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone26N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone26S;

                case 27:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone27N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone27S;

                case 28:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone28N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone28S;

                case 29:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone29N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone29S;

                case 30:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone30N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone30S;

                case 31:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone31N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone31S;

                case 32:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone32N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone32S;

                case 33:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33S;

                case 34:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone34N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone34S;

                case 35:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone35N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone35S;

                case 36:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone36N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone36S;

                case 37:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone37N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone37S;

                case 38:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone38N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone38S;

                case 39:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone39N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone39S;

                case 40:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone40N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone40S;

                case 41:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone41N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone41S;

                case 42:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone42N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone42S;

                case 43:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone43N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone43S;

                case 44:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone44N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone44S;

                case 45:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone45N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone45S;

                case 46:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone46N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone46S;

                case 47:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone47N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone47S;

                case 48:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone48N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone48S;

                case 49:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone49N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone49S;

                case 50:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone50N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone50S;

                case 51:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone51N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone51S;

                case 52:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone52N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone52S;

                case 53:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone53N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone53S;

                case 54:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone54N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone54S;

                case 55:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone55N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone55S;

                case 56:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone56N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone56S;

                case 57:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone57N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone57S;

                case 58:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone58N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone58S;

                case 59:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone59N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone59S;

                case 60:
                    return north ? KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone60N : KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone60S;

                default:
                    return KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone1N;
            }
        }
    }
}
