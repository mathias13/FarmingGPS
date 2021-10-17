using DotSpatial.Positioning;
using DotSpatial.Topology;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Shaders;
using HelixToolkit.Wpf;
using log4net;
using SharpDX;
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
using System.Runtime.InteropServices;

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
            public Vector3Collection Points { get; set; }

            public IntCollection Indices { get; set; }

            public Vector3Collection Normals { get; set; }
        }

        #region Consts

        private const float LINE_Z_INDEX = 0.0f;

        private const double FIELD_TRACK_HOLES_Z_INDEX = -0.03;
        
        private const double FIELD_TRACK_Z_INDEX = -0.06;

        private const float FIELD_Z_INDEX = -0.09f;

        private readonly Vector3D VIEW_TOP_POSTION = new Vector3D(2.0, 0.0, 30.0);

        private readonly Vector3D VIEW_TOP_ZOOM_VECTOR = new Vector3D(1.8, 0.0, 10.0);

        private readonly Vector3D VIEW_TOP_LOOK_DIRECTION = new Vector3D(0.0, 0.0, -30.0);

        private readonly double VIEW_TOP_NEAR_PLANE = 25.0;

        private readonly double VIEW_TOP_FAR_PLANE = 35.0;

        private readonly Vector3D VIEW_BEHIND_POSITION = new Vector3D(-30.0, 0.0, 14.0);

        private readonly Vector3D VIEW_BEHIND_ZOOM_VECTOR = new Vector3D(-3.0, 0.0, 1.4);

        private readonly Vector3D VIEW_BEHIND_LOOK_DIRECTION = new Vector3D(4.7, 0.0, -1.5);

        private readonly double VIEW_BEHIND_NEAR_PLANE = 20.0;

        private readonly double VIEW_BEHIND_FAR_PLANE = 200.0;

        private readonly Vector3D VIEW_TRACKLINE_POSITION = new Vector3D(-308.0, 0.0, 88.0);

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

        private Vector3D _cameraPosition = new Vector3D();

        private double _cameraRotation = 0.0;

        private Vector3D _viewZoomVector = new Vector3D();

        private Vector3D _viewPositionVector = new Vector3D(0.0, 0.0, 0.0);

        private Vector3D _viewLookVector = new Vector3D(0.0, 0.0, 0.0);

        private TrackingLine _focusedTrackline = null;
                        
        #endregion

        public FarmingVisualizer()
        {
            InitializeComponent();
            _viewPort.EffectsManager = new DefaultEffectsManager();
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
                _cameraPosition = new Vector3D(position.X, position.Y, 0.0);
                _cameraRotation = angle;
                UpdateCamera();
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
                _cameraPosition = new Vector3D(x, y, 0.0);
                _cameraRotation = bearing.DecimalDegrees;
                UpdateCamera();
            }
            else
                Dispatcher.BeginInvoke(new Action<DotSpatial.Topology.Coordinate, Azimuth>(UpdatePosition), System.Windows.Threading.DispatcherPriority.Render, coord, bearing);
        }
       
        public void FocusTrackingLine(TrackingLine trackingLine)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                _viewTrackLine = true;
                var x = trackingLine.MainLine.P0.X - _minPoint.X;
                var y = trackingLine.MainLine.P0.Y - _minPoint.Y;
                SetValue(ShiftXProperty, x);
                SetValue(ShiftYProperty, y);
                double angle = 360 - trackingLine.Angle;
                SetValue(ShiftHeadingProperty, angle);
                if (!_trackingLines.ContainsKey(trackingLine))
                    return;
                _focusedTrackline = trackingLine;
                RedrawLines();
                _cameraRotation = angle;
                _cameraPosition = new Vector3D(x, y, 0.0);
                _viewPositionVector = VIEW_TRACKLINE_POSITION;
                SetValue(CameraLookDirectionProperty, VIEW_TRACKLINE_LOOK_DIRECTION);
                SetValue(CameraNearPlaneDistanceProperty, VIEW_TRACKLINE_NEAR_PLANE);
                SetValue(CameraFarPlaneDistanceProperty, VIEW_TRACKLINE_FAR_PLANE);
                UpdateCamera();
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

        public void AddPoint(DotSpatial.Topology.Coordinate coord, SharpDX.Color color)
        {
            //if (!Dispatcher.Thread.Equals(Thread.CurrentThread))
            //    Dispatcher.Invoke(new Action<DotSpatial.Topology.Coordinate, SharpDX.Color>(AddPoint), System.Windows.Threading.DispatcherPriority.Normal, coord, color);
            //else
            //{
            //    double x = coord.X;
            //    double y = coord.Y;
            //    if (_minPoint != null)
            //    {
            //        x -= _minPoint.X;
            //        y -= _minPoint.Y;
            //    }
            //    PointsVisual3D pointVisual = new PointsVisual3D();
            //    pointVisual.Points.Add(new Point3D(x, y, 0.1));
            //    pointVisual.Color = color;
            //    pointVisual.Size = 2;
            //    _viewPort.Children.Add(pointVisual);
            //}

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
                bool redraw = false;
                foreach (var trackingLine in trackingLines)
                {
                    if (!_trackingLines.ContainsKey(trackingLine))
                    {
                        DoAddLine(trackingLine);
                        redraw = true;
                    }
                }
                if(redraw)
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
            try
            {
                var geometry = new HelixToolkit.Wpf.SharpDX.MeshGeometry3D();
                var polygonPositions = new Vector3Collection();
                var polygonPoints = new List<Vector2>();
                var normals = new Vector3Collection();
                foreach (var coord in polygonData.Coordinates)
                {
                    polygonPositions.Add(new Vector3((float)coord.X, (float)coord.Y, (float)coord.Z));
                    polygonPoints.Add(new Vector2((float)coord.X, (float)coord.Y));
                    normals.Add(new Vector3(0, 0, 1));
                }

                var meshData = new MeshData() { Points = polygonPositions, Indices = new IntCollection(CuttingEarsTriangulator.Triangulate(polygonPoints)), Normals = normals };
                _trackMesh[polygonId] = meshData;

                var offset = 0;
                var newPositions = new Vector3Collection();
                var newIndices = new IntCollection();
                var newNormals = new Vector3Collection();
                foreach (var mesh in _trackMesh.Values)
                {
                    foreach (var point in mesh.Points)
                        newPositions.Add(new Vector3(point.X, point.Y, point.Z));
                    foreach (var indice in mesh.Indices)
                        newIndices.Add(indice + offset);
                    foreach (var normal in mesh.Normals)
                        newNormals.Add(normal);
                    offset += mesh.Points.Count;
                }

                geometry.Positions = newPositions;
                geometry.Indices = newIndices;
                geometry.Normals = newNormals;
                _fieldTrack.Geometry = geometry;
            }
            catch(Exception e)
            {
                Log.Error("Failed to update polygon", e);
            }
        }
        
        private void AddPolygon(PolygonData polygonData, int polygonId)
        {
            var geometry = new HelixToolkit.Wpf.SharpDX.MeshGeometry3D();
            var polygonPositions = new Vector3Collection();
            var polygonPoints = new List<Vector2>();
            var normals = new Vector3Collection();
            foreach (var coord in polygonData.Coordinates)
            {
                polygonPositions.Add(new Vector3((float)coord.X, (float)coord.Y, (float)coord.Z));
                polygonPoints.Add(new Vector2((float)coord.X, (float)coord.Y));
                normals.Add(new Vector3(0, 0, 1));
            }

            var meshData = new MeshData() { Points = polygonPositions, Indices = new IntCollection(CuttingEarsTriangulator.Triangulate(polygonPoints)), Normals = normals };
            _trackMesh.Add(polygonId, meshData);
            _trackMeshHoles.Add(polygonId, new List<MeshData>());

            var offset = 0;
            var newPositions = new Vector3Collection();
            var newIndices = new IntCollection();
            var newNormals = new Vector3Collection();
            foreach (var mesh in _trackMesh.Values)
            {
                foreach (var point in mesh.Points)
                    newPositions.Add(new Vector3((float)point.X, (float)point.Y, (float)point.Z));
                foreach (var indice in mesh.Indices)
                    newIndices.Add(indice + offset);
                foreach (var normal in mesh.Normals)
                    newNormals.Add(normal);
                offset += mesh.Points.Count;
            }

            geometry.Positions = newPositions;
            geometry.Indices = newIndices;
            geometry.Normals = newNormals;
            _fieldTrack.Geometry = geometry;
        }

        private void AddPolygonHole(PolygonData polygonData, int polygonId)
        {
            var polygonPositions = new Vector3Collection();
            var polygonPoints = new List<Vector2>();
            var normals = new Vector3Collection();
            foreach (var coord in polygonData.Coordinates)
            {
                polygonPositions.Add(new Vector3((float)coord.X, (float)coord.Y, (float)coord.Z));
                polygonPoints.Add(new Vector2((float)coord.X, (float)coord.Y));
                normals.Add(new Vector3(0, 0, 1));
            }

            var meshData = new MeshData() { Points = polygonPositions, Indices = new IntCollection(CuttingEarsTriangulator.Triangulate(polygonPoints)), Normals = normals };
            _trackMeshHoles[polygonId].Add(meshData);
        }

        private void RedrawHoles()
        {
            var offset = 0;
            var geometry = new HelixToolkit.Wpf.SharpDX.MeshGeometry3D();
            var newPositions = new Vector3Collection();
            var newIndices = new IntCollection();
            var normals = new Vector3Collection();
            foreach (var polygonHoles in _trackMeshHoles.Values)
            {
                foreach (var mesh in polygonHoles)
                {
                    foreach (var point in mesh.Points)
                        newPositions.Add(new Vector3((float)point.X, (float)point.Y, (float)point.Z));
                    foreach (var indice in mesh.Indices)
                        newIndices.Add(indice + offset);
                    foreach (var normal in mesh.Normals)
                        normals.Add(normal);
                    offset += mesh.Points.Count;
                }
            }

            geometry.Positions = newPositions;
            geometry.Indices = newIndices;
            geometry.Normals = normals;
            _fieldTrackHoles.Geometry = geometry;
        }

        private void RemovePolygon(int polygonId)
        {
            _trackMesh.Remove(polygonId);

            var offset = 0;
            var geometry = new HelixToolkit.Wpf.SharpDX.MeshGeometry3D();
            var positions = new Vector3Collection();
            var indices = new IntCollection();
            var normals = new Vector3Collection();
            foreach (var mesh in _trackMesh.Values)
            {
                foreach (var point in mesh.Points)
                    positions.Add(new Vector3((float)point.X, (float)point.Y, (float)point.Z));
                foreach (var indice in mesh.Indices)
                    indices.Add(indice + offset);
                foreach (var normal in mesh.Normals)
                    normals.Add(normal);
                offset += mesh.Points.Count;
            }


            geometry.Positions = positions;
            geometry.Indices = indices;
            geometry.Normals = normals;
            _fieldTrack.Geometry = geometry;
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
            var points = new Vector3Collection();
            for (int i = 0; i < trackingLine.Points.Count; i++)
            {
                double x1 = trackingLine.Points[i].X;
                double y1 = trackingLine.Points[i].Y;
                if (_minPoint != null)
                {
                    x1 -= _minPoint.X;
                    y1 -= _minPoint.Y;
                }
                points.Add(new Vector3((float)x1, (float)y1, LINE_Z_INDEX));
            }
            trackingLine.ActiveChanged += trackingLine_ActiveChanged;
            _trackingLines.Add(trackingLine, new MeshData() { Points = points  });
        }

        private void DoDeleteLine(TrackingLine trackingLine)
        {
            _trackingLines.Remove(trackingLine);
            trackingLine.ActiveChanged -= trackingLine_ActiveChanged;
        }

        private void FarmingVisualizer_Loaded(object sender, RoutedEventArgs e)
        {
            _trackLineFocus.Geometry = new LineGeometry3D();
            _trackLineActive.Geometry = new LineGeometry3D();
            _trackLineInactive.Geometry = new LineGeometry3D();
            _trackLineInactive.Geometry.IsDynamic = true;
            _trackLineInactive.Geometry.PreDefinedVertexCount = 300;
            _trackLineInactive.Geometry.PreDefinedIndexCount = 300;
            UpdateZoomLevel();
        }

        private void UpdateCamera()
        {            
            var test = new Matrix3D();
            test.Translate(_viewPositionVector);
            test.Rotate(new System.Windows.Media.Media3D.Quaternion(new Vector3D(0.0, 0.0, -1.0), _cameraRotation));
            test.Translate(_cameraPosition);
            var position = test.Transform(new Point3D(0.0, 0.0, 0.0));
            SetValue(CameraPositionProperty, position);
            test = new Matrix3D();
            test.Translate(_viewLookVector);
            test.Rotate(new System.Windows.Media.Media3D.Quaternion(new Vector3D(0.0, 0.0, -1.0), _cameraRotation));            
            var rotatedVector = test.Transform(new Point3D(0.0, 0.0, 0.0));
            SetValue(CameraLookDirectionProperty, new Vector3D(rotatedVector.X, rotatedVector.Y, rotatedVector.Z));
            test = new Matrix3D();
            test.Translate(new Vector3D(1.0,0.0,1.0));
            test.Rotate(new System.Windows.Media.Media3D.Quaternion(new Vector3D(0.0, 0.0, -1.0), _cameraRotation));
            rotatedVector = test.Transform(new Point3D(0.0, 0.0, 0.0));
            SetValue(CameraUpDirection, new Vector3D(rotatedVector.X, rotatedVector.Y, rotatedVector.Z));
        }

        private void UpdateZoomLevel()
        {
            if (_viewTrackLine)
                return;
            if (_viewTopActive)
            {
                _viewZoomVector  = VIEW_TOP_ZOOM_VECTOR * _viewTopZoomLevel;
                _viewPositionVector = VIEW_TOP_POSTION + _viewZoomVector;
                _viewLookVector = VIEW_TOP_LOOK_DIRECTION;
                SetValue(CameraNearPlaneDistanceProperty, VIEW_TOP_NEAR_PLANE + _viewZoomVector.Length);
                SetValue(CameraFarPlaneDistanceProperty, VIEW_TOP_FAR_PLANE + _viewZoomVector.Length);
            }
            else
            {
                _viewZoomVector = VIEW_BEHIND_ZOOM_VECTOR * _viewBehindZoomLevel;
                _viewPositionVector = VIEW_BEHIND_POSITION + _viewZoomVector;
                _viewLookVector = VIEW_BEHIND_LOOK_DIRECTION;
                SetValue(CameraNearPlaneDistanceProperty, VIEW_BEHIND_NEAR_PLANE + _viewZoomVector.Length);
                SetValue(CameraFarPlaneDistanceProperty, VIEW_BEHIND_FAR_PLANE + _viewZoomVector.Length);
            }
            UpdateCamera();
        }

        private void DrawEquipment()
        {
            double widthDivided = _equipmentWidth / 2.0;
            for (int i = 0; i < 4; i++)
                _equipmentMesh.Geometry.Positions[i] = new Vector3(_equipmentMesh.Geometry.Positions[i].X, (float)widthDivided + (float)_equipmentOffset, _equipmentMesh.Geometry.Positions[i].Z);

            for (int i = 4; i < 8; i++)
                _equipmentMesh.Geometry.Positions[i] = new Vector3(_equipmentMesh.Geometry.Positions[i].X, ((float)widthDivided * -1.0f) + (float)_equipmentOffset, _equipmentMesh.Geometry.Positions[i].Z);

            _equipmentMesh.Geometry.UpdateVertices();
        }

        private void DrawOutline(IList<DotSpatial.Topology.Coordinate> coordinates)
        {
            LineBuilder builder = new LineBuilder();

            for (int i = 0; i < coordinates.Count - 1; i++)
                builder.AddLine(new Vector3((float)(coordinates[i].X - _minPoint.X), (float)(coordinates[i].Y - _minPoint.Y), LINE_Z_INDEX), new Vector3((float)(coordinates[i + 1].X - _minPoint.X), (float)(coordinates[i + 1].Y - _minPoint.Y), LINE_Z_INDEX));

            builder.AddLine(new Vector3((float)(coordinates[coordinates.Count - 1].X - _minPoint.X), (float)(coordinates[coordinates.Count - 1].Y - _minPoint.Y), LINE_Z_INDEX), new Vector3((float)(coordinates[0].X - _minPoint.X), (float)(coordinates[0].Y - _minPoint.Y), LINE_Z_INDEX));

            _fieldOutline.Geometry = builder.ToLineGeometry3D();
        }

        private void DrawFieldMesh(IList<DotSpatial.Topology.Coordinate> coordinates)
        {
            var geometry = new HelixToolkit.Wpf.SharpDX.MeshGeometry3D();
            var points3D = new Vector3Collection(coordinates.Count - 1);
            var points2D = new List<Vector2>(coordinates.Count - 1);
            geometry.Normals = new Vector3Collection();

            for (int i = 0; i < coordinates.Count - 1; i++)
            {
                points3D.Add(new Vector3((float)(coordinates[i].X - _minPoint.X), (float)(coordinates[i].Y - _minPoint.Y), FIELD_Z_INDEX));
                points2D.Add(new Vector2((float)(coordinates[i].X - _minPoint.X), (float)(coordinates[i].Y - _minPoint.Y)));
                geometry.Normals.Add(new Vector3(0, 0, 1));
            }

            var indices = CuttingEarsTriangulator.Triangulate(points2D);
            geometry.Positions = points3D;
            geometry.Indices = new IntCollection(indices);
            _fieldMesh.Geometry = geometry;
        }

        private void AddLine(List<Point3D> positions)
        {

        }

        private void RedrawLines()
        {
            var focusbuilder = new LineBuilder();
            var activebuilder = new LineBuilder();
            var inactivebuilder = new LineBuilder();

            foreach (var trackingLine in _trackingLines)
            {
                if (trackingLine.Key == _focusedTrackline)
                    for (int i = 0; i < trackingLine.Value.Points.Count - 1; i++)
                        focusbuilder.AddLine(trackingLine.Value.Points[i], trackingLine.Value.Points[i + 1]);
                else if (trackingLine.Key.Active)
                    for (int i = 0; i < trackingLine.Value.Points.Count - 1; i++)
                        activebuilder.AddLine(trackingLine.Value.Points[i], trackingLine.Value.Points[i + 1]);
                else if (!trackingLine.Key.Depleted)
                    for (int i = 0; i < trackingLine.Value.Points.Count - 1; i++)
                        inactivebuilder.AddLine(trackingLine.Value.Points[i], trackingLine.Value.Points[i + 1]);
            }

            _trackLineFocus.Geometry = focusbuilder.ToLineGeometry3D();
            _trackLineActive.Geometry = activebuilder.ToLineGeometry3D();
            _trackLineInactive.Geometry = inactivebuilder.ToLineGeometry3D();
        }

        #endregion

        #region DependencyProperties

        protected static readonly DependencyProperty ShiftXProperty = DependencyProperty.Register("ShiftX", typeof(double), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty ShiftYProperty = DependencyProperty.Register("ShiftY", typeof(double), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty ShiftHeadingProperty = DependencyProperty.Register("ShiftHeading", typeof(double), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty CameraPositionProperty = DependencyProperty.Register("CameraPosition", typeof(Point3D), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty CameraLookDirectionProperty = DependencyProperty.Register("CameraLookDirection", typeof(Vector3D), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty CameraUpDirection = DependencyProperty.Register("CameraUpDirection", typeof(Vector3D), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty CameraNearPlaneDistanceProperty = DependencyProperty.Register("CameraNearPlaneDistance", typeof(double), typeof(FarmingVisualizer));

        protected static readonly DependencyProperty CameraFarPlaneDistanceProperty = DependencyProperty.Register("CameraFarPlaneDistance", typeof(double), typeof(FarmingVisualizer));
        #endregion
    }

}
