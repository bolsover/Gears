using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// The property grid layout of the gear train editor: which properties are shown, in which order, the user's notes
    /// and the default layout that is kept as JSON.
    /// </summary>
    [TestFixture]
    public class TestPropertyLayout
    {
        private sealed class FakeView : IGearTrainView
        {
            public double InputSpeedRpm => 1000.0;
            public void ShowTree(TrainTreeNode root, object selectedTag) { }
            public void ShowSelection(object selectedTag) { }
            public void ShowProperties(object properties) { }
            public void ShowScene(AssemblyScene scene, object selectedTag, ISet<GearComponent> hiddenGears) { }
            public void ShowReport(GearTrainReport report) { }
            public void ShowMessage(string message, bool isError) { }
        }

        private Shaft _shaft;
        private ShaftNodeProperties _properties;
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _shaft = new Shaft("S1");
            _properties = new ShaftNodeProperties(new GearTrainPresenter(new FakeView()), _shaft);
            _folder = Path.Combine(Path.GetTempPath(), "GearsPropertyLayoutTests_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        private static string[] Names(FilteredProperties filtered)
        {
            return filtered.GetProperties().Cast<PropertyDescriptor>().Select(p => p.DisplayName).ToArray();
        }

        [Test]
        public void TestResolve_WithoutLayout_ListsEveryPropertyVisible()
        {
            var items = PropertyLayoutResolver.Resolve(_properties, null);

            CollectionAssert.AreEquivalent(
                new[] { "Id", "IsInput", "Diameter", "Length", "EndMargin", "RotationDeg", "Origin" },
                items.Select(i => i.Name).ToArray());
            Assert.IsTrue(items.All(i => i.Visible));
            Assert.AreEqual("Diameter (mm)", items.First(i => i.Name == "Diameter").DisplayName);
            Assert.AreEqual("Dimensions", items.First(i => i.Name == "Diameter").Category);
        }

        [Test]
        public void TestFiltered_HiddenPropertiesAreLeftOut_AndTheStoredOrderIsKept()
        {
            var layout = new TypePropertyLayout();
            layout.Entries.Add(new PropertyLayoutEntry { Name = "Length", Visible = true });
            layout.Entries.Add(new PropertyLayoutEntry { Name = "Origin", Visible = false });
            layout.Entries.Add(new PropertyLayoutEntry { Name = "Diameter", Visible = true });

            var names = Names(new FilteredProperties(_properties, layout));

            Assert.AreEqual("Length (mm, 0 = automatic)", names[0]);
            Assert.AreEqual("Diameter (mm)", names[1]);
            CollectionAssert.DoesNotContain(names, "Origin (mm)");
            Assert.AreEqual(6, names.Length, "The properties that the layout does not mention follow, visible.");
        }

        [Test]
        public void TestResolve_DropsStoredNamesThatNoLongerExist_AndKeepsNewPropertiesVisible()
        {
            var layout = new TypePropertyLayout();
            layout.Entries.Add(new PropertyLayoutEntry { Name = "Removed in a later version", Visible = false });
            layout.Entries.Add(new PropertyLayoutEntry { Name = "Diameter", Visible = false });

            var items = PropertyLayoutResolver.Resolve(_properties, layout);

            Assert.IsFalse(items.Any(i => i.Name == "Removed in a later version"));
            Assert.AreEqual("Diameter", items[0].Name);
            Assert.IsFalse(items[0].Visible);
            Assert.IsTrue(items.Skip(1).All(i => i.Visible));
            Assert.AreEqual(7, items.Count);
        }

        [Test]
        public void TestFiltered_ReadsAndWritesThroughToTheNode()
        {
            var filtered = new FilteredProperties(_properties, new TypePropertyLayout());
            var diameter = filtered.GetProperties().Cast<PropertyDescriptor>().First(p => p.Name == "Diameter");

            diameter.SetValue(filtered.GetPropertyOwner(diameter), 42.0);

            Assert.AreEqual(42.0, _shaft.Diameter, 1e-12);
            Assert.AreEqual(42.0, (double)diameter.GetValue(filtered.GetPropertyOwner(diameter)), 1e-12);
        }

        [Test]
        public void TestNotes_AppearInTheNotesCategory_AndCanBeEdited()
        {
            var layout = new TypePropertyLayout();
            layout.Notes.Add(new PropertyNote { Name = "Material", Value = "EN8" });
            layout.Notes.Add(new PropertyNote { Name = " ", Value = "ignored: no name" });
            var filtered = new FilteredProperties(_properties, layout);

            var note = filtered.GetProperties().Cast<PropertyDescriptor>().Single(p => p.Category == FilteredProperties.NotesCategory);

            Assert.AreEqual("Material", note.DisplayName);
            Assert.AreEqual("EN8", note.GetValue(filtered.GetPropertyOwner(note)));
            Assert.IsFalse(note.IsReadOnly);

            note.SetValue(filtered.GetPropertyOwner(note), "Phosphor bronze");
            Assert.AreEqual("Phosphor bronze", layout.Notes[0].Value);
        }

        [Test]
        public void TestNote_WithTheNameOfAProperty_DoesNotClashWithIt()
        {
            var layout = new TypePropertyLayout();
            layout.Notes.Add(new PropertyNote { Name = "Id", Value = "note" });

            var descriptors = new FilteredProperties(_properties, layout).GetProperties().Cast<PropertyDescriptor>().ToList();

            Assert.AreEqual(8, descriptors.Count);
            Assert.AreEqual(8, descriptors.Select(d => d.Name).Distinct().Count());
        }

        [Test]
        public void TestToLayout_StoresOrderVisibilityAndNotes()
        {
            var items = PropertyLayoutResolver.Resolve(_properties, null);
            items.Reverse();
            items[0].Visible = false;

            var layout = PropertyLayoutResolver.ToLayout(items, new[] { new PropertyNote { Name = "N", Value = "V" } });

            CollectionAssert.AreEqual(items.Select(i => i.Name).ToArray(), layout.Entries.Select(e => e.Name).ToArray());
            Assert.IsFalse(layout.Entries[0].Visible);
            Assert.AreEqual("V", layout.Notes.Single().Value);
        }

        [Test]
        public void TestSettings_KeyIsTheTypeOfThePropertyObject_AndCloneIsIndependent()
        {
            var settings = new PropertyLayoutSettings { SortMode = PropertySortMode.Custom };
            var key = PropertyLayoutSettings.KeyOf(_properties);
            Assert.AreEqual("ShaftNodeProperties", key);
            Assert.IsNull(settings.Find(key));

            var layout = new TypePropertyLayout();
            layout.Notes.Add(new PropertyNote { Name = "A", Value = "1" });
            settings.Set(key, layout);

            var copy = settings.Clone();
            copy.Find(key).Notes[0].Value = "changed";
            copy.SortMode = PropertySortMode.Alphabetical;

            Assert.AreEqual("1", settings.Find(key).Notes[0].Value);
            Assert.AreEqual(PropertySortMode.Custom, settings.SortMode);
        }

        [Test]
        public void TestMoveCategory_MovesTheWholeCategory_AndKeepsTheOrderInside()
        {
            var items = PropertyLayoutResolver.Resolve(_properties, null);
            var order = PropertyLayoutResolver.CategoryOrder(PropertyLayoutResolver.GroupByCategory(items));
            Assert.Greater(order.Count, 1, "The shaft properties have more than one category.");

            var moved = PropertyLayoutResolver.MoveCategory(items, order[1], -1);

            Assert.IsNotNull(moved);
            Assert.AreEqual(items.Count, moved.Count);
            var newOrder = PropertyLayoutResolver.CategoryOrder(moved);
            Assert.AreEqual(order[1], newOrder[0]);
            Assert.AreEqual(order[0], newOrder[1]);
            CollectionAssert.AreEqual(
                items.Where(i => i.Category == order[1]).Select(i => i.Name).ToArray(),
                moved.Where(i => i.Category == order[1]).Select(i => i.Name).ToArray());
            Assert.IsNull(PropertyLayoutResolver.MoveCategory(items, order[0], -1), "The first category cannot move up.");
            Assert.IsNull(PropertyLayoutResolver.MoveCategory(items, order[order.Count - 1], 1), "The last category cannot move down.");
        }

        [Test]
        public void TestFiltered_CustomMode_ShowsTheCategoriesInTheLayoutOrder()
        {
            var items = PropertyLayoutResolver.Resolve(_properties, null);
            var order = PropertyLayoutResolver.CategoryOrder(PropertyLayoutResolver.GroupByCategory(items));
            var reversed = PropertyLayoutResolver.GroupByCategory(items.OrderByDescending(i => order.IndexOf(i.Category)));
            var layout = PropertyLayoutResolver.ToLayout(reversed, new PropertyNote[0]);

            var descriptors = new FilteredProperties(_properties, layout, PropertySortMode.Custom).GetProperties().Cast<PropertyDescriptor>().ToList();
            var categories = descriptors.Select(d => d.Category).Distinct().ToList();

            CollectionAssert.AreEqual(order.AsEnumerable().Reverse().ToArray(), categories.Select(c => c.TrimStart('\t')).ToArray());
            CollectionAssert.AreEqual(categories.OrderBy(c => c, StringComparer.Ordinal).ToArray(), categories.ToArray(), "Sorted by name they are in the chosen order.");
            Assert.IsTrue(descriptors.All(d => d.Name != null && _properties.GetType().GetProperty(d.Name) != null));
        }

        [Test]
        public void TestFiltered_OtherModes_LeaveTheCategoryNamesAlone()
        {
            var descriptors = new FilteredProperties(_properties, null, PropertySortMode.Category).GetProperties().Cast<PropertyDescriptor>();

            Assert.IsTrue(descriptors.All(d => !d.Category.StartsWith("\t")));
        }

        [Test]
        public void TestStore_SavesAndLoadsTheDefaultLayout()
        {
            var path = Path.Combine(_folder, "nested", "layout.json");
            var settings = new PropertyLayoutSettings { SortMode = PropertySortMode.Alphabetical };
            var layout = new TypePropertyLayout();
            layout.Entries.Add(new PropertyLayoutEntry { Name = "Length", Visible = false });
            layout.Entries.Add(new PropertyLayoutEntry { Name = "Diameter", Visible = true });
            layout.Notes.Add(new PropertyNote { Name = "Material", Value = "EN8" });
            settings.Set("ShaftNodeProperties", layout);

            PropertyLayoutStore.Save(settings, path);
            var loaded = PropertyLayoutStore.Load(path);

            Assert.AreEqual(PropertySortMode.Alphabetical, loaded.SortMode);
            var loadedLayout = loaded.Find("ShaftNodeProperties");
            Assert.AreEqual(new[] { "Length", "Diameter" }, loadedLayout.Entries.Select(e => e.Name).ToArray());
            Assert.IsFalse(loadedLayout.Entries[0].Visible);
            Assert.AreEqual("EN8", loadedLayout.Notes.Single().Value);
            StringAssert.Contains("\"Alphabetical\"", File.ReadAllText(path), "The sort mode is stored by name.");
        }

        [Test]
        public void TestStore_MissingOrDamagedFile_GivesTheBuiltInLayout()
        {
            var missing = PropertyLayoutStore.Load(Path.Combine(_folder, "missing.json"));
            Assert.AreEqual(PropertySortMode.Category, missing.SortMode);
            Assert.AreEqual(0, missing.Types.Count);

            Directory.CreateDirectory(_folder);
            var damaged = Path.Combine(_folder, "damaged.json");
            File.WriteAllText(damaged, "{ this is not json");
            Assert.AreEqual(0, PropertyLayoutStore.Load(damaged).Types.Count);

            var partial = Path.Combine(_folder, "partial.json");
            File.WriteAllText(partial, "{\"Types\":{\"ShaftNodeProperties\":{}, \"Other\":null}}");
            var loaded = PropertyLayoutStore.Load(partial);
            Assert.IsNotNull(loaded.Find("ShaftNodeProperties").Entries);
            Assert.IsNotNull(loaded.Find("ShaftNodeProperties").Notes);
            Assert.IsNull(loaded.Find("Other"));
        }

        [Test]
        public void TestStore_DefaultPathIsInTheApplicationDataFolder()
        {
            StringAssert.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), PropertyLayoutStore.DefaultPath);
            StringAssert.EndsWith("property-layout.json", PropertyLayoutStore.DefaultPath);
        }
    }
}
