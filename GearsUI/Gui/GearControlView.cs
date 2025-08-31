using System.Windows.Forms;

namespace Bolsover.Gears.Gui;

public partial class GearControlView : UserControl, IGearControlViewerView
{
    private readonly GearControlPresenter _presenter;
    
    public GearControlView()
    {
        InitializeComponent();
        _presenter = new GearControlPresenter(this);
    }

    public string Caption { get; set; }
    
    public void PopulateContexts(string[] contextNames, string selected)
    {
        contextCombo.BeginUpdate();
        try
        {
            contextCombo.Items.Clear();
            if (contextNames != null && contextNames.Length > 0)
            {
                contextCombo.Items.AddRange(contextNames);
            }

            if (!string.IsNullOrWhiteSpace(selected) && contextCombo.Items.Contains(selected))
            {
                contextCombo.SelectedItem = selected;
            }
            else if (contextCombo.Items.Count > 0)
            {
                contextCombo.SelectedIndex = 0;
            }
        }
        finally
        {
            contextCombo.EndUpdate();
        }
    }
}