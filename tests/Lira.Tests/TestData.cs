using System.Runtime.CompilerServices;
using Lira.Objects;

namespace Lira.Tests;

internal static class TestData
{
    public static UserDetails User(string name, TimeZoneInfo? timeZone = null) => new()
    {
        Name = name,
        Key = name.ToLowerInvariant(),
        DisplayName = name.ToUpperInvariant(),
        EmailAddress = $"{name}@example.com",
        TimeZone = timeZone,
        SelfLink = new Uri($"https://jira.example.com/rest/api/2/user?username={name}"),
    };

    public static Worklog Worklog(string author, DateTimeOffset started, TimeSpan timeSpent, string issueId = "10000", string issueKey = "TEST-100", string comment = "") => new()
    {
        Issue = Issue(issueKey),
        Comment = comment,
        Author = User(author),
        UpdateAuthor = User(author),
        Started = started,
        Created = started,
        Updated = started,
        TimeSpent = timeSpent,
        IssueId = issueId,
        ID = Guid.NewGuid().ToString(),
        SelfLink = new Uri($"https://jira.example.com/rest/api/2/issue/{issueId}/worklog/1"),
    };

    // IssueLite has only an internal constructor; worklog comparisons only need the Key.
    public static IssueLite Issue(string key)
    {
        var issue = (IssueLite)RuntimeHelpers.GetUninitializedObject(typeof(IssueLite));
        typeof(IssueStem).GetProperty(nameof(IssueStem.Key))!.SetValue(issue, key);
        return issue;
    }

    // LiraClient has no public constructor and fetches Myself from the server; the JQL code only needs AccountTimezone.
    public static LiraClient Client(TimeZoneInfo accountTimezone)
    {
        var client = (LiraClient)RuntimeHelpers.GetUninitializedObject(typeof(LiraClient));
        typeof(LiraClient).GetProperty(nameof(LiraClient.Myself))!.SetValue(client, User("me", accountTimezone));
        return client;
    }

    public static readonly TimeZoneInfo Warsaw = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
}
