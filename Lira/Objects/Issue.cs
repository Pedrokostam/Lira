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
public record IssueStem : SelfReferential
{
    [JsonPropertyName("key")]
    public required string Key { get; init; }
    public override string ToString() => Key;
}
public record IssueLite : IssueStem
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
    private List<Worklog>? _worklogs;
    internal readonly List<IssueStem> _shallowSubtasks;
    public string Description { get; init; }
    public DateTime Fetched { get; init; }
    public UserDetails Assignee { get; init; }
    public UserDetails Reporter { get; init; }
    public UserDetails Creator { get; init; }

    public DateTimeOffset Created { get; init; }
    public DateTimeOffset Updated { get; init; }

    public IReadOnlyList<Worklog> Worklogs => _worklogs is null ? [] : _worklogs.AsReadOnly();

    internal IReadOnlyList<IssueStem> ShallowSubtasks => _shallowSubtasks.AsReadOnly();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="lira"></param>
    /// <param name="cache">If provided, all issue whose worklogs have been fetched will be added to it.</param>
    /// <returns></returns>
    internal async Task LoadWorklogs(LiraClient lira)
    {
        lira.Logger.LoadingWorklogs(this);
        var address = $"{SelfLink}/worklog";
        var response = await lira.HttpClient.GetAsync(address, lira.CancellationTokenSource.Token).ConfigureAwait(false);
        await lira.HandleErrorResponse(response).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        _worklogs = JsonHelper.Deserialize<List<Worklog>>(content, "worklogs") ?? [];
        foreach (var log in _worklogs)
        {
            log.Issue = this;
        }
    }
    ///// <summary>
    ///// 
    ///// </summary>
    ///// <param name="lira"></param>
    ///// <param name="cache">If provided, all issue whose worklogs have been fetched will be added to it.</param>
    ///// <returns></returns>
    //public async Task LoadWorklogsRecurse(LiraClient lira, IssueCache? cache = null)
    //{
    //    await LoadWorklogs(lira, cache).ConfigureAwait(false);
    //    List<Worklog> gather = [.. _worklogs];
    //    if (Subtasks.Count != 0)
    //    {

    //        lira.Logger.LoadingWorklogsOfSubtask(this, this.Subtasks);
    //        //var bag = new ConcurrentBag<Worklog>(Worklogs);
    //        for (int i = 0; i < _shallowSubtasks.Count; i++)
    //        {
    //            if (_shallowSubtasks[i] is not Issue fullIssue)
    //            {
    //                lira.Logger.UpliftingShallowIssue(_shallowSubtasks[i]);
    //            }
    //        }
    //        foreach (var sub in Subtasks)
    //        {
    //            await sub.LoadWorklogsRecurse(lira,cache).ConfigureAwait(false);
    //            gather.AddRange(sub.AllWorklogs);
    //        }
    //    }
    //    _recurseWorklogs = [.. gather.OrderBy(x => x.Started)];
    //}
    /// <summary>
    /// Total time spent on this issue and its Subtasks.
    /// </summary>
    //public TimeSpan TotalTimeSpent => TimeSpan.FromMinutes(AllWorklogs.Sum(x => x.TimeSpent.TotalMinutes));
    /// <summary>
    /// Time noted in this issue's worklogs, excluding time spent on Subtasks.
    /// </summary>
    public TimeSpan TimeSpent => TimeSpan.FromMinutes(Worklogs.Sum(x => x.TimeSpent.TotalMinutes));
    //public Issue? Parent { get; set; }
}

public record Issue : IssueLite
{
    [SetsRequiredMembers]
    public Issue(IssueLite issueLite, IEnumerable<Issue> substasks) : base(issueLite)
    {
        _subtasks = [.. substasks.OrderBy(x => x.Created)];
        Fetched = DateTime.UtcNow;
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
    internal readonly List<Issue> _subtasks = [];
    public IReadOnlyList<IssueStem> Subtasks => _subtasks.AsReadOnly();
    public TimeSpan TotalTimeSpent => TimeSpan.FromMinutes(AllWorklogs.Sum(x => x.TimeSpent.TotalMinutes));
}