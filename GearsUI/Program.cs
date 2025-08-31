using System;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Gui;
using Bolsover.Gears.Model;


namespace Bolsover.Gears;

public class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]

    public static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        
        // var gear = new Gear(GearContext.ProfileShiftedExternalInvolutePair, CalculationMode.Automatic);
        // gear.Calculate();
        //
        // var control = new GearVariableControl(gear);
        // var form = new Form();
        //
        // form.Controls.Add(control);
       // GearVariableTestForm form = new GearVariableTestForm();
        // GearVariableForm form = new GearVariableForm();
        GearControlForm form = new GearControlForm();
        Application.Run(form);
   
    }
   
}