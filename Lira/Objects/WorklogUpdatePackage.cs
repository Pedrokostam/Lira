using System;
using System.Text.Json.Serialization;

namespace Lira.Objects;

public readonly record struct WorklogUpdatePackage
{
    public WorklogUpdatePackage(DateTimeOffset? started, TimeSpan? timeSpent, string? comment) : this()
    {
        Comment = comment;
        TimeSpent = timeSpent;
        Started = started;
    }

    [JsonPropertyName("timeSpentSeconds")]
    [JsonPropertyOrder(0)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TimeSpan? TimeSpent { get; init; }

    [JsonPropertyOrder(1)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? Started { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(2)]
    public string? Comment { get; init; }

    [JsonIgnore]
    public bool HasContent => TimeSpent is not null || Comment is not null || Started is not null;

}
