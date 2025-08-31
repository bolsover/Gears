using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Data;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui;


public interface IGearControlViewerView
{
    // Sets caption text like existing JSON viewer
    string Caption { get; set; }
    // Replace rows displayed in the design grid
    // void SetDesignRows(IList<GearVariableViewerPresenter.RowModel> rows);
    // // Replace rows displayed in the calculated grid
    // void SetCalculatedRows(IList<GearVariableViewerPresenter.RowModel> rows);
    // // Populate context list and reflect current selection
    void PopulateContexts(string[] contextNames, string selected);
}
public class GearControlPresenter

{
    private readonly IGearControlViewerView _view;
    private Gear _gear;
    
    
    public GearControlPresenter(IGearControlViewerView view)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
         InitializeContexts();
    }
    
    private void InitializeContexts()
    {
       
        var preferred = nameof(GearContext.ExternalInvolute);
        var contexts = GearVariableLoader.GetGearContexts();
        var selected = contexts.Contains(preferred) ? preferred : contexts.FirstOrDefault();
        _view.PopulateContexts(contexts.ToArray(), selected);
      
    }
}