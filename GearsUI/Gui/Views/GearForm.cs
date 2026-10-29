using System;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui.Views
{
    public partial class GearForm : Form
    {
        public GearForm()
        {
            InitializeComponent();
            Icon = Globals.Icon;
            involuteToolStripMenuItem_Click(this, null);
        }

      

        private void alibreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AlibreGearForm().Show();
        }

        private void involuteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            panel1.Controls.Clear();
            var gearView = new GearView();
            gearView.AutoSize = true;
            gearView.Dock = DockStyle.Fill;
            gearView.Margin = new Padding(5);
            gearView.Name = "gearView1";
            gearView.Size = new System.Drawing.Size(744, 0);
            gearView.TabIndex = 3;
            panel1.Controls.Add(gearView);
            var gear = new Gear(GearTypes.ProfileShiftedExternalNormalHelicalPair, CalculationMode.Automatic);
            gearView.Presenter.InitialiseGear(gear);
        }
    }
}