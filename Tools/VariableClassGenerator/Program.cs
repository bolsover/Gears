using System.Text;

// if (args.Length < 1)
// {
//     Console.Error.WriteLine("Usage: VariableClassGenerator <input.csv> [outputDir]");
//     Console.Error.WriteLine();
//     Console.Error.WriteLine("CSV columns (with header row): ClassName, Name, NCalc, Symbol, Formula, VariableRole");
//     return 1;
// }

var input = "F:\\Result_5.csv";
var output = "f:\\output";
string csvPath = input;
string outputDir = output;

// string csvPath = args[0];
// string outputDir = args.Length >= 2 ? args[1] : Path.GetDirectoryName(Path.GetFullPath(csvPath))!;

if (!File.Exists(csvPath))
{
    Console.Error.WriteLine($"File not found: {csvPath}");
    return 1;
}

Directory.CreateDirectory(outputDir);

string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
if (lines.Length < 2)
{
    Console.Error.WriteLine("CSV must have a header row and at least one data row.");
    return 1;
}

// Parse header to find column indices
string[] headers = ParseCsvLine(lines[0]);
int idxClassName    = IndexOf(headers, "ClassName");
int idxName         = IndexOf(headers, "Name");
int idxNCalc        = IndexOf(headers, "NCalc");
int idxSymbol       = IndexOf(headers, "Symbol");
int idxFormula      = IndexOf(headers, "Formula");
int idxRole         = IndexOf(headers, "VariableRole");

if (new[] { idxClassName, idxName, idxNCalc, idxSymbol, idxFormula, idxRole }.Any(i => i == -1))
{
    Console.Error.WriteLine("CSV header must contain: ClassName, Name, NCalc, Symbol, Formula, VariableRole");
    return 1;
}

int generated = 0;

for (int lineNum = 1; lineNum < lines.Length; lineNum++)
{
    string line = lines[lineNum].Trim();
    if (string.IsNullOrEmpty(line)) continue;

    string[] fields = ParseCsvLine(line);

    if (fields.Length <= Math.Max(idxClassName, Math.Max(idxName, Math.Max(idxNCalc, Math.Max(idxSymbol, Math.Max(idxFormula, idxRole))))))
    {
        Console.Error.WriteLine($"Line {lineNum + 1}: not enough fields, skipping.");
        continue;
    }

    string className    = fields[idxClassName].Trim();
    string name         = fields[idxName].Trim();
    string nCalc        = fields[idxNCalc].Trim();
    string symbol       = fields[idxSymbol].Trim();
    string formula      = fields[idxFormula].Trim();
    string role         = fields[idxRole].Trim();
    if (role == "1")
    {
        role = "Design";
    }
    else if (role == "0")
    {
        role = "Calculated";
    }

    if (string.IsNullOrEmpty(className))
    {
        Console.Error.WriteLine($"Line {lineNum + 1}: ClassName is empty, skipping.");
        continue;
    }

    string content = GenerateClass(className, name, nCalc, symbol, formula, role);
    string outputPath = Path.Combine(outputDir, $"{className}.cs");
    File.WriteAllText(outputPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    Console.WriteLine($"Generated: {outputPath}");
    generated++;
}

Console.WriteLine($"\n{generated} file(s) generated.");
return 0;

static string GenerateClass(string className, string name, string nCalc, string symbol, string formula, string role)
{
    return string.Format(
        "namespace Bolsover.Gears.Model.Variable;\n\n" +
        "public class {0} : IVariable\n" +
        "{{\n" +
        "    public double Value {{ get; set; }}\n" +
        "    public string Name {{ get; }} = @\"{1}\";\n" +
        "    public string NCalc {{ get; }} = @\"{2}\";\n" +
        "    public string Symbol {{ get; }} = @\"{3}\";\n" +
        "    public string Formula {{ get; }} = @\"{4}\";\n" +
        "    public VariableRole Role {{ get; set; }} = VariableRole.{5};\n" +
        "}}",
        className, name, nCalc, symbol, formula, role);
}

static int IndexOf(string[] headers, string name)
{
    for (int i = 0; i < headers.Length; i++)
        if (string.Equals(headers[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
            return i;
    return -1;
}

// RFC 4180-compliant CSV parser for a single line
static string[] ParseCsvLine(string line)
{
    var fields = new List<string>();
    var sb = new StringBuilder();
    bool inQuotes = false;
    int i = 0;

    while (i < line.Length)
    {
        char c = line[i];

        if (inQuotes)
        {
            if (c == '"')
            {
                // Peek ahead: doubled quote = escaped quote
                if (i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i += 2;
                }
                else
                {
                    inQuotes = false;
                    i++;
                }
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }
        else
        {
            if (c == '"')
            {
                inQuotes = true;
                i++;
            }
            else if (c == ',')
            {
                fields.Add(sb.ToString());
                sb.Clear();
                i++;
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }
    }

    fields.Add(sb.ToString());
    return fields.ToArray();
}
