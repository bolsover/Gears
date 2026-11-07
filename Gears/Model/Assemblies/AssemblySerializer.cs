using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// Saves and loads a <see cref="GearAssembly"/> as JSON: shafts with their placement, the gears mounted on them
    /// (spur, helical, internal, rack, bevel, worm and worm wheel) and the meshing relations. Derived results (centre
    /// distances, tooth phases of driven gears) are not stored; they are solved again after loading.
    /// </summary>
    public static class AssemblySerializer
    {
        public const int CurrentVersion = 1;

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string ToJson(GearAssembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var dto = new AssemblyDto { Version = CurrentVersion };
            foreach (var shaft in assembly.Shafts)
            {
                var shaftDto = new ShaftDto
                {
                    Id = shaft.Id,
                    Origin = ToArray(shaft.Placement.Origin),
                    Axis = ToArray(shaft.Placement.ZAxis),
                    Reference = ToArray(shaft.Placement.XAxis),
                    Diameter = shaft.Diameter,
                    Length = shaft.Length,
                    EndMargin = shaft.EndMargin,
                    RotationRad = shaft.RotationRad
                };

                foreach (var mount in shaft.Mounts)
                {
                    var gear = ToDto(mount.Component);
                    gear.AxialOffset = mount.AxialOffset;
                    gear.PhaseOffsetRad = mount.PhaseOffsetRad;
                    shaftDto.Gears.Add(gear);
                }

                dto.Shafts.Add(shaftDto);
            }

            foreach (var relation in assembly.Relations)
            {
                dto.Relations.Add(new RelationDto
                {
                    Driver = relation.Driver.Id,
                    Driven = relation.Driven.Id,
                    MeshType = relation.MeshType.ToString(),
                    CentreLineAngleDeg = relation.CentreLineAngleDeg,
                    CentreDistance = relation.CentreDistanceOverride
                });
            }

            foreach (var belt in assembly.Belts)
            {
                dto.Belts.Add(new BeltDto
                {
                    Id = belt.Id,
                    Name = belt.Name,
                    Driver = belt.Driver.Id,
                    Driven = belt.Driven.Id,
                    CentreDistance = belt.CentreDistance,
                    CentreLineAngleDeg = belt.CentreLineAngleDeg
                });
            }

            return JsonSerializer.Serialize(dto, Options);
        }

        public static GearAssembly FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("The JSON text is empty.", nameof(json));

            AssemblyDto dto;
            try
            {
                dto = JsonSerializer.Deserialize<AssemblyDto>(json, Options);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("The gear assembly file is not valid JSON: " + ex.Message, ex);
            }

            if (dto == null) throw new InvalidDataException("The gear assembly file is empty.");
            if (dto.Version < 1 || dto.Version > CurrentVersion)
            {
                throw new InvalidDataException($"The gear assembly file has an unsupported version ({dto.Version}).");
            }

            var assembly = new GearAssembly();
            var gears = new Dictionary<string, GearComponent>(StringComparer.Ordinal);

            foreach (var shaftDto in dto.Shafts ?? new List<ShaftDto>())
            {
                var placement = new Placement3D(
                    ToVector(shaftDto.Origin, nameof(shaftDto.Origin)),
                    ToVector(shaftDto.Axis, nameof(shaftDto.Axis)),
                    ToVector(shaftDto.Reference, nameof(shaftDto.Reference)));

                var shaft = new Shaft(shaftDto.Id, placement)
                {
                    Diameter = shaftDto.Diameter,
                    Length = shaftDto.Length,
                    EndMargin = shaftDto.EndMargin,
                    RotationRad = shaftDto.RotationRad
                };

                foreach (var gearDto in shaftDto.Gears ?? new List<GearDto>())
                {
                    var component = FromDto(gearDto);
                    if (gears.ContainsKey(component.Id))
                    {
                        throw new InvalidDataException($"Gear '{component.Id}' appears more than once in the file.");
                    }

                    gears.Add(component.Id, component);
                    var mount = shaft.Mount(component, gearDto.AxialOffset);
                    mount.PhaseOffsetRad = gearDto.PhaseOffsetRad;
                }

                assembly.AddShaft(shaft);
            }

            foreach (var relationDto in dto.Relations ?? new List<RelationDto>())
            {
                if (!gears.TryGetValue(relationDto.Driver ?? string.Empty, out var driver))
                {
                    throw new InvalidDataException($"A relation refers to the unknown gear '{relationDto.Driver}'.");
                }

                if (!gears.TryGetValue(relationDto.Driven ?? string.Empty, out var driven))
                {
                    throw new InvalidDataException($"A relation refers to the unknown gear '{relationDto.Driven}'.");
                }

                if (!Enum.TryParse(relationDto.MeshType, true, out MeshType meshType))
                {
                    throw new InvalidDataException($"Unknown mesh type '{relationDto.MeshType}'.");
                }

                var added = assembly.AddRelation(driver, driven, meshType, relationDto.CentreLineAngleDeg);
                added.CentreDistanceOverride = relationDto.CentreDistance;
            }

            foreach (var beltDto in dto.Belts ?? new List<BeltDto>())
            {
                if (!gears.TryGetValue(beltDto.Driver ?? string.Empty, out var beltDriver) || !(beltDriver is HtdPulleyComponent driverPulley))
                {
                    throw new InvalidDataException($"The belt '{beltDto.Id}' refers to '{beltDto.Driver}', which is not a pulley of the file.");
                }

                if (!gears.TryGetValue(beltDto.Driven ?? string.Empty, out var beltDriven) || !(beltDriven is HtdPulleyComponent drivenPulley))
                {
                    throw new InvalidDataException($"The belt '{beltDto.Id}' refers to '{beltDto.Driven}', which is not a pulley of the file.");
                }

                var belt = new TimingBelt(beltDto.Id, driverPulley, drivenPulley, beltDto.CentreDistance)
                {
                    Name = beltDto.Name ?? beltDto.Id,
                    CentreLineAngleDeg = beltDto.CentreLineAngleDeg
                };
                assembly.AddBelt(belt);
            }

            return assembly;
        }

        public static void Save(GearAssembly assembly, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A file name is required.", nameof(path));
            File.WriteAllText(path, ToJson(assembly), new UTF8Encoding(false));
        }

        public static GearAssembly Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A file name is required.", nameof(path));
            return FromJson(File.ReadAllText(path, Encoding.UTF8));
        }

        private static double[] ToArray(Vector3D v) => new[] { v.X, v.Y, v.Z };

        private static Vector3D ToVector(double[] values, string name)
        {
            if (values == null || values.Length != 3)
            {
                throw new InvalidDataException($"'{name}' must contain exactly three numbers.");
            }

            return new Vector3D(values[0], values[1], values[2]);
        }

        private static GearDto ToDto(GearComponent component)
        {
            var dto = new GearDto
            {
                Id = component.Id,
                Name = component.Name,
                Module = component.Module,
                PressureAngleDeg = component.PressureAngleDeg,
                FaceWidth = component.FaceWidth
            };

            switch (component)
            {
                case InvoluteGearComponent g:
                    dto.Type = "Involute";
                    dto.Teeth = g.Teeth;
                    dto.HelixAngleDeg = g.HelixAngleDeg;
                    dto.HelicalDirection = g.HelicalDirection;
                    if (g.IsHerringbone) dto.IsHerringbone = true;
                    dto.Clearance = g.Clearance;
                    dto.Backlash = g.Backlash;
                    dto.ProfileShift = g.ProfileShift;
                    dto.IsInternal = g.IsInternal;
                    dto.RimThickness = g.RimThickness;
                    dto.OptimalShift = g.OptimalShift;
                    dto.ShowFullGear = g.ShowFullGear;
                    break;
                case RackComponent g:
                    dto.Type = "Rack";
                    dto.Teeth = g.Teeth;
                    dto.Backlash = g.Backlash;
                    dto.BodyHeight = g.BodyHeight;
                    if (g.IsHelical)
                    {
                        dto.HelixAngleDeg = g.HelixAngleDeg;
                        dto.HelicalDirection = g.HelicalDirection;
                        if (g.IsHerringbone) dto.IsHerringbone = true;
                    }
                    break;
                case BevelGearComponent g:
                    dto.Type = "Bevel";
                    dto.Teeth = g.Teeth;
                    dto.MateTeeth = g.MateTeeth;
                    dto.ShaftAngleDeg = g.ShaftAngleDeg;
                    dto.Backlash = g.Backlash;
                    dto.BevelType = g.BevelType.ToString();
                    dto.RackTipRadiusFactor = g.RackTipRadiusFactor;
                    dto.BoreDiameter = g.BoreDiameter;
                    dto.HasHub = g.HasHub;
                    dto.HubDiameter = g.HubDiameter;
                    dto.HubLength = g.HubLength;
                    break;
                case WormComponent g:
                    dto.Type = "Worm";
                    dto.Starts = g.Starts;
                    dto.DiameterQuotient = g.DiameterQuotient;
                    dto.HelicalDirection = g.HelicalDirection;
                    break;
                case WormWheelComponent g:
                    dto.Type = "WormWheel";
                    dto.Teeth = g.Teeth;
                    dto.LeadAngleDeg = g.LeadAngleDeg;
                    dto.HelicalDirection = g.HelicalDirection;
                    dto.Backlash = g.Backlash;
                    break;
                case HtdPulleyComponent g:
                    dto.Type = "HtdPulley";
                    dto.Teeth = g.Teeth;
                    dto.HtdSeries = g.Series.ToString();
                    if (g.HasFlanges)
                    {
                        dto.HasFlanges = true;
                        dto.FlangeHeight = g.FlangeHeight;
                        dto.FlangeThickness = g.FlangeThickness;
                    }

                    if (g.BoreDiameter > 0.0) dto.BoreDiameter = g.BoreDiameter;
                    if (g.HasHub)
                    {
                        dto.HubDiameter = g.HubDiameter;
                        dto.HubLength = g.HubLength;
                    }

                    break;
                default:
                    throw new NotSupportedException($"Gear '{component.Id}' is of type {component.GetType().Name}, which cannot be saved.");
            }

            return dto;
        }

        private static GearComponent FromDto(GearDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Id)) throw new InvalidDataException("A gear in the file has no identifier.");

            GearComponent component;
            switch (dto.Type)
            {
                case "Involute":
                    component = new InvoluteGearComponent(dto.Id)
                    {
                        Teeth = dto.Teeth ?? 20,
                        HelixAngleDeg = dto.HelixAngleDeg ?? 0.0,
                        HelicalDirection = dto.HelicalDirection ?? "R",
                        IsHerringbone = dto.IsHerringbone ?? false,
                        Clearance = dto.Clearance ?? 0.5,
                        Backlash = dto.Backlash ?? 0.0,
                        ProfileShift = dto.ProfileShift ?? 0.0,
                        IsInternal = dto.IsInternal ?? false,
                        RimThickness = dto.RimThickness ?? 10.0,
                        OptimalShift = dto.OptimalShift ?? false,
                        ShowFullGear = dto.ShowFullGear ?? true
                    };
                    break;
                case "Rack":
                    component = new RackComponent(dto.Id)
                    {
                        Teeth = dto.Teeth ?? 12,
                        Backlash = dto.Backlash ?? 0.0,
                        BodyHeight = dto.BodyHeight ?? 10.0,
                        HelixAngleDeg = dto.HelixAngleDeg ?? 0.0,
                        HelicalDirection = dto.HelicalDirection ?? "R",
                        IsHerringbone = dto.IsHerringbone ?? false
                    };
                    break;
                case "Bevel":
                    component = new BevelGearComponent(dto.Id)
                    {
                        Teeth = dto.Teeth ?? 20,
                        MateTeeth = dto.MateTeeth ?? 0,
                        ShaftAngleDeg = dto.ShaftAngleDeg ?? 90.0,
                        Backlash = dto.Backlash ?? 0.0,
                        BevelType = Enum.TryParse(dto.BevelType, true, out BevelGearType bevelType) ? bevelType : BevelGearType.Standard,
                        RackTipRadiusFactor = dto.RackTipRadiusFactor ?? 0.0,
                        BoreDiameter = dto.BoreDiameter ?? 0.0,
                        HasHub = dto.HasHub ?? true,
                        HubDiameter = dto.HubDiameter ?? 0.0,
                        HubLength = dto.HubLength ?? 0.0
                    };
                    break;
                case "Worm":
                    component = new WormComponent(dto.Id)
                    {
                        Starts = dto.Starts ?? 1,
                        DiameterQuotient = dto.DiameterQuotient ?? 10.0,
                        HelicalDirection = dto.HelicalDirection ?? "R"
                    };
                    break;
                case "WormWheel":
                    component = new WormWheelComponent(dto.Id)
                    {
                        Teeth = dto.Teeth ?? 30,
                        LeadAngleDeg = dto.LeadAngleDeg ?? 0.0,
                        HelicalDirection = dto.HelicalDirection ?? "R",
                        Backlash = dto.Backlash ?? 0.0
                    };
                    break;
                case "HtdPulley":
                    component = new HtdPulleyComponent(dto.Id)
                    {
                        Series = Enum.TryParse(dto.HtdSeries, true, out HtdSeries series) ? series : HtdSeries.Htd5M,
                        Teeth = dto.Teeth ?? 20,
                        HasFlanges = dto.HasFlanges ?? false,
                        FlangeHeight = dto.FlangeHeight ?? 2.0,
                        FlangeThickness = dto.FlangeThickness ?? 1.5,
                        BoreDiameter = dto.BoreDiameter ?? 0.0,
                        HubDiameter = dto.HubDiameter ?? 0.0,
                        HubLength = dto.HubLength ?? 0.0
                    };
                    break;
                default:
                    throw new InvalidDataException($"Gear '{dto.Id}' has the unknown type '{dto.Type}'.");
            }

            component.Name = dto.Name ?? dto.Id;
            component.Module = dto.Module;
            component.PressureAngleDeg = dto.PressureAngleDeg;
            component.FaceWidth = dto.FaceWidth;
            return component;
        }

        private sealed class AssemblyDto
        {
            public int Version { get; set; }
            public List<ShaftDto> Shafts { get; set; } = new List<ShaftDto>();
            public List<RelationDto> Relations { get; set; } = new List<RelationDto>();

            /// <summary>Timing belts; absent in older files.</summary>
            public List<BeltDto> Belts { get; set; } = new List<BeltDto>();
        }

        private sealed class BeltDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Driver { get; set; }
            public string Driven { get; set; }
            public double CentreDistance { get; set; }
            public double CentreLineAngleDeg { get; set; }
        }

        private sealed class ShaftDto
        {
            public string Id { get; set; }
            public double[] Origin { get; set; }
            public double[] Axis { get; set; }
            public double[] Reference { get; set; }
            public double Diameter { get; set; }
            public double Length { get; set; }
            public double EndMargin { get; set; }
            public double RotationRad { get; set; }
            public List<GearDto> Gears { get; set; } = new List<GearDto>();
        }

        private sealed class GearDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public double Module { get; set; }
            public double PressureAngleDeg { get; set; }
            public double FaceWidth { get; set; }
            public double AxialOffset { get; set; }
            public double PhaseOffsetRad { get; set; }

            public int? Teeth { get; set; }
            public double? HelixAngleDeg { get; set; }
            public string HelicalDirection { get; set; }
            public double? Clearance { get; set; }
            public double? Backlash { get; set; }
            public double? ProfileShift { get; set; }
            public bool? IsHerringbone { get; set; }
            public bool? IsInternal { get; set; }
            public double? RimThickness { get; set; }
            public bool? OptimalShift { get; set; }
            public bool? ShowFullGear { get; set; }
            public double? BodyHeight { get; set; }
            public int? MateTeeth { get; set; }
            public double? ShaftAngleDeg { get; set; }

            /// <summary>Bevel tooth system (Standard or Gleason); absent in older files, which are Standard.</summary>
            public string BevelType { get; set; }

            /// <summary>Bevel rack tip corner radius as a multiple of the module; absent in older files, which get a sharp corner.</summary>
            public double? RackTipRadiusFactor { get; set; }

            /// <summary>Bevel bore and hub (mm); zero means automatic. Absent in older files, which get the automatic values and a hub.</summary>
            public double? BoreDiameter { get; set; }
            public bool? HasHub { get; set; }
            public double? HubDiameter { get; set; }
            public double? HubLength { get; set; }
            public int? Starts { get; set; }
            public double? DiameterQuotient { get; set; }
            public double? LeadAngleDeg { get; set; }

            /// <summary>HTD pulley: the belt series (Htd3M, Htd5M, Htd8M or Htd14M) and the optional flanges (mm).</summary>
            public string HtdSeries { get; set; }
            public bool? HasFlanges { get; set; }
            public double? FlangeHeight { get; set; }
            public double? FlangeThickness { get; set; }
        }

        private sealed class RelationDto
        {
            public string Driver { get; set; }
            public string Driven { get; set; }
            public string MeshType { get; set; }
            public double CentreLineAngleDeg { get; set; }

            /// <summary>Fixed working centre distance (mm); absent when the distance follows from the profile shifts.</summary>
            public double? CentreDistance { get; set; }
        }
    }
}
