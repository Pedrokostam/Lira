using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lira.Extensions;
using Lira.Objects;

namespace Lira.Objects;
/// <summary>
/// Class representing a single worklog in a Jira issue. Depending on its source, <see cref="Issue"/> is either an <see cref="Lira.Objects.IssueLite"/> or <see cref="Lira.Objects.Issue"/>
/// </summary>
public record Worklog : SelfReferential
{
    private string? _commentPlain;
    private string _comment = string.Empty;
    public required UserDetails Author { get; set; }
    public required UserDetails UpdateAuthor { get; set; }
    public string Comment { get => _comment; set => _comment = value; }
    public string CommentPlain { get => _commentPlain ??= Comment.StripMarkup(); }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Updated { get; set; }
    public DateTimeOffset Started { get; set; }
    [JsonPropertyName("timeSpentSeconds")]
    public TimeSpan TimeSpent { get; set; }
    public required string IssueId { get; set; }
    public IssueCommon Issue { get; set; } = default!;
    public required string ID { get; set; }

}