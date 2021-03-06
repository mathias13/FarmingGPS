﻿using DotSpatial.Positioning;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
using HelixToolkit.Wpf;
using log4net;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace FarmingGPS.Visualization
{
    /// <summary>
    /// Interaction logic for FarmingVisualizer.xaml
    /// </summary>
    public partial class FarmingVisualizer : UserControl
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        struct PolygonData
        {
            public ulong PolygonSum { get; set; }
            public IList<Point3D> Polygon { get; set; }
            public IList<Point> Polygon2D { get; set; }
            public IList<Vector3D> Vectors { get; set; }
        }

        #region Consts

        private const double LINE_THICKNESS = 0.2;

        private const double LINE_Z_INDEX = 0.0;

        private const double FIELD_TRACK_HOLES_Z_INDEX = -0.03;
        
        private const double FIELD_TRACK_Z_INDEX = -0.06;

        private const double FIELD_Z_INDEX = -0.09;

        private const double FIELD_TRACK_HOLE_RED_MAX_AREA = 100.0;

        private readonly Point3D VIEW_TOP_POSTION = new Point3D(2.0, 0.0, 30.0);

        private readonly Vector3D VIEW_TOP_ZOOM_VECTOR = new Vector3D(1.8, 0.0, 10.0);

        private readonly Vector3D VIEW_TOP_LOOK_DIRECTION = new Vector3D(0.0, 0.0, -30.0);

        private readonly double VIEW_TOP_NEAR_PLANE = 25.0;

        private readonly double VIEW_TOP_FAR_PLANE = 35.0;

        private readonly Point3D VIEW_BEHIND_POSITION = new Point3D(-30.0, 0.0, 14.0);

        private readonly Vector3D VIEW_BEHIND_ZOOM_VECTOR = new Vector3D(-3.0, 0.0, 1.4);

        private readonly Vector3D VIEW_BEHIND_LOOK_DIRECTION = new Vector3D(4.7, 0.0, -1.5);

        private readonly double VIEW_BEHIND_NEAR_PLANE = 20.0;

        private readonly double VIEW_BEHIND_FAR_PLANE = 200.0;

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

        private IDictionary<int, ulong> _trackSums = new Dictionary<int, ulong>();

        private IDictionary<int, IList<MeshVisual3D>> _trackMeshHoles = new Dictionary<int, IList<MeshVisual3D>>();

        private IDictionary<int, IList<ulong>> _trackSumsHoles = new Dictionary<int, IList<ulong>>();

        private ModelVisual3D _outlineModel = new ModelVisual3D();

        private MeshVisual3D _fieldMesh = null;
        
        private bool _viewTopActive = false;

        private bool _viewTrackLine = false;

        private double _viewTopZoomLevel = 7.0;

        private double _viewBehindZoomLevel = 7.0;

        private TrackingLine _focusedTrackline = null;
                
        #endregion

        public FarmingVisualizer()
        {
            InitializeComponent();
            _fieldFillMaterial.Freeze();
            _fieldTrackMaterial.Freeze();
            _fieldTrackHoleMaterial.Freeze();
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
                Dispatcher.BeginInvoke(new Action<Point,double>(UpdatePosition), System.Windows.Threading.DispatcherPriority.Render, position, angle);
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
                Dispatcher.BeginInvoke(new Action<DotSpatial.Topology.Coordinate, Azimuth>(UpdatePosition), System.Windows.Threading.DispatcherPriority.Render, coord, bearing);
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
                Dispatcher.BeginInvoke(new Action<TrackingLine>(FocusTrackingLine), System.Windows.Threading.DispatcherPriority.Normal, trackingLine);
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
                Dispatcher.BeginInvoke(new Action(CancelFocus), System.Windows.Threading.DispatcherPriority.Normal);
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
                Dispatcher.Invoke(new Action<Distance>(SetEquipmentWidth), System.Windows.Threading.DispatcherPriority.Normal, width);
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
        
        public void SetField(IField field)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                if (_minPoint == null)
                    _minPoint = field.Polygon.Envelope.Minimum;
                DrawOutline(field.Polygon.Coordinates);
                DrawFieldMesh(field.Polygon.Coordinates);
            }
            else
                Dispatcher.Invoke(new Action<IField>(SetField), System.Windows.Threading.DispatcherPriority.Normal, field);
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
                Dispatcher.Invoke(new Action(UpdateZoomLevel), System.Windows.Threading.DispatcherPriority.Normal);
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
                Dispatcher.Invoke(new Action(UpdateZoomLevel), System.Windows.Threading.DispatcherPriority.Normal);
            }
        }

        public void ChangeView()
        {
            lock(_syncObject)
            {
                _viewTopActive = !_viewTopActive;
                Dispatcher.Invoke(new Action(UpdateZoomLevel), System.Windows.Threading.DispatcherPriority.Normal);
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
                Dispatcher.Invoke(new Action<object, bool>(trackingLine_ActiveChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, active);
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
                Dispatcher.Invoke(new Action<object, bool>(trackingLine_DepletedChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, depleted);
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
        
        private void UpdateFieldtrack(PolygonUpdatedEventArgs e)
        {
            try
            {
                if (_trackMesh.Keys.Contains(e.ID))
                {
                    PolygonData polygonData = GetPolygonData(e.Polygon.Shell.Coordinates, FIELD_TRACK_Z_INDEX);
                    if (polygonData.PolygonSum != _trackSums[e.ID])
                        Dispatcher.Invoke(new Action<PolygonData, int>(UpdatePolygon), System.Windows.Threading.DispatcherPriority.Render, polygonData, e.ID);

                    IList<ulong> holePolygonSums = new List<ulong>(_trackSumsHoles[e.ID]);
                    for (int i = 0; i < e.Polygon.Holes.Length; i++)
                    {
                        PolygonData holePolygonData = GetPolygonData(e.Polygon.Holes[i].Coordinates, FIELD_TRACK_HOLES_Z_INDEX);
                        int holeIndex = _trackSumsHoles[e.ID].IndexOf(holePolygonData.PolygonSum);
                        if (holeIndex > -1)
                            holePolygonSums.Remove(holePolygonData.PolygonSum);
                        else
                        {
                            double area = Math.Abs(DotSpatial.Topology.Algorithm.CgAlgorithms.SignedArea(e.Polygon.Holes[i].Coordinates));
                            Dispatcher.Invoke(new Action<PolygonData, int, bool>(AddPolygonHole), System.Windows.Threading.DispatcherPriority.Render, holePolygonData, e.ID, area > FIELD_TRACK_HOLE_RED_MAX_AREA);
                        }
                    }

                    foreach (ulong holePolygonSum in holePolygonSums)
                    {
                        int holeIndex = _trackSumsHoles[e.ID].IndexOf(holePolygonSum);
                        Dispatcher.Invoke(new Action<int, int>(RemovePolygonHole), System.Windows.Threading.DispatcherPriority.Render, e.ID, holeIndex);
                        _trackMeshHoles[e.ID].RemoveAt(holeIndex);
                        _trackSumsHoles[e.ID].RemoveAt(holeIndex);
                    }

                }
                else
                    AddCompletePolygon(e);
                    //Dispatcher.Invoke(new Action<PolygonUpdatedEventArgs>(AddCompletePolygon), System.Windows.Threading.DispatcherPriority.Render, e);
                
            }
            catch (Exception exception)
            {
                Log.Error("Failed to draw fieldtrack", exception);
            }
        }

        private PolygonData GetPolygonData(IList<DotSpatial.Topology.Coordinate> coordinates, double zIndex)
        {
            PolygonData data = new PolygonData();
            data.Polygon = new List<Point3D>();
            data.Polygon2D = new List<Point>();
            data.Vectors = new List<Vector3D>();
            for(int i = 0; i < coordinates.Count - 1; i++)
            {
                double x = coordinates[i].X - _minPoint.X;
                double y = coordinates[i].Y - _minPoint.Y;
                data.Polygon.Add(new Point3D(x, y, zIndex));
                data.Polygon2D.Add(new Point(x, y));
                data.PolygonSum += (ulong)(x * 10) + (ulong)(y * 10);
                data.Vectors.Add(new Vector3D(0.0, 0.0, 1.0));
            }
            return data;
        }

        private void AddCompletePolygon(PolygonUpdatedEventArgs e)
        {
            try
            {
                PolygonData polygonData = GetPolygonData(e.Polygon.Shell.Coordinates, FIELD_TRACK_Z_INDEX);
                Dispatcher.Invoke(new Action<PolygonData, int>(AddPolygon), System.Windows.Threading.DispatcherPriority.Render, polygonData, e.ID);

                _trackMeshHoles.Add(e.ID, new List<MeshVisual3D>());
                _trackSumsHoles.Add(e.ID, new List<ulong>());
                foreach (DotSpatial.Topology.ILinearRing hole in e.Polygon.Holes)
                {
                    double area = Math.Abs(DotSpatial.Topology.Algorithm.CgAlgorithms.SignedArea(hole.Coordinates));
                    PolygonData holePolygonData = GetPolygonData(hole.Coordinates, FIELD_TRACK_HOLES_Z_INDEX);
                    Dispatcher.Invoke(new Action<PolygonData, int, bool>(AddPolygonHole), System.Windows.Threading.DispatcherPriority.Render, holePolygonData, e.ID, area > FIELD_TRACK_HOLE_RED_MAX_AREA);
                }

            }
            catch(Exception e1)
            {
                Log.Error("Failed to Add polygon", e1);
            }
        }

        private void UpdatePolygon(PolygonData polygonData, int polygonId)
        {
            MeshVisual3D mesh = _trackMesh[polygonId];
            _viewPort.Children.Remove(mesh);
            Mesh3D mesh3D = new Mesh3D(polygonData.Polygon, CuttingEarsTriangulator.Triangulate(polygonData.Polygon2D));
            mesh.Mesh = mesh3D;
            _viewPort.Children.Add(mesh);
            _trackSums[polygonId] = polygonData.PolygonSum;
        }
        
        private void AddPolygon(PolygonData polygonData, int polygonId)
        {
            Mesh3D mesh3D = new Mesh3D(polygonData.Polygon, CuttingEarsTriangulator.Triangulate(polygonData.Polygon2D));
            MeshVisual3D mesh = new MeshVisual3D();
            mesh.FaceMaterial = _fieldTrackMaterial;
            mesh.EdgeDiameter = 0;
            mesh.VertexRadius = 0;
            mesh.Mesh = mesh3D;

            _trackMesh.Add(polygonId, mesh);
            _trackSums.Add(polygonId, polygonData.PolygonSum);
            _viewPort.Children.Add(mesh);
        }

        private void AddPolygonHole(PolygonData polygonData, int polygonId, bool redArea)
        {
            Mesh3D holeMesh3D = new Mesh3D(polygonData.Polygon, CuttingEarsTriangulator.Triangulate(polygonData.Polygon2D));
            MeshVisual3D holeMesh = new MeshVisual3D();
            holeMesh.FaceMaterial = redArea ? _fieldFillMaterial : _fieldTrackHoleMaterial;
            holeMesh.EdgeDiameter = 0;
            holeMesh.VertexRadius = 0;
            holeMesh.Mesh = holeMesh3D;
            _trackMeshHoles[polygonId].Add(holeMesh);
            _trackSumsHoles[polygonId].Add(polygonData.PolygonSum);
            _viewPort.Children.Add(holeMesh);
        }

        private void RemovePolygon(int polygonId)
        {
            _viewPort.Children.Remove(_trackMesh[polygonId]);
        }

        private void RemovePolygonHole(int polygonId, int holeIndex)
        {
            _viewPort.Children.Remove(_trackMeshHoles[polygonId][holeIndex]);
        }
        
        private void fieldTracker_PolygonUpdated(object sender, PolygonUpdatedEventArgs e)
        {
            UpdateFieldtrack(e);                    
        }

        private void fieldTracker_PolygonDeleted(object sender, PolygonDeletedEventArgs e)
        {
            if (_trackMesh.ContainsKey(e.ID))
            {
                Dispatcher.Invoke(new Action<int>(RemovePolygon), System.Windows.Threading.DispatcherPriority.Render, e.ID);
                _trackMesh.Remove(e.ID);
                _trackSums.Remove(e.ID);
            }

            if (_trackMeshHoles.ContainsKey(e.ID))
            {
                for (int i = 0; i < _trackSumsHoles[e.ID].Count; i++)
                {
                    Dispatcher.Invoke(new Action<int,int>(RemovePolygonHole), System.Windows.Threading.DispatcherPriority.Render, e.ID, i);
                    Thread.Yield();
                }
                _trackMeshHoles.Remove(e.ID);
                _trackSumsHoles.Remove(e.ID);
            }
            
        }

        #endregion

        #region FieldCreatorEvents

        private void FieldCreator_FieldBoundaryUpdated(object sender, FieldBoundaryUpdatedEventArgs e)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                FieldCreator fieldCreator = sender as FieldCreator;
                _minPoint = fieldCreator.GetField().Polygon.Envelope.Minimum;
                DrawOutline(e.Boundary);
            }
            else
                Dispatcher.BeginInvoke(new Action<object, FieldBoundaryUpdatedEventArgs>(FieldCreator_FieldBoundaryUpdated), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
        }

        private void FieldCreator_FieldCreated(object sender, FieldCreatedEventArgs e)
        {
            FieldCreator fieldCreator = sender as FieldCreator;
            SetField(fieldCreator.GetField());
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
            outlineModel3D.Freeze();
            _outlineModel = new ModelVisual3D();
            _outlineModel.Content = outlineModel3D;
            
            _viewPort.Children.Add(_outlineModel);
        }

        private void DrawFieldMesh(IList<DotSpatial.Topology.Coordinate> coordinates)
        {
            if(_fieldMesh != null)
                _viewPort.Children.Remove(_fieldMesh);

            Polygon3D polygon = new Polygon3D();
            Polygon polygon2D = new Polygon();
            
            for (int i = 0; i < coordinates.Count - 1; i++)
            {
                polygon.Points.Add(new Point3D(coordinates[i].X - _minPoint.X, coordinates[i].Y - _minPoint.Y, FIELD_Z_INDEX));
                polygon2D.Points.Add(new Point(coordinates[i].X - _minPoint.X, coordinates[i].Y - _minPoint.Y));
            }
            Mesh3D mesh3D = new Mesh3D(polygon.Points, polygon2D.Triangulate());
            _fieldMesh = new MeshVisual3D();
            DiffuseMaterial material = _fieldFillMaterial;
            material.Freeze();
            _fieldMesh.FaceMaterial = material;
            _fieldMesh.EdgeDiameter = 0;
            _fieldMesh.VertexRadius = 0;
            _fieldMesh.Mesh = mesh3D;
            _fieldMesh.Content.Freeze();

            _viewPort.Children.Add(_fieldMesh);
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
