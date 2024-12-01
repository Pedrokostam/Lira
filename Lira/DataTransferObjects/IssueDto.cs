using System;
using System.Text.Json.Serialization;
using Lira.Objects;

namespace Lira.DataTransferObjects;

public class IssueDto : IToObject<Issue>
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

    public Issue ToObject()
    {
        return new Issue(this);
    }
}
