using System;
using System.Linq;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Turns the input shaft of an assembly over time so that a viewer can show the train running.
    /// The animator only changes <see cref="Shaft.RotationRad"/> of the input shaft; every other shaft, gear and rack
    /// follows from the mesh relations when the scene is rebuilt.
    /// </summary>
    public sealed class GearTrainAnimator
    {
        private readonly GearAssembly _assembly;

        public GearTrainAnimator(GearAssembly assembly, Shaft inputShaft = null)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            if (assembly.Shafts.Count == 0) throw new InvalidOperationException("The assembly has no shafts.");

            InputShaft = inputShaft ?? assembly.Shafts[0];
            if (!assembly.Shafts.Contains(InputShaft))
            {
                throw new ArgumentException("The input shaft is not part of the assembly.", nameof(inputShaft));
            }
        }

        public Shaft InputShaft { get; }

        /// <summary>Speed of the input shaft in rpm (negative runs backwards).</summary>
        public double SpeedRpm { get; set; } = 30.0;

        /// <summary>Current input angle (radians), kept within one turn.</summary>
        public double AngleRad => InputShaft.RotationRad;

        /// <summary>Advances the input angle by <paramref name="seconds"/> at <see cref="SpeedRpm"/>.</summary>
        public void Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds))
            {
                throw new ArgumentException("The time step must be a finite number.", nameof(seconds));
            }

            var angle = InputShaft.RotationRad + SpeedRpm * 2.0 * Math.PI / 60.0 * seconds;
            var turn = 2.0 * Math.PI;
            angle -= turn * Math.Floor(angle / turn);
            InputShaft.RotationRad = angle;
        }

        /// <summary>Advances the input angle and returns the rebuilt scene.</summary>
        public AssemblyScene Step(double seconds)
        {
            Advance(seconds);
            return GearAssemblySceneBuilder.Build(_assembly);
        }
    }
}
