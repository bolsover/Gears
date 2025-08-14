namespace Bolsover.Gears.Model;

public enum GeometryType
{
    StraightLine,
    Arc,        // Defined by start, end, and center or three points
    Spline, // Defined by multiple points
    Circle // Defined by center point and radius. Radius id defined in a property named 'Radius' 
}