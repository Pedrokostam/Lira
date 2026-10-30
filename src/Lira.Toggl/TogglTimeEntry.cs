using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Lira.Toggl
{
    public partial class TogglTimeEntry {
        [JsonPropertyName("start")]
        [JsonConverter(typeof(TogglDateConverter))]
        public DateTimeOffset Start { get; set; }
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("project_name")]
        public string? ProjectName { get; set; }
        [JsonPropertyName("project_id")]
        public long? ProjectId { get; set; }
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }
        [JsonIgnore]
        public string? ClientName { get; set; }

        [GeneratedRegex(@"[\[\(]\s*(?<key>[A-Za-z]{2,})-?(?<num>\d{3,})\s*[\]\)]")]
        private static partial Regex BracketedIssueRegex();

        [GeneratedRegex(@"(?<!\w)(?<key>[A-Za-z]{2,})-?(?<num>\d{3,})(?!\w)")]
        private static partial Regex BareIssueRegex();

        /// <summary>
        /// Finds a Jira issue key in <see cref="Description"/>, then in <see cref="ProjectName"/>. Keys in square brackets take precedence within each.
        /// </summary>
        /// <value>Issue key normalized to <c>KEY-123</c>, or <see langword="null"/> if none found.</value>
        [JsonIgnore]
        public string? JiraIssue => ToKey(FindIssue(Description)) ?? ToKey(FindIssue(ProjectName));

        /// <summary>
        /// <see cref="Description"/> with the Jira issue key removed, trimmed. <see langword="null"/> if nothing remains.
        /// </summary>
        [JsonIgnore]
        public string? DescriptionWithoutIssue
        {
            get
            {
                if (Description is null)
                {
                    return null;
                }
                var match = FindIssue(Description);
                var text = match is null ? Description : Description.Remove(match.Index, match.Length);
                text = text.AsSpan().TrimStart(" -:").TrimEnd().ToString();
                return text.Length == 0 ? null : text;
            }
        }

        private static string? ToKey(Match? match)
            => match is null ? null : $"{match.Groups["key"].Value.ToUpperInvariant()}-{match.Groups["num"].Value}";

        private static Match? FindIssue(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            var match = BracketedIssueRegex().Match(text);
            if (!match.Success)
            {
                match = BareIssueRegex().Match(text);
            }
            return match.Success ? match : null;
        }
    }
}

