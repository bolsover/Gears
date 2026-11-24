using System;
using System.IO;
using Bolsover.Gears.Gui.Views;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class TestReferenceRenderer
    {
        [Test]
        public void RenderPage_OverviewContainsWritersideContentAndLinks()
        {
            var topicsFolder = MarkdownTopicRenderer.FindTopicsFolder(AppDomain.CurrentDomain.BaseDirectory);
            Assert.IsNotNull(topicsFolder, "The Writerside topics should be available beside the test output.");
            var markdown = File.ReadAllText(Path.Combine(topicsFolder, "Gears-Overview.md"));

            var html = MarkdownTopicRenderer.RenderPage(markdown, topicsFolder);

            StringAssert.Contains("<table>", html);
            StringAssert.Contains("href=\"Symbols.md\"", html);
            StringAssert.Contains("<note>", html);
            StringAssert.Contains("<base href=", html);
        }

        [Test]
        public void RenderPage_MathematicsUsesInlineAndDisplayMathElements()
        {
            var html = MarkdownTopicRenderer.RenderPage("Inline $x$\n\n$$\nx^2\n$$", AppDomain.CurrentDomain.BaseDirectory);

            StringAssert.Contains("<span class=\"math\">", html);
            StringAssert.Contains("<div class=\"math\">", html);
        }

        [Test]
        public void RenderPage_ConfiguresMathJaxBeforeLoadingTheLibrary()
        {
            var html = MarkdownTopicRenderer.RenderPage("Inline $x$", AppDomain.CurrentDomain.BaseDirectory);
            var configurationIndex = html.IndexOf("<script type=\"text/x-mathjax-config\">", StringComparison.Ordinal);
            var libraryIndex = html.IndexOf("MathJax.js?config=TeX-AMS_HTML", StringComparison.Ordinal);

            StringAssert.Contains(@"inlineMath:[['\\(','\\)']]", html);
            StringAssert.Contains(@"displayMath:[['\\[','\\]']]", html);
            Assert.IsTrue(configurationIndex >= 0 && configurationIndex < libraryIndex,
                "MathJax configuration must be declared before loading the library.");
        }

        [Test]
        public void TryResolveTopicLink_ResolvesAbsoluteAndRelativeMarkdownLinks()
        {
            var topicsFolder = MarkdownTopicRenderer.FindTopicsFolder(AppDomain.CurrentDomain.BaseDirectory);
            Assert.IsNotNull(topicsFolder);
            var currentFile = Path.Combine(topicsFolder, "Gears-Overview.md");
            var expectedPath = Path.GetFullPath(Path.Combine(topicsFolder, "Symbols.md"));
            string filePath;
            string fragment;

            Assert.IsTrue(MarkdownTopicRenderer.TryResolveTopicLink(new Uri(expectedPath).AbsoluteUri, currentFile, topicsFolder, out filePath, out fragment));
            Assert.AreEqual(expectedPath, filePath);
            Assert.IsNull(fragment);

            Assert.IsTrue(MarkdownTopicRenderer.TryResolveTopicLink("Symbols.md", currentFile, topicsFolder, out filePath, out fragment));
            Assert.AreEqual(expectedPath, filePath);
            Assert.IsNull(fragment);
        }

        [Test]
        public void TryResolveTopicLink_ResolvesFragmentAndMarkdownFragmentLinks()
        {
            var topicsFolder = MarkdownTopicRenderer.FindTopicsFolder(AppDomain.CurrentDomain.BaseDirectory);
            Assert.IsNotNull(topicsFolder);
            var currentFile = Path.Combine(topicsFolder, "Gears-Overview.md");
            var expectedPath = Path.GetFullPath(Path.Combine(topicsFolder, "Symbols.md"));
            string filePath;
            string fragment;

            Assert.IsTrue(MarkdownTopicRenderer.TryResolveTopicLink("#overview", currentFile, topicsFolder, out filePath, out fragment));
            Assert.AreEqual(Path.GetFullPath(currentFile), filePath);
            Assert.AreEqual("overview", fragment);

            var topicsFolderUri = new Uri(Path.GetFullPath(topicsFolder) + Path.DirectorySeparatorChar).AbsoluteUri;
            Assert.IsTrue(MarkdownTopicRenderer.TryResolveTopicLink(topicsFolderUri + "#overview", currentFile, topicsFolder, out filePath, out fragment));
            Assert.AreEqual(Path.GetFullPath(currentFile), filePath);
            Assert.AreEqual("overview", fragment);

            Assert.IsTrue(MarkdownTopicRenderer.TryResolveTopicLink("Symbols.md#tooth-profile", currentFile, topicsFolder, out filePath, out fragment));
            Assert.AreEqual(expectedPath, filePath);
            Assert.AreEqual("tooth-profile", fragment);

            Assert.IsTrue(MarkdownTopicRenderer.TryResolveTopicLink("about:blank#overview", currentFile, topicsFolder, out filePath, out fragment));
            Assert.AreEqual(Path.GetFullPath(currentFile), filePath);
            Assert.AreEqual("overview", fragment);
        }

        [TestCase("https://example.com/reference.md")]
        [TestCase("../outside.md")]
        public void TryResolveTopicLink_RejectsExternalAndOutOfFolderLinks(string href)
        {
            var topicsFolder = MarkdownTopicRenderer.FindTopicsFolder(AppDomain.CurrentDomain.BaseDirectory);
            Assert.IsNotNull(topicsFolder);
            string filePath;
            string fragment;

            Assert.IsFalse(MarkdownTopicRenderer.TryResolveTopicLink(href, Path.Combine(topicsFolder, "Gears-Overview.md"), topicsFolder, out filePath, out fragment));
            Assert.IsNull(filePath);
            Assert.IsNull(fragment);
        }

        [Test]
        public void ReadTableOfContents_UsesOverviewAndItsFourteenChildTopics()
        {
            var topicsFolder = MarkdownTopicRenderer.FindTopicsFolder(AppDomain.CurrentDomain.BaseDirectory);
            Assert.IsNotNull(topicsFolder);

            var contents = MarkdownTopicRenderer.ReadTableOfContents(topicsFolder);

            Assert.AreEqual("Gears Technical Reference", contents[0].Title);
            Assert.AreEqual(14, contents[0].Children.Count);
        }

        [Test]
        public void FindTopicsFolder_ReturnsNullWhenNoTopicsExist()
        {
            Assert.IsNull(MarkdownTopicRenderer.FindTopicsFolder(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
        }
    }
}
