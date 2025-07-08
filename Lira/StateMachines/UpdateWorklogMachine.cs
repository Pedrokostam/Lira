using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lira.Objects;

namespace Lira.StateMachines;
public class UpdateWorklogMachine(LiraClient client) : StateMachine<UpdateWorklogMachine.State, UpdateWorklogMachine.Steps>(client)
{

    public enum Steps
    {
        None,
        EnsureAuthorization,
        UpdateWorklog,
        End,
    }
    public readonly record struct State(WorklogUpdatePackage Package, Worklog OldWorklog, Worklog? UpdateWorklog=null, Steps FinishedStep = Steps.None) : IState<Steps,State>
    {
        public Steps NextStep
        {
            get
            {
                return FinishedStep switch
                {
                    Steps.None => Steps.EnsureAuthorization,
                    Steps.EnsureAuthorization => Steps.UpdateWorklog,
                    Steps.UpdateWorklog => Steps.End,
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
    private async Task<State> UpdateWorklog(State state)
    {
        if (!state.Package.HasContent)
        {
            throw new ArgumentException("Update payload must contain some changes", nameof(state));
        }
        var address = $"{LiraClient.GetIssueEndpoint(state.OldWorklog.Issue.Key)}/worklog/{state.OldWorklog.ID}";
        var response = await PutAsync(address, state.Package).ConfigureAwait(false);
        await LiraClient.HandleErrorResponse(response).ConfigureAwait(false);
        var responseContent = await ReadContentString(response).ConfigureAwait(false);
        var updateWorklog = JsonHelper.Deserialize<Worklog>(responseContent);
        return state.Advance() with
        {
            UpdateWorklog = updateWorklog,
        };
    }
    public override Task<State> Process(State state)
    {
        state = AdjustState(in state);
        return state.NextStep switch
        {
            Steps.EnsureAuthorization => EnsureAuthorization(state),
            Steps.UpdateWorklog => UpdateWorklog(state),
            _ => Task.FromResult(state),
        };
    }
    public State GetStartState(Worklog oldWorklog,in WorklogUpdatePackage updatePayload)
    {
        return new State(updatePayload, oldWorklog);
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