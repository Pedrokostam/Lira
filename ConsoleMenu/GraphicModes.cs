namespace ConsoleMenu;

[Flags]
public enum GraphicModes
{
    None = 0,
    Reset = 1 << 1,
    Bold = 1 << 2,
    Italics = 1 << 3,
    Dim = 1 << 4,
    Invert = 1 << 5,
    Deactivate = 1 << 30,
}
