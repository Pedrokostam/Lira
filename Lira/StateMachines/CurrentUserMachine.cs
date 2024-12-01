using System.Net.Http;
using System.Threading.Tasks;
using Lira.Objects;

namespace Lira.StateMachines;

public class CurrentUserMachine(LiraClient client) : StateMachine<CurrentUserMachine.State, CurrentUserMachine.Steps>(client)
{

    public enum Steps
    {
        None,
        EnsureAuthorization,
        GetUser,
        End,
    }
    public readonly record struct State(UserDetails? User, Steps FinishedStep = Steps.None) : IState<Steps>
    {
        public Steps NextStep
        {
            get
            {
                return FinishedStep switch
                {
                    Steps.None => Steps.EnsureAuthorization,
                    Steps.EnsureAuthorization => Steps.GetUser,
                    Steps.GetUser => Steps.End,
                    _ => Steps.End,
                };
            }
        }
        public bool IsFinished => NextStep == Steps.End;
        public bool ShouldContinue => !IsFinished;
    }
    private async Task<State> GetUser(State state)
    {
        HttpResponseMessage myselfResponse = await HttpClient.GetAsync(LiraClient.MyselfEndpoint).ConfigureAwait(false);
        await HandleErrorResponse(myselfResponse).ConfigureAwait(false);
        var content = await ReadContentString(myselfResponse).ConfigureAwait(false);
        var userDetails = JsonHelper.Deserialize<UserDetails>(content)!;
        return state with { User = userDetails, FinishedStep=Steps.GetUser };
    }
    public override Task<State> Process(State state)
    {
        state = AdjustState(in state);
        return state.NextStep switch
        {
            Steps.EnsureAuthorization => EnsureAuthorization(state),
            Steps.GetUser => GetUser(state),
            _ => Task.FromResult(state),
        };
    }
    public State GetStartState()
    {
        return new State();
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