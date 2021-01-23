using DotSpatial.Positioning;
using DotSpatial.Topology;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
using HelixToolkit.Wpf;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
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

            public Coordinate[] Coordinates { get; set; }
        }

        struct MeshData
        {
            public IList<Point3D> Points { get; set; }

            public Int32Collection Indices { get; set; }
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
        
        private System.Windows.Point _lastPoint = new System.Windows.Point(0.0, 0.0);

        private double _lastAngle = 0.0;

        private double _equipmentWidth = 4.0;

        private double _equipmentOffset = 0.0;

        private DotSpatial.Topology.Coordinate _minPoint;

        private IDictionary<TrackingLine, MeshData> _trackingLines = new Dictionary<TrackingLine, MeshData>();

        private object _syncObject = new object();

        private IDictionary<int, MeshData> _trackMesh = new Dictionary<int, MeshData>();

        private IDictionary<int, ulong> _trackSums = new Dictionary<int, ulong>();

        private IDictionary<int, IList<MeshData>> _trackMeshHoles = new Dictionary<int, IList<MeshData>>();

        private IDictionary<int, IList<ulong>> _trackSumsHoles = new Dictionary<int, IList<ulong>>();

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
        
        public void UpdatePosition(System.Windows.Point position, double angle)
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
                Dispatcher.BeginInvoke(new Action<System.Windows.Point,double>(UpdatePosition), System.Windows.Threading.DispatcherPriority.Render, position, angle);
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
                _lastPoint = new System.Windows.Point((double)GetValue(ShiftXProperty), (double)GetValue(ShiftYProperty));
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
                SetValue(ShiftXProperty, trackingLine.MainLine.P0.X - _minPoint.X);
                SetValue(ShiftYProperty, trackingLine.MainLine.P0.Y - _minPoint.Y);
                double angle = 360 - trackingLine.Angle;
                SetValue(ShiftHeadingProperty, angle);
                if (!_trackingLines.ContainsKey(trackingLine))
                    return;
                _focusedTrackline = trackingLine;
                RedrawLines();
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
                _focusedTrackline = null;
                RedrawLines();
                UpdatePosition(_lastPoint, _lastAngle);
                UpdateZoomLevel();
            }
            else
                Dispatcher.BeginInvoke(new Action(CancelFocus), System.Windows.Threading.DispatcherPriority.Normal);
        }

        public void SetEquipmentWidth(Distance width)
        {
            _equipmentWidth = width.ToMeters().Value;
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
                DrawEquipment();
            else
                Dispatcher.BeginInvoke(new Action(DrawEquipment), System.Windows.Threading.DispatcherPriority.Normal);
        }

        public void SetEquipmentOffset(Distance offset, bool right)
        {
            var newOffset = offset.ToMeters().Value * (right ? -1.0 : 1.0);
            if (newOffset != _equipmentOffset)
            {
                _equipmentOffset = newOffset;
                if (Dispatcher.Thread.Equals(Thread.CurrentThread))
                    DrawEquipment();
                else
                    Dispatcher.BeginInvoke(new Action(DrawEquipment), System.Windows.Threading.DispatcherPriority.Normal);
            }
        }

        public void AddPoint(Coordinate coord, Color color)
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
                DoAddLine(trackingLine);
                RedrawLines();
            }
            else
                Dispatcher.Invoke(new Action<TrackingLine>(AddLine), System.Windows.Threading.DispatcherPriority.Normal, trackingLine);
        }

        public void AddLines(TrackingLine[] trackingLines)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                foreach(var trackingLine in trackingLines)
                    if(!_trackingLines.ContainsKey(trackingLine))
                        DoAddLine(trackingLine);
                RedrawLines();
            }
            else
                Dispatcher.Invoke(new Action<TrackingLine[]>(AddLines), System.Windows.Threading.DispatcherPriority.Normal, trackingLines);

        }

        public void DeleteLine(TrackingLine trackingLine)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                if (_trackingLines.ContainsKey(trackingLine))
                {
                    DoDeleteLine(trackingLine);
                    RedrawLines();
                }
            }
            else
                Dispatcher.Invoke(new Action<TrackingLine>(DeleteLine), System.Windows.Threading.DispatcherPriority.Normal, trackingLine);
        }
        
        public void DeleteLines(TrackingLine[] trackingLines)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                foreach (var trackingLine in trackingLines)
                    if (_trackingLines.ContainsKey(trackingLine))
                        DoDeleteLine(trackingLine);
                RedrawLines();
            }
            else
                Dispatcher.Invoke(new Action<TrackingLine[]>(DeleteLines), System.Windows.Threading.DispatcherPriority.Normal, trackingLines);
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
                if (!_trackingLines.ContainsKey(trackingLine) || !active)
                    return;
                RedrawLines();
            }
            else
                Dispatcher.Invoke(new Action<object, bool>(trackingLine_ActiveChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, active);
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
                    {
                        Dispatcher.Invoke(new Action<PolygonData, int>(UpdatePolygon), System.Windows.Threading.DispatcherPriority.Render, polygonData, e.ID);
                        _trackSums[e.ID] = polygonData.PolygonSum;
                    }

                    IList<ulong> holePolygonSums = new List<ulong>(_trackSumsHoles[e.ID]);
                    bool redrawHoles = false;
                    for (int i = 0; i < e.Polygon.Holes.Length; i++)
                    {
                        PolygonData holePolygonData = GetPolygonData(e.Polygon.Holes[i].Coordinates, FIELD_TRACK_HOLES_Z_INDEX);
                        int holeIndex = _trackSumsHoles[e.ID].IndexOf(holePolygonData.PolygonSum);
                        if (holeIndex > -1)
                            holePolygonSums.Remove(holePolygonData.PolygonSum);
                        else
                        {
                            _trackSumsHoles[e.ID].Add(holePolygonData.PolygonSum);
                            Dispatcher.Invoke(new Action<PolygonData, int>(AddPolygonHole), System.Windows.Threading.DispatcherPriority.Render, holePolygonData, e.ID);
                            redrawHoles = true;
                        }
                    }

                    foreach (ulong holePolygonSum in holePolygonSums)
                    {
                        int holeIndex = _trackSumsHoles[e.ID].IndexOf(holePolygonSum);
                        Dispatcher.Invoke(new Action<int, int>(RemovePolygonHole), System.Windows.Threading.DispatcherPriority.Render, e.ID, holeIndex);
                        _trackSumsHoles[e.ID].RemoveAt(holeIndex);
                        redrawHoles = true;
                    }

                    if (redrawHoles)
                        Dispatcher.Invoke(new Action(RedrawHoles), System.Windows.Threading.DispatcherPriority.Render);
                }
                else
                    AddCompletePolygon(e);
                
            }
            catch (Exception exception)
            {
                Log.Error("Failed to draw fieldtrack", exception);
            }
        }

        private PolygonData GetPolygonData(IList<DotSpatial.Topology.Coordinate> coordinates, double zIndex)
        {
            PolygonData data = new PolygonData();
            List<Coordinate> newCoordinates = new List<Coordinate>();

            for(int i = 0; i < coordinates.Count - 1; i++)
            {
                double x = coordinates[i].X - _minPoint.X;
                double y = coordinates[i].Y - _minPoint.Y;
                newCoordinates.Add(new Coordinate(x, y, zIndex));
                data.PolygonSum += (ulong)(x * 10) + (ulong)(y * 10);
            }

            data.Coordinates = newCoordinates.ToArray();
            return data;
        }

        private void AddCompletePolygon(PolygonUpdatedEventArgs e)
        {
            try
            {
                PolygonData polygonData = GetPolygonData(e.Polygon.Shell.Coordinates, FIELD_TRACK_Z_INDEX);
                Dispatcher.Invoke(new Action<PolygonData, int>(AddPolygon), System.Windows.Threading.DispatcherPriority.Render, polygonData, e.ID);
                _trackSums.Add(e.ID, polygonData.PolygonSum);
                _trackSumsHoles.Add(e.ID, new List<ulong>());

                bool redrawHoles = false;
                foreach (DotSpatial.Topology.ILinearRing hole in e.Polygon.Holes)
                {
                    PolygonData holePolygonData = GetPolygonData(hole.Coordinates, FIELD_TRACK_HOLES_Z_INDEX);
                    Dispatcher.Invoke(new Action<PolygonData, int>(AddPolygonHole), System.Windows.Threading.DispatcherPriority.Render, holePolygonData, e.ID);
                    _trackSumsHoles[e.ID].Add(holePolygonData.PolygonSum);
                    redrawHoles = true;
                }

                if (redrawHoles)
                    Dispatcher.Invoke(new Action(RedrawHoles), System.Windows.Threading.DispatcherPriority.Render);

            }
            catch(Exception e1)
            {
                Log.Error("Failed to Add polygon", e1);
            }
        }

        private void UpdatePolygon(PolygonData polygonData, int polygonId)
        {
            _fieldTrack.Positions = null;
            _fieldTrack.TriangleIndices = null;

            var polygonPositions = new Point3DCollection();
            var polygonPoints = new List<System.Windows.Point>();
            foreach (var coord in polygonData.Coordinates)
            {
                polygonPositions.Add(new Point3D(coord.X, coord.Y, coord.Z));
                polygonPoints.Add(new System.Windows.Point(coord.X, coord.Y));
            }

            var meshData = new MeshData() { Points = polygonPositions, Indices = CuttingEarsTriangulator.Triangulate(polygonPoints) };
            _trackMesh[polygonId] = meshData;

            var offset = 0;
            var newPositions = new Point3DCollection();
            var newIndices = new Int32Collection();
            foreach (var mesh in _trackMesh.Values)
            {
                foreach (var point in mesh.Points)
                    newPositions.Add(point);
                foreach (var indice in mesh.Indices)
                    newIndices.Add(indice + offset);
                offset += mesh.Points.Count;
            }

            _fieldTrack.Positions = newPositions;
            _fieldTrack.TriangleIndices = newIndices;
        }
        
        private void AddPolygon(PolygonData polygonData, int polygonId)
        {
            _fieldTrack.Positions = null;
            _fieldTrack.TriangleIndices = null;
            var polygonPositions = new Point3DCollection();
            var polygonPoints = new List<System.Windows.Point>();
            foreach (var coord in polygonData.Coordinates)
            {
                polygonPositions.Add(new Point3D(coord.X, coord.Y, coord.Z));
                polygonPoints.Add(new System.Windows.Point(coord.X, coord.Y));
            }

            var meshData = new MeshData() { Points = polygonPositions, Indices = CuttingEarsTriangulator.Triangulate(polygonPoints) };
            _trackMesh.Add(polygonId, meshData);
            _trackMeshHoles.Add(polygonId, new List<MeshData>());

            var offset = 0;
            var newPositions = new Point3DCollection();
            var newIndices = new Int32Collection();
            foreach (var mesh in _trackMesh.Values)
            {
                foreach (var point in mesh.Points)
                    newPositions.Add(point);
                foreach (var indice in mesh.Indices)
                    newIndices.Add(indice + offset);
                offset += mesh.Points.Count;
            }

            _fieldTrack.Positions = newPositions;
            _fieldTrack.TriangleIndices = newIndices;

        }

        private void AddPolygonHole(PolygonData polygonData, int polygonId)
        {
            var polygonPositions = new Point3DCollection();
            var polygonPoints = new List<System.Windows.Point>();
            foreach (var coord in polygonData.Coordinates)
            {
                polygonPositions.Add(new Point3D(coord.X, coord.Y, coord.Z));
                polygonPoints.Add(new System.Windows.Point(coord.X, coord.Y));
            }

            var meshData = new MeshData() { Points = polygonPositions, Indices = CuttingEarsTriangulator.Triangulate(polygonPoints) };
            _trackMeshHoles[polygonId].Add(meshData);
        }

        private void RedrawHoles()
        {
            _fieldTrackHoles.Positions = null;
            _fieldTrackHoles.TriangleIndices = null;

            var offset = 0;
            var newPositions = new Point3DCollection();
            var newIndices = new Int32Collection();
            foreach (var polygonHoles in _trackMeshHoles.Values)
            {
                foreach (var mesh in polygonHoles)
                {
                    foreach (var point in mesh.Points)
                        newPositions.Add(point);
                    foreach (var indice in mesh.Indices)
                        newIndices.Add(indice + offset);
                    offset += mesh.Points.Count;
                }
            }

            _fieldTrackHoles.Positions = newPositions;
            _fieldTrackHoles.TriangleIndices = newIndices;
        }

        private void RemovePolygon(int polygonId)
        {
            _fieldTrack.Positions = null;
            _fieldTrack.TriangleIndices = null;

            _trackMesh.Remove(polygonId);

            var offset = 0;
            var positions = new Point3DCollection();
            var indices = new Int32Collection();
            foreach (var mesh in _trackMesh.Values)
            {
                foreach (var point in mesh.Points)
                    positions.Add(point);
                foreach (var indice in mesh.Indices)
                    indices.Add(indice + offset);
                offset += mesh.Points.Count;
            }

            _fieldTrack.Positions = positions;
            _fieldTrack.TriangleIndices = indices;
        }

        private void RemovePolygonHole(int polygonId, int holeIndex)
        {            
            _trackMeshHoles[polygonId].RemoveAt(holeIndex);
        }

        private void RemovePolygonHoles(int polygonId)
        {
            _trackMeshHoles.Remove(polygonId);
        }

        private void fieldTracker_PolygonUpdated(object sender, PolygonUpdatedEventArgs e)
        {
            UpdateFieldtrack(e);                    
        }

        private void fieldTracker_PolygonDeleted(object sender, PolygonDeletedEventArgs e)
        {
            if (_trackMesh.ContainsKey(e.ID))
            {
                _trackSums.Remove(e.ID);
                Dispatcher.Invoke(new Action<int>(RemovePolygon), System.Windows.Threading.DispatcherPriority.Render, e.ID);
            }

            if (_trackMeshHoles.ContainsKey(e.ID))
            {
                bool redrawHoles = _trackSumsHoles[e.ID].Count > 0;
                Dispatcher.Invoke(new Action<int>(RemovePolygonHoles), System.Windows.Threading.DispatcherPriority.Render, e.ID);
                _trackSumsHoles.Remove(e.ID);

                if (redrawHoles)
                    Dispatcher.Invoke(new Action(RedrawHoles), System.Windows.Threading.DispatcherPriority.Render);
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

        private void DoAddLine(TrackingLine trackingLine)
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

            trackingLine.ActiveChanged += trackingLine_ActiveChanged;
            _trackingLines.Add(trackingLine, new MeshData() { Points = points, Indices = indices });
        }

        private void DoDeleteLine(TrackingLine trackingLine)
        {
            _trackingLines.Remove(trackingLine);
            trackingLine.ActiveChanged -= trackingLine_ActiveChanged;
        }

        private void FarmingVisualizer_Loaded(object sender, RoutedEventArgs e)
        {
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

        private void DrawEquipment()
        {
            double widthDivided = _equipmentWidth / 2.0;
            for (int i = 0; i < 4; i++)
                _equipmentMesh.Positions[i] = new Point3D(_equipmentMesh.Positions[i].X, widthDivided + _equipmentOffset, _equipmentMesh.Positions[i].Z);

            for (int i = 4; i < 8; i++)
                _equipmentMesh.Positions[i] = new Point3D(_equipmentMesh.Positions[i].X, (widthDivided * -1.0) + _equipmentOffset, _equipmentMesh.Positions[i].Z);
        }

        private void DrawOutline(IList<DotSpatial.Topology.Coordinate> coordinates)
        {
            LineGeometryBuilder builder = new LineGeometryBuilder(_fieldVisual);
            List<Point3D> outlinePoints = new List<Point3D>();

            for (int i = 0; i < coordinates.Count - 1; i++)
            {
                outlinePoints.Add(new Point3D(coordinates[i].X - _minPoint.X, coordinates[i].Y - _minPoint.Y, LINE_Z_INDEX));
                outlinePoints.Add(new Point3D(coordinates[i + 1].X - _minPoint.X, coordinates[i + 1].Y - _minPoint.Y, LINE_Z_INDEX));
            }
            outlinePoints.Add(outlinePoints[outlinePoints.Count - 1]);
            outlinePoints.Add(outlinePoints[0]);

            _fieldOutline.Positions = builder.CreatePositions(outlinePoints, LINE_THICKNESS, 0.0, null);
            _fieldOutline.TriangleIndices = builder.CreateIndices(outlinePoints.Count);
        }

        private void DrawFieldMesh(IList<DotSpatial.Topology.Coordinate> coordinates)
        {
            Point3DCollection points3D = new Point3DCollection(coordinates.Count - 1);
            PointCollection points2D = new PointCollection(coordinates.Count - 1);
            
            for (int i = 0; i < coordinates.Count - 1; i++)
            {
                points3D.Add(new Point3D(coordinates[i].X - _minPoint.X, coordinates[i].Y - _minPoint.Y, FIELD_Z_INDEX));
                points2D.Add(new System.Windows.Point(coordinates[i].X - _minPoint.X, coordinates[i].Y - _minPoint.Y));
            }

            _fieldMesh.Positions = points3D;
            _fieldMesh.TriangleIndices = CuttingEarsTriangulator.Triangulate(points2D);
        }

        private void AddLine(List<Point3D> positions)
        {

        }

        private void RedrawLines()
        {
            _trackLineFocus.Positions = null;
            _trackLineFocus.TriangleIndices = null;

            _trackLineActive.Positions = null;
            _trackLineActive.TriangleIndices = null;

            _trackLineInactive.Positions = null;
            _trackLineInactive.TriangleIndices = null;

            var activePositions = new Point3DCollection();
            var activeIndices = new Int32Collection();
            var activeOffset = 0;
            var inactivePositions = new Point3DCollection();
            var inactiveIndices = new Int32Collection();
            var inactiveOffset = 0;

            foreach (var trackingLine in _trackingLines)
            {
                if (trackingLine.Key == _focusedTrackline)
                {
                    var focusPositions = new Point3DCollection();
                    var focusIndices = new Int32Collection();
                    foreach (var point in trackingLine.Value.Points)
                        focusPositions.Add(point);
                    foreach (var indice in trackingLine.Value.Indices)
                        focusIndices.Add(indice);

                    _trackLineFocus.Positions = focusPositions;
                    _trackLineFocus.TriangleIndices = focusIndices;
                }
                else if (trackingLine.Key.Active)
                {
                    foreach (var point in trackingLine.Value.Points)
                        activePositions.Add(point);
                    foreach (var indice in trackingLine.Value.Indices)
                        activeIndices.Add(indice + activeOffset);
                    activeOffset += trackingLine.Value.Points.Count;
                }
                else
                {
                    foreach (var point in trackingLine.Value.Points)
                        inactivePositions.Add(point);
                    foreach (var indice in trackingLine.Value.Indices)
                        inactiveIndices.Add(indice + inactiveOffset);
                    inactiveOffset += trackingLine.Value.Points.Count;
                }
            }

            _trackLineActive.Positions = activePositions;
            _trackLineActive.TriangleIndices = activeIndices;

            _trackLineInactive.Positions = inactivePositions;
            _trackLineInactive.TriangleIndices = inactiveIndices;
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
