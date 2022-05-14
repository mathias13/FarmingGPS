using DotSpatial.Positioning;
using FarmingGPS.Usercontrols.GoogleMaps;
using FarmingGPS.Dialogs;
using FarmingGPSLib.FieldItems;
using FarmingGPSLib.Settings;
using GeoAPI.Geometries;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using GpsUtilities.HelperClasses;
using log4net;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FarmingGPS.Usercontrols
{
    /// <summary>
    /// Interaction logic for FieldIndent.xaml
    /// </summary>
    public partial class FieldIndent : UserControl, ISettingsChanged
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IField _fieldChoosen;

        private IField _newField;

        private GMapPolygon _fieldPolygon;

        private GMapMarker _selectedMarker;

        private GMapMarker _startMarker;

        private GMapMarker _endMarker;

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
                _fieldPolygon = new GMapPolygon(points);
                Path path = new Path();
                path.Fill = Brushes.Blue;
                path.Stroke = Brushes.Yellow;
                path.Opacity = 0.3;
                _fieldPolygon.Shape = path;
                GMapControl.Markers.Add(_fieldPolygon);
                RectLatLng rect = RectLatLng.FromLTRB(left, top, right, bottom);
                GMapControl.SetZoomToFitRect(rect);

                _selectedMarker = new GMapMarker(new PointLatLng(_fieldChoosen.BoundaryPositions[0].Latitude.DecimalDegrees, _fieldChoosen.BoundaryPositions[0].Longitude.DecimalDegrees));
                _selectedMarker.Shape = new MarkerBlueDot(_selectedMarker);
                _selectedMarker.Shape.Visibility = Visibility.Hidden;
                _startMarker = new GMapMarker(new PointLatLng(_fieldChoosen.BoundaryPositions[0].Latitude.DecimalDegrees, _fieldChoosen.BoundaryPositions[0].Longitude.DecimalDegrees));
                _startMarker.Shape = new MarkerGreenDot(_startMarker);
                _startMarker.Shape.Visibility = Visibility.Hidden;
                _endMarker = new GMapMarker(new PointLatLng(_fieldChoosen.BoundaryPositions[0].Latitude.DecimalDegrees, _fieldChoosen.BoundaryPositions[0].Longitude.DecimalDegrees));
                _endMarker.Shape = new MarkerRedDot(_endMarker);
                _endMarker.Shape.Visibility = Visibility.Hidden;
                GMapControl.Markers.Add(_selectedMarker);
                GMapControl.Markers.Add(_startMarker);
                GMapControl.Markers.Add(_endMarker);
            }
        }

        private void ButtonChooseStart_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxPoints.SelectedItem != null)
            {
                Position selectedPoint = (Position)ListBoxPoints.SelectedItem;
                _startMarker.Position = new PointLatLng(selectedPoint.Latitude.DecimalDegrees, selectedPoint.Longitude.DecimalDegrees);
                _startMarker.Shape.Visibility = Visibility.Visible;
                _positionStart = (Position)ListBoxPoints.SelectedItem;
            }
            else
                _startMarker.Shape.Visibility = Visibility.Hidden;
        }

        private void ButtonChooseEnd_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxPoints.SelectedItem != null)
            {
                Position selectedPoint = (Position)ListBoxPoints.SelectedItem;
                _endMarker.Position = new PointLatLng(selectedPoint.Latitude.DecimalDegrees, selectedPoint.Longitude.DecimalDegrees);
                _endMarker.Shape.Visibility = Visibility.Visible;
                _positionEnd = (Position)ListBoxPoints.SelectedItem;
            }
            else
                _endMarker.Shape.Visibility = Visibility.Hidden;
        }

        private void ButtonFinished_Click(object sender, RoutedEventArgs e)
        {
            if(_positionStart != null && _positionEnd != null)
            {
                try
                {
                    var startIndex = _fieldChoosen.BoundaryPositions.IndexOf(_positionStart);
                    var endIndex = _fieldChoosen.BoundaryPositions.IndexOf(_positionEnd);
                    var startCoord = _fieldChoosen.Polygon.Coordinates[startIndex];
                    var endCoord = _fieldChoosen.Polygon.Coordinates[endIndex];
                    var lastDistanceStartCoord = double.MaxValue;
                    var lastDistanceEndCoord = double.MaxValue;

                    var curveBuilder = new OffsetCurveBuilder(new PrecisionModel(PrecisionModels.Floating), new BufferParameters());
                    var ringCoords = curveBuilder.GetRingCurve(_fieldChoosen.Polygon.Coordinates, NetTopologySuite.GeometriesGraph.Positions.Left, NumericDistance.Value.Value);

                    var startIndexRing = 0;
                    var endIndexRing = 0;
                    for (int i = 1; i < ringCoords.Length; i++)
                    {
                        if (ringCoords[i].Distance(endCoord) < lastDistanceEndCoord)
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
                        if (newCoords.Count == 0)
                            newCoords.Add(ringCoords[i]);
                        else if (newCoords[newCoords.Count - 1] != ringCoords[i])
                            newCoords.Add(ringCoords[i]);
                    }
                    newCoords.Add(ringCoords[endIndexRing]);

                    var startSegment = new NetTopologySuite.Geometries.LineSegment(newCoords[1], newCoords[0]);
                    var endSegment = new NetTopologySuite.Geometries.LineSegment(newCoords[newCoords.Count - 2], newCoords[newCoords.Count - 1]);

                    startSegment = new NetTopologySuite.Geometries.LineSegment(HelperClassCoordinate.ComputePoint(startSegment.P1, startSegment.Angle, NumericDistance.Value.Value * 2.0), startSegment.P1);
                    endSegment = new NetTopologySuite.Geometries.LineSegment(endSegment.P1, HelperClassCoordinate.ComputePoint(endSegment.P1, endSegment.Angle, NumericDistance.Value.Value * 2.0));

                    var newStartIndex = startIndex;
                    var newEndIndex = endIndex;
                    Coordinate startIntersection = null;
                    Coordinate endIntersection = null;
                    for (int i = endIndex; i != startIndex; i++)
                    {
                        if (i > _fieldChoosen.Polygon.Shell.Coordinates.Length - 2)
                        {
                            i = -1;
                            continue;
                        }

                        var p1Index = i + 1;
                        if (p1Index > _fieldChoosen.Polygon.Shell.Coordinates.Length - 2)
                            p1Index = 0;

                        if (startIntersection == null)
                        {
                            startIntersection = startSegment.Intersection(new NetTopologySuite.Geometries.LineSegment(_fieldChoosen.Polygon.Coordinates[i], _fieldChoosen.Polygon.Coordinates[p1Index]));
                            if (startIntersection != null)
                                newStartIndex = i;
                        }

                        if (endIntersection == null)
                        {
                            endIntersection = endSegment.Intersection(new NetTopologySuite.Geometries.LineSegment(_fieldChoosen.Polygon.Coordinates[i], _fieldChoosen.Polygon.Coordinates[p1Index]));
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
                        if (i > _fieldChoosen.Polygon.Shell.Coordinates.Length - 1)
                        {
                            i = -1;
                            continue;
                        }

                        if (!newCoords[newCoords.Count - 1].Equals2D(_fieldChoosen.Polygon.Shell.Coordinates[i]))
                            newCoords.Add(_fieldChoosen.Polygon.Shell.Coordinates[i]);
                    }
                    newCoords.Add(_fieldChoosen.Polygon.Shell.Coordinates[newStartIndex]);

                    if (newCoords[0] != newCoords[newCoords.Count - 1])
                        newCoords.Add(newCoords[0]);

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
                    path.Opacity = 0.7;
                    polygon.Shape = path;
                    GMapControl.Markers.Add(polygon);
                    ((Path)_fieldPolygon.Shape).Fill = Brushes.Red;
                    ((Path)_fieldPolygon.Shape).Opacity = 0.7;

                    _newField = new Field(positions, _fieldChoosen.Projection);

                    if (_newField != null)
                        if (SettingChanged != null)
                            SettingChanged.Invoke(this, FIELD_CHANGED);
                }
                catch(Exception e1)
                {
                    Log.Error(e1);
                    OKDialog dialog = new OKDialog("Det gick inte att skapa en kantzon");
                    dialog.Show();
                }
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
