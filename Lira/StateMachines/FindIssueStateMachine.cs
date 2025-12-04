using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Lira.Objects;

namespace Lira.StateMachines;

/// <summary> State machine responsible for retrieving Issues matching given query. IssueCommons are returned (either Full or Lite) </summary>
public class FindIssueStateMachine(LiraClient client) : FindByQueryStateMachineImpl<IssueCommon>(client)
{
    protected override bool IsCached(State state, out State newState)
    {
        newState = default;
        return false;
    }

    protected override async Task<State> LoadPayload(State state)
    {
        var issueLites = state.Query.FilterItems(state.PaginationState.Values, LiraClient).ToImmutableList();
        var commons = TryGetCached(issueLites).ToImmutableList();
        //var allWorklogs = state.PaginationState.Values.SelectMany(x => x.Worklogs);
        // Log.Information("Filtering worklogs");
        //LiraClient.AddToCache(state.Query.BuildQueryString(LiraClient), worklogs);
        return state.Advance() with
        {
            Payload = commons,
        };
    }
    private List<IssueCommon> TryGetCached(IEnumerable<IssueLite> issueLites) {

        List<IssueCommon> outputColl = [];
        foreach (var potentiallyUncached in issueLites)
        {
            var updatedOn = potentiallyUncached.Updated;
            if (LiraClient.TryGetCachedIssue(potentiallyUncached.Key, out Issue? issue) && issue.Fetched > updatedOn)
            {
                outputColl.Add(issue);
            }
            else if (LiraClient.TryGetCachedIssue(potentiallyUncached.Key, out IssueLite? cachedLite) && cachedLite.Fetched > updatedOn)
            {
                outputColl.Add(cachedLite);
            }
            else
            {
                LiraClient.AddToCache(potentiallyUncached); // Update lite with newest
                outputColl.Add(potentiallyUncached);
                //uncachedLites.Add(potentiallyUncached);
            }
        }
        return outputColl;
    }
}

