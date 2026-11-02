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
        /// <param name="grouping">How Toggl entries of one day and issue are grouped into suggestions.</param>
        /// <param name="now">Time of day used for aggregated worklogs. Defaults to <see cref="DateTimeOffset.Now"/>.</param>
        public static TogglJiraComparison Compare(IEnumerable<TogglTimeEntry> toggl, IEnumerable<Worklog> jira, TimeZoneInfo timeZone, TogglGrouping grouping = TogglGrouping.ByName, DateTimeOffset? now = null)
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
                    suggestions.AddRange(Suggest(key, entries, logs, difference, grouping, timeZone, timeOfDay));
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

        private static List<TogglJiraSuggestion> Suggest(DayIssue key, List<TogglTimeEntry> entries, List<Worklog> logs, TimeSpan difference, TogglGrouping grouping, TimeZoneInfo timeZone, TimeSpan timeOfDay)
        {
            var unmatched = Candidates(entries, grouping);
            if (grouping == TogglGrouping.ByDay || !logs.TrueForAll(log => TryRemoveMatch(unmatched, log)))
            {
                var descriptions = entries.Select(x => x.DescriptionWithoutIssue).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var comment = descriptions.Count == 0 ? null : string.Join("; ", descriptions);
                return [Aggregated(key, difference, comment, timeZone, timeOfDay)];
            }
            List<TogglJiraSuggestion> result = [..
                unmatched
                .Where(x => x.Description is not null)
                .Select(x => new TogglJiraSuggestion(key.Issue, TimeZoneInfo.ConvertTime(x.Start, timeZone), RoundToMinutes(x.Duration), x.Description))
                ];
            var undescribed = unmatched
                .Where(x => x.Description is null)
                .Select(x => x.Duration).Sum();
            result.Add(Aggregated(key, undescribed, null, timeZone, timeOfDay));
            return result;
        }

        private readonly record struct Candidate(DateTimeOffset Start, TimeSpan Duration, string? Description);

        // Entries without description are always summed into one candidate.
        private static List<Candidate> Candidates(List<TogglTimeEntry> entries, TogglGrouping grouping)
        {
            var described = entries.Where(x => x.DescriptionWithoutIssue is not null);
            var undescribed = entries.Where(x => x.DescriptionWithoutIssue is null).ToList();
            IEnumerable<Candidate> candidates = grouping == TogglGrouping.NoGrouping
                ? described.Select(x => new Candidate(x.Start, x.Duration, x.DescriptionWithoutIssue))
                : described
                    .GroupBy(x => x.DescriptionWithoutIssue!, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new Candidate(g.Min(x => x.Start), g.Select(x => x.Duration).Sum(), g.First().DescriptionWithoutIssue));
            if (undescribed.Count > 0)
            {
                candidates = candidates.Append(new Candidate(undescribed.Min(x => x.Start), undescribed.Select(x => x.Duration).Sum(), null));
            }
            return [.. candidates];
        }

        /// <summary>
        /// Merges uncommented suggestions of the same day and issue into one, e.g. after their comments were cleared.
        /// </summary>
        public static List<TogglJiraSuggestion> MergeUncommented(IEnumerable<TogglJiraSuggestion> suggestions)
        {
            var list = suggestions.ToList();
            var merged = list
                .Where(x => string.IsNullOrWhiteSpace(x.Comment))
                .GroupBy(x => (DateOnly.FromDateTime(x.Started.DateTime), x.Issue))
                .Select(g =>
                {
                    var first = g.MinBy(x => x.Started)!.Started;
                    var duration = g.Select(x => x.Duration).Sum();
                    return new TogglJiraSuggestion(g.Key.Issue, new DateTimeOffset(FitInDay(first.DateTime, duration), first.Offset), duration, null);
                });
            return [..
                list
                .Where(x => !string.IsNullOrWhiteSpace(x.Comment))
                .Concat(merged)
                .OrderBy(x => x.Started)
                .ThenBy(x => x.Issue, StringComparer.Ordinal)
                ];
        }

        private static bool TryRemoveMatch(List<Candidate> unmatched, Worklog log)
        {
            var comment = log.CommentPlain.Trim();
            var index = unmatched.FindIndex(x =>
                string.Equals(x.Description ?? "", comment, StringComparison.OrdinalIgnoreCase)
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
            var start = FitInDay(key.Date.ToDateTime(TimeOnly.MinValue) + timeOfDay, duration);
            return new(key.Issue, new DateTimeOffset(start, timeZone.GetUtcOffset(start)), duration, comment);
        }

        private static DateTime FitInDay(DateTime start, TimeSpan duration)
        {
            var dayStart = start.Date;
            var dayEnd = dayStart.AddDays(1);
            if (start + duration <= dayEnd)
            {
                return start;
            }
            return dayEnd - duration < dayStart ? dayStart : dayEnd - duration;
        }

        private static TimeSpan RoundToMinutes(TimeSpan time) => TimeSpan.FromMinutes(Math.Round(time.TotalMinutes));
    }
}
