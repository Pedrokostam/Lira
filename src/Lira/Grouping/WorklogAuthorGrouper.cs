using System;
using Lira.Objects;

namespace Lira.Grouping;

public sealed class WorklogAuthorGrouper : Grouper<Worklog?, string?>
{
    public static WorklogAuthorGrouper Instance { get; } = new();
   
    public override string Name { get; } = "Author";

    public override string? GetGenericPropertyValue(Worklog? obj) => obj?.Author.Name;

    public override int CompareProperties(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Compare(x, y);

    public override string GetDisplay(Worklog? obj) => obj?.Author.DisplayName ?? "No author";
}
