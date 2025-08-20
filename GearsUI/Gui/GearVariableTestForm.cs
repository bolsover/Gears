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
        private readonly GearVariableJsonViewerControl _viewer;

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

            _viewer = new GearVariableJsonViewerControl { Dock = DockStyle.Fill };

            header.Controls.Add(label, 0, 0);
            header.Controls.Add(_contextCombo, 1, 0);
            header.Controls.Add(_loadButton, 2, 0);
            header.Controls.Add(_statusLabel, 3, 0);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(_viewer, 0, 1);

            Controls.Add(root);

            PopulateContexts();
            _loadButton.Click += (_, _) => LoadSelectedContext();
            _contextCombo.SelectedIndexChanged += (_, _) => ClearStatus();
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
                var gear = new Gear(context, CalculationMode.Automatic);
                gear.Calculate(); // Ensure calculated variables are up-to-date

                var json = GearVariableExporter.ToJson(gear);
                _viewer.LoadJson(json);
                SetStatus($"Loaded {context}.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
            }
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
