namespace ConsoleMenu;

public record Part(string Text, GraphicModes StartMode = GraphicModes.None, GraphicModes EndMode = GraphicModes.None)
{
    private const string Escape = "\x001b[";
    private const string Reset = "\x001b[0m";

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
    public static string? ToSequence(GraphicModes mode)
    {
        if (mode == GraphicModes.None || mode == GraphicModes.Deactivate)
        {
            return null;
        }
        var buffer = new List<string>(Values.Length + 2);
        if (mode.HasFlag(GraphicModes.Reset))
        {
            return Reset;
        }
        bool deactivate = mode.HasFlag(GraphicModes.Deactivate);
        for (int i = 0; i < Values.Length; i++)
        {
            if (!mode.HasFlag(Values[i].Mode))
            {
                continue;
            }

            buffer.Add(Values[i].Get(deactivate));
        }
        if (buffer.Count == 1)
        {
            return Escape + buffer[0] + 'm';
        }
        return Escape + string.Join(';', buffer) + 'm';
    }
    public static implicit operator Part(string txt) => new Part(txt);
    public int Length => Text.Length;
    public string GetConsoleString(bool disableEscapeCodes = false)
    {
        if (disableEscapeCodes)
        {
            return Text;
        }
        var startBuff = ToSequence(StartMode);
        var endBuff = ToSequence(EndMode);
        return (startBuff, endBuff) switch
        {
            (null, null) => Text,
            (Reset, Reset or null) => Reset + Text,
            (_, _) => startBuff + Text + endBuff,
        };

    }
    public Part(string text, GraphicModes wrappingMode) : this(text, wrappingMode, GraphicModes.Reset)
    {

    }
}
