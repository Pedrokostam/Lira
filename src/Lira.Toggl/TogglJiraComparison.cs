namespace Lira.Toggl
{
    public readonly record struct TogglJiraComparison(IReadOnlyList<TogglJiraSuggestion> Suggestions, IReadOnlyList<TogglJiraDaySummary> Summary);
}
