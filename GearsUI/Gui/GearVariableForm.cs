using System.Windows.Forms;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui
{
    /// <summary>
    /// A simple form that hosts the GearVariableViewer control.
    /// </summary>
    public class GearVariableForm : Form
    {
        private readonly GearVariableViewer _viewer;

        public GearVariableForm()
        {
            Text = "Gear Variables";
            Width = 900;
            Height = 600;
            _viewer = new GearVariableViewer
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(_viewer);
        }

        public GearVariableForm(Gear gear) : this()
        {
            SetGear(gear);
        }

        public void SetGear(Gear gear)
        {
            _viewer.SetGear(gear);
        }
    }
}
