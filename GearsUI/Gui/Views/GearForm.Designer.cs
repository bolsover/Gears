using System.ComponentModel;

namespace Bolsover.Gears.Gui.Views
{
    partial class GearForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.gearView = new Bolsover.Gears.Gui.Views.GearView();
            this.olvColumn1 = new BrightIdeasSoftware.OLVColumn();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // gearView
            // 
            this.gearView.AutoSize = true;
            this.gearView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gearView.Location = new System.Drawing.Point(4, 40);
            this.gearView.Margin = new System.Windows.Forms.Padding(4);
            this.gearView.Name = "gearView";
            this.gearView.Size = new System.Drawing.Size(1626, 909);
            this.gearView.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Controls.Add(this.gearView, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 3.882476F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 96.11752F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1634, 953);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // GearForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1634, 953);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "GearForm";
            this.Text = "Involute Spur & Helical Gears";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;

        private BrightIdeasSoftware.OLVColumn olvColumn1;

        private Bolsover.Gears.Gui.Views.GearView gearView;

        #endregion
    }
}