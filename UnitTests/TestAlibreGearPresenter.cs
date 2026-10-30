using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Gui;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class TestAlibreGearPresenter
    {
        private class FakeAlibreGearView : IAlibreGearView
        {
            public int Teeth { get; set; } = 20;
            public double Module { get; set; } = 2.0;
            public double PressureAngle { get; set; } = 20.0;
            public double HelixAngle { get; set; } = 0.0;
            public string HelicalDirection { get; set; } = "R";
            public double Clearance { get; set; } = 0.5;
            public double Backlash { get; set; } = 0.0;
            public double FaceWidth { get; set; } = 15.0;
            public double ProfileShift { get; set; } = 0.0;
            public bool OptimalShift { get; set; } = false;
            public bool IsInternal { get; set; } = false;
            public double RimThickness { get; set; } = 10.0;
            public bool ShowFullGear { get; set; } = true;
            public bool ShowReferenceCircles { get; set; } = true;

            public bool RimThicknessEnabled { get; set; }
            public string TableTitle { get; set; }
            public string AddMeshedGearMenuText { get; set; }
            public bool RemoveMeshedGearMenuEnabled { get; set; }

            public int LastProgressPercentage { get; private set; }
            public string LastProgressMessage { get; private set; }
            public string LastErrorMessage { get; private set; }
            public CalculatedGearData LastCalcData { get; private set; }
            public CalculatedGearPairData LastPairData { get; private set; }
            public List<GearParameterRow> LastParameterRows { get; private set; } = new();

            public List<List<GearPoint>> LastPolylines { get; private set; }
            public GearMesh3DBuilder.Mesh3D LastMesh { get; private set; }
            public bool LastHasSecondGear { get; private set; }
            public PreviewViewMode LastViewMode { get; private set; } = PreviewViewMode.Mode2D;
            public Render3DMode LastMode3D { get; private set; } = Render3DMode.Solid;
            public bool ResetViewCalled { get; private set; }
            public string LastShownMessage { get; private set; }

            public bool DialogResultToReturn { get; set; } = true;
            public MeshedGearParameters DialogParamsToReturn { get; set; }
            public string SaveFilePathToReturn { get; set; }

            public event EventHandler CalculateRequested;
            public event EventHandler ModuleChanged;
            public event EventHandler InternalChanged;
            public event EventHandler AddOrEditMeshedGearRequested;
            public event EventHandler RemoveMeshedGearRequested;
            public event EventHandler<ExportFormat> ExportRequested;
            public event EventHandler<VisualisationModeEventArgs> VisualisationModeChanged;
            public event EventHandler ReferenceCirclesChanged;
            public event EventHandler ResetViewRequested;

            public void FireCalculate() => CalculateRequested?.Invoke(this, EventArgs.Empty);
            public void FireModuleChanged() => ModuleChanged?.Invoke(this, EventArgs.Empty);
            public void FireInternalChanged() => InternalChanged?.Invoke(this, EventArgs.Empty);
            public void FireAddOrEditMeshed() => AddOrEditMeshedGearRequested?.Invoke(this, EventArgs.Empty);
            public void FireRemoveMeshed() => RemoveMeshedGearRequested?.Invoke(this, EventArgs.Empty);
            public void FireExport(ExportFormat format) => ExportRequested?.Invoke(this, format);
            public void FireVisualisationMode(PreviewViewMode mode, Render3DMode style = Render3DMode.Solid) =>
                VisualisationModeChanged?.Invoke(this, new VisualisationModeEventArgs(mode, style));
            public void FireReferenceCirclesChanged() => ReferenceCirclesChanged?.Invoke(this, EventArgs.Empty);
            public void FireResetView() => ResetViewRequested?.Invoke(this, EventArgs.Empty);

            public void UpdateProgress(int percentage, string message)
            {
                LastProgressPercentage = percentage;
                LastProgressMessage = message;
            }

            public void SetErrorMessage(string message)
            {
                LastErrorMessage = message;
            }

            public void UpdateResultLabels(CalculatedGearData calcData)
            {
                LastCalcData = calcData;
            }

            public void UpdateResultLabelsForPair(CalculatedGearPairData pairData)
            {
                LastPairData = pairData;
            }

            public void ClearResultLabels()
            {
                LastCalcData = null;
                LastPairData = null;
            }

            public void PopulateParameterTable(IEnumerable<GearParameterRow> parameterRows)
            {
                LastParameterRows = parameterRows?.ToList() ?? new List<GearParameterRow>();
            }

            public void UpdatePreviewSingle(
                List<List<GearPoint>> polylines,
                GearMesh3DBuilder.Mesh3D mesh,
                double pitchRadius,
                double baseRadius,
                double tipRadius,
                double rootRadius)
            {
                LastPolylines = polylines;
                LastMesh = mesh;
                LastHasSecondGear = false;
            }

            public void UpdatePreviewPair(
                List<List<GearPoint>> polylines,
                GearMesh3DBuilder.Mesh3D mesh,
                double g1Pitch,
                double g1Base,
                double g1Tip,
                double g1Root,
                double g2CenterDist,
                double g2Pitch,
                double g2Base,
                double g2Tip,
                double g2Root)
            {
                LastPolylines = polylines;
                LastMesh = mesh;
                LastHasSecondGear = true;
            }

            public void ClearPreview()
            {
                LastPolylines = null;
                LastMesh = null;
                LastHasSecondGear = false;
            }

            public void SetVisualisationMode(PreviewViewMode viewMode, Render3DMode mode3D = Render3DMode.Solid)
            {
                LastViewMode = viewMode;
                LastMode3D = mode3D;
            }

            public void ResetView()
            {
                ResetViewCalled = true;
            }

            public bool ShowAddMeshedGearDialog(ref MeshedGearParameters meshedParams, double sharedModule, double sharedPressureAngle)
            {
                if (DialogResultToReturn && DialogParamsToReturn != null)
                {
                    meshedParams = DialogParamsToReturn;
                    return true;
                }
                return DialogResultToReturn;
            }

            public string ShowSaveFileDialog(string filter, string defaultFileName)
            {
                return SaveFilePathToReturn;
            }

            public void ShowMessage(string message, string title, MessageBoxIcon icon)
            {
                LastShownMessage = message;
            }
        }

        [Test]
        public void TestPresenter_Initialization_CalculatesSingleGearSuccessfully()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 30,
                Module = 2.5,
                PressureAngle = 20.0
            };

            var presenter = new AlibreGearPresenter(fakeView);

            Assert.IsFalse(presenter.HasMeshedGear);
            Assert.AreEqual(100, fakeView.LastProgressPercentage);
            Assert.IsNotNull(fakeView.LastCalcData);
            Assert.AreEqual(75.0, fakeView.LastCalcData.PitchDiameter, 1e-4);
            Assert.IsNotNull(fakeView.LastPolylines);
            Assert.Greater(fakeView.LastPolylines.Count, 0);
            Assert.IsNotNull(fakeView.LastMesh);
            Assert.Greater(fakeView.LastMesh.Vertices.Count, 0);
            Assert.Greater(fakeView.LastMesh.TriangleIndices.Count, 0);
            Assert.Greater(fakeView.LastParameterRows.Count, 0);
            Assert.IsFalse(fakeView.LastHasSecondGear);
        }

        [Test]
        public void TestPresenter_ModuleChanged_UpdatesClearanceAndRecalculates()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 20,
                Module = 4.0
            };

            var presenter = new AlibreGearPresenter(fakeView);
            fakeView.Module = 4.0;
            fakeView.FireModuleChanged();

            Assert.AreEqual(1.0, fakeView.Clearance, 1e-4); // 0.25 * 4.0 = 1.0
            Assert.AreEqual(80.0, fakeView.LastCalcData.PitchDiameter, 1e-4);
        }

        [Test]
        public void TestPresenter_InternalChanged_UpdatesRimThicknessAndRecalculates()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 40,
                Module = 2.0,
                IsInternal = true,
                RimThickness = 12.0
            };

            var presenter = new AlibreGearPresenter(fakeView);
            fakeView.FireInternalChanged();

            Assert.IsTrue(fakeView.RimThicknessEnabled);
            Assert.AreEqual(100, fakeView.LastProgressPercentage);
            Assert.IsNotNull(fakeView.LastCalcData);
            Assert.IsTrue(fakeView.LastCalcData.IsInternal);
        }

        [Test]
        public void TestPresenter_AddAndRemoveMeshedGear_Workflow()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 20,
                Module = 2.0,
                PressureAngle = 20.0,
                DialogResultToReturn = true,
                DialogParamsToReturn = new MeshedGearParameters
                {
                    Teeth = 40,
                    ProfileShift = 0.2,
                    FaceWidth = 15.0,
                    IsInternal = false,
                    Clearance = 0.5,
                    Backlash = 0.1,
                    HelixAngle = 15.0,
                    HelicalDirection = "L"
                }
            };

            var presenter = new AlibreGearPresenter(fakeView);

            // Add Meshed Gear
            fakeView.FireAddOrEditMeshed();

            Assert.IsTrue(presenter.HasMeshedGear);
            Assert.AreEqual("Edit Meshed Gear…", fakeView.AddMeshedGearMenuText);
            Assert.IsTrue(fakeView.RemoveMeshedGearMenuEnabled);
            Assert.IsTrue(fakeView.LastHasSecondGear);
            Assert.IsNotNull(fakeView.LastPairData);
            Assert.AreEqual(20, fakeView.LastPairData.Gear1.Teeth);
            Assert.AreEqual(40, fakeView.LastPairData.Gear2.Teeth);

            // Remove Meshed Gear
            fakeView.FireRemoveMeshed();

            Assert.IsFalse(presenter.HasMeshedGear);
            Assert.AreEqual("Add Meshed Gear…", fakeView.AddMeshedGearMenuText);
            Assert.IsFalse(fakeView.RemoveMeshedGearMenuEnabled);
            Assert.IsFalse(fakeView.LastHasSecondGear);
            Assert.IsNotNull(fakeView.LastCalcData);
        }

        [Test]
        public void TestPresenter_SetVisualisationMode()
        {
            var fakeView = new FakeAlibreGearView();
            var presenter = new AlibreGearPresenter(fakeView);

            fakeView.FireVisualisationMode(PreviewViewMode.Mode3D, Render3DMode.Wireframe);
            Assert.AreEqual(PreviewViewMode.Mode3D, fakeView.LastViewMode);
            Assert.AreEqual(Render3DMode.Wireframe, fakeView.LastMode3D);

            fakeView.FireReferenceCirclesChanged();

            fakeView.FireResetView();
            Assert.IsTrue(fakeView.ResetViewCalled);
        }

        [Test]
        public void TestPresenter_ExportDxfAndCsv_GeneratesValidContent()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 18,
                Module = 2.0
            };

            var presenter = new AlibreGearPresenter(fakeView);

            var dxf = AlibreGearPresenter.BuildDxf(presenter.CurrentPolylines);
            Assert.IsNotEmpty(dxf);
            Assert.IsTrue(dxf.Contains("SECTION"));
            Assert.IsTrue(dxf.Contains("LWPOLYLINE"));
            Assert.IsTrue(dxf.Contains("EOF"));

            var csv = AlibreGearPresenter.BuildCsv(presenter.CurrentPolylines);
            Assert.IsNotEmpty(csv);
            Assert.IsTrue(csv.StartsWith("segment,point,x,y"));

            // Test export action with fake save path
            var tempFile = Path.Combine(Path.GetTempPath(), "test_gear_export.dxf");
            try
            {
                fakeView.SaveFilePathToReturn = tempFile;
                fakeView.FireExport(ExportFormat.Dxf);
                Assert.IsTrue(File.Exists(tempFile));
                Assert.Greater(new FileInfo(tempFile).Length, 0);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Test]
        public void TestPresenter_ErrorHandling_ClearsPreviewAndReportsError()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 10,
                Module = 1.0,
                ProfileShift = -50.0 // Extreme negative profile shift triggers baseRadius > pitchRadius exception
            };

            var presenter = new AlibreGearPresenter(fakeView);

            Assert.AreEqual(0, fakeView.LastProgressPercentage);
            Assert.IsNotEmpty(fakeView.LastErrorMessage);
            Assert.IsNull(fakeView.LastPolylines);
        }
    }
}
