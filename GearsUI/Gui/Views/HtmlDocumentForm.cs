using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>
    /// A popup window that shows an HTML document that ships with the program (the read me and the license) in an embedded browser.
    /// Links to other local pages are followed inside the window; links to web sites open in the default browser.
    /// </summary>
    public sealed class HtmlDocumentForm : Form
    {
        private readonly WebBrowser _browser;

        public HtmlDocumentForm(string title, string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("A document path is required.", nameof(path));

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(900, 700);
            ShowIcon = false;
            ShowInTaskbar = false;
            MinimizeBox = false;
            KeyPreview = true;

            _browser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                AllowWebBrowserDrop = false,
                IsWebBrowserContextMenuEnabled = false,
                ScriptErrorsSuppressed = true,
                WebBrowserShortcutsEnabled = true
            };
            _browser.Navigating += OnNavigating;
            Controls.Add(_browser);

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
            };

            _browser.Navigate(new Uri(path));
        }

        private void OnNavigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            var url = e.Url;
            if (url == null || url.IsFile || url.Scheme == "about") return;

            e.Cancel = true;
            if (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps) return;

            try
            {
                Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception)
            {
                // A link that cannot be opened is ignored.
            }
        }
    }
}
