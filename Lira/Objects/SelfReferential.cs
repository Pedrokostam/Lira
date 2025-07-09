using System;
using System.Text.Json.Serialization;

namespace Lira.Objects;

public abstract record SelfReferential
{
    /// <summary>
    /// The link to itself, that every jira item has. Not really usable in browser, tho
    /// </summary>
    [JsonPropertyName("self")]
    public required Uri SelfLink { get; init; }
    /// <summary>
    /// Get the base server address from the selflink.
    /// </summary>
    protected string SelfLinkBaseAddress => SelfLink.GetLeftPart(UriPartial.Authority);
}