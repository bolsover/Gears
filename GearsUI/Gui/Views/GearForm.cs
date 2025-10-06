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
            LoadEmptyView();
            //    involuteToolStripMenuItem_Click(this, null);
        }

        private void LoadEmptyView()
        {
            this.SuspendLayout();
            panel1.Controls.Clear();
            panel1.Controls.Add(new EmptyView());
            this.ResumeLayout(true);
        }

        private void involuteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            panel1.Controls.Clear();
            var gearView = new GearView();
            gearView.AutoSize = true;
            gearView.Dock = DockStyle.Fill;
            gearView.Margin = new Padding(5);
            gearView.Name = "gearView1";
            gearView.Size = new System.Drawing.Size(1700, 1000);
            // gearView.TabIndex = 3;
            panel1.Controls.Add(gearView);
            var gear = new Gear(GearContext.ProfileShiftedExternalNormalHelicalPair, CalculationMode.Automatic);
            gearView.Presenter.InitialiseGear(gear);
            this.ResumeLayout(true);
        }

        private void gearsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}