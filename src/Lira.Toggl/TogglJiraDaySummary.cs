namespace Lira.Toggl
{
    public record TogglJiraDaySummary(DateOnly Date, string? Issue, TimeSpan TogglTime, int TogglCount, TimeSpan JiraTime, int JiraCount, TogglSyncStatus Status)
    {
        public TimeSpan Difference => TogglTime - JiraTime;
    }
}
