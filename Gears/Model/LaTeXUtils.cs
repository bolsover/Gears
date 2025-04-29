using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WpfMath;
using WpfMath.Parsers;
using XamlMath;

namespace Bolsover.Gears.Model;

/// <summary>
/// Provides utility methods for processing LaTeX strings and generating image representations.
/// </summary>
public static class LaTeXUtils
{
    private static readonly TexFormulaParser Parser = WpfTeXFormulaParser.Instance;

    private static Image ByteArrayToImage(byte[] byteArrayIn)
    {
        Image returnImage = null;
        try
        {
            var ms = new MemoryStream(byteArrayIn, 0, byteArrayIn.Length);
            ms.Write(byteArrayIn, 0, byteArrayIn.Length);
            returnImage = Image.FromStream(ms, true, false); //Exception occurs here
        }
        catch
        {
            // ignored
        }

        return returnImage;
    }


    /// <summary>
    /// Creates an image representation of a LaTeX string.
    /// </summary>
    /// <param name="latex">The LaTeX string to be parsed and converted into an image.</param>
    /// <returns>An <see cref="Image"/> object generated from the parsed LaTeX string, or null if parsing fails.</returns>
    public static Image CreateImageFromLatex(string latex)
    {
        TexFormula formula;
        try
        {
            formula = Parser.Parse(latex);
        }
        catch (Exception)
        {
            MessageBox.Show("Error parsing latex" + latex);
            return null;
        }

        var pngBytes = formula.RenderToPng(17.0, 0.0, 0.0, "Cambria");
        return ByteArrayToImage(pngBytes);
    }
}