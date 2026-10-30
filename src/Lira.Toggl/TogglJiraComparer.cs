using Lira.Extensions;
using Lira.Objects;

namespace Lira.Toggl
{
    public static class TogglJiraComparer
    {
        public static TimeSpan Tolerance { get; } = TimeSpan.FromMinutes(15);

        private readonly record struct DayIssue(DateOnly Date, string Issue);

        /// <summary>
        /// Compares Toggl entries with Jira worklogs per day and issue and suggests worklogs to add to Jira.
        /// </summary>
        /// <param name="jira">Worklogs of the user the Toggl entries belong to.</param>
        /// <param name="timeZone">Time zone defining day boundaries, usually the Jira account time zone.</param>
        /// <param name="forceAggregate">Always suggest one worklog per day and issue, never 1-to-1 copies of Toggl entries.</param>
        /// <param name="now">Time of day used for aggregated worklogs. Defaults to <see cref="DateTimeOffset.Now"/>.</param>
        public static TogglJiraComparison Compare(IEnumerable<TogglTimeEntry> toggl, IEnumerable<Worklog> jira, TimeZoneInfo timeZone, bool forceAggregate = false, DateTimeOffset? now = null)
        {
            var timeOfDay = TimeZoneInfo.ConvertTime(now ?? DateTimeOffset.Now, timeZone).TimeOfDay;

            DateOnly DayOf(DateTimeOffset date) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(date, timeZone).DateTime);

            var togglEntries = toggl.ToList();
            List<TogglJiraDaySummary> summary = [];
            List<TogglJiraSuggestion> suggestions = [];

            foreach (var day in togglEntries.Where(x => x.JiraIssue is null).GroupBy(x => DayOf(x.Start)))
            {
                summary.Add(new(day.Key, null, day.Select(x => x.Duration).Sum(), day.Count(), TimeSpan.Zero, 0, TogglSyncStatus.Unassigned));
            }

            var togglGroups = togglEntries.Where(x => x.JiraIssue is not null).ToLookup(x => new DayIssue(DayOf(x.Start), x.JiraIssue!));
            var jiraGroups = jira.ToLookup(x => new DayIssue(DayOf(x.Started), x.Issue.Key.ToUpperInvariant()));
            var unionKeys = togglGroups.Select(x => x.Key).Union(jiraGroups.Select(x => x.Key));
            foreach (var key in unionKeys)
            {
                var entries = togglGroups[key].ToList();
                var logs = jiraGroups[key].ToList();
                var togglTime = entries.Select(x => x.Duration).Sum();
                var jiraTime = logs.Select(x => x.TimeSpent).Sum();
                var difference = togglTime - jiraTime;
                var status = difference.Duration() <= Tolerance ? TogglSyncStatus.Match
                    : difference < TimeSpan.Zero ? TogglSyncStatus.TogglShort
                    : TogglSyncStatus.Missing;
                summary.Add(new(key.Date, key.Issue, togglTime, entries.Count, jiraTime, logs.Count, status));
                if (status == TogglSyncStatus.Missing)
                {
                    suggestions.AddRange(Suggest(key, entries, logs, difference, forceAggregate, timeZone, timeOfDay));
                }
            }

            return new(
                [..
                suggestions
                .Where(x => x.Duration > TimeSpan.Zero)
                .OrderBy(x => x.Started)
                .ThenBy(x => x.Issue, StringComparer.Ordinal)
                ],
                [..
                summary
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Issue is null)
                .ThenBy(x => x.Issue, StringComparer.Ordinal)
                ]
                );
        }

        private static List<TogglJiraSuggestion> Suggest(DayIssue key, List<TogglTimeEntry> entries, List<Worklog> logs, TimeSpan difference, bool forceAggregate, TimeZoneInfo timeZone, TimeSpan timeOfDay)
        {
            List<TogglTimeEntry> unmatched = [.. entries];
            if (forceAggregate || !logs.TrueForAll(log => TryRemoveMatch(unmatched, log)))
            {
                var descriptions = entries.Select(x => x.DescriptionWithoutIssue).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var comment = descriptions.Count == 0 ? null : string.Join("; ", descriptions);
                return [Aggregated(key, difference, comment, timeZone, timeOfDay)];
            }
            List<TogglJiraSuggestion> result = [.. 
                unmatched
                .Where(x => x.DescriptionWithoutIssue is not null)
                .Select(x => new TogglJiraSuggestion(key.Issue, TimeZoneInfo.ConvertTime(x.Start, timeZone), RoundToMinutes(x.Duration), x.DescriptionWithoutIssue))
                ];
            var undescribed = unmatched
                .Where(x => x.DescriptionWithoutIssue is null)
                .Select(x => x.Duration).Sum();
            result.Add(Aggregated(key, undescribed, null, timeZone, timeOfDay));
            return result;
        }

        private static bool TryRemoveMatch(List<TogglTimeEntry> unmatched, Worklog log)
        {
            var comment = log.CommentPlain.Trim();
            var index = unmatched.FindIndex(x =>
                string.Equals(x.DescriptionWithoutIssue ?? "", comment, StringComparison.OrdinalIgnoreCase)
                && (x.Duration - log.TimeSpent).Duration() <= Tolerance);
            if (index < 0)
            {
                return false;
            }
            unmatched.RemoveAt(index);
            return true;
        }

        // Jira starts new worklogs at the current time, so aggregated ones do the same - shifted back if they would end after midnight.
        private static TogglJiraSuggestion Aggregated(DayIssue key, TimeSpan duration, string? comment, TimeZoneInfo timeZone, TimeSpan timeOfDay)
        {
            duration = RoundToMinutes(duration);
            var dayStart = key.Date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = dayStart.AddDays(1);
            var start = dayStart + timeOfDay;
            if (start + duration > dayEnd)
            {
                start = dayEnd - duration < dayStart ? dayStart : dayEnd - duration;
            }
            return new(key.Issue, new DateTimeOffset(start, timeZone.GetUtcOffset(start)), duration, comment);
        }

        private static TimeSpan RoundToMinutes(TimeSpan time) => TimeSpan.FromMinutes(Math.Round(time.TotalMinutes));
    }
}
