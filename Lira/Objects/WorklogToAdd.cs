using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lira.Objects;

public readonly record struct WorklogToAdd
{
    private readonly TimeSpan _timeThreshold = TimeSpan.FromMinutes(1);

    public WorklogToAdd(DateTimeOffset started, TimeSpan timeSpent, string? comment) : this()
    {
        Comment = comment;
        TimeSpent = timeSpent;
        Started = started;
    }

    [JsonPropertyName("timeSpentSeconds")]
    [JsonPropertyOrder(0)]
    public TimeSpan TimeSpent { get; init; }

    [JsonPropertyOrder(1)]
    public DateTimeOffset Started { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(2)]
    public string? Comment { get; init; }

    [JsonIgnore]
    public bool CanBeAdded => Started != default && TimeSpent > _timeThreshold;

}

public readonly record struct WorklogUpdatePackage
{
    private readonly TimeSpan _timeThreshold = TimeSpan.FromMinutes(1);

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
