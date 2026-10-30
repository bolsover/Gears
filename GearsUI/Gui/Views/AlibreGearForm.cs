using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>
    /// Interactive front-end for designing and visualising external and internal involute gears.
    /// Implements IAlibreGearView to provide UI interaction and rendering through the AlibreGearPresenter MVP strategy.
    /// </summary>
    public partial class AlibreGearForm : Form, IAlibreGearView
    {
        private readonly Dictionary<string, Image> _latexImageCache = new();

        public AlibreGearPresenter Presenter { get; }

        #region IAlibreGearView Parameter Inputs

        public int Teeth
        {
            get => (int)numTeeth.Value;
            set => numTeeth.Value = Math.Max(numTeeth.Minimum, Math.Min(numTeeth.Maximum, value));
        }

        public double Module
        {
            get => (double)numModule.Value;
            set => numModule.Value = Math.Max(numModule.Minimum, Math.Min(numModule.Maximum, (decimal)value));
        }

        public double PressureAngle
        {
            get => (double)numPressureAngle.Value;
            set => numPressureAngle.Value = Math.Max(numPressureAngle.Minimum, Math.Min(numPressureAngle.Maximum, (decimal)value));
        }

        public double HelixAngle
        {
            get => (double)numHelixAngle.Value;
            set => numHelixAngle.Value = Math.Max(numHelixAngle.Minimum, Math.Min(numHelixAngle.Maximum, (decimal)value));
        }

        public string HelicalDirection
        {
            get => cboHelicalDirection.SelectedItem?.ToString().StartsWith("L", StringComparison.OrdinalIgnoreCase) == true ? "L" : "R";
            set => cboHelicalDirection.SelectedIndex = string.Equals(value, "L", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }

        public double Clearance
        {
            get => (double)numClearance.Value;
            set => numClearance.Value = Math.Max(numClearance.Minimum, Math.Min(numClearance.Maximum, (decimal)value));
        }

        public double Backlash
        {
            get => (double)numBacklash.Value;
            set => numBacklash.Value = Math.Max(numBacklash.Minimum, Math.Min(numBacklash.Maximum, (decimal)value));
        }

        public double FaceWidth
        {
            get => (double)numFaceWidth.Value;
            set => numFaceWidth.Value = Math.Max(numFaceWidth.Minimum, Math.Min(numFaceWidth.Maximum, (decimal)value));
        }

        public double ProfileShift
        {
            get => (double)numProfileShift.Value;
            set => numProfileShift.Value = Math.Max(numProfileShift.Minimum, Math.Min(numProfileShift.Maximum, (decimal)value));
        }

        public bool OptimalShift
        {
            get => chkOptimalShift.Checked;
            set => chkOptimalShift.Checked = value;
        }

        public bool IsInternal
        {
            get => chkInternal.Checked;
            set => chkInternal.Checked = value;
        }

        public double RimThickness
        {
            get => (double)numRimThickness.Value;
            set => numRimThickness.Value = Math.Max(numRimThickness.Minimum, Math.Min(numRimThickness.Maximum, (decimal)value));
        }

        public bool ShowFullGear
        {
            get => chkFullGear.Checked;
            set => chkFullGear.Checked = value;
        }

        public bool ShowReferenceCircles
        {
            get => menuShowReferenceCircles.Checked;
            set => menuShowReferenceCircles.Checked = value;
        }

        #endregion

        #region IAlibreGearView UI Control States

        public bool RimThicknessEnabled
        {
            set
            {
                numRimThickness.Enabled = value;
                lblRimThickness.Enabled = value;
            }
        }

        public string TableTitle
        {
            set => lblTableTitle.Text = value;
        }

        public string AddMeshedGearMenuText
        {
            set => menuAddMeshedGear.Text = value;
        }

        public bool RemoveMeshedGearMenuEnabled
        {
            set => menuRemoveMeshedGear.Enabled = value;
        }

        #endregion

        #region IAlibreGearView Events

        public event EventHandler CalculateRequested;
        public event EventHandler ModuleChanged;
        public event EventHandler InternalChanged;
        public event EventHandler AddOrEditMeshedGearRequested;
        public event EventHandler RemoveMeshedGearRequested;
        public event EventHandler<ExportFormat> ExportRequested;
        public event EventHandler<VisualisationModeEventArgs> VisualisationModeChanged;
        public event EventHandler ReferenceCirclesChanged;
        public event EventHandler ResetViewRequested;

        #endregion

        public AlibreGearForm()
        {
            InitializeComponent();
            try
            {
                Icon = Globals.Icon;
            }
            catch
            {
                // Resources may be unavailable outside an install; keep default icon.
            }

            if (cboHelicalDirection.SelectedIndex < 0)
            {
                cboHelicalDirection.SelectedIndex = 0; // Default "R (Right)"
            }

            // Parameter change listeners
            numTeeth.ValueChanged += (s, e) => CalculateRequested?.Invoke(this, EventArgs.Empty);
            numModule.ValueChanged += (s, e) => ModuleChanged?.Invoke(this, EventArgs.Empty);
            numPressureAngle.ValueChanged += (s, e) => CalculateRequested?.Invoke(this, EventArgs.Empty);
            numHelixAngle.ValueChanged += (s, e) => CalculateRequested?.Invoke(this, EventArgs.Empty);
            cboHelicalDirection.SelectedIndexChanged += (s, e) => CalculateRequested?.Invoke(this, EventArgs.Empty);
            numClearance.ValueChanged += (s, e) => CalculateRequested?.Invoke(this, EventArgs.Empty);
            numBacklash.ValueChanged += (s, e) => CalculateRequested?.Invoke(this, EventArgs.Empty);
            numFaceWidth.ValueChanged += (s, e) => CalculateRequested?.Invoke(this, EventArgs.Empty);
            numProfileShift.ValueChanged += (s, e) => CalculateRequested?.Invoke(this, EventArgs.Empty);
            numRimThickness.ValueChanged += (s, e) => CalculateRequested?.Invoke(this, EventArgs.Empty);
            chkOptimalShift.CheckedChanged += (s, e) => CalculateRequested?.Invoke(this, EventArgs.Empty);
            chkFullGear.CheckedChanged += (s, e) => CalculateRequested?.Invoke(this, EventArgs.Empty);
            chkInternal.CheckedChanged += (s, e) => InternalChanged?.Invoke(this, EventArgs.Empty);

            // Menu File options
            menuExportDxf.Click += (s, e) => ExportRequested?.Invoke(this, ExportFormat.Dxf);
            menuExportCsv.Click += (s, e) => ExportRequested?.Invoke(this, ExportFormat.Csv);
            menuExit.Click += (s, e) => Close();

            // Menu Gear options
            menuAddMeshedGear.Click += (s, e) => AddOrEditMeshedGearRequested?.Invoke(this, EventArgs.Empty);
            menuRemoveMeshedGear.Click += (s, e) => RemoveMeshedGearRequested?.Invoke(this, EventArgs.Empty);
            menuCalculate.Click += (s, e) => CalculateRequested?.Invoke(this, EventArgs.Empty);

            // Menu View options
            menuView2D.Click += (s, e) => VisualisationModeChanged?.Invoke(this, new VisualisationModeEventArgs(PreviewViewMode.Mode2D));
            menuView3DSolid.Click += (s, e) => VisualisationModeChanged?.Invoke(this, new VisualisationModeEventArgs(PreviewViewMode.Mode3D, Render3DMode.Solid));
            menuView3DWireframe.Click += (s, e) => VisualisationModeChanged?.Invoke(this, new VisualisationModeEventArgs(PreviewViewMode.Mode3D, Render3DMode.Wireframe));

            menuShowReferenceCircles.CheckedChanged += (s, e) =>
            {
                preview.ShowReferenceCircles = menuShowReferenceCircles.Checked;
                preview.Invalidate();
                ReferenceCirclesChanged?.Invoke(this, EventArgs.Empty);
            };
            menuResetView.Click += (s, e) => ResetViewRequested?.Invoke(this, EventArgs.Empty);

            // Buttons
            btnCalculate.Click += (s, e) => CalculateRequested?.Invoke(this, EventArgs.Empty);

            Presenter = new AlibreGearPresenter(this);
        }

        #region IAlibreGearView Methods

        public void UpdateProgress(int percentage, string message)
        {
            progressBar.Value = Math.Max(0, Math.Min(100, percentage));
            lblStatus.Text = message;
        }

        public void SetErrorMessage(string message)
        {
            lblError.Text = message;
        }

        public void UpdateResultLabels(CalculatedGearData calcData)
        {
            lblPitch.Text = "Pitch (d): " + Fmt(calcData.PitchDiameter);
            lblBase.Text = "Base (db): " + Fmt(calcData.BaseDiameter);
            lblAddendum.Text = "Tip (da): " + Fmt(calcData.TipDiameter);
            lblDedendum.Text = "Root (df): " + Fmt(calcData.RootDiameter);
            lblClearanceVal.Text = "Clearance (c): " + Fmt(calcData.Clearance);
            lblBacklashVal.Text = "Backlash (jt): " + Fmt(calcData.Backlash);
            lblHelixVal.Text = calcData.HelixAngleDeg > 1e-6
                ? $"Helix (β): {calcData.HelixAngleDeg.ToString("0.##")}° ({calcData.HelicalDirection})"
                : "Helix (β): Spur (0°)";
        }

        public void UpdateResultLabelsForPair(CalculatedGearPairData pairData)
        {
            lblPitch.Text = $"Pitch: d₁={Fmt(pairData.Gear1.PitchDiameter)}, d₂={Fmt(pairData.Gear2.PitchDiameter)}";
            lblBase.Text = $"Base: db₁={Fmt(pairData.Gear1.BaseDiameter)}, db₂={Fmt(pairData.Gear2.BaseDiameter)}";
            lblAddendum.Text = $"Tip: da₁={Fmt(pairData.Gear1.TipDiameter)}, da₂={Fmt(pairData.Gear2.TipDiameter)}";
            lblDedendum.Text = $"Centre Dist (aw): {Fmt(pairData.WorkingCentreDistance)}";
            lblClearanceVal.Text = $"Clearance: c₁={Fmt(pairData.Gear1Clearance)}, c₂={Fmt(pairData.Gear2Clearance)}";
            lblBacklashVal.Text = $"Backlash: jt₁={Fmt(pairData.Gear1Backlash)}, jt₂={Fmt(pairData.Gear2Backlash)}, jn={Fmt(pairData.NormalBacklash)}";
            lblHelixVal.Text = $"Helix: β₁={pairData.Gear1HelixAngle.ToString("0.##")}° ({pairData.Gear1HelicalDirection}), β₂={pairData.Gear2HelixAngle.ToString("0.##")}° ({pairData.Gear2HelicalDirection})";
        }

        public void ClearResultLabels()
        {
            lblPitch.Text = "Pitch: –";
            lblBase.Text = "Base: –";
            lblAddendum.Text = "Tip: –";
            lblDedendum.Text = "Dedendum: –";
            lblClearanceVal.Text = "Clearance: –";
            lblBacklashVal.Text = "Backlash: –";
            lblHelixVal.Text = "Helix: –";
        }

        public void PopulateParameterTable(IEnumerable<GearParameterRow> parameterRows)
        {
            gridCalculations.Rows.Clear();

            foreach (var row in parameterRows)
            {
                var symImage = GetOrRenderLatexImage(row.SymbolLatex);
                var formImage = GetOrRenderLatexImage(row.FormulaLatex);
                var valText = row.Value.ToString("0.####", CultureInfo.InvariantCulture);

                var rowIndex = gridCalculations.Rows.Add(
                    row.ParameterName,
                    symImage,
                    formImage,
                    valText,
                    row.Unit);

                // Set tooltip text with LaTeX source strings
                gridCalculations.Rows[rowIndex].Cells[1].ToolTipText = row.SymbolLatex;
                gridCalculations.Rows[rowIndex].Cells[2].ToolTipText = row.FormulaLatex;
            }
        }

        public void UpdatePreviewSingle(
            List<List<GearPoint>> polylines,
            GearMesh3DBuilder.Mesh3D mesh,
            double pitchRadius,
            double baseRadius,
            double tipRadius,
            double rootRadius)
        {
            preview.Polylines = polylines;
            preview.Mesh = mesh;
            preview.PitchRadius = pitchRadius;
            preview.BaseRadius = baseRadius;
            preview.AddendumRadius = tipRadius;
            preview.DedendumRadius = rootRadius;

            preview.HasSecondGear = false;
            preview.ShowReferenceCircles = menuShowReferenceCircles.Checked;
            UpdatePreviewViewMode();
            preview.Invalidate();
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
            preview.Polylines = polylines;
            preview.Mesh = mesh;
            preview.PitchRadius = g1Pitch;
            preview.BaseRadius = g1Base;
            preview.AddendumRadius = g1Tip;
            preview.DedendumRadius = g1Root;

            preview.HasSecondGear = true;
            preview.Gear2CenterDistance = g2CenterDist;
            preview.Gear2PitchRadius = g2Pitch;
            preview.Gear2BaseRadius = g2Base;
            preview.Gear2AddendumRadius = g2Tip;
            preview.Gear2DedendumRadius = g2Root;

            preview.ShowReferenceCircles = menuShowReferenceCircles.Checked;
            UpdatePreviewViewMode();
            preview.Invalidate();
        }

        public void ClearPreview()
        {
            preview.Polylines = new List<List<GearPoint>>();
            preview.Mesh = new GearMesh3DBuilder.Mesh3D();
            preview.HasSecondGear = false;
            preview.Invalidate();
        }

        public void SetVisualisationMode(PreviewViewMode viewMode, Render3DMode mode3D = Render3DMode.Solid)
        {
            menuView2D.Checked = (viewMode == PreviewViewMode.Mode2D);
            menuView3DSolid.Checked = (viewMode == PreviewViewMode.Mode3D && mode3D == Render3DMode.Solid);
            menuView3DWireframe.Checked = (viewMode == PreviewViewMode.Mode3D && mode3D == Render3DMode.Wireframe);

            preview.ViewMode = viewMode;
            if (viewMode == PreviewViewMode.Mode3D)
            {
                preview.Mode3D = mode3D;
            }
            preview.Invalidate();
        }

        public void ResetView()
        {
            preview.ResetView();
        }

        public bool ShowAddMeshedGearDialog(ref MeshedGearParameters meshedParams, double sharedModule, double sharedPressureAngle)
        {
            using var dialog = new AddMeshedGearDialog();
            dialog.SetValues(
                meshedParams.Teeth,
                meshedParams.ProfileShift,
                meshedParams.FaceWidth,
                meshedParams.IsInternal,
                meshedParams.RimThickness,
                meshedParams.OptimalShift,
                sharedModule,
                sharedPressureAngle,
                meshedParams.Clearance,
                meshedParams.Backlash,
                meshedParams.HelixAngle,
                meshedParams.HelicalDirection);

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                meshedParams = new MeshedGearParameters
                {
                    Teeth = dialog.Teeth,
                    HelixAngle = dialog.HelixAngle,
                    HelicalDirection = dialog.HelicalDirection,
                    Clearance = dialog.Clearance,
                    Backlash = dialog.Backlash,
                    ProfileShift = dialog.ProfileShift,
                    FaceWidth = dialog.FaceWidth,
                    IsInternal = dialog.IsInternal,
                    RimThickness = dialog.RimThickness,
                    OptimalShift = dialog.OptimalShift
                };
                return true;
            }

            return false;
        }

        public string ShowSaveFileDialog(string filter, string defaultFileName)
        {
            using var dialog = new SaveFileDialog();
            dialog.Filter = filter;
            dialog.FileName = defaultFileName;
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
        }

        public void ShowMessage(string message, string title, MessageBoxIcon icon)
        {
            MessageBox.Show(this, message, title, MessageBoxButtons.OK, icon);
        }

        /// <summary>
        /// Explicit execution helper forwarding to the presenter for backward compatibility.
        /// </summary>
        public void ExecuteCalculation()
        {
            Presenter?.ExecuteCalculation();
        }

        #endregion

        #region Private Helpers

        private void UpdatePreviewViewMode()
        {
            if (menuView3DWireframe.Checked)
            {
                preview.ViewMode = PreviewViewMode.Mode3D;
                preview.Mode3D = Render3DMode.Wireframe;
            }
            else if (menuView3DSolid.Checked)
            {
                preview.ViewMode = PreviewViewMode.Mode3D;
                preview.Mode3D = Render3DMode.Solid;
            }
            else
            {
                preview.ViewMode = PreviewViewMode.Mode2D;
            }
        }

        private Image GetOrRenderLatexImage(string latex)
        {
            if (string.IsNullOrWhiteSpace(latex)) return null;

            if (_latexImageCache.TryGetValue(latex, out var cached))
            {
                return cached;
            }

            try
            {
                if (LaTeXUtils.TryParse(latex, out _))
                {
                    var img = LaTeXUtils.CreateImageFromLatex(latex);
                    if (img != null)
                    {
                        _latexImageCache[latex] = img;
                        return img;
                    }
                }
            }
            catch
            {
                // Return null if LaTeX parser encounters an unhandled token
            }

            return null;
        }

        private static string Fmt(double value) => value.ToString("0.###", CultureInfo.InvariantCulture) + " mm";

        #endregion
    }
}
