using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;
using Microsoft.Extensions.Logging;

namespace Lira;
internal static partial class LogMessages
{
    public enum Events
    {
        AuthorizationLost,
        Authorized,
        InvalidResponse,
        PaginatedResponse,
        CookieProviderPeriodicRecheck,
        CookieProviderPeriodicRecheckFinished,
        GotIssueFromQuery,
        ExecutingRequest,
        LoadingWorklogs,
        UsingCachedWorklogs,
        UsingCachedIssue,
        UsingCachedIssueLite,
        CachingQuery,
        CachingIssue,
        CachingIssueLite,
        RemoveIssueFromCache,
        RemovedIssueLiteFromCache,
        RemovedQueryFromCache,
        RemovedQueriesFromCacheByIssue,
        ClearingIssueCaches,
        ClearingQueryCache,
        LoadingWorklogsSubtask,
        UpliftingShallowIssue,
        StartedMethod,
        EndedMethod,
        CreatedNewInstance,
    }

    [LoggerMessage(
           EventId = (int)Events.CachingIssue,
           Level = LogLevel.Trace,
           Message = "Caching issue {Issue}"
           )]
    public static partial void CachingIssue(this ILogger logger, Issue issue);
    [LoggerMessage(
           EventId = (int)Events.ExecutingRequest,
           Level = LogLevel.Information,
           Message = "Executing {Type} request: {Url}"
           )]
    public static partial void ExecutingRequest(this ILogger logger, string type, string Url);
    [LoggerMessage(
           EventId = (int)Events.CachingIssueLite,
           Level = LogLevel.Information,
           Message = "Caching issue lite {Issue}"
           )]
    public static partial void CachingIssue(this ILogger logger, IssueLite issue);
    [LoggerMessage(
           EventId = (int)Events.ClearingIssueCaches,
           Level = LogLevel.Debug,
           Message = "Clearing issue caches"
           )]
    public static partial void ClearingIssueCache(this ILogger logger);
    [LoggerMessage(
          EventId = (int)Events.ClearingQueryCache,
          Level = LogLevel.Debug,
          Message = "Clearing query cache"
          )]
    public static partial void ClearingQueryCache(this ILogger logger);
    [LoggerMessage(
          EventId = (int)Events.RemoveIssueFromCache,
          Level = LogLevel.Information,
          Message = "Removed {Issue} from cache"
          )]
    public static partial void RemovingCacheEntry(this ILogger logger,Issue issue);
    [LoggerMessage(
        EventId = (int)Events.RemovedQueryFromCache,
        Level = LogLevel.Information,
        Message = "Removed query from cache {Query}"
        )]
    public static partial void RemovingCacheEntry(this ILogger logger, string query);
    [LoggerMessage(
        EventId = (int)Events.RemovedIssueLiteFromCache,
        Level = LogLevel.Information,
        Message = "Removed {Issue} from cache (lite)"
        )]
    public static partial void RemovingCacheEntry(this ILogger logger, IssueLite issue);
    [LoggerMessage(
        EventId = (int)Events.RemovedQueriesFromCacheByIssue,
        Level = LogLevel.Information,
        Message = "Removing queries tied to {issue}"
        )]
    public static partial void RemovedCacheEntryByIssue(this ILogger logger, string issue);

    [LoggerMessage(
           EventId = (int)Events.CreatedNewInstance,
           Level = LogLevel.Information,
           Message = "Created client instance for {Address}"
           )]
    public static partial void CreatedNewInstance(this ILogger logger, Uri address);

    [LoggerMessage(
           EventId = (int)Events.LoadingWorklogsSubtask,
           Level = LogLevel.Debug,
           Message = "Loading worklogs of subtasks of {Issue}: {Subtasks}"
           )]
    public static partial void LoadingWorklogsOfSubtask(this ILogger logger, Issue issue, IEnumerable<IssueStem> Subtasks);

    [LoggerMessage(
           EventId = (int)Events.CookieProviderPeriodicRecheck,
           Level = LogLevel.Trace,
           Message = "Periodic check for authorization."
           )]
    public static partial void CookieProviderRecheck(this ILogger logger);
    [LoggerMessage(
           EventId = (int)Events.CookieProviderPeriodicRecheckFinished,
           Level = LogLevel.Trace,
           Message = "Periodic check for authorization finished."
           )]
    public static partial void CookieProviderRecheckFinished(this ILogger logger);
    [LoggerMessage(
           EventId = (int)Events.GotIssueFromQuery,
           Level = LogLevel.Information,
           Message = "Query yielded {IssueCount} issues."
           )]
    public static partial void GotIssueFromQuery(this ILogger logger, int issueCount);

    [LoggerMessage(
           EventId = (int)Events.UpliftingShallowIssue,
           Level = LogLevel.Information,
           Message = "Converting IssueLite {issue} to Issue."
           )]
    public static partial void UpliftingIssueLite(this ILogger logger, IssueLite issue);

    [LoggerMessage(
           EventId = (int)Events.LoadingWorklogs,
           Level = LogLevel.Information,
           Message = "Loading worklogs of {Issue}."
           )]
    public static partial void LoadingWorklogs(this ILogger logger, IssueStem issue);

    [LoggerMessage(
           EventId = (int)Events.UsingCachedWorklogs,
           Level = LogLevel.Debug,
           Message = "Using cached worklogs of {Issue}."
           )]
    public static partial void UsingCachedWorklogs(this ILogger logger, IssueCommon issue);

    [LoggerMessage(
           EventId = (int)Events.UsingCachedIssue,
           Level = LogLevel.Debug,
           Message = "Using cached Issue {Issue}."
           )]
    public static partial void UsingCachedIssue(this ILogger logger, Issue issue);

    [LoggerMessage(
           EventId = (int)Events.UsingCachedIssueLite,
           Level = LogLevel.Debug,
           Message = "Using cached IssueLite {Issue}."
           )]
    public static partial void UsingCachedIssue(this ILogger logger, IssueLite issue);

    [LoggerMessage(
          EventId = (int)Events.UsingCachedWorklogs,
          Level = LogLevel.Debug,
          Message = "Using cached query {Query}."
          )]
    public static partial void UsingCachedQuery(this ILogger logger,string query);

    [LoggerMessage(
           EventId = (int)Events.CachingQuery,
           Level = LogLevel.Debug,
           Message = "Caching worklogs for query {Query}."
           )]
    public static partial void CachingWorklogsForQuery(this ILogger logger, string query);

    [LoggerMessage(
           EventId = (int)Events.PaginatedResponse,
           Level = LogLevel.Debug,
           Message = "Query {query} yielded paginated response {Pagination}."
           )]
    public static partial void PaginatedResponse(this ILogger logger, string query,PaginationParams pagination);
    [LoggerMessage(
        EventId = (int)Events.AuthorizationLost,
        Level = LogLevel.Information,
        Message = "Authorization {AuthorizationType} has expired."
        )]
    public static partial void AuthorizationLost(this ILogger logger, Type authorizationType);
    [LoggerMessage(
        EventId = (int)Events.Authorized,
        Level = LogLevel.Information,
        Message = "Authorized using {AuthorizationType}."
        )]
    public static partial void Authorized(this ILogger logger, Type authorizationType);
    [LoggerMessage(
      EventId = (int)Events.InvalidResponse,
      Level = LogLevel.Error,
      Message = "Request yielded invalid response {Exception}"
      )]
    public static partial void LogErrorResponse(this ILogger logger, Exception exception);
}