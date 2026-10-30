using Lira.Toggl;

namespace Lira.Tests;

public class TogglTimeEntryTests
{
    [Theory]
    [InlineData("[ABC-123] fix bug", null, "ABC-123")]
    [InlineData("[abc123] fix bug", null, "ABC-123")]
    [InlineData("[ ABC-123 ]", null, "ABC-123")]
    [InlineData("ABC-123 fix bug", null, "ABC-123")]
    [InlineData("see XYZ-999 for [ABC-123]", null, "ABC-123")]
    [InlineData("no issue", "Project [PRJ-4567]", "PRJ-4567")]
    [InlineData(null, "PRJ4567 project", "PRJ-4567")]
    [InlineData("DES-111", "PRJ-222", "DES-111")]
    [InlineData("AB-123", null, null)]
    [InlineData("ABC-12", null, null)]
    [InlineData("xABC-123x", null, null)]
    [InlineData(null, null, null)]
    public void FindsJiraIssue(string? description, string? projectName, string? expected)
    {
        var entry = new TogglTimeEntry { Description = description, ProjectName = projectName };
        Assert.Equal(expected, entry.JiraIssue);
    }

    [Theory]
    [InlineData("[ABC-123] fix bug", "fix bug")]
    [InlineData("ABC-123: fix bug", "fix bug")]
    [InlineData("fix bug ABC-123", "fix bug")]
    [InlineData("[ABC-123]", null)]
    [InlineData("  ", null)]
    [InlineData(" fix bug ", "fix bug")]
    [InlineData(null, null)]
    public void StripsIssueFromDescription(string? description, string? expected)
    {
        var entry = new TogglTimeEntry { Description = description, ProjectName = "[PRJ-456]" };
        Assert.Equal(expected, entry.DescriptionWithoutIssue);
    }
}
