using System;
using System.Globalization;
using Lira.Objects;

namespace Lira.Grouping;

public sealed class WorklogDayGrouper(DateSelector date) : WorklogDateGrouperBase<DateTime>(date)
{
    public static WorklogDayGrouper Started { get; } = new(DateSelector.Started);
    public static WorklogDayGrouper Created { get; } = new(DateSelector.Created);
    public static WorklogDayGrouper Updated { get; } = new(DateSelector.Updated);

    public override string Name { get; } = "Day";
    public override DateTime GetGenericPropertyValue(Worklog? obj) => GetDate(obj).Date;
    public override int CompareProperties(DateTime x, DateTime y) => x.CompareTo(y);
    public override string GetDisplay(Worklog? obj) => GetDate(obj).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
