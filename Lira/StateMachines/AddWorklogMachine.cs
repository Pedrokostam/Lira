using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lira.Objects;

namespace Lira.StateMachines;
public class AddWorklogMachine(LiraClient client) : StateMachine<AddWorklogMachine.State, AddWorklogMachine.Steps>(client)
{

    public enum Steps
    {
        None,
        EnsureAuthorization,
        AddWorklog,
        End,
    }
    public readonly record struct State(string IssueId, WorklogToAdd Worklog, Worklog? AddedWorklog=null, Steps FinishedStep = Steps.None) : IState<Steps>
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
    }
    private async Task<State> AddWorklog(State state)
    {
        var address = $"{LiraClient.GetIssueEndpoint(state.IssueId)}/worklog";
        var response = await PostAsync(address, state.Worklog).ConfigureAwait(false);
        await LiraClient.HandleErrorResponse(response).ConfigureAwait(false);
        var responseContent = await ReadContentString(response).ConfigureAwait(false);
        var addedWorklog = JsonHelper.Deserialize<Worklog>(responseContent);
        return state with
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
    public State GetStartState(string issueId,in WorklogToAdd worklogToAdd)
    {
        return new State(issueId, worklogToAdd);
    }
    public State GetStartState(string issueId, DateTimeOffset started, TimeSpan timeSpent, string? comment) => GetStartState(issueId, new(started, timeSpent, comment));
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