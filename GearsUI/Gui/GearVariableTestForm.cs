using System;
using System.Linq;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Data;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Gui
{
    /// <summary>
    /// A simple test form that lets a user select a GearContext, calculates non-design variables
    /// using the Gear model, and displays the results in a GearVariableJsonViewerControl.
    /// </summary>
    public class GearVariableTestForm : Form
    {
        private readonly ComboBox _contextCombo;
        private readonly Button _loadButton;
        private readonly Label _statusLabel;
        private readonly GearVariableJsonViewerControl _designViewer;
        private readonly GearVariableJsonViewerControl _calcViewer;
        private readonly NumericUpDown _designValueEditor;
        private readonly Label _designValueLabel;
        private Gear _currentGear;

        public GearVariableTestForm()
        {
            Text = "Gear Variable Test";
            Width = 1000;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(6)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 5,
                AutoSize = true
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // label
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60)); // combo
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // button
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); // status
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // filler

            var label = new Label { Text = "Gear Context:", AutoSize = true, Anchor = AnchorStyles.Left };
            _contextCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Width = 400
            };
            _loadButton = new Button { Text = "Load", AutoSize = true, Anchor = AnchorStyles.Left };
            _statusLabel = new Label { Text = "", AutoSize = true, Anchor = AnchorStyles.Left };

            // Upper panel (Design variables)
            var designPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(3)
            };
            designPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            designPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            designPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var designLabel = new Label { Text = "Design Variables", AutoSize = true, Font = new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, 9f, System.Drawing.FontStyle.Bold) };
            _designValueLabel = new Label { Text = "Design Value:", AutoSize = true, Anchor = AnchorStyles.Left };
            _designValueEditor = new NumericUpDown { DecimalPlaces = 6, Increment = 0.1M, Minimum = -1000000M, Maximum = 1000000M, Width = 120, Anchor = AnchorStyles.Left };
            _designViewer = new GearVariableJsonViewerControl { Dock = DockStyle.Fill };
            // Row 0: Label
            designPanel.Controls.Add(designLabel, 0, 0);
            // Row 1: editor panel (label + numeric)
            var editorPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            editorPanel.Controls.Add(_designValueLabel);
            editorPanel.Controls.Add(_designValueEditor);
            designPanel.Controls.Add(editorPanel, 0, 1);
            // Row 2: grid
            designPanel.Controls.Add(_designViewer, 0, 2);

            // Lower panel (Calculated variables)
            var calcPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(3)
            };
            calcPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            calcPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var calcLabel = new Label { Text = "Calculated Variables", AutoSize = true, Font = new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, 9f, System.Drawing.FontStyle.Bold) };
            _calcViewer = new GearVariableJsonViewerControl { Dock = DockStyle.Fill };
            calcPanel.Controls.Add(calcLabel, 0, 0);
            calcPanel.Controls.Add(_calcViewer, 0, 1);

            // Content: two rows for two panels
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0)
            };
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            content.Controls.Add(designPanel, 0, 0);
            content.Controls.Add(calcPanel, 0, 1);

            header.Controls.Add(label, 0, 0);
            header.Controls.Add(_contextCombo, 1, 0);
            header.Controls.Add(_loadButton, 2, 0);
            header.Controls.Add(_statusLabel, 3, 0);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(content, 0, 1);

            Controls.Add(root);

            // Initially disable editor until selection is made
            _designValueEditor.Enabled = false;

            PopulateContexts();
            _loadButton.Click += (_, _) => LoadSelectedContext();
            _contextCombo.SelectedIndexChanged += (_, _) => ClearStatus();

            // Wire selection and editor changes
            _designViewer.SelectionChanged += (_, _) => OnDesignSelectionChanged();
            _designValueEditor.ValueChanged += (_, _) => OnDesignEditorValueChanged();
        }

        private void PopulateContexts()
        {
            // Populate with all enum names, ordered to keep common ones near the top if desired.
            var names = Enum.GetNames(typeof(GearContext)).OrderBy(n => n).ToArray();
            _contextCombo.Items.AddRange(names);

            // Select a sensible default known to exist in tests/data
            var preferred = nameof(GearContext.ExternalNormalHelical);
            var fallback = names.FirstOrDefault();
            _contextCombo.SelectedItem = _contextCombo.Items.Contains(preferred) ? preferred : fallback;
        }

        private void LoadSelectedContext()
        {
            try
            {
                if (_contextCombo.SelectedItem is not string ctxName || string.IsNullOrWhiteSpace(ctxName))
                {
                    SetStatus("Please select a context.");
                    return;
                }

                if (!Enum.TryParse<GearContext>(ctxName, out var context))
                {
                    SetStatus($"Unknown context: {ctxName}");
                    return;
                }

                // Create gear, calculate, export, and display
                _currentGear = new Gear(context, CalculationMode.Automatic);
                                var gear = _currentGear;
                gear.Calculate(); // Ensure calculated variables are up-to-date

                var json = GearVariableExporter.ToJson(gear);
                // Split JSON into design and calculated subsets and load into respective viewers
                try
                {
                    var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var contextVal = obj["context"]?.ToString();
                    var modeVal = obj["mode"]?.ToString();
                    var variables = obj["variables"] as Newtonsoft.Json.Linq.JObject;

                    var designObj = new Newtonsoft.Json.Linq.JObject
                    {
                        ["context"] = contextVal,
                        ["mode"] = modeVal,
                        ["variables"] = new Newtonsoft.Json.Linq.JObject
                        {
                            ["design"] = variables?["design"] ?? new Newtonsoft.Json.Linq.JArray(),
                            ["calculated"] = new Newtonsoft.Json.Linq.JArray()
                        }
                    };

                    var calcObj = new Newtonsoft.Json.Linq.JObject
                    {
                        ["context"] = contextVal,
                        ["mode"] = modeVal,
                        ["variables"] = new Newtonsoft.Json.Linq.JObject
                        {
                            ["design"] = new Newtonsoft.Json.Linq.JArray(),
                            ["calculated"] = variables?["calculated"] ?? new Newtonsoft.Json.Linq.JArray()
                        }
                    };

                    _designViewer.LoadJObject(designObj);
                    _calcViewer.LoadJObject(calcObj);
                }
                catch
                {
                    // Fallback: if parsing failed, load the same json into both for visibility
                    _designViewer.LoadJson(json);
                    _calcViewer.LoadJson(json);
                }

                // Reset editor state after load
                _designValueEditor.Enabled = false;
                _designValueLabel.Text = "Design Value:";

                SetStatus($"Loaded {context}.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
            }
        }

        private bool _updatingEditor;

        private void OnDesignSelectionChanged()
        {
            if (_currentGear == null)
            {
                _designValueEditor.Enabled = false;
                return;
            }

            var sel = _designViewer.GetSelected();
            if (sel == null || !string.Equals(sel.Value.category, "Design", StringComparison.OrdinalIgnoreCase))
            {
                _designValueEditor.Enabled = false;
                _designValueLabel.Text = "Design Value:";
                return;
            }

            var resolved = ResolveSelectedDesignVariable(sel.Value.name, sel.Value.ncalc);
            if (resolved == null)
            {
                _designValueEditor.Enabled = false;
                _designValueLabel.Text = "Design Value:";
                return;
            }

            var variable = resolved.Value.variable;
            _updatingEditor = true;
            try
            {
                var value = (decimal)variable.Value;
                var clamped = Math.Min(_designValueEditor.Maximum, Math.Max(_designValueEditor.Minimum, value));
                _designValueEditor.Value = clamped;
                _designValueEditor.Enabled = true;
                _designValueLabel.Text = $"Design Value: {variable.Name}";
            }
            finally
            {
                _updatingEditor = false;
            }
        }

        private void OnDesignEditorValueChanged()
        {
            if (_updatingEditor || _currentGear == null) return;
            var sel = _designViewer.GetSelected();
            if (sel == null || !string.Equals(sel.Value.category, "Design", StringComparison.OrdinalIgnoreCase)) return;
            var resolved = ResolveSelectedDesignVariable(sel.Value.name, sel.Value.ncalc);
            if (resolved == null) return;
            var variable = resolved.Value.variable;
            if (!variable.IsDesign) return;

            // Update model, which triggers recalculation via PropertyChanged wiring in Gear
            variable.Value = (double)_designValueEditor.Value;

            // Re-export and refresh the viewers to reflect new values
            RefreshViewersFromCurrentGear();
        }

        private void RefreshViewersFromCurrentGear()
        {
            if (_currentGear == null) return;
            try
            {
                // Ensure calculated variables reflect latest design values
                _currentGear.Calculate();

                var json = GearVariableExporter.ToJson(_currentGear);
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                var contextVal = obj["context"]?.ToString();
                var modeVal = obj["mode"]?.ToString();
                var variables = obj["variables"] as Newtonsoft.Json.Linq.JObject;

                var designObj = new Newtonsoft.Json.Linq.JObject
                {
                    ["context"] = contextVal,
                    ["mode"] = modeVal,
                    ["variables"] = new Newtonsoft.Json.Linq.JObject
                    {
                        ["design"] = variables?["design"] ?? new Newtonsoft.Json.Linq.JArray(),
                        ["calculated"] = new Newtonsoft.Json.Linq.JArray()
                    }
                };

                var calcObj = new Newtonsoft.Json.Linq.JObject
                {
                    ["context"] = contextVal,
                    ["mode"] = modeVal,
                    ["variables"] = new Newtonsoft.Json.Linq.JObject
                    {
                        ["design"] = new Newtonsoft.Json.Linq.JArray(),
                        ["calculated"] = variables?["calculated"] ?? new Newtonsoft.Json.Linq.JArray()
                    }
                };

                _designViewer.LoadJObject(designObj);
                _calcViewer.LoadJObject(calcObj);
            }
            catch
            {
                // If anything goes wrong, fall back to raw export in both viewers
                var json = GearVariableExporter.ToJson(_currentGear);
                _designViewer.LoadJson(json);
                _calcViewer.LoadJson(json);
            }
        }

        private (Gear owningGear, GearVariable variable)? ResolveSelectedDesignVariable(string name, string ncalc)
        {
            if (_currentGear == null) return null;

            // Prefer resolution by NCalc symbol across the gear tree
            if (!string.IsNullOrWhiteSpace(ncalc))
            {
                var match = FindByNCalc(_currentGear, ncalc);
                if (match != null && match.Value.variable.IsDesign) return match;
            }

            // Fallback: resolve by enum name and search root + children
            if (!string.IsNullOrWhiteSpace(name) && Enum.TryParse<VariableNames>(name, out var varName))
            {
                var byName = FindByName(_currentGear, varName);
                if (byName != null && byName.Value.variable.IsDesign) return byName;
            }

            return null;
        }

        private static (Gear owningGear, GearVariable variable)? FindByNCalc(Gear start, string ncalc)
        {
            if (start == null) return null;
            // Normalize: GearVariable.NCalc stores bracketed symbols; assume the incoming value is already the stored text.
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
                    var found = FindByNCalc(child, ncalc);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private static (Gear owningGear, GearVariable variable)? FindByName(Gear start, VariableNames name)
        {
            if (start == null) return null;
            var vLocal = start.GetVariable(name);
            if (vLocal != null) return (start, vLocal);
            if (start.Gears != null)
            {
                foreach (var child in start.Gears)
                {
                    var found = FindByName(child, name);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private void SetStatus(string message)
        {
            _statusLabel.Text = message;
        }

        private void ClearStatus()
        {
            _statusLabel.Text = string.Empty;
        }
    }
}
