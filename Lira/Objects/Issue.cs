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

namespace Lira.Objects;
public record Issue : SelfReferential
{
    [SetsRequiredMembers()]
    internal Issue(IssueDto donor)
    {
        Key = donor.Key;
        SelfLink = donor.Self;
        Assignee = donor.Fields.Assignee;
        Reporter = donor.Fields.Reporter;
        Creator = donor.Fields.Creator;
        _worklogs = donor.Fields.Worklog.HasAllWorklogs ? new(donor.Fields.Worklog.InitialWorklogs) : null;
        Subtasks = new List<Issue>(donor.Fields.Subtasks).AsReadOnly();
        Created = donor.Fields.Created;
        Updated = donor.Fields.Updated;
    }
    private List<Worklog>? _worklogs;
    private List<Worklog>? _recurseWorklogs;
    public UserDetails Assignee { get; init; }
    public UserDetails Reporter { get; init; }
    public UserDetails Creator { get; init; }

    public DateTimeOffset Created { get; init; }
    public DateTimeOffset Updated { get; init; }

    public required string Key { get; init; }
    public IReadOnlyList<Worklog> Worklogs => _worklogs is null ? [] : _worklogs.AsReadOnly();
    public IReadOnlyList<Worklog> AllWorklogs => _recurseWorklogs is null ? [] : _recurseWorklogs.AsReadOnly();
    public IReadOnlyList<Issue> Subtasks { get; set; }


    public async Task LoadWorklogs(LiraClient lira)
    {
        lira.Logger.LoadingWorklogs(this);
        if (_worklogs is not null)
        {
            return;
        }
        var address = $"{SelfLink}/worklog";
        var response = await lira.HttpClient.GetAsync(address).ConfigureAwait(false);
        await lira.HandleErrorResponse(response).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        _worklogs = JsonHelper.Deserialize<List<Worklog>>(content, "worklogs");
    }
    public async Task LoadWorklogsRecurse(LiraClient lira)
    {
        await LoadWorklogs(lira).ConfigureAwait(false);
        if (Subtasks.Count == 0)
        {
            return;
        }
        lira.Logger.LoadingWorklogsOfSubtask(this, this.Subtasks);
        List<Worklog> gather = [.. _worklogs];
        //var bag = new ConcurrentBag<Worklog>(Worklogs);
        foreach (var sub in Subtasks)
        {
            await sub.LoadWorklogsRecurse(lira).ConfigureAwait(false);
            gather.AddRange(sub.AllWorklogs);
        }
        _recurseWorklogs = [.. gather.OrderBy(x => x.Started)];
    }
    /// <summary>
    /// Total time spent on this issue and its Subtasks.
    /// </summary>
    public TimeSpan TotalTimeSpent => TimeSpan.FromMinutes(AllWorklogs.Sum(x => x.TimeSpent.TotalMinutes));
    /// <summary>
    /// Time noted in this issue's worklogs, excluding time spent on Subtasks.
    /// </summary>
    public TimeSpan TimeSpent => TimeSpan.FromMinutes(Worklogs.Sum(x => x.TimeSpent.TotalMinutes));
    //public Issue? Parent { get; set; }
    public override string ToString() => Key;
}