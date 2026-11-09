using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui
{
    /// <summary>
    /// 2D (GDI+) view of a whole gear assembly: the outline of every visible gear in world X/Y coordinates, coloured by
    /// shaft, with the shafts, pitch circles and gear names as options. The mouse wheel zooms about the cursor, dragging
    /// pans, and a click selects a gear or shaft. It draws the same <see cref="AssemblyScene"/> as the 3D viewport.
    /// </summary>
    public class GearScene2DControl : Control
    {
        private static readonly Color[] Palette =
        {
            Color.FromArgb(70, 130, 180),
            Color.FromArgb(60, 160, 110),
            Color.FromArgb(170, 110, 190),
            Color.FromArgb(200, 120, 80),
            Color.FromArgb(90, 160, 170),
            Color.FromArgb(150, 150, 70)
        };

        private static readonly Color SelectedColor = Color.FromArgb(255, 170, 0);
        private static readonly Color ShaftColor = Color.FromArgb(150, 150, 155);
        private static readonly Color BeltColor = Color.FromArgb(55, 55, 60);

        private readonly List<Part> _parts = new List<Part>();
        private double _scale = 1.0;
        private double _offsetX;
        private double _offsetY;
        private bool _fitPending;
        private bool _showPitchCircles = true;
        private bool _showLabels = true;
        private IReadOnlyCollection<object> _selected = new object[0];
        private bool _dragging;
        private Point _downPoint;
        private Point _lastPoint;
        private bool _moved;
        private string _cursorText = string.Empty;

        public GearScene2DControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 8.5f);
        }

        /// <summary>Raised when the user clicks a gear or shaft. The argument is the picked <see cref="GearComponent"/> or <see cref="Shaft"/>.</summary>
        public event Action<object> ItemPicked;

        /// <summary>Number of parts (gears and shafts) currently drawn.</summary>
        public int PartCount => _parts.Count;

        /// <summary>Pixels per millimetre.</summary>
        public double Scale => _scale;

        public bool ShowPitchCircles
        {
            get => _showPitchCircles;
            set
            {
                _showPitchCircles = value;
                Invalidate();
            }
        }

        public bool ShowLabels
        {
            get => _showLabels;
            set
            {
                _showLabels = value;
                Invalidate();
            }
        }

        /// <summary>Replaces the displayed scene.</summary>
        /// <param name="scene">The solved scene; null clears the view.</param>
        /// <param name="selected">The selected gears, shafts and belts, drawn in the highlight colour; may be null.</param>
        /// <param name="hiddenGears">Gears that are not drawn; may be null.</param>
        /// <param name="showShafts">True to draw the shafts.</param>
        public void SetScene(AssemblyScene scene, IReadOnlyCollection<object> selected, ISet<GearComponent> hiddenGears, bool showShafts)
        {
            var hadParts = _parts.Count > 0;
            _parts.Clear();
            _selected = selected ?? new object[0];

            if (scene != null)
            {
                var shaftIndex = new Dictionary<Shaft, int>();
                foreach (var shaftItem in scene.ShaftItems)
                {
                    shaftIndex[shaftItem.Shaft] = shaftIndex.Count;
                    if (!showShafts) continue;
                    var hull = ConvexHull(ProjectVertices(shaftItem.Mesh));
                    if (hull.Count < 3) continue;
                    _parts.Add(new Part(shaftItem.Shaft, true, 0, new List<List<Pt>> { hull }));
                }

                foreach (var beltItem in scene.BeltItems)
                {
                    if (hiddenGears != null && (hiddenGears.Contains(beltItem.Belt.Driver) || hiddenGears.Contains(beltItem.Belt.Driven))) continue;

                    var beltLines = new List<List<Pt>>();
                    foreach (var polyline in beltItem.Polylines)
                    {
                        if (polyline.Count > 1) beltLines.Add(polyline.Select(p => new Pt(p.X, p.Y)).ToList());
                    }

                    if (beltLines.Count > 0) _parts.Add(new Part(beltItem.Belt, false, 0, beltLines) { IsBelt = true });
                }

                foreach (var item in scene.Items)
                {
                    if (hiddenGears != null && hiddenGears.Contains(item.Component)) continue;

                    var polylines = new List<List<Pt>>();
                    foreach (var polyline in item.Polylines ?? new List<List<GearPoint>>())
                    {
                        if (polyline.Count > 1) polylines.Add(polyline.Select(p => new Pt(p.X, p.Y)).ToList());
                    }

                    if (polylines.Count == 0)
                    {
                        var hull = ConvexHull(ProjectVertices(item.Mesh));
                        if (hull.Count >= 3) polylines.Add(hull);
                    }

                    if (polylines.Count == 0) continue;

                    var index = shaftIndex.TryGetValue(item.Shaft, out var i) ? i : 0;
                    var part = new Part(item.Component, false, index, polylines);
                    var centre = item.LocalToWorld.TransformPoint(new Vector3D(0.0, 0.0, 0.0));
                    part.Centre = new Pt(centre.X, centre.Y);
                    part.HasCentre = true;

                    // A pitch circle is only true to scale when the gear axis is perpendicular to the drawing plane.
                    var axis = item.LocalToWorld.TransformDirection(new Vector3D(0.0, 0.0, 1.0));
                    if ((item.Component is InvoluteGearComponent || item.Component is HtdPulleyComponent || item.Component is CycloidalGearComponent)
                        && Math.Abs(axis.Z) > 0.999 && item.Component.PitchDiameter > 0)
                    {
                        part.PitchRadius = item.Component.PitchDiameter / 2.0;
                    }

                    part.Label = item.Component.Name;
                    _parts.Add(part);
                }
            }

            if (_parts.Count == 0)
            {
                _fitPending = false;
            }
            else if (!hadParts)
            {
                _fitPending = true;
            }

            Invalidate();
        }

        /// <summary>Scales and centres the view so that every drawn part is visible.</summary>
        public void FitView()
        {
            if (!TryGetWorldBounds(out var minX, out var minY, out var maxX, out var maxY)) return;
            if (ClientSize.Width < 20 || ClientSize.Height < 20)
            {
                _fitPending = true;
                return;
            }

            const double margin = 30.0;
            var width = Math.Max(maxX - minX, 1e-6);
            var height = Math.Max(maxY - minY, 1e-6);
            _scale = Math.Min((ClientSize.Width - 2 * margin) / width, (ClientSize.Height - 2 * margin) / height);
            if (_scale <= 0 || double.IsNaN(_scale) || double.IsInfinity(_scale)) _scale = 1.0;
            _offsetX = ClientSize.Width / 2.0 - (minX + maxX) / 2.0 * _scale;
            _offsetY = ClientSize.Height / 2.0 + (minY + maxY) / 2.0 * _scale;
            _fitPending = false;
            Invalidate();
        }

        /// <summary>World bounds (millimetres) of everything drawn; false when nothing is drawn.</summary>
        public bool TryGetWorldBounds(out double minX, out double minY, out double maxX, out double maxY)
        {
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            var any = false;
            foreach (var part in _parts)
            {
                foreach (var polyline in part.Polylines)
                {
                    foreach (var p in polyline)
                    {
                        any = true;
                        if (p.X < minX) minX = p.X;
                        if (p.X > maxX) maxX = p.X;
                        if (p.Y < minY) minY = p.Y;
                        if (p.Y > maxY) maxY = p.Y;
                    }
                }
            }

            if (!any) minX = minY = maxX = maxY = 0.0;
            return any;
        }

        /// <summary>
        /// The gear or shaft at a world position: a gear whose outline contains the point (or lies within
        /// <paramref name="tolerance"/> of it) wins over a shaft; among overlapping gears the smallest wins. Null when nothing is there.
        /// </summary>
        public object PickAtWorld(double x, double y, double tolerance)
        {
            Part best = null;
            var bestScore = double.MaxValue;
            var bestArea = double.MaxValue;

            foreach (var part in _parts.Where(p => !p.IsShaft))
            {
                var inside = ContainsEvenOdd(part.Loops, x, y);
                var distance = inside ? 0.0 : DistanceToOutline(part.Polylines, x, y);
                if (!inside && distance > tolerance) continue;

                var area = part.BoundsArea;
                if (distance < bestScore - 1e-12 || (Math.Abs(distance - bestScore) <= 1e-12 && area < bestArea))
                {
                    best = part;
                    bestScore = distance;
                    bestArea = area;
                }
            }

            if (best != null) return best.Tag;

            foreach (var part in _parts.Where(p => p.IsShaft))
            {
                if (ContainsEvenOdd(part.Loops, x, y)) return part.Tag;
            }

            return null;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_fitPending) FitView();

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            DrawAxes(g);

            if (_parts.Count == 0)
            {
                using (var brush = new SolidBrush(Color.Gray))
                {
                    g.DrawString("No gears to show.", Font, brush, 8f, 8f);
                }

                return;
            }

            foreach (var part in _parts.Where(p => p.IsShaft)) DrawShaft(g, part);
            foreach (var part in _parts.Where(p => p.IsBelt)) DrawBelt(g, part);
            foreach (var part in _parts.Where(p => !p.IsShaft && !p.IsBelt)) DrawGear(g, part);

            using (var brush = new SolidBrush(Color.DimGray))
            {
                var text = _cursorText.Length > 0 ? _cursorText + "   " : string.Empty;
                text += $"Scale {_scale.ToString("0.###", CultureInfo.InvariantCulture)} px/mm";
                g.DrawString(text, Font, brush, 6f, Math.Max(0, ClientSize.Height - Font.Height - 4));
            }
        }

        private void DrawAxes(Graphics g)
        {
            using (var pen = new Pen(Color.FromArgb(225, 225, 225)))
            {
                var origin = ToScreen(new Pt(0.0, 0.0));
                if (origin.X >= 0 && origin.X <= ClientSize.Width) g.DrawLine(pen, origin.X, 0, origin.X, ClientSize.Height);
                if (origin.Y >= 0 && origin.Y <= ClientSize.Height) g.DrawLine(pen, 0, origin.Y, ClientSize.Width, origin.Y);
            }
        }

        private bool IsSelected(object tag)
        {
            foreach (var item in _selected)
            {
                if (ReferenceEquals(item, tag)) return true;
            }

            return false;
        }

        private void DrawShaft(Graphics g, Part part)
        {
            var selected = IsSelected(part.Tag);
            var points = part.Polylines[0].Select(ToScreen).ToArray();
            using (var brush = new SolidBrush(Color.FromArgb(70, ShaftColor)))
            using (var pen = new Pen(selected ? SelectedColor : ShaftColor, selected ? 2.5f : 1f))
            {
                g.FillPolygon(brush, points);
                g.DrawPolygon(pen, points);
            }
        }

        private void DrawBelt(Graphics g, Part part)
        {
            var selected = IsSelected(part.Tag);
            using (var path = new GraphicsPath(FillMode.Alternate))
            {
                foreach (var polyline in part.Polylines) path.AddPolygon(polyline.Select(ToScreen).ToArray());
                using (var brush = new SolidBrush(Color.FromArgb(110, BeltColor)))
                using (var pen = new Pen(selected ? SelectedColor : BeltColor, selected ? 2.5f : 1.2f))
                {
                    g.FillPath(brush, path);
                    g.DrawPath(pen, path);
                }
            }
        }

        private void DrawGear(Graphics g, Part part)
        {
            var selected = IsSelected(part.Tag);
            var color = selected ? SelectedColor : Palette[part.ColorIndex % Palette.Length];

            if (_showPitchCircles && part.PitchRadius > 0 && part.HasCentre)
            {
                var centre = ToScreen(part.Centre);
                var radius = (float)(part.PitchRadius * _scale);
                using (var pen = new Pen(Color.FromArgb(150, color), 1f) { DashStyle = DashStyle.Dash })
                {
                    if (radius > 1f && radius < 20000f) g.DrawEllipse(pen, centre.X - radius, centre.Y - radius, 2f * radius, 2f * radius);
                }
            }

            using (var pen = new Pen(color, selected ? 2.5f : 1.4f) { LineJoin = LineJoin.Round })
            {
                foreach (var polyline in part.Polylines)
                {
                    if (polyline.Count < 2) continue;
                    g.DrawLines(pen, polyline.Select(ToScreen).ToArray());
                }
            }

            if (part.HasCentre)
            {
                var centre = ToScreen(part.Centre);
                using (var pen = new Pen(color, 1f))
                {
                    g.DrawLine(pen, centre.X - 4f, centre.Y, centre.X + 4f, centre.Y);
                    g.DrawLine(pen, centre.X, centre.Y - 4f, centre.X, centre.Y + 4f);
                }

                if (_showLabels && !string.IsNullOrEmpty(part.Label))
                {
                    using (var brush = new SolidBrush(color))
                    {
                        g.DrawString(part.Label, Font, brush, centre.X + 5f, centre.Y + 3f);
                    }
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Focus();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Middle) return;
            _dragging = true;
            _moved = false;
            _downPoint = e.Location;
            _lastPoint = e.Location;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var world = ToWorld(e.Location);
            _cursorText = $"X {world.X.ToString("0.###", CultureInfo.InvariantCulture)}  Y {world.Y.ToString("0.###", CultureInfo.InvariantCulture)} mm";

            if (_dragging)
            {
                if (Math.Abs(e.X - _downPoint.X) > 3 || Math.Abs(e.Y - _downPoint.Y) > 3) _moved = true;
                if (_moved)
                {
                    _offsetX += e.X - _lastPoint.X;
                    _offsetY += e.Y - _lastPoint.Y;
                    _lastPoint = e.Location;
                }
            }

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!_dragging) return;
            _dragging = false;
            if (_moved || e.Button != MouseButtons.Left) return;

            var world = ToWorld(e.Location);
            var picked = PickAtWorld(world.X, world.Y, 6.0 / _scale);
            if (picked != null) ItemPicked?.Invoke(picked);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            var factor = e.Delta > 0 ? 1.15 : 1.0 / 1.15;
            var newScale = Math.Max(1e-3, Math.Min(1e5, _scale * factor));
            factor = newScale / _scale;

            // Keep the world point under the cursor fixed.
            _offsetX = e.X - (e.X - _offsetX) * factor;
            _offsetY = e.Y - (e.Y - _offsetY) * factor;
            _scale = newScale;
            Invalidate();
        }

        private PointF ToScreen(Pt p)
        {
            return new PointF((float)(_offsetX + p.X * _scale), (float)(_offsetY - p.Y * _scale));
        }

        private Pt ToWorld(Point screen)
        {
            return new Pt((screen.X - _offsetX) / _scale, (_offsetY - screen.Y) / _scale);
        }

        private static List<Pt> ProjectVertices(GearMesh3DBuilder.Mesh3D mesh)
        {
            var points = new List<Pt>();
            if (mesh == null) return points;
            foreach (var v in mesh.Vertices) points.Add(new Pt(v.X, v.Y));
            return points;
        }

        /// <summary>Convex hull (Andrew's monotone chain), anticlockwise.</summary>
        private static List<Pt> ConvexHull(List<Pt> points)
        {
            var sorted = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            var unique = new List<Pt>();
            foreach (var p in sorted)
            {
                if (unique.Count == 0 || Math.Abs(p.X - unique[unique.Count - 1].X) > 1e-9 || Math.Abs(p.Y - unique[unique.Count - 1].Y) > 1e-9) unique.Add(p);
            }

            if (unique.Count < 3) return unique;

            var hull = new List<Pt>();
            foreach (var p in unique)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0) hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }

            var lowerCount = hull.Count + 1;
            for (var i = unique.Count - 2; i >= 0; i--)
            {
                var p = unique[i];
                while (hull.Count >= lowerCount && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0) hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }

            hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        private static double Cross(Pt o, Pt a, Pt b)
        {
            return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
        }

        /// <summary>
        /// Joins polylines whose end points meet into longer polylines. A gear outline is generated as many short pieces
        /// (trochoid, involute and arc segments of each tooth); chaining them gives the closed loops that a point-in-polygon
        /// test needs.
        /// </summary>
        private static List<List<Pt>> ChainLoops(List<List<Pt>> polylines)
        {
            const double tolerance = 1e-4;
            var remaining = polylines.Where(p => p.Count > 1).Select(p => new List<Pt>(p)).ToList();
            var loops = new List<List<Pt>>();

            while (remaining.Count > 0)
            {
                var chain = remaining[remaining.Count - 1];
                remaining.RemoveAt(remaining.Count - 1);

                var extended = true;
                while (extended && remaining.Count > 0)
                {
                    extended = false;
                    var end = chain[chain.Count - 1];
                    for (var i = 0; i < remaining.Count; i++)
                    {
                        var candidate = remaining[i];
                        if (Near(end, candidate[0], tolerance))
                        {
                            chain.AddRange(candidate.Skip(1));
                        }
                        else if (Near(end, candidate[candidate.Count - 1], tolerance))
                        {
                            for (var k = candidate.Count - 2; k >= 0; k--) chain.Add(candidate[k]);
                        }
                        else
                        {
                            continue;
                        }

                        remaining.RemoveAt(i);
                        extended = true;
                        break;
                    }

                    if (Near(chain[0], chain[chain.Count - 1], tolerance) && chain.Count > 2) break;
                }

                loops.Add(chain);
            }

            return loops;
        }

        private static bool Near(Pt a, Pt b, double tolerance)
        {
            return Math.Abs(a.X - b.X) <= tolerance && Math.Abs(a.Y - b.Y) <= tolerance;
        }

        /// <summary>Even-odd point-in-polygon test over all loops (each is treated as closed), so bores are holes.</summary>
        private static bool ContainsEvenOdd(List<List<Pt>> polylines, double x, double y)
        {
            var inside = false;
            foreach (var polyline in polylines)
            {
                var n = polyline.Count;
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    var a = polyline[i];
                    var b = polyline[j];
                    if ((a.Y > y) != (b.Y > y) && x < (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
                }
            }

            return inside;
        }

        private static double DistanceToOutline(List<List<Pt>> polylines, double x, double y)
        {
            var best = double.MaxValue;
            foreach (var polyline in polylines)
            {
                for (var i = 1; i < polyline.Count; i++)
                {
                    var d = DistanceToSegment(polyline[i - 1], polyline[i], x, y);
                    if (d < best) best = d;
                }
            }

            return best;
        }

        private static double DistanceToSegment(Pt a, Pt b, double x, double y)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var lengthSquared = dx * dx + dy * dy;
            var t = lengthSquared < 1e-18 ? 0.0 : Math.Max(0.0, Math.Min(1.0, ((x - a.X) * dx + (y - a.Y) * dy) / lengthSquared));
            var px = a.X + t * dx - x;
            var py = a.Y + t * dy - y;
            return Math.Sqrt(px * px + py * py);
        }

        private struct Pt
        {
            public Pt(double x, double y)
            {
                X = x;
                Y = y;
            }

            public double X { get; }
            public double Y { get; }
        }

        private sealed class Part
        {
            public Part(object tag, bool isShaft, int colorIndex, List<List<Pt>> polylines)
            {
                Tag = tag;
                IsShaft = isShaft;
                ColorIndex = colorIndex;
                Polylines = polylines;
                Loops = ChainLoops(polylines);

                var minX = double.MaxValue;
                var minY = double.MaxValue;
                var maxX = double.MinValue;
                var maxY = double.MinValue;
                foreach (var polyline in polylines)
                {
                    foreach (var p in polyline)
                    {
                        if (p.X < minX) minX = p.X;
                        if (p.X > maxX) maxX = p.X;
                        if (p.Y < minY) minY = p.Y;
                        if (p.Y > maxY) maxY = p.Y;
                    }
                }

                BoundsArea = Math.Max(maxX - minX, 0.0) * Math.Max(maxY - minY, 0.0);
            }

            public object Tag { get; }
            public bool IsShaft { get; }

            /// <summary>True for a timing belt: a ring between its two outlines.</summary>
            public bool IsBelt { get; set; }

            public int ColorIndex { get; }
            public List<List<Pt>> Polylines { get; }

            /// <summary>The polylines chained into loops, for hit testing.</summary>
            public List<List<Pt>> Loops { get; }

            public double BoundsArea { get; }
            public Pt Centre { get; set; }
            public bool HasCentre { get; set; }
            public double PitchRadius { get; set; }
            public string Label { get; set; }
        }
    }
}
