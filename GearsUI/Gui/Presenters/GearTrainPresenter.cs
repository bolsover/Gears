using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>The kind of item a <see cref="TrainTreeNode"/> stands for.</summary>
    public enum TrainNodeKind
    {
        Assembly,
        Shaft,
        Gear
    }

    /// <summary>One node of the assembly tree: assembly, then shafts, then the gears mounted on each shaft.</summary>
    public sealed class TrainTreeNode
    {
        public TrainTreeNode(string text, TrainNodeKind kind, object tag)
        {
            Text = text;
            Kind = kind;
            Tag = tag;
        }

        public string Text { get; }
        public TrainNodeKind Kind { get; }

        /// <summary>The <see cref="GearAssembly"/>, <see cref="Shaft"/> or <see cref="GearComponent"/> behind the node.</summary>
        public object Tag { get; }

        /// <summary>For gear nodes: false when the gear is hidden in the 3D scene.</summary>
        public bool Visible { get; set; } = true;

        public List<TrainTreeNode> Children { get; } = new List<TrainTreeNode>();
    }

    /// <summary>
    /// Presenter of the gear train editor. It owns a <see cref="GearAssembly"/> and builds the shaft/gear tree,
    /// the 3D scene and the ratio and speed report. Several gears can sit on one shaft (compound gears), and gears
    /// can mesh in chains (idlers) or branch in different directions.
    /// </summary>
    public class GearTrainPresenter
    {
        private const double AxialGap = 2.0;

        private readonly IGearTrainView _view;
        private readonly HashSet<GearComponent> _hidden = new HashSet<GearComponent>();
        private int _gearCounter;
        private int _shaftCounter;

        public GearTrainPresenter(IGearTrainView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            Assembly = new GearAssembly();
        }

        public GearAssembly Assembly { get; private set; }

        /// <summary>The selected <see cref="Shaft"/> or <see cref="GearComponent"/>; null when nothing is selected.</summary>
        public object Selected { get; private set; }

        /// <summary>The most recent successfully built scene; null before the first refresh.</summary>
        public AssemblyScene Scene { get; private set; }

        /// <summary>The most recent successfully built report; null before the first refresh.</summary>
        public GearTrainReport Report { get; private set; }

        public ISet<GearComponent> HiddenGears => _hidden;

        /// <summary>The shaft that is turned from outside: the first shaft of the assembly.</summary>
        public Shaft InputShaft => Assembly.Shafts.Count > 0 ? Assembly.Shafts[0] : null;

        /// <summary>Starts a new train with one shaft carrying one 20 tooth gear (module 2 mm, 20 degree pressure angle).</summary>
        public void NewTrain()
        {
            Assembly = new GearAssembly();
            _hidden.Clear();
            _gearCounter = 0;
            _shaftCounter = 0;
            Scene = null;
            Report = null;

            var shaft = CreateShaft();
            var gear = CreateGear(null, 20);
            shaft.Mount(gear, 0.0);
            Assembly.AddShaft(shaft);

            Selected = gear;
            Refresh();
        }

        /// <summary>
        /// Adds another gear to a shaft next to the existing gears (a compound gear). The new gear copies the
        /// parameters of the last gear on the shaft and sits beside it with a small axial gap.
        /// </summary>
        public InvoluteGearComponent AddGearToShaft(Shaft shaft, int teeth = 20)
        {
            if (shaft == null) throw new ArgumentNullException(nameof(shaft));
            if (!Assembly.Shafts.Contains(shaft)) throw new ArgumentException("The shaft is not part of the assembly.", nameof(shaft));

            var template = shaft.Mounts.Count > 0 ? shaft.Mounts[shaft.Mounts.Count - 1].Component as InvoluteGearComponent : null;
            var gear = CreateGear(template, teeth);

            var offset = 0.0;
            if (shaft.Mounts.Count > 0)
            {
                var end = double.MinValue;
                foreach (var mount in shaft.Mounts)
                {
                    mount.GetAxialExtent(out _, out var e);
                    if (e > end) end = e;
                }

                offset = end + AxialGap + gear.FaceWidth / 2.0;
            }

            shaft.Mount(gear, GearPrecision.Round(offset));
            Selected = gear;
            Refresh();
            return gear;
        }

        /// <summary>
        /// Adds a new shaft with a gear that meshes with <paramref name="driver"/>. The gear takes over module,
        /// pressure angle and face width, uses the opposite helix hand for an external pair, and sits in the same plane as the driver.
        /// A gear that is both driven and a driver is an idler; several gears driven by one driver form a branching train.
        /// </summary>
        /// <param name="driver">The gear to mesh with; it must be an involute gear mounted in the assembly.</param>
        /// <param name="teeth">Number of teeth; zero or less chooses the driver's teeth plus ten.</param>
        /// <param name="centreLineAngleDeg">Direction of the new shaft from the driver shaft; null spaces branches 60 degrees apart.</param>
        public InvoluteGearComponent AddMeshedGear(GearComponent driver, int teeth = 0, double? centreLineAngleDeg = null)
        {
            var driverGear = driver as InvoluteGearComponent
                             ?? throw new ArgumentException("Only involute gears can be meshed.", nameof(driver));
            var driverMount = Assembly.FindMount(driverGear, out var driverShaft)
                              ?? throw new ArgumentException("The driver gear is not mounted in the assembly.", nameof(driver));

            var gear = CreateGear(driverGear, teeth > 0 ? teeth : Math.Max(12, driverGear.Teeth + 10));
            gear.IsInternal = false;
            if (driverGear.IsHelical && !driverGear.IsInternal)
            {
                gear.HelicalDirection = driverGear.HelicalDirection == "R" ? "L" : "R";
            }

            var shaft = CreateShaft();
            shaft.Mount(gear, driverMount.AxialOffset);

            var branches = Assembly.Relations.Count(r => driverShaft.Mounts.Any(m => ReferenceEquals(m.Component, r.Driver)));
            var angle = centreLineAngleDeg ?? branches * 60.0;

            Assembly.AddShaft(shaft);
            try
            {
                Assembly.AddRelation(driverGear, gear, MeshType.Parallel, angle);
            }
            catch
            {
                Assembly.RemoveShaft(shaft);
                throw;
            }

            Selected = gear;
            Refresh();
            return gear;
        }

        /// <summary>
        /// Removes the selected gear or shaft together with everything that hangs off it: a gear that drives other
        /// gears takes those branches with it, and a driven gear takes its whole shaft and the shaft's branches.
        /// </summary>
        public void RemoveSelected()
        {
            if (Selected is Shaft shaft)
            {
                RemoveShaftBranch(shaft, new HashSet<Shaft>());
            }
            else if (Selected is GearComponent gear)
            {
                RemoveGearBranch(gear);
            }
            else
            {
                return;
            }

            Selected = null;
            Refresh();
        }

        /// <summary>Selects a shaft or gear (or nothing) and updates the selection highlight and the property editor.</summary>
        public void Select(object tag)
        {
            Selected = tag is Shaft || tag is GearComponent ? tag : null;
            _view.ShowSelection(Selected);
            _view.ShowProperties(CreateProperties(Selected));
            if (Scene != null) _view.ShowScene(Scene, Selected, _hidden);
        }

        public void SetGearVisible(GearComponent gear, bool visible)
        {
            SetGearsVisible(new[] { gear }, visible);
        }

        public void SetGearsVisible(IEnumerable<GearComponent> gears, bool visible)
        {
            foreach (var gear in gears)
            {
                if (visible) _hidden.Remove(gear);
                else _hidden.Add(gear);
            }

            if (Scene != null) _view.ShowScene(Scene, Selected, _hidden);
        }

        /// <summary>Turns the input shaft to the given angle (degrees) and rebuilds the scene.</summary>
        public void SetInputAngle(double degrees)
        {
            var input = InputShaft;
            if (input == null) return;
            input.RotationRad = degrees * Math.PI / 180.0;
            Refresh();
        }

        /// <summary>
        /// Solves the train and updates the tree, property editor, 3D scene and report. A problem (for example differing
        /// modules or a locked-up train) is shown as a message and the previous scene and report stay on screen.
        /// </summary>
        public void Refresh()
        {
            string error = null;
            if (Assembly.Shafts.Count > 0)
            {
                try
                {
                    var scene = GearAssemblySceneBuilder.Build(Assembly);
                    var report = GearTrainAnalyzer.Analyze(Assembly, null, _view.InputSpeedRpm);
                    Scene = scene;
                    Report = report;
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is NotSupportedException)
                {
                    error = ex.Message;
                }
            }
            else
            {
                Scene = new AssemblyScene();
                Report = null;
            }

            _view.ShowTree(BuildTree(), Selected);
            _view.ShowProperties(CreateProperties(Selected));
            if (error == null)
            {
                _view.ShowScene(Scene, Selected, _hidden);
                _view.ShowReport(Report);
                if (Report != null && Report.Warnings.Count > 0)
                {
                    _view.ShowMessage("Warning: " + string.Join(" ", Report.Warnings), false);
                }
                else
                {
                    _view.ShowMessage("Gear train solved.", false);
                }
            }
            else
            {
                _view.ShowMessage(error, true);
            }
        }

        /// <summary>
        /// Gives <paramref name="source"/>'s module and pressure angle to every gear that meshes with it, directly or
        /// through a chain of meshes, because meshing gears must share both.
        /// </summary>
        internal void ApplyMeshCommon(InvoluteGearComponent source)
        {
            var visited = new HashSet<GearComponent> { source };
            var queue = new Queue<GearComponent>();
            queue.Enqueue(source);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var relation in Assembly.Relations)
                {
                    GearComponent other = null;
                    if (ReferenceEquals(relation.Driver, current)) other = relation.Driven;
                    else if (ReferenceEquals(relation.Driven, current)) other = relation.Driver;
                    if (other == null || !visited.Add(other)) continue;

                    other.Module = source.Module;
                    other.PressureAngleDeg = source.PressureAngleDeg;
                    queue.Enqueue(other);
                }
            }
        }

        internal bool IsInputShaft(Shaft shaft) => ReferenceEquals(shaft, InputShaft);

        private InvoluteGearComponent CreateGear(InvoluteGearComponent template, int teeth)
        {
            var gear = new InvoluteGearComponent("Gear " + (++_gearCounter)) { Teeth = Math.Max(1, teeth) };
            if (template != null)
            {
                gear.Module = template.Module;
                gear.PressureAngleDeg = template.PressureAngleDeg;
                gear.FaceWidth = template.FaceWidth;
                gear.HelixAngleDeg = template.HelixAngleDeg;
                gear.HelicalDirection = template.HelicalDirection;
                gear.Clearance = template.Clearance;
                gear.Backlash = template.Backlash;
                gear.ShowFullGear = template.ShowFullGear;
            }

            return gear;
        }

        private Shaft CreateShaft()
        {
            return new Shaft("Shaft " + (++_shaftCounter));
        }

        private void RemoveShaftBranch(Shaft shaft, ISet<Shaft> visited)
        {
            if (!visited.Add(shaft)) return;

            foreach (var mount in shaft.Mounts.ToList())
            {
                foreach (var relation in Assembly.Relations.Where(r => ReferenceEquals(r.Driver, mount.Component)).ToList())
                {
                    if (Assembly.FindMount(relation.Driven, out var drivenShaft) != null)
                    {
                        RemoveShaftBranch(drivenShaft, visited);
                    }
                }
            }

            foreach (var mount in shaft.Mounts.ToList())
            {
                _hidden.Remove(mount.Component);
            }

            Assembly.RemoveShaft(shaft);
        }

        private void RemoveGearBranch(GearComponent gear)
        {
            if (Assembly.FindMount(gear, out var shaft) == null) return;

            // A driven gear is what connects its shaft to the rest of the train, so its whole shaft goes.
            if (Assembly.FindDrivingRelation(gear) != null)
            {
                RemoveShaftBranch(shaft, new HashSet<Shaft>());
                return;
            }

            var visited = new HashSet<Shaft> { shaft };
            foreach (var relation in Assembly.Relations.Where(r => ReferenceEquals(r.Driver, gear)).ToList())
            {
                if (Assembly.FindMount(relation.Driven, out var drivenShaft) != null)
                {
                    RemoveShaftBranch(drivenShaft, visited);
                }
            }

            _hidden.Remove(gear);
            Assembly.RemoveGear(gear);
        }

        private object CreateProperties(object selected)
        {
            if (selected is InvoluteGearComponent gear)
            {
                var mount = Assembly.FindMount(gear, out _);
                if (mount == null) return null;
                return new GearNodeProperties(this, gear, mount, Assembly.FindDrivingRelation(gear));
            }

            if (selected is Shaft shaft && Assembly.Shafts.Contains(shaft))
            {
                return new ShaftNodeProperties(this, shaft);
            }

            return null;
        }

        private TrainTreeNode BuildTree()
        {
            var ci = CultureInfo.InvariantCulture;
            var root = new TrainTreeNode(
                $"Gear train ({Assembly.Shafts.Count} shafts, {Assembly.Components.Count()} gears)",
                TrainNodeKind.Assembly,
                Assembly);

            for (var i = 0; i < Assembly.Shafts.Count; i++)
            {
                var shaft = Assembly.Shafts[i];
                var shaftNode = new TrainTreeNode(i == 0 ? shaft.Id + " (input)" : shaft.Id, TrainNodeKind.Shaft, shaft);
                foreach (var mount in shaft.Mounts)
                {
                    var gear = mount.Component;
                    var relation = Assembly.FindDrivingRelation(gear);
                    var text = $"{gear.Name}  z={gear.ToothCount}  @ {mount.AxialOffset.ToString("0.###", ci)} mm";
                    if (relation != null) text += $"  (driven by {relation.Driver.Name})";
                    shaftNode.Children.Add(new TrainTreeNode(text, TrainNodeKind.Gear, gear) { Visible = !_hidden.Contains(gear) });
                }

                root.Children.Add(shaftNode);
            }

            return root;
        }
    }
}
