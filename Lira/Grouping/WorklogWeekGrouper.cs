using System;
using System.Globalization;
using Lira.Extensions;
using Lira.Objects;

namespace Lira.Grouping;

public sealed class WorklogWeekGrouper(DateSelector date) : WorklogDateGrouperBase<int>(date)
{
    public static WorklogWeekGrouper Started { get; } = new(DateSelector.Started);
    public static WorklogWeekGrouper Created { get; } = new(DateSelector.Created);
    public static WorklogWeekGrouper Updated { get; } = new(DateSelector.Updated);
    public override string Name { get; } = "Week";
    public override int GetGenericPropertyValue(Worklog? obj)
    {
        var AccDate = GetDate(obj);
        return AccDate.Year * 100 + AccDate.WeekNumber();
    }
    public override int CompareProperties(int x, int y) => x.CompareTo(y);
    public override string GetDisplay(Worklog? obj)
    {
        var accDate = GetDate(obj);

        var dateStartString = accDate.WeekStart().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"{dateStartString} (week {accDate.WeekNumber()})";
    }
}
