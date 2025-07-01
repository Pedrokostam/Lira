using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lira.DataTransferObjects;
using Lira.Objects;
using Serilog.Filters;

namespace Lira.StateMachines;

public class IssueMachine(LiraClient client) : StateMachine<IssueMachine.State, IssueMachine.Steps>(client)
{
    public enum Steps
    {
        None,
        EnsureAuthorization,
        GetIssue,
        GetSubTasks,
        LoadWorklogs,
        End
    }
    public readonly record struct State(string IssueId, Steps FinishedStep = Steps.None, IssueLite? IssueLite = null, Issue? Issue = null) : IState<Steps>
    {
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(25);
        public Steps NextStep
        {
            get
            {
                return FinishedStep switch
                {
                    Steps.None => Steps.EnsureAuthorization,
                    Steps.EnsureAuthorization => Steps.GetIssue,
                    Steps.GetIssue => IssueLite is null ? Steps.End : Steps.LoadWorklogs,
                    Steps.LoadWorklogs => Steps.GetSubTasks,
                    Steps.GetSubTasks => Steps.End,
                    _ => Steps.End,
                };
            }
        }
        public bool IsFinished => NextStep == Steps.End;
        public bool ShouldContinue => !IsFinished;
    }
    private async Task<IssueLite?> GetIssueImpl(string issueId)
    {
        var endpoint = LiraClient.GetIssueEndpoint(issueId);
        var response = await GetAsync(endpoint).ConfigureAwait(false);
        await HandleErrorResponse(response).ConfigureAwait(false);

        var stringContent = await ReadContentString(response).ConfigureAwait(false);
        var issue = JsonHelper.Deserialize<IssueLite>(stringContent);
        return issue;
    }
    private async Task<State> GetIssue(State state)
    {
        if (Cache is not null && Cache.TryGetValue(state.IssueId, out var cachedIssue))
        {
            LiraClient.Logger.UsingCachedWorklogs(cachedIssue);
            // Reuse cached issue. Cached issues already have worklogs loaded with subtrasks.
            return state with
            {
                FinishedStep = Steps.GetSubTasks,
                Issue = cachedIssue,
            };
        }
        var issueLite = await GetIssueImpl(state.IssueId).ConfigureAwait(false);
        // Do not add the issue to cache - we need to load its worklogs.
        return state with
        {
            FinishedStep = Steps.GetIssue,
            IssueLite = issueLite,
        };
    }

    private async Task<State> GetSubtasks(State state)
    {
        var issueLite = state.IssueLite!;
        var shallows = issueLite.ShallowSubtasks;
        var bag = new ConcurrentBag<Issue>();
        var tasks = shallows.Select(async shallow =>
        {
            Debug.WriteLine(state.Semaphore.CurrentCount);
            await state.Semaphore.WaitAsync(LiraClient.CancellationTokenSource.Token).ConfigureAwait(false);
            try
            {
                LiraClient.Logger.UpliftingShallowIssue(shallow);
                var state = GetStartState(shallow.Key);
                while (!state.IsFinished)
                {
                    state = await Process(state).ConfigureAwait(false);
                }
                if (state.Issue is not null)
                {
                    bag.Add(state.Issue);
                }
            }
            finally
            {
                state.Semaphore.Release();
            }
        });
        await Task.WhenAll(tasks).ConfigureAwait(false);
        issueLite._shallowSubtasks.Clear();
        var issue = new Issue(issueLite, bag);
        Cache.Add(issue);
        return state with
        {
            FinishedStep = Steps.GetSubTasks,
            Issue = issue,
        };
    }
    private async Task<State> LoadWorklogs(State state)
    {
        IssueLite issueLite = state.IssueLite!;
        await issueLite.LoadWorklogs(LiraClient).ConfigureAwait(false);
        LiraClient.Logger.CachingWorklogs(issueLite);
        return state with
        {
            FinishedStep = Steps.LoadWorklogs,
        };
    }
    public override Task<State> Process(State state)
    {
        state = AdjustState(in state);
        return state.NextStep switch
        {
            Steps.EnsureAuthorization => EnsureAuthorization(state),
            Steps.GetIssue => GetIssue(state),
            Steps.LoadWorklogs => LoadWorklogs(state),
            Steps.GetSubTasks => GetSubtasks(state),
            _ => Task.FromResult(state),
        };
    }

    public State GetStartState(string issueId)
    {
        return new State(issueId);
    }
}

