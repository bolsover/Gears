using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Bolsover.Gears.Gui.Views
{
    public sealed class ReferenceForm : Form
    {
        private sealed class HistoryEntry
        {
            public HistoryEntry(string filePath, string fragment)
            {
                FilePath = filePath;
                Fragment = fragment;
            }

            public string FilePath { get; }
            public string Fragment { get; }
        }

        private readonly string _topicsFolder;
        private readonly TreeView _contents;
        private readonly WebBrowser _browser;
        private readonly ToolStripButton _backButton;
        private readonly ToolStripButton _forwardButton;
        private readonly string _homePage;
        private readonly List<HistoryEntry> _history = new List<HistoryEntry>();
        private int _historyIndex = -1;
        private string _currentFile;
        private string _pendingFragment;
        private bool _clickHandlerAttached;
        private bool _updatingSelection;

        public ReferenceForm(string topicsFolder)
        {
            if (string.IsNullOrWhiteSpace(topicsFolder) || !Directory.Exists(topicsFolder))
                throw new ArgumentException("A Writerside topics folder is required.", nameof(topicsFolder));

            _topicsFolder = Path.GetFullPath(topicsFolder);
            var tableOfContents = MarkdownTopicRenderer.ReadTableOfContents(_topicsFolder);
            if (tableOfContents.Count == 0) throw new InvalidOperationException("No Markdown topics were found.");
            _homePage = tableOfContents[0].FilePath;

            Text = "Gears Reference";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1000, 750);
            ShowIcon = false;
            ShowInTaskbar = false;
            MinimizeBox = false;
            KeyPreview = true;

            _contents = new TreeView { Dock = DockStyle.Fill, HideSelection = false };
            foreach (var topic in tableOfContents) _contents.Nodes.Add(CreateTreeNode(topic));
            _contents.AfterSelect += OnTopicSelected;

            _browser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                AllowWebBrowserDrop = false,
                IsWebBrowserContextMenuEnabled = false,
                ScriptErrorsSuppressed = true,
                WebBrowserShortcutsEnabled = true
            };
            _browser.Navigating += OnNavigating;
            _browser.DocumentCompleted += OnDocumentCompleted;

            _backButton = new ToolStripButton("Back") { Enabled = false };
            _forwardButton = new ToolStripButton("Forward") { Enabled = false };
            _backButton.Click += (sender, args) => GoBack();
            _forwardButton.Click += (sender, args) => GoForward();
            var homeButton = new ToolStripButton("Home");
            homeButton.Click += (sender, args) => NavigateTo(_homePage, null, true);
            var toolStrip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Top };
            toolStrip.Items.AddRange(new ToolStripItem[] { _backButton, _forwardButton, new ToolStripSeparator(), homeButton });

            var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 250, FixedPanel = FixedPanel.Panel1 };
            split.Panel1.Controls.Add(_contents);
            split.Panel2.Controls.Add(_browser);
            Controls.Add(split);
            Controls.Add(toolStrip);

            KeyDown += (sender, args) =>
            {
                if (args.KeyCode == Keys.Escape) Close();
                else if (args.Alt && args.KeyCode == Keys.Left) GoBack();
                else if (args.Alt && args.KeyCode == Keys.Right) GoForward();
            };

            NavigateTo(_homePage, null, true);
            _contents.Nodes[0].Expand();
        }

        private static TreeNode CreateTreeNode(MarkdownTopic topic)
        {
            var node = new TreeNode(topic.Title) { Tag = topic.FilePath };
            foreach (var child in topic.Children) node.Nodes.Add(CreateTreeNode(child));
            return node;
        }

        private void OnTopicSelected(object sender, TreeViewEventArgs e)
        {
            if (_updatingSelection) return;
            var path = e.Node.Tag as string;
            if (!string.IsNullOrEmpty(path)) NavigateTo(path, null, true);
        }

        private void NavigateTo(string filePath, string fragment, bool addHistory)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
            var fullPath = Path.GetFullPath(filePath);
            var markdown = File.ReadAllText(fullPath);
            var html = MarkdownTopicRenderer.RenderPage(markdown, _topicsFolder);

            if (addHistory)
            {
                if (_historyIndex < _history.Count - 1) _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
                _history.Add(new HistoryEntry(fullPath, fragment));
                _historyIndex = _history.Count - 1;
            }

            _currentFile = fullPath;
            _pendingFragment = fragment;
            _clickHandlerAttached = false;
            _browser.DocumentText = html;
            SelectTopic(fullPath);
            UpdateHistoryButtons();
        }

        private void GoBack()
        {
            if (_historyIndex <= 0) return;
            _historyIndex--;
            ShowHistoryEntry(_history[_historyIndex]);
        }

        private void GoForward()
        {
            if (_historyIndex >= _history.Count - 1) return;
            _historyIndex++;
            ShowHistoryEntry(_history[_historyIndex]);
        }

        private void ShowHistoryEntry(HistoryEntry entry)
        {
            NavigateTo(entry.FilePath, entry.Fragment, false);
        }

        private void UpdateHistoryButtons()
        {
            _backButton.Enabled = _historyIndex > 0;
            _forwardButton.Enabled = _historyIndex >= 0 && _historyIndex < _history.Count - 1;
        }

        private void SelectTopic(string filePath)
        {
            var node = FindNode(_contents.Nodes, filePath);
            if (node == null || _contents.SelectedNode == node) return;
            _updatingSelection = true;
            _contents.SelectedNode = node;
            node.EnsureVisible();
            _updatingSelection = false;
        }

        private static TreeNode FindNode(TreeNodeCollection nodes, string filePath)
        {
            foreach (TreeNode node in nodes)
            {
                if (string.Equals(node.Tag as string, filePath, StringComparison.OrdinalIgnoreCase)) return node;
                var child = FindNode(node.Nodes, filePath);
                if (child != null) return child;
            }

            return null;
        }

        private void OnNavigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            var url = e.Url;
            if (url == null || string.Equals(url.Scheme, "about", StringComparison.OrdinalIgnoreCase)) return;

            if (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps)
            {
                e.Cancel = true;
                try
                {
                    Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
                }
                catch (Exception)
                {
                    // A link that cannot be opened is ignored.
                }
                return;
            }

            if (!url.IsFile) return;
            var localPath = Path.GetFullPath(url.LocalPath);
            var fragment = Uri.UnescapeDataString(url.Fragment.TrimStart('#'));
            if (!string.IsNullOrEmpty(fragment) &&
                (PathsEqual(localPath, _currentFile) || PathsEqual(localPath.TrimEnd(Path.DirectorySeparatorChar), _topicsFolder.TrimEnd(Path.DirectorySeparatorChar))))
            {
                e.Cancel = true;
                ScrollToFragment(fragment);
                return;
            }

            if (!string.Equals(Path.GetExtension(localPath), ".md", StringComparison.OrdinalIgnoreCase)) return;
            e.Cancel = true;
            if (!IsWithinTopicsFolder(localPath)) return;
            if (PathsEqual(localPath, _currentFile) && string.IsNullOrEmpty(fragment)) return;
            NavigateTo(localPath, string.IsNullOrEmpty(fragment) ? null : fragment, true);
        }

        private bool IsWithinTopicsFolder(string path)
        {
            var root = _topicsFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        private static bool PathsEqual(string first, string second)
        {
            return !string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(second) &&
                   string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase);
        }

        private void OnDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            var document = _browser.Document;
            if (document != null && !_clickHandlerAttached)
            {
                document.Click += OnDocumentClick;
                _clickHandlerAttached = true;
            }

            if (string.IsNullOrEmpty(_pendingFragment)) return;
            ScrollToFragment(_pendingFragment);
            _pendingFragment = null;
        }

        private void OnDocumentClick(object sender, HtmlElementEventArgs e)
        {
            var document = _browser.Document;
            var element = document == null ? null : document.GetElementFromPoint(e.ClientMousePosition);
            while (element != null && !string.Equals(element.TagName, "A", StringComparison.OrdinalIgnoreCase))
                element = element.Parent;
            if (element == null) return;

            var href = element.GetAttribute("href");
            if (TryOpenExternalLink(href))
            {
                e.ReturnValue = false;
                return;
            }

            string filePath;
            string fragment;
            if (MarkdownTopicRenderer.TryResolveTopicLink(href, _currentFile, _topicsFolder, out filePath, out fragment))
            {
                e.ReturnValue = false;
                if (PathsEqual(filePath, _currentFile))
                {
                    if (!string.IsNullOrEmpty(fragment)) ScrollToFragment(fragment);
                    return;
                }

                NavigateTo(filePath, fragment, true);
                return;
            }

            Uri uri;
            var isMarkdownLink = Uri.TryCreate(href, UriKind.Absolute, out uri) && uri.IsFile
                ? string.Equals(Path.GetExtension(uri.LocalPath), ".md", StringComparison.OrdinalIgnoreCase)
                : string.Equals(Path.GetExtension(href.Split('#')[0].Split('?')[0]), ".md", StringComparison.OrdinalIgnoreCase);
            if (isMarkdownLink) e.ReturnValue = false;
        }

        private static bool TryOpenExternalLink(string href)
        {
            Uri url;
            if (!Uri.TryCreate(href, UriKind.Absolute, out url) ||
                (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeMailto))
                return false;

            try
            {
                Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception)
            {
                // A link that cannot be opened is ignored.
            }

            return true;
        }

        private void ScrollToFragment(string fragment)
        {
            if (_browser.Document == null || string.IsNullOrEmpty(fragment)) return;
            var element = _browser.Document.GetElementById(fragment);
            if (element != null) element.ScrollIntoView(true);
        }
    }
}
