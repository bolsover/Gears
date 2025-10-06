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
            this.panel1 = new System.Windows.Forms.Panel();
            this.gearsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cycloidalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.involuteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bevelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.standardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gleasonToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.miniToolStrip = new System.Windows.Forms.MenuStrip();
            this.miniToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.panel1.Location = new System.Drawing.Point(0, 24);
            this.panel1.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1682, 929);
            this.panel1.TabIndex = 4;
            // 
            // gearsToolStripMenuItem
            // 
            this.gearsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { this.cycloidalToolStripMenuItem, this.involuteToolStripMenuItem, this.bevelToolStripMenuItem });
            this.gearsToolStripMenuItem.Name = "gearsToolStripMenuItem";
            this.gearsToolStripMenuItem.Size = new System.Drawing.Size(56, 22);
            this.gearsToolStripMenuItem.Text = "Gears";
            // 
            // cycloidalToolStripMenuItem
            // 
            this.cycloidalToolStripMenuItem.Name = "cycloidalToolStripMenuItem";
            this.cycloidalToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.cycloidalToolStripMenuItem.Text = "Cycloidal";
            // 
            // involuteToolStripMenuItem
            // 
            this.involuteToolStripMenuItem.Name = "involuteToolStripMenuItem";
            this.involuteToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.involuteToolStripMenuItem.Text = "Involute";
            this.involuteToolStripMenuItem.Click += new System.EventHandler(this.involuteToolStripMenuItem_Click);
            // 
            // bevelToolStripMenuItem
            // 
            this.bevelToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { this.standardToolStripMenuItem, this.gleasonToolStripMenuItem });
            this.bevelToolStripMenuItem.Name = "bevelToolStripMenuItem";
            this.bevelToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.bevelToolStripMenuItem.Text = "Bevel";
            // 
            // standardToolStripMenuItem
            // 
            this.standardToolStripMenuItem.Name = "standardToolStripMenuItem";
            this.standardToolStripMenuItem.Size = new System.Drawing.Size(129, 22);
            this.standardToolStripMenuItem.Text = "Standard";
            // 
            // gleasonToolStripMenuItem
            // 
            this.gleasonToolStripMenuItem.Name = "gleasonToolStripMenuItem";
            this.gleasonToolStripMenuItem.Size = new System.Drawing.Size(129, 22);
            this.gleasonToolStripMenuItem.Text = "Gleason";
            // 
            // miniToolStrip
            // 
            this.miniToolStrip.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            this.miniToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.gearsToolStripMenuItem });
            this.miniToolStrip.Location = new System.Drawing.Point(0, 0);
            this.miniToolStrip.Name = "miniToolStrip";
            this.miniToolStrip.Padding = new System.Windows.Forms.Padding(4, 1, 0, 1);
            this.miniToolStrip.Size = new System.Drawing.Size(1682, 24);
            this.miniToolStrip.TabIndex = 2;
            this.miniToolStrip.Text = "menuStrip";
            // 
            // GearForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1682, 953);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.miniToolStrip);
            this.MainMenuStrip = this.miniToolStrip;
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Name = "GearForm";
            this.Text = "Involute Spur & Helical Gears";
            this.miniToolStrip.ResumeLayout(false);
            this.miniToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Panel panel1;

      

        private System.Windows.Forms.ToolStripMenuItem gearsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cycloidalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem involuteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bevelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem standardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gleasonToolStripMenuItem;

        private System.Windows.Forms.MenuStrip miniToolStrip;

        //   private BrightIdeasSoftware.OLVColumn olvColumn1;

     #endregion
    }
}