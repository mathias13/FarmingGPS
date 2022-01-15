using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using FarmingGPSLib.FieldItems;
using FarmingGPSLib.Settings;
using DotSpatial.Positioning;
using DotSpatial.Topology;
using DotSpatial.Topology.Operation.Buffer;
using FarmingGPS.Usercontrols.GoogleMaps;
using GpsUtilities.HelperClasses;

namespace FarmingGPS.Usercontrols
{
    /// <summary>
    /// Interaction logic for FieldIndent.xaml
    /// </summary>
    public partial class FieldIndent : UserControl, ISettingsChanged
    {
        private IField _fieldChoosen;

        private IField _newField;

        private GMapMarker _selectedMarker;

        private Position _positionStart;

        private Position _positionEnd;

        public const string FIELD_CHANGED = "FIELD_CHANGED";

        public FieldIndent()
        {
            InitializeComponent();
            GMapControl.Bearing = 0F;
            GMapControl.CanDragMap = true;
            GMapControl.EmptytileBrush = Brushes.Navy;
            GMapControl.HelperLineOption = HelperLineOptions.DontShow;
            GMapControl.LevelsKeepInMemmory = 5;
            GMapControl.MaxZoom = 18;
            GMapControl.MinZoom = 14;
            GMapControl.MouseWheelZoomEnabled = true;
            GMapControl.MouseWheelZoomType = MouseWheelZoomType.ViewCenter;
            GMapControl.RetryLoadTile = 0;
            GMapControl.ScaleMode = ScaleModes.Integer;
            GMapControl.SelectedAreaFill = new SolidColorBrush(Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(65)))), ((int)(((byte)(105)))), ((int)(((byte)(225))))));
            GMapControl.ShowTileGridLines = false;
            GMapControl.TabIndex = 0;
            GMapProvider provider = GoogleSatelliteMapProvider.Instance;
            GMapControl.MapProvider = provider;
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            GMapControl.CenterCrossPen = new Pen(Brushes.White, 0.0);

            ListBoxPoints.SelectionChanged += ListBoxPoints_SelectionChanged;

            ReloadField();
        }

        private void ListBoxPoints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBoxPoints.SelectedItem != null)
            {
                Position selectedPoint = (Position)ListBoxPoints.SelectedItem;
                _selectedMarker.Position = new PointLatLng(selectedPoint.Latitude.DecimalDegrees, selectedPoint.Longitude.DecimalDegrees);
                _selectedMarker.Shape.Visibility = Visibility.Visible;
            }
            else
                _selectedMarker.Shape.Visibility = Visibility.Hidden;
        }

        private void ReloadField()
        {
            if (_fieldChoosen != null)
            {
                double left = double.MaxValue;
                double top = double.MinValue;
                double right = double.MinValue;
                double bottom = double.MaxValue;
                GMapControl.Markers.Clear();
                List<PointLatLng> points = new List<PointLatLng>();
                foreach (Position coord in _fieldChoosen.BoundaryPositions)
                {
                    points.Add(new PointLatLng(coord.Latitude.DecimalDegrees, coord.Longitude.DecimalDegrees));
                    points.Add(new PointLatLng(coord.Latitude.DecimalDegrees, coord.Longitude.DecimalDegrees));
                    left = Math.Min(left, coord.Longitude.DecimalDegrees);
                    right = Math.Max(right, coord.Longitude.DecimalDegrees);
                    bottom = Math.Min(bottom, coord.Latitude.DecimalDegrees);
                    top = Math.Max(top, coord.Latitude.DecimalDegrees);
                }
                GMapPolygon polygon = new GMapPolygon(points);
                Path path = new Path();
                path.Fill = Brushes.Red;
                path.Stroke = Brushes.Yellow;
                path.Opacity = 0.5;
                polygon.Shape = path;
                GMapControl.Markers.Add(polygon);
                RectLatLng rect = RectLatLng.FromLTRB(left, top, right, bottom);
                GMapControl.SetZoomToFitRect(rect);

                _selectedMarker = new GMapMarker(new PointLatLng(_fieldChoosen.BoundaryPositions[0].Latitude.DecimalDegrees, _fieldChoosen.BoundaryPositions[0].Longitude.DecimalDegrees));
                _selectedMarker.Shape = new MarkerRedDot(_selectedMarker);
                _selectedMarker.Shape.Visibility = Visibility.Hidden;
                GMapControl.Markers.Add(_selectedMarker);
            }
        }

        private void ButtonChooseStart_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxPoints.SelectedItem != null)
                _positionStart = (Position)ListBoxPoints.SelectedItem;
        }

        private void ButtonChooseEnd_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxPoints.SelectedItem != null)
                _positionEnd = (Position)ListBoxPoints.SelectedItem;
        }

        private void ButtonFinished_Click(object sender, RoutedEventArgs e)
        {
            if(_positionStart != null && _positionEnd != null)
            {
                var startIndex = _fieldChoosen.BoundaryPositions.IndexOf(_positionStart);
                var endIndex = _fieldChoosen.BoundaryPositions.IndexOf(_positionEnd);
                var startCoord = _fieldChoosen.Polygon.Coordinates[startIndex];
                var endCoord = _fieldChoosen.Polygon.Coordinates[endIndex];
                var lastDistanceStartCoord = double.MaxValue;
                var lastDistanceEndCoord = double.MaxValue;

                var curveBuilder = new OffsetCurveBuilder(new PrecisionModel(PrecisionModelType.Floating));
                var list = curveBuilder.GetRingCurve(_fieldChoosen.Polygon.Coordinates, DotSpatial.Topology.GeometriesGraph.PositionType.Left, NumericDistance.Value.Value);

                var startIndexRing = 0;
                var endIndexRing = 0;
                var ringCoords = ((IEnumerable<Coordinate>)list[0]).ToArray();
                for (int i = 1; i < ringCoords.Length; i++)
                {
                    if(ringCoords[i].Distance(endCoord) < lastDistanceEndCoord)
                    {
                        lastDistanceEndCoord = ringCoords[i].Distance(endCoord);
                        endIndexRing = i;
                    }
                    if (ringCoords[i].Distance(startCoord) < lastDistanceStartCoord)
                    {
                        lastDistanceStartCoord = ringCoords[i].Distance(startCoord);
                        startIndexRing = i;
                    }
                }

                var newCoords = new List<Coordinate>();
                for (int i = startIndexRing; i != endIndexRing; i++)
                {
                    if (i > ringCoords.Count() - 2)
                    {
                        i = -1;
                        continue;
                    }
                    if(newCoords.Count == 0)
                        newCoords.Add(ringCoords[i]);
                    else if (newCoords[newCoords.Count - 1] != ringCoords[i])
                        newCoords.Add(ringCoords[i]);
                }
                newCoords.Add(ringCoords[endIndexRing]);

                var startSegment = new DotSpatial.Topology.LineSegment(newCoords[1], newCoords[0]);
                var endSegment = new DotSpatial.Topology.LineSegment(newCoords[newCoords.Count - 2], newCoords[newCoords.Count - 1]);

                startSegment = new DotSpatial.Topology.LineSegment(HelperClassCoordinate.ComputePoint(startSegment.P1, startSegment.Angle, NumericDistance.Value.Value * 2.0), startSegment.P1);
                endSegment = new DotSpatial.Topology.LineSegment(endSegment.P1, HelperClassCoordinate.ComputePoint(endSegment.P1, endSegment.Angle, NumericDistance.Value.Value * 2.0));

                var newStartIndex = startIndex;
                var newEndIndex = endIndex;
                Coordinate startIntersection = null;
                Coordinate endIntersection = null;
                for (int i = endIndex; i != startIndex; i++)
                {
                    if (i > _fieldChoosen.Polygon.Shell.Coordinates.Count - 2)
                    {
                        i = -1;
                        continue;
                    }
                    
                    var p1Index = i + 1;
                    if (p1Index > _fieldChoosen.Polygon.Shell.Coordinates.Count - 2)
                        p1Index = 0;

                    if (startIntersection == null)
                    {
                        startIntersection = startSegment.Intersection(new DotSpatial.Topology.LineSegment(_fieldChoosen.Polygon.Coordinates[i], _fieldChoosen.Polygon.Coordinates[p1Index]));
                        if (startIntersection != null)
                            newStartIndex = i;
                    }

                    if (endIntersection == null)
                    {
                        endIntersection = endSegment.Intersection(new DotSpatial.Topology.LineSegment(_fieldChoosen.Polygon.Coordinates[i], _fieldChoosen.Polygon.Coordinates[p1Index]));
                        if (endIntersection != null)
                            newEndIndex = p1Index;
                    }
                }

                if (startIntersection != null)
                    newCoords.Insert(0, startIntersection);
                else
                    newCoords.Insert(0, startCoord);

                if (endIntersection != null)
                    newCoords[newCoords.Count - 1] = endIntersection;
                else
                    newCoords.Add(endCoord);

                for (int i = newEndIndex; i != newStartIndex; i++)
                {
                    if (i > _fieldChoosen.Polygon.Shell.Coordinates.Count - 1)
                    {
                        i = -1;
                        continue;
                    }

                    if (newCoords[newCoords.Count - 1] != _fieldChoosen.Polygon.Shell.Coordinates[i])
                        newCoords.Add(_fieldChoosen.Polygon.Shell.Coordinates[i]);
                }
                newCoords.Add(_fieldChoosen.Polygon.Shell.Coordinates[newStartIndex]);

                if(newCoords[0] != newCoords[newCoords.Count - 1])
                    newCoords.Add(newCoords[0]);

                var ring = new LinearRing(newCoords);
                ring.Reverse();

                var newPolygon = new DotSpatial.Topology.Polygon(ring.Coordinates);

                List<double> xy = new List<double>();
                List<double> z = new List<double>();
                foreach (Coordinate coord in newCoords)
                {
                    xy.Add(coord.X);
                    xy.Add(coord.Y);
                    z.Add(Distance.EarthsAverageRadius.ToMeters().Value);
                }

                var xyArray = xy.ToArray();
                var zArray = z.ToArray();
                DotSpatial.Projections.Reproject.ReprojectPoints(xyArray, zArray, _fieldChoosen.Projection, DotSpatial.Projections.KnownCoordinateSystems.Geographic.World.WGS1984, 0, z.Count);

                List<PointLatLng> points = new List<PointLatLng>();
                List<Position> positions = new List<Position>();
                for (int i = 0; i < zArray.Length; i++)
                {
                    positions.Add(new Position(new Latitude(xyArray[i * 2 + 1]), new Longitude(xyArray[i * 2])));
                    points.Add(new PointLatLng(xyArray[i * 2 + 1], xyArray[i * 2]));
                }

                GMapPolygon polygon = new GMapPolygon(points);
                Path path = new Path();
                path.Fill = Brushes.Green;
                path.Stroke = Brushes.Yellow;
                path.Opacity = 0.5;
                polygon.Shape = path;
                GMapControl.Markers.Add(polygon);

                _newField = new Field(positions, _fieldChoosen.Projection);

                if (_newField != null)
                    if (SettingChanged != null)
                        SettingChanged.Invoke(this, FIELD_CHANGED);
            }
        }

        #region ISettingsChanged

        public event EventHandler<string> SettingChanged;

        public void RegisterSettingEvent(ISettingsChanged settings)
        {
            settings.SettingChanged += Settings_SettingChanged;
        }

        private void Settings_SettingChanged(object sender, string e)
        {
            if (sender is GetField && e == GetField.FIELD_CHOOSEN)
            {
                GetField field = sender as GetField;
                DotSpatial.Projections.ProjectionInfo projection = GpsUtilities.Reciever.HelperClass.GetUtmProjectionZone(field.FieldChoosen[0]);
                _fieldChoosen = new Field(field.FieldChoosen, projection);
                ListBoxPoints.Items.Clear();
                foreach (Position position in field.FieldChoosen)
                    ListBoxPoints.Items.Add(position);
            }

            ReloadField();
        }

        #endregion

        public IList<Position> FieldChoosen
        {
            get { return _newField.BoundaryPositions; }
        }
    }
}
