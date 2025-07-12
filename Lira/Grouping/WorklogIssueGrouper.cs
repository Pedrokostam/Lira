using System;
using Lira.Objects;

namespace Lira.Grouping;

public sealed class WorklogIssueGrouper : Grouper<Worklog?,string?>
{
    public static WorklogIssueGrouper Instance { get; } = new();
  
    public override string Name { get; } = "Issue";

    public override string? GetGenericPropertyValue(Worklog? obj) => obj?.Issue.Key;

    public override int CompareProperties(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Compare(x, y);

    public override string GetDisplay(Worklog? obj) => obj is null ? "No issue" : $"{obj.Issue.Key} {obj.Issue.Summary}";
}
