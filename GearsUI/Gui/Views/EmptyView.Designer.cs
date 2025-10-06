using System.ComponentModel;

namespace Bolsover.Gears.Gui.Views;

partial class EmptyView
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
        this.SuspendLayout();
        // 
        // EmptyView
        // 
        // this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 23F);
        // this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        // this.Font = new System.Drawing.Font("Segoe UI", 10.2F);
        this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.Name = "EmptyView";
        this.Size = new System.Drawing.Size(169, 216);
        this.ResumeLayout(false);
    }

    #endregion
}