using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Lira.Extensions;

namespace Lira.StateMachines;


public class PaginationStateMachine<TPaginatedElement> : StateMachine<PaginationStateMachine<TPaginatedElement>.State, PaginationStateMachine<TPaginatedElement>.Steps>
{
    internal PaginationStateMachine(LiraClient client, Uri endpoint, string propertyName) : base(client)
    {
        Endpoint = endpoint;
        PropertyName = propertyName;
    }

    internal PaginationStateMachine(LiraClient client, string endpoint, string propertyName)
        : this(client, new Uri(endpoint, UriKind.Relative), propertyName) { }

    public enum Steps
    {
        None,
        EnsureAuthorization, 
        Paginate,
        End,
    }
    public readonly record struct State(Steps FinishedStep,
                                        PaginationParams Pagination,
                                        ImmutableList<TPaginatedElement> Values,
                                        HttpQuery Query) : IState<Steps,State>
    {
        internal State(HttpQuery query) : this(Steps.None, default, [],query)
        {
            
        }
        public State(Steps startStep,HttpQuery query) : this(startStep, default, [], query)
        {

        }
        public Steps NextStep
        {
            get
            {
                return FinishedStep switch
                {
                    Steps.None => Steps.EnsureAuthorization,
                    Steps.EnsureAuthorization => Steps.Paginate,
                    Steps.Paginate => Pagination.ShouldRequestNewPage ? Steps.Paginate : Steps.End,
                    _ => Steps.End,
                };
            }
        }
        public State Advance()
        {
            return this with { FinishedStep = NextStep };
        }
        public bool IsFinished => NextStep == Steps.End;
        public double Progress
        {
            get
            {
                if (Pagination.Total <= 0)
                {
                    return 0;
                }
                var progress = (double)Pagination.EndsAt / Pagination.Total;
                return progress switch
                {
                    < 0 => 0,
                    > 1 => 1,
                    double x => x,
                };
            }
        }
        public bool ShouldContinue => !IsFinished;
    }
    public Uri Endpoint { get; }
    public string PropertyName { get; }
    
    private async Task<State> GetQuery(State state)
    {
        state.Pagination.UpdateQuery(state.Query);
        var queryAddress = state.Query.AddQueryToEndpoint(Endpoint);
        var response = await GetAsync(queryAddress).ConfigureAwait(false);
        await HandleErrorResponse(response).ConfigureAwait(false);
        var responseString = await ReadContentString(response).ConfigureAwait(false);
        var pagination = PaginationParams.FromResponse(responseString);
        LiraClient.Logger.PaginatedResponse(Uri.UnescapeDataString(queryAddress), pagination);
        var pageValues = JsonHelper.Deserialize<IList<TPaginatedElement>>(responseString, PropertyName);
        PaginationParams.RemoveFromQuery(state.Query);
        return state.Advance() with {
            Pagination= pagination,
            Values= state.Values.AddRangeNotNull(pageValues),
        };
    }
    public override Task<State> Process(State state)
    {
        state = AdjustState(in state);
        return state.NextStep switch
        {
            Steps.EnsureAuthorization => EnsureAuthorization(state),
            Steps.Paginate => GetQuery(state),
            _ => Task.FromResult(state),
        };
    }
    public State GetStartState(HttpQuery query,Steps startStep)
    {
        return new State(startStep, query);
    }
}
