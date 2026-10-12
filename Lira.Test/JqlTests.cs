using Lira.Extensions;
using Lira.Jql;
using static Lira.Test.TestData;

namespace Lira.Test;

public class JqlTests
{
    private static readonly LiraClient WarsawClient = Client(Warsaw);

    [Fact]
    public void EmptyQueryIsEmpty()
    {
        Assert.Equal("", new JqlQuery().BuildQueryString(WarsawClient));
    }

    [Fact]
    public void SingleValueUsesEquals()
    {
        var query = new JqlQuery().WhereIssueIs("ABC-1");
        Assert.Equal("issue = \"ABC-1\"", query.BuildQueryString(WarsawClient));
    }

    [Fact]
    public void ManyValuesUseIn()
    {
        var query = new JqlQuery().WhereIssueStatusIs("Open", "In Progress");
        Assert.Equal("status IN (\"Open\", \"In Progress\")", query.BuildQueryString(WarsawClient));
    }

    [Fact]
    public void RejectedValuesUseNotEqualsAndNotIn()
    {
        Assert.Equal("labels != \"x\"", new JqlQuery().WhereIssueLabelsAreNot("x").BuildQueryString(WarsawClient));
        Assert.Equal("labels NOT IN (\"x\", \"y\")", new JqlQuery().WhereIssueLabelsAreNot("x", "y").BuildQueryString(WarsawClient));
    }

    [Fact]
    public void ConditionsAreJoinedWithAnd()
    {
        var query = new JqlQuery()
            .WhereIssueIs("ABC-1")
            .WhereIssueComponentsAre("api")
            .WhereIssueComponentsAreNot("ui", "docs")
            .WhereWorklogAuthorIs("alice", "bob");
        Assert.Equal(
            "issue = \"ABC-1\" AND component = \"api\" AND component NOT IN (\"ui\", \"docs\") AND worklogAuthor IN (\"alice\", \"bob\")",
            query.BuildQueryString(WarsawClient));
    }

    [Fact]
    public void QuotesInValuesAreEscaped()
    {
        var query = new JqlQuery().WhereIssueLabelsAre("say \"hi\"");
        Assert.Equal("labels = \"say \\\"hi\\\"\"", query.BuildQueryString(WarsawClient));
    }

    [Fact]
    public void BackslashesInValuesAreEscaped()
    {
        var query = new JqlQuery().WhereIssueLabelsAreNot(@"a\b", "c");
        Assert.Equal("labels NOT IN (\"a\\\\b\", \"c\")", query.BuildQueryString(WarsawClient));
    }

    [Fact]
    public void ManualDateIsConvertedToAccountTimezone()
    {
        var utc = new DateTimeOffset(2024, 1, 15, 23, 30, 0, TimeSpan.Zero);
        var query = new JqlQuery().WithWorklogsAfter(new JqlManualDate(utc));
        Assert.Equal("worklogDate >= \"2024-01-16 00:30\"", query.BuildQueryString(WarsawClient));
    }

    [Fact]
    public void DateRangeUsesBoundaries()
    {
        var query = new JqlQuery()
            .WithWorklogsAfter(JqlKeywordDate.StartOfMonth.WithOffset(-1).ToBounded(JqlDateBoundary.Exclusive))
            .WithWorklogsBefore(new JqlRelativeDate(JqlRelativeDate.Unit.Day, -1));
        Assert.Equal("worklogDate > StartOfMonth(-1) AND worklogDate <= -1d", query.BuildQueryString(WarsawClient));
    }

    [Fact]
    public void UpdatedOnUsesExactBoundary()
    {
        var query = new JqlQuery().WhereIssueUpdatedOn(JqlKeywordDate.StartOfDay);
        Assert.Equal("updated = StartOfDay(0)", query.BuildQueryString(WarsawClient));
    }

    // JQL relative dates for fields only accept w, d, h, m; M and y exist only as keyword-function increments.
    [Theory]
    [InlineData(JqlRelativeDate.Unit.Minutes)]
    [InlineData(JqlRelativeDate.Unit.Hour)]
    [InlineData(JqlRelativeDate.Unit.Day)]
    [InlineData(JqlRelativeDate.Unit.Week)]
    [InlineData(JqlRelativeDate.Unit.Month)]
    [InlineData(JqlRelativeDate.Unit.Year)]
    public void RelativeDateIsValidJql(JqlRelativeDate.Unit unit)
    {
        Assert.Matches("^(-?[0-9]+[wdhm]|\"[0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2}\")$", new JqlRelativeDate(unit, -2).GetJqlValue(Warsaw));
    }

    [Fact]
    public void MonthRelativeDateBecomesAbsoluteDateInAccountTimezone()
    {
        var expected = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow.AddMonths(-2), Warsaw);
        var query = new JqlQuery().WithWorklogsAfter(new JqlRelativeDate(JqlRelativeDate.Unit.Month, -2));
        // ponytail: minute-resolution race if the clock ticks over between the two UtcNow calls
        Assert.Equal($"worklogDate >= \"{expected:yyyy-MM-dd HH:mm}\"", query.BuildQueryString(WarsawClient));
    }

    [Theory]
    [InlineData("-1d", JqlRelativeDate.Unit.Day, -1)]
    [InlineData("2w", JqlRelativeDate.Unit.Week, 2)]
    [InlineData(" -3M ", JqlRelativeDate.Unit.Month, -3)]
    [InlineData("5m", JqlRelativeDate.Unit.Minutes, 5)]
    [InlineData("4H", JqlRelativeDate.Unit.Hour, 4)]
    [InlineData("-1", JqlRelativeDate.Unit.Day, -1)]
    public void ParsesRelativeDate(string input, JqlRelativeDate.Unit unit, int offset)
    {
        Assert.True(JqlRelativeDate.TryParse(input, out var date));
        Assert.Equal(unit, date.TimeUnit);
        Assert.Equal(offset, date.Offset);
    }

    [Theory]
    [InlineData("")]
    [InlineData("d")]
    [InlineData("1q")]
    public void RejectsInvalidRelativeDate(string input)
    {
        Assert.False(JqlRelativeDate.TryParse(input, out _));
    }

    [Theory]
    [InlineData(0, JqlRelativeDate.Unit.Day, "Today")]
    [InlineData(-1, JqlRelativeDate.Unit.Day, "1 day ago")]
    [InlineData(-3, JqlRelativeDate.Unit.Hour, "3 hours ago")]
    [InlineData(2, JqlRelativeDate.Unit.Week, "in 2 weeks")]
    public void RelativeDatePrettyForm(int offset, JqlRelativeDate.Unit unit, string expected)
    {
        Assert.Equal(expected, new JqlRelativeDate(unit, offset).PrettyForm());
    }

    [Theory]
    [InlineData("startOfDay", JqlKeywordDate.Keywords.StartOfDay, 0)]
    [InlineData("STARTOFMONTH(-1)", JqlKeywordDate.Keywords.StartOfMonth, -1)]
    [InlineData("endOfWeek(\"+2\")", JqlKeywordDate.Keywords.EndOfWeek, 2)]
    [InlineData("endOfYear('-3')", JqlKeywordDate.Keywords.EndOfYear, -3)]
    public void ParsesKeywordDate(string input, JqlKeywordDate.Keywords keyword, int offset)
    {
        Assert.True(JqlKeywordDate.TryParse(input, out var date));
        Assert.Equal(keyword, date.Keyword);
        Assert.Equal(offset, date.Offset);
    }

    [Fact]
    public void RejectsUnknownKeyword()
    {
        Assert.False(JqlKeywordDate.TryParse("startOfDecade(1)", out _));
    }

    [Theory]
    [InlineData("=", JqlOperator.Operators.Equals, "=")]
    [InlineData(" >= ", JqlOperator.Operators.GreaterThanEquals, ">=")]
    [InlineData("not in", JqlOperator.Operators.NotIn, "NOT IN")]
    [InlineData("Was Not", JqlOperator.Operators.WasNot, "WAS NOT")]
    [InlineData("does not contain", JqlOperator.Operators.DoesNotContain, "!~")]
    public void ParsesOperator(string input, JqlOperator.Operators expected, string queryForm)
    {
        Assert.True(JqlOperator.TryGetFromString(input, out var op));
        Assert.Equal(expected, op!.Value.Operator);
        Assert.Equal(queryForm, op.Value.QueryForm);
    }

    [Fact]
    public void RejectsUnknownOperator()
    {
        Assert.False(JqlOperator.TryGetFromString("<>", out _));
    }

    private static readonly DateTimeOffset Day = new(2024, 1, 15, 10, 0, 0, TimeSpan.FromHours(1));
    private static readonly Objects.Worklog[] Logs =
    [
        Worklog("alice", Day, TimeSpan.FromHours(1)),
        Worklog("bob", Day, TimeSpan.FromHours(1)),
    ];

    [Fact]
    public void FilterWithoutConditionsKeepsEverything()
    {
        Assert.Equal(2, new JqlQuery().FilterItems(Logs, WarsawClient).Count());
    }

    [Theory]
    [InlineData("alice")]
    [InlineData("ALICE")]
    [InlineData("Alice@Example.com")]
    public void FilterKeepsRequiredAuthorByNameKeyDisplayNameOrEmail(string author)
    {
        var result = new JqlQuery().WhereWorklogAuthorIs(author).FilterItems(Logs, WarsawClient).ToList();
        Assert.Equal("alice", Assert.Single(result).Author.Name);
    }

    private static Objects.IssueLite Issue(string key, string status, string labelsJson = "[]") => System.Text.Json.JsonSerializer.Deserialize<Objects.IssueLite>($$"""
        {
          "key": "{{key}}",
          "self": "https://jira.example.com/rest/api/2/issue/{{key}}",
          "fields": {
            "summary": "s",
            "labels": {{labelsJson}},
            "status": { "name": "{{status}}" },
            "reporter": { "self": "https://jira.example.com/rest/api/2/user?username=fred", "name": "fred", "key": "fred", "displayName": "Fred" },
            "creator": { "self": "https://jira.example.com/rest/api/2/user?username=fred", "name": "fred", "key": "fred", "displayName": "Fred" },
            "created": "2024-01-15T10:00:00.000+0100",
            "updated": "2024-01-15T10:00:00.000+0100"
          }
        }
        """, JsonHelper.Options)!;

    // Issue filters are declared for IssueCommon; they must also apply to its subtypes.
    [Fact]
    public void IssueFiltersApplyToIssueLite()
    {
        var issues = new[] { Issue("ABC-1", "Open"), Issue("ABC-2", "Closed") };
        var query = new JqlQuery().WhereIssueStatusIsNot("closed");

        Assert.Equal("ABC-1", Assert.Single(query.FilterItems(issues, WarsawClient)).Key);
        Assert.Equal(["ABC-1"], query.Filter(issues, WarsawClient).Cast<Objects.IssueLite>().Select(x => x.Key));
    }

    // Matches Jira: `labels IS EMPTY AND labels NOT IN ("kek", "lol")` returns nothing.
    [Fact]
    public void RejectionDropsIssuesWithEmptyField()
    {
        var issues = new[] { Issue("ABC-1", "Open", "[]"), Issue("ABC-2", "Open", """["kek"]"""), Issue("ABC-3", "Open", """["other"]""") };

        var labels = new JqlQuery().WhereIssueLabelsAreNot("kek", "lol").FilterItems(issues, WarsawClient);
        var assignee = new JqlQuery().WhereIssueAssigneeIsNot(["bob"]).FilterItems(issues, WarsawClient);

        Assert.Equal(["ABC-3"], labels.Select(x => x.Key));
        Assert.Empty(assignee);
    }

    [Fact]
    public void FilterDropsRejectedAuthor()
    {
        var result = new JqlQuery().WhereWorklogAuthorIsNot("alice").FilterItems(Logs, WarsawClient).ToList();
        Assert.Equal("bob", Assert.Single(result).Author.Name);
    }
}
