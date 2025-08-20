namespace Bolsover.Gears.Model;

public enum FeatureType
{
    StraightLine,
    Arc,        // Defined by start, end, and center or three points
    Arc3Point, // Defined by start, mid and end points where all three points are on the arc

    Spline, // Defined by multiple points
    Circle, // Defined by center point and radius. Radius id defined in a property named 'Radius' 
 
    Rectangle
}