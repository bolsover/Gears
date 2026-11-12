using System.Collections.Generic;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>One node of the assembly tree: assembly, then shafts, then the gears mounted on each shaft.</summary>
    public sealed class TrainTreeNode
    {
        public TrainTreeNode(string text, TrainNodeKind kind, object tag)
        {
            Text = text;
            Kind = kind;
            Tag = tag;
        }

        public string Text { get; }
        public TrainNodeKind Kind { get; }

        /// <summary>The <see cref="GearAssembly"/>, <see cref="Shaft"/> or <see cref="GearComponent"/> behind the node.</summary>
        public object Tag { get; }

        /// <summary>For gear nodes: false when the gear is hidden in the 3D scene.</summary>
        public bool Visible { get; set; } = true;

        public List<TrainTreeNode> Children { get; } = new List<TrainTreeNode>();
    }
}
