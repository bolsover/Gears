using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Bolsover.Gears.Gui;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui
{
    /// <summary>
    /// A WinForms user control that displays gear variables for a Gear model using MVP.
    /// This is the View. It mirrors the look-and-feel of GearVariableJsonViewerControl but does not use JSON.
    /// </summary>
    public class GearVariableViewer : UserControl, IGearVariableViewerView
    {
        private readonly DataGridView _designGrid;
        private readonly DataGridView _calcGrid;
        private readonly Label _headerLabel;
        private readonly ComboBox _contextCombo;
        private readonly GearVariableViewerPresenter _presenter;

        public event EventHandler SelectionChanged;

        public GearVariableViewer()
        {
            Dock = DockStyle.Fill;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(6),
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Header panel includes caption and context selector
            var headerPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                AutoSize = true
            };
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _headerLabel = new Label
            {
                Text = "Gear GearVariables",
                AutoSize = true,
                Font = new Font(FontFamily.GenericSansSerif, 10f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 6, 6)
            };
            var ctxLabel = new Label
                { Text = "Context:", AutoSize = true, Margin = new Padding(12, 2, 6, 2), Anchor = AnchorStyles.Left };
            _contextCombo = new ComboBox
                { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280, Anchor = AnchorStyles.Left };
            headerPanel.Controls.Add(_headerLabel, 0, 0);
            headerPanel.Controls.Add(ctxLabel, 1, 0);
            headerPanel.Controls.Add(_contextCombo, 2, 0);

            // Create two panels: design (top) and calculated (bottom)
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0)
            };
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            // Design panel
            var designPanel = new TableLayoutPanel
                { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(3) };
            designPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            designPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var designLabel = new Label
            {
                Text = "Design Variables", AutoSize = true,
                Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold)
            };
            _designGrid = CreateGrid(forDesign: true);
            designPanel.Controls.Add(designLabel, 0, 0);
            designPanel.Controls.Add(_designGrid, 0, 1);

            // Calculated panel
            var calcPanel = new TableLayoutPanel
                { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(3) };
            calcPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            calcPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var calcLabel = new Label
            {
                Text = "Calculated Variables", AutoSize = true,
                Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold)
            };
            _calcGrid = CreateGrid();
            calcPanel.Controls.Add(calcLabel, 0, 0);
            calcPanel.Controls.Add(_calcGrid, 0, 1);

            content.Controls.Add(designPanel, 0, 0);
            content.Controls.Add(calcPanel, 0, 1);

            layout.Controls.Add(headerPanel, 0, 0);
            layout.Controls.Add(content, 0, 1);
            Controls.Add(layout);
            _presenter = new GearVariableViewerPresenter(this);
            
           // _designGrid.SelectionChanged += (_, _) => SelectionChanged?.Invoke(this, EventArgs.Empty);
           _designGrid.SelectionChanged += (_, _) => _designGrid.ClearSelection();
            
          // _calcGrid.SelectionChanged += (_, _) => SelectionChanged?.Invoke(this, EventArgs.Empty);
           _calcGrid.SelectionChanged += (_, _) => _calcGrid.ClearSelection();

            _contextCombo.SelectedIndexChanged += (_, _) => OnContextChanged();

            // Overlay NumericUpDowns in the design Value column (index 6)
            _designGrid.Scroll += (_, _) => LayoutDesignNumericEditors();
            _designGrid.Resize += (_, _) => LayoutDesignNumericEditors();
            _designGrid.RowHeightChanged += (_, _) => LayoutDesignNumericEditors();
            _designGrid.RowsAdded += (_, _) => LayoutDesignNumericEditors();
            _designGrid.RowsRemoved += (_, _) => LayoutDesignNumericEditors();

            // Relayout after data refill
            this.HandleCreated += (_, _) => LayoutDesignNumericEditors();
        }

        private void OnContextChanged()
        {
            if (_contextCombo.SelectedItem is not string name) return;
            if (!Enum.TryParse<GearContext>(name, out var context)) return;
            // When context changes, rebuild the Gear with same or default mode
            var mode = _currentMode ?? Bolsover.Gears.Calculator.CalculationMode.Automatic;
            var gear = new Gear(context, mode);
            gear.Calculate();
            SetGear(gear);
            
        }

        private Bolsover.Gears.Calculator.CalculationMode? _currentMode;

        // IGearVariableViewerView
        public string Caption
        {
            get => _headerLabel.Text;
            set => _headerLabel.Text = value;
        }

        private static DataGridView CreateGrid(bool forDesign = false)
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn
                { HeaderText = "Category", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn
                { HeaderText = "Ordinal", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn
                { HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
            grid.Columns.Add(new DataGridViewImageColumn
            {
                HeaderText = "Symbol", ImageLayout = DataGridViewImageCellLayout.Normal,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, Name = "SymbolImage", ReadOnly = true
            });
            grid.Columns.Add(new DataGridViewImageColumn
            {
                HeaderText = "Formula", ImageLayout = DataGridViewImageCellLayout.Normal,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, Name = "FormulaImage", ReadOnly = true
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
                { HeaderText = "NCalc", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
            if (forDesign)
            {
                // Use a plain text column to hold the numeric value, we will overlay always-visible NumericUpDowns.
                grid.Columns.Add(new DataGridViewTextBoxColumn
                    { HeaderText = "Value", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            }
            else
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Value", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "F4" }, ReadOnly = true
                });
            }
            


            grid.RowTemplate.Height = 28;
            return grid;
        }

        private readonly List<NumericUpDown> _designEditors = new List<NumericUpDown>();

        private void LayoutDesignNumericEditors()
        {
            // Clear existing editors
            foreach (var ctl in _designEditors)
            {
                ctl.ValueChanged -= DesignEditorOnValueChanged;
                _designGrid.Controls.Remove(ctl);
                ctl.Dispose();
            }

            _designEditors.Clear();

            // Create editors for visible rows only
            for (int i = 0; i < _designGrid.Rows.Count; i++)
            {
                var row = _designGrid.Rows[i];
                if (row.IsNewRow) continue;
                var cell = row.Cells.Count > 6 ? row.Cells[6] : null;
                if (cell == null) continue;
                var rect = _designGrid.GetCellDisplayRectangle(6, i, true);
                if (rect.Height <= 0 || rect.Width <= 0) continue;

                var nud = new NumericUpDown
                {
                    DecimalPlaces = 4,
                    Increment = 0.1M,
                    Minimum = -1000000M,
                    Maximum = 1000000M,
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = i,
                    Height = 35
                };
                // initialize from cell value
                var valObj = cell.Value;
                decimal dec = 0m;
                if (valObj is decimal d0) dec = d0;
                else if (valObj is double d1) dec = (decimal)d1;
                else if (valObj is float f) dec = (decimal)f;
                else if (valObj is int ii) dec = ii;
                else if (valObj != null && decimal.TryParse(valObj.ToString(), out var parsed)) dec = parsed;
                nud.Value = Math.Min(nud.Maximum, Math.Max(nud.Minimum, dec));

                nud.Bounds = rect;
                nud.ValueChanged += DesignEditorOnValueChanged;
                _designGrid.Controls.Add(nud);
                _designEditors.Add(nud);
            }
        }

        private void DesignEditorOnValueChanged(object? sender, EventArgs e)
        {
            if (sender is not NumericUpDown nud) return;
            if (nud.Tag is not int rowIndex) return;
            if (rowIndex < 0 || rowIndex >= _designGrid.Rows.Count) return;
            // write back to cell value
            var row = _designGrid.Rows[rowIndex];
            if (row.Cells.Count <= 6) return;
            row.Cells[6].Value = (double)nud.Value;
            // trigger model update via existing handler
            var args = new DataGridViewCellEventArgs(6, rowIndex);
            DesignGridOnCellValueChanged(_designGrid, args);
        }

        private static void FillGrid(DataGridView grid, IList<GearVariableViewerPresenter.RowModel> rows)
        {
            grid.DataSource = null;
            grid.Rows.Clear();
            grid.RowTemplate.Height = 35;
            if (rows == null) return;
            foreach (var r in rows)
            {
                var symbolImage = string.IsNullOrWhiteSpace(r.Symbol)
                    ? null
                    : Bolsover.Gears.Calculator.LaTeXUtils.CreateImageFromLatex(r.Symbol);
                var formulaImage = string.IsNullOrWhiteSpace(r.Formula)
                    ? null
                    : Bolsover.Gears.Calculator.LaTeXUtils.CreateImageFromLatex(r.Formula);
                var idx = grid.Rows.Add();
                var row = grid.Rows[idx];
                row.Cells[0].Value = r.Category;
                row.Cells[1].Value = r.Ordinal;
                row.Cells[2].Value = r.Name;
                row.Cells[3].Value = symbolImage;
                row.Cells[4].Value = formulaImage;
                row.Cells[5].Value = r.NCalc;
                row.Cells[6].Value = r.Value;
                row.Cells[3].ToolTipText = r.Symbol;
                row.Cells[4].ToolTipText = r.Formula;
            }
            grid.ClearSelection();
        }

        public void SetDesignRows(IList<GearVariableViewerPresenter.RowModel> rows)
        {
            FillGrid(_designGrid, rows);
            LayoutDesignNumericEditors();
        }

        public void SetCalculatedRows(IList<GearVariableViewerPresenter.RowModel> rows)
        {
            FillGrid(_calcGrid, rows);
        }

        public void PopulateContexts(string[] contextNames, string selected)
        {
            _contextCombo.BeginUpdate();
            try
            {
                _contextCombo.Items.Clear();
                if (contextNames != null && contextNames.Length > 0)
                {
                    _contextCombo.Items.AddRange(contextNames);
                }

                if (!string.IsNullOrWhiteSpace(selected) && _contextCombo.Items.Contains(selected))
                {
                    _contextCombo.SelectedItem = selected;
                }
                else if (_contextCombo.Items.Count > 0)
                {
                    _contextCombo.SelectedIndex = 0;
                }
            }
            finally
            {
                _contextCombo.EndUpdate();
            }
        }

        private Gear _currentGear;

        public void SetGear(Gear gear)
        {
            _currentGear = gear;
            _currentMode = gear?.Mode;
            _presenter.SetGear(gear);
        }

        private void DesignGridOnCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // Value column index is 6
            if (e.RowIndex < 0 || e.ColumnIndex != 6) return;
            if (_currentGear == null) return;
            var row = _designGrid.Rows[e.RowIndex];
            var ncalc = row.Cells[5].Value?.ToString();
            var valObj = row.Cells[6].Value;
            if (valObj == null) return;
            double newVal;
            if (valObj is decimal dec) newVal = (double)dec;
            else if (valObj is double d) newVal = d;
            else if (double.TryParse(valObj.ToString(), out var parsed)) newVal = parsed;
            else return;

            var resolved = ResolveByNCalc(_currentGear, ncalc);
            if (resolved == null || !resolved.Value.variable.IsDesign) return;
            resolved.Value.variable.Value = newVal;
            // Recalculate and refresh
            _currentGear.Calculate();
             _presenter.SetGear(_currentGear);
        }

        private static (Gear owningGear, GearVariable variable)? ResolveByNCalc(Gear start, string ncalc)
        {
            if (start == null || string.IsNullOrWhiteSpace(ncalc)) return null;
            if (start.GearVariables != null)
            {
                foreach (var v in start.GearVariables.Values)
                {
                    if (string.Equals(v.NCalc, ncalc, StringComparison.Ordinal))
                    {
                        return (start, v);
                    }
                }
            }

            if (start.Gears != null)
            {
                foreach (var child in start.Gears)
                {
                    var found = ResolveByNCalc(child, ncalc);
                    if (found != null) return found;
                }
            }

            return null;
        }
    }
}