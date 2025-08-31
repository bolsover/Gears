using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Data;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui
{
    public interface IGearVariableViewerView
    {
        // Sets caption text like existing JSON viewer
        string Caption { get; set; }
        // Replace rows displayed in the design grid
        void SetDesignRows(IList<GearVariableViewerPresenter.RowModel> rows);
        // Replace rows displayed in the calculated grid
        void SetCalculatedRows(IList<GearVariableViewerPresenter.RowModel> rows);
        // Populate context list and reflect current selection
        void PopulateContexts(string[] contextNames, string selected);
    }

    public class GearVariableViewerPresenter
    {
        private readonly IGearVariableViewerView _view;
        private Gear _gear;

        public GearVariableViewerPresenter(IGearVariableViewerView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            InitializeContexts();
        }

        public void SetGear(Gear gear)
        {
            _gear = gear ?? throw new ArgumentNullException(nameof(gear));
            _view.Caption = $"Gear GearVariables — {_gear.Context} ({_gear.Mode})";
            var rows = BuildRows(_gear);
            var design = rows.Where(r => string.Equals(r.Category, "Design", StringComparison.OrdinalIgnoreCase)).ToList();
            var calculated = rows.Where(r => string.Equals(r.Category, "Calculated", StringComparison.OrdinalIgnoreCase)).ToList();
            _view.SetDesignRows(design);
            _view.SetCalculatedRows(calculated);
        }

        public void SetContext(GearContext context)
        {
            // Preserve mode if we already have a gear; default to Automatic otherwise
            var mode = _gear?.Mode ?? Bolsover.Gears.Calculator.CalculationMode.Automatic;
            SetGear(new Gear(context, mode));
        }

        private void InitializeContexts()
        {
            // var names = Enum.GetNames(typeof(GearContext)).OrderBy(n => n).ToArray();
             var preferred = nameof(GearContext.ExternalNormalHelical);
             // var selected = names.Contains(preferred) ? preferred : names.FirstOrDefault();

            var contexts = GearVariableLoader.GetGearContexts();
            var selected = contexts.Contains(preferred) ? preferred : contexts.FirstOrDefault();
            _view.PopulateContexts(contexts.ToArray(), selected);
            // _view.PopulateContexts(names, selected);
        }

        private static IList<RowModel> BuildRows(Gear gear)
        {
            var rows = new List<RowModel>();

            // The Gear model exposes GearVariables dictionary. Each GearVariable has metadata.
            // We will split by IsDesign flag similar to exporter and create rows per variable.
            foreach (var kvp in gear.GearVariables)
            {
                var v = kvp.Value;
                var category = v.IsDesign ? "Design" : "Calculated";
                rows.Add(new RowModel
                {
                    Category = category,
                    Ordinal = v.Ordinal,
                    Name = v.GearPart == "GearSet" ? v.Name.ToString() : ($"{v.GearPart} {v.Name}"),
                    Symbol = v.Symbol,
                    Formula = v.Formula,
                    NCalc = v.NCalc,
                    Value = v.Value
                });
            }

            // If there are child gears (named sub-gears), include their variables too
            if (gear.Gears != null)
            {
                foreach (var child in gear.Gears)
                {
                    foreach (var kvp in child.GearVariables)
                    {
                        var v = kvp.Value;
                        var category = v.IsDesign ? "Design" : "Calculated";
                        rows.Add(new RowModel
                        {
                            Category = category,
                            Ordinal = v.Ordinal,
                            Name = (string.IsNullOrWhiteSpace(child.Name) ? "" : child.Name + ": ") + (v.GearPart == "GearSet" ? v.Name.ToString() : ($"{v.GearPart} {v.Name}")),
                            Symbol = v.Symbol,
                            Formula = v.Formula,
                            NCalc = v.NCalc,
                            Value = v.Value
                        });
                    }
                }
            }

            return rows
                .OrderBy(r => r.Ordinal)
                .ThenBy(r => r.Category)
                .ThenBy(r => r.Name)
                .ToList();
        }

        public class RowModel
        {
            public string Category { get; set; }
            public string Name { get; set; }
            public int? Ordinal { get; set; }
            public string Symbol { get; set; }
            public string Formula { get; set; }
            public string NCalc { get; set; }
            public double? Value { get; set; }
        }
    }
}
