using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>
    /// Event arguments for changes in visualization mode (2D / 3D Solid / 3D Wireframe).
    /// </summary>
    public class VisualisationModeEventArgs : EventArgs
    {
        public PreviewViewMode ViewMode { get; }
        public Render3DMode Mode3D { get; }

        public VisualisationModeEventArgs(PreviewViewMode viewMode, Render3DMode mode3D = Render3DMode.Solid)
        {
            ViewMode = viewMode;
            Mode3D = mode3D;
        }
    }

    /// <summary>
    /// View interface contract for the Alibre Gear design and visualisation form following MVP architecture.
    /// </summary>
    public interface IAlibreGearView
    {
        // Parameter Inputs
        int Teeth { get; set; }
        double Module { get; set; }
        double PressureAngle { get; set; }
        double HelixAngle { get; set; }
        string HelicalDirection { get; set; }
        double Clearance { get; set; }
        double Backlash { get; set; }
        double FaceWidth { get; set; }
        double ProfileShift { get; set; }
        bool OptimalShift { get; set; }
        bool IsInternal { get; set; }
        double RimThickness { get; set; }
        bool ShowFullGear { get; set; }
        bool ShowReferenceCircles { get; set; }

        // UI Control States
        bool RimThicknessEnabled { set; }
        string TableTitle { set; }
        string AddMeshedGearMenuText { set; }
        bool RemoveMeshedGearMenuEnabled { set; }

        // UI Update Methods
        void UpdateProgress(int percentage, string message);
        void SetErrorMessage(string message);
        void UpdateResultLabels(CalculatedGearData calcData);
        void UpdateResultLabelsForPair(CalculatedGearPairData pairData);
        void ClearResultLabels();
        void PopulateParameterTable(IEnumerable<GearParameterRow> parameterRows);
        void UpdatePreviewSingle(List<List<GearPoint>> polylines, GearMesh3DBuilder.Mesh3D mesh, double pitchRadius, double baseRadius, double tipRadius, double rootRadius);
        void UpdatePreviewPair(List<List<GearPoint>> polylines, GearMesh3DBuilder.Mesh3D mesh, double g1Pitch, double g1Base, double g1Tip, double g1Root, double g2CenterDist, double g2Pitch, double g2Base, double g2Tip, double g2Root);
        void ClearPreview();
        void SetVisualisationMode(PreviewViewMode viewMode, Render3DMode mode3D = Render3DMode.Solid);
        void ResetView();
        bool ShowAddMeshedGearDialog(ref MeshedGearParameters meshedParams, double sharedModule, double sharedPressureAngle);
        string ShowSaveFileDialog(string filter, string defaultFileName);
        void ShowMessage(string message, string title, MessageBoxIcon icon);

        // View Events
        event EventHandler CalculateRequested;
        event EventHandler ModuleChanged;
        event EventHandler InternalChanged;
        event EventHandler AddOrEditMeshedGearRequested;
        event EventHandler RemoveMeshedGearRequested;
        event EventHandler<ExportFormat> ExportRequested;
        event EventHandler<VisualisationModeEventArgs> VisualisationModeChanged;
        event EventHandler ReferenceCirclesChanged;
        event EventHandler ResetViewRequested;
    }
}
