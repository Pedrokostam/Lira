using Lira.Extensions;
using Lira.Grouping;
using static Lira.Test.TestData;

namespace Lira.Test;

public class GroupingTests
{
    private static readonly TimeSpan Plus1 = TimeSpan.FromHours(1);
    private static readonly TimeSpan OneTick = new(1);

    [Theory]
    [InlineData(2024, 1, 18, 2024, 1, 14)] // Thursday
    [InlineData(2024, 1, 14, 2024, 1, 14)] // Sunday
    [InlineData(2024, 1, 20, 2024, 1, 14)] // Saturday
    [InlineData(2024, 1, 2, 2023, 12, 31)] // crosses the year
    public void StartOfWeekIsSunday(int y, int m, int d, int ey, int em, int ed)
    {
        var date = new DateTimeOffset(y, m, d, 15, 45, 0, Plus1);
        Assert.Equal(new DateTimeOffset(ey, em, ed, 0, 0, 0, Plus1), date.StartOfWeek());
        Assert.Equal(new DateTimeOffset(ey, em, ed, 0, 0, 0, Plus1).AddDays(7) - OneTick, date.EndOfWeek());
    }

    [Fact]
    public void DayBoundaries()
    {
        var date = new DateTimeOffset(2024, 1, 15, 15, 45, 0, Plus1);
        Assert.Equal(new DateTimeOffset(2024, 1, 15, 0, 0, 0, Plus1), date.StartOfDay());
        Assert.Equal(new DateTimeOffset(2024, 1, 16, 0, 0, 0, Plus1) - OneTick, date.EndOfDay());
    }

    [Theory]
    [InlineData(2024, 2, 10, 29)]
    [InlineData(2023, 2, 10, 28)]
    [InlineData(2024, 1, 31, 31)]
    [InlineData(2024, 12, 31, 31)]
    public void MonthBoundaries(int y, int m, int d, int lastDay)
    {
        var date = new DateTimeOffset(y, m, d, 15, 45, 0, Plus1);
        Assert.Equal(new DateTimeOffset(y, m, 1, 0, 0, 0, Plus1), date.StartOfMonth());
        Assert.Equal(new DateTimeOffset(y, m, lastDay, 0, 0, 0, Plus1).AddDays(1) - OneTick, date.EndOfMonth());
    }

    [Fact]
    public void YearBoundaries()
    {
        var date = new DateTimeOffset(2024, 7, 4, 15, 45, 0, Plus1);
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 0, 0, 0, Plus1), date.StartOfYear());
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, Plus1) - OneTick, date.EndOfYear());
    }

    [Fact]
    public void WeekGrouperDisplaysIsoWeek()
    {
        var log = Worklog("alice", new DateTimeOffset(2024, 1, 18, 10, 0, 0, Plus1), Plus1);
        Assert.Equal("2024-01-15 (week 3)", WorklogWeekGrouper.Started.GetDisplay(log));
    }

    // 2024-12-30 is in ISO week 1 of 2025, not week 1 of 2024.
    [Fact]
    public void WeekGrouperSeparatesWeekOneOfDifferentYears()
    {
        var january = Worklog("alice", new DateTimeOffset(2024, 1, 2, 10, 0, 0, Plus1), Plus1);
        var december = Worklog("alice", new DateTimeOffset(2024, 12, 30, 10, 0, 0, Plus1), Plus1);
        Assert.NotEqual(0, WorklogWeekGrouper.Started.Compare(january, december));
        Assert.Equal("2024-12-30 (week 1)", WorklogWeekGrouper.Started.GetDisplay(december));
    }

    // 2021-01-01 is in ISO week 53 of 2020.
    [Fact]
    public void WeekGrouperHandlesWeek53OfPreviousYear()
    {
        var log = Worklog("alice", new DateTimeOffset(2021, 1, 1, 10, 0, 0, Plus1), Plus1);
        Assert.Equal("2020-12-28 (week 53)", WorklogWeekGrouper.Started.GetDisplay(log));
    }

    [Fact]
    public void DayAndMonthGrouperDisplay()
    {
        var log = Worklog("alice", new DateTimeOffset(2024, 1, 15, 23, 59, 0, Plus1), Plus1);
        Assert.Equal("2024-01-15", WorklogDayGrouper.Started.GetDisplay(log));
        Assert.Equal("January 2024", WorklogMonthGrouper.Started.GetDisplay(log));
        Assert.Equal("2024", WorklogYearGrouper.Started.GetDisplay(log));
    }

    [Fact]
    public void GroupsByAuthorThenDayAndSumsTime()
    {
        var day1 = new DateTimeOffset(2024, 1, 15, 9, 0, 0, Plus1);
        var day2 = day1.AddDays(1);
        var logs = new[]
        {
            Worklog("bob", day1, TimeSpan.FromHours(2)),
            Worklog("alice", day2, TimeSpan.FromHours(1)),
            Worklog("alice", day1, TimeSpan.FromMinutes(30)),
            Worklog("alice", day1.AddHours(5), TimeSpan.FromHours(1)),
        };
        var calculator = new WorklogGroupingTimeSummator { WorklogAuthorGrouper.Instance, WorklogDayGrouper.Started };

        var groups = calculator.Group(logs).ToList();

        Assert.Equal(["ALICE > 2024-01-15", "ALICE > 2024-01-16", "BOB > 2024-01-15"], groups.Select(x => x.Header));
        Assert.Equal([TimeSpan.FromMinutes(90), TimeSpan.FromHours(1), TimeSpan.FromHours(2)], groups.Select(x => x.CalculatedValue));
        Assert.Equal(["Author", "Day"], groups[0].Columns);
        Assert.Equal(2, groups[0].Items.Length);
    }

    [Fact]
    public void NoGroupersGivesSingleGroup()
    {
        var day = new DateTimeOffset(2024, 1, 15, 9, 0, 0, Plus1);
        var logs = new[]
        {
            Worklog("alice", day, TimeSpan.FromHours(1)),
            Worklog("bob", day.AddDays(3), TimeSpan.FromHours(2)),
        };

        var group = Assert.Single(new WorklogGroupingTimeSummator().Group(logs));

        Assert.Equal(TimeSpan.FromHours(3), group.CalculatedValue);
        Assert.Equal(2, group.Items.Length);
    }
}
