using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Lira.Objects;

namespace Lira.DataTransferObjects;

public class IssueFields
{
    public WorklogResponse Worklog { get; set; } = new();
    public IList<IssueStem> Subtasks { get; set; } = [];
    public UserDetails? Assignee { get; set; } = default!;
    public UserDetails Reporter { get; set; } = default!;
    public UserDetails Creator { get; set; } = default!;
    public string Description { get; set; } = "";
    public string Summary { get; set; } = "";
    public IList<string> Labels { get; set; } = [];
    public IList<NamedValue> Components { get; set; } = [];
    public NamedValue Status { get; set; } = default!;
    public DateTimeOffset Created { get; set; } = default!;
    public DateTimeOffset Updated { get; set; } = default!;
}
