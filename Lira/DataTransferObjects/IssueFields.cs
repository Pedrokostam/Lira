using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Lira.Objects;

namespace Lira.DataTransferObjects;

public class IssueFields
{
    // [JsonPropertyName("parent")]
    //public Issue? Parent { get; set; }
    [JsonPropertyName("worklog")]
    public WorklogResponse Worklog { get; set; }
    [JsonPropertyName("subtasks")]
    public IList<IssueStem> Subtasks { get; set; }
    public UserDetails? Assignee { get; set; } = default!;
    public UserDetails Reporter { get; set; } = default!;
    public UserDetails Creator { get; set; } = default!;
    public string Description { get; set; } = "";
    public string Summary { get; set; } = "";
    public DateTimeOffset Created { get; set; } = default!;
    public DateTimeOffset Updated { get; set; } = default!;
    public IssueFields()
    {
        Worklog = new();
        Subtasks = [];
    }
}
