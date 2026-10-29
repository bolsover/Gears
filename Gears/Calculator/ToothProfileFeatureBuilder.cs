using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Calculator
{
    /// <summary>
    /// Helper/utility methods to add common geometric features into a ToothProfile.
    /// Centralizes construction logic used across computation strategies.
    /// </summary>
    public static class ToothProfileFeatureBuilder
    {
        /// <summary>
        /// Standardizes the pen for reference geometry (e.g., construction/reference lines).
        /// </summary>
        private static Pen CreateReferencePen()
        {
            var pen = new Pen(Color.DarkGray, 0.25f);
            // Dash pattern gives clear reference look; avoid very short segments to render well.
            pen.DashStyle = DashStyle.Dash;
            pen.DashPattern = new float[] { 5f, 3f };
            return pen;
        }

        /// <summary>
        /// Adds a circle feature identified by a key.
        /// Points: center point. Variables: Radius (clamped to >= 0).
        /// </summary>
        public static void AddCircle(ToothProfile profile, ToothFeatureKey key, GearPoint center, double radius)
        {
            AddCircle(profile, key, center, radius, false);
        }

        /// <summary>
        /// Adds a circle feature with a reference style option.
        /// </summary>
        public static void AddCircle(ToothProfile profile, ToothFeatureKey key, GearPoint center, double radius, bool isReference)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (center == null) throw new ArgumentNullException(nameof(center));

            var circle = new Feature(FeatureType.Circle, new List<GearPoint> { center });
            circle.Parameters["Radius"] = Math.Max(0.0, radius);
            if (isReference)
            {
                circle.Pen = CreateReferencePen();
            }
            profile.AddFeature(key, circle);
        }

        /// <summary>
        /// Adds a straight line feature between two points.
        /// </summary>
        public static void AddLine(ToothProfile profile, ToothFeatureKey key, GearPoint start, GearPoint end)
        {
            AddLine(profile, key, start, end, false);
        }

        /// <summary>
        /// Adds a straight line feature with a reference style option.
        /// </summary>
        public static void AddLine(ToothProfile profile, ToothFeatureKey key, GearPoint start, GearPoint end, bool isReference)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (end == null) throw new ArgumentNullException(nameof(end));

            var line = new Feature(FeatureType.StraightLine, new List<GearPoint> { start, end });
            if (isReference)
            {
                line.Pen = CreateReferencePen();
            }
            profile.AddFeature(key, line);
        }

        /// <summary>
        /// Adds an arc feature defined by center, radius and start/end angles (radians).
        /// Points: center. Variables: Radius, StartAngle, EndAngle (radians).
        /// </summary>
        public static void AddArc(ToothProfile profile, ToothFeatureKey key, GearPoint center, double radius, double startAngle, double endAngle)
        {
            AddArc(profile, key, center, radius, startAngle, endAngle, false);
        }

        /// <summary>
        /// Adds an arc feature with a reference style option.
        /// </summary>
        public static void AddArc(ToothProfile profile, ToothFeatureKey key, GearPoint center, double radius, double startAngle, double endAngle, bool isReference)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (center == null) throw new ArgumentNullException(nameof(center));

            var arc = new Feature(FeatureType.Arc, new List<GearPoint> { center });
            arc.Parameters["Radius"] = Math.Max(0.0, radius);
            arc.Parameters["StartAngle"] = startAngle;
            arc.Parameters["EndAngle"] = endAngle;
            if (isReference)
            {
                arc.Pen = CreateReferencePen();
            }
            profile.AddFeature(key, arc);
        }

        /// <summary>
        /// Adds a spline feature defined by a sequence of control points.
        /// </summary>
        public static void AddSpline(ToothProfile profile, ToothFeatureKey key, IList<GearPoint> points)
        {
            AddSpline(profile, key, points, false);
        }

        /// <summary>
        /// Adds a spline feature with a reference style option.
        /// </summary>
        public static void AddSpline(ToothProfile profile, ToothFeatureKey key, IList<GearPoint> points, bool isReference)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (points == null) throw new ArgumentNullException(nameof(points));

            var spline = new Feature(FeatureType.Spline, new List<GearPoint>(points));
            if (isReference)
            {
                spline.Pen = CreateReferencePen();
            }
            profile.AddFeature(key, spline);
        }

        /// <summary>
        /// Adds a rectangle feature defined by two opposite corners (p1, p2).
        /// The renderer may infer remaining corners; parameters optionally include Width/Height if needed later.
        /// </summary>
        public static void AddRectangle(ToothProfile profile, ToothFeatureKey key, GearPoint p1, GearPoint p2)
        {
            AddRectangle(profile, key, p1, p2, false);
        }

        /// <summary>
        /// Adds a rectangle feature with a reference style option.
        /// </summary>
        public static void AddRectangle(ToothProfile profile, ToothFeatureKey key, GearPoint p1, GearPoint p2, bool isReference)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (p1 == null) throw new ArgumentNullException(nameof(p1));
            if (p2 == null) throw new ArgumentNullException(nameof(p2));

            var rect = new Feature(FeatureType.Rectangle, new List<GearPoint> { p1, p2 });
            if (isReference)
            {
                rect.Pen = CreateReferencePen();
            }
            profile.AddFeature(key, rect);
        }
    }
}
