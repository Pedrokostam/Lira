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
    public readonly record struct State : IState<Steps,State>
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
        public State Advance()
        {
            return this with { FinishedStep = NextStep };
        }

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

    private int _queryLimit = 15;
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
                _queryLimit = 15;
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
        return state.Advance() with
        {
            PaginationState = pagiState,
        };
    }
    private async Task<List<Worklog>> LoadWorklogsImpl(IEnumerable<IssueLite> issueLites)
    {
        List<IssueLite> uncachedLites = [];
        List<Worklog> worklogs = [];
        foreach (var potentiallyUncached in issueLites)
        {
            if (TryGetValue(potentiallyUncached.Key, out Issue? issue))
            {
                worklogs.AddRange(issue.Worklogs);
            }
            else if (TryGetValue(potentiallyUncached.Key, out IssueLite? cachedLite))
            {
                worklogs.AddRange(cachedLite.Worklogs);
            }
            else
            {
                uncachedLites.Add(potentiallyUncached);
            }
        }
        await uncachedLites.LoadWorklogs(LiraClient).ConfigureAwait(false);
        foreach (var issue in uncachedLites)
        {
            worklogs.AddRange(issue.Worklogs);
            AddToCache(issue);
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
        return state.Advance() with
        {
            Worklogs = worklogs,
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

