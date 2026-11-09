using System.Drawing;
using System.Windows.Forms;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>What to do with several selected parts when they are exported to Alibre.</summary>
    public enum ExportChoice
    {
        Cancel,

        /// <summary>Export each selected part as its own new Alibre part.</summary>
        SeparateParts,

        /// <summary>Export the selected parts as one new Alibre assembly.</summary>
        Assembly
    }

    /// <summary>Asks whether the selected parts are saved as separate Alibre parts or as one assembly.</summary>
    public sealed class ExportChoiceDialog : Form
    {
        private ExportChoice _choice = ExportChoice.Cancel;

        public ExportChoiceDialog(int partCount)
        {
            Text = "Export selected to Alibre";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(440, 120);

            var message = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                Text = $"{partCount} parts are selected. Save them as separate Alibre parts, or as one assembly that holds them at their train positions?"
            };

            var separate = new Button { Text = "Save Separate Parts", AutoSize = true, Margin = new Padding(6) };
            var assembly = new Button { Text = "Save Assembly", AutoSize = true, Margin = new Padding(6) };
            var cancel = new Button { Text = "Cancel", AutoSize = true, Margin = new Padding(6), DialogResult = DialogResult.Cancel };
            separate.Click += (s, e) => Choose(ExportChoice.SeparateParts);
            assembly.Click += (s, e) => Choose(ExportChoice.Assembly);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(6)
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(assembly);
            buttons.Controls.Add(separate);

            Controls.Add(message);
            Controls.Add(buttons);
            AcceptButton = assembly;
            CancelButton = cancel;
        }

        /// <summary>Shows the dialog and returns the choice; <see cref="ExportChoice.Cancel"/> when it was dismissed.</summary>
        public static ExportChoice Ask(IWin32Window owner, int partCount)
        {
            using (var dialog = new ExportChoiceDialog(partCount))
            {
                dialog.ShowDialog(owner);
                return dialog._choice;
            }
        }

        private void Choose(ExportChoice choice)
        {
            _choice = choice;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
