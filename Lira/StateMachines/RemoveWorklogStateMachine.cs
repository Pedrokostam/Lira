using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lira.Objects;

namespace Lira.StateMachines;
public class RemoveWorklogStateMachine(LiraClient client) : StateMachine<RemoveWorklogStateMachine.State, RemoveWorklogStateMachine.Steps>(client)
{

    public enum Steps
    {
        None,
        EnsureAuthorization,
        RemoveWorklog,
        End,
    }
    public readonly record struct State(Worklog WorklogToRemove, bool RemovalSuccess = false, Steps FinishedStep = Steps.None) : IState<Steps, State>
    {
        public Steps NextStep
        {
            get
            {
                return FinishedStep switch
                {
                    Steps.None => Steps.EnsureAuthorization,
                    Steps.EnsureAuthorization => Steps.RemoveWorklog,
                    Steps.RemoveWorklog => Steps.End,
                    _ => Steps.End,
                };
            }
        }
        public string IssueKey => WorklogToRemove.Issue.Key;
        public bool IsFinished => NextStep == Steps.End;
        public bool ShouldContinue => !IsFinished;

        public State Advance()
        {
            return this with { FinishedStep = NextStep };
        }
    }
    private async Task<State> RemoveWorklog(State state)
    {
        var address = $"{LiraClient.GetIssueEndpoint(state.IssueKey)}/worklog/{state.WorklogToRemove.ID}";
        var response = await DeleteAsync(address).ConfigureAwait(false);
        await LiraClient.HandleErrorResponse(response).ConfigureAwait(false);

        return state.Advance() with { RemovalSuccess = response.IsSuccessStatusCode };
    }
    public override Task<State> Process(State state)
    {
        state = AdjustState(in state);
        return state.NextStep switch
        {
            Steps.EnsureAuthorization => EnsureAuthorization(state),
            Steps.RemoveWorklog => RemoveWorklog(state),
            _ => Task.FromResult(state),
        };
    }
    public State GetStartState(Worklog worklogToRemove)
    {
        return new State(worklogToRemove);
    }
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