using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using AlibreAddOn;
using AlibreX;
using Bolsover.Gears.AddOn;

namespace AlibreAddOnAssembly
{
    /// <summary>
    /// Entry points that Alibre Design looks for in a managed add-on assembly. The namespace and class name
    /// must stay as they are: Alibre finds the add-on by them.
    /// </summary>
    public static class AlibreAddOn
    {
        private static GearTrainAddOn _addOn;
        private static bool _resolverInstalled;

        private static IADRoot AlibreRoot { get; set; }

        /// <summary>Called when Alibre starts (the .adc file asks for loading at startup).</summary>
        public static void AddOnLoad(IntPtr hwnd, IAutomationHook pAutomationHook, IntPtr unused)
        {
            InstallAssemblyResolver();

            try
            {
                AlibreRoot = (IADRoot) pAutomationHook.Root;
                _addOn = new GearTrainAddOn(AlibreRoot, hwnd);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Gears - Gear Train add-on failed to load", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        public static IADRoot GetRoot()
        {
            return AlibreRoot;
        }

        /// <summary>Called when the user chooses the add-on from the add-ons menu; the menu command does the work.</summary>
        public static void AddOnInvoke(
            IntPtr hwnd,
            IntPtr pAutomationHook,
            string sessionName,
            bool isLicensed,
            int reserved1,
            int reserved2)
        {
        }

        /// <summary>Called when Alibre closes or the add-on is switched off in the add-on manager.</summary>
        public static void AddOnUnload(
            IntPtr hwnd,
            bool forceUnload,
            ref bool cancel,
            int reserved1,
            int reserved2)
        {
            _addOn?.CloseForm();
            _addOn = null;
        }

        /// <summary>Returns the object that gives Alibre the menu tree and the commands.</summary>
        public static IAlibreAddOn GetAddOnInterface()
        {
            return _addOn;
        }

        /// <summary>
        /// The add-on ships with its own copies of libraries that Alibre may also use in other versions
        /// (for example System.Text.Json). Alibre's process does not apply this add-on's binding redirects, so a
        /// library that cannot be bound by version is taken from the add-on folder by name.
        /// </summary>
        private static void InstallAssemblyResolver()
        {
            if (_resolverInstalled) return;
            _resolverInstalled = true;

            var folder = Path.GetDirectoryName(typeof(AlibreAddOn).Assembly.Location);
            if (string.IsNullOrEmpty(folder)) return;

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name).Name;
                if (string.IsNullOrEmpty(name) || name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)) return null;

                foreach (var extension in new[] { ".dll", ".exe" })
                {
                    var path = Path.Combine(folder, name + extension);
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }

                return null;
            };
        }
    }
}
