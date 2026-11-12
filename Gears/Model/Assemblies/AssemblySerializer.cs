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
                    CentreDistance = relation.CentreDistanceOverride,
                    ShaftAngleDeg = relation.ShaftAngleDeg,
                    OffsetMm = relation.OffsetMm
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

            foreach (var chain in assembly.Chains)
            {
                dto.Chains.Add(new BeltDto
                {
                    Id = chain.Id,
                    Name = chain.Name,
                    Driver = chain.Driver.Id,
                    Driven = chain.Driven.Id,
                    CentreDistance = chain.CentreDistance,
                    CentreLineAngleDeg = chain.CentreLineAngleDeg
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

                var added = assembly.AddRelation(driver, driven, meshType, relationDto.CentreLineAngleDeg, relationDto.ShaftAngleDeg, relationDto.OffsetMm);
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

            foreach (var chainDto in dto.Chains ?? new List<BeltDto>())
            {
                if (!gears.TryGetValue(chainDto.Driver ?? string.Empty, out var chainDriver) || !(chainDriver is SprocketComponent driverSprocket))
                {
                    throw new InvalidDataException($"The chain '{chainDto.Id}' refers to '{chainDto.Driver}', which is not a sprocket of the file.");
                }

                if (!gears.TryGetValue(chainDto.Driven ?? string.Empty, out var chainDriven) || !(chainDriven is SprocketComponent drivenSprocket))
                {
                    throw new InvalidDataException($"The chain '{chainDto.Id}' refers to '{chainDto.Driven}', which is not a sprocket of the file.");
                }

                var chain = new RollerChain(chainDto.Id, driverSprocket, drivenSprocket, chainDto.CentreDistance)
                {
                    Name = chainDto.Name ?? chainDto.Id,
                    CentreLineAngleDeg = chainDto.CentreLineAngleDeg
                };
                assembly.AddChain(chain);
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
                    WriteInvolute(dto, g);
                    break;
                case RackComponent g:
                    WriteRack(dto, g);
                    break;
                case HypoidGearComponent g:
                    WriteHypoid(dto, g);
                    break;
                case ZerolBevelGearComponent g:
                    WriteZerolBevel(dto, g);
                    break;
                case SpiralBevelGearComponent g:
                    WriteSpiralBevel(dto, g);
                    break;
                case BevelGearComponent g:
                    WritePlainBevel(dto, g);
                    break;
                case WormComponent g:
                    WriteWorm(dto, g);
                    break;
                case WormWheelComponent g:
                    WriteWormWheel(dto, g);
                    break;
                case HtdPulleyComponent g:
                    WriteHtdPulley(dto, g);
                    break;
                case SprocketComponent g:
                    WriteSprocket(dto, g);
                    break;
                case CycloidalGearComponent g:
                    WriteCycloidal(dto, g);
                    break;
                default:
                    throw new NotSupportedException($"Gear '{component.Id}' is of type {component.GetType().Name}, which cannot be saved.");
            }

            return dto;
        }

        private static void WriteInvolute(GearDto dto, InvoluteGearComponent g)
        {
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
        }

        private static void WriteRack(GearDto dto, RackComponent g)
        {
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
        }

        private static void WriteHypoid(GearDto dto, HypoidGearComponent g)
        {
            dto.Type = "Hypoid";
            WriteBevel(dto, g);
            WriteSpiral(dto, g);
            dto.OffsetMm = g.OffsetMm;
            dto.IsPinion = g.IsPinion;
        }

        private static void WriteZerolBevel(GearDto dto, ZerolBevelGearComponent g)
        {
            dto.Type = "ZerolBevel";
            WriteBevel(dto, g);
            dto.CutterRadius = g.CutterRadius;
            dto.Hand = g.Hand == SpiralHand.Right ? "R" : "L";
        }

        private static void WriteSpiralBevel(GearDto dto, SpiralBevelGearComponent g)
        {
            dto.Type = "SpiralBevel";
            WriteBevel(dto, g);
            WriteSpiral(dto, g);
        }

        private static void WritePlainBevel(GearDto dto, BevelGearComponent g)
        {
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
        }

        private static void WriteWorm(GearDto dto, WormComponent g)
        {
            dto.Type = "Worm";
            dto.Starts = g.Starts;
            dto.DiameterQuotient = g.DiameterQuotient;
            dto.HelicalDirection = g.HelicalDirection;
        }

        private static void WriteWormWheel(GearDto dto, WormWheelComponent g)
        {
            dto.Type = "WormWheel";
            dto.Teeth = g.Teeth;
            dto.LeadAngleDeg = g.LeadAngleDeg;
            dto.HelicalDirection = g.HelicalDirection;
            dto.Backlash = g.Backlash;
        }

        private static void WriteHtdPulley(GearDto dto, HtdPulleyComponent g)
        {
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
        }

        private static void WriteSprocket(GearDto dto, SprocketComponent g)
        {
            dto.Type = "Sprocket";
            dto.Teeth = g.Teeth;
            dto.ChainSeries = g.Series.ToString();
            dto.ToothForm = g.ToothForm.ToString();
            if (g.BoreDiameter > 0.0) dto.BoreDiameter = g.BoreDiameter;
            if (g.HasHub)
            {
                dto.HubDiameter = g.HubDiameter;
                dto.HubLength = g.HubLength;
            }
        }

        private static void WriteCycloidal(GearDto dto, CycloidalGearComponent g)
        {
            dto.Type = "Cycloidal";
            dto.Teeth = g.Teeth;
            dto.MateTeeth = g.MateTeeth;
            dto.CycloidalRole = g.Role.ToString();
            if (g.CustomSlopEnabled)
            {
                dto.CustomSlopEnabled = true;
                dto.CustomSlop = g.CustomSlop;
            }

            if (g.CentreHole > 0.0) dto.CentreHole = g.CentreHole;
        }

        private static void WriteBevel(GearDto dto, BevelGearComponent g)
        {
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
        }

        private static void WriteSpiral(GearDto dto, SpiralBevelGearComponent g)
        {
            dto.MeanSpiralAngleDeg = g.MeanSpiralAngleDeg;
            dto.CutterRadius = g.CutterRadius;
            dto.Hand = g.Hand == SpiralHand.Right ? "R" : "L";
        }

        private static void ReadBevel(GearDto dto, BevelGearComponent g)
        {
            g.Teeth = dto.Teeth ?? 20;
            g.MateTeeth = dto.MateTeeth ?? 0;
            g.ShaftAngleDeg = dto.ShaftAngleDeg ?? 90.0;
            g.Backlash = dto.Backlash ?? 0.0;
            g.BevelType = Enum.TryParse(dto.BevelType, true, out BevelGearType bevelType) ? bevelType : BevelGearType.Standard;
            g.RackTipRadiusFactor = dto.RackTipRadiusFactor ?? 0.0;
            g.BoreDiameter = dto.BoreDiameter ?? 0.0;
            g.HasHub = dto.HasHub ?? true;
            g.HubDiameter = dto.HubDiameter ?? 0.0;
            g.HubLength = dto.HubLength ?? 0.0;
        }

        private static void ReadSpiral(GearDto dto, SpiralBevelGearComponent g, double defaultSpiralAngleDeg)
        {
            g.MeanSpiralAngleDeg = dto.MeanSpiralAngleDeg ?? defaultSpiralAngleDeg;
            g.CutterRadius = dto.CutterRadius ?? 0.0;
            g.Hand = string.Equals(dto.Hand, "L", StringComparison.OrdinalIgnoreCase) ? SpiralHand.Left : SpiralHand.Right;
        }

        private static GearComponent FromDto(GearDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Id)) throw new InvalidDataException("A gear in the file has no identifier.");

            GearComponent component;
            switch (dto.Type)
            {
                case "Involute":
                    component = ReadInvolute(dto);
                    break;
                case "Rack":
                    component = ReadRack(dto);
                    break;
                case "Bevel":
                    component = ReadPlainBevel(dto);
                    break;
                case "SpiralBevel":
                    component = ReadSpiralBevel(dto);
                    break;
                case "ZerolBevel":
                    component = ReadZerolBevel(dto);
                    break;
                case "Hypoid":
                    component = ReadHypoid(dto);
                    break;
                case "Worm":
                    component = ReadWorm(dto);
                    break;
                case "WormWheel":
                    component = ReadWormWheel(dto);
                    break;
                case "HtdPulley":
                    component = ReadHtdPulley(dto);
                    break;
                case "Sprocket":
                    component = ReadSprocket(dto);
                    break;
                case "Cycloidal":
                    component = ReadCycloidal(dto);
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

        private static InvoluteGearComponent ReadInvolute(GearDto dto)
        {
            return new InvoluteGearComponent(dto.Id)
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
        }

        private static RackComponent ReadRack(GearDto dto)
        {
            return new RackComponent(dto.Id)
            {
                Teeth = dto.Teeth ?? 12,
                Backlash = dto.Backlash ?? 0.0,
                BodyHeight = dto.BodyHeight ?? 10.0,
                HelixAngleDeg = dto.HelixAngleDeg ?? 0.0,
                HelicalDirection = dto.HelicalDirection ?? "R",
                IsHerringbone = dto.IsHerringbone ?? false
            };
        }

        private static BevelGearComponent ReadPlainBevel(GearDto dto)
        {
            return new BevelGearComponent(dto.Id)
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
        }

        private static SpiralBevelGearComponent ReadSpiralBevel(GearDto dto)
        {
            var spiral = new SpiralBevelGearComponent(dto.Id);
            ReadBevel(dto, spiral);
            ReadSpiral(dto, spiral, 35.0);
            return spiral;
        }

        private static ZerolBevelGearComponent ReadZerolBevel(GearDto dto)
        {
            var zerol = new ZerolBevelGearComponent(dto.Id);
            ReadBevel(dto, zerol);
            ReadSpiral(dto, zerol, 0.0);
            return zerol;
        }

        private static HypoidGearComponent ReadHypoid(GearDto dto)
        {
            var hypoid = new HypoidGearComponent(dto.Id);
            ReadBevel(dto, hypoid);
            ReadSpiral(dto, hypoid, 45.0);
            hypoid.OffsetMm = dto.OffsetMm ?? 0.0;
            hypoid.IsPinion = dto.IsPinion ?? false;
            return hypoid;
        }

        private static WormComponent ReadWorm(GearDto dto)
        {
            return new WormComponent(dto.Id)
            {
                Starts = dto.Starts ?? 1,
                DiameterQuotient = dto.DiameterQuotient ?? 10.0,
                HelicalDirection = dto.HelicalDirection ?? "R"
            };
        }

        private static WormWheelComponent ReadWormWheel(GearDto dto)
        {
            return new WormWheelComponent(dto.Id)
            {
                Teeth = dto.Teeth ?? 30,
                LeadAngleDeg = dto.LeadAngleDeg ?? 0.0,
                HelicalDirection = dto.HelicalDirection ?? "R",
                Backlash = dto.Backlash ?? 0.0
            };
        }

        private static HtdPulleyComponent ReadHtdPulley(GearDto dto)
        {
            return new HtdPulleyComponent(dto.Id)
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
        }

        private static SprocketComponent ReadSprocket(GearDto dto)
        {
            return new SprocketComponent(dto.Id)
            {
                Series = Enum.TryParse(dto.ChainSeries, true, out ChainSeries series) ? series : ChainSeries.B08,
                Teeth = dto.Teeth ?? 19,
                ToothForm = Enum.TryParse(dto.ToothForm, true, out SprocketToothForm form) ? form : SprocketToothForm.Maximum,
                BoreDiameter = dto.BoreDiameter ?? 0.0,
                HubDiameter = dto.HubDiameter ?? 0.0,
                HubLength = dto.HubLength ?? 0.0
            };
        }

        private static CycloidalGearComponent ReadCycloidal(GearDto dto)
        {
            return new CycloidalGearComponent(dto.Id)
            {
                Teeth = dto.Teeth ?? CycloidalGearComponent.DefaultWheelTeeth,
                MateTeeth = dto.MateTeeth ?? 0,
                Role = Enum.TryParse(dto.CycloidalRole, true, out CycloidalGearRole role) ? role : CycloidalGearRole.Wheel,
                CustomSlopEnabled = dto.CustomSlopEnabled ?? false,
                CustomSlop = dto.CustomSlop ?? 0.0,
                CentreHole = dto.CentreHole ?? 0.0
            };
        }

        private sealed class AssemblyDto
        {
            public int Version { get; set; }
            public List<ShaftDto> Shafts { get; set; } = new List<ShaftDto>();
            public List<RelationDto> Relations { get; set; } = new List<RelationDto>();

            /// <summary>Timing belts; absent in older files.</summary>
            public List<BeltDto> Belts { get; set; } = new List<BeltDto>();

            /// <summary>Roller chains (the same fields as a belt); absent in older files.</summary>
            public List<BeltDto> Chains { get; set; } = new List<BeltDto>();
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

            /// <summary>Cycloidal gear: Wheel or Pinion, the optional clearance (mm) and the centre hole (mm); the teeth of the mate are in <see cref="MateTeeth"/>.</summary>
            public string CycloidalRole { get; set; }
            public bool? CustomSlopEnabled { get; set; }
            public double? CustomSlop { get; set; }
            public double? CentreHole { get; set; }

            /// <summary>Sprocket: the ISO 606 chain series (B06 to B40) and the tooth form (Maximum or Minimum); the teeth, bore and hub use the shared fields.</summary>
            public string ChainSeries { get; set; }
            public string ToothForm { get; set; }

            /// <summary>Spiral, zerol and hypoid bevel gears: the mean spiral angle (degrees; the pinion's for a hypoid pair), the cutter radius (mm, zero is automatic) and the hand ("R" or "L").</summary>
            public double? MeanSpiralAngleDeg { get; set; }
            public double? CutterRadius { get; set; }
            public string Hand { get; set; }

            /// <summary>Hypoid gears: the offset of the pinion axis (mm) and whether this is the pinion.</summary>
            public double? OffsetMm { get; set; }
            public bool? IsPinion { get; set; }
        }

        private sealed class RelationDto
        {
            public string Driver { get; set; }
            public string Driven { get; set; }
            public string MeshType { get; set; }
            public double CentreLineAngleDeg { get; set; }

            /// <summary>Fixed working centre distance (mm); absent when the distance follows from the profile shifts.</summary>
            public double? CentreDistance { get; set; }

            /// <summary>Angle between the two shafts (degrees) of a crossed helical mesh; absent (0) in older files and for the other mesh types.</summary>
            public double ShaftAngleDeg { get; set; }

            /// <summary>Offset between the shafts (mm) of a hypoid mesh; absent (0) in older files and for the other mesh types.</summary>
            public double OffsetMm { get; set; }
        }
    }
}
