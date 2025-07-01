using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lira.Extensions;
using Lira.Jql;
using Lira.Objects;

namespace Lira.StateMachines;
public class WorklogMachine : StateMachine<WorklogMachine.State, WorklogMachine.Steps>
{
    public enum Steps
    {
        None,
        Ensure,
        QueryForIssues,
        LoadWorklogs,
        End,
    }
    public readonly record struct State : IState<Steps>
    {
        public Steps FinishedStep { get; init; }
        public PaginationMachine<IssueLite>.State PaginationState { get; init; }
        public JqlQuery Query { get; init; }
        public ImmutableList<Worklog> Worklogs { get; init; } = [];
        public State(LiraClient client, JqlQuery jqlQuery)
        {
            FinishedStep = Steps.None;
            Query = jqlQuery;
            HttpQuery httpQuery = [HttpQuery.JqlSearchQuery(jqlQuery.BuildQueryString(client))];
            PaginationState = new PaginationMachine<IssueLite>.State(PaginationMachine<IssueLite>.Steps.EnsureAuthorization, httpQuery);
        }
        public bool IsFinished => NextStep == Steps.End;
        public bool ShouldContinue => !IsFinished;
        public double QueryProgress => PaginationState.Progress;

        public Steps NextStep
        {
            get
            {
                return FinishedStep switch
                {
                    Steps.None => Steps.Ensure,
                    Steps.Ensure => Steps.QueryForIssues,
                    Steps.QueryForIssues => PaginationState.IsFinished ? Steps.LoadWorklogs : Steps.QueryForIssues,
                    Steps.LoadWorklogs => Steps.End,
                    _ => Steps.End,
                };
            }
        }
    }

    private int _queryLimit = 25;
    public int QueryLimit
    {
        get => _queryLimit;
        set
        {
            if (value > 1)
            {
                _queryLimit = value;
            }
            else
            {
                _queryLimit = 25;
            }
        }
    }

    private Task<State> Init(State state)
    {
        return EnsureAuthorization(state);
    }
    private async Task<State> GetIssues(State state)
    {
        state.PaginationState.Query.Add(HttpQuery.MaxResults(QueryLimit));
        var pagiState = await _pagination.Process(state.PaginationState).ConfigureAwait(false);
        return state with
        {
            PaginationState = pagiState,
            FinishedStep = Steps.QueryForIssues,
        };
    }
    private async Task<List<Worklog>> LoadWorklogsImpl(IEnumerable<IssueLite> issueLites)
    {
        List<IssueLite> unchachedLites = [];
        List<Worklog> worklogs = [];
        foreach (var potentiallyUncached in issueLites)
        {
            if (Cache.TryGetValue(potentiallyUncached.Key, out var issue))
            {
                worklogs.AddRange(issue.Worklogs);
            }
            else if (CacheLite.TryGetValue(potentiallyUncached.Key, out var cachedLite))
            {
                worklogs.AddRange(cachedLite.Worklogs);
            }
            else
            {
                unchachedLites.Add(potentiallyUncached);
            }
        }
        await unchachedLites.LoadWorklogs(LiraClient).ConfigureAwait(false);
        foreach (var issue in unchachedLites)
        {
            worklogs.AddRange(issue.Worklogs);
            CacheLite.Add(issue);
        }
        return worklogs;
    }
    private async Task<State> LoadWorklogs(State state)
    {
        var loadedLogs = await LoadWorklogsImpl(state.PaginationState.Values).ConfigureAwait(false);
        await state.PaginationState.Values.LoadWorklogs(LiraClient).ConfigureAwait(false);
        var allWorklogs = state.PaginationState.Values.SelectMany(x => x.Worklogs);
        // Log.Information("Filtering worklogs");
        var worklogs = state.Query.FilterItems(loadedLogs, LiraClient).ToImmutableList();
        return state with
        {
            Worklogs = worklogs,
            FinishedStep = Steps.LoadWorklogs,
        };
    }


    private readonly PaginationMachine<IssueLite> _pagination;

    public override Task<State> Process(State state)
    {
        state = AdjustState(in state);
        return state.NextStep switch
        {
            Steps.Ensure => Init(state),
            Steps.QueryForIssues => GetIssues(state),
            Steps.LoadWorklogs => LoadWorklogs(state),
            _ => Task.FromResult(state),
        };
    }
    public WorklogMachine(LiraClient client) : base(client)
    {
        _pagination = new PaginationMachine<IssueLite>(LiraClient, LiraClient.SearchEndpoint, "issues");
    }
    public State GetStartState(JqlQuery jqlQuery)
    {
        return new State(LiraClient, jqlQuery);
    }
}

