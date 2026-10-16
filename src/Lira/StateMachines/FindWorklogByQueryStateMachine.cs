using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Lira.Extensions;
using Lira.Objects;

namespace Lira.StateMachines;

/// <summary> State machine responsible for retrieving Worklogs matching given query. </summary>
public class FindWorklogByQueryStateMachine(LiraClient client) : FindByQueryStateMachineImpl<Worklog>(client)
{
    protected override bool IsCached(State state, out State newState)
    {
        newState = default;
        if (state.PaginationState.Query.TryGetQueryPart("jql", out var jqlQuery))
        {
            List<IssueCommon> issues = [];
            if (LiraClient.CheckWorklogCache((string)jqlQuery.Value, out var relevantIssues))
            {
                foreach (var issue in relevantIssues)
                {
                    if (LiraClient.TryGetCachedIssue(issue, out var cachedIssue))
                    {
                        issues.Add(cachedIssue);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            var cachedWorklogs = issues.SelectMany(x => x.Worklogs);
            var filtered = state.Query.FilterItems(cachedWorklogs, LiraClient).ToImmutableList();
            LiraClient.Logger.UsingCachedQuery((string)jqlQuery.Value);
            newState = state with
            {
                FinishedStep = Steps.LoadPayload,
                PaginationState = state.PaginationState with { FinishedStep = PaginationStateMachine<IssueLite>.Steps.End },
                Payload = filtered,

            };
            return true;
        }
        return false;
    }

    protected async override Task<State> LoadPayload(State state)
    {
        var loadedLogs = await LoadWorklogsImpl(state.PaginationState.Values).ConfigureAwait(false);
        var allWorklogs = state.PaginationState.Values.SelectMany(x => x.Worklogs);
        // Log.Information("Filtering worklogs");
        var worklogs = state.Query.FilterItems(loadedLogs, LiraClient).ToImmutableList();
        LiraClient.AddToCache(state.Query.BuildQueryString(LiraClient), worklogs);
        return state.Advance() with
        {
            Payload = worklogs,
        };
    }

    /// <summary>
    /// Gather all worklogs of all issues and prepared them for filtering
    /// </summary>
    /// <param name="issueLites"></param>
    /// <returns></returns>
    private async Task<List<Worklog>> LoadWorklogsImpl(IEnumerable<IssueLite> issueLites)
    {
        List<IssueLite> uncachedLites = [];
        List<Worklog> worklogs = [];
        foreach (var potentiallyUncached in issueLites)
        {
            var updatedOn = potentiallyUncached.Updated;
            if (LiraClient.TryGetCachedIssue(potentiallyUncached.Key, out Issue? issue) && issue.Fetched > updatedOn)
            {
                worklogs.AddRange(issue.Worklogs);
            }
            else if (LiraClient.TryGetCachedIssue(potentiallyUncached.Key, out IssueLite? cachedLite) && cachedLite.Fetched > updatedOn)
            {
                worklogs.AddRange(cachedLite.Worklogs);
            }
            else
            {
                uncachedLites.Add(potentiallyUncached);
            }
        }
        await uncachedLites.LoadWorklogs(LiraClient).ConfigureAwait(false);
        foreach (var issue in uncachedLites)
        {
            worklogs.AddRange(issue.Worklogs);
            LiraClient.AddToCache(issue);
        }
        return worklogs;
    }
}

