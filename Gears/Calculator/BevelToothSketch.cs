using System;
using System.Collections.Generic;

namespace Bolsover.Gears.Calculator
{
    /// <summary>
    /// A point of the tooth sketch on the developed back cone: the distance from the back cone apex (pointC) and the angle
    /// around it, in radians. The valley centre line is at angle 0 and positive angles run in the direction of rotation.
    /// </summary>
    public readonly struct BevelToothPoint
    {
        public BevelToothPoint(double radius, double angle)
        {
            Radius = radius;
            Angle = angle;
        }

        public double Radius { get; }
        public double Angle { get; }

        /// <summary>Position in the plane of the developed back cone (the back cone apex is the origin).</summary>
        public double X => Radius * Math.Cos(Angle);

        public double Y => Radius * Math.Sin(Angle);

        public override string ToString() => $"(r {Radius:0.####}, {Angle * 180.0 / Math.PI:0.####} deg)";
    }

    /// <summary>
    /// The closed sketch of one tooth valley, drawn on the back cone face of a <see cref="BevelBlankProfile"/> using Tredgold's
    /// approximation: the back cone, which is perpendicular to the pitch cone, is developed into a plane where it becomes an
    /// ordinary spur gear. Its pitch radius is the back cone distance (pointC to pointA) and its tooth count is
    /// <c>2 * back cone distance / module</c>, which is the virtual tooth count <c>z / cos(pitch cone angle)</c>. The involute
    /// flanks of that spur gear are laid back onto the back cone.
    /// <para>
    /// The sketch is the valley between two adjacent teeth: the root arc, the two trochoidal root fillets, the two involute flanks
    /// and, closing the loop, an arc outside the tooth tips linking the end points of the flanks. Because every line of a straight
    /// bevel tooth runs to the cone apex, the same sketch scaled about the apex gives the valley at the toe, and a loft between the
    /// two cuts the valley.
    /// </para>
    /// <para>
    /// The root fillets are true trochoids: the path of the sharp tip corner of the generating rack (addendum equal to the dedendum of
    /// the gear, flanks at the pressure angle) as the rack rolls on the pitch circle of the developed back cone. Each fillet leaves
    /// the root arc tangentially at the end of the flat of the rack tip and runs up to the involute flank. Where the gear has few
    /// virtual teeth the corner undercuts the flank, and the fillet then ends where it crosses the involute.
    /// </para>
    /// <para>
    /// With a rounded rack tip the corner is an arc of the given radius, tangent to the flat of the tip and to the flank. The fillet is
    /// then the offset, by the tip radius, of the trochoid traced by the centre of that arc, and it joins the involute where the flank
    /// tangent point of the arc reaches the line of action.
    /// </para>
    /// <para>
    /// For the Gleason system the blank carries a profile shift (long pinion addendum, short gear addendum): the generating rack is
    /// moved out by the shift, so the tooth is thicker (pinion) or thinner (gear) at the pitch circle and the dedendum follows the
    /// Gleason depth.
    /// </para>
    /// </summary>
    public sealed class BevelToothSketch
    {
        private const int RootSteps = 6;
        private const int FilletSteps = 10;
        private const int OuterSteps = 8;
        private const int LandStepsPerPitch = 3;
        private const double MaxLandStep = 0.02;
        private const double MergeTolerance = 1e-10;

        private readonly double _halfPitchAngle;
        private readonly double _involutePressureAngle;
        private readonly double _shiftAngle;
        private readonly double _backlashHalfAngle;
        private readonly double _backlash;
        private readonly double _rackTipRadius;
        private double _centreU;
        private double _centreV;
        private readonly List<BevelToothPoint> _rootArc = new List<BevelToothPoint>();
        private readonly List<BevelToothPoint> _rightFillet = new List<BevelToothPoint>();
        private readonly List<BevelToothPoint> _leftFillet = new List<BevelToothPoint>();
        private readonly List<BevelToothPoint> _rightFlank = new List<BevelToothPoint>();
        private readonly List<BevelToothPoint> _leftFlank = new List<BevelToothPoint>();
        private readonly List<BevelToothPoint> _outerArc = new List<BevelToothPoint>();
        private readonly List<BevelToothPoint> _loop = new List<BevelToothPoint>();
        private int _tipIndex;

        /// <param name="profile">The blank that carries the teeth.</param>
        /// <param name="pressureAngleDeg">Pressure angle of the involute flanks, degrees.</param>
        /// <param name="flankSteps">Number of segments of each flank between the start of the involute and the tooth tip.</param>
        /// <param name="rackTipRadiusFactor">Corner radius of the generating rack tip as a multiple of the module; zero is a sharp corner.</param>
        /// <param name="backlash">Circular backlash (mm) on the pitch circle of the developed back cone, as for a spur gear: the tooth is thinner and
        /// the valley wider by this amount, so the generating rack is thicker by it. Zero gives the nominal tooth thickness.</param>
        public BevelToothSketch(BevelBlankProfile profile, double pressureAngleDeg = 20.0, int flankSteps = 12, double rackTipRadiusFactor = 0.0,
            double backlash = 0.0)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (!(backlash >= 0.0) || double.IsInfinity(backlash)) throw new ArgumentException("The backlash cannot be negative.", nameof(backlash));
            if (!(rackTipRadiusFactor >= 0.0 && rackTipRadiusFactor < 1.0)) throw new ArgumentException("The rack tip radius factor must be from 0 to below 1 module.", nameof(rackTipRadiusFactor));
            if (!(pressureAngleDeg > 0.0 && pressureAngleDeg < 45.0)) throw new ArgumentException("The pressure angle must be between 0 and 45 degrees.", nameof(pressureAngleDeg));
            if (flankSteps < 2) throw new ArgumentException("A flank needs at least 2 segments.", nameof(flankSteps));

            Profile = profile;
            PressureAngleDeg = pressureAngleDeg;
            _involutePressureAngle = Involute(pressureAngleDeg * Math.PI / 180.0);
            ProfileShift = profile.ProfileShift;
            _rackTipRadius = rackTipRadiusFactor * profile.Module;

            ConeRadius = profile.BackConeDistance;
            RootRadius = (profile.PointD - profile.PointC).Length;
            TipRadius = (profile.PointE - profile.PointC).Length;
            VirtualTeeth = 2.0 * ConeRadius / profile.Module;
            BaseRadius = ConeRadius * Math.Cos(pressureAngleDeg * Math.PI / 180.0);
            _halfPitchAngle = Math.PI / VirtualTeeth;
            PitchAngle = 2.0 * _halfPitchAngle;
            _shiftAngle = ProfileShift * Math.Tan(pressureAngleDeg * Math.PI / 180.0) / ConeRadius;
            _backlash = backlash;
            _backlashHalfAngle = backlash / (2.0 * ConeRadius);
            ComputeFilletExtent();

            var pointedRadius = FindPointedRadius();
            if (pointedRadius <= TipRadius) throw new InvalidOperationException("The teeth come to a point below the tip circle; this gear has too few teeth for the pressure angle.");
            OuterRadius = Math.Min(TipRadius + 0.25 * profile.Module, 0.5 * (TipRadius + pointedRadius));

            if (!(ValleyHalfAngle(FlankStartRadius) > 1e-9)) throw new InvalidOperationException("The teeth are too wide for the pitch: the valley closes at the start of the involute.");
            if (!(FlankStartRadius < TipRadius)) throw new InvalidOperationException("The root fillet reaches the tip circle; the tooth has no involute flank.");

            BuildSketch(flankSteps);
        }

        public BevelBlankProfile Profile { get; }
        public double PressureAngleDeg { get; }

        /// <summary>Profile shift of the virtual spur gear in mm (Gleason long and short addendum); zero for standard proportions.</summary>
        public double ProfileShift { get; }

        /// <summary>Circular backlash (mm) taken off the tooth thickness on the pitch circle of the developed back cone; zero for none.</summary>
        public double Backlash => _backlash;

        /// <summary>Corner radius of the generating rack tip, mm; zero for the sharp corner.</summary>
        public double RackTipRadius => _rackTipRadius;

        /// <summary>Pitch radius of the developed spur gear: the back cone distance from pointC to pointA.</summary>
        public double ConeRadius { get; }

        /// <summary>Tooth count of the developed spur gear (not necessarily a whole number).</summary>
        public double VirtualTeeth { get; }

        public double BaseRadius { get; }

        /// <summary>Radius of the root arc, from pointC to pointD.</summary>
        public double RootRadius { get; }

        /// <summary>Radius of the tooth tips, from pointC to pointE.</summary>
        public double TipRadius { get; }

        /// <summary>Radius where the involute flank starts: where the trochoidal root fillet ends.</summary>
        public double FlankStartRadius { get; private set; }

        /// <summary>Depth of the generating rack tip below the pitch circle: the dedendum on the back cone.</summary>
        public double RackDepth => ConeRadius - RootRadius;

        /// <summary>True when the rack tip corner cuts under the involute (few virtual teeth), so the fillet ends at a crossing with the involute.</summary>
        public bool IsUndercut { get; private set; }

        /// <summary>Radius of the arc that closes the sketch outside the tooth tips.</summary>
        public double OuterRadius { get; }

        /// <summary>Angle between two adjacent teeth on the developed back cone, radians.</summary>
        public double PitchAngle { get; }

        /// <summary>Half the angular width of the root arc, the flat of the rack tip, radians. The trochoidal fillets start at its ends.</summary>
        public double RootHalfAngle { get; private set; }

        private double _filletEnd;

        /// <summary>The root arc of the valley, left to right.</summary>
        public IReadOnlyList<BevelToothPoint> RootArc => _rootArc;

        /// <summary>The trochoidal root fillet on the positive angle side, from the end of the root arc up to the start of the involute.</summary>
        public IReadOnlyList<BevelToothPoint> RightFillet => _rightFillet;

        /// <summary>The trochoidal root fillet on the negative angle side, from the end of the root arc up to the start of the involute.</summary>
        public IReadOnlyList<BevelToothPoint> LeftFillet => _leftFillet;

        /// <summary>The involute flank on the positive angle side, from the start of the involute out to <see cref="OuterRadius"/>.</summary>
        public IReadOnlyList<BevelToothPoint> RightFlank => _rightFlank;

        /// <summary>The involute flank on the negative angle side, from the start of the involute out to <see cref="OuterRadius"/>.</summary>
        public IReadOnlyList<BevelToothPoint> LeftFlank => _leftFlank;

        /// <summary>The arc linking the outer end points of the flanks, from the right flank to the left flank.</summary>
        public IReadOnlyList<BevelToothPoint> OuterArc => _outerArc;

        /// <summary>The closed valley sketch: root arc, right fillet and flank, outer arc, left flank and fillet. The first point is not repeated.</summary>
        public IReadOnlyList<BevelToothPoint> Loop => _loop;

        private static double Involute(double alpha) => Math.Tan(alpha) - alpha;

        private double PressureAngleAt(double radius) => Math.Acos(Math.Min(1.0, BaseRadius / radius));

        /// <summary>Half the angular width of the valley at the given radius, radians. The valley widens towards the tips.</summary>
        public double ValleyHalfAngle(double radius) =>
            _halfPitchAngle / 2.0 - _shiftAngle + _backlashHalfAngle - _involutePressureAngle + Involute(PressureAngleAt(radius));

        /// <summary>Half the angular width of a tooth at the given radius, radians.</summary>
        public double ToothHalfAngle(double radius) => _halfPitchAngle - ValleyHalfAngle(radius);

        /// <summary>
        /// Point of the root fillet as the rack rolls on the pitch circle, with <paramref name="along"/> the distance of the rack tip
        /// corner (the centre of the rounded corner) from the contact point, measured along the pitch line tangent. The contact point is
        /// at the angle <c>(centreU - along) / cone radius</c>. A sharp corner gives the trochoid itself, a rounded one its offset.
        /// </summary>
        private BevelToothPoint FilletPoint(double along)
        {
            // The rack tip corner (or the centre of the rounded corner) is _centreV below the pitch line and _centreU from the valley
            // centre line; the rack has rolled so that it is 'along' from the contact point.
            var rv = ConeRadius - _centreV;
            var rc = Math.Sqrt(rv * rv + along * along);
            var theta = (_centreU - along) / ConeRadius + Math.Atan2(along, rv);
            if (!(_rackTipRadius > 0.0)) return new BevelToothPoint(rc, theta);

            // Rounded tip: offset the path of the arc centre by the tip radius, towards the tooth (the normal is the tangent turned
            // 90 degrees towards increasing angle).
            var tangentRadial = along / rc;
            var tangentAround = rc * (-1.0 / ConeRadius + rv / (rv * rv + along * along));
            var length = Math.Sqrt(tangentRadial * tangentRadial + tangentAround * tangentAround);
            var normalRadial = -tangentAround / length;
            var normalAround = tangentRadial / length;
            var cos = Math.Cos(theta);
            var sin = Math.Sin(theta);
            var x = rc * cos + _rackTipRadius * (normalRadial * cos - normalAround * sin);
            var y = rc * sin + _rackTipRadius * (normalRadial * sin + normalAround * cos);
            return new BevelToothPoint(Math.Sqrt(x * x + y * y), Math.Atan2(y, x));
        }

        /// <summary>
        /// Finds where the trochoid of the rack tip corner ends. The corner at depth <see cref="RackDepth"/> is on the line of action
        /// (and so on the involute) when it is <c>depth / tan(pressure angle)</c> from the pitch point along the tangent, unless that
        /// is beyond the tangent point of the base circle: then the corner undercuts and the fillet stops where it crosses the involute.
        /// </summary>
        private void ComputeFilletExtent()
        {
            var alpha = PressureAngleDeg * Math.PI / 180.0;
            var depth = RackDepth;
            if (!(RootRadius > 0.0 && depth > 0.0)) throw new InvalidOperationException("The root radius and the dedendum must be positive.");

            // Half the rack tooth thickness at its tip, which is a dedendum and a profile shift below the rack reference line; the
            // sharp corner is at +flat and the flat of the rack tip lies between -flat and +flat.
            // The rack tooth is as thick as the valley of the gear, which backlash widens by the backlash.
            var flat = ConeRadius * _halfPitchAngle / 2.0 + _backlash / 2.0 - (ProfileShift + depth) * Math.Tan(alpha);
            if (!(flat > 1e-9)) throw new InvalidOperationException("The rack tip has no flat: the dedendum is too deep for the pressure angle.");

            // A rounded corner of radius r is tangent to the flat and to the flank; its centre is r above the tip.
            var tangentLength = _rackTipRadius * (1.0 - Math.Sin(alpha)) / Math.Cos(alpha);
            _centreU = flat - tangentLength;
            _centreV = depth - _rackTipRadius;
            if (!(_centreU > 1e-9 && _centreV > 1e-9)) throw new InvalidOperationException("The rack tip radius is too large for the rack tip: the rounded corners meet.");
            RootHalfAngle = _centreU / ConeRadius;

            // The flank tangent point of the (rounded) corner is on the line of action when the centre is centreV / tan(alpha) along.
            var onLineOfAction = _centreV / Math.Tan(alpha);
            var tangentDepth = depth - _rackTipRadius * (1.0 - Math.Sin(alpha));
            IsUndercut = tangentDepth / Math.Sin(alpha) > ConeRadius * Math.Sin(alpha) + 1e-12;
            var end = onLineOfAction;
            if (IsUndercut)
            {
                var start = 0.0;
                if (BaseRadius > RootRadius)
                {
                    var low = 0.0;
                    var high = onLineOfAction;
                    for (var k = 0; k < 100; k++)
                    {
                        var mid = 0.5 * (low + high);
                        if (FilletPoint(mid).Radius < BaseRadius) low = mid;
                        else high = mid;
                    }

                    start = 0.5 * (low + high);
                }

                end = onLineOfAction;
                if (Gap(start) <= 0.0)
                {
                    end = start;
                }
                else
                {
                    var found = false;
                    var previous = start;
                    const int scanSteps = 400;
                    for (var i = 1; i <= scanSteps && !found; i++)
                    {
                        var current = start + (onLineOfAction - start) * i / scanSteps;
                        if (Gap(current) <= 0.0)
                        {
                            var low = previous;
                            var high = current;
                            for (var k = 0; k < 100; k++)
                            {
                                var mid = 0.5 * (low + high);
                                if (Gap(mid) > 0.0) low = mid;
                                else high = mid;
                            }

                            end = 0.5 * (low + high);
                            found = true;
                        }

                        previous = current;
                    }

                    if (!found) throw new InvalidOperationException("The root fillet never meets the involute flank.");
                }
            }

            _filletEnd = end;
            FlankStartRadius = FilletPoint(end).Radius;
        }

        /// <summary>Angle of the fillet minus the angle of the involute flank at the same radius: positive while the fillet undercuts the flank.</summary>
        private double Gap(double along)
        {
            var p = FilletPoint(along);
            return p.Angle - ValleyHalfAngle(p.Radius);
        }

        private double FindPointedRadius()
        {
            var low = FlankStartRadius;
            var high = Math.Max(TipRadius, ConeRadius) * 2.0;
            for (var i = 0; ToothHalfAngle(high) > 0.0 && i < 60; i++)
            {
                high *= 2.0;
            }

            for (var i = 0; i < 100; i++)
            {
                var mid = 0.5 * (low + high);
                if (ToothHalfAngle(mid) > 0.0) low = mid;
                else high = mid;
            }

            return 0.5 * (low + high);
        }

        private void BuildSketch(int flankSteps)
        {
            // Involute flanks, from the start of the involute to the tip circle and on to the outer arc.
            var radii = new List<double>();
            for (var i = 0; i <= flankSteps; i++)
            {
                radii.Add(FlankStartRadius + (TipRadius - FlankStartRadius) * i / flankSteps);
            }

            _tipIndex = radii.Count - 1;
            const int extensionSteps = 3;
            for (var i = 1; i <= extensionSteps; i++)
            {
                radii.Add(TipRadius + (OuterRadius - TipRadius) * i / extensionSteps);
            }

            foreach (var r in radii)
            {
                var half = ValleyHalfAngle(r);
                _rightFlank.Add(new BevelToothPoint(r, half));
                _leftFlank.Add(new BevelToothPoint(r, -half));
            }

            for (var i = 0; i <= RootSteps; i++)
            {
                _rootArc.Add(new BevelToothPoint(RootRadius, -RootHalfAngle + 2.0 * RootHalfAngle * i / RootSteps));
            }

            // Trochoid of the rack tip corner, from the end of the root arc to the involute. The end point is the start of the flank.
            for (var i = 0; i <= FilletSteps; i++)
            {
                var point = i == FilletSteps
                    ? new BevelToothPoint(FlankStartRadius, ValleyHalfAngle(FlankStartRadius))
                    : FilletPoint(_filletEnd * i / FilletSteps);
                _rightFillet.Add(point);
                _leftFillet.Add(new BevelToothPoint(point.Radius, -point.Angle));
            }

            var outerHalf = ValleyHalfAngle(OuterRadius);
            for (var i = 0; i <= OuterSteps; i++)
            {
                _outerArc.Add(new BevelToothPoint(OuterRadius, outerHalf - 2.0 * outerHalf * i / OuterSteps));
            }

            foreach (var p in _rootArc) AddPoint(_loop, p);
            foreach (var p in _rightFillet) AddPoint(_loop, p);
            foreach (var p in _rightFlank) AddPoint(_loop, p);
            foreach (var p in _outerArc) AddPoint(_loop, p);
            for (var i = _leftFlank.Count - 1; i >= 0; i--) AddPoint(_loop, _leftFlank[i]);
            for (var i = _leftFillet.Count - 1; i >= 0; i--) AddPoint(_loop, _leftFillet[i]);
            if (_loop.Count > 1 && IsSame(_loop[0], _loop[_loop.Count - 1])) _loop.RemoveAt(_loop.Count - 1);
        }

        private static bool IsSame(BevelToothPoint a, BevelToothPoint b)
        {
            return Math.Abs(a.Radius - b.Radius) < MergeTolerance && Math.Abs(a.Angle - b.Angle) < MergeTolerance;
        }

        private static void AddPoint(List<BevelToothPoint> points, BevelToothPoint p)
        {
            if (points.Count > 0 && IsSame(points[points.Count - 1], p)) return;
            points.Add(p);
        }

        /// <summary>
        /// The outline of the toothed rim on the developed back cone: the blank's tip circle with the sketch valley cut and
        /// patterned about the cone axis. The points run in the direction of increasing angle around the whole circle
        /// (<c>teeth * PitchAngle</c>, the developed full turn) and the last point is not repeated.
        /// </summary>
        /// <param name="valleys">Number of valleys cut, from 1 to the number of teeth. Valley k is centred at <c>k * PitchAngle</c>.</param>
        public IReadOnlyList<BevelToothPoint> BuildOutline(int valleys)
        {
            var teeth = Profile.Teeth;
            if (valleys < 1 || valleys > teeth) throw new ArgumentException("The number of valleys must be between 1 and the number of teeth.", nameof(valleys));

            var outline = new List<BevelToothPoint>();
            var tipHalf = ValleyHalfAngle(TipRadius);
            for (var k = 0; k < valleys; k++)
            {
                var centre = k * PitchAngle;

                // Down the left flank and fillet, across the root and up the right fillet and flank.
                for (var i = _tipIndex; i >= 0; i--)
                {
                    AddPoint(outline, new BevelToothPoint(_leftFlank[i].Radius, centre + _leftFlank[i].Angle));
                }

                for (var i = _leftFillet.Count - 1; i >= 0; i--)
                {
                    AddPoint(outline, new BevelToothPoint(_leftFillet[i].Radius, centre + _leftFillet[i].Angle));
                }

                foreach (var p in _rootArc)
                {
                    AddPoint(outline, new BevelToothPoint(p.Radius, centre + p.Angle));
                }

                foreach (var p in _rightFillet)
                {
                    AddPoint(outline, new BevelToothPoint(p.Radius, centre + p.Angle));
                }

                for (var i = 0; i <= _tipIndex; i++)
                {
                    AddPoint(outline, new BevelToothPoint(_rightFlank[i].Radius, centre + _rightFlank[i].Angle));
                }

                // The tooth land on the tip circle, up to the next valley (or round to the first one).
                var from = centre + tipHalf;
                var to = (k + 1 < valleys ? (k + 1) : teeth) * PitchAngle - tipHalf;
                var steps = Math.Max(Math.Max(1, (int)Math.Ceiling((to - from) / PitchAngle * LandStepsPerPitch - 1e-9)), (int)Math.Ceiling((to - from) / MaxLandStep - 1e-9));
                for (var i = 1; i < steps; i++)
                {
                    AddPoint(outline, new BevelToothPoint(TipRadius, from + (to - from) * i / steps));
                }
            }

            if (outline.Count > 1 && IsSame(outline[0], new BevelToothPoint(outline[outline.Count - 1].Radius, outline[outline.Count - 1].Angle - teeth * PitchAngle)))
            {
                outline.RemoveAt(outline.Count - 1);
            }

            return outline;
        }
    }
}
