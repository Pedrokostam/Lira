using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lira.Objects;

namespace Lira.StateMachines;
public class AddWorklogStateMachine(LiraClient client) : StateMachine<AddWorklogStateMachine.State, AddWorklogStateMachine.Steps>(client)
{

    public enum Steps
    {
        None,
        EnsureAuthorization,
        AddWorklog,
        End,
    }
    public readonly record struct State(string IssueKey, WorklogToAdd Worklog, Worklog? AddedWorklog = null, Steps FinishedStep = Steps.None) : IState<Steps, State>
    {
        public Steps NextStep
        {
            get
            {
                return FinishedStep switch
                {
                    Steps.None => Steps.EnsureAuthorization,
                    Steps.EnsureAuthorization => Steps.AddWorklog,
                    Steps.AddWorklog => Steps.End,
                    _ => Steps.End,
                };
            }
        }
        public bool IsFinished => NextStep == Steps.End;
        public bool ShouldContinue => !IsFinished;
        public State Advance()
        {
            return this with { FinishedStep = NextStep };
        }
    }
    private async Task<State> AddWorklog(State state)
    {
        // ugly >_<
        System.Runtime.CompilerServices.ConfiguredTaskAwaitable<IssueLite?> fallbackIssueLiteTask;

        if (LiraClient.TryGetCachedIssue(state.IssueKey, out Issue? issueFull))
        {
            fallbackIssueLiteTask = Task.FromResult<IssueLite?>(null).ConfigureAwait(false);
        }
        else
        {
            fallbackIssueLiteTask = LiraClient.GetIssueLite(state.IssueKey).ConfigureAwait(false);
        }
       
        var address = $"{LiraClient.GetIssueEndpoint(state.IssueKey)}/worklog";
        var response = await PostAsync(address, state.Worklog).ConfigureAwait(false);
        await LiraClient.HandleErrorResponse(response).ConfigureAwait(false);
        var responseContent = await ReadContentString(response).ConfigureAwait(false);
        var addedWorklog = JsonHelper.Deserialize<Worklog>(responseContent);
        
        var issueLite = await fallbackIssueLiteTask; // should return immediately if FromResult is used
        if (addedWorklog is not null && issueFull is not null)
        {
            addedWorklog.Issue = issueFull;
            issueFull.AppendNewWorklog(addedWorklog);
        }
        else if (addedWorklog is not null && issueLite is not null)
        {
            addedWorklog.Issue = issueLite;
        }
        return state.Advance() with
        {
            AddedWorklog = addedWorklog,
        };
    }
    public override Task<State> Process(State state)
    {
        state = AdjustState(in state);
        return state.NextStep switch
        {
            Steps.EnsureAuthorization => EnsureAuthorization(state),
            Steps.AddWorklog => AddWorklog(state),
            _ => Task.FromResult(state),
        };

    }
    public State GetStartState(string issueKey, in WorklogToAdd worklogToAdd)
    {
        return new State(issueKey, worklogToAdd);
    }
    public State GetStartState(string issueKey, DateTimeOffset started, TimeSpan timeSpent, string? comment) => GetStartState(issueKey, new(started, timeSpent, comment));
}


//public async Task<Worklog?> AddWorklog(string issueId, string? comment, TimeSpan timeSpent, DateTimeOffset started)
//   {
//       var log = new WorklogToAdd(comment, timeSpent, started);
//       var address = $"{GetIssueEndpoint(issueId)}/worklog";
//       var response = await PostAsync(address, log).ConfigureAwait(false);
//       await HandleErrorResponse(response).ConfigureAwait(false);
//       var responseContent = await ReadContentString(response).ConfigureAwait(false);
//       var addedWorklog = JsonHelper.Deserialize<Worklog>(responseContent);
//       return addedWorklog;
//   }