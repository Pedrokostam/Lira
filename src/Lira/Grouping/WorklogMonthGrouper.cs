using System.Globalization;
using Lira.Objects;

namespace Lira.Grouping;

public sealed class WorklogMonthGrouper(DateSelector date) : WorklogDateGrouperBase<int>(date)
{
    public static WorklogMonthGrouper Started { get; } = new(DateSelector.Started);
    public static WorklogMonthGrouper Created { get; } = new(DateSelector.Created);
    public static WorklogMonthGrouper Updated { get; } = new(DateSelector.Updated);

    public override string Name { get; } = "Month";
    public override int GetGenericPropertyValue(Worklog? obj)
    {
        var accDate = GetDate(obj);
        return accDate.Year * 100 + accDate.Month;
    }
    public override int CompareProperties(int x, int y) => x.CompareTo(y);
    public override string GetDisplay(Worklog? obj) => GetDate(obj).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
}
