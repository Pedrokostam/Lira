using System;
using System.Text.Json.Serialization;
using Lira.Objects;

namespace Lira.DataTransferObjects;

public class IssueDto : IToObject<IssueLite>
{
    public string Key { get; set; }
    public Uri Self { get; set; }
    [JsonPropertyName("fields")]
    public IssueFields Fields { get; set; }
    public IssueDto()
    {
        Key = default!;
        Self = default!;
        Fields = new();
    }

    public IssueLite ToObject()
    {
        return new IssueLite(this);
    }
}
