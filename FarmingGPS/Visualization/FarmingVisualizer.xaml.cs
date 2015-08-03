using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using HelixToolkit.Wpf;
using DotSpatial.Positioning;
using FarmingGPSLib.Positioning;
using FarmingGPSLib.FarmingModes;
using FarmingGPSLib.FieldItems;
using FarmingGPSLib.FarmingModes.Tools;

namespace FarmingGPS.Visualization
{
    /// <summary>
    /// Interaction logic for FarmingVisualizer.xaml
    /// </summary>
    public partial class FarmingVisualizer : UserControl
    {
        //TODO cleanup delegates
        protected delegate void UpdatePositionDelegate1(Point position, double angle);

        protected delegate void UpdatePositionDelegate(DotSpatial.Topology.Coordinate coord, Azimuth angle);

        protected delegate void SetEquipmentWidthDelegate(Distance width);

        protected delegate void AddFieldDelegate(IField field);
        
        protected delegate void LineDelegate(TrackingLine line);

        protected delegate void LineDelegateColor(TrackingLine line, Brush color);

        protected delegate void PointDelegate(DotSpatial.Topology.Coordinate coord, Color color);

        protected delegate void TrackingLineEventDelegate(object sender, bool value);

        protected delegate void TrackPolygonUpdatedDelegate(object sender, PolygonUpdatedEventArgs e);

        #region Consts

        private const double LINE_THICKNESS = 0.05;

        private const double LINE_Z_INDEX = 0.0;

        private const double FIELD_TRACK_HOLES_Z_INDEX = -0.004;

        private const double FIELD_TRACK_Z_INDEX = -0.008;

        private const double FIELD_Z_INDEX = -0.012;

        private readonly Point3D VIEW_TOP_POSTION = new Point3D(2.0, 0.0, 30.0);

        private readonly Vector3D VIEW_TOP_ZOOM_VECTOR = new Vector3D(1.0, 0.0, 10.0);

        private readonly Vector3D VIEW_TOP_LOOK_DIRECTION = new Vector3D(0.0, 0.0, -30.0);

        private readonly Point3D VIEW_BEHIND_POSTION = new Point3D(-30.0, 0.0, 12.0);

        private readonly Vector3D VIEW_BEHIND_ZOOM_VECTOR = new Vector3D(-4.7, 0.0, 1);

        private readonly Vector3D VIEW_BEHIND_LOOK_DIRECTION = new Vector3D(4.7, 0.0, -1.0);

        private const double VIEW_ZOOM_INCREMENT = 0.5;

        private const double VIEW_ZOOM_MIN = 0.0;

        private const double VIEW_ZOOM_MAX = 10.0;

        #endregion

        #region Private Variables

        private Point? _lowerLeft;

        private Point? _upperRight;

        private Point _lastPoint = new Point(0.0, 0.0);

        private DotSpatial.Topology.Coordinate _minPoint;

        private IDictionary<TrackingLine, ModelVisual3D> _trackingLines = new Dictionary<TrackingLine, ModelVisual3D>();

        private object _syncObject = new object();

        private DiffuseMaterial _lineNormalMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Blue));

        private DiffuseMaterial _lineDepletedMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.White));

        private DiffuseMaterial _lineActiveMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Red));

        private SolidColorBrush _lineActiveColor = new SolidColorBrush(Colors.Red);
        
        private Color _fieldFillColor = Colors.Green;

        private Color _fieldOutlineColor = Colors.Black;

        private DiffuseMaterial _fieldTrackMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue));

        private DiffuseMaterial _fieldTrackHoleMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue));
        
        private IDictionary<int, MeshVisual3D> _trackMesh = new Dictionary<int, MeshVisual3D>();

        private IDictionary<int, MeshBuilder> _trackMeshBuilder = new Dictionary<int, MeshBuilder>();

        private IDictionary<int, IList<MeshVisual3D>> _trackMeshHoles = new Dictionary<int, IList<MeshVisual3D>>();

        private bool _viewTopActive = false;

        private double _viewTopZoomLevel = 0.0;

        private double _viewBehindZoomLevel = 0.0;

        #endregion

        public FarmingVisualizer()
        {
            InitializeComponent();
            this.Loaded += FarmingVisualizer_Loaded;
        }

        #region Public Methods

        public void SetBoundary(Point lowerLeft, Point upperRight)
        {
            _lowerLeft = lowerLeft;
            _upperRight = upperRight;
        }

        public void UpdatePosition(Point position, double angle)
        {
            if(!_lowerLeft.HasValue || !_upperRight.HasValue)
                return;

            if(Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                SetValue(ShiftXProperty, position.X);
                SetValue(ShiftYProperty, position.Y);
                angle -= 90;
                SetValue(ShiftHeadingProperty, angle);
                _lastPoint = position;
            }
            else
                Dispatcher.BeginInvoke(new UpdatePositionDelegate1(UpdatePosition), System.Windows.Threading.DispatcherPriority.Render, position, angle);
        }

        public void UpdatePosition(DotSpatial.Topology.Coordinate coord, Azimuth bearing)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                double x = coord.X;
                double y = coord.Y;
                if (_minPoint != null)
                {
                    x -= _minPoint.X;
                    y -= _minPoint.Y;
                }
                SetValue(ShiftXProperty, x);
                SetValue(ShiftYProperty, y);
                bearing = bearing.Subtract(90).Normalize();
                SetValue(ShiftHeadingProperty, bearing.DecimalDegrees);
                _lastPoint = new Point((double)GetValue(ShiftXProperty), (double)GetValue(ShiftYProperty));
            }
            else
                Dispatcher.BeginInvoke(new UpdatePositionDelegate(UpdatePosition), System.Windows.Threading.DispatcherPriority.Render, coord, bearing);
        }
        
        public void SetEquipmentWidth(Distance width)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                double widthDivided = width.ToMeters().Value / 2.0;
                for (int i = 0; i < 4; i++)
                    _equipmentMesh.Positions[i] = new Point3D(_equipmentMesh.Positions[i].X, widthDivided, _equipmentMesh.Positions[i].Z);

                for (int i = 4; i < 8; i++)
                    _equipmentMesh.Positions[i] = new Point3D(_equipmentMesh.Positions[i].X, widthDivided * -1.0, _equipmentMesh.Positions[i].Z);
            }
            else
                Dispatcher.Invoke(new SetEquipmentWidthDelegate(SetEquipmentWidth), System.Windows.Threading.DispatcherPriority.Render, width);
        }

        public void AddPoint(DotSpatial.Topology.Coordinate coord, Color color)
        {
            if (!Dispatcher.Thread.Equals(Thread.CurrentThread))
                Dispatcher.Invoke(new PointDelegate(AddPoint), System.Windows.Threading.DispatcherPriority.Render, coord, color);
            else
            {
                double x = coord.X;
                double y = coord.Y;
                if (_minPoint != null)
                {
                    x -= _minPoint.X;
                    y -= _minPoint.Y;
                }
                PointsVisual3D pointVisual = new PointsVisual3D();
                pointVisual.Points.Add(new Point3D(x, y, 0.1));
                pointVisual.Color = color;
                pointVisual.Size = 2;
                _viewPort.Children.Add(pointVisual);
            }

        }

        public void AddLine(TrackingLine trackingLine)
        {
            if (_trackingLines.ContainsKey(trackingLine))
                return;
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                LineGeometryBuilder builder = new LineGeometryBuilder(_viewPort.Children[0]);
                List<Point3D> linePoints = new List<Point3D>();
                for (int i = 0; i < trackingLine.Points.Count - 1; i++)
                {
                    double x1 = trackingLine.Points[i].X;
                    double y1 = trackingLine.Points[i].Y;
                    double x2 = trackingLine.Points[i + 1].X;
                    double y2 = trackingLine.Points[i + 1].Y;
                    if (_minPoint != null)
                    {
                        x1 -= _minPoint.X;
                        y1 -= _minPoint.Y;
                        x2 -= _minPoint.X;
                        y2 -= _minPoint.Y;
                    }
                    linePoints.Add(new Point3D(x1, y1, LINE_Z_INDEX));
                    linePoints.Add(new Point3D(x2, y2, LINE_Z_INDEX));
                }

                Point3DCollection points = builder.CreatePositions(linePoints, LINE_THICKNESS, 0.0, null);
                Int32Collection indices = builder.CreateIndices(linePoints.Count);

                Mesh3D mesh3D = new Mesh3D(points, indices);
                GeometryModel3D model3D = new GeometryModel3D(mesh3D.ToMeshGeometry3D(), _lineActiveMaterial);
                model3D.BackMaterial = _lineNormalMaterial;
                ModelVisual3D modelVisual = new ModelVisual3D();
                modelVisual.Content = model3D;

                trackingLine.ActiveChanged += trackingLine_ActiveChanged;
                trackingLine.DepletedChanged += trackingLine_DepletedChanged;
                _trackingLines.Add(trackingLine, modelVisual);
                _viewPort.Children.Add(modelVisual);
            }
            else
                Dispatcher.Invoke(new LineDelegate(AddLine), System.Windows.Threading.DispatcherPriority.Render, trackingLine);
        }

        public void DeleteLine(TrackingLine trackingLine)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                if (_trackingLines.ContainsKey(trackingLine))
                {
                    ModelVisual3D lineVisual = _trackingLines[trackingLine];
                    _viewPort.Children.Remove(lineVisual);
                    _trackingLines.Remove(trackingLine);
                    trackingLine.ActiveChanged -= trackingLine_ActiveChanged;
                    trackingLine.DepletedChanged -= trackingLine_DepletedChanged;
                }
            }
            else
                Dispatcher.Invoke(new LineDelegate(DeleteLine), System.Windows.Threading.DispatcherPriority.Render, trackingLine);
        }
        
        public void AddField(IField field)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                _minPoint = field.Polygon.Envelope.Minimum;
                Polygon3D polygon = new Polygon3D();
                Polygon polygon2D = new Polygon();

                LineGeometryBuilder builder = new LineGeometryBuilder(_viewPort.Children[0]);
                List<Point3D> outlinePoints = new List<Point3D>();

                Point3DCollection polygonPoints = new Point3DCollection();
                for (int i = 0; i < field.Polygon.Coordinates.Count - 1; i++)
                {
                    polygon.Points.Add(new Point3D(field.Polygon.Coordinates[i].X - _minPoint.X, field.Polygon.Coordinates[i].Y - _minPoint.Y, FIELD_Z_INDEX));
                    polygon2D.Points.Add(new Point(field.Polygon.Coordinates[i].X - _minPoint.X, field.Polygon.Coordinates[i].Y - _minPoint.Y));
                    outlinePoints.Add(new Point3D(field.Polygon.Coordinates[i].X - _minPoint.X, field.Polygon.Coordinates[i].Y - _minPoint.Y, LINE_Z_INDEX));
                }
                outlinePoints.Add(outlinePoints[0]);

                Mesh3D mesh3D = new Mesh3D(polygon.Points, polygon2D.Triangulate());
                MeshVisual3D mesh = new MeshVisual3D();
                DiffuseMaterial material = new DiffuseMaterial(new SolidColorBrush(_fieldFillColor));
                material.Freeze();
                mesh.FaceMaterial = material;
                mesh.EdgeDiameter = 0;
                mesh.VertexRadius = 0;
                mesh.Mesh = mesh3D;

                Point3DCollection points = builder.CreatePositions(outlinePoints, LINE_THICKNESS, 0.0, null);
                Int32Collection indices = builder.CreateIndices(outlinePoints.Count);

                Mesh3D outlienMesh3D = new Mesh3D(points, indices);
                DiffuseMaterial outlineMaterial = new DiffuseMaterial(new SolidColorBrush(_fieldOutlineColor));
                outlineMaterial.Freeze();
                GeometryModel3D outlineModel3D = new GeometryModel3D(outlienMesh3D.ToMeshGeometry3D(), outlineMaterial);
                outlineModel3D.BackMaterial = outlineMaterial;
                ModelVisual3D modelVisual = new ModelVisual3D();
                modelVisual.Content = outlineModel3D;
                
                _viewPort.Children.Add(mesh);
                _viewPort.Children.Add(modelVisual);
            }
            else
                Dispatcher.Invoke(new AddFieldDelegate(AddField), System.Windows.Threading.DispatcherPriority.Render, field);
        }

        public void AddFieldTracker(FieldTracker fieldTracker)
        {
            fieldTracker.PolygonUpdated += fieldTracker_PolygonUpdated;
        }

        public void ZoomOut()
        {
            lock (_syncObject)
            {
                if(_viewTopActive)
                {
                    _viewTopZoomLevel += VIEW_ZOOM_INCREMENT;
                    if (_viewTopZoomLevel > VIEW_ZOOM_MAX)
                        _viewTopZoomLevel = VIEW_ZOOM_MAX;
                }
                else
                {
                    _viewBehindZoomLevel += VIEW_ZOOM_INCREMENT;
                    if (_viewBehindZoomLevel > VIEW_ZOOM_MAX)
                        _viewBehindZoomLevel = VIEW_ZOOM_MAX;
                }
                Dispatcher.Invoke(new Action(UpdateZoomLevel), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        public void ZoomIn()
        {
            lock (_syncObject)
            {
                if (_viewTopActive)
                {
                    _viewTopZoomLevel -= VIEW_ZOOM_INCREMENT;
                    if (_viewTopZoomLevel < VIEW_ZOOM_MIN)
                        _viewTopZoomLevel = VIEW_ZOOM_MIN;
                }
                else
                {
                    _viewBehindZoomLevel -= VIEW_ZOOM_INCREMENT;
                    if (_viewBehindZoomLevel < VIEW_ZOOM_MIN)
                        _viewBehindZoomLevel = VIEW_ZOOM_MIN;
                }
                Dispatcher.Invoke(new Action(UpdateZoomLevel), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        public void ChangeView()
        {
            lock(_syncObject)
            {
                _viewTopActive = !_viewTopActive;
                Dispatcher.Invoke(new Action(UpdateZoomLevel), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        #endregion

        #region TrackingLineEvents

        private void trackingLine_ActiveChanged(object sender, bool active)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                TrackingLine trackingLine = (TrackingLine)sender;
                if (!_trackingLines.ContainsKey(trackingLine))
                    return;
                ModelVisual3D visualLine = _trackingLines[trackingLine];
                if (active)
                    ActivateTrackingLine(visualLine);
                else
                {
                    if (trackingLine.Depleted)
                        DepletedTrackingLine(visualLine);
                    else
                        NormalTrackingLine(visualLine);
                }
            }
            else
                Dispatcher.Invoke(new TrackingLineEventDelegate(trackingLine_ActiveChanged), System.Windows.Threading.DispatcherPriority.Render, sender, active);
        }

        private void trackingLine_DepletedChanged(object sender, bool depleted)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                TrackingLine trackingLine = (TrackingLine)sender;
                if (!_trackingLines.ContainsKey(trackingLine))
                    return;
                ModelVisual3D visualLine = _trackingLines[trackingLine];
                if (depleted)
                    DepletedTrackingLine(visualLine);
                else
                    NormalTrackingLine(visualLine);
            }
            else
                Dispatcher.Invoke(new TrackingLineEventDelegate(trackingLine_DepletedChanged), System.Windows.Threading.DispatcherPriority.Render, sender, depleted);
        }
  
        private void ActivateTrackingLine(ModelVisual3D visualLine)
        {
            ((GeometryModel3D)visualLine.Content).BackMaterial = _lineActiveMaterial;
        }

        private void DepletedTrackingLine(ModelVisual3D visualLine)
        {
            ((GeometryModel3D)visualLine.Content).BackMaterial = _lineDepletedMaterial;
        }

        private void NormalTrackingLine(ModelVisual3D visualLine)
        {
            ((GeometryModel3D)visualLine.Content).BackMaterial = _lineNormalMaterial;
        }

        #endregion

        #region FieldTrackerEvents

        private void fieldTracker_PolygonUpdated(object sender, PolygonUpdatedEventArgs e)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                if (_trackMesh.Keys.Contains(e.ID) && !e.RedrawPolygon)
                {
                    MeshVisual3D mesh = _trackMesh[e.ID];
                    _viewPort.Children.Remove(mesh);
                    MeshBuilder meshBuilder = _trackMeshBuilder[e.ID];
                    Polygon3D polygon = new Polygon3D();
                    Polygon polygon2D = new Polygon();
                    IList<Vector3D> vectors = new List<Vector3D>();
                    foreach (DotSpatial.Topology.Coordinate coord in e.NewCoordinates.Coordinates)
                    {
                        polygon.Points.Add(new Point3D(coord.X - _minPoint.X, coord.Y - _minPoint.Y, FIELD_TRACK_Z_INDEX));
                        polygon2D.Points.Add(new Point(coord.X - _minPoint.X, coord.Y - _minPoint.Y));
                        vectors.Add(new Vector3D(0.0, 0.0, 1.0));
                    }

                    meshBuilder.Append(polygon.Points, polygon2D.Triangulate(), vectors, polygon2D.Points);
                    GeometryModel3D geometry = mesh.Content as GeometryModel3D;
                    geometry.Geometry = meshBuilder.ToMesh();
                    _viewPort.Children.Add(mesh);
                }
                else
                {
                    MeshBuilder meshBuilder = new MeshBuilder(true, true);
                    Polygon3D polygon = new Polygon3D();
                    Polygon polygon2D = new Polygon();
                    IList<Vector3D> vectors = new List<Vector3D>();
                    for (int i = 0; i < e.Polygon.Shell.Coordinates.Count - 1; i++)
                    {
                        polygon.Points.Add(new Point3D(e.Polygon.Shell.Coordinates[i].X - _minPoint.X, e.Polygon.Shell.Coordinates[i].Y - _minPoint.Y, FIELD_TRACK_Z_INDEX));
                        polygon2D.Points.Add(new Point(e.Polygon.Shell.Coordinates[i].X - _minPoint.X, e.Polygon.Shell.Coordinates[i].Y - _minPoint.Y));
                        vectors.Add(new Vector3D(0.0, 0.0, 1.0));
                    }
                    meshBuilder.Append(polygon.Points, polygon2D.Triangulate(), vectors, polygon2D.Points);
                    MeshVisual3D mesh = new MeshVisual3D();
                    GeometryModel3D geometry = new GeometryModel3D();
                    geometry.Material = _fieldTrackMaterial;
                    geometry.Geometry = meshBuilder.ToMesh();
                    mesh.FaceMaterial = _fieldTrackMaterial;
                    mesh.EdgeDiameter = 0;
                    mesh.VertexRadius = 0;
                    mesh.Content = geometry;

                    IList<MeshVisual3D> holes = new List<MeshVisual3D>();
                    foreach(DotSpatial.Topology.ILinearRing hole in e.Polygon.Holes)
                    {
                        Polygon3D holePolygon = new Polygon3D();
                        Polygon holePolygon2D = new Polygon();
                        for (int i = 0; i < hole.Coordinates.Count - 1; i++)
                        {
                            holePolygon.Points.Add(new Point3D(hole.Coordinates[i].X - _minPoint.X, hole.Coordinates[i].Y - _minPoint.Y, FIELD_TRACK_HOLES_Z_INDEX));
                            holePolygon2D.Points.Add(new Point(hole.Coordinates[i].X - _minPoint.X, hole.Coordinates[i].Y - _minPoint.Y));
                        }
                        Mesh3D holeMesh3D = new Mesh3D(holePolygon.Points, holePolygon2D.Triangulate());
                        MeshVisual3D holeMesh = new MeshVisual3D();
                        holeMesh.FaceMaterial = _fieldTrackHoleMaterial;
                        holeMesh.EdgeDiameter = 0;
                        holeMesh.VertexRadius = 0;
                        holeMesh.Mesh = holeMesh3D;
                        holes.Add(holeMesh);
                    }

                    if (_trackMesh.ContainsKey(e.ID))
                    {
                        _viewPort.Children.Remove(_trackMesh[e.ID]);
                        _trackMesh.Remove(e.ID);
                    }
                    _trackMesh.Add(e.ID, mesh);
                    _viewPort.Children.Add(mesh);

                    if (_trackMeshBuilder.ContainsKey(e.ID))
                        _trackMeshBuilder.Remove(e.ID);

                    _trackMeshBuilder.Add(e.ID, meshBuilder);

                    if(_trackMeshHoles.ContainsKey(e.ID))
                    {
                        foreach (MeshVisual3D meshHole in _trackMeshHoles[e.ID])
                            _viewPort.Children.Remove(meshHole);
                        _trackMeshHoles.Remove(e.ID);
                    }

                    if(holes.Count > 0)
                    {
                        _trackMeshHoles.Add(e.ID, holes);
                        foreach (MeshVisual3D hole in holes)
                            _viewPort.Children.Add(hole);
                    }

                }
            }
            else
                Dispatcher.Invoke(new TrackPolygonUpdatedDelegate(fieldTracker_PolygonUpdated), System.Windows.Threading.DispatcherPriority.Render, sender, e);

        }
        
        #endregion

        #region Private Methods

        private void FarmingVisualizer_Loaded(object sender, RoutedEventArgs e)
        {
            object resource = TryFindResource("TRACK_LINE_ANIMATION");
            if (resource != null && resource is ColorAnimationUsingKeyFrames)
            {
                this.RegisterName("LineActiveColor", _lineActiveColor);
                ColorAnimationUsingKeyFrames animation = (ColorAnimationUsingKeyFrames)resource;
                Storyboard.SetTargetName(animation, "LineActiveColor");
                Storyboard.SetTargetProperty(animation, new PropertyPath(SolidColorBrush.ColorProperty));
                _lineActiveMaterial = new DiffuseMaterial(_lineActiveColor);
                Storyboard storyboard = new Storyboard();
                storyboard.Children.Add(animation);
                storyboard.Begin(this);
            }

            resource = TryFindResource("TRACK_LINE_INACTIVE");
            if (resource != null && resource is SolidColorBrush)
                _lineNormalMaterial = new DiffuseMaterial((SolidColorBrush)resource);

            resource = TryFindResource("TRACK_LINE_DEPLETED");
            if (resource != null && resource is SolidColorBrush)
                _lineDepletedMaterial = new DiffuseMaterial((SolidColorBrush)resource);

            resource = TryFindResource("FIELD_OUTLINE");
            if (resource != null && resource is Color)
                _fieldOutlineColor = (Color)resource;

            resource = TryFindResource("FIELD_FILL");
            if (resource != null && resource is Color)
                _fieldFillColor = (Color)resource;

            resource = TryFindResource("TRACK_FILL");
            if (resource != null && resource is SolidColorBrush)
                _fieldTrackMaterial = new DiffuseMaterial((SolidColorBrush)resource);

            resource = TryFindResource("TRACK_HOLE");
            if (resource != null && resource is SolidColorBrush)
                _fieldTrackHoleMaterial = new DiffuseMaterial((SolidColorBrush)resource);

            _fieldTrackMaterial.Freeze();
            _fieldTrackHoleMaterial.Freeze();

            UpdateZoomLevel();
        }

        private void UpdateZoomLevel()
        {
            if (_viewTopActive)
            {
                SetValue(CameraPositionProperty, VIEW_TOP_POSTION + (VIEW_TOP_ZOOM_VECTOR * _viewTopZoomLevel));
                SetValue(CameraLookDirectionProperty, VIEW_TOP_LOOK_DIRECTION);
            }
            else
            {
                SetValue(CameraPositionProperty, VIEW_BEHIND_POSTION + (VIEW_BEHIND_ZOOM_VECTOR * _viewBehindZoomLevel));
                SetValue(CameraLookDirectionProperty, VIEW_BEHIND_LOOK_DIRECTION);
            }
        }

        #endregion

        #region DependencyProperties

        protected static readonly DependencyProperty ShiftXProperty = DependencyProperty.Register("ShiftX", typeof(double), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty ShiftYProperty = DependencyProperty.Register("ShiftY", typeof(double), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty ShiftHeadingProperty = DependencyProperty.Register("ShiftHeading", typeof(double), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty CameraPositionProperty = DependencyProperty.Register("CameraPosition", typeof(Point3D), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty CameraLookDirectionProperty = DependencyProperty.Register("CameraLookDirection", typeof(Vector3D), typeof(FarmingVisualizer));

        #endregion
    }

}
