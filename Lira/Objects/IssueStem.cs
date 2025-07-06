using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Lira.Objects;

/// <summary>
/// Class containing the issue Key and its SelfLink. Does not contain anything else.
/// </summary>
[DebuggerDisplay("STEM: {Key}")]
public record IssueStem : SelfReferential
{
    /// <summary>
    /// Human-readable key for issue.
    /// </summary>
    [JsonPropertyName("key")]
    public required string Key { get; init; }
    public override string ToString() => Key;
}
