using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>How the property grid orders the properties it shows.</summary>
    public enum PropertySortMode
    {
        /// <summary>Grouped by category, each category sorted alphabetically.</summary>
        Category,

        /// <summary>One flat list sorted alphabetically.</summary>
        Alphabetical,

        /// <summary>Grouped by category, in the order chosen by the user.</summary>
        Custom
    }

    /// <summary>One property of a node in the layout: its name and whether the grid shows it. The position in the list is the custom order.</summary>
    public sealed class PropertyLayoutEntry
    {
        public string Name { get; set; } = string.Empty;
        public bool Visible { get; set; } = true;
    }

    /// <summary>A name/value item that the user added to the property grid.</summary>
    public sealed class PropertyNote
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>The chosen properties, their order and the added notes for one kind of node (gear, rack, shaft ...).</summary>
    public sealed class TypePropertyLayout
    {
        public List<PropertyLayoutEntry> Entries { get; set; } = new List<PropertyLayoutEntry>();
        public List<PropertyNote> Notes { get; set; } = new List<PropertyNote>();

        public TypePropertyLayout Clone()
        {
            return new TypePropertyLayout
            {
                Entries = Entries.Select(e => new PropertyLayoutEntry { Name = e.Name, Visible = e.Visible }).ToList(),
                Notes = Notes.Select(n => new PropertyNote { Name = n.Name, Value = n.Value }).ToList()
            };
        }
    }

    /// <summary>The property grid layout for every kind of node, plus the sort mode.</summary>
    public sealed class PropertyLayoutSettings
    {
        public PropertySortMode SortMode { get; set; } = PropertySortMode.Category;
        public Dictionary<string, TypePropertyLayout> Types { get; set; } = new Dictionary<string, TypePropertyLayout>();

        /// <summary>The key under which the layout of a node's property object is stored: the name of its type.</summary>
        public static string KeyOf(object properties)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            return properties.GetType().Name;
        }

        /// <summary>The layout stored for <paramref name="key"/>; null when the user has not changed it.</summary>
        public TypePropertyLayout Find(string key)
        {
            return key != null && Types != null && Types.TryGetValue(key, out var layout) ? layout : null;
        }

        public void Set(string key, TypePropertyLayout layout)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            Types[key] = layout;
        }

        public PropertyLayoutSettings Clone()
        {
            var copy = new PropertyLayoutSettings { SortMode = SortMode };
            foreach (var pair in Types) copy.Types[pair.Key] = pair.Value.Clone();
            return copy;
        }
    }

    /// <summary>One property in the order the layout gives it.</summary>
    public sealed class PropertyListItem
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; }
        public bool Visible { get; set; }

        public override string ToString() => Category + "  -  " + DisplayName;
    }

    /// <summary>Combines the properties that a node object really has with a stored layout.</summary>
    public static class PropertyLayoutResolver
    {
        /// <summary>
        /// Lists every browsable property of <paramref name="properties"/>: first those named in <paramref name="layout"/>, in the stored order
        /// and with the stored visibility, then the remaining ones (for example properties added later) in their natural order and visible.
        /// Stored names that no longer exist are dropped.
        /// </summary>
        public static List<PropertyListItem> Resolve(object properties, TypePropertyLayout layout)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            var actual = TypeDescriptor.GetProperties(properties)
                .Cast<PropertyDescriptor>()
                .Where(p => p.IsBrowsable)
                .ToList();
            var byName = actual.ToDictionary(p => p.Name);

            var result = new List<PropertyListItem>();
            var used = new HashSet<string>();
            if (layout != null)
            {
                foreach (var entry in layout.Entries)
                {
                    if (entry == null || !byName.TryGetValue(entry.Name ?? string.Empty, out var descriptor) || !used.Add(entry.Name)) continue;
                    result.Add(CreateItem(descriptor, entry.Visible));
                }
            }

            foreach (var descriptor in actual)
            {
                if (used.Add(descriptor.Name)) result.Add(CreateItem(descriptor, true));
            }

            return result;
        }

        /// <summary>
        /// The categories of <paramref name="items"/> in the order in which they first appear in the list. This is the order in which the grid
        /// shows the categories when the sort mode is <see cref="PropertySortMode.Custom"/>.
        /// </summary>
        public static List<string> CategoryOrder(IEnumerable<PropertyListItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var order = new List<string>();
            foreach (var item in items)
            {
                if (!order.Contains(item.Category)) order.Add(item.Category);
            }

            return order;
        }

        /// <summary>
        /// Arranges <paramref name="items"/> so that the properties of a category are next to each other, keeping the order of the categories
        /// and the order of the properties inside each category.
        /// </summary>
        public static List<PropertyListItem> GroupByCategory(IEnumerable<PropertyListItem> items)
        {
            var list = items.ToList();
            return CategoryOrder(list).SelectMany(category => list.Where(i => i.Category == category)).ToList();
        }

        /// <summary>
        /// Moves the whole category <paramref name="category"/> of <paramref name="items"/> one place up (<paramref name="delta"/> = -1) or down (1)
        /// among the categories. Returns the new list, or null when the category cannot move that way.
        /// </summary>
        public static List<PropertyListItem> MoveCategory(IEnumerable<PropertyListItem> items, string category, int delta)
        {
            var grouped = GroupByCategory(items);
            var order = CategoryOrder(grouped);
            var index = order.IndexOf(category);
            var target = index + delta;
            if (index < 0 || target < 0 || target >= order.Count) return null;

            order.RemoveAt(index);
            order.Insert(target, category);
            return order.SelectMany(c => grouped.Where(i => i.Category == c)).ToList();
        }

        /// <summary>Stores the order and visibility of <paramref name="items"/> as the layout, keeping the notes of <paramref name="notes"/>.</summary>
        public static TypePropertyLayout ToLayout(IEnumerable<PropertyListItem> items, IEnumerable<PropertyNote> notes)
        {
            var layout = new TypePropertyLayout();
            foreach (var item in items) layout.Entries.Add(new PropertyLayoutEntry { Name = item.Name, Visible = item.Visible });
            foreach (var note in notes) layout.Notes.Add(new PropertyNote { Name = note.Name, Value = note.Value });
            return layout;
        }

        private static PropertyListItem CreateItem(PropertyDescriptor descriptor, bool visible)
        {
            return new PropertyListItem
            {
                Name = descriptor.Name,
                DisplayName = descriptor.DisplayName,
                Category = descriptor.Category,
                Visible = visible
            };
        }
    }

    /// <summary>Reads and writes the default property layout as JSON.</summary>
    public static class PropertyLayoutStore
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>%APPDATA%\Gears\property-layout.json</summary>
        public static string DefaultPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Gears", "property-layout.json");

        /// <summary>Loads the layout; a missing, empty or damaged file gives the built-in layout (everything shown, natural order, no notes).</summary>
        public static PropertyLayoutSettings Load(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var settings = JsonSerializer.Deserialize<PropertyLayoutSettings>(File.ReadAllText(path), Options);
                    if (settings != null)
                    {
                        if (settings.Types == null) settings.Types = new Dictionary<string, TypePropertyLayout>();
                        foreach (var layout in settings.Types.Values.Where(l => l != null))
                        {
                            if (layout.Entries == null) layout.Entries = new List<PropertyLayoutEntry>();
                            if (layout.Notes == null) layout.Notes = new List<PropertyNote>();
                        }

                        foreach (var key in settings.Types.Where(p => p.Value == null).Select(p => p.Key).ToList()) settings.Types.Remove(key);
                        return settings;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException || ex is NotSupportedException)
            {
                // The built-in layout is used.
            }

            return new PropertyLayoutSettings();
        }

        /// <summary>Saves the layout, creating the folder when needed.</summary>
        public static void Save(PropertyLayoutSettings settings, string path)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("A file name is required.", nameof(path));

            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(path, JsonSerializer.Serialize(settings, Options));
        }
    }

    /// <summary>
    /// What the property grid is given instead of a node's property object: only the properties the layout shows, in the layout's order,
    /// followed by the user's notes in the category <see cref="NotesCategory"/>. Reads and writes go to the original object.
    /// </summary>
    public sealed class FilteredProperties : ICustomTypeDescriptor
    {
        public const string NotesCategory = "Notes";

        private readonly object _target;
        private readonly PropertyDescriptorCollection _properties;

        /// <param name="target">The property object whose properties are shown.</param>
        /// <param name="layout">The stored layout for the type of <paramref name="target"/>; null for the natural order.</param>
        /// <param name="sortMode">
        /// With <see cref="PropertySortMode.Custom"/> the categories are given the order in which they appear in the layout; the grid itself
        /// would sort them alphabetically.
        /// </param>
        public FilteredProperties(object target, TypePropertyLayout layout, PropertySortMode sortMode = PropertySortMode.Category)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));

            var byName = TypeDescriptor.GetProperties(target).Cast<PropertyDescriptor>().ToDictionary(p => p.Name);
            var list = new List<PropertyDescriptor>();
            var items = PropertyLayoutResolver.Resolve(target, layout);
            var order = sortMode == PropertySortMode.Custom ? PropertyLayoutResolver.CategoryOrder(items) : null;
            foreach (var item in items)
            {
                if (!item.Visible) continue;
                var descriptor = byName[item.Name];
                if (order != null)
                {
                    // The grid sorts categories by name, so leading tabs (which it does not show) put them in the chosen order.
                    var prefix = new string('\t', order.Count - order.IndexOf(item.Category));
                    descriptor = TypeDescriptor.CreateProperty(target.GetType(), descriptor, new CategoryAttribute(prefix + item.Category));
                }

                list.Add(descriptor);
            }

            if (layout != null)
            {
                foreach (var note in layout.Notes.Where(n => n != null && !string.IsNullOrWhiteSpace(n.Name))) list.Add(new NoteDescriptor(note));
            }

            _properties = new PropertyDescriptorCollection(list.ToArray(), true);
        }

        /// <summary>The property object behind the filtered view.</summary>
        public object Target => _target;

        public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(_target, true);
        public string GetClassName() => TypeDescriptor.GetClassName(_target, true);
        public string GetComponentName() => TypeDescriptor.GetComponentName(_target, true);
        public TypeConverter GetConverter() => TypeDescriptor.GetConverter(_target, true);
        public EventDescriptor GetDefaultEvent() => null;
        public PropertyDescriptor GetDefaultProperty() => null;
        public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(_target, editorBaseType, true);
        public EventDescriptorCollection GetEvents(Attribute[] attributes) => EventDescriptorCollection.Empty;
        public EventDescriptorCollection GetEvents() => EventDescriptorCollection.Empty;
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes) => _properties;
        public PropertyDescriptorCollection GetProperties() => _properties;
        public object GetPropertyOwner(PropertyDescriptor pd) => _target;

        private sealed class NoteDescriptor : PropertyDescriptor
        {
            private readonly PropertyNote _note;

            public NoteDescriptor(PropertyNote note)
                : base("Note: " + note.Name, new Attribute[]
                {
                    new CategoryAttribute(NotesCategory),
                    new DisplayNameAttribute(note.Name),
                    new DescriptionAttribute("A note added by the user. Notes are kept in the default layout when it is saved.")
                })
            {
                _note = note;
            }

            public override Type ComponentType => typeof(object);
            public override bool IsReadOnly => false;
            public override Type PropertyType => typeof(string);
            public override bool CanResetValue(object component) => false;
            public override object GetValue(object component) => _note.Value;
            public override void ResetValue(object component) { }
            public override bool ShouldSerializeValue(object component) => false;

            public override void SetValue(object component, object value)
            {
                _note.Value = value as string ?? string.Empty;
                OnValueChanged(component, EventArgs.Empty);
            }
        }
    }
}
