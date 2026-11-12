using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>The Alibre export commands of the gear train editor: they ask for the parts to export, run the export and report the outcome.</summary>
    internal sealed class AlibreExportController
    {
        private readonly IWin32Window _owner;
        private readonly GearTrainPresenter _presenter;
        private readonly Func<AlibreGearExporter> _getExporter;
        private readonly Func<IReadOnlyCollection<object>> _selectedItems;
        private readonly Action<string, bool> _showMessage;

        public AlibreExportController(IWin32Window owner, GearTrainPresenter presenter, Func<AlibreGearExporter> getExporter,
            Func<IReadOnlyCollection<object>> selectedItems, Action<string, bool> showMessage)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _getExporter = getExporter ?? throw new ArgumentNullException(nameof(getExporter));
            _selectedItems = selectedItems ?? throw new ArgumentNullException(nameof(selectedItems));
            _showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
        }

        private AlibreGearExporter Exporter => _getExporter();

        private void ShowMessage(string message, bool isError) => _showMessage(message, isError);

        /// <summary>
        /// Exports the selected parts: one part directly, several as separate Alibre parts or as one assembly, as the user chooses.
        /// </summary>
        internal void ExportSelectedToAlibre()
        {
            if (Exporter == null)
            {
                ShowMessage("Export to Alibre is only available when the editor is opened from the Alibre Design add-on.", true);
                return;
            }

            var items = _selectedItems().ToList();
            if (items.Count == 0)
            {
                ShowMessage("Select one or more parts first.", true);
                return;
            }

            if (items.Count == 1)
            {
                ExportItemToAlibre(items[0]);
                return;
            }

            switch (ExportChoiceDialog.Ask(_owner, items.Count))
            {
                case ExportChoice.SeparateParts:
                    ExportSeparatePartsToAlibre(items);
                    break;
                case ExportChoice.Assembly:
                    ExportAssemblyToAlibre(items);
                    break;
            }
        }

        /// <summary>Exports every gear, shaft, carrier and belt of the train to one new Alibre assembly.</summary>
        internal void ExportAllToAlibre()
        {
            if (Exporter == null)
            {
                ShowMessage("Export to Alibre is only available when the editor is opened from the Alibre Design add-on.", true);
                return;
            }

            ExportAssemblyToAlibre(null);
        }

        /// <summary>Exports each item as its own new Alibre part; a failure is reported and the rest continue.</summary>
        private void ExportSeparatePartsToAlibre(IReadOnlyCollection<object> items)
        {
            var failures = new List<string>();
            var exported = 0;
            foreach (var item in items)
            {
                try
                {
                    ExportItemToAlibre(item);
                    exported++;
                }
                catch (Exception ex)
                {
                    failures.Add(DescribeItem(item) + ": " + ex.Message);
                }
            }

            if (failures.Count == 0)
            {
                ShowMessage($"Exported {exported} parts to new Alibre parts.", false);
                return;
            }

            var report = $"Exported {exported} of {items.Count} parts. Not exported:\n" + string.Join("\n", failures);
            ShowMessage(report.Replace("\n", " "), true);
            MessageBox.Show(_owner, report, "Export selected to Alibre", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static string DescribeItem(object item)
        {
            switch (item)
            {
                case GearComponent gear: return gear.Id;
                case Shaft shaft: return shaft.Id;
                case TimingBelt belt: return belt.Id;
                case RollerChain chain: return chain.Id;
                default: return "item";
            }
        }

        /// <summary>Exports the selection (null: the whole train) to one new Alibre assembly.</summary>
        private void ExportAssemblyToAlibre(IReadOnlyCollection<object> selection)
        {
            var count = AlibreGearExporter.GetAssemblyParts(_presenter.Assembly, selection).Count;
            var extraParts = AlibreGearExporter.GetShaftAssemblyParts(_presenter.Assembly, selection).Count;
            var beltCount = AlibreGearExporter.GetBeltAssemblyParts(_presenter.Assembly, selection).Count;
            var chainCount = AlibreGearExporter.GetChainAssemblyParts(_presenter.Assembly, selection).Count;
            if (count + extraParts + beltCount + chainCount < AlibreGearExporter.MinimumAssemblyParts)
            {
                ShowMessage($"Select at least {AlibreGearExporter.MinimumAssemblyParts} exportable parts to export an assembly to Alibre.", true);
                return;
            }

            var description = $"{count} gears";
            if (extraParts > 0) description += $" and {extraParts} shaft or carrier part(s)";
            if (beltCount > 0) description += $" and {beltCount} belt(s)";
            if (chainCount > 0) description += $" and {chainCount} chain(s)";

            ShowMessage($"Exporting {description} to a new Alibre assembly…", false);
            try
            {
                Exporter.ExportAssembly(_presenter.Assembly, AlibreGearExporter.DefaultAssemblyName, selection);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
                MessageBox.Show(_owner, ex.Message, "Export Assembly to Alibre", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ShowMessage($"Exported {description} to a new Alibre assembly, each as a new part. Log: {Exporter.LastExportLog}", false);
        }

        private void ExportShaftToAlibre(Shaft shaft)
        {
            var isCarrier = PlanetaryGearSet.IsCarrier(shaft);
            var kind = isCarrier ? "carrier" : "shaft";
            ShowMessage($"Exporting {shaft.Id} to Alibre…", false);
            Exporter.Export(shaft);
            shaft.GetAxialSpan(out var start, out var end);
            ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} ({1}, diameter {2:0.###} mm, length {3:0.###} mm) to a new Alibre part.", shaft.Id, kind, shaft.Diameter, end - start), false);
        }

        /// <summary>Exports one gear, shaft, carrier or belt as a new Alibre part; throws when the item cannot be exported.</summary>
        private void ExportItemToAlibre(object item)
        {
            switch (item)
            {
                case InvoluteGearComponent gear:
                {
                    ShowMessage($"Exporting {gear.Id} to Alibre…", false);
                    Exporter.Export(gear);
                    var kind = (gear.IsInternal ? "internal " : "external ") + (gear.IsHelical ? "helical" : "spur");
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} ({1} gear, {2} teeth, module {3:0.###}) to a new Alibre part.", gear.Id, kind, gear.Teeth, gear.Module), false);
                    break;
                }
                case RackComponent rack:
                {
                    ShowMessage($"Exporting {rack.Id} to Alibre…", false);
                    Exporter.Export(rack);
                    var kind = rack.IsHelical ? string.Format(CultureInfo.InvariantCulture, "helical, {0:0.###} degrees {1}", rack.HelixAngleDeg, rack.HelicalDirection) : "straight";
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} (rack, {1}, {2} teeth, module {3:0.###}) to a new Alibre part.", rack.Id, kind, rack.Teeth, rack.Module), false);
                    break;
                }
                case WormComponent worm:
                {
                    ShowMessage($"Exporting {worm.Id} to Alibre…", false);
                    Exporter.Export(worm);
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} (worm, {1} start(s), axial module {2:0.###}, {3} hand) to a new Alibre part.", worm.Id, worm.Starts, worm.Module, worm.HelicalDirection == "R" ? "right" : "left"), false);
                    break;
                }
                case WormWheelComponent wheel:
                {
                    ShowMessage($"Exporting {wheel.Id} to Alibre…", false);
                    Exporter.Export(wheel);
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} (worm wheel, {1} teeth, axial module {2:0.###}, helix angle {3:0.###} degrees) to a new Alibre part.", wheel.Id, wheel.Teeth, wheel.Module, wheel.LeadAngleDeg), false);
                    break;
                }
                case HtdPulleyComponent pulley:
                {
                    ShowMessage($"Exporting {pulley.Id} to Alibre…", false);
                    Exporter.Export(pulley);
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} (HTD {1} pulley, {2} grooves, {3:0.###} mm pitch diameter) to a new Alibre part.", pulley.Id, pulley.Profile.Name, pulley.Teeth, pulley.PitchDiameter), false);
                    break;
                }
                case SprocketComponent sprocket:
                {
                    ShowMessage($"Exporting {sprocket.Id} to Alibre…", false);
                    Exporter.Export(sprocket);
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} (ISO 606 {1} sprocket, {2} teeth, {3:0.###} mm pitch diameter) to a new Alibre part.", sprocket.Id, sprocket.Profile.Name, sprocket.Teeth, sprocket.PitchDiameter), false);
                    break;
                }
                case CycloidalGearComponent cycloidal:
                {
                    ShowMessage($"Exporting {cycloidal.Id} to Alibre…", false);
                    Exporter.Export(cycloidal);
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} (cycloidal {1}, {2} teeth, module {3:0.###}) to a new Alibre part.", cycloidal.Id, cycloidal.IsPinion ? "pinion" : "wheel", cycloidal.Teeth, cycloidal.Module), false);
                    break;
                }
                case BevelGearComponent bevel:
                {
                    ShowMessage($"Exporting {bevel.Id} to Alibre…", false);
                    Exporter.Export(bevel);
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} ({1}, {2} teeth, module {3:0.###}, {4} system) to a new Alibre part.", bevel.Id, bevel.TypeName.ToLowerInvariant(), bevel.Teeth, bevel.Module, bevel.BevelType), false);
                    break;
                }
                case TimingBelt belt:
                {
                    ShowMessage($"Exporting {belt.Id} to Alibre...", false);
                    Exporter.Export(belt);
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} (HTD {1} belt without teeth, {2:0.###} mm wide, {3} teeth long) to a new Alibre part.", belt.Id, belt.Profile.Name, belt.Width, belt.BeltTeeth), false);
                    break;
                }
                case RollerChain chain:
                {
                    ShowMessage($"Exporting {chain.Id} to Alibre...", false);
                    var driverMount = _presenter.Assembly.FindMount(chain.Driver, out _);
                    Exporter.Export(chain, driverMount == null ? 0.0 : driverMount.PhaseRad);
                    ShowMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} (ISO 606 {1} chain, {2} links, {3:0.###} mm wide) to a new Alibre part.", chain.Id, chain.Profile.Name, chain.LinkCount, chain.Width), false);
                    break;
                }
                case Shaft shaft:
                    ExportShaftToAlibre(shaft);
                    break;
                default:
                    throw new InvalidOperationException("Select a gear, rack, worm, worm wheel, bevel gear, cycloidal gear, HTD pulley, sprocket, timing belt, roller chain, shaft or planetary carrier to export to Alibre.");
            }
        }
    }
}
