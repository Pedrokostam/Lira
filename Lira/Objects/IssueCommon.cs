using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lira.Extensions;

namespace Lira.Objects;

public abstract record IssueCommon : IssueStem
{
    protected List<Worklog>? _worklogs = [];
    private string? _summaryPlain;
    private string? _descriptionPlain;

    public required string Summary { get; init; }
    public string SummaryPlain { get => _summaryPlain ??= Summary.StripMarkup(); }
    public required string Description { get; init; }
    public string DescriptionPlain { get => _descriptionPlain ??= Description.StripMarkup(); }
    public DateTime Fetched { get; init; }
    public required UserDetails Assignee { get; init; }
    public required UserDetails Reporter { get; init; }
    public required UserDetails Creator { get; init; }

    public DateTimeOffset Created { get; init; }
    public DateTimeOffset Updated { get; init; }

    public IReadOnlyList<Worklog> Worklogs => _worklogs is null ? [] : _worklogs.AsReadOnly();

    /// <summary>
    /// Total time spent on this issue and its Subtasks.
    /// </summary>
    //public TimeSpan TotalTimeSpent => TimeSpan.FromMinutes(AllWorklogs.Sum(x => x.TimeSpent.TotalMinutes));
    /// <summary>
    /// Time noted in this issue's worklogs, excluding time spent on Subtasks.
    /// </summary>
    public TimeSpan TimeSpent => TimeSpan.FromMinutes(Worklogs.Sum(x => x.TimeSpent.TotalMinutes));
    public override string ToString() => Key;

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

}
