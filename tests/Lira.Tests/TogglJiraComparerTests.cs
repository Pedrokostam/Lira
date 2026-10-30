using Lira.Toggl;

namespace Lira.Tests;

public class TogglJiraComparerTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;
    private static readonly DateTimeOffset Day = new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    private static TogglTimeEntry Toggl(double startHour, double hours, string? description, string? project = null) => new()
    {
        Start = Day.AddHours(startHour),
        Duration = TimeSpan.FromHours(hours),
        Description = description,
        ProjectName = project,
    };

    private static Objects.Worklog Jira(double startHour, double hours, string comment = "", string issue = "ABC-123")
        => TestData.Worklog("me", Day.AddHours(startHour), TimeSpan.FromHours(hours), issueKey: issue, comment: comment);

    private static TogglJiraComparison Compare(IEnumerable<TogglTimeEntry> toggl, IEnumerable<Objects.Worklog> jira, bool forceAggregate = false, DateTimeOffset? now = null)
        => TogglJiraComparer.Compare(toggl, jira, Utc, forceAggregate, now ?? Now);

    // 6h in 5 Toggl entries
    private static List<TogglTimeEntry> SixHours() =>
    [
        Toggl(8, 1, "[ABC-123] design"),
        Toggl(9, 1, "[ABC-123] review"),
        Toggl(10, 1, "[ABC-123] code"),
        Toggl(11, 1, "[ABC-123] test"),
        Toggl(12, 2, "[ABC-123] docs"),
    ];

    [Fact]
    public void WithinToleranceIsMatch()
    {
        var result = Compare([Toggl(8, 2.2, "[ABC-123] code")], [Jira(17, 2, "other")]);

        Assert.Empty(result.Suggestions);
        var row = Assert.Single(result.Summary);
        Assert.Equal(TogglSyncStatus.Match, row.Status);
    }

    [Fact]
    public void NonMatchingJiraLogsGiveOneAggregatedSuggestion()
    {
        var result = Compare(SixHours(), [Jira(17, 1, "something"), Jira(18, 1, "else")]);

        var suggestion = Assert.Single(result.Suggestions);
        Assert.Equal("ABC-123", suggestion.Issue);
        Assert.Equal(TimeSpan.FromHours(4), suggestion.Duration);
        Assert.Equal(Day.Date.AddHours(12), suggestion.Started.DateTime);
        var row = Assert.Single(result.Summary);
        Assert.Equal(TogglSyncStatus.Missing, row.Status);
        Assert.Equal((5, 2), (row.TogglCount, row.JiraCount));
        Assert.Equal(TimeSpan.FromHours(4), row.Difference);
    }

    [Fact]
    public void MatchingJiraLogsGiveOneToOneSuggestions()
    {
        var result = Compare(SixHours(), [Jira(17, 1, "Design"), Jira(18, 1.1, "review")]);

        Assert.Equal(["code", "test", "docs"], result.Suggestions.Select(x => x.Comment));
        Assert.Equal([Day.AddHours(10), Day.AddHours(11), Day.AddHours(12)], result.Suggestions.Select(x => x.Started));
    }

    [Fact]
    public void ForceAggregateGivesOneSuggestion()
    {
        var result = Compare(SixHours(), [Jira(17, 1, "design"), Jira(18, 1, "review")], forceAggregate: true);

        var suggestion = Assert.Single(result.Suggestions);
        Assert.Equal(TimeSpan.FromHours(4), suggestion.Duration);
    }

    [Fact]
    public void EmptyJiraCopiesDescribedEntriesAndAggregatesTheRest()
    {
        var result = Compare([Toggl(8, 1, "[ABC-123] code"), Toggl(9, 0.5, "ABC-123"), Toggl(10, 0.25, "[ABC-123]")], []);

        Assert.Equal(2, result.Suggestions.Count);
        var copied = result.Suggestions.Single(x => x.Comment == "code");
        Assert.Equal(Day.AddHours(8), copied.Started);
        Assert.Equal(TimeSpan.FromHours(1), copied.Duration);
        var aggregated = result.Suggestions.Single(x => x.Comment is null);
        Assert.Equal(TimeSpan.FromMinutes(45), aggregated.Duration);
        Assert.Equal(Day.AddHours(12), aggregated.Started);
    }

    [Fact]
    public void AggregatedSuggestionDoesNotCrossMidnight()
    {
        var result = Compare([Toggl(8, 2, "ABC-123")], [], now: Now.Date.AddHours(23));

        var suggestion = Assert.Single(result.Suggestions);
        Assert.Equal(Day.AddHours(22), suggestion.Started);
    }

    [Fact]
    public void TogglShortAndJiraOnlyAreReportedWithoutSuggestions()
    {
        var result = Compare([Toggl(8, 1, "[ABC-123] code")], [Jira(17, 3), Jira(17, 2, issue: "XYZ-999")]);

        Assert.Empty(result.Suggestions);
        Assert.All(result.Summary, x => Assert.Equal(TogglSyncStatus.TogglShort, x.Status));
        Assert.Equal(["ABC-123", "XYZ-999"], result.Summary.Select(x => x.Issue));
    }

    [Fact]
    public void UnassignedEntriesAreCounted()
    {
        var result = Compare([Toggl(8, 1, "meeting"), Toggl(9, 0.5, null), Toggl(10, 1, "[ABC-123]")], [Jira(17, 1)]);

        var row = Assert.Single(result.Summary, x => x.Issue is null);
        Assert.Equal(TogglSyncStatus.Unassigned, row.Status);
        Assert.Equal(2, row.TogglCount);
        Assert.Equal(TimeSpan.FromHours(1.5), row.TogglTime);
    }

    [Fact]
    public void DaysFollowTimeZone()
    {
        var entry = Toggl(23.5, 1, "[ABC-123] late");

        var result = TogglJiraComparer.Compare([entry], [], TestData.Warsaw, now: Now);

        Assert.Equal(DateOnly.FromDateTime(Day.Date.AddDays(1)), Assert.Single(result.Summary).Date);
    }

    [Fact]
    public void ProjectIssueIsUsedWhenDescriptionHasNone()
    {
        var result = Compare([Toggl(8, 1, "code", project: "Project [ABC-123]")], [Jira(17, 1, "CODE")]);

        Assert.Equal(TogglSyncStatus.Match, Assert.Single(result.Summary).Status);
    }
}
