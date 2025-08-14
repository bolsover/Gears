using System;
using System.Drawing;

namespace Bolsover.Gears.Model
{
    public class ToothProfileRenderer
    {
        
        public void GenerateTrochoidalRootRelief(
            double hobRadius,
            double gearRotationStart,
            double gearRotationEnd,
            double feedRate,
            int segments = 100)
        {
            // RootRelief.Clear();

            // Simulate the hob cutting path
            for (int i = 0; i <= segments; i++)
            {
                double t = (double)i / segments;
                double theta = gearRotationStart + t * (gearRotationEnd - gearRotationStart); // gear rotation angle (radians)
                double feed = t * feedRate; // linear feed of the hob

                // Hob center position in gear coordinates
                double hobX = feed;
                double hobY = hobRadius;

                // Cutting point on hob periphery (rotated by theta)
                double cutX = hobX + hobRadius * Math.Cos(theta);
                double cutY = hobY - hobRadius * Math.Sin(theta);

                // RootRelief.Add(new Point2D(cutX, cutY));
            }
        }
        
        
        // var profile = new GearToothProfile();
        // profile.GenerateTrochoidalRootRelief(
        // hobRadius: 2.0,
        // gearRotationStart: 0,
        // gearRotationEnd: Math.PI / 4,  // simulate 45° of gear rotation
        // feedRate: 1.0
        // );


        
        /// <summary>
        /// Renders a ToothProfile onto a Bitmap image.
        /// </summary>
        /// <param name="toothProfile">The tooth profile to render.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <returns>A Bitmap image containing the rendered tooth profile.</returns>
        public Bitmap RenderToImage(ToothProfile toothProfile, int width, int height)
        {
            // Create a blank bitmap to render the tooth profile.
            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // Set up a white background.
                graphics.Clear(Color.White);

                // Set anti-aliasing for smooth curves.
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw each feature of the tooth profile.
                // DrawFeature(graphics, toothProfile.RootGeometry, Color.Red, 2);
                // DrawFeature(graphics, toothProfile.RootRelief, Color.Blue, 2);
                // DrawFeature(graphics, toothProfile.RootToBase, Color.Green, 2);
                // DrawFeature(graphics, toothProfile.InvoluteGeometry, Color.Orange, 2);
                // DrawFeature(graphics, toothProfile.TipRelief, Color.Purple, 2);
                // DrawFeature(graphics, toothProfile.OutsideGeometry, Color.Black, 2);

                // Optionally, add labels or axis lines to improve visualization.
                AddAxisLines(graphics, width, height);
            }

            return bitmap;
        }

        /// <summary>
        /// Draws a single feature (a series of points) onto the Graphics object.
        /// </summary>
        /// <param name="graphics">The Graphics object to draw on.</param>
        /// <param name="feature">The feature (list of points) to draw.</param>
        /// <param name="color">The color of the feature.</param>
        /// <param name="thickness">The line thickness.</param>
        private void DrawFeature(Graphics graphics, System.Collections.Generic.List<PointF> feature, Color color, float thickness)
        {
            if (feature.Count > 1)
            {
                using (Pen pen = new Pen(color, thickness))
                {
                    graphics.DrawLines(pen, feature.ToArray());
                }
            }
        }

        /// <summary>
        /// Adds X and Y axis lines to the drawing for reference.
        /// </summary>
        /// <param name="graphics">The Graphics object to draw on.</param>
        /// <param name="width">The width of the canvas.</param>
        /// <param name="height">The height of the canvas.</param>
        private void AddAxisLines(Graphics graphics, int width, int height)
        {
            using (Pen axisPen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                // Draw horizontal X-axis (centered vertically).
                graphics.DrawLine(axisPen, new Point(0, height / 2), new Point(width, height / 2));

                // Draw vertical Y-axis (centered horizontally).
                graphics.DrawLine(axisPen, new Point(width / 2, 0), new Point(width / 2, height));
            }
        }
    }
}