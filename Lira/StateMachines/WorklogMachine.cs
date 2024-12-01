using System;
using System.Collections;
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
        public PaginationMachine<Issue>.State PaginationState { get; init; }
        public JqlQuery Query{ get; init; }
        public ImmutableList<Worklog> Worklogs { get; init; } = [];
        public State(LiraClient client, JqlQuery jqlQuery) 
        {
            FinishedStep = Steps.None;
            Query = jqlQuery;
            HttpQuery httpQuery = [HttpQuery.JqlSearchQuery(jqlQuery.BuildQueryString(client))];
            PaginationState = new PaginationMachine<Issue>.State(PaginationMachine<Issue>.Steps.EnsureAuthorization, httpQuery);
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
    private async Task<State> LoadWorklogs(State state)
    {
        await state.PaginationState.Values.LoadWorklogs(LiraClient).ConfigureAwait(false);
        var allWorklogs = state.PaginationState.Values.SelectMany(x => x.Worklogs);
        // Log.Information("Filtering worklogs");
        var worklogs = state.Query.FilterItems(allWorklogs, LiraClient).ToImmutableList();
        return state with
        {
            Worklogs = worklogs,
            FinishedStep = Steps.LoadWorklogs,
        };
    }


    private readonly PaginationMachine<Issue> _pagination;

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
        _pagination = new PaginationMachine<Issue>(LiraClient, LiraClient.SearchEndpoint, "issues");
    }
    public State GetStartState(JqlQuery jqlQuery)
    {
        return new State(LiraClient, jqlQuery);
    }
}

