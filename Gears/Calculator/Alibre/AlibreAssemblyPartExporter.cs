using System;
using System.Collections.Generic;
using System.IO;
using AlibreX;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>
    /// Exports a gear train to a new Alibre assembly by creating the assembly first and every part directly inside it, for when Alibre is
    /// set to save to the Alibre Vault (PDM): a part cannot then be saved to a Windows folder and added to the assembly afterwards.
    /// Each gear, shaft, carrier and belt is made with <c>IADOccurrences.AddEmptyPart</c> at its placement in the train and then built
    /// on the design session of its part occurrence, with the same geometry as <see cref="AlibreGearExporter"/>: this class reuses the
    /// builders of its base class and only overrides where a part and its sketches are created (<see cref="CreatePart"/>,
    /// <see cref="NewSketch"/>) and which owner occurrences are tried for the construction geometry (<see cref="GeometryOccurrenceCandidates"/>).
    /// Nothing is saved or checked in.
    /// </summary>
    public class AlibreAssemblyPartExporter : AlibreGearExporter
    {
        private IADOccurrence _rootOccurrence;
        private IADTransformation _pendingTransform;
        private IADOccurrence _currentOccurrence;
        private IADDesignSession _changesStarted;
        private List<string> _log;

        /// <param name="root">The Alibre root object (<c>IADRoot</c>) that the assembly is created in.</param>
        public AlibreAssemblyPartExporter(IADRoot root) : base(root)
        {
        }

        /// <summary>
        /// While a part is being built: no owner occurrence first (the planes and axes belong to the part's own design session, and the
        /// extrusion, which works, passes none), then the part occurrence. Outside a part build: no owner occurrence only.
        /// </summary>
        protected override IReadOnlyList<IADOccurrence> GeometryOccurrenceCandidates =>
            _currentOccurrence == null ? new IADOccurrence[] { null } : new[] { null, _currentOccurrence };

        /// <summary>Copies the diagnostic line into the export log of the running assembly export.</summary>
        protected override void AddDiagnostic(string message)
        {
            _log?.Add(message);
        }

        /// <summary>
        /// Creates the part as an empty part occurrence of the assembly at the pending placement and opens a change block on its design
        /// session. <see cref="ExportAssemblyInPlace"/> closes the change block once the part is built.
        /// </summary>
        protected override IADPartSession CreatePart(string name)
        {
            if (_rootOccurrence == null || _pendingTransform == null)
            {
                throw new InvalidOperationException("A part can only be created inside an assembly by " + nameof(ExportAssemblyInPlace) + ".");
            }

            ResetGeometryOccurrenceForm();
            _currentOccurrence = _rootOccurrence.Occurrences.AddEmptyPart(name, false, _pendingTransform);
            var session = _currentOccurrence.DesignSession;
            session.StartChanges();
            _changesStarted = session;
            return (IADPartSession)session;
        }

        /// <summary>Adds the sketch to the part occurrence that <see cref="CreatePart"/> made.</summary>
        protected override IADSketch NewSketch(IADPartSession session, IADDesignPlane plane, string name)
        {
            return session.Sketches.AddSketch(_currentOccurrence, plane, name);
        }

        /// <summary>Always exports with <see cref="ExportAssemblyInPlace"/>, whatever the Alibre save option is.</summary>
        public override IADAssemblySession ExportAssembly(GearAssembly train, string assemblyName = DefaultAssemblyName, IReadOnlyCollection<object> selection = null)
        {
            return ExportAssemblyInPlace(train, assemblyName, selection);
        }

        /// <summary>
        /// Exports the train to a new Alibre assembly, creating every part inside it (see the class remarks): every gear, shaft, carrier and
        /// timing belt, or only the items of <paramref name="selection"/> when one is given. Throws <see cref="InvalidOperationException"/>
        /// when there are fewer than <see cref="AlibreGearExporter.MinimumAssemblyParts"/> parts, a gear family cannot be exported yet, or
        /// Alibre rejects a step. The message names the stage that failed and the log file (<see cref="AlibreGearExporter.LastExportLog"/>).
        /// </summary>
        /// <param name="selection">The gears, shafts and belts to export; null exports everything.</param>
        public IADAssemblySession ExportAssemblyInPlace(GearAssembly train, string assemblyName = DefaultAssemblyName, IReadOnlyCollection<object> selection = null)
        {
            var parts = GetAssemblyParts(train, selection);
            var shaftParts = GetShaftAssemblyParts(train, selection);
            var beltParts = GetBeltAssemblyParts(train, selection);
            var chainParts = GetChainAssemblyParts(train, selection);
            CheckPartCount(parts.Count + shaftParts.Count + beltParts.Count + chainParts.Count);

            var stage = "creating the assembly";
            var logFolder = string.IsNullOrWhiteSpace(AssemblyFolder)
                ? Path.Combine(Path.GetTempPath(), "GearsAlibre", DateTime.Now.ToString("yyyyMMdd_HHmmss"))
                : AssemblyFolder;
            var log = new List<string>();
            if (SaveModeLines != null && SaveModeLines.Count > 0)
            {
                log.AddRange(SaveModeLines);
            }
            else
            {
                log.Add("save mode: PDM (assembly export in place requested directly)");
            }

            var modeLabel = log[0];
            _log = log;
            LastExportLog = null;
            IADAssemblySession assembly;
            try
            {
                assembly = CreateAssembly(assemblyName, log);
                log.Add("created the assembly (parts are created inside it)");
                stage = "opening the root occurrence";
                _rootOccurrence = assembly.RootOccurrence;
                stage = "reading the geometry factory";
                var factory = assembly.GeometryFactory;

                foreach (var part in parts)
                {
                    var current = part.Key.Id;
                    stage = "building part '" + current + "'";
                    BuildInPlace(assembly, factory, part.Value, () => ExportComponent(part.Key), log);
                    log.Add("built part '" + current + "' in the assembly");
                }

                foreach (var shaftPart in shaftParts)
                {
                    var current = shaftPart.Key.Id;
                    var what = (PlanetaryGearSet.IsCarrier(shaftPart.Key) ? "carrier '" : "shaft '") + current + "'";
                    stage = "building " + what;
                    BuildInPlace(assembly, factory, shaftPart.Value, () => Export(shaftPart.Key), log);
                    log.Add("built " + what + " in the assembly");
                }

                foreach (var beltPart in beltParts)
                {
                    var current = beltPart.Key.Id;
                    stage = "building belt '" + current + "'";
                    var beltDriverMount = train.FindMount(beltPart.Key.Driver, out _);
                    var phase = beltDriverMount == null ? 0.0 : beltDriverMount.PhaseRad;
                    BuildInPlace(assembly, factory, beltPart.Value, () => Export(beltPart.Key, phase), log);
                    log.Add("built belt '" + current + "' in the assembly");
                }

                foreach (var chainPart in chainParts)
                {
                    var current = chainPart.Key.Id;
                    stage = "building chain '" + current + "'";
                    var chainDriverMount = train.FindMount(chainPart.Key.Driver, out _);
                    var chainPhase = chainDriverMount == null ? 0.0 : chainDriverMount.PhaseRad;
                    BuildInPlace(assembly, factory, chainPart.Value, () => Export(chainPart.Key, chainPhase), log);
                    log.Add("built chain '" + current + "' in the assembly");
                }

                stage = "regenerating the assembly";
                assembly.RegenerateDesign(true);
                log.Add("regenerated the assembly");
                WriteLog(logFolder, log);
                return assembly;
            }
            catch (Exception ex)
            {
                log.Add("FAILED while " + stage + ": " + ex);
                var logPath = WriteLog(logFolder, log);
                throw new InvalidOperationException(
                    $"Alibre export of the gear train to an assembly failed while {stage} [{modeLabel}]: {ex.Message}"
                    + (logPath == null ? "" : " (details: " + logPath + ")"), ex);
            }
            finally
            {
                _rootOccurrence = null;
                _pendingTransform = null;
                _currentOccurrence = null;
                _log = null;
            }
        }

        /// <summary>
        /// Builds one part inside the assembly: makes the placement the pending transform of <see cref="CreatePart"/>, runs the builder, and
        /// always closes the change block of the part and makes the root occurrence the active one again, ready for the next part.
        /// A failure of that clean-up is only logged when the builder failed already, so that it never hides the builder's own error.
        /// </summary>
        private void BuildInPlace(IADAssemblySession assembly, IADGeometryFactory factory, Transform3D placement, Action build, List<string> log)
        {
            Array matrix = ToAlibreMatrix(placement);
            _pendingTransform = factory.CreateTransform(ref matrix);
            _changesStarted = null;
            var buildFailed = false;
            try
            {
                // AddEmptyPart requires the parent occurrence to be the active occurrence.
                assembly.ActiveOccurrence = _rootOccurrence;
                build();
            }
            catch (Exception)
            {
                buildFailed = true;
                throw;
            }
            finally
            {
                try
                {
                    _changesStarted?.StopChanges();
                }
                catch (Exception ex) when (buildFailed)
                {
                    log.Add("ending the changes of the part failed after the build failed: " + ex.Message);
                }
                finally
                {
                    _changesStarted = null;
                    _currentOccurrence = null;
                    try
                    {
                        assembly.ActiveOccurrence = _rootOccurrence;
                    }
                    catch (Exception ex)
                    {
                        log.Add("setting the active occurrence failed: " + ex.Message);
                    }
                }
            }
        }
    }
}
