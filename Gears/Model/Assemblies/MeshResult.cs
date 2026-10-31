using Bolsover.Gears.Calculator;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// Result of solving how a driven gear meshes with its driver. Lengths are rounded to
    /// <see cref="GearPrecision.DecimalPlaces"/> decimal places; angles keep full precision so that
    /// coordinates computed from them can be rounded accurately.
    /// </summary>
    public sealed class MeshResult
    {
        public MeshResult(double centreDistance, double drivenPhaseRad, double gearRatio, CalculatedGearPairData pairData)
            : this(centreDistance, drivenPhaseRad, gearRatio, pairData, null)
        {
        }

        public MeshResult(double centreDistance, double drivenPhaseRad, double gearRatio, CalculatedGearPairData pairData, Placement3D drivenFrame)
        {
            CentreDistance = centreDistance;
            DrivenPhaseRad = drivenPhaseRad;
            GearRatio = gearRatio;
            PairData = pairData;
            DrivenFrame = drivenFrame;
        }

        /// <summary>Working centre distance between the two shaft axes (mm).</summary>
        public double CentreDistance { get; }

        /// <summary>
        /// Rotation of the driven gear about its own axis that puts its teeth into mesh (radians).
        /// For a rack this is the linear displacement of the rack along its length (mm).
        /// </summary>
        public double DrivenPhaseRad { get; }

        /// <summary>Driven teeth divided by driver teeth.</summary>
        public double GearRatio { get; }

        /// <summary>Full pair calculation (contact ratio, working pressure angle, parameter rows...) when available.</summary>
        public CalculatedGearPairData PairData { get; }

        /// <summary>
        /// Frame of the driven gear (origin on its axis at the gear's mid-plane, Z along its axis) expressed in the
        /// driver gear's frame with the line of centres along the driver's X axis. Null for parallel-axis meshes,
        /// where the driven shaft is simply parallel to the driver shaft at <see cref="CentreDistance"/>.
        /// </summary>
        public Placement3D DrivenFrame { get; }
    }
}
