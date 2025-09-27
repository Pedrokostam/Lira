using System.Text.RegularExpressions;

namespace ConsoleMenu;

public partial record Part(string Text, GraphicModes StartMode = GraphicModes.None, ConsoleColor? Color = null)
{
    private static string? GetAnsiColor(ConsoleColor? color)
    {
        return color switch
        {

            ConsoleColor.Black => "30",
            ConsoleColor.DarkBlue => "34",
            ConsoleColor.DarkGreen => "32",
            ConsoleColor.DarkCyan => "36",
            ConsoleColor.DarkRed => "31",
            ConsoleColor.DarkMagenta => "35",
            ConsoleColor.DarkYellow => "33",
            ConsoleColor.Gray => "37",
            ConsoleColor.DarkGray => "90",
            ConsoleColor.Blue => "94",
            ConsoleColor.Green => "92",
            ConsoleColor.Cyan => "96",
            ConsoleColor.Red => "91",
            ConsoleColor.Magenta => "95",
            ConsoleColor.Yellow => "93",
            ConsoleColor.White => "97",
            _ => null,
        };
    }
    private record ModeValue(GraphicModes Mode, string Activate, string Deactivate)
    {
        public string Get(bool deactivate) => deactivate ? Deactivate : Activate;
    };
    private static readonly ModeValue[] Values = [
        new(GraphicModes.Bold,"1","22"),
        new(GraphicModes.Dim,"2","22"),
        new(GraphicModes.Italics,"3","23"),
        new(GraphicModes.Invert,"7","27"),
        ];

    private const string EscapeStart = "\x001b[";
    private const string ColorReset = "39";
    private const string Reset = "0";
    private const string EscapeEnd = "m";
    private const string EscapeSeparator = ";";
    private const string EscapeReset = EscapeStart+Reset+EscapeEnd;

    public static (string Activation, string Deactivation) ToSequence(GraphicModes mode, ConsoleColor? consoleColor)
    {
        if (mode == GraphicModes.None && consoleColor is null)
        {
            return (string.Empty, string.Empty);
        }
        var buffer = new List<ModeValue>(Values.Length + 2);
        var bufferStart = new List<string>(Values.Length + 1);
        //var bufferEnd = new List<string>(Values.Length + 1);
        for (int i = 0; i < Values.Length; i++)
        {
            if (!mode.HasFlag(Values[i].Mode))
            {
                continue;
            }
            bufferStart.Add(Values[i].Activate.ToString());
            //bufferEnd.Add(Values[i].Deactivate.ToString());
        }
        if (GetAnsiColor(consoleColor) is string colString)
        {
            bufferStart.Add(colString);
            //bufferEnd.Add(ColorReset);
        }
        if (bufferStart.Count == 0)
        {
            return (string.Empty,string.Empty);
        }
        if (buffer.Count == 1)
        {
            return (EscapeStart + bufferStart[0] + EscapeEnd, EscapeReset);
        }
        var start = EscapeStart + string.Join(EscapeSeparator,bufferStart) + EscapeEnd;
        return (start, EscapeReset);
    }
    //public static implicit operator Part(string txt) => new Part(txt);
    public int Length => Text.Length;
    public string GetConsoleString(bool plainText = false)
    {
        if (plainText)
        {
            return Text;
        }
        var bounds = ToSequence(StartMode,consoleColor:null);
        return bounds.Activation + Text + bounds.Deactivation;
    }
    public bool IsMultiline() => NewLineFinder().IsMatch(Text);
    public IEnumerable<Part> SplitLines()
    {
        var texts = NewLineFinder().Split(Text);
        if (texts.Length == 0)
        {
            yield return this;
            yield break;
        }
        foreach (var item in texts)
        {
            yield return this with { Text = item.TrimEnd() };
        }
    }
    [GeneratedRegex(@"\r?\n")]
    public static partial Regex NewLineFinder();
}
