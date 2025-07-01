using System;
using System.Text.Json.Serialization;
using Lira.Objects;

namespace Lira.Objects;
public record Worklog : SelfReferential
{
    public required UserDetails Author { get; set; }
    public required UserDetails UpdateAuthor { get; set; }
    public required string Comment { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Updated { get; set; }
    public DateTimeOffset Started { get; set; }
    [JsonPropertyName("timeSpentSeconds")]
    public TimeSpan TimeSpent { get; set; }
    public required string IssueId { get; set; }
    public IssueLite Issue { get; set; } = default!;
    public required string ID { get; set; }
}