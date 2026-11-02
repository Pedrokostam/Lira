namespace Lira.Toggl
{
    /// <summary>How Toggl entries of one issue and day are grouped into suggested worklogs. Entries without description are always summed into one worklog.</summary>
    public enum TogglGrouping
    {
        /// <summary>Each entry is a separate worklog.</summary>
        NoGrouping,
        /// <summary>Entries with the same description are summed into one worklog.</summary>
        ByName,
        /// <summary>All entries are summed into one worklog.</summary>
        ByDay,
    }
}
