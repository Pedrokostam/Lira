using System.Text.Json;
using Lira.Objects;

namespace Lira.Test;

public class ConverterTests
{
    private static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonHelper.Options)!;
    private static string Write<T>(T value) => JsonSerializer.Serialize(value, JsonHelper.Options);

    [Theory]
    [InlineData("2016-03-16T04:22:37.471+0000", 0)]
    [InlineData("2016-03-16T04:22:37.471+0100", 60)]
    [InlineData("2016-03-16T04:22:37.471-0500", -300)]
    [InlineData("2016-03-16T04:22:37.471+0530", 330)]
    public void ReadsJiraDate(string jiraDate, int offsetMinutes)
    {
        var expected = new DateTimeOffset(2016, 3, 16, 4, 22, 37, 471, TimeSpan.FromMinutes(offsetMinutes));
        var actual = Read<DateTimeOffset>($"\"{jiraDate}\"");
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Offset, actual.Offset);
    }

    [Theory]
    [InlineData(60, "2024-01-15T10:30:00.123+0100")]
    [InlineData(-300, "2024-01-15T10:30:00.123-0500")]
    [InlineData(330, "2024-01-15T10:30:00.123+0530")]
    [InlineData(0, "2024-01-15T10:30:00.123+0000")]
    public void WritesJiraDateWithoutColonInOffset(int offsetMinutes, string expected)
    {
        var date = new DateTimeOffset(2024, 1, 15, 10, 30, 0, 123, TimeSpan.FromMinutes(offsetMinutes));
        // the default encoder escapes '+' as +, so compare the decoded string
        Assert.Equal(expected, JsonSerializer.Deserialize<string>(Write(date)));
    }

    [Fact]
    public void NullJiraDateReadsAsMinValue()
    {
        Assert.Equal(DateTimeOffset.MinValue, Read<DateTimeOffset>("null"));
    }

    [Fact]
    public void ReadsSecondsAsTimeSpan()
    {
        Assert.Equal(new TimeSpan(3, 20, 0), Read<TimeSpan>("12000"));
    }

    [Fact]
    public void WritesTimeSpanAsWholeSeconds()
    {
        Assert.Equal("90", Write(TimeSpan.FromSeconds(90.7)));
    }

    [Theory]
    [InlineData("Europe/Warsaw")]
    [InlineData("Australia/Sydney")]
    [InlineData("America/New_York")]
    public void TimeZoneRoundTrips(string iana)
    {
        var tz = Read<TimeZoneInfo?>($"\"{iana}\"");
        Assert.NotNull(tz);
        Assert.Equal($"\"{iana}\"", Write(tz));
    }

    [Fact]
    public void UnknownTimeZoneReadsAsNull()
    {
        Assert.Null(Read<TimeZoneInfo?>("\"Mars/Olympus_Mons\""));
    }

    // Shape of GET /rest/api/2/issue/{issueIdOrKey}/worklog/{id} from the Jira Server REST docs.
    [Fact]
    public void ReadsJiraWorklog()
    {
        const string json = """
            {
              "self": "https://jira.example.com/rest/api/2/issue/10010/worklog/10000",
              "author": {
                "self": "https://jira.example.com/rest/api/2/user?username=fred",
                "name": "fred",
                "key": "fred",
                "displayName": "Fred F. User",
                "emailAddress": "fred@example.com",
                "active": false,
                "timeZone": "Australia/Sydney"
              },
              "updateAuthor": {
                "self": "https://jira.example.com/rest/api/2/user?username=fred",
                "name": "fred",
                "key": "fred",
                "displayName": "Fred F. User",
                "active": false
              },
              "comment": "I did some work here.",
              "created": "2016-03-16T04:22:37.356+0000",
              "updated": "2016-03-16T04:22:37.356+0000",
              "started": "2016-03-16T04:22:37.471+1100",
              "timeSpent": "3h 20m",
              "timeSpentSeconds": 12000,
              "id": "100028",
              "issueId": "10002"
            }
            """;
        var worklog = Read<Worklog>(json);

        Assert.Equal("fred", worklog.Author.Name);
        Assert.Equal("Fred F. User", worklog.Author.DisplayName);
        Assert.Equal("fred@example.com", worklog.Author.EmailAddress);
        Assert.Null(worklog.UpdateAuthor.EmailAddress);
        Assert.NotNull(worklog.Author.TimeZone);
        Assert.Null(worklog.UpdateAuthor.TimeZone);
        Assert.Equal("I did some work here.", worklog.Comment);
        Assert.Equal(new DateTimeOffset(2016, 3, 16, 4, 22, 37, 471, TimeSpan.FromHours(11)), worklog.Started);
        Assert.Equal(TimeSpan.FromHours(11), worklog.Started.Offset);
        Assert.Equal(new TimeSpan(3, 20, 0), worklog.TimeSpent);
        Assert.Equal("100028", worklog.ID);
        Assert.Equal("10002", worklog.IssueId);
        Assert.Equal(new Uri("https://jira.example.com/rest/api/2/issue/10010/worklog/10000"), worklog.SelfLink);
    }
}
