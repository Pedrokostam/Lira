using System;
using System.Text.Json.Serialization;

namespace Lira.Objects;

public abstract record SelfReferential
{
    [JsonPropertyName("self")]
    public required Uri SelfLink { get; init; }
}