using System.Collections.Generic;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>
    /// View contract for the gear train editor: assembly tree, 3D scene, property editor and ratio/speed report.
    /// </summary>
    public interface IGearTrainView
    {
        /// <summary>Speed of the input shaft in rpm.</summary>
        double InputSpeedRpm { get; }

        /// <summary>Shows the assembly tree and selects the node whose tag is <paramref name="selectedTag"/>.</summary>
        void ShowTree(TrainTreeNode root, object selectedTag);

        /// <summary>Selects the tree node whose tag is <paramref name="selectedTag"/> without rebuilding the tree.</summary>
        void ShowSelection(object selectedTag);

        /// <summary>Shows the editable properties of the selected node; null clears the editor.</summary>
        void ShowProperties(object properties);

        /// <summary>Shows the 3D scene; the selected gear or shaft is highlighted and hidden gears are skipped.</summary>
        void ShowScene(AssemblyScene scene, object selectedTag, ISet<GearComponent> hiddenGears);

        /// <summary>Shows the ratio and speed report; null clears it.</summary>
        void ShowReport(GearTrainReport report);

        /// <summary>Shows a status, warning or error message.</summary>
        void ShowMessage(string message, bool isError);
    }
}
