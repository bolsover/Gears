using System.ComponentModel;

namespace Bolsover.Gears.Gui;

partial class GearControlView
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

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.contextCombo = new System.Windows.Forms.ComboBox();
        this.SuspendLayout();
        // 
        // contextCombo
        // 
        this.contextCombo.FormattingEnabled = true;
        this.contextCombo.Location = new System.Drawing.Point(53, 50);
        this.contextCombo.Name = "contextCombo";
        this.contextCombo.Size = new System.Drawing.Size(352, 24);
        this.contextCombo.TabIndex = 0;
        // 
        // GearControlView
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this.contextCombo);
        this.Name = "GearControlView";
        this.Size = new System.Drawing.Size(949, 598);
        this.ResumeLayout(false);
    }

    public System.Windows.Forms.ComboBox contextCombo;

    #endregion
}