using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui;

public class GearControlProvider
{
    UserControl Get(GearContext context)
    {
        switch (context)
        {
            case GearContext.External: return new GearView();
            
            default: return new GearView();
        }
    }
}