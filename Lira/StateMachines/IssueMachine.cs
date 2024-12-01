using System;
using System.Threading.Tasks;
using Lira.Objects;

namespace Lira.StateMachines;

public class IssueMachine(LiraClient client) : StateMachine<IssueMachine.State, IssueMachine.Steps>(client)
{

    public enum Steps
    {
        None,
        EnsureAuthorization,
        GetIssue,
        LoadWorklogs,
        End
    }
    public readonly record struct State(string IssueId, Steps FinishedStep=Steps.None, Issue? Issue=null) : IState<Steps>
    {
        public Steps NextStep
        {
            get
            {
                return FinishedStep switch
                {
                    Steps.None => Steps.EnsureAuthorization,
                    Steps.EnsureAuthorization => Steps.GetIssue,
                    Steps.GetIssue => Issue is null ? Steps.End : Steps.LoadWorklogs,
                    Steps.LoadWorklogs => Steps.End,
                    _ => Steps.End,
                };
            }
        }
        public bool IsFinished => NextStep == Steps.End;
        public bool ShouldContinue => !IsFinished;
    }
    private async Task<State> GetIssue(State state)
    {
        var endpoint = LiraClient.GetIssueEndpoint(state.IssueId);
        var response = await GetAsync(endpoint).ConfigureAwait(false);
        await HandleErrorResponse(response).ConfigureAwait(false);

        var stringContent = await ReadContentString(response).ConfigureAwait(false);
        var issue = JsonHelper.Deserialize<Issue>(stringContent);
        return state with
        {
            FinishedStep = Steps.GetIssue,
            Issue = issue,
        };
    }
    private async Task<State> LoadWorklogs(State state)
    {
        await state.Issue!.LoadWorklogsRecurse(LiraClient).ConfigureAwait(false);
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
            _ => Task.FromResult(state),
        };
    }

    public State GetStartState(string issueId)
    {
        return new State(issueId);
    }
}

