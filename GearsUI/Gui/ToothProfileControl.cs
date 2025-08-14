using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Bolsover.Gears.Model;
using static Bolsover.Gears.Calculator.ConversionUtils;


namespace Bolsover.Gears.Gui;

public class ToothProfileControl : UserControl
{
    /// <summary>
    /// The ToothProfile that will be rendered on the control.
    /// </summary>
    public ToothProfile ToothProfile { get; set; }

    /// <summary>
    /// Scale factor for zooming in/out.
    /// </summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>
    /// Offset for panning the rendered profile.
    /// </summary>
    public PointF Offset { get; set; } = new PointF(0, 0);

    /// <summary>
    /// Constructor for the control.
    /// </summary>
    public ToothProfileControl()
    {
    ToothProfile = new ToothProfile();
    DoubleBuffered = true; // Reduce flicker during rendering.
    
    //Subscribe to resize event to trigger repaint.
    this.Resize += OnResize;

    // Subscribe to mouse wheel event to enable zooming.
    this.MouseWheel += OnMouseWheel;
}

/// <summary>
/// Handles the mouse wheel event to zoom in and out by adjusting the Scale property.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="e">Mouse wheel event arguments.</param>
private void OnMouseWheel(object sender, MouseEventArgs e)
{
    const double zoomFactor = 1.2; // Factor by which to zoom.
    const double minScale = 0.1; // Minimum scale value.
    const double maxScale = 10.0; // Maximum scale value.

    if (e.Delta > 0)
    {
        // Zoom in (increase scale)
        Scale = Math.Min(Scale * zoomFactor, maxScale);
    }
    else if (e.Delta < 0)
    {
        // Zoom out (decrease scale)
        Scale = Math.Max(Scale / zoomFactor, minScale);
    }

    // Trigger a repaint to reflect the updated scale.
    Invalidate();
}

/// <summary>
/// Handles the Resize event and triggers a repaint of the control.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="e">Event arguments.</param>
private void OnResize(object sender, EventArgs e)
{
    // Invalidate the control to force a repaint when resized.
    Invalidate();
}

protected override void OnPaint(PaintEventArgs e)
{
    base.OnPaint(e);

    // Clear the background.
    e.Graphics.Clear(Color.White);

    // Enable anti-aliasing for smooth rendering.
    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

    // Draw the axis lines.
    DrawAxisLines(e.Graphics);

    // Draw the scales along the X and Y axes.
    DrawScale(e.Graphics);

    // If there is no ToothProfile or no geometries to draw, stop rendering.
    if (ToothProfile == null || ToothProfile.Geometries.Count == 0)
    {
        return;
    }

    // Iterate through and draw each geometry in the ToothProfile.
    foreach (var entry in ToothProfile.Geometries)
    {
        var key = entry.Key;
        var geometry = entry.Value;

        // Choose a default color for each geometry. You can customize the colors as needed.
        var color = key switch
        {
            "Root" => Color.Red,
            "Outside" => Color.Black,
            "Centreline" => Color.Gray,
            _ => Color.Blue, // Default color
        };

        DrawGeometry(e.Graphics, geometry, color);
    }
}

/// <summary>
/// Draws the axes (X and Y) for reference.
/// </summary>
private void DrawAxisLines(Graphics graphics)
{
    using (var pen = new Pen(Color.LightGray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
    {
        // Draw horizontal axis (X-axis).
        graphics.DrawLine(pen, 0, Height / 2, Width, Height / 2);

        // Draw vertical axis (Y-axis).
        graphics.DrawLine(pen, Width / 2, 0, Width / 2, Height);
    }
}

private void DrawScale(Graphics graphics)
{
    const int tickSize = 5; // Length of the tick marks.
    const double baseInterval = 50; // Base interval in screen pixels before scaling.
    double scaledInterval = baseInterval / Scale; // Adjust interval based on the zoom.

    // Ensure the interval aligns with multiples of 5 in the physical coordinate space.
    scaledInterval = RoundToNearestMultiple(scaledInterval, 5);

    using (var pen = new Pen(Color.Black))
    using (var font = new Font("Arial", 8)) // Set the font for rendering labels.
    using (var brush = new SolidBrush(Color.Black))
    {
        // Draw X-axis scale (horizontal)
        for (double x = GetNearestStart(-Width / 2.0, scaledInterval);
             x <= Width / 2.0;
             x += scaledInterval)
        {
            float screenX = (float)(Width / 2.0 + x * Scale + Offset.X);

            // Skip tick marks and labels that fall outside the visible area.
            if (screenX < 0 || screenX > Width)
                continue;

            // Draw the tick mark.
            graphics.DrawLine(pen, screenX, Height / 2 - tickSize, screenX, Height / 2 + tickSize);

            // Draw the label.
            string label = ((int)Math.Round(x)).ToString();
            var labelSize = graphics.MeasureString(label, font);
            graphics.DrawString(label, font, brush, screenX - labelSize.Width / 2, Height / 2 + tickSize);
        }

        // Draw Y-axis scale (vertical)
        for (double y = GetNearestStart(-Height / 2.0, scaledInterval);
             y <= Height / 2.0;
             y += scaledInterval)
        {
            float screenY = (float)(Height / 2.0 - y * Scale + Offset.Y);

            // Skip tick marks and labels that fall outside the visible area.
            if (screenY < 0 || screenY > Height)
                continue;

            // Draw the tick mark.
            graphics.DrawLine(pen, Width / 2 - tickSize, screenY, Width / 2 + tickSize, screenY);

            // Draw the label.
            string label = ((int)Math.Round(y)).ToString();
            var labelSize = graphics.MeasureString(label, font);
            graphics.DrawString(label, font, brush, Width / 2 - tickSize - labelSize.Width, screenY - labelSize.Height / 2);
        }
    }
}

/// <summary>
/// Rounds a value to the nearest multiple of the given base.
/// </summary>
private double RoundToNearestMultiple(double value, double multiple)
{
    return Math.Round(value / multiple) * multiple;
}

/// <summary>
/// Calculates the nearest start point for tick marks, ensuring alignment with the grid.
/// </summary>
/// <param name="minValue">The minimum visible value.</param>
/// <param name="interval">The distance between tick marks.</param>
/// <returns>A starting point that is a multiple of the interval.</returns>
private double GetNearestStart(double minValue, double interval)
{
    return Math.Floor(minValue / interval) * interval;
}
    
    /// <summary>
    /// Draws a specific geometry on the control using the given color.
    /// </summary>
    private void DrawGeometry(Graphics graphics, Geometry geometry, Color color)
    {
        using var pen = new Pen(color, 2f);
        
        
        {
            var transformedPoints = ScaleAndOffsetPoints(geometry.Points);
            List<PointF> transformedPointsF = new List<PointF>();
            foreach (var point in transformedPoints)
            {
                transformedPointsF.Add(ToPointF(point));
            }

            switch (geometry.Type)
            {
                case GeometryType.StraightLine:
                case GeometryType.Spline:
                    DrawPolyline(graphics, pen, transformedPointsF);
                    break;
                case GeometryType.Arc:
                    DrawArc(graphics, pen, transformedPointsF);
                    break;
                case GeometryType.Circle:
                    DrawCircle(graphics, pen, geometry);
                    break;
            }
        }
    }
    
    /// <summary>
    /// Draws a polyline for straight lines or splines.
    /// </summary>
    private void DrawPolyline(Graphics graphics, Pen pen, List<PointF> points)
    {
        if (points.Count > 1)
        {
            graphics.DrawLines(pen, points.ToArray());
        }
    }
    
    /// <summary>
    /// Draws an arc using the given points (start, end, and center).
    /// </summary>
    /// <param name="graphics">Graphics object for drawing.</param>
    /// <param name="pen">Pen for line drawing.</param>
    /// <param name="points">The points defining the arc (start, end, center).</param>
    private void DrawArc(Graphics graphics, Pen pen, List<PointF> points)
    {
        if (points.Count < 3) return; // Not enough points for an arc.
    
        var start = points[0];
        var end = points[1];
        var center = points[2];
    
        float radius = (float)Math.Sqrt(Math.Pow(center.X - start.X, 2) + Math.Pow(center.Y - start.Y, 2));
    
        // Calculate angles for the arc
        float startAngle = (float)(Math.Atan2(start.Y - center.Y, start.X - center.X) * 180 / Math.PI);
        float endAngle = (float)(Math.Atan2(end.Y - center.Y, end.X - center.X) * 180 / Math.PI);
        float sweepAngle = endAngle - startAngle;
    
        // Ensure the angles are positive and correct the orientation if needed.
        if (sweepAngle < 0)
        {
            sweepAngle += 360;
        }
    
        var rect = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        graphics.DrawArc(pen, rect, startAngle, sweepAngle);
    }
    
    /// <summary>
    /// Scales and offsets points according to the current Zoom and Offset settings.
    /// </summary>
    private List<GearPoint> ScaleAndOffsetPoints(List<GearPoint> points)
    {
        var result = new List<GearPoint>();
        foreach (var point in points)
        {
            var transformedPoint = new GearPoint(
                (double)(point.X * Scale + Offset.X + Width / 2),
                (double)(point.Y * Scale + Offset.Y + Height / 2)
            );
            result.Add(transformedPoint);
        }
        return result;
    }
    
    
 

private void DrawCircle(Graphics graphics, Pen pen, Geometry circle)
{
    var transformedCenter = ScaleAndOffsetPoints(circle.Points);
    var radius = circle.Parameters.TryGetValue("Radius", out var radiusValue) ? (double)radiusValue : 1;
    float scaledRadius = (float)(radius * Scale);

    var rect = new RectangleF(
        (float)(transformedCenter[0].X - scaledRadius),
        (float)(transformedCenter[0].Y - scaledRadius),
        scaledRadius * 2,
        scaledRadius * 2);

    graphics.DrawEllipse(pen, rect);
}
}