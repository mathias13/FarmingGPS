using System.Collections.Generic;

namespace FarmingGPS.Database
{
    public class DatabaseHelperClass
    {
        public static List<List<GpsCoordinate>> GetSubfields(FieldCut[] fieldCuts, List<GpsCoordinate> coordinates)
        {
            List<List<GpsCoordinate>> subFields = new List<List<GpsCoordinate>>();
            if (coordinates.Count < 3)
                return subFields;
            subFields.Add(coordinates);
            if (fieldCuts.Length > 0)
            {
                foreach (FieldCut fieldCut in fieldCuts)
                {
                    List<GpsCoordinate> listToCut = null;
                    foreach (List<GpsCoordinate> list in subFields)
                    {
                        if (list.Contains(fieldCut.GpsCoordinateFirst))
                        {
                            listToCut = list;
                            break;
                        }
                    }
                    if (listToCut == null)
                        continue;
                    int first = listToCut.IndexOf(fieldCut.GpsCoordinateFirst);
                    int second = listToCut.IndexOf(fieldCut.GpsCoordinateSecond);
                    if (second == -1)
                        continue;
                    if (first > second)
                    {
                        int temp = first;
                        first = second;
                        second = temp;
                    }

                    List<GpsCoordinate> newList = new List<GpsCoordinate>();
                    for (int i = first; i <= second; i++)
                        newList.Add(listToCut[i]);

                    for (int i = 1; i < newList.Count - 1; i++)
                        listToCut.Remove(newList[i]);
                    subFields.Add(newList);
                }
            }
            return subFields;
        }
    }
}
