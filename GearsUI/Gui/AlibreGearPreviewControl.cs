using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui
{
    /// <summary>
    /// A lightweight control that renders a collection of polylines (each a list of
    /// <see cref="GearPoint"/> in gear/world coordinates). The view auto-fits the geometry to the
    /// control and supports mouse-wheel zoom and drag panning.
    /// </summary>
    public class AlibreGearPreviewControl : UserControl
    {
        private const float MarginPx = 20f;
        private const double ZoomFactor = 1.2;
        private const double MinZoom = 0.05;
        private const double MaxZoom = 200.0;

        private List<List<GearPoint>> _polylines = new List<List<GearPoint>>();
        private double _userZoom = 1.0;
        private PointF _userPan = PointF.Empty;
        private bool _dragging;
        private Point _lastMouse;

        /// <summary>
        /// The polylines to render, in gear/world coordinates. Setting resets the pan/zoom fit.
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
        /// Pen used to draw the profile lines.
        /// </summary>
        public Color LineColor { get; set; } = Color.MidnightBlue;

        public AlibreGearPreviewControl()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Resize += (s, e) => Invalidate();
            MouseWheel += HandleMouseWheelZoom;
            MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _dragging = true;
                    _lastMouse = e.Location;
                }
            };
            MouseUp += (s, e) => _dragging = false;
            MouseMove += HandleMouseMove;
            DoubleClick += (s, e) => ResetView();
        }

        /// <summary>
        /// Resets zoom and pan so the geometry re-fits the control.
        /// </summary>
        public void ResetView()
        {
            _userZoom = 1.0;
            _userPan = PointF.Empty;
            Invalidate();
        }

        private void HandleMouseWheelZoom(object sender, MouseEventArgs e)
        {
            _userZoom = e.Delta > 0
                ? Math.Min(_userZoom * ZoomFactor, MaxZoom)
                : Math.Max(_userZoom / ZoomFactor, MinZoom);
            Invalidate();
        }

        private void HandleMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            _userPan = new PointF(_userPan.X + (e.X - _lastMouse.X), _userPan.Y + (e.Y - _lastMouse.Y));
            _lastMouse = e.Location;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (!TryGetBounds(out var minX, out var minY, out var maxX, out var maxY))
            {
                DrawAxes(g, Width / 2f, Height / 2f);
                return;
            }

            // Fit-to-control scale (world -> screen), leaving padding.
            var worldWidth = Math.Max(maxX - minX, 1e-6);
            var worldHeight = Math.Max(maxY - minY, 1e-6);
            var scaleX = (Width - 2 * MarginPx) / worldWidth;
            var scaleY = (Height - 2 * MarginPx) / worldHeight;
            var fitScale = Math.Min(scaleX, scaleY) * _userZoom;

            // World centre maps to control centre (plus user pan).
            var worldCx = (minX + maxX) / 2.0;
            var worldCy = (minY + maxY) / 2.0;
            var screenCx = Width / 2f + _userPan.X;
            var screenCy = Height / 2f + _userPan.Y;

            PointF ToScreen(GearPoint p) => new PointF(
                (float) (screenCx + (p.X - worldCx) * fitScale),
                (float) (screenCy - (p.Y - worldCy) * fitScale)); // Y flipped for screen coords

            DrawAxes(g, screenCx, screenCy);

            using (var pen = new Pen(LineColor, 1.5f))
            {
                foreach (var polyline in _polylines)
                {
                    if (polyline == null || polyline.Count < 2) continue;
                    var pts = new PointF[polyline.Count];
                    for (var i = 0; i < polyline.Count; i++) pts[i] = ToScreen(polyline[i]);
                    g.DrawLines(pen, pts);
                }
            }
        }

        private void DrawAxes(Graphics g, float cx, float cy)
        {
            using (var pen = new Pen(Color.LightGray, 1) { DashStyle = DashStyle.Dash })
            {
                g.DrawLine(pen, 0, cy, Width, cy);
                g.DrawLine(pen, cx, 0, cx, Height);
            }
        }

        private bool TryGetBounds(out double minX, out double minY, out double maxX, out double maxY)
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

            return any;
        }
    }
}
