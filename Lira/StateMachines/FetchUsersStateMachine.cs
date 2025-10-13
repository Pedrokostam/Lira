using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;

namespace Lira.StateMachines;
public class FetchUsersStateMachine(LiraClient client) : StateMachine<FetchUsersStateMachine.State,FetchUsersStateMachine.Steps>(client) 
{
    public enum Steps
    {
        None,
        EnsureAuthorization,
        GetUsers,
        End,
    }
    public readonly record struct State(string UserName, ImmutableArray<UserDetails> Users,Steps FinishedStep = Steps.None) : IState<Steps,State>
    {
        public State(string userName) : this(userName, [])
        {
            
        }
        public Steps NextStep
        {
            get
            {
                return FinishedStep switch
                {
                    Steps.None => Steps.EnsureAuthorization,
                    Steps.EnsureAuthorization => Steps.GetUsers,
                    Steps.GetUsers => Steps.End,
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
    private async Task<State> GetUsers(State state)
    {
        HttpQuery httpQuery = [("username", state.UserName)];
        var address = httpQuery.AddQueryToEndpoint(LiraClient.UserSearchEndpoint);
        var response = await GetAsync(address).ConfigureAwait(false);
        await HandleErrorResponse(response).ConfigureAwait(false);
        var stringContent = await ReadContentString(response).ConfigureAwait(false);
        var users = JsonHelper.Deserialize<IList<UserDetails>>(stringContent) ?? [];
        return state.Advance() with
        {
            Users = [.. users],
        };
    }

    public override Task<State> Process(State state)
    {
        state = AdjustState(in state);
        return state.NextStep switch
        {
            Steps.EnsureAuthorization => EnsureAuthorization(state),
            Steps.GetUsers => GetUsers(state),
            _ => Task.FromResult(state),
        };
    }

    public State GetStartState(string username)
    {
        return new State(username);
    }
}
