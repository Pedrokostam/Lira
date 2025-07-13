using System;
using Lira.Objects;

namespace Lira.Grouping;

public sealed class WorklogIssueGrouper : Grouper<Worklog?,IssueCommon?>
{
    public static WorklogIssueGrouper Instance { get; } = new();
  
    public override string Name { get; } = "Issue";

    public override IssueCommon? GetGenericPropertyValue(Worklog? obj) => obj?.Issue;

    public override int CompareProperties(IssueCommon? x, IssueCommon? y) => StringComparer.OrdinalIgnoreCase.Compare(x?.Key, y?.Key);

    public override string GetDisplay(Worklog? obj) => obj is null ? "No issue" : obj.Issue.Key;
}
