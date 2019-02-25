using System.Collections.Generic;

namespace FarmingGPS.Database
{
    public class DatabaseHelperClass
    {

        public struct SubField
        {
            public List<GpsCoordinate> Field;
            public int FieldCut1;
            public int FieldCut2;
        }

        public static List<GpsCoordinate> GetSubfield(FieldCut fieldCut1, bool? fieldCut1Inv, FieldCut fieldCut2, bool? fieldCut2Inv, List<GpsCoordinate> coordinates)
        {
            if (fieldCut1 != null)
            {
                int first = coordinates.IndexOf(fieldCut1.GpsCoordinateFirst);
                int second = coordinates.IndexOf(fieldCut1.GpsCoordinateSecond);
                if (first > second)
                {
                    int temp = first;
                    first = second;
                    second = temp;
                }

                List<GpsCoordinate> otherCoordinates = new List<GpsCoordinate>();
                for (int i = first; i <= second; i++)
                    otherCoordinates.Add(coordinates[i]);

                for (int i = 1; i < otherCoordinates.Count - 1; i++)
                    coordinates.Remove(otherCoordinates[i]);
                if (fieldCut1Inv.Value)
                    coordinates = otherCoordinates;
            }

            if (fieldCut2 != null)
            {
                int first = coordinates.IndexOf(fieldCut2.GpsCoordinateFirst);
                int second = coordinates.IndexOf(fieldCut2.GpsCoordinateSecond);
                if (first > second)
                {
                    int temp = first;
                    first = second;
                    second = temp;
                }

                List<GpsCoordinate> otherCoordinates = new List<GpsCoordinate>();
                for (int i = first; i <= second; i++)
                    otherCoordinates.Add(coordinates[i]);

                for (int i = 1; i < otherCoordinates.Count - 1; i++)
                    coordinates.Remove(otherCoordinates[i]);
                if (fieldCut2Inv.Value)
                    coordinates = otherCoordinates;
            }

            return coordinates;
        }

        public static List<SubField> GetSubfields(FieldCut[] fieldCuts, List<GpsCoordinate> coordinates)
        {
            List<SubField> subFields = new List<SubField>();
            if (coordinates.Count < 3)
                return subFields;
            
            subFields.Add(new SubField() { Field = new List<GpsCoordinate>(coordinates), FieldCut1 = 0, FieldCut2 = 0 });
            if (fieldCuts.Length > 0)
            {
                foreach (FieldCut fieldCut in fieldCuts)
                {
                    int subFieldToCut = -1;
                    for (int i = 0; i < subFields.Count; i++)
                    {
                        if (subFields[i].Field.Contains(fieldCut.GpsCoordinateFirst))
                        {
                            subFieldToCut = i;
                            break;
                        }
                    }
                    if (subFieldToCut < 0)
                        continue;

                    SubField subField = subFields[subFieldToCut];
                    int first = subField.Field.IndexOf(fieldCut.GpsCoordinateFirst);
                    int second = subField.Field.IndexOf(fieldCut.GpsCoordinateSecond);
                    if (second == -1)
                        continue;
                    if (first > second)
                    {
                        int temp = first;
                        first = second;
                        second = temp;
                        if (subField.FieldCut1 == 0)
                            subField.FieldCut1 = fieldCut.FieldCutId * -1;
                        else
                            subField.FieldCut2 = fieldCut.FieldCutId * -1;
                    }
                    else
                    {
                        if (subField.FieldCut1 != 0)
                            subField.FieldCut2 = fieldCut.FieldCutId;
                        else
                            subField.FieldCut1 = fieldCut.FieldCutId;
                    }

                    SubField newSubField = new SubField(){ Field = new List<GpsCoordinate>(), FieldCut1 = (subField.FieldCut2 == 0 ? subField.FieldCut1 * -1: subField.FieldCut2 * -1), FieldCut2 = 0 };
                    for (int i = first; i <= second; i++)
                        newSubField.Field.Add(subField.Field[i]);

                    for (int i = 1; i < newSubField.Field.Count - 1; i++)
                        subField.Field.Remove(newSubField.Field[i]);

                    subFields[subFieldToCut] = subField;
                    subFields.Add(newSubField);
                }
            }
            return subFields;
        }
    }
}
