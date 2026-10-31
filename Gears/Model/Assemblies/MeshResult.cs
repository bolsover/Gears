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
        {
            CentreDistance = centreDistance;
            DrivenPhaseRad = drivenPhaseRad;
            GearRatio = gearRatio;
            PairData = pairData;
        }

        /// <summary>Working centre distance between the two shaft axes (mm).</summary>
        public double CentreDistance { get; }

        /// <summary>Rotation of the driven gear about its own axis that puts its teeth into mesh (radians).</summary>
        public double DrivenPhaseRad { get; }

        /// <summary>Driven teeth divided by driver teeth.</summary>
        public double GearRatio { get; }

        /// <summary>Full pair calculation (contact ratio, working pressure angle, parameter rows...) when available.</summary>
        public CalculatedGearPairData PairData { get; }
    }
}
