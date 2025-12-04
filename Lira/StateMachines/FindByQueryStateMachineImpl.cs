using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lira.Jql;
using Lira.Objects;

namespace Lira.StateMachines;
/// <summary>Abstract state machine that executes JQL queries, uses pagination to retrieve issue identifiers and delegates loading of the final payload to concrete implementations.</summary>
public abstract class FindByQueryStateMachineImpl<T> : StateMachine<FindByQueryStateMachineImpl<T>.State, FindByQueryStateMachineImpl<T>.Steps>
{
    public enum Steps
    {
        None,
        Ensure,
        QueryForIssues,
        LoadPayload,
        End,
    }
    public readonly record struct State : IState<Steps, State>
    {
        public Steps FinishedStep { get; init; }
        public PaginationStateMachine<IssueLite>.State PaginationState { get; init; }
        public JqlQuery Query { get; init; }
        public ImmutableList<T> Payload { get; init; } = [];
        public State(LiraClient client, JqlQuery jqlQuery)
        {
            FinishedStep = Steps.None;
            Query = jqlQuery;
            HttpQuery httpQuery = [HttpQuery.JqlSearchQuery(jqlQuery.BuildQueryString(client))];
            PaginationState = new PaginationStateMachine<IssueLite>.State(PaginationStateMachine<IssueLite>.Steps.EnsureAuthorization, httpQuery);
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
                    Steps.QueryForIssues => PaginationState.IsFinished ? Steps.LoadPayload : Steps.QueryForIssues,
                    Steps.LoadPayload => Steps.End,
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
    protected abstract bool IsCached(State state, out State newState);
    //{
    //    newState = default;
    //    if (state.PaginationState.Query.TryGetQueryPart("jql", out var jqlQuery))
    //    {
    //        List<IssueCommon> issues = [];
    //        if (LiraClient.CheckWorklogCache((string)jqlQuery.Value, out var relevantIssues))
    //        {
    //            foreach (var issue in relevantIssues)
    //            {
    //                if (LiraClient.TryGetCachedIssue(issue, out var cachedIssue))
    //                {
    //                    issues.Add(cachedIssue);
    //                }
    //                else
    //                {
    //                    return false;
    //                }
    //            }
    //        }
    //        else
    //        {
    //            return false;
    //        }
    //        var cachedWorklogs = issues.SelectMany(x => x.Worklogs);
    //        var filtered = state.Query.FilterItems(cachedWorklogs, LiraClient).ToImmutableList();
    //        LiraClient.Logger.UsingCachedQuery((string)jqlQuery.Value);
    //        newState = state with
    //        {
    //            FinishedStep = Steps.LoadPayload,
    //            PaginationState = state.PaginationState with { FinishedStep = PaginationStateMachine<IssueLite>.Steps.End },
    //            Payload = filtered,

    //        };
    //        return true;
    //    }
    //    return false;
    //}
    private async Task<State> QueryForIssues(State state)
    {
        if (IsCached(state, out State newState))
        {
            return newState;
        }
        state.PaginationState.Query.Add(HttpQuery.MaxResults(QueryLimit));
        var pagiState = await _pagination.Process(state.PaginationState).ConfigureAwait(false);
        return state.Advance() with
        {
            PaginationState = pagiState,
        };
    }

    protected abstract Task<State> LoadPayload(State state);
    //{
    //    var loadedLogs = await LoadWorklogsImpl(state.PaginationState.Values).ConfigureAwait(false);
    //    var allWorklogs = state.PaginationState.Values.SelectMany(x => x.Worklogs);
    //    // Log.Information("Filtering worklogs");
    //    var worklogs = state.Query.FilterItems(loadedLogs, LiraClient).ToImmutableList();
    //    LiraClient.AddToCache(state.Query.BuildQueryString(LiraClient), worklogs);
    //    return state.Advance() with
    //    {
    //        Worklogs = worklogs,
    //    };
    //}


    private readonly PaginationStateMachine<IssueLite> _pagination;

    public override Task<State> Process(State state)
    {
        state = AdjustState(in state);
        return state.NextStep switch
        {
            Steps.Ensure => Init(state),
            Steps.QueryForIssues => QueryForIssues(state),
            Steps.LoadPayload => LoadPayload(state),
            _ => Task.FromResult(state),
        };
    }
    public FindByQueryStateMachineImpl(LiraClient client) : base(client)
    {
        _pagination = new PaginationStateMachine<IssueLite>(LiraClient, LiraClient.SearchEndpoint, "issues");
    }
    public State GetStartState(JqlQuery jqlQuery)
    {
        return new State(LiraClient, jqlQuery);
    }
}

