using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Bolsover.Gears.Model;
using static Bolsover.Gears.Calculator.ConversionUtils;


namespace Bolsover.Gears.Gui;

public class ToothProfileControl : UserControl
{
    private const double ZoomFactor = 1.2;
    private const double MinScale = 0.5;
    private const double MaxScale = 100.0;
    private const int TickSize = 5;
    private const double BaseInterval = 100;
    private const int LabelFontSize = 8;
    private const string LabelFontFamily = "Arial";
    private const int BufferSize = 5;


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
        SubscribeToEvents();
    }

    #region Initialization and Event Subscription

    /// <summary>
    /// Subscribes to relevant events for rendering and interaction.
    /// </summary>
    private void SubscribeToEvents()
    {
        Resize += (s, e) => Invalidate(); // Trigger repaint on resize.
        MouseWheel += HandleMouseWheelZoom; // Zoom with mouse wheel.
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles zooming in and out using the mouse wheel.
    /// </summary>
    private void HandleMouseWheelZoom(object sender, MouseEventArgs e)
    {
        if (e.Delta > 0)
        {
            Scale = Math.Min(Scale * ZoomFactor, MaxScale);
        }
        else if (e.Delta < 0)
        {
            Scale = Math.Max(Scale / ZoomFactor, MinScale);
        }
        Invalidate(); // Trigger a repaint.
    }

    #endregion
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        ClearBackground(e.Graphics);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        DrawAxisLines(e.Graphics);
        RenderScale(e.Graphics);
        RenderToothProfile(e.Graphics);

    }
    
    /// <summary>
    /// Clears the control background.
    /// </summary>
    private void ClearBackground(Graphics graphics) => graphics.Clear(Color.White);
   
    /// <summary>
    /// Renders the tooth profile geometries.
    /// </summary>
    private void RenderToothProfile(Graphics graphics)
    {
        if (ToothProfile == null || ToothProfile.Features.Count == 0)
            return;

        foreach (var entry in ToothProfile.Features)
        {
            DrawGeometry(graphics, entry.Value);
        }
    }


    /// <summary>
    /// Draws the axes (X and Y) for reference.
    /// </summary>
    private void DrawAxisLines(Graphics graphics)
    {
        using (var pen = new Pen(Color.LightGray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
        {
            // Offset the X-axis by -10 (Shift center horizontally)
            float xAxisOffset = Width / 2;

            // Draw horizontal axis (X-axis).
            graphics.DrawLine(pen, 0, Height / 2, Width, Height / 2);

            // Draw vertical axis (Y-axis).
            // graphics.DrawLine(pen, Width / 2, 0, Width / 2, Height);
            
            // Draw vertical axis (Y-axis), taking the offset into account.
            graphics.DrawLine(pen, xAxisOffset, 0, xAxisOffset, Height);

        }
    }


    private void DrawScale(Graphics graphics)
    {
        double scaledInterval = GetScaledInterval(Scale);
        DrawXAxisScale(graphics, scaledInterval);
        DrawYAxisScale(graphics, scaledInterval);
    }


    
    private double GetScaledInterval(double scale)
    {
        // Adjust the interval dynamically with respect to the scale
        double scaledInterval = BaseInterval / scale;
        
        // Enforce a minimum pixel distance between ticks
        double minPixelDistance = 10.0; // Minimum distance between ticks in pixels
        double adjustedInterval = Math.Max(scaledInterval, minPixelDistance / scale);
        
        return RoundToNearestMultiple(adjustedInterval, 1);
    }


    private void DrawXAxisScale(Graphics graphics, double scaledInterval)
    {
        
        using (var pen = new Pen(Color.Black))
        using (var font = new Font("Arial", 8))
        using (var brush = new SolidBrush(Color.Black))
        {
            for (double x = GetNearestStart(-Width / 2.0, scaledInterval); x <= Width / 2.0; x += scaledInterval)
            {
                float screenX = (float)(Width / 2.0 + x * Scale + Offset.X);
                if (IsOutsideVisibleArea(screenX, Width)) continue;

                DrawTick(graphics, pen, screenX, Height / 2, Height / 2, TickSize, isHorizontal: true);
                DrawLabel(graphics, font, brush, screenX, Height / 2 + TickSize, ((int)Math.Round(x)).ToString(),
                    isHorizontal: true);
            }
        }
    }
    
    private void RenderScale(Graphics graphics)
    {
      //  double scaledInterval = CalculateScaledInterval(Scale);
        double scaledInterval = GetScaledInterval(Scale);

        // Render scales on both axes
        RenderXAxisScale(graphics, scaledInterval);
        RenderYAxisScale(graphics, scaledInterval);
    }


    /// <summary>
    /// Draws X-axis scale including ticks and labels.
    /// </summary>
    private void RenderXAxisScale(Graphics graphics, double scaledInterval)
    {
        using var pen = new Pen(Color.Black);
        using var font = new Font(LabelFontFamily, LabelFontSize);

        using var brush = new SolidBrush(Color.Black);
        // Offset the X-axis by -10 (Shift center horizontally)
        float xAxisOffset = Width / 2 ;

        for (double x = GetNearestStart(-Width / 2.0, scaledInterval); x <= Width / 2.0; x += scaledInterval)
        {
            // float screenX = TransformToScreenX(x);
            float screenX = (float)(xAxisOffset + x * Scale + Offset.X);

            if (IsOutsideVisibleArea(screenX, Width)) continue;

            DrawTick(graphics, pen, screenX, Height / 2, Height / 2, TickSize, isHorizontal: true);

            DrawLabel(graphics, font, brush, screenX, Height / 2 + TickSize, ((int)Math.Round(x)).ToString(), true);
        }
    }

    /// <summary>
    /// Draws Y-axis scale including ticks and labels.
    /// </summary>
    private void RenderYAxisScale(Graphics graphics, double scaledInterval)
    {
        using var pen = new Pen(Color.Black);
        using var font = new Font(LabelFontFamily, LabelFontSize);

        using var brush = new SolidBrush(Color.Black);

        for (double y = GetNearestStart(-Height / 2.0, scaledInterval); y <= Height / 2.0; y += scaledInterval)
        {
            float screenY = TransformToScreenY(y);
            if (IsOutsideVisibleArea(screenY, Height)) continue;

            DrawTick(graphics, pen, Width / 2, screenY, Width/2,screenY ,false);
            DrawLabel(graphics, font, brush, Width / 2 - TickSize, screenY, ((int)Math.Round(y)).ToString(), false);
        }
    }

    
    private static bool IsOutsideVisibleArea(float coord, int dimensionSize) =>
        coord < -BufferSize || coord > dimensionSize + BufferSize;



    private static void DrawTick(Graphics graphics, Pen pen, float startX, float startY, float endX, float endY,
        bool isHorizontal)
    {
        if (isHorizontal)
        {
            graphics.DrawLine(pen, startX, startY - TickSize, startX, startY + TickSize);
        }
        else
        {
            graphics.DrawLine(pen, startX - TickSize, startY, startX + TickSize, startY);
        }
    }

    private static void DrawLabel(Graphics graphics, Font font, SolidBrush brush, float x, float y, string label,
        bool isHorizontal)
    {
        var labelSize = graphics.MeasureString(label, font);
        if (isHorizontal)
        {
            graphics.DrawString(label, font, brush, x - labelSize.Width / 2, y);
        }
        else
        {
            graphics.DrawString(label, font, brush, x - labelSize.Width, y - labelSize.Height / 2);
        }
    }

    private void DrawYAxisScale(Graphics graphics, double scaledInterval)
    {
        using (var pen = new Pen(Color.Black))
        using (var font = new Font("Arial", 8))
        using (var brush = new SolidBrush(Color.Black))
        {
            for (double y = GetNearestStart(-Height / 2.0, scaledInterval); y <= Height / 2.0; y += scaledInterval)
            {
                float screenY = (float)(Height / 2.0 - y * Scale + Offset.Y);
                if (IsOutsideVisibleArea(screenY, Height)) continue;

                DrawTick(graphics, pen, Width / 2, screenY, Width / 2, TickSize, isHorizontal: false);
                DrawLabel(graphics, font, brush, Width / 2 - TickSize, screenY, ((int)Math.Round(y)).ToString(),
                    isHorizontal: false);
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
    /// Draws a specific feature on the control using the given color.
    /// </summary>
    private void DrawGeometry(Graphics graphics, Feature feature)
    {
        if (feature?.Pen == null)
        {
            feature.Pen = new Pen(Color.Black, 1);
        }

        switch (feature.Type)
        {
            case FeatureType.StraightLine:
                DrawPolyline(graphics, feature);
                break;
            case FeatureType.Spline:
                DrawSpline(graphics, feature);
                break;
            case FeatureType.Arc:
                DrawArc(graphics, feature);
                break;
            case FeatureType.Circle:
                DrawCircle(graphics, feature);
                break;
            case FeatureType.Rectangle:
                DrawRectangle(graphics, feature);
                break;
        }
    }


    /// <summary>
    /// Draws a polyline for straight lines or splines.
    /// </summary>
    private void DrawPolyline(Graphics graphics, Feature feature)
    {
        var transformedPointsF = TransformAndConvertPoints(feature);

        if (transformedPointsF.Count > 1)
        {
            graphics.DrawLines(feature.Pen, transformedPointsF.ToArray());
        }
    }

    private List<PointF> TransformAndConvertPoints(Feature feature)
    {
        var transformedPoints = ScaleAndOffsetPoints(feature.Points);
        List<PointF> transformedPointsF = new List<PointF>();
        foreach (var point in transformedPoints)
        {
            transformedPointsF.Add(ToPointF(point));
        }

        return transformedPointsF;
    }

    /// <summary>
    /// Draws an arc using the given points (start, end, and center).
    /// </summary>
    /// <param name="graphics">Graphics object for drawing.</param>
    /// <param name="pen">Pen for line drawing.</param>
    /// <param name="points">The points defining the arc (start, end, center).</param>
    private void DrawArc(Graphics graphics, Feature feature)
    {
        var transformedPointsF = TransformAndConvertPoints(feature);

        if (transformedPointsF.Count < 3) return; // Not enough points for an arc.

        var start = transformedPointsF[0];
        var end = transformedPointsF[1];
        var center = transformedPointsF[2];

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
        graphics.DrawArc(feature.Pen, rect, startAngle, sweepAngle);
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


    private void DrawCircle(Graphics graphics, Feature feature)
    {
        var transformedCenter = ScaleAndOffsetPoints(feature.Points);
        var radius = feature.Parameters.TryGetValue("Radius", out var radiusValue) ? (double)radiusValue : 1;
        float scaledRadius = (float)(radius * Scale);

        var rect = new RectangleF(
            (float)(transformedCenter[0].X - scaledRadius),
            (float)(transformedCenter[0].Y - scaledRadius),
            scaledRadius * 2,
            scaledRadius * 2);

        graphics.DrawEllipse(feature.Pen, rect);
    }

    private void DrawRectangle(Graphics graphics, Feature feature)
    {
        if (feature.Points.Count < 2)
            return; // A rectangle requires at least two points (corner and opposite corner).

        var topLeft = ScaleAndOffsetPoints(new List<GearPoint> { feature.Points[0] })[0];
        var bottomRight = ScaleAndOffsetPoints(new List<GearPoint> { feature.Points[1] })[0];

        var x = (float)topLeft.X;
        var y = (float)topLeft.Y;
        var width = (float)(bottomRight.X - topLeft.X);
        var height = (float)(bottomRight.Y - topLeft.Y);


        var rect = new RectangleF(x, y, width, height);

        graphics.DrawRectangle(feature.Pen, Rectangle.Round(rect));
    }

    private void DrawSpline(Graphics graphics, Feature feature)
    {
        if (feature.Points == null || feature.Points.Count < 2)
            return; // A spline requires at least two points to be drawn.

        var transformedPointsF = TransformAndConvertPoints(feature);

        if (transformedPointsF.Count < 2)
            return; // After transformation, we must still have at least two points.


        graphics.DrawCurve(feature.Pen, transformedPointsF.ToArray());
    }
    //
    // /// <summary>
    // /// Transforms a world X coordinate to screen coordinates.
    // /// </summary>
    // private float TransformToScreenX(double x) => (float)(Width / 2.0 + x * Scale + Offset.X);
    
    
    

    /// <summary>
    /// Transforms a world Y coordinate to screen coordinates.
    /// </summary>
    private float TransformToScreenY(double y) => (float)(Height / 2.0 - y * Scale + Offset.Y);

    

}