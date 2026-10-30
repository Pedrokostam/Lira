namespace Lira.Toggl
{
    public enum TogglSyncStatus
    {
        /// <summary>Toggl and Jira totals are within <see cref="TogglJiraComparer.Tolerance"/>.</summary>
        Match,
        /// <summary>Toggl has more time than Jira; suggestions were made.</summary>
        Missing,
        /// <summary>Toggl has less time than Jira.</summary>
        TogglShort,
        /// <summary>Toggl entries without a Jira issue.</summary>
        Unassigned,
    }
}
