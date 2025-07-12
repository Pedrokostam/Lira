using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Lira.DataTransferObjects;
using Lira.Extensions;

namespace Lira.Objects;
public record Issue : IssueCommon
{
    [SetsRequiredMembers]
    public Issue(IssueLite issueLite, IEnumerable<Issue> substasks) : base(issueLite)
    {
        _subtasks = [.. substasks.OrderBy(x => x.Created)];
        Fetched = DateTimeOffset.UtcNow;
        foreach (var log in Worklogs)
        {
            log.Issue = this;
        }
    }
    public IReadOnlyList<Worklog> AllWorklogs
    {
        get
        {
            List<Worklog> q = [.. Worklogs];
            foreach (var sub in _subtasks)
            {
                q.AddRange(sub.Worklogs);
            }
            return q.AsReadOnly();
        }
    }
    public override string ToString() => Key;

    internal readonly List<Issue> _subtasks = [];
    public IReadOnlyList<IssueStem> Subtasks => _subtasks.AsReadOnly();
    public TimeSpan TotalTimeSpent => AllWorklogs.Select(x => x.TimeSpent).Sum();
}