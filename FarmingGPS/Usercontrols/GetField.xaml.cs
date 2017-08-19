using FarmingGPS.Database;
using FarmingGPSLib;
using FarmingGPSLib.DatabaseHelper;
using DotSpatial.Positioning;
using DotSpatial.Projections;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
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

namespace FarmingGPS.Usercontrols
{
    /// <summary>
    /// Interaction logic for GetField.xaml
    /// </summary>
    public partial class GetField : UserControl
    {
        private DatabaseHandler _database;

        private List<Position> _fieldChoosen;

        public event EventHandler<List<Position>> FieldChoosen;

        public GetField()
        {
            InitializeComponent();
            GMapControl.Bearing = 0F;
            GMapControl.CanDragMap = true;
            GMapControl.EmptytileBrush = Brushes.Navy;
            GMapControl.HelperLineOption = GMap.NET.WindowsPresentation.HelperLineOptions.DontShow;
            GMapControl.LevelsKeepInMemmory = 5;
            GMapControl.MaxZoom = 18;
            GMapControl.MinZoom = 14;
            GMapControl.MouseWheelZoomEnabled = true;
            GMapControl.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.ViewCenter;
            GMapControl.RetryLoadTile = 0;
            GMapControl.ScaleMode = GMap.NET.WindowsPresentation.ScaleModes.Integer;
            GMapControl.SelectedAreaFill = new SolidColorBrush(Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(65)))), ((int)(((byte)(105)))), ((int)(((byte)(225))))));
            GMapControl.ShowTileGridLines = false;
            GMapControl.TabIndex = 0;
            GMapProvider provider = GoogleSatelliteMapProvider.Instance;
            GMapControl.MapProvider = provider;
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            GMapControl.CenterCrossPen = new Pen(Brushes.White, 0.0);

            ListBoxFields.SelectionChanged += ListBoxFields_SelectionChanged;
            ListBoxIntersects.SelectionChanged += ListBoxIntersects_SelectionChanged;
        }

        public void AddDatabase(DatabaseHandler database)
        {
            _database = database;
            Field[] _fields = _database.GetFields();
            if (_fields != null)
                foreach (Field field in _fields)
                    ListBoxFields.Items.Add(field);
        }

        private void ListBoxFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBoxIntersects.Items.Clear();
            if (ListBoxFields.SelectedItem != null)
            {
                Field field = ListBoxFields.SelectedItem as Field;
                SubFieldIntersect[] intersects = _database.GetIntersects(field.FieldId);
                foreach (SubFieldIntersect intersect in intersects)
                    ListBoxIntersects.Items.Add(intersect);
            }
            ReloadFields();
        }

        private void ListBoxIntersects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReloadFields();
        }

        private void ReloadFields()
        {
            _fieldChoosen = null;

            if (ListBoxFields.SelectedItem != null)
            {
                double left = double.MaxValue;
                double top = double.MinValue;
                double right = double.MinValue;
                double bottom = double.MaxValue;
                GMapControl.Markers.Clear();
                Field field = ListBoxFields.SelectedItem as Field;
                List<GpsCoordinate> coordinates = new List<GpsCoordinate>();
                coordinates.AddRange(_database.GetCoordinatesForField(field.FieldId));
                List<GMapPolygon> polygons = new List<GMapPolygon>();
                List<GMapMarker> markers = new List<GMapMarker>();
                List<SubFieldIntersect> intersects = new List<SubFieldIntersect>();
                foreach (object intersect in ListBoxIntersects.SelectedItems)
                    intersects.Add(intersect as SubFieldIntersect);
                List<List<GpsCoordinate>> subFields = DatabaseHelperClass.GetSubfields(intersects.ToArray(), coordinates);
                foreach (List<GpsCoordinate> subField in subFields)
                {
                    List<PointLatLng> points = new List<PointLatLng>();
                    List<Position> pointsField = new List<Position>();
                    foreach (GpsCoordinate coord in subField)
                    {
                        points.Add(new PointLatLng(coord.Latitude, coord.Longitude));
                        left = Math.Min(left, coord.Longitude);
                        right = Math.Max(right, coord.Longitude);
                        bottom = Math.Min(bottom, coord.Latitude);
                        top = Math.Max(top, coord.Latitude);
                        pointsField.Add(new Position(new Latitude(coord.Latitude), new Longitude(coord.Longitude)));
                    }
                    pointsField.Add(pointsField[0].Clone());
                    GMapPolygon polygon = new GMapPolygon(points);
                    polygon.Tag = pointsField;
                    Path path = new Path();
                    path.Fill = Brushes.Blue;
                    path.Stroke = Brushes.DarkBlue;
                    path.Opacity = 0.5;
                    polygon.Shape = path;
                    GMapControl.Markers.Add(polygon);
                    path.MouseLeftButtonUp += Path_MouseLeftButtonUp;
                }
                RectLatLng rect = RectLatLng.FromLTRB(left, top, right, bottom);
                GMapControl.SetZoomToFitRect(rect);
            }
        }

        private void Path_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            foreach(GMapMarker marker in GMapControl.Markers)
            {
                if(marker is GMapPolygon)
                {
                    Path path = marker.Shape as Path;
                    if (sender.Equals(path))
                    {
                        path.Fill = Brushes.Green;
                        _fieldChoosen = marker.Tag as List<Position>;
                    }
                    else
                        path.Fill = Brushes.Blue;
                }
            }
        }

        private void ButtonChoose_Click(object sender, RoutedEventArgs e)
        {
            if (_fieldChoosen != null)
                if (FieldChoosen != null)
                    FieldChoosen.Invoke(this, _fieldChoosen);
        }
    }
}
