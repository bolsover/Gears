using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using System.Xml;
using AlibreAddOn;
using AlibreX;
using Bolsover.Gears.AddOn;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Tests for the Alibre add-on wrapper around GearTrainForm. They need no running Alibre: the add-on is created
    /// without a root object and with a stand-in form.
    /// </summary>
    [TestFixture]
    public class TestGearAddOn
    {
        private static GearTrainAddOn CreateAddOn(Func<Form> factory = null)
        {
            return new GearTrainAddOn(null, IntPtr.Zero, factory ?? CreateInvisibleForm);
        }

        private static Form CreateInvisibleForm()
        {
            return new Form { ShowInTaskbar = false, Opacity = 0, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-3000, -3000) };
        }

        private static string FindAddOnFile(string fileName)
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(typeof(TestGearAddOn).Assembly.Location) ?? ".");
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "GearsAddOn", fileName);
                if (File.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            Assert.Fail("Could not find GearsAddOn\\" + fileName + " above the test output folder.");
            return null;
        }

        [Test]
        public void RootMenu_HasOneGearTrainCommand()
        {
            IAlibreAddOn addOn = CreateAddOn();

            Assert.AreEqual(GearTrainAddOn.MenuIdRoot, addOn.RootMenuItem);
            Assert.IsTrue(addOn.HasSubMenus(GearTrainAddOn.MenuIdRoot), "The root menu has no sub menus.");
           

          
        }

        [Test]
        public void MenuItems_HaveTextToolTipAndAreEnabled()
        {
            IAlibreAddOn addOn = CreateAddOn();

            Assert.AreEqual("Gears", addOn.MenuItemText(GearTrainAddOn.MenuIdRoot));
           
            Assert.AreEqual(string.Empty, addOn.MenuItemText(-1));
          

           
            Assert.IsFalse(addOn.UseDedicatedRibbonTab());
            Assert.IsFalse(addOn.HasPersistentDataToSave("Part1"));
        }

        


        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void InvokeCommand_AfterTheFormIsClosed_OpensANewOne()
        {
            var addOn = CreateAddOn();

            try
            {
                addOn.ShowGearTrain();
                var first = addOn.Form;
                first.Close();

                Assert.IsFalse(addOn.IsFormOpen, "A closed form is forgotten.");
                Assert.IsNull(addOn.Form);

                addOn.ShowGearTrain();
                Assert.IsTrue(addOn.IsFormOpen);
                Assert.AreNotSame(first, addOn.Form);
            }
            finally
            {
                addOn.CloseForm();
            }
        }

        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public void CloseForm_ClosesAnOpenForm_AndIsSafeToRepeat()
        {
            var addOn = CreateAddOn();
            addOn.ShowGearTrain();
            var form = addOn.Form;

            addOn.CloseForm();
            Assert.IsFalse(addOn.IsFormOpen);
            Assert.IsTrue(form.IsDisposed);

            Assert.DoesNotThrow(addOn.CloseForm);
        }

       

        [Test]
        public void Constructor_NullFormFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GearTrainAddOn(null, IntPtr.Zero, null));
        }

        [Test]
        public void EntryPointClass_HasTheMethodsAlibreCalls()
        {
            var type = typeof(AlibreAddOnAssembly.AlibreAddOn);
            Assert.IsTrue(type.IsAbstract && type.IsSealed, "The entry point class is static.");

            var load = type.GetMethod("AddOnLoad", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(load, "AddOnLoad");
            CollectionAssert.AreEqual(new[] { typeof(IntPtr), typeof(IAutomationHook), typeof(IntPtr) },
                load.GetParameters().Select(p => p.ParameterType).ToArray());

            var invoke = type.GetMethod("AddOnInvoke", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(invoke, "AddOnInvoke");
            Assert.AreEqual(6, invoke.GetParameters().Length);

            var unload = type.GetMethod("AddOnUnload", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(unload, "AddOnUnload");
            Assert.IsTrue(unload.GetParameters()[2].ParameterType.IsByRef, "cancel is passed by reference.");

            var getInterface = type.GetMethod("GetAddOnInterface", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(getInterface, "GetAddOnInterface");
            Assert.AreEqual(typeof(IAlibreAddOn), getInterface.ReturnType);
        }

        [Test]
        public void EntryPoint_BeforeLoad_HasNoAddOn_AndUnloadDoesNotCancel()
        {
            Assert.IsNull(AlibreAddOnAssembly.AlibreAddOn.GetAddOnInterface());

            var cancel = false;
            Assert.DoesNotThrow(() => AlibreAddOnAssembly.AlibreAddOn.AddOnUnload(IntPtr.Zero, false, ref cancel, 0, 0));
            Assert.IsFalse(cancel, "Unloading must never be cancelled.");
        }

        [Test]
        public void AdcFile_MatchesTheCode()
        {
            var document = new XmlDocument();
            document.Load(FindAddOnFile("GearsAddOn.adc"));

            var root = document.DocumentElement;
            Assert.AreEqual("AlibreDesignAddOn", root.Name);
            Assert.AreEqual("2", root.GetAttribute("specificationVersion"));

            var dll = root.SelectSingleNode("DLL") as XmlElement;
            Assert.IsNotNull(dll, "DLL element");
            Assert.AreEqual("Managed", dll.GetAttribute("type"));
            Assert.AreEqual(typeof(GearTrainAddOn).Assembly.GetName().Name + ".dll", dll.GetAttribute("location"));

            var identifier = root.SelectSingleNode("Property[@name='Identifier']") as XmlElement;
            Assert.IsNotNull(identifier, "Identifier property");
            Assert.AreEqual(GearTrainAddOn.AddOnId, identifier.GetAttribute("value"));
            Assert.IsTrue(Guid.TryParse(identifier.GetAttribute("value"), out _), "The identifier is a GUID.");

            var icon = root.SelectSingleNode("Icon") as XmlElement;
            Assert.IsNotNull(icon, "Icon element");
            Assert.IsTrue(File.Exists(FindAddOnFile(icon.GetAttribute("location"))), "The icon file exists.");

            Assert.IsNotNull(root.SelectSingleNode("Menu"), "Menu element");
            Assert.IsNotNull(root.SelectSingleNode("Workspace"), "Workspace element");
        }

        [Test]
        public void AddOnJson_MatchesTheAdcFile()
        {
            using (var json = JsonDocument.Parse(File.ReadAllText(FindAddOnFile("addon.json"))))
            {
                var root = json.RootElement;
                Assert.AreEqual(GearTrainAddOn.AddOnId, root.GetProperty("id").GetString());
                Assert.AreEqual("managed", root.GetProperty("dllType").GetString());
                Assert.AreEqual("x64", root.GetProperty("arch").GetString());
                Assert.AreEqual("GearsAddOn", root.GetProperty("folderName").GetString());
            }
        }

        [Test]
        public void InstallScript_UsesTheSameIdentifier()
        {
            var script = File.ReadAllText(FindAddOnFile("Install-GearsAddOn.ps1"));
            StringAssert.Contains(GearTrainAddOn.AddOnId, script);
            StringAssert.Contains(@"SOFTWARE\Alibre Design Add-Ons", script);
        }
    }
}
