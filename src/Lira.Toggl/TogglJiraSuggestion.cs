namespace Lira.Toggl
{
    public record TogglJiraSuggestion(string Issue, DateTimeOffset Started, TimeSpan Duration, string? Comment);
}
