using System.Collections.Generic;
using System.Drawing;
using System.Windows;

namespace Bolsover.Gears.Model;

/// <summary>
/// Represents a geometric feature that can be drawn, defined by a type, a set of points, and associated parameters.
/// </summary>
public record Feature
{
    /// <summary>
    /// The type of the feature (e.g., StraightLine, Arc, Spline).
    /// </summary>
    public FeatureType Type { get; set; }

    /// <summary>
    /// The points defining the feature.
    /// </summary>
    public List<GearPoint> Points { get; set; }

    /// <summary>
    /// Additional parameters for curves like arcs (e.g., radius, center).
    /// </summary>
    public Dictionary<string, double> Parameters { get; set; }

    /// <summary>
    /// Gets or sets the pen used to define the appearance of the feature's outline and stroke properties.
    /// </summary>
    public Pen Pen { get; set; }

    /// <summary>
    /// Initializes a new instance of the Feature class.
    /// </summary>
    /// <param id="type">Type of the feature.</param>
    /// <param id="points">Points defining the feature.</param>
    public Feature(FeatureType type, List<GearPoint> points)
    {
        Type = type;
        Points = points;
        Parameters = new Dictionary<string, double>();
    }
}
