using System.Diagnostics.CodeAnalysis;

namespace ConsoleMenu;

public record MenuItem
{
    private string? _tooltip;

    public required string Name { get; init; }
    public string? Tooltip
    {
        get => _tooltip; init
        {
            TooltipLines = value?.Split(["\r\n", "\n",  "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.RemoveEmptyEntries) ?? [];
            TooltipHeight = TooltipLines.Length;
            TooltipWidth = TooltipLines.Length == 0 ? 0 : TooltipLines.Max(x => x.Length);
            _tooltip = string.Join(Environment.NewLine, TooltipLines);
        }
    }
    public string[] TooltipLines { get; private init; } = [];
    public int TooltipHeight { get; private init; }
    public int TooltipWidth { get; private init; }
    public required object? Payload { get; init; }
    public bool HasTooltip => !string.IsNullOrEmpty(Tooltip);
    [SetsRequiredMembers]
    public MenuItem(string name, object? payload, string? tooltip = null)
    {
        Name = name;
        Tooltip = tooltip;
        Payload = payload;
    }
}
