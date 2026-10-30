using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Bolsover.Gears.Calculator;
using HelixToolkit.Wpf;

namespace Bolsover.Gears.Gui
{
    /// <summary>
    /// Hardware-accelerated WPF 3D viewport control leveraging Helix Toolkit and DirectX.
    /// </summary>
    public class HelixGearViewport : UserControl
    {
        private readonly ModelVisual3D _modelVisual = new ModelVisual3D();
        private readonly GeometryModel3D _gearModel = new GeometryModel3D();
        private readonly LinesVisual3D _wireframeVisual = new LinesVisual3D();

        public HelixViewport3D Viewport { get; }

        public HelixGearViewport()
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

            // Setup Lights
            Viewport.Children.Add(new DefaultLights());

            // Setup Gear Model Visual (Solid Shaded)
            _modelVisual.Content = _gearModel;
            Viewport.Children.Add(_modelVisual);

            // Setup Wireframe Overlay Visual
            _wireframeVisual.Thickness = 1.0;
            _wireframeVisual.Color = System.Windows.Media.Color.FromArgb(200, 30, 65, 135);
            Viewport.Children.Add(_wireframeVisual);

            Content = Viewport;
        }

        public void SetMesh(GearMesh3DBuilder.Mesh3D mesh, System.Drawing.Color baseColor, Render3DMode renderMode)
        {
            if (mesh == null || mesh.Vertices == null || mesh.Vertices.Count == 0 || mesh.TriangleIndices == null || mesh.TriangleIndices.Count < 3)
            {
                _gearModel.Geometry = null;
                _wireframeVisual.Points = new Point3DCollection();
                return;
            }

            if (renderMode == Render3DMode.Wireframe)
            {
                _gearModel.Geometry = null;
                _wireframeVisual.Points = HelixMeshAdapter.ToWireframeLinePoints(mesh);
            }
            else
            {
                _wireframeVisual.Points = new Point3DCollection();

                var wpfMesh = HelixMeshAdapter.ToMeshGeometry3D(mesh);

                var diffuse = new DiffuseMaterial(new SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(baseColor.R, baseColor.G, baseColor.B)));

                var specular = new SpecularMaterial(
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(220, 255, 255, 255)), 40.0);

                var materialGroup = new MaterialGroup();
                materialGroup.Children.Add(diffuse);
                materialGroup.Children.Add(specular);

                _gearModel.Geometry = wpfMesh;
                _gearModel.Material = materialGroup;
                _gearModel.BackMaterial = materialGroup;
            }

            try
            {
                Viewport.ZoomExtents();
            }
            catch
            {
                // Ignore if viewport is not yet fully rendered/sized
            }
        }

        public void ResetCamera()
        {
            try
            {
                Viewport.ResetCamera();
                Viewport.ZoomExtents();
            }
            catch
            {
                // Ignore if viewport is not yet sized
            }
        }
    }
}
