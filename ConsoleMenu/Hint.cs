namespace ConsoleMenu;

[Flags]
public enum Hint
{
    None = 0b0,
    Validation = 0b1,
    ParsedOutput = 0b10,
    All = Validation | ParsedOutput,
}
