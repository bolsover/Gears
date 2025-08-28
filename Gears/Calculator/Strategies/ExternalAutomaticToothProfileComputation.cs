using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Calculator.Strategies
{
    internal class ExternalAutomaticToothProfileComputation : IToothProfileComputation
    {
        public ToothProfile Calculate(Gear gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            gear.Calculate();

            var rootDiameter = GetValueOrDefault(gear, VariableNames.RootDiameter);
            var baseDiameter = GetValueOrDefault(gear, VariableNames.BaseDiameter);
            var pitchDiameter = GetValueOrDefault(gear, VariableNames.PitchDiameter);
            var outsideDiameter = GetValueOrDefault(gear, VariableNames.OutsideDiameter);

            var rRoot = rootDiameter / 2.0;
           
            var rOutside = outsideDiameter / 2.0;

            var profile = new ToothProfile();
            var origin = new GearPoint(0, 0);
            // Add reference circles
            AddCircle(profile, origin, ToothFeatureKey.RootCircle, rootDiameter, true);
            AddCircle(profile, origin, ToothFeatureKey.BaseCircle, baseDiameter, true);
            AddCircle(profile, origin, ToothFeatureKey.PitchCircle, pitchDiameter, true);
            AddCircle(profile, origin, ToothFeatureKey.OutsideCircle, outsideDiameter, true);
            // Add tooth flanks
            // AddToothFlank(profile, rRoot, rOutside);
            // AddMirrorToothFlank(profile, rRoot, rOutside);
            AddRhsInvolute(gear, profile, ToothFeatureKey.RhsInvolute,  false);

            return profile;
        }

        private static void AddCircle(ToothProfile profile, GearPoint origin, ToothFeatureKey key, double diameter, bool isReference)
        {
            var radius = diameter / 2;
            ToothProfileFeatureBuilder.AddCircle(profile, key, origin, radius, isReference);
        }
       
       
        public static List<GearPoint> RhsInvolute(Gear gear)
        {
            
            var rhSinvolute = BuildBasicInvolute(gear, 25);
            var rootFilletEnd = GearPointRhsEndRootRelief(gear);
            var tipReliefStart = RhsStartOfTipRelief(gear);
            rhSinvolute = TrimmedInvolutePoints(rhSinvolute, tipReliefStart, rootFilletEnd);
            // for graphics need to invert the y-axis 
            foreach (var point in rhSinvolute)
            {
                point.Y = -point.Y;
            }
            return rhSinvolute;
        }
        
        /// <summary>
        ///     Trims the given list of points to remove any points below the intersection with the root fillet end point or above
        ///     the tip relief radius start point.
        /// </summary>
        /// <param name="involutePoints"></param>
        /// <returns></returns>
        public static List<GearPoint> TrimmedInvolutePoints(List<GearPoint> involutePoints, GearPoint tipReliefStart,
            GearPoint rootFilletEnd)
        {
            var points = PointsToIntersectionWithTipReliefArc(involutePoints, tipReliefStart);
            var result = PointsFromIntersectionWithRootFillet(points, rootFilletEnd);
            
            return result;
        }
        
        /// <summary>
        ///     Trims the given List
        ///     <GearPoint>
        ///         of involute points to remove any points above the intersection with the tip relief arc.
        ///         The final point in the returned list will be the tip relief start point .
        /// </summary>
        /// <param name="involutePoints"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<GearPoint> PointsToIntersectionWithTipReliefArc(List<GearPoint> involutePoints,
            GearPoint tipReliefStart)
        {
            var resultList = new List<GearPoint>();
            var centre = new GearPoint(0, 0);
            var radius = DistanceBetweenPoints(centre, tipReliefStart);
            GearPoint priorGearPoint = null;
            foreach (var point in involutePoints)
            {
                if (priorGearPoint != null && !IsInsideCircle(centre, radius, point) &&
                    IsInsideCircle(centre, radius, priorGearPoint))
                {
                    resultList.Add(tipReliefStart);
                }
                
                if (IsInsideCircle(centre, radius, point))
                {
                    resultList.Add(point);
                }
                
                priorGearPoint = point;
            }
            
            return resultList;
        }
        
        /// <summary>
        ///     Method to calculate if a specified checkPoint is within a circle
        /// </summary>
        /// <param name="centreGearPoint"></param>
        /// <param name="circleRadius"></param>
        /// <param name="checkGearPoint"></param>
        /// <returns>Return bool true if the checkPoint is within the specifies circle. Otherwise false</returns>
        private static bool IsInsideCircle(GearPoint centreGearPoint, double circleRadius, GearPoint checkGearPoint)
        {
            return Math.Sqrt(Math.Pow(centreGearPoint.X - checkGearPoint.X, 2) +
                             Math.Pow(centreGearPoint.Y - checkGearPoint.Y, 2)) < circleRadius;
        }
        
        /// <summary>
        ///     Returns the absolute distance between two points
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double DistanceBetweenPoints(GearPoint a, GearPoint b)
        {
            var num1 = b.X - a.X;
            var num2 = b.Y - a.Y;
            var num1Squared = num1 * num1;
            var num2Squared = num2 * num2;
            var result = Math.Sqrt(num1Squared + num2Squared);
            return result;
        }
        
        /// <summary>
        ///     Trims the given List
        ///     <GearPoint>
        ///         of involute points to remove any points below the intersection with the root fillet.
        ///         A calculated (interpolated) point will be added to ensure the root fillet ends where the involute curve starts.
        /// </summary>
        /// <param name="involutePoints"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<GearPoint> PointsFromIntersectionWithRootFillet(List<GearPoint> involutePoints,
            GearPoint rootFilletEnd)
        {
            var resultList = new List<GearPoint>();
            var centre = new GearPoint(0, 0);
            var radius = DistanceBetweenPoints(centre, rootFilletEnd);
            GearPoint priorGearPoint = null;
            foreach (var point in involutePoints)
            {
                if (priorGearPoint != null && IsInsideCircle(centre, radius, priorGearPoint) &&
                    !IsInsideCircle(centre, radius, point))
                {
                    var intersection = new GearPoint(0, 0);
                    var result = Intersect(centre, radius,
                        priorGearPoint, point, ref intersection);
                    if (result == 0)
                    {
                        resultList.Add(intersection);
                    }
                }
                
                if (!IsInsideCircle(centre, radius, point))
                {
                    resultList.Add(point);
                }
                
                priorGearPoint = point;
            }
            
            return resultList;
        }
        
        /// <summary>
        // Method to calculate the absolute position where a line intersects a circle.
        // The position is available in the ref Point Intersection.
        // Method will return -1 if:
        // 1 The line is tangential to the circle
        // 2 One or both points are exactly ON the circle
        // 3 The line intersects at two points
        //
        //This function takes the intersection point as a ref parameter and returns either -1 (no intersection) or 0 (intersection found). I used an int return value in case you want to extend this to differentiate the edge cases. The intersection is calculated from elementary geometry - remember that a line is expressed as (see : Slope Intercept and Point Slope Form )
        //
        // y = M*x + B
        //     and a circle (centered at (C.x, C.y) with radius r) is
        //
        // (x - C.x)^2 + (y - C.y)^2 - r^2 = 0
        // You solve this system of equations by substitution :
        //
        // (x - C.x)^2 + ((M*x + B) - C.y)^2 - r^2 = 0
        // Expanding and collecting terms you get :
        //
        // (1+M^2)x^2 + 2*(MB - MC.y - C.x)x + (C.x^2 + C.y^2 + B^2 - r^2 - 2B*C.y) = 0
        // This is a standard quadratic equation of the form
        //
        //     ax^2 + bx + c = 0
        // where :
        // a = (1+M^2)
        // b = 2*(MB - MC.y - C.x)
        // c = (C.x^2 + C.y^2 + B^2 - r^2 - 2B*C.y)
        // Which can be solved by the quadratic formula (see : Quadratic Formula ):
        //
        // x = (-b ± Sqrt(b^2 - 4ac))/(2a)
        // This gives two roots for the infinite line on which our line segment lies - we do a final check above to make sure that we choose the solution for our specific line segment.
        /// </summary>
        /// <param name="centreGearPoint"></param>
        /// <param name="circleRadius"></param>
        /// <param name="lineStart"></param>
        /// <param name="lineEnd"></param>
        /// <param name="intersectionGearPoint"></param>
        /// <returns>Returns 0 if a solution is found otherwise returns -1</returns>
        public static int Intersect(GearPoint centreGearPoint, double circleRadius,
            GearPoint lineStart, GearPoint lineEnd, ref GearPoint intersectionGearPoint)
        {
            if (IsIntersecting(centreGearPoint, circleRadius, lineStart, lineEnd))
            {
                //Calculate terms of the linear and quadratic equations
                var m = (lineEnd.Y - lineStart.Y) / (lineEnd.X - lineStart.X);
                var d = lineStart.Y - m * lineStart.X;
                var a = 1 + m * m;
                var b = 2 * (m * d - m * centreGearPoint.Y - centreGearPoint.X);
                var c = centreGearPoint.X * centreGearPoint.X + d * d + centreGearPoint.Y * centreGearPoint.Y -
                        circleRadius * circleRadius - 2 * d * centreGearPoint.Y;
                // solve quadratic equation
                var sqRtTerm = Math.Sqrt(b * b - 4 * a * c);
                var x = (-b + sqRtTerm) / (2 * a);
                // make sure we have the correct root for our line segment
                if (x < Math.Min(lineStart.X, lineEnd.X) ||
                    x > Math.Max(lineStart.X, lineEnd.X))
                {
                    x = (-b - sqRtTerm) / (2 * a);
                }
                
                //solve for the y-component
                var y = m * x + d;
                // Intersection Calculated
                intersectionGearPoint = new GearPoint(x, y);
                return 0;
            }
            
            // Line segment does not intersect at one point.  It is either 
            // fully outside, fully inside, intersects at two points, is 
            // tangential to, or one or more points is exactly on the 
            // circle radius.
            intersectionGearPoint = new GearPoint(0, 0);
            return -1;
        }
        
        /// <summary>
        ///     Method to calculate if a line between two points intersects a given circle
        /// </summary>
        /// <param name="circlePos"></param>
        /// <param name="circleRad"></param>
        /// <param name="lineStart"></param>
        /// <param name="lineEnd"></param>
        /// <returns></returns>
        private static bool IsIntersecting(GearPoint circlePos, double circleRad, GearPoint lineStart,
            GearPoint lineEnd)
        {
            return IsInsideCircle(circlePos, circleRad, lineStart) ^
                   IsInsideCircle(circlePos, circleRad, lineEnd);
        }
        
        
        /// <summary>
        ///     Calculates the X,Y coordinate of the point at which the tip relief radius starts.
        /// </summary>
        /// <param name="gearDesignOutputParams"></param>
        /// <returns>Start point of the tip relief radius</returns>
        public static GearPoint RhsStartOfTipRelief(Gear gear)
        {
            var baseDiameter = GetValueOrDefault(gear, VariableNames.BaseDiameter);
            var baseRadius = baseDiameter / 2;
            var outsideDiameter = GetValueOrDefault(gear, VariableNames.OutsideDiameter);
            var addendumRadius = outsideDiameter / 2;
            var tipReliefRadius = GetValueOrDefault(gear, VariableNames.TipReliefRadius);
            var distanceToInvolute = CentreToTipReliefRadiusStart(baseRadius, addendumRadius, tipReliefRadius);
            var pointc = PointOnInvolute(baseRadius, distanceToInvolute);
            return pointc;
        }
        
        /// <summary>
        ///     Calculates the distance form the gear centre (0,0) to the start of the tip release radius.
        /// </summary>
        /// <param name="baseRadius"></param>
        /// <param name="addendumRadius"></param>
        /// <param name="reliefRadius"></param>
        /// <returns>A double value. The distance from the gear centre (0,0) to the start of the tip relief radius.</returns>
        private static double CentreToTipReliefRadiusStart(double baseRadius, double addendumRadius, double reliefRadius)
        {
            var oa = addendumRadius - reliefRadius;
            var oaSquared = oa * oa;
            var opSquared = baseRadius * baseRadius;
            var paSquared = oaSquared - opSquared;
            var pa = Math.Sqrt(paSquared);
            var pc = pa + reliefRadius;
            var pcSquared = pc * pc;
            var ocSquared = pcSquared + opSquared;
            return Math.Sqrt(ocSquared);
        }
        
        /// <summary>
        ///     Calculates a point in the involute at a distance from the gear centre (0,0)
        /// </summary>
        /// <param name="baseRadius"></param>
        /// <param name="distanceToInvolute"></param>
        /// <returns>A point in the involute at a distance from the gear centre</returns>
        public static GearPoint PointOnInvolute(double baseRadius, double distanceToInvolute)
        {
            var alpha = Math.Acos(baseRadius / distanceToInvolute);
            var invAlpha = Math.Tan(alpha) - alpha; // involute function
            var x = distanceToInvolute * Math.Cos(invAlpha); // X coordinate
            var y = distanceToInvolute * Math.Sin(invAlpha); // Y coordinate
            return new GearPoint(x, y);
        }
        
        private static List<GearPoint> BuildBasicInvolute(Gear gear, int steps)
        {
            var baseDiameter = GetValueOrDefault(gear, VariableNames.BaseDiameter);
            var baseRadius = baseDiameter / 2;
            var outsideDiameter = GetValueOrDefault(gear, VariableNames.OutsideDiameter);
            var addendumRadius = outsideDiameter / 2;
            var involuteList = InvolutePoints(gear, steps);
            return involuteList;
        }
        public static GearPoint GearPointRhsEndRootRelief(Gear gear)
        {
            var rhSinvolute = BuildBasicInvolute(gear, 25);
            var rootDiameter = GetValueOrDefault(gear, VariableNames.RootDiameter);
            var rootradius = rootDiameter / 2;
            var x =
                rootradius * Math.Tan(AngleToRootFilletCentre(gear)) +
                rootradius / Math.Cos(AngleToRootFilletCentre(gear));
            
            var rootFilletEnd = new GearPoint(x, 0);
            
            var tipReliefStart = RhsStartOfTipRelief(gear);
            rhSinvolute = TrimmedInvolutePoints(rhSinvolute,
                tipReliefStart, rootFilletEnd);
            var centre = new GearPoint(0, 0);
            var angleToAdjustedRightRootFilletEnd = AngleToPointOnCircle(centre, rhSinvolute[0]);
            
            var result = rootFilletEnd.Rotate(angleToAdjustedRightRootFilletEnd);
            return result;
        }
        
        private static double AngleToRootFilletCentre(Gear gear)
        {
            var rfd = GetValueOrDefault(gear, VariableNames.RootFilletDiameter);
            var rootDiameter = GetValueOrDefault(gear, VariableNames.RootDiameter);
            var result = Math.Asin(rfd / (rootDiameter + rfd));
            return result;
        }
        
         
        /// <summary>
        ///     Returns the angle to a Point(x,y) on a circle diameter.
        /// </summary>
        /// <param name="circleCentre"></param>
        /// <param name="gearPointOnCircleDiameter"></param>
        /// <returns></returns>
        public static double AngleToPointOnCircle(GearPoint circleCentre, GearPoint gearPointOnCircleDiameter)
        {
            return Math.Atan2(gearPointOnCircleDiameter.Y - circleCentre.Y, gearPointOnCircleDiameter.X - circleCentre.X);
        }
      
        /// <summary>
        ///     Calculates the coordinates (X,Y) of points on an involute curve given the radius of the involute base
        ///     circle, the radius of the tip circle of the gear and the number of steps required.
        ///     The base circle is given as reference pitch circle diameter as db = d cos(a).
        ///     Where:
        ///     db = Base circle diameter
        ///     d is the reference pitch circle diameter (tooth count * module size)
        ///     a = Gear pressure angle;
        ///     The tip circle is usually the reference pitch circle diameter of the gear given as dt = z * m + 2ha
        ///     Where:
        ///     dt = tip circle diameter
        ///     z = number of teeth
        ///     m = module size in mm
        ///     ha = addendum (usually 1m)
        ///     Steps is the number of points to be returned for an involute curve extending from the base to the tip radii.
        ///     For 25 steps, the first step (0) will be on the base circle, the last step (24) will be on the tip circle.
        ///     Intervening points will be regularly spaced from the base circle.
        /// </summary>
        /// <param name="baseRadius"></param>
        /// <param name="addendumRadius"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        public static List<GearPoint> InvolutePoints(Gear gear, int steps)
        {
            var baseDiameter = GetValueOrDefault(gear, VariableNames.BaseDiameter);
            var baseRadius = baseDiameter / 2;
            var outsideDiameter = GetValueOrDefault(gear, VariableNames.OutsideDiameter);
            var addendumRadius = outsideDiameter / 2;
            var points = new List<GearPoint>(steps);
            var stepSize = (addendumRadius - baseRadius) / steps;
            
            for (var i = 0; i < steps + 1; i++)
            {
                var step = baseRadius + i * stepSize; // dimension of current step
                var alpha = Math.Acos(baseRadius / step);
                var invAlpha = Math.Tan(alpha) - alpha; // involute function
                var x = step * Math.Cos(invAlpha); // X coordinate
                var y = step * Math.Sin(invAlpha); // Y coordinate
                points.Add(new GearPoint(x, y));
            }
            
            return points;
        }

        private static void AddRhsInvolute(Gear gear, ToothProfile profile,  ToothFeatureKey key, bool isReference)
        {
            var rhsInvolute = RhsInvolute(gear);
            // var involuteList = BuildBasicInvolute(gear, 25);
            // var rootDiameter = GetValueOrDefault(gear, VariableNames.RootDiameter);
            // var rootradius = rootDiameter / 2;
            // var x =
            //     rootradius * Math.Tan(AngleToRootFilletCentre(gear)) +
            //     rootradius / Math.Cos(AngleToRootFilletCentre(gear));
            //
            // var rootFilletEnd = new GearPoint(x, 0);
            //
            // var tipReliefStart = RhsStartOfTipRelief(gear);
            
            var involute = new Feature(FeatureType.Spline, rhsInvolute);
            profile.AddFeature(ToothFeatureKey.RhsInvolute, involute);
        }

        /// <summary>
        /// Adds the right-hand tooth flank line from root to outside radius.
        /// </summary>
        private static void AddToothFlank(ToothProfile profile, double rRoot, double rOutside)
        {
            var flankStart = new GearPoint(rRoot, 0);
            var flankEnd = new GearPoint(rOutside, 0);
            var flank = new Feature(FeatureType.StraightLine, new List<GearPoint> { flankStart, flankEnd });
            profile.AddFeature(ToothFeatureKey.ToothFlank, flank);
        }

        /// <summary>
        /// Adds the mirrored (left-hand) tooth flank line from root to outside radius.
        /// </summary>
        private static void AddMirrorToothFlank(ToothProfile profile, double rRoot, double rOutside)
        {
            var mirrorFlankStart = new GearPoint(-rRoot, 0);
            var mirrorFlankEnd = new GearPoint(-rOutside, 0);
            var mirrorFlank = new Feature(FeatureType.StraightLine, new List<GearPoint> { mirrorFlankStart, mirrorFlankEnd });
            profile.AddFeature(ToothFeatureKey.ToothFlankMirror, mirrorFlank);
        }

        private static double GetValueOrDefault(Gear gear, VariableNames name)
        {
            var variable = gear.GetVariable(name);
            return variable?.Value ?? 0.0;
        }
    }
}
