using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Bolsover.Gears.Gui.Presenters;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>
    /// Lets the user choose which properties of the selected item the property grid shows, put them in order, pick the sort mode,
    /// add notes (name/value items) and save the result as the default layout.
    /// </summary>
    public sealed class PropertyLayoutDialog : Form
    {
        private readonly object _properties;
        private readonly string _key;
        private readonly PropertyLayoutSettings _settings;
        private readonly List<PropertyListItem> _items;
        private readonly CheckedListBox _list = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false, HorizontalScrollbar = true };
        private readonly ComboBox _sort = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly DataGridView _notes = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window
        };

        private bool _rebuilding;

        /// <param name="properties">The property object of the selected item; its type decides which properties are listed.</param>
        /// <param name="settings">The current layout; it is copied, not changed.</param>
        public PropertyLayoutDialog(object properties, PropertyLayoutSettings settings)
        {
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            _settings = settings.Clone();
            _key = PropertyLayoutSettings.KeyOf(properties);
            var layout = _settings.Find(_key);
            _items = PropertyLayoutResolver.Resolve(properties, layout);

            BuildLayout();
            _sort.SelectedIndex = (int)_settings.SortMode;
            if (layout != null)
            {
                foreach (var note in layout.Notes) _notes.Rows.Add(note.Name, note.Value);
            }

            RebuildList(-1);
        }

        /// <summary>The layout the user chose. Valid after the dialog closed with OK.</summary>
        public PropertyLayoutSettings Result => _settings;

        /// <summary>True when the user asked to keep the layout as the default for the next start.</summary>
        public bool SaveAsDefault { get; private set; }

        private void BuildLayout()
        {
            Text = "Choose properties - " + _key.Replace("NodeProperties", string.Empty);
            Size = new Size(620, 640);
            MinimumSize = new Size(520, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            _sort.Items.AddRange(new object[] { "By category (A-Z)", "Alphabetical (one list)", "Custom order (as listed below)" });
            _notes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNoteName", HeaderText = "Name", FillWeight = 40 });
            _notes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNoteValue", HeaderText = "Value", FillWeight = 60 });

            var sortRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
            sortRow.Controls.Add(new Label { Text = "Sort the grid:", AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
            sortRow.Controls.Add(_sort);

            var listTitle = new Label { Text = "Properties - tick the ones to show, move them or their categories to set the custom order", AutoSize = true, Margin = new Padding(0, 8, 0, 4) };

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            buttons.Controls.Add(CreateButton("Move &up", (s, e) => MoveSelected(-1)));
            buttons.Controls.Add(CreateButton("Move &down", (s, e) => MoveSelected(1)));
            buttons.Controls.Add(CreateButton("Category u&p", (s, e) => MoveSelectedCategory(-1), "Move the whole category of the selected property up."));
            buttons.Controls.Add(CreateButton("Category d&own", (s, e) => MoveSelectedCategory(1), "Move the whole category of the selected property down."));
            buttons.Controls.Add(CreateButton("Sort &A-Z", (s, e) => SortAlphabetically(), "Arrange the list by category and name and switch to custom order."));
            buttons.Controls.Add(CreateButton("Show a&ll", (s, e) => SetAllVisible(true)));
            buttons.Controls.Add(CreateButton("Show &none", (s, e) => SetAllVisible(false)));

            var listRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            listRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            listRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            listRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            listRow.Controls.Add(_list, 0, 0);
            listRow.Controls.Add(buttons, 1, 0);

            var notesTitle = new Label
            {
                Text = "Notes - type in the empty last row to add an item; select a row and press Delete to remove it",
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 4)
            };

            var btnDefault = CreateButton("Apply and save as &default", (s, e) => Accept(true), "Apply the layout and keep it as the default for the next start.");
            btnDefault.AutoSize = true;
            var btnReset = CreateButton("&Reset", (s, e) => ResetLayout(), "Show every property in its natural order and remove all notes.");
            var btnOk = CreateButton("OK", (s, e) => Accept(false));
            var btnCancel = CreateButton("Cancel", null);
            btnCancel.DialogResult = DialogResult.Cancel;

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, WrapContents = false };
            bottom.Controls.Add(btnCancel);
            bottom.Controls.Add(btnOk);
            bottom.Controls.Add(btnDefault);
            bottom.Controls.Add(btnReset);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(10) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(sortRow, 0, 0);
            root.Controls.Add(listTitle, 0, 1);
            root.Controls.Add(listRow, 0, 2);
            root.Controls.Add(notesTitle, 0, 3);
            root.Controls.Add(_notes, 0, 4);
            root.Controls.Add(bottom, 0, 5);
            Controls.Add(root);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            _list.ItemCheck += (s, e) =>
            {
                if (_rebuilding || e.Index < 0 || e.Index >= _items.Count) return;
                _items[e.Index].Visible = e.NewValue == CheckState.Checked;
            };
        }

        private static Button CreateButton(string text, EventHandler click, string toolTip = null)
        {
            var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(96, 28), Margin = new Padding(3) };
            if (click != null) button.Click += click;
            if (toolTip != null) new ToolTip().SetToolTip(button, toolTip);
            return button;
        }

        private void RebuildList(int select)
        {
            _rebuilding = true;
            try
            {
                _list.BeginUpdate();
                _list.Items.Clear();
                foreach (var item in _items) _list.Items.Add(item, item.Visible);
                if (select >= 0 && select < _items.Count) _list.SelectedIndex = select;
                _list.EndUpdate();
            }
            finally
            {
                _rebuilding = false;
            }
        }

        private void MoveSelected(int delta)
        {
            var index = _list.SelectedIndex;
            var target = index + delta;
            if (index < 0 || target < 0 || target >= _items.Count) return;

            var item = _items[index];
            _items.RemoveAt(index);
            _items.Insert(target, item);
            _sort.SelectedIndex = (int)PropertySortMode.Custom;
            RebuildList(target);
        }

        private void MoveSelectedCategory(int delta)
        {
            var index = _list.SelectedIndex;
            if (index < 0 || index >= _items.Count) return;

            var selected = _items[index];
            var moved = PropertyLayoutResolver.MoveCategory(_items, selected.Category, delta);
            if (moved == null) return;

            _items.Clear();
            _items.AddRange(moved);
            _sort.SelectedIndex = (int)PropertySortMode.Custom;
            RebuildList(_items.IndexOf(selected));
        }

        private void SortAlphabetically()
        {
            var ordered = _items
                .OrderBy(i => i.Category, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(i => i.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            _items.Clear();
            _items.AddRange(ordered);
            _sort.SelectedIndex = (int)PropertySortMode.Custom;
            RebuildList(-1);
        }

        private void SetAllVisible(bool visible)
        {
            foreach (var item in _items) item.Visible = visible;
            RebuildList(_list.SelectedIndex);
        }

        private void ResetLayout()
        {
            _items.Clear();
            _items.AddRange(PropertyLayoutResolver.Resolve(_properties, null));
            _notes.Rows.Clear();
            _sort.SelectedIndex = (int)PropertySortMode.Category;
            RebuildList(-1);
        }

        private List<PropertyNote> ReadNotes()
        {
            _notes.EndEdit();
            var notes = new List<PropertyNote>();
            foreach (DataGridViewRow row in _notes.Rows)
            {
                if (row.IsNewRow) continue;
                var name = Convert.ToString(row.Cells[0].Value)?.Trim() ?? string.Empty;
                var value = Convert.ToString(row.Cells[1].Value) ?? string.Empty;
                if (name.Length == 0 && value.Length == 0) continue;
                notes.Add(new PropertyNote { Name = name, Value = value });
            }

            return notes;
        }

        private string ValidateNotes(IList<PropertyNote> notes)
        {
            var taken = new HashSet<string>(_items.Select(i => i.DisplayName), StringComparer.CurrentCultureIgnoreCase);
            foreach (var note in notes)
            {
                if (note.Name.Length == 0) return "Every note needs a name.";
                if (!taken.Add(note.Name)) return "The name \"" + note.Name + "\" is already used by a property or another note.";
            }

            return null;
        }

        private void Accept(bool saveAsDefault)
        {
            var notes = ReadNotes();
            var problem = ValidateNotes(notes);
            if (problem != null)
            {
                MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _settings.SortMode = (PropertySortMode)Math.Max(0, _sort.SelectedIndex);
            _settings.Set(_key, PropertyLayoutResolver.ToLayout(_items, notes));
            SaveAsDefault = saveAsDefault;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
