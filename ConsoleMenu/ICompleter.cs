using System.Diagnostics.CodeAnalysis;

namespace ConsoleMenu;

public interface ICompleter
{
    public record Completion()
    {
        private readonly string? _tooltip;
        private readonly string? _listItem;
        public required string CompletionText { get; init; }
        [AllowNull]
        public string? Tooltip { get; init; }
        [AllowNull]
        public string ListItem { get => _listItem ?? CompletionText; init => _listItem = value; }
        [SetsRequiredMembers]
        public Completion(string completionText, string? listItem, string? tooltip) : this()
        {
            CompletionText = completionText;
            Tooltip = tooltip;
            ListItem = listItem;
        }
        [SetsRequiredMembers]
        public Completion(string completionText) : this(completionText, null, null) { }

    }
    IEnumerable<Completion> Complete(string item);
}
