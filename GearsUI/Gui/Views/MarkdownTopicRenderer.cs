using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Markdig;

namespace Bolsover.Gears.Gui.Views
{
    public sealed class MarkdownTopic
    {
        internal MarkdownTopic(string filePath, string title)
        {
            FilePath = filePath;
            Title = title;
            Children = new List<MarkdownTopic>();
        }

        public string FilePath { get; }
        public string Title { get; }
        public IList<MarkdownTopic> Children { get; }
    }

    public static class MarkdownTopicRenderer
    {
        private const string StartPage = "Gears-Overview.md";
        private static readonly Regex HeadingPattern = new Regex(@"(?m)^\s{0,3}#{1,6}\s+(.+?)\s*#*\s*$", RegexOptions.Compiled);
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseGridTables()
            .UseEmphasisExtras()
            .UseAutoIdentifiers()
            .UseMathematics()
            .UseListExtras()
            .UseAutoLinks()
            .UseFootnotes()
            .Build();

        public static string FindTopicsFolder(string startFolder)
        {
            if (string.IsNullOrWhiteSpace(startFolder)) return null;

            var folder = Path.GetFullPath(startFolder);
            while (!string.IsNullOrEmpty(folder))
            {
                var topicsFolder = Path.Combine(folder, "Writerside", "topics");
                if (File.Exists(Path.Combine(topicsFolder, StartPage))) return topicsFolder;
                folder = Path.GetDirectoryName(folder);
            }

            return null;
        }

        public static IList<MarkdownTopic> ReadTableOfContents(string topicsFolder)
        {
            if (string.IsNullOrWhiteSpace(topicsFolder) || !Directory.Exists(topicsFolder)) return new List<MarkdownTopic>();

            var tocPath = Path.Combine(Directory.GetParent(Path.GetFullPath(topicsFolder)).FullName, "g.tree");
            if (File.Exists(tocPath))
            {
                try
                {
                    var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
                    using (var reader = XmlReader.Create(tocPath, settings))
                    {
                        var document = XDocument.Load(reader);
                        var root = document.Descendants("toc-element")
                            .FirstOrDefault(element => string.Equals((string)element.Attribute("topic"), StartPage, StringComparison.OrdinalIgnoreCase));
                        if (root != null)
                        {
                            var topic = ReadTopic(root, topicsFolder);
                            if (topic != null) return new List<MarkdownTopic> { topic };
                        }
                    }
                }
                catch (XmlException)
                {
                    // A missing or malformed TOC falls back to the available Markdown files.
                }
                catch (IOException)
                {
                    // A missing or unreadable TOC falls back to the available Markdown files.
                }
            }

            return ReadMarkdownFiles(topicsFolder);
        }

        public static string GetTitle(string markdown)
        {
            if (!string.IsNullOrEmpty(markdown))
            {
                var match = HeadingPattern.Match(markdown);
                if (match.Success)
                {
                    var title = Regex.Replace(match.Groups[1].Value, @"[`*_~]", string.Empty);
                    return WebUtility.HtmlDecode(title).Trim();
                }
            }

            return "Untitled";
        }

        public static string RenderPage(string markdown, string baseFolder)
        {
            if (string.IsNullOrWhiteSpace(baseFolder)) throw new ArgumentException("A base folder is required.", nameof(baseFolder));

            var fullFolder = Path.GetFullPath(baseFolder);
            if (!fullFolder.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                fullFolder += Path.DirectorySeparatorChar;
            var baseUri = new Uri(fullFolder).AbsoluteUri;
            var html = Markdown.ToHtml(markdown ?? string.Empty, Pipeline);

            return "<!DOCTYPE html><html><head>" +
                   "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\"><meta charset=\"utf-8\">" +
                   "<base href=\"" + WebUtility.HtmlEncode(baseUri) + "\">" +
                   "<style>" +
                   "body{font-family:Segoe UI,Arial,sans-serif;font-size:10pt;line-height:1.45;color:#222;margin:1.5em 2em;}" +
                   "table{border-collapse:collapse;margin:1em 0;}th,td{border:1px solid #bbb;padding:.35em .6em;}" +
                   "pre{background:#f5f5f5;padding:.8em;overflow:auto;}code{font-family:Consolas,monospace;}" +
                   "link-summary{display:none;}note,tip,warning,seealso{display:block;margin:1em 0;padding:.8em 1em;border-left:4px solid #6b9fcb;background:#eef5fa;}" +
                   "tip{border-color:#5a9b58;background:#eff8ed;}warning{border-color:#d29c37;background:#fff8e7;}" +
                   "seealso{border-left:0;background:transparent;padding:.3em 0;}seealso:before{content:'See also';display:block;font-weight:bold;margin-bottom:.3em;}" +
                   "seealso a{margin-right:.8em;}" +
                   "</style>" +
                   "<script type=\"text/x-mathjax-config\">MathJax.Hub.Config({tex2jax:{inlineMath:[['\\\\(','\\\\)']],displayMath:[['\\\\[','\\\\]']]},TeX:{extensions:['AMSmath.js','AMSsymbols.js']},'HTML-CSS':{availableFonts:['TeX'],imageFont:null},showMathMenu:false});</script>" +
                   "<script type=\"text/javascript\" src=\"https://cdnjs.cloudflare.com/ajax/libs/mathjax/2.7.9/MathJax.js?config=TeX-AMS_HTML\"></script>" +
                   "</head><body>" + html + "</body></html>";
        }

        public static bool TryResolveTopicLink(string href, string currentFile, string topicsFolder, out string filePath, out string fragment)
        {
            filePath = null;
            fragment = null;
            if (string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(currentFile) || string.IsNullOrWhiteSpace(topicsFolder))
                return false;

            var link = href.Trim();
            try
            {
                string resolvedPath;
                string resolvedFragment;
                if (link.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
                {
                    Uri aboutUri;
                    const string aboutBlank = "about:blank";
                    if (!Uri.TryCreate(link, UriKind.Absolute, out aboutUri) ||
                        !string.Equals(aboutUri.Scheme, "about", StringComparison.OrdinalIgnoreCase) ||
                        !link.StartsWith(aboutBlank, StringComparison.OrdinalIgnoreCase) ||
                        (link.Length > aboutBlank.Length && link[aboutBlank.Length] != '#' && link[aboutBlank.Length] != '?'))
                        return false;

                    resolvedPath = currentFile;
                    resolvedFragment = aboutUri.Fragment;
                }
                else if (link[0] == '#')
                {
                    resolvedPath = currentFile;
                    resolvedFragment = link;
                }
                else if (link.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                {
                    Uri fileUri;
                    if (!Uri.TryCreate(link, UriKind.Absolute, out fileUri) || !fileUri.IsFile) return false;
                    resolvedPath = fileUri.LocalPath;
                    resolvedFragment = fileUri.Fragment;
                    var folderPath = Path.GetFullPath(topicsFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (!string.IsNullOrEmpty(resolvedFragment) &&
                        string.Equals(Path.GetFullPath(resolvedPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                            folderPath, StringComparison.OrdinalIgnoreCase))
                        resolvedPath = currentFile;
                }
                else
                {
                    var fragmentIndex = link.IndexOf('#');
                    var pathAndQuery = fragmentIndex < 0 ? link : link.Substring(0, fragmentIndex);
                    resolvedFragment = fragmentIndex < 0 ? null : link.Substring(fragmentIndex);
                    var queryIndex = pathAndQuery.IndexOf('?');
                    var path = queryIndex < 0 ? pathAndQuery : pathAndQuery.Substring(0, queryIndex);

                    if (Path.IsPathRooted(path))
                        resolvedPath = path;
                    else
                    {
                        if (Regex.IsMatch(link, @"^[A-Za-z][A-Za-z0-9+.-]*:")) return false;
                        resolvedPath = string.IsNullOrEmpty(path)
                            ? currentFile
                            : Path.Combine(topicsFolder, Uri.UnescapeDataString(path));
                    }
                }

                resolvedPath = Path.GetFullPath(resolvedPath);
                var topicsPath = Path.GetFullPath(topicsFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!resolvedPath.StartsWith(topicsPath, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(Path.GetExtension(resolvedPath), ".md", StringComparison.OrdinalIgnoreCase))
                    return false;

                var decodedFragment = string.IsNullOrEmpty(resolvedFragment)
                    ? null
                    : Uri.UnescapeDataString(resolvedFragment.TrimStart('#'));
                filePath = resolvedPath;
                fragment = decodedFragment;
                return true;
            }
            catch (UriFormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static MarkdownTopic ReadTopic(XElement element, string topicsFolder)
        {
            var fileName = (string)element.Attribute("topic");
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            var filePath = Path.GetFullPath(Path.Combine(topicsFolder, fileName));
            if (!filePath.StartsWith(Path.GetFullPath(topicsFolder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
                return null;

            var markdown = File.ReadAllText(filePath);
            var title = GetTitle(markdown);
            if (title == "Untitled") title = Path.GetFileNameWithoutExtension(fileName).Replace('-', ' ');
            var topic = new MarkdownTopic(filePath, title);

            foreach (var child in element.Elements("toc-element"))
            {
                var childTopic = ReadTopic(child, topicsFolder);
                if (childTopic != null) topic.Children.Add(childTopic);
            }

            return topic;
        }

        private static IList<MarkdownTopic> ReadMarkdownFiles(string topicsFolder)
        {
            return Directory.GetFiles(topicsFolder, "*.md")
                .OrderBy(path => string.Equals(Path.GetFileName(path), StartPage, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                .Select(path =>
                {
                    var markdown = File.ReadAllText(path);
                    var title = GetTitle(markdown);
                    if (title == "Untitled") title = Path.GetFileNameWithoutExtension(path).Replace('-', ' ');
                    return new MarkdownTopic(path, title);
                })
                .ToList();
        }
    }
}
