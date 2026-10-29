using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui
{
    /// <summary>
    /// Supported visualization preview modes.
    /// </summary>
    public enum PreviewViewMode
    {
        Mode2D,
        Mode3D
    }

    /// <summary>
    /// 3D rendering style representation.
    /// </summary>
    public enum Render3DMode
    {
        /// <summary>
        /// Solid shaded faces with directional lighting.
        /// </summary>
        Solid,

        /// <summary>
        /// Wireframe mesh displaying triangle edges.
        /// </summary>
        Wireframe
    }

    /// <summary>
    /// Interactive preview control supporting 2D profile rendering (with reference circles, pan, and zoom)
    /// and 3D extruded solid mesh rendering (with 3D orbit rotation, lighting, wireframe, and depth sorting).
    /// </summary>
    public class AlibreGearPreviewControl : UserControl
    {
        private const float MarginPx = 25f;
        private const double ZoomFactor = 1.15;
        private const double MinZoom = 0.02;
        private const double MaxZoom = 300.0;

        private List<List<GearPoint>> _polylines = new List<List<GearPoint>>();
        private GearMesh3DBuilder.Mesh3D _mesh = new GearMesh3DBuilder.Mesh3D();
        private PreviewViewMode _viewMode = PreviewViewMode.Mode2D;
        private Render3DMode _mode3D = Render3DMode.Solid;

        // 2D Navigation state
        private double _userZoom2D = 1.0;
        private PointF _userPan2D = PointF.Empty;

        // 3D Navigation state (orbit angles in degrees)
        private double _yawDeg = -35.0;
        private double _pitchDeg = 25.0;
        private double _userZoom3D = 1.0;
        private PointF _userPan3D = PointF.Empty;

        // Mouse interaction state
        private bool _isDraggingLeft;
        private bool _isDraggingRight;
        private Point _lastMouse;

        // Reference Circles in 2D
        public bool ShowReferenceCircles { get; set; } = true;
        public double PitchRadius { get; set; }
        public double BaseRadius { get; set; }
        public double AddendumRadius { get; set; }
        public double DedendumRadius { get; set; }

        public bool HasSecondGear { get; set; }
        public double Gear2CenterDistance { get; set; }
        public double Gear2PitchRadius { get; set; }
        public double Gear2BaseRadius { get; set; }
        public double Gear2AddendumRadius { get; set; }
        public double Gear2DedendumRadius { get; set; }

        /// <summary>
        /// Current visualization mode (2D or 3D).
        /// </summary>
        public PreviewViewMode ViewMode
        {
            get => _viewMode;
            set
            {
                _viewMode = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Current 3D visualization style (Solid or Wireframe).
        /// </summary>
        public Render3DMode Mode3D
        {
            get => _mode3D;
            set
            {
                _mode3D = value;
                Invalidate();
            }
        }

        /// <summary>
        /// The 2D polylines to render in gear/world coordinates.
        /// </summary>
        public List<List<GearPoint>> Polylines
        {
            get => _polylines;
            set
            {
                _polylines = value ?? new List<List<GearPoint>>();
                Invalidate();
            }
        }

        /// <summary>
        /// The 3D mesh geometry to render in 3D mode.
        /// </summary>
        public GearMesh3DBuilder.Mesh3D Mesh
        {
            get => _mesh;
            set
            {
                _mesh = value ?? new GearMesh3DBuilder.Mesh3D();
                Invalidate();
            }
        }

        /// <summary>
        /// Pen color used for profile outlines.
        /// </summary>
        public Color LineColor { get; set; } = Color.MidnightBlue;

        /// <summary>
        /// Base color used for 3D shaded rendering.
        /// </summary>
        public Color MeshColor { get; set; } = Color.FromArgb(70, 130, 180);

        public AlibreGearPreviewControl()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Resize += (s, e) => Invalidate();
            MouseWheel += HandleMouseWheelZoom;
            MouseDown += HandleMouseDown;
            MouseUp += HandleMouseUp;
            MouseMove += HandleMouseMove;
            DoubleClick += (s, e) => ResetView();
        }

        /// <summary>
        /// Resets zoom, pan, and rotation so the geometry re-fits the control.
        /// </summary>
        public void ResetView()
        {
            _userZoom2D = 1.0;
            _userPan2D = PointF.Empty;
            _yawDeg = -35.0;
            _pitchDeg = 25.0;
            _userZoom3D = 1.0;
            _userPan3D = PointF.Empty;
            Invalidate();
        }

        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            _lastMouse = e.Location;
            if (e.Button == MouseButtons.Left)
            {
                _isDraggingLeft = true;
            }
            else if (e.Button == MouseButtons.Right)
            {
                _isDraggingRight = true;
            }
        }

        private void HandleMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) _isDraggingLeft = false;
            if (e.Button == MouseButtons.Right) _isDraggingRight = false;
        }

        private void HandleMouseWheelZoom(object sender, MouseEventArgs e)
        {
            var factor = e.Delta > 0 ? ZoomFactor : 1.0 / ZoomFactor;
            if (_viewMode == PreviewViewMode.Mode2D)
            {
                _userZoom2D = Math.Max(MinZoom, Math.Min(MaxZoom, _userZoom2D * factor));
            }
            else
            {
                _userZoom3D = Math.Max(MinZoom, Math.Min(MaxZoom, _userZoom3D * factor));
            }
            Invalidate();
        }

        private void HandleMouseMove(object sender, MouseEventArgs e)
        {
            var dx = e.X - _lastMouse.X;
            var dy = e.Y - _lastMouse.Y;
            _lastMouse = e.Location;

            if (_viewMode == PreviewViewMode.Mode2D)
            {
                if (_isDraggingLeft || _isDraggingRight)
                {
                    _userPan2D = new PointF(_userPan2D.X + dx, _userPan2D.Y + dy);
                    Invalidate();
                }
            }
            else
            {
                if (_isDraggingLeft)
                {
                    _yawDeg += dx * 0.5;
                    _pitchDeg = Math.Max(-89.0, Math.Min(89.0, _pitchDeg + dy * 0.5));
                    Invalidate();
                }
                else if (_isDraggingRight)
                {
                    _userPan3D = new PointF(_userPan3D.X + dx, _userPan3D.Y + dy);
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_viewMode == PreviewViewMode.Mode2D)
            {
                Render2D(g);
            }
            else
            {
                Render3D(g);
            }

            RenderOverlayInfo(g);
        }

        #region 2D Rendering

        private void Render2D(Graphics g)
        {
            if (!TryGet2DBounds(out var minX, out var minY, out var maxX, out var maxY))
            {
                Draw2DAxes(g, Width / 2f, Height / 2f);
                return;
            }

            var worldWidth = Math.Max(maxX - minX, 1e-6);
            var worldHeight = Math.Max(maxY - minY, 1e-6);
            var scaleX = (Width - 2 * MarginPx) / worldWidth;
            var scaleY = (Height - 2 * MarginPx) / worldHeight;
            var fitScale = (float)(Math.Min(scaleX, scaleY) * _userZoom2D);

            var worldCx = (minX + maxX) / 2.0;
            var worldCy = (minY + maxY) / 2.0;
            var screenCx = Width / 2f + _userPan2D.X;
            var screenCy = Height / 2f + _userPan2D.Y;

            PointF ToScreen(GearPoint p) => new PointF(
                (float)(screenCx + (p.X - worldCx) * fitScale),
                (float)(screenCy - (p.Y - worldCy) * fitScale));

            Draw2DAxes(g, screenCx, screenCy);

            // Render Reference Circles
            if (ShowReferenceCircles)
            {
                var g1Cx = screenCx - (float)(worldCx * fitScale);
                var g1Cy = screenCy + (float)(worldCy * fitScale);
                DrawReferenceCircle(g, g1Cx, g1Cy,
                    (float)(PitchRadius * fitScale), Color.DarkSeaGreen, "Pitch (d)");
                DrawReferenceCircle(g, g1Cx, g1Cy,
                    (float)(BaseRadius * fitScale), Color.DarkGoldenrod, "Base (db)");
                DrawReferenceCircle(g, g1Cx, g1Cy,
                    (float)(AddendumRadius * fitScale), Color.CornflowerBlue, "Tip (da)");
                DrawReferenceCircle(g, g1Cx, g1Cy,
                    (float)(DedendumRadius * fitScale), Color.IndianRed, "Root (df)");

                if (HasSecondGear)
                {
                    var g2Cx = screenCx + (float)((Gear2CenterDistance - worldCx) * fitScale);
                    var g2Cy = screenCy + (float)(worldCy * fitScale);
                    DrawReferenceCircle(g, g2Cx, g2Cy,
                        (float)(Gear2PitchRadius * fitScale), Color.DarkSeaGreen, "Pitch 2 (d2)");
                    DrawReferenceCircle(g, g2Cx, g2Cy,
                        (float)(Gear2BaseRadius * fitScale), Color.DarkGoldenrod, "Base 2 (db2)");
                    DrawReferenceCircle(g, g2Cx, g2Cy,
                        (float)(Gear2AddendumRadius * fitScale), Color.CornflowerBlue, "Tip 2 (da2)");
                    DrawReferenceCircle(g, g2Cx, g2Cy,
                        (float)(Gear2DedendumRadius * fitScale), Color.IndianRed, "Root 2 (df2)");
                }
            }

            // Draw Gear Polylines
            using var pen = new Pen(LineColor, 1.6f);
            foreach (var polyline in _polylines)
            {
                if (polyline == null || polyline.Count < 2) continue;
                var pts = new PointF[polyline.Count];
                for (var i = 0; i < polyline.Count; i++) pts[i] = ToScreen(polyline[i]);
                g.DrawLines(pen, pts);
            }
        }

        private void DrawReferenceCircle(Graphics g, float cx, float cy, float radiusPx, Color color, string label)
        {
            if (radiusPx <= 0.5f) return;
            using var pen = new Pen(Color.FromArgb(160, color), 1f) { DashStyle = DashStyle.Dash };
            g.DrawEllipse(pen, cx - radiusPx, cy - radiusPx, radiusPx * 2f, radiusPx * 2f);
        }

        private void Draw2DAxes(Graphics g, float cx, float cy)
        {
            using var pen = new Pen(Color.FromArgb(220, 220, 220), 1f) { DashStyle = DashStyle.Dash };
            g.DrawLine(pen, 0, cy, Width, cy);
            g.DrawLine(pen, cx, 0, cx, Height);
        }

        private bool TryGet2DBounds(out double minX, out double minY, out double maxX, out double maxY)
        {
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            var any = false;

            foreach (var polyline in _polylines)
            {
                if (polyline == null) continue;
                foreach (var p in polyline)
                {
                    any = true;
                    if (p.X < minX) minX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y > maxY) maxY = p.Y;
                }
            }

            if (ShowReferenceCircles && AddendumRadius > 0)
            {
                any = true;
                minX = Math.Min(minX, -AddendumRadius);
                minY = Math.Min(minY, -AddendumRadius);
                maxX = Math.Max(maxX, AddendumRadius);
                maxY = Math.Max(maxY, AddendumRadius);

                if (HasSecondGear && Gear2AddendumRadius > 0)
                {
                    minX = Math.Min(minX, Gear2CenterDistance - Gear2AddendumRadius);
                    minY = Math.Min(minY, -Gear2AddendumRadius);
                    maxX = Math.Max(maxX, Gear2CenterDistance + Gear2AddendumRadius);
                    maxY = Math.Max(maxY, Gear2AddendumRadius);
                }
            }

            return any;
        }

        #endregion

        #region 3D Rendering

        private class ProjectedTriangle
        {
            public PointF P0;
            public PointF P1;
            public PointF P2;
            public double AvgZ;
            public Color ShadedColor;
            public bool IsFrontFacing;
        }

        private void Render3D(Graphics g)
        {
            if (_mesh == null || _mesh.Vertices.Count == 0 || _mesh.TriangleIndices.Count < 3)
            {
                using var font = new Font("Segoe UI", 10f);
                using var brush = new SolidBrush(Color.Gray);
                g.DrawString("No 3D mesh available. Execute calculation to generate 3D model.", font, brush, 20, 20);
                return;
            }

            // Calculate bounding box for center and scale
            var minX = double.MaxValue; var maxX = double.MinValue;
            var minY = double.MaxValue; var maxY = double.MinValue;
            var minZ = double.MaxValue; var maxZ = double.MinValue;
            for (var i = 0; i < _mesh.Vertices.Count; i++)
            {
                var v = _mesh.Vertices[i];
                if (v.X < minX) minX = v.X;
                if (v.X > maxX) maxX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Y > maxY) maxY = v.Y;
                if (v.Z < minZ) minZ = v.Z;
                if (v.Z > maxZ) maxZ = v.Z;
            }

            var cx = (minX + maxX) / 2.0;
            var cy = (minY + maxY) / 2.0;
            var cz = (minZ + maxZ) / 2.0;

            var dx = maxX - minX;
            var dy = maxY - minY;
            var dz = maxZ - minZ;
            var maxR = Math.Max(Math.Sqrt(dx * dx + dy * dy + dz * dz) / 2.0, 1.0);

            var fitScale = (float)((Math.Min(Width, Height) * 0.42 / maxR) * _userZoom3D);
            var screenCx = Width / 2f + _userPan3D.X;
            var screenCy = Height / 2f + _userPan3D.Y;

            var radYaw = _yawDeg * Math.PI / 180.0;
            var radPitch = _pitchDeg * Math.PI / 180.0;

            var cosY = Math.Cos(radYaw);
            var sinY = Math.Sin(radYaw);
            var cosP = Math.Cos(radPitch);
            var sinP = Math.Sin(radPitch);

            // Normalized light vector pointing from top-right-front towards scene
            var lx = 0.577;
            var ly = 0.577;
            var lz = 0.577;

            // Transform vertices to view coordinates
            var numVerts = _mesh.Vertices.Count;
            var projX = new float[numVerts];
            var projY = new float[numVerts];
            var depthZ = new double[numVerts];

            for (var i = 0; i < numVerts; i++)
            {
                var v = _mesh.Vertices[i];
                var vx = v.X - cx;
                var vy = v.Y - cy;
                var vz = v.Z - cz;

                // Yaw rotation around Y axis
                var x1 = vx * cosY + vz * sinY;
                var y1 = vy;
                var z1 = -vx * sinY + vz * cosY;

                // Pitch rotation around X axis
                var x2 = x1;
                var y2 = y1 * cosP - z1 * sinP;
                var z2 = y1 * sinP + z1 * cosP;

                projX[i] = (float)(screenCx + x2 * fitScale);
                projY[i] = (float)(screenCy - y2 * fitScale);
                depthZ[i] = z2;
            }

            // Project and shade triangles
            var numTris = _mesh.TriangleIndices.Count / 3;
            var triList = new List<ProjectedTriangle>(numTris);

            for (var i = 0; i < _mesh.TriangleIndices.Count; i += 3)
            {
                var i0 = _mesh.TriangleIndices[i];
                var i1 = _mesh.TriangleIndices[i + 1];
                var i2 = _mesh.TriangleIndices[i + 2];

                if (i0 < 0 || i0 >= numVerts || i1 < 0 || i1 >= numVerts || i2 < 0 || i2 >= numVerts)
                    continue;

                var p0 = new PointF(projX[i0], projY[i0]);
                var p1 = new PointF(projX[i1], projY[i1]);
                var p2 = new PointF(projX[i2], projY[i2]);

                // 2D screen cross product for front-facing test
                var cross2D = (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);
                var isFrontFacing = cross2D < 0; // Screen Y inverted

                // Average depth for Painter's sorting
                var avgZ = (depthZ[i0] + depthZ[i1] + depthZ[i2]) / 3.0;

                // Compute normal lighting
                var n0 = (i0 < _mesh.Normals.Count) ? _mesh.Normals[i0] : new GearPoint3D(0, 0, 1);
                var n1 = (i1 < _mesh.Normals.Count) ? _mesh.Normals[i1] : new GearPoint3D(0, 0, 1);
                var n2 = (i2 < _mesh.Normals.Count) ? _mesh.Normals[i2] : new GearPoint3D(0, 0, 1);

                var nx = (n0.X + n1.X + n2.X) / 3.0;
                var ny = (n0.Y + n1.Y + n2.Y) / 3.0;
                var nz = (n0.Z + n1.Z + n2.Z) / 3.0;

                // Rotate normal
                var nx1 = nx * cosY + nz * sinY;
                var ny1 = ny;
                var nz1 = -nx * sinY + nz * cosY;

                var nx2 = nx1;
                var ny2 = ny1 * cosP - nz1 * sinP;
                var nz2 = ny1 * sinP + nz1 * cosP;

                var dot = nx2 * lx + ny2 * ly + nz2 * lz;
                var intensity = Math.Max(0.25, Math.Min(1.0, 0.35 + 0.65 * Math.Max(0.0, dot)));

                var r = (int)(MeshColor.R * intensity);
                var gCol = (int)(MeshColor.G * intensity);
                var b = (int)(MeshColor.B * intensity);

                triList.Add(new ProjectedTriangle
                {
                    P0 = p0,
                    P1 = p1,
                    P2 = p2,
                    AvgZ = avgZ,
                    ShadedColor = Color.FromArgb(r, gCol, b),
                    IsFrontFacing = isFrontFacing
                });
            }

            if (_mode3D == Render3DMode.Wireframe)
            {
                using var wirePen = new Pen(Color.FromArgb(30, 65, 135), 1.2f);
                var triPoints = new PointF[3];

                foreach (var tri in triList)
                {
                    triPoints[0] = tri.P0;
                    triPoints[1] = tri.P1;
                    triPoints[2] = tri.P2;

                    g.DrawPolygon(wirePen, triPoints);
                }
            }
            else
            {
                // Painter's algorithm: sort triangles back-to-front
                triList.Sort((a, b) => a.AvgZ.CompareTo(b.AvgZ));

                using var meshEdgePen = new Pen(Color.FromArgb(50, 20, 40, 70), 1f);
                var triPoints = new PointF[3];

                foreach (var tri in triList)
                {
                    if (!tri.IsFrontFacing) continue;

                    triPoints[0] = tri.P0;
                    triPoints[1] = tri.P1;
                    triPoints[2] = tri.P2;

                    using var brush = new SolidBrush(tri.ShadedColor);
                    g.FillPolygon(brush, triPoints);
                    g.DrawPolygon(meshEdgePen, triPoints);
                }
            }

            Draw3DAxesIndicator(g, (float)cosY, (float)sinY, (float)cosP, (float)sinP);
        }

        private void Draw3DAxesIndicator(Graphics g, float cosY, float sinY, float cosP, float sinP)
        {
            var originX = 50f;
            var originY = Height - 50f;
            var axisLen = 30f;

            // Project 3D unit axis vectors
            PointF ProjectAxis(float ax, float ay, float az)
            {
                var x1 = ax * cosY + az * sinY;
                var y1 = ay;
                var z1 = -ax * sinY + az * cosY;

                var x2 = x1;
                var y2 = y1 * cosP - z1 * sinP;

                return new PointF(originX + x2 * axisLen, originY - y2 * axisLen);
            }

            var o = new PointF(originX, originY);
            var px = ProjectAxis(1, 0, 0);
            var py = ProjectAxis(0, 1, 0);
            var pz = ProjectAxis(0, 0, 1);

            using var penX = new Pen(Color.Crimson, 2f);
            using var penY = new Pen(Color.ForestGreen, 2f);
            using var penZ = new Pen(Color.DodgerBlue, 2f);
            using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);

            g.DrawLine(penX, o, px);
            g.DrawString("X", font, Brushes.Crimson, px.X + 2, px.Y - 6);

            g.DrawLine(penY, o, py);
            g.DrawString("Y", font, Brushes.ForestGreen, py.X + 2, py.Y - 6);

            g.DrawLine(penZ, o, pz);
            g.DrawString("Z", font, Brushes.DodgerBlue, pz.X + 2, pz.Y - 6);
        }

        #endregion

        private void RenderOverlayInfo(Graphics g)
        {
            string modeText;
            if (_viewMode == PreviewViewMode.Mode2D)
            {
                modeText = "2D Profile View (Left-drag to pan | Scroll to zoom | Double-click to reset)";
            }
            else if (_mode3D == Render3DMode.Wireframe)
            {
                modeText = "3D Wireframe View (Left-drag to rotate | Right-drag to pan | Scroll to zoom | Double-click to reset)";
            }
            else
            {
                modeText = "3D Solid Mesh View (Left-drag to rotate | Right-drag to pan | Scroll to zoom | Double-click to reset)";
            }

            using var bgBrush = new SolidBrush(Color.FromArgb(190, 245, 245, 245));
            using var borderPen = new Pen(Color.FromArgb(200, 200, 200), 1f);
            using var font = new Font("Segoe UI", 8.25f);

            var size = g.MeasureString(modeText, font);
            var rect = new RectangleF(10, Height - size.Height - 14, size.Width + 12, size.Height + 6);

            g.FillRectangle(bgBrush, rect);
            g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);
            g.DrawString(modeText, font, Brushes.DarkSlateGray, rect.X + 6, rect.Y + 3);
        }
    }
}
