using System;
using System.Windows.Forms;
using AlibreAddOn;
using AlibreX;
using Bolsover.Gears.Gui.Views;

namespace Bolsover.Gears.AddOn
{
    /// <summary>
    /// The Alibre add-on: a "Gears" menu with a single command that opens <see cref="GearTrainForm"/>.
    /// The form is modeless and owned by the Alibre main window, so Alibre stays usable while it is open.
    /// </summary>
    public sealed class GearTrainAddOn : IAlibreAddOn
    {
        /// <summary>Identifier of the add-on; it must match the Identifier property of the .adc file.</summary>
        public const string AddOnId = "{05A0F2F2-61D4-48B3-A737-BD23152F6FFF}";

        /// <summary>Menu ID of the root menu item.</summary>
        public const int MenuIdRoot = 3001;

        /// <summary>Menu ID of the command that opens the gear train editor.</summary>
        public const int MenuIdGearTrain = 3002;

        private static readonly int[] RootItems = { MenuIdGearTrain };

        private readonly Func<Form> _formFactory;
        private readonly IWin32Window _owner;
        private Form _form;

        /// <param name="alibreRoot">The Alibre root object (kept for future use; the editor does not need it yet).</param>
        /// <param name="parentWindowHandle">Handle of the Alibre main window, used as the owner of the editor.</param>
        public GearTrainAddOn(IADRoot alibreRoot, IntPtr parentWindowHandle)
            : this(alibreRoot, parentWindowHandle, () => new GearTrainForm())
        {
        }

        /// <param name="alibreRoot">The Alibre root object.</param>
        /// <param name="parentWindowHandle">Handle of the Alibre main window, or <see cref="IntPtr.Zero"/> for no owner.</param>
        /// <param name="formFactory">Creates the editor form; replaced in tests so that no window is shown.</param>
        public GearTrainAddOn(IADRoot alibreRoot, IntPtr parentWindowHandle, Func<Form> formFactory)
        {
            AlibreRoot = alibreRoot;
            _formFactory = formFactory ?? throw new ArgumentNullException(nameof(formFactory));
            _owner = parentWindowHandle == IntPtr.Zero ? null : new WindowOwner(parentWindowHandle);
        }

        public IADRoot AlibreRoot { get; }

        /// <summary>True while the editor form exists and has not been closed.</summary>
        public bool IsFormOpen => _form != null && !_form.IsDisposed;

        /// <summary>The editor form, or null when it is not open.</summary>
        public Form Form => IsFormOpen ? _form : null;

        /// <summary>Closes the editor if it is open. Called when Alibre unloads the add-on.</summary>
        public void CloseForm()
        {
            var form = _form;
            _form = null;
            if (form == null || form.IsDisposed) return;

            form.Close();
            form.Dispose();
        }

        /// <summary>Opens the editor, or brings it to the front if it is already open.</summary>
        public void ShowGearTrain()
        {
            if (IsFormOpen)
            {
                if (_form.WindowState == FormWindowState.Minimized) _form.WindowState = FormWindowState.Normal;
                _form.Activate();
                return;
            }

            var form = _formFactory();
            form.FormClosed += OnFormClosed;
            _form = form;
            if (_owner != null) form.Show(_owner);
            else form.Show();
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            if (ReferenceEquals(sender, _form)) _form = null;
        }

        #region Menus

        /// <summary>Returns the menu ID of the add-on root menu item.</summary>
        public int RootMenuItem => MenuIdRoot;

        /// <summary>Returns whether the menu item has sub menus.</summary>
        public bool HasSubMenus(int menuId)
        {
            return menuId == MenuIdRoot;
        }

        /// <summary>Returns the IDs of the sub menu items under a menu item; the ID of a leaf item is its command ID.</summary>
        public Array SubMenuItems(int menuId)
        {
            return menuId == MenuIdRoot ? (int[]) RootItems.Clone() : null;
        }

        /// <summary>Returns the display text of a menu item.</summary>
        public string MenuItemText(int menuId)
        {
            switch (menuId)
            {
                case MenuIdRoot: return "Gears";
                case MenuIdGearTrain: return "Gear Train";
                default: return string.Empty;
            }
        }

        /// <summary>Returns true if the menu item is a popup menu.</summary>
        public bool PopupMenu(int menuId)
        {
            return false;
        }

        /// <summary>The editor does not depend on an open document, so every command is enabled everywhere.</summary>
        public ADDONMenuStates MenuItemState(int menuId, string sessionIdentifier)
        {
            return ADDONMenuStates.ADDON_MENU_ENABLED;
        }

        /// <summary>Returns the tool tip of a leaf menu item.</summary>
        public string MenuItemToolTip(int menuId)
        {
            switch (menuId)
            {
                case MenuIdRoot: return "Gears";
                case MenuIdGearTrain: return "Opens the gear train editor: shafts, spur, helical, bevel, worm and planetary gears, with 2D and 3D views";
                default: return string.Empty;
            }
        }

        /// <summary>Returns the icon file name of a menu item, searched for in the folder of the .adc file.</summary>
        public string MenuIcon(int menuId)
        {
            return string.Empty;
        }

        /// <summary>This add-on does not use a dedicated ribbon tab.</summary>
        public bool UseDedicatedRibbonTab()
        {
            return false;
        }

        #endregion

        #region Commands and data

        /// <summary>
        /// Runs the command for a menu item. The editor is an ordinary window, so no command object is returned
        /// and Alibre does not enter a command mode.
        /// </summary>
        public IAlibreAddOnCommand InvokeCommand(int menuId, string sessionIdentifier)
        {
            if (menuId != MenuIdGearTrain) return null;

            try
            {
                ShowGearTrain();
            }
            catch (Exception ex)
            {
                // An exception that escapes into Alibre can take the whole application down.
                MessageBox.Show(_owner, ex.ToString(), "Gears - Gear Train", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }

        /// <summary>Gear trains are saved to their own files, not into the Alibre document.</summary>
        public bool HasPersistentDataToSave(string sessionIdentifier)
        {
            return false;
        }

        public void LoadData(IStream pCustomData, string sessionIdentifier)
        {
        }

        public void SaveData(IStream pCustomData, string sessionIdentifier)
        {
        }

        public void setIsAddOnLicensed(bool isLicensed)
        {
        }

        #endregion

        /// <summary>Wraps the handle of the Alibre main window so that it can own a WinForms form.</summary>
        private sealed class WindowOwner : IWin32Window
        {
            public WindowOwner(IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; }
        }
    }
}
