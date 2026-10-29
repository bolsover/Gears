using System;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Gui;
using Bolsover.Gears.Gui.Views;
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


        var form = new AlibreGearForm();
       // var form = new GearForm();
        Application.Run(form);
    }
}