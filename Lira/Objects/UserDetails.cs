using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Lira.Objects;

[DebuggerDisplay("{Name,nq}, {DisplayName,nq} in {TimeZone}")]
public record UserDetails : SelfReferential
{
    public required string Name { get; set; }
    public required string Key { get; set; }
    public required string DisplayName { get; set; }
    /// <summary>
    /// Null when Jira's email visibility setting hides it.
    /// </summary>
    public string? EmailAddress { get; set; }
    public TimeZoneInfo? TimeZone { get; set; }
    public bool NameMatches(string name)
    {
        var c = StringComparer.OrdinalIgnoreCase;
        return c.Equals(name, Name) || c.Equals(name, DisplayName) || c.Equals(name, Key) || c.Equals(name, EmailAddress);
    }
}
