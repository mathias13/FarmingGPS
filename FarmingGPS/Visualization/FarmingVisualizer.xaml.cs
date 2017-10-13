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
        #region Consts

        private const double LINE_THICKNESS = 0.2;

        private const double LINE_Z_INDEX = 0.0;

        private const double FIELD_TRACK_HOLES_Z_INDEX = -0.05;
        
        private const double FIELD_TRACK_Z_INDEX = -0.10;

        private const double FIELD_Z_INDEX = -0.15;

        private readonly Point3D VIEW_TOP_POSTION = new Point3D(2.0, 0.0, 30.0);

        private readonly Vector3D VIEW_TOP_ZOOM_VECTOR = new Vector3D(1.8, 0.0, 10.0);

        private readonly Vector3D VIEW_TOP_LOOK_DIRECTION = new Vector3D(0.0, 0.0, -30.0);

        private readonly double VIEW_TOP_NEAR_PLANE = 25.0;

        private readonly double VIEW_TOP_FAR_PLANE = 35.0;

        private readonly Point3D VIEW_BEHIND_POSITION = new Point3D(-30.0, 0.0, 14.0);

        private readonly Vector3D VIEW_BEHIND_ZOOM_VECTOR = new Vector3D(-3.0, 0.0, 1.4);

        private readonly Vector3D VIEW_BEHIND_LOOK_DIRECTION = new Vector3D(4.7, 0.0, -1.5);

        private readonly double VIEW_BEHIND_NEAR_PLANE = 20.0;

        private readonly double VIEW_BEHIND_FAR_PLANE = 220.0;

        private readonly Point3D VIEW_TRACKLINE_POSITION = new Point3D(-308.0, 0.0, 88.0);

        private readonly Vector3D VIEW_TRACKLINE_LOOK_DIRECTION = new Vector3D(8.0, 0.0, -1.0);

        private readonly double VIEW_TRACKLINE_NEAR_PLANE = 250.0;

        private readonly double VIEW_TRACKLINE_FAR_PLANE = 1200.0;

        private const double VIEW_ZOOM_INCREMENT = 1;

        private const double VIEW_ZOOM_MIN = 0.0;

        private const double VIEW_ZOOM_MAX = 15.0;

        #endregion

        #region Private Variables
        
        private Point _lastPoint = new Point(0.0, 0.0);

        private double _lastAngle = 0.0;

        private DotSpatial.Topology.Coordinate _minPoint;

        private IDictionary<TrackingLine, ModelVisual3D> _trackingLines = new Dictionary<TrackingLine, ModelVisual3D>();

        private object _syncObject = new object();

        private DiffuseMaterial _lineNormalMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Blue));

        private DiffuseMaterial _lineDepletedMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.White));

        private DiffuseMaterial _lineActiveMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Red));

        private DiffuseMaterial _lineFocusMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Red));

        private SolidColorBrush _lineActiveColor = new SolidColorBrush(Colors.Red);

        private SolidColorBrush _lineFocusColor = new SolidColorBrush(Colors.Red);

        private Color _fieldOutlineColor = Colors.Black;

        private DiffuseMaterial _fieldFillMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue));

        private DiffuseMaterial _fieldTrackMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue));

        private DiffuseMaterial _fieldTrackHoleMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue));
        
        private IDictionary<int, MeshVisual3D> _trackMesh = new Dictionary<int, MeshVisual3D>();

        private IDictionary<int, MeshBuilder> _trackMeshBuilder = new Dictionary<int, MeshBuilder>();

        private IDictionary<int, IList<MeshVisual3D>> _trackMeshHoles = new Dictionary<int, IList<MeshVisual3D>>();

        private ModelVisual3D _outlineModel = new ModelVisual3D();

        private bool _viewTopActive = false;

        private bool _viewTrackLine = false;

        private double _viewTopZoomLevel = 7.0;

        private double _viewBehindZoomLevel = 7.0;

        private TrackingLine _focusedTrackline = null;

        #endregion

        public FarmingVisualizer()
        {
            InitializeComponent();
            this.Loaded += FarmingVisualizer_Loaded;
        }

        #region Public Methods
        
        public void UpdatePosition(Point position, double angle)
        {
            if(Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                if (_viewTrackLine)
                    return;
                SetValue(ShiftXProperty, position.X);
                SetValue(ShiftYProperty, position.Y);
                angle -= 90;
                SetValue(ShiftHeadingProperty, angle);
                _lastPoint = position;
                _lastAngle = angle;
            }
            else
                Dispatcher.BeginInvoke(new Action<Point,double>(UpdatePosition), System.Windows.Threading.DispatcherPriority.Send, position, angle);
        }

        public void UpdatePosition(DotSpatial.Topology.Coordinate coord, Azimuth bearing)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                if (_viewTrackLine)
                    return;
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
                _lastAngle = (double)GetValue(ShiftHeadingProperty);
            }
            else
                Dispatcher.BeginInvoke(new Action<DotSpatial.Topology.Coordinate, Azimuth>(UpdatePosition), System.Windows.Threading.DispatcherPriority.Send, coord, bearing);
        }
       
        public void FocusTrackingLine(TrackingLine trackingLine)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                _viewTrackLine = true;
                if (_focusedTrackline != null)
                {
                    if (_focusedTrackline.Depleted)
                        DepletedTrackingLine(_trackingLines[_focusedTrackline]);
                    else
                        NormalTrackingLine(_trackingLines[_focusedTrackline]);
                }
                SetValue(ShiftXProperty, trackingLine.MainLine.P0.X - _minPoint.X);
                SetValue(ShiftYProperty, trackingLine.MainLine.P0.Y - _minPoint.Y);
                double angle = 360 - trackingLine.Angle;
                SetValue(ShiftHeadingProperty, angle);
                if (!_trackingLines.ContainsKey(trackingLine))
                    return;
                ModelVisual3D visualLine = _trackingLines[trackingLine];
                FocusTrackingLine(visualLine);
                _focusedTrackline = trackingLine;
                SetValue(CameraPositionProperty, VIEW_TRACKLINE_POSITION);
                SetValue(CameraLookDirectionProperty, VIEW_TRACKLINE_LOOK_DIRECTION);
                SetValue(CameraNearPlaneDistanceProperty, VIEW_TRACKLINE_NEAR_PLANE);
                SetValue(CameraFarPlaneDistanceProperty, VIEW_TRACKLINE_FAR_PLANE);
            }
            else
                Dispatcher.BeginInvoke(new Action<TrackingLine>(FocusTrackingLine), System.Windows.Threading.DispatcherPriority.Render, trackingLine);
        }
         
        public void CancelFocus()
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                _viewTrackLine = false;
                if (_focusedTrackline == null)
                    return;
                if (_focusedTrackline.Depleted)
                    DepletedTrackingLine(_trackingLines[_focusedTrackline]);
                else
                    NormalTrackingLine(_trackingLines[_focusedTrackline]);
                _focusedTrackline = null;
                UpdatePosition(_lastPoint, _lastAngle);
                UpdateZoomLevel();
            }
            else
                Dispatcher.BeginInvoke(new Action(CancelFocus), System.Windows.Threading.DispatcherPriority.Render);
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
                Dispatcher.Invoke(new Action<Distance>(SetEquipmentWidth), System.Windows.Threading.DispatcherPriority.Render, width);
        }

        public void AddPoint(DotSpatial.Topology.Coordinate coord, Color color)
        {
            if (!Dispatcher.Thread.Equals(Thread.CurrentThread))
                Dispatcher.Invoke(new Action<DotSpatial.Topology.Coordinate, Color>(AddPoint), System.Windows.Threading.DispatcherPriority.Normal, coord, color);
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
                Dispatcher.Invoke(new Action<TrackingLine>(AddLine), System.Windows.Threading.DispatcherPriority.Normal, trackingLine);
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
                Dispatcher.Invoke(new Action<TrackingLine>(DeleteLine), System.Windows.Threading.DispatcherPriority.Normal, trackingLine);
        }
        
        public void AddField(IField field)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                if (_minPoint == null)
                    _minPoint = field.Polygon.Envelope.Minimum;
                DrawOutline(field.Polygon.Coordinates);
                DrawFieldMesh(field.Polygon.Coordinates);
            }
            else
                Dispatcher.Invoke(new Action<IField>(AddField), System.Windows.Threading.DispatcherPriority.Normal, field);
        }

        public void AddFieldTracker(FieldTracker fieldTracker)
        {
            fieldTracker.PolygonUpdated += fieldTracker_PolygonUpdated;
            fieldTracker.PolygonDeleted += fieldTracker_PolygonDeleted;
        }

        public void AddFieldCreator(FieldCreator fieldCreator)
        {
            fieldCreator.FieldBoundaryUpdated += FieldCreator_FieldBoundaryUpdated;
            fieldCreator.FieldCreated += FieldCreator_FieldCreated;
            _minPoint = fieldCreator.GetField().Polygon.Envelope.Minimum;
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
                Dispatcher.Invoke(new Action<object, bool>(trackingLine_ActiveChanged), System.Windows.Threading.DispatcherPriority.Render, sender, active);
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
                Dispatcher.Invoke(new Action<object, bool>(trackingLine_DepletedChanged), System.Windows.Threading.DispatcherPriority.Render, sender, depleted);
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

        private void FocusTrackingLine(ModelVisual3D visualLine)
        {
            ((GeometryModel3D)visualLine.Content).BackMaterial = _lineFocusMaterial;
        }

        #endregion

        #region FieldTrackerEvents

        private void fieldTracker_PolygonUpdated(object sender, PolygonUpdatedEventArgs e)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                try
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
                        foreach (DotSpatial.Topology.ILinearRing hole in e.Polygon.Holes)
                        {
                            double area = Math.Abs(DotSpatial.Topology.Algorithm.CgAlgorithms.SignedArea(hole.Coordinates));
                            Polygon3D holePolygon = new Polygon3D();
                            Polygon holePolygon2D = new Polygon();
                            for (int i = 0; i < hole.Coordinates.Count - 1; i++)
                            {
                                holePolygon.Points.Add(new Point3D(hole.Coordinates[i].X - _minPoint.X, hole.Coordinates[i].Y - _minPoint.Y, FIELD_TRACK_HOLES_Z_INDEX));
                                holePolygon2D.Points.Add(new Point(hole.Coordinates[i].X - _minPoint.X, hole.Coordinates[i].Y - _minPoint.Y));
                            }
                            Mesh3D holeMesh3D = new Mesh3D(holePolygon.Points, holePolygon2D.Triangulate());
                            MeshVisual3D holeMesh = new MeshVisual3D();
                            //TODO make this constant or setting
                            holeMesh.FaceMaterial = area > 100.0 ? _fieldFillMaterial : _fieldTrackHoleMaterial;
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

                        if (_trackMeshHoles.ContainsKey(e.ID))
                        {
                            foreach (MeshVisual3D meshHole in _trackMeshHoles[e.ID])
                                _viewPort.Children.Remove(meshHole);
                            _trackMeshHoles.Remove(e.ID);
                        }

                        if (holes.Count > 0)
                        {
                            _trackMeshHoles.Add(e.ID, holes);
                            foreach (MeshVisual3D hole in holes)
                                _viewPort.Children.Add(hole);
                        }

                    }
                }
                catch (Exception exception)
                {
                    Utilities.Log.Log.Error(exception);
                }
            }
            else
                Dispatcher.Invoke(new Action<object, PolygonUpdatedEventArgs>(fieldTracker_PolygonUpdated), System.Windows.Threading.DispatcherPriority.Normal, sender, e);

        }

        private void fieldTracker_PolygonDeleted(object sender, PolygonDeletedEventArgs e)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                if (_trackMesh.ContainsKey(e.ID))
                {
                    _viewPort.Children.Remove(_trackMesh[e.ID]);
                    _trackMesh.Remove(e.ID);
                }

                if (_trackMeshBuilder.ContainsKey(e.ID))
                    _trackMeshBuilder.Remove(e.ID);

                if (_trackMeshHoles.ContainsKey(e.ID))
                {
                    foreach (MeshVisual3D meshHole in _trackMeshHoles[e.ID])
                        _viewPort.Children.Remove(meshHole);
                    _trackMeshHoles.Remove(e.ID);
                }
            }
            else
                Dispatcher.Invoke(new Action<object, PolygonDeletedEventArgs>(fieldTracker_PolygonDeleted), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
        }

        #endregion

        #region FieldCreatorEvents

        private void FieldCreator_FieldBoundaryUpdated(object sender, FieldBoundaryUpdatedEventArgs e)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                FieldCreator fieldCreator = sender as FieldCreator;
                _minPoint = fieldCreator.GetField().Polygon.Envelope.Minimum;
                IList<DotSpatial.Topology.Coordinate> coordinates = new List<DotSpatial.Topology.Coordinate>();
                foreach (Position position in e.Boundary)
                    coordinates.Add(fieldCreator.GetField().GetPositionInField(position));

                DrawOutline(coordinates);
            }
            else
                Dispatcher.Invoke(new Action<object, FieldBoundaryUpdatedEventArgs>(FieldCreator_FieldBoundaryUpdated), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
        }

        private void FieldCreator_FieldCreated(object sender, FieldCreatedEventArgs e)
        {
            FieldCreator fieldCreator = sender as FieldCreator;
            fieldCreator.FieldBoundaryUpdated -= FieldCreator_FieldBoundaryUpdated;
            fieldCreator.FieldCreated -= FieldCreator_FieldCreated;
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

            resource = TryFindResource("TRACK_LINE_FOCUS_ANIMATION");
            if (resource != null && resource is ColorAnimationUsingKeyFrames)
            {
                this.RegisterName("LineFocusColor", _lineFocusColor);
                ColorAnimationUsingKeyFrames animation = (ColorAnimationUsingKeyFrames)resource;
                Storyboard.SetTargetName(animation, "LineFocusColor");
                Storyboard.SetTargetProperty(animation, new PropertyPath(SolidColorBrush.ColorProperty));
                _lineFocusMaterial = new DiffuseMaterial(_lineFocusColor);
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
            if (resource != null && resource is SolidColorBrush)
                _fieldFillMaterial = new DiffuseMaterial((SolidColorBrush)resource);

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
            if (_viewTrackLine)
                return;
            if (_viewTopActive)
            {
                Vector3D zoomVector = VIEW_TOP_ZOOM_VECTOR * _viewTopZoomLevel;
                SetValue(CameraPositionProperty, VIEW_TOP_POSTION + zoomVector);
                SetValue(CameraLookDirectionProperty, VIEW_TOP_LOOK_DIRECTION);
                SetValue(CameraNearPlaneDistanceProperty, VIEW_TOP_NEAR_PLANE + zoomVector.Length);
                SetValue(CameraFarPlaneDistanceProperty, VIEW_TOP_FAR_PLANE + zoomVector.Length);
            }
            else
            {
                Vector3D zoomVector = VIEW_BEHIND_ZOOM_VECTOR * _viewBehindZoomLevel;
                SetValue(CameraPositionProperty, VIEW_BEHIND_POSITION + zoomVector);
                SetValue(CameraLookDirectionProperty, VIEW_BEHIND_LOOK_DIRECTION);
                SetValue(CameraNearPlaneDistanceProperty, VIEW_BEHIND_NEAR_PLANE + zoomVector.Length);
                SetValue(CameraFarPlaneDistanceProperty, VIEW_BEHIND_FAR_PLANE + zoomVector.Length);
            }
        }

        private void DrawOutline(IList<DotSpatial.Topology.Coordinate> coordinates)
        {
            _viewPort.Children.Remove(_outlineModel);
            LineGeometryBuilder builder = new LineGeometryBuilder(_viewPort.Children[0]);
            List<Point3D> outlinePoints = new List<Point3D>();

            Point3DCollection polygonPoints = new Point3DCollection();
            for (int i = 0; i < coordinates.Count - 1; i++)
            {
                outlinePoints.Add(new Point3D(coordinates[i].X - _minPoint.X, coordinates[i].Y - _minPoint.Y, LINE_Z_INDEX));
                outlinePoints.Add(new Point3D(coordinates[i + 1].X - _minPoint.X, coordinates[i + 1].Y - _minPoint.Y, LINE_Z_INDEX));
            }
            outlinePoints.Add(outlinePoints[outlinePoints.Count - 1]);
            outlinePoints.Add(outlinePoints[0]);

            Point3DCollection points = builder.CreatePositions(outlinePoints, LINE_THICKNESS, 0.0, null);
            Int32Collection indices = builder.CreateIndices(outlinePoints.Count);

            Mesh3D outlienMesh3D = new Mesh3D(points, indices);
            DiffuseMaterial outlineMaterial = new DiffuseMaterial(new SolidColorBrush(_fieldOutlineColor));
            outlineMaterial.Freeze();
            GeometryModel3D outlineModel3D = new GeometryModel3D(outlienMesh3D.ToMeshGeometry3D(), outlineMaterial);
            outlineModel3D.BackMaterial = outlineMaterial;
            _outlineModel = new ModelVisual3D();
            _outlineModel.Content = outlineModel3D;
            
            _viewPort.Children.Add(_outlineModel);
        }

        private void DrawFieldMesh(IList<DotSpatial.Topology.Coordinate> coordinates)
        {
            Polygon3D polygon = new Polygon3D();
            Polygon polygon2D = new Polygon();

            LineGeometryBuilder builder = new LineGeometryBuilder(_viewPort.Children[0]);

            Point3DCollection polygonPoints = new Point3DCollection();
            for (int i = 0; i < coordinates.Count - 1; i++)
            {
                polygon.Points.Add(new Point3D(coordinates[i].X - _minPoint.X, coordinates[i].Y - _minPoint.Y, FIELD_Z_INDEX));
                polygon2D.Points.Add(new Point(coordinates[i].X - _minPoint.X, coordinates[i].Y - _minPoint.Y));
            }
            Mesh3D mesh3D = new Mesh3D(polygon.Points, polygon2D.Triangulate());
            MeshVisual3D mesh = new MeshVisual3D();
            DiffuseMaterial material = _fieldFillMaterial;
            material.Freeze();
            mesh.FaceMaterial = material;
            mesh.EdgeDiameter = 0;
            mesh.VertexRadius = 0;
            mesh.Mesh = mesh3D;

            _viewPort.Children.Add(mesh);
        }

        #endregion

        #region DependencyProperties

        protected static readonly DependencyProperty ShiftXProperty = DependencyProperty.Register("ShiftX", typeof(double), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty ShiftYProperty = DependencyProperty.Register("ShiftY", typeof(double), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty ShiftHeadingProperty = DependencyProperty.Register("ShiftHeading", typeof(double), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty CameraPositionProperty = DependencyProperty.Register("CameraPosition", typeof(Point3D), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty CameraLookDirectionProperty = DependencyProperty.Register("CameraLookDirection", typeof(Vector3D), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty CameraNearPlaneDistanceProperty = DependencyProperty.Register("CameraNearPlaneDistance", typeof(double), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty CameraFarPlaneDistanceProperty = DependencyProperty.Register("CameraFarPlaneDistance", typeof(double), typeof(FarmingVisualizer));
        #endregion
    }

}
