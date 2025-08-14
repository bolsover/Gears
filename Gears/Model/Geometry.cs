using System.Collections.Generic;
using System.Windows;

namespace Bolsover.Gears.Model;

public class Geometry
{
    /// <summary>
    /// The type of the geometry (e.g., StraightLine, Arc, Spline).
    /// </summary>
    public GeometryType Type { get; set; }

    /// <summary>
    /// The points defining the geometry.
    /// </summary>
    public List<GearPoint> Points { get; set; }

    /// <summary>
    /// Additional parameters for curves like arcs (e.g., radius, center).
    /// </summary>
    public Dictionary<string, double> Parameters { get; set; }

    /// <summary>
    /// Initializes a new instance of the Geometry class.
    /// </summary>
    /// <param name="type">Type of the geometry.</param>
    /// <param name="points">Points defining the geometry.</param>
    public Geometry(GeometryType type, List<GearPoint> points)
    {
        Type = type;
        Points = points;
        Parameters = new Dictionary<string, double>();
    }
}
