using System.Collections.Generic;
using Bolsover.Gears.Calculator;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// One gear of a solved assembly with its outline and mesh expressed in world coordinates.
    /// </summary>
    public sealed class SceneItem
    {
        public SceneItem(
            GearComponent component,
            Shaft shaft,
            Transform3D localToWorld,
            List<List<GearPoint>> polylines,
            GearMesh3DBuilder.Mesh3D mesh)
        {
            Component = component;
            Shaft = shaft;
            LocalToWorld = localToWorld;
            Polylines = polylines;
            Mesh = mesh;
        }

        public GearComponent Component { get; }
        public Shaft Shaft { get; }

        /// <summary>Transform that was applied to the component's local geometry.</summary>
        public Transform3D LocalToWorld { get; }

        /// <summary>World X/Y coordinates of the outline (the projection onto the world XY plane), 7 decimal places.</summary>
        public List<List<GearPoint>> Polylines { get; }

        /// <summary>World-space mesh, 7 decimal places. Kept per component so it can be coloured, picked or hidden.</summary>
        public GearMesh3DBuilder.Mesh3D Mesh { get; }
    }

    /// <summary>
    /// One shaft of a solved assembly as a world-space cylinder mesh (7 decimal places).
    /// </summary>
    public sealed class SceneShaftItem
    {
        public SceneShaftItem(Shaft shaft, GearMesh3DBuilder.Mesh3D mesh)
        {
            Shaft = shaft;
            Mesh = mesh;
        }

        public Shaft Shaft { get; }
        public GearMesh3DBuilder.Mesh3D Mesh { get; }
    }

    /// <summary>One timing belt of a solved assembly with its outlines (world X/Y) and its world-space mesh.</summary>
    public sealed class SceneBeltItem
    {
        public SceneBeltItem(TimingBelt belt, List<List<GearPoint>> polylines, GearMesh3DBuilder.Mesh3D mesh)
        {
            Belt = belt;
            Polylines = polylines;
            Mesh = mesh;
        }

        public TimingBelt Belt { get; }
        public List<List<GearPoint>> Polylines { get; }
        public GearMesh3DBuilder.Mesh3D Mesh { get; }
    }

    /// <summary>One roller chain of a solved assembly with its outlines (world X/Y) and its world-space mesh.</summary>
    public sealed class SceneChainItem
    {
        public SceneChainItem(RollerChain chain, List<List<GearPoint>> polylines, GearMesh3DBuilder.Mesh3D mesh)
        {
            Chain = chain;
            Polylines = polylines;
            Mesh = mesh;
        }

        public RollerChain Chain { get; }
        public List<List<GearPoint>> Polylines { get; }
        public GearMesh3DBuilder.Mesh3D Mesh { get; }
    }

    /// <summary>
    /// A solved assembly as a list of per-component world-space geometry.
    /// <see cref="Items"/> holds the gears; <see cref="ShaftItems"/> holds the shafts and is not part of
    /// <see cref="CombinedPolylines"/> or <see cref="CombinedMesh"/>.
    /// </summary>
    public sealed class AssemblyScene
    {
        private readonly List<SceneItem> _items = new List<SceneItem>();
        private readonly List<SceneShaftItem> _shaftItems = new List<SceneShaftItem>();
        private readonly List<SceneBeltItem> _beltItems = new List<SceneBeltItem>();

        /// <summary>The belts; like the shafts they are not part of <see cref="CombinedPolylines"/> or <see cref="CombinedMesh"/>.</summary>
        public IReadOnlyList<SceneBeltItem> BeltItems => _beltItems;

        public void Add(SceneBeltItem item) => _beltItems.Add(item);

        private readonly List<SceneChainItem> _chainItems = new List<SceneChainItem>();

        /// <summary>The roller chains; like the belts and shafts they are not part of <see cref="CombinedPolylines"/> or <see cref="CombinedMesh"/>.</summary>
        public IReadOnlyList<SceneChainItem> ChainItems => _chainItems;

        public void Add(SceneChainItem item) => _chainItems.Add(item);

        public IReadOnlyList<SceneItem> Items => _items;

        public IReadOnlyList<SceneShaftItem> ShaftItems => _shaftItems;

        public void Add(SceneItem item) => _items.Add(item);

        public void Add(SceneShaftItem item) => _shaftItems.Add(item);

        /// <summary>Finds the scene item of a gear; null when the gear is not in the scene.</summary>
        public SceneItem Find(GearComponent component)
        {
            foreach (var item in _items)
            {
                if (ReferenceEquals(item.Component, component)) return item;
            }

            return null;
        }

        /// <summary>All component outlines in scene order.</summary>
        public List<List<GearPoint>> CombinedPolylines()
        {
            var all = new List<List<GearPoint>>();
            foreach (var item in _items)
            {
                all.AddRange(item.Polylines);
            }

            return all;
        }

        /// <summary>All component meshes merged into one mesh (for export or single-mesh viewers).</summary>
        public GearMesh3DBuilder.Mesh3D CombinedMesh()
        {
            var combined = new GearMesh3DBuilder.Mesh3D();
            foreach (var item in _items)
            {
                combined = GearMesh3DBuilder.Combine(combined, item.Mesh);
            }

            return combined;
        }
    }
}
