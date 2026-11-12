using System;
using System.Collections.Generic;
using System.IO;
using AlibreX;
using Bolsover.Gears.Model.Assemblies;
using static Bolsover.Gears.Calculator.Alibre.AlibreGearExporter;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>Builds every part of a gear train, saves it and adds it to a new Alibre assembly, and writes the export log.</summary>
    internal sealed class AlibreAssemblyBuilder
    {
        private readonly AlibreGearExporter _owner;
        private readonly IADRoot _root;

        public AlibreAssemblyBuilder(AlibreGearExporter owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _root = owner.Root;
        }

        private string AssemblyFolder => _owner.AssemblyFolder;

        private string LastExportLog
        {
            get => _owner.LastExportLog;
            set => _owner.LastExportLog = value;
        }

        private IADPartSession ExportComponent(GearComponent component) => _owner.ExportComponent(component);

        private IADPartSession Export(Shaft shaft) => _owner.Export(shaft);

        private IADPartSession Export(TimingBelt belt, double driverPhaseRad) => _owner.Export(belt, driverPhaseRad);

        private IADPartSession Export(RollerChain chain, double driverPhaseRad) => _owner.Export(chain, driverPhaseRad);

        internal IADAssemblySession ExportAssembly(GearAssembly train, string assemblyName = DefaultAssemblyName, IReadOnlyCollection<object> selection = null)
        {
            var parts = GetAssemblyParts(train, selection);
            var shaftParts = GetShaftAssemblyParts(train, selection);
            var beltParts = GetBeltAssemblyParts(train, selection);
            var chainParts = GetChainAssemblyParts(train, selection);
            CheckPartCount(parts.Count + shaftParts.Count + beltParts.Count + chainParts.Count);

            var stage = "creating the assembly";
            var folder = string.IsNullOrWhiteSpace(AssemblyFolder)
                ? Path.Combine(Path.GetTempPath(), "GearsAlibre", DateTime.Now.ToString("yyyyMMdd_HHmmss"))
                : AssemblyFolder;
            var log = new List<string>();
            LastExportLog = null;
            try
            {
                stage = "checking the Alibre save location";
                CheckSaveTarget(log);

                stage = "creating the assembly";
                Directory.CreateDirectory(folder);

                // Every part is built and saved before the assembly is created: creating a part opens it as the active window, and
                // IADOccurrences.Add only works while the assembly (its root occurrence) is the active session.
                var saved = new List<IADPartSession>();
                var index = 0;
                foreach (var part in parts)
                {
                    var current = part.Key.Id;
                    index++;

                    stage = "building part '" + current + "'";
                    var session = ExportComponent(part.Key);
                    log.Add("built part '" + current + "'");
                    saved.Add(session);
                    SavePart(session, current, folder, index, log);
                }

                foreach (var shaftPart in shaftParts)
                {
                    var current = shaftPart.Key.Id;
                    index++;

                    stage = "building " + (PlanetaryGearSet.IsCarrier(shaftPart.Key) ? "carrier '" : "shaft '") + current + "'";
                    var session = Export(shaftPart.Key);
                    log.Add("built " + stage.Substring("building ".Length));
                    saved.Add(session);
                    SavePart(session, current, folder, index, log);
                }

                foreach (var beltPart in beltParts)
                {
                    var current = beltPart.Key.Id;
                    index++;

                    stage = "building belt '" + current + "'";
                    var beltDriverMount = train.FindMount(beltPart.Key.Driver, out _);
                    var session = Export(beltPart.Key, beltDriverMount == null ? 0.0 : beltDriverMount.PhaseRad);
                    log.Add("built belt '" + current + "'");
                    saved.Add(session);
                    SavePart(session, current, folder, index, log);
                }

                foreach (var chainPart in chainParts)
                {
                    var current = chainPart.Key.Id;
                    index++;

                    stage = "building chain '" + current + "'";
                    var chainDriverMount = train.FindMount(chainPart.Key.Driver, out _);
                    var session = Export(chainPart.Key, chainDriverMount == null ? 0.0 : chainDriverMount.PhaseRad);
                    log.Add("built chain '" + current + "'");
                    saved.Add(session);
                    SavePart(session, current, folder, index, log);
                }

                stage = "creating the assembly";
                var assembly = _root.CreateEmptyAssemblyEx(string.IsNullOrWhiteSpace(assemblyName) ? DefaultAssemblyName : assemblyName, true);
                log.Add("created the assembly");
                var rootOccurrence = assembly.RootOccurrence;
                try
                {
                    assembly.ActiveOccurrence = rootOccurrence;
                }
                catch (Exception activeError)
                {
                    log.Add("setting the active occurrence failed: " + activeError.Message);
                }

                var occurrences = rootOccurrence.Occurrences;
                var factory = assembly.GeometryFactory;

                for (var i = 0; i < parts.Count; i++)
                {
                    var current = parts[i].Key.Id;
                    stage = "adding part '" + current + "' to the assembly";
                    Array matrix = ToAlibreMatrix(parts[i].Value);
                    var transformation = factory.CreateTransform(ref matrix);
                    AddPart(occurrences, saved[i], transformation);
                    log.Add("added part '" + current + "' to the assembly");
                }

                for (var i = 0; i < shaftParts.Count; i++)
                {
                    var current = shaftParts[i].Key.Id;
                    stage = "adding shaft part '" + current + "' to the assembly";
                    Array matrix = ToAlibreMatrix(shaftParts[i].Value);
                    var transformation = factory.CreateTransform(ref matrix);
                    AddPart(occurrences, saved[parts.Count + i], transformation);
                    log.Add("added shaft part '" + current + "' to the assembly");
                }

                for (var i = 0; i < beltParts.Count; i++)
                {
                    var current = beltParts[i].Key.Id;
                    stage = "adding belt '" + current + "' to the assembly";
                    Array matrix = ToAlibreMatrix(beltParts[i].Value);
                    var transformation = factory.CreateTransform(ref matrix);
                    AddPart(occurrences, saved[parts.Count + shaftParts.Count + i], transformation);
                    log.Add("added belt '" + current + "' to the assembly");
                }

                for (var i = 0; i < chainParts.Count; i++)
                {
                    var current = chainParts[i].Key.Id;
                    stage = "adding chain '" + current + "' to the assembly";
                    Array matrix = ToAlibreMatrix(chainParts[i].Value);
                    var transformation = factory.CreateTransform(ref matrix);
                    AddPart(occurrences, saved[parts.Count + shaftParts.Count + beltParts.Count + i], transformation);
                    log.Add("added chain '" + current + "' to the assembly");
                }

                stage = "regenerating the assembly";
                assembly.RegenerateDesign(true);
                log.Add("regenerated the assembly");
                WriteLog(folder, log);
                return assembly;
            }
            catch (Exception ex)
            {
                log.Add("FAILED while " + stage + ": " + ex);
                var logPath = WriteLog(folder, log);
                throw new InvalidOperationException(
                    $"Alibre export of the gear train to an assembly failed while {stage}: {ex.Message}"
                    + (logPath == null ? "" : " (details: " + logPath + ")"), ex);
            }
        }

        /// <summary>
        /// Saves a part to its own numbered folder, as a gear train often holds several parts with the same name. A rejected save is not fatal:
        /// the part is then added to the assembly from its open session.
        /// </summary>
        private static void SavePart(IADPartSession session, string name, string folder, int index, List<string> log)
        {
            try
            {
                var partFolder = Path.Combine(folder, index.ToString("00"));
                Directory.CreateDirectory(partFolder);
                object destination = partFolder;
                session.SaveNew(ref destination);
                log.Add("saved part '" + name + "' to " + partFolder);
            }
            catch (Exception saveError)
            {
                log.Add("saving part '" + name + "' failed: " + saveError.Message);
            }
        }

        /// <summary>
        /// Checks where Alibre is configured to save designs (the system option, read from the Alibre user profile, see
        /// <see cref="AlibreSaveTargetReader"/>) and notes it in the export log. Throws <see cref="InvalidOperationException"/> for every
        /// target but the Windows file system (the Vault, a PDM repository, an AD repository, or an unknown target): the Alibre API cannot
        /// change the option, and parts cannot be saved to a Windows folder while it is on.
        /// </summary>
        private void CheckSaveTarget(List<string> log)
        {
            var target = AlibreSaveTargetReader.Read(_root, out var source);
            log.Add("save target: " + target + " (" + source + ")");
            if (!AlibreSaveTargetReader.IsSafe(target))
            {
                throw new InvalidOperationException(AlibreSaveTargetReader.DescribeRefusal(target));
            }
        }

        private string WriteLog(string folder, List<string> lines)
        {
            try
            {
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, "export.log");
                File.WriteAllLines(path, lines);
                LastExportLog = path;
                return path;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Adds a saved part to the assembly by its file path, falling back to the open part session if the path is rejected.</summary>
        private static void AddPart(IADOccurrences occurrences, IADPartSession session, IADTransformation transformation)
        {
            var path = session.FilePath;
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    object file = path;
                    occurrences.Add(ref file, transformation);
                    return;
                }
                catch (Exception pathError)
                {
                    try
                    {
                        object design = session;
                        occurrences.Add(ref design, transformation);
                        return;
                    }
                    catch (Exception sessionError)
                    {
                        throw new InvalidOperationException(
                            $"by file '{path}': {pathError.Message}; by part session: {sessionError.Message}", sessionError);
                    }
                }
            }

            object part = session;
            occurrences.Add(ref part, transformation);
        }
    }
}
