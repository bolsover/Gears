using System;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Gui;
using Bolsover.Gears.Model;

namespace Bolsover.Gears;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]

    public static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
      
        var gear = new Gear(GearContext.ProfileShiftedExternalInvolutePair, CalculationMode.Automatic);
        gear.Calculate();
        
        var control = new GearVariableControl(gear);
        var form = new Form();
        form.Controls.Add(control);
        Application.Run(form);
   
    }
}