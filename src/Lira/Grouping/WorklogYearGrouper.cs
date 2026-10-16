using System.Globalization;
using Lira.Objects;

namespace Lira.Grouping;

public sealed class WorklogYearGrouper(DateSelector date) : WorklogDateGrouperBase<int>(date)
{
    public static WorklogYearGrouper Started { get; } = new(DateSelector.Started);
    public static WorklogYearGrouper Created { get; } = new(DateSelector.Created);
    public static WorklogYearGrouper Updated { get; } = new(DateSelector.Updated);

    public override string Name { get; } = "Year";
    public override int GetGenericPropertyValue(Worklog? obj) => GetDate(obj).Year;
    public override int CompareProperties(int x, int y) => x.CompareTo(y);
    public override string GetDisplay(Worklog? obj) => GetDate(obj).Year.ToString(CultureInfo.InvariantCulture);
}
