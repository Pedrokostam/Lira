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
        LoadingWorklogs,
        UsingCachedWorklogs,
        CachingWorklogs,
        LoadingWorklogsSubtask,
        UpliftingShallowIssue,
        StartedMethod,
        EndedMethod,
        CreatedNewInstance,
    }

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
           EventId = (int)Events.StartedMethod,
           Level = LogLevel.Information,
           Message = "Started {Method}."
           )]
    public static partial void StartedMethod(this ILogger logger, [CallerMemberName] string? method=null);
    [LoggerMessage(
           EventId = (int)Events.EndedMethod,
           Level = LogLevel.Information,
           Message = "Ended {Method} (execution time: {TimeSpan:%s'.'ffff' seconds'})."
           )]
    public static partial void EndedMethod(this ILogger logger,TimeSpan timeSpan, [CallerMemberName] string? method = null);
    [LoggerMessage(
           EventId = (int)Events.CookieProviderPeriodicRecheck,
           Level = LogLevel.Debug,
           Message = "Periodic check for authorization."
           )]
    public static partial void CookieProviderRecheck(this ILogger logger);
    [LoggerMessage(
           EventId = (int)Events.CookieProviderPeriodicRecheckFinished,
           Level = LogLevel.Debug,
           Message = "Periodic check for authorization finished."
           )]
    public static partial void CookieProviderRecheckFinished(this ILogger logger);
    [LoggerMessage(
           EventId = (int)Events.GotIssueFromQuery,
           Level = LogLevel.Information,
           Message = "query yielded {IssueCount} issues."
           )]
    public static partial void GotIssueFromQuery(this ILogger logger, int issueCount);

    [LoggerMessage(
           EventId = (int)Events.UpliftingShallowIssue,
           Level = LogLevel.Debug,
           Message = "Converting IssueStem {issue} to Issue."
           )]
    public static partial void UpliftingShallowIssue(this ILogger logger, IssueStem issue);

    [LoggerMessage(
           EventId = (int)Events.LoadingWorklogs,
           Level = LogLevel.Debug,
           Message = "Loading worklogs of {Issue}."
           )]
    public static partial void LoadingWorklogs(this ILogger logger, IssueStem issue);

    [LoggerMessage(
           EventId = (int)Events.UsingCachedWorklogs,
           Level = LogLevel.Debug,
           Message = "Using cached worklogs of {Issue}."
           )]
    public static partial void UsingCachedWorklogs(this ILogger logger, Issue issue);

    [LoggerMessage(
           EventId = (int)Events.CachingWorklogs,
           Level = LogLevel.Debug,
           Message = "Caching worklogs of {Issue}."
           )]
    public static partial void CachingWorklogs(this ILogger logger, IssueStem issue);

    [LoggerMessage(
           EventId = (int)Events.PaginatedResponse,
           Level = LogLevel.Debug,
           Message = "query {query} yielded paginated response {Pagination}."
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
      Message = "Request to yielded invalid response."
      )]
    public static partial void LogErrorResponse(this ILogger logger, Exception exception);
}