using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model.Assemblies;
using HelixToolkit.Wpf;

namespace Bolsover.Gears.Gui
{
    /// <summary>
    /// WPF 3D viewport for a whole gear assembly. It keeps one visual per gear and per shaft (a scene graph), so each
    /// part can be coloured by shaft, highlighted when selected, hidden, or picked with the mouse.
    /// </summary>
    public class HelixAssemblyViewport : UserControl
    {
        private static readonly System.Windows.Media.Color[] Palette =
        {
            System.Windows.Media.Color.FromRgb(70, 130, 180),
            System.Windows.Media.Color.FromRgb(60, 160, 110),
            System.Windows.Media.Color.FromRgb(170, 110, 190),
            System.Windows.Media.Color.FromRgb(200, 120, 80),
            System.Windows.Media.Color.FromRgb(90, 160, 170),
            System.Windows.Media.Color.FromRgb(150, 150, 70)
        };

        private static readonly System.Windows.Media.Color SelectedColor = System.Windows.Media.Color.FromRgb(255, 170, 0);
        private static readonly System.Windows.Media.Color ShaftColor = System.Windows.Media.Color.FromRgb(150, 150, 155);
        private static readonly System.Windows.Media.Color BeltColor = System.Windows.Media.Color.FromRgb(55, 55, 60);

        private readonly List<Visual3D> _sceneVisuals = new List<Visual3D>();
        private readonly Dictionary<Model3D, object> _modelTags = new Dictionary<Model3D, object>();
        private bool _hasFitted;
        private System.Windows.Point _downPoint;

        public HelixAssemblyViewport()
        {
            Viewport = new HelixViewport3D
            {
                ShowCoordinateSystem = true,
                ShowViewCube = true,
                ShowCameraInfo = false,
                ShowTriangleCountInfo = false,
                CameraRotationMode = CameraRotationMode.Trackball,
                IsPanEnabled = true,
                IsRotationEnabled = true,
                IsZoomEnabled = true
            };

            Viewport.Children.Add(new DefaultLights());
            Viewport.MouseLeftButtonDown += (s, e) => _downPoint = e.GetPosition(Viewport);
            Viewport.MouseLeftButtonUp += HandleMouseUp;
            Content = Viewport;
        }

        public HelixViewport3D Viewport { get; }

        /// <summary>Raised when the user clicks a gear or shaft in solid mode. The argument is the picked <see cref="GearComponent"/> or <see cref="Shaft"/>.</summary>
        public event Action<object> ItemPicked;

        /// <summary>Number of visuals currently in the scene (gears and shafts).</summary>
        public int VisualCount => _sceneVisuals.Count;

        /// <summary>Replaces the displayed scene.</summary>
        /// <param name="scene">The solved scene; null clears the viewport.</param>
        /// <param name="selected">The selected gears, shafts and belts, drawn in the highlight colour; may be null.</param>
        /// <param name="hiddenGears">Gears that are not drawn; may be null.</param>
        /// <param name="wireframe">True to draw triangle edges instead of solid faces.</param>
        /// <param name="showShafts">True to draw the shaft cylinders.</param>
        public void SetScene(AssemblyScene scene, IReadOnlyCollection<object> selected, ISet<GearComponent> hiddenGears, bool wireframe, bool showShafts)
        {
            bool IsSelected(object tag)
            {
                if (selected == null) return false;
                foreach (var candidate in selected)
                {
                    if (ReferenceEquals(candidate, tag)) return true;
                }

                return false;
            }

            foreach (var visual in _sceneVisuals)
            {
                Viewport.Children.Remove(visual);
            }

            _sceneVisuals.Clear();
            _modelTags.Clear();
            if (scene == null) return;

            var shaftIndex = new Dictionary<Shaft, int>();
            foreach (var shaftItem in scene.ShaftItems)
            {
                shaftIndex[shaftItem.Shaft] = shaftIndex.Count;
                if (!showShafts) continue;
                var color = IsSelected(shaftItem.Shaft) ? SelectedColor : ShaftColor;
                AddMesh(shaftItem.Mesh, shaftItem.Shaft, color, wireframe);
            }

            foreach (var item in scene.Items)
            {
                if (hiddenGears != null && hiddenGears.Contains(item.Component)) continue;
                var index = shaftIndex.TryGetValue(item.Shaft, out var i) ? i : 0;
                var color = IsSelected(item.Component) ? SelectedColor : Palette[index % Palette.Length];
                AddMesh(item.Mesh, item.Component, color, wireframe);
            }

            foreach (var beltItem in scene.BeltItems)
            {
                if (hiddenGears != null && (hiddenGears.Contains(beltItem.Belt.Driver) || hiddenGears.Contains(beltItem.Belt.Driven))) continue;
                var color = IsSelected(beltItem.Belt) ? SelectedColor : BeltColor;
                AddMesh(beltItem.Mesh, beltItem.Belt, color, wireframe);
            }

            if (!_hasFitted && _sceneVisuals.Count > 0)
            {
                _hasFitted = true;
                FitView();
            }
        }

        /// <summary>Replaces the displayed scene with a single mesh, for example a gear blank under test.</summary>
        /// <param name="mesh">The mesh to show; null clears the viewport.</param>
        /// <param name="color">The colour of the solid.</param>
        /// <param name="wireframe">True to draw triangle edges instead of solid faces.</param>
        /// <param name="fit">True to zoom the view to the mesh.</param>
        public void ShowMesh(GearMesh3DBuilder.Mesh3D mesh, System.Windows.Media.Color color, bool wireframe, bool fit)
        {
            foreach (var visual in _sceneVisuals)
            {
                Viewport.Children.Remove(visual);
            }

            _sceneVisuals.Clear();
            _modelTags.Clear();
            if (mesh == null) return;

            AddMesh(mesh, null, color, wireframe);
            if (fit && _sceneVisuals.Count > 0) FitView();
        }

        public void FitView()
        {
            try
            {
                Viewport.ZoomExtents();
            }
            catch
            {
                // Ignore if the viewport is not yet sized.
            }
        }

        private void AddMesh(GearMesh3DBuilder.Mesh3D mesh, object tag, System.Windows.Media.Color color, bool wireframe)
        {
            if (mesh == null || mesh.Vertices.Count == 0 || mesh.TriangleIndices.Count < 3) return;

            if (wireframe)
            {
                var lines = new LinesVisual3D
                {
                    Thickness = 1.0,
                    Color = color,
                    Points = HelixMeshAdapter.ToWireframeLinePoints(mesh)
                };
                Viewport.Children.Add(lines);
                _sceneVisuals.Add(lines);
                return;
            }

            var material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255)), 10.0));

            var model = new GeometryModel3D
            {
                Geometry = HelixMeshAdapter.ToMeshGeometry3D(mesh),
                Material = material,
                BackMaterial = material
            };

            var visual = new ModelVisual3D { Content = model };
            Viewport.Children.Add(visual);
            _sceneVisuals.Add(visual);
            _modelTags[model] = tag;
        }

        private void HandleMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var position = e.GetPosition(Viewport);
            if ((position - _downPoint).Length > 3.0) return; // the mouse was dragged, not clicked

            try
            {
                var hits = Viewport3DHelper.FindHits(Viewport.Viewport, position);
                foreach (var hit in hits)
                {
                    if (hit.Model != null && _modelTags.TryGetValue(hit.Model, out var tag))
                    {
                        ItemPicked?.Invoke(tag);
                        return;
                    }
                }
            }
            catch
            {
                // Picking is a convenience; ignore failures from a viewport that is not ready.
            }
        }
    }
}
