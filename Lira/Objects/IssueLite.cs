using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Lira.DataTransferObjects;
using Lira.Objects;

namespace Lira.Objects;
/// <summary>
/// Represents a lightweight version of a Jira issue, containing essential fields such as key, self-link, assignee, reporter, creator, description, timestamps, worklogs, and subtasks.
/// Used for scenarios where only summary information about an issue is needed, improving performance and reducing memory usage compared to a full issue object.
/// <para/>
/// Its subtasks are just <see cref="IssueStem"/>s - this class cannot be used to calculate total worklog time.
/// It can only accurately describe the time spent on this specific issue, not its children.
/// </summary>
[DebuggerDisplay(value: "LITE: {Key}")]
public record IssueLite : IssueCommon
{
    [SetsRequiredMembers()]
    internal IssueLite(IssueDto donor)
    {
        Key = donor.Key;
        SelfLink = donor.Self;
        Assignee = donor.Fields.Assignee;
        Reporter = donor.Fields.Reporter;
        Creator = donor.Fields.Creator;
        _worklogs = donor.Fields.Worklog.HasAllWorklogs ? new(donor.Fields.Worklog.InitialWorklogs) : null;
        _shallowSubtasks = new List<IssueStem>(donor.Fields.Subtasks);
        Created = donor.Fields.Created;
        Updated = donor.Fields.Updated;
        Description = donor.Fields.Description;
        Fetched = DateTime.UtcNow;

    }

    internal readonly List<IssueStem> _shallowSubtasks;

    public IReadOnlyList<IssueStem> ShallowSubtasks => _shallowSubtasks.AsReadOnly();
    //public Issue? Parent { get; set; }
}
