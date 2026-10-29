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
        /// <param id="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.gearsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cycloidalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.involuteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bevelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.standardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gleasonToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.alibreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panel1 = new System.Windows.Forms.Panel();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.gearsToolStripMenuItem, this.alibreToolStripMenuItem });
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1634, 28);
            this.menuStrip.TabIndex = 2;
            this.menuStrip.Text = "menuStrip";
            // 
            // gearsToolStripMenuItem
            // 
            this.gearsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { this.cycloidalToolStripMenuItem, this.involuteToolStripMenuItem, this.bevelToolStripMenuItem });
            this.gearsToolStripMenuItem.Name = "gearsToolStripMenuItem";
            this.gearsToolStripMenuItem.Size = new System.Drawing.Size(58, 24);
            this.gearsToolStripMenuItem.Text = "Gears";
            // 
            // cycloidalToolStripMenuItem
            // 
            this.cycloidalToolStripMenuItem.Name = "cycloidalToolStripMenuItem";
            this.cycloidalToolStripMenuItem.Size = new System.Drawing.Size(139, 24);
            this.cycloidalToolStripMenuItem.Text = "Cycloidal";
            // 
            // involuteToolStripMenuItem
            // 
            this.involuteToolStripMenuItem.Name = "involuteToolStripMenuItem";
            this.involuteToolStripMenuItem.Size = new System.Drawing.Size(139, 24);
            this.involuteToolStripMenuItem.Text = "Involute";
            this.involuteToolStripMenuItem.Click += new System.EventHandler(this.involuteToolStripMenuItem_Click);
            // 
            // bevelToolStripMenuItem
            // 
            this.bevelToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { this.standardToolStripMenuItem, this.gleasonToolStripMenuItem });
            this.bevelToolStripMenuItem.Name = "bevelToolStripMenuItem";
            this.bevelToolStripMenuItem.Size = new System.Drawing.Size(139, 24);
            this.bevelToolStripMenuItem.Text = "Bevel";
            // 
            // standardToolStripMenuItem
            // 
            this.standardToolStripMenuItem.Name = "standardToolStripMenuItem";
            this.standardToolStripMenuItem.Size = new System.Drawing.Size(138, 24);
            this.standardToolStripMenuItem.Text = "Standard";
            // 
            // gleasonToolStripMenuItem
            // 
            this.gleasonToolStripMenuItem.Name = "gleasonToolStripMenuItem";
            this.gleasonToolStripMenuItem.Size = new System.Drawing.Size(138, 24);
            this.gleasonToolStripMenuItem.Text = "Gleason";
            //
            // alibreToolStripMenuItem
            //
            this.alibreToolStripMenuItem.Name = "alibreToolStripMenuItem";
            this.alibreToolStripMenuItem.Size = new System.Drawing.Size(140, 24);
            this.alibreToolStripMenuItem.Text = "Alibre Generator";
            this.alibreToolStripMenuItem.Click += new System.EventHandler(this.alibreToolStripMenuItem_Click);
            //
            // panel1
            // 
            this.panel1.AutoSize = true;
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.panel1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.panel1.Location = new System.Drawing.Point(0, 28);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1634, 953);
            this.panel1.TabIndex = 4;
            // 
            // GearForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1634, 953);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.menuStrip);
            this.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MainMenuStrip = this.menuStrip;
            this.Name = "GearForm";
            this.Text = "Involute Spur & Helical Gears";
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
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
        private System.Windows.Forms.ToolStripMenuItem alibreToolStripMenuItem;

        private System.Windows.Forms.MenuStrip menuStrip;

        //   private BrightIdeasSoftware.OLVColumn olvColumn1;

     #endregion
    }
}