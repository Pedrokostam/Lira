using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Lira.Authorization;
using Lira.Exceptions;
using Lira.Extensions;
using Lira.Jql;
using Lira.Objects;
using Lira.StateMachines;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lira;

public partial class LiraClient : IDisposable
{
    private const string ApplicationAgentName = "Lira_";
    public const string LoginEndpoint = "rest/auth/1/session";
    public const string MyselfEndpoint = "rest/api/2/myself";
    public const string SearchEndpoint = "rest/api/2/search";
    public const string IssueEndpoint = "rest/api/2/issue";
    public const string UserSearchEndpoint = "rest/api/2/user/search";

    #region StateMachines
    private readonly UsersMachine _usersMachine;
    private readonly WorklogMachine _worklogMachine;
    private readonly PaginationMachine<Issue> _issuePaginationMachine;
    private readonly IssueMachine _issueMachine;
    private readonly AddWorklogMachine _addWorklogMachine;
    private readonly CurrentUserMachine _currentUserMachine;

    internal IssueCache<Issue> Cache { get; } = new();
    internal IssueCache<IssueLite> CacheLite { get; } = new();

    public UsersMachine GetUsersStateMachine() => _usersMachine;
    public WorklogMachine GetWorklogStateMachine() => _worklogMachine;
    internal PaginationMachine<Issue> GetIssuePaginationStateMachine() => _issuePaginationMachine;
    public IssueMachine GetIssueStateMachine() => _issueMachine;
    public AddWorklogMachine GetAddWorklogMachine() => _addWorklogMachine;
    public CurrentUserMachine GetCurrentUserMachine() => _currentUserMachine;
    #endregion StateMachines

    internal LiraClient(Uri baseAddress, ILogger logger)
    {
        Logger = logger;
        HttpClient = new HttpClient()
        {
            BaseAddress = baseAddress,
            DefaultRequestHeaders = {
                Accept={ new MediaTypeWithQualityHeaderValue("application/json") { CharSet = Encoding.UTF8.WebName } },
            },
        };
        HttpClient.DefaultRequestHeaders.Add("User-Agent", ApplicationAgentName);
        _usersMachine = new(this);
        _worklogMachine = new(this);
        _issuePaginationMachine = new(this, SearchEndpoint, "issues");
        _issueMachine = new(this);
        _addWorklogMachine = new(this);
        _currentUserMachine = new(this);
    }

    #region Properties
    public bool IsDisposed { get; private set; }
    public Uri ServerAddress => HttpClient.BaseAddress!;
    public HttpClient HttpClient { get; }
    public UserDetails Myself { get; private set; } = default!;
    public TimeZoneInfo AccountTimezone => Myself.TimeZone ?? TimeZoneInfo.Local;
    public IAuthorization Authorization { get; internal set; } = default!;
    public CancellationTokenSource CancellationTokenSource { get; } = new();
    public ILogger Logger { get; } = NullLogger<LiraClient>.Instance;
    #endregion Properties

    public static string GetIssueEndpoint(string issueId) => $"{IssueEndpoint}/{issueId}";

    internal async Task EnsureAuthorized()
    {
        if (!await Authorization.EnsureAuthorized(this).ConfigureAwait(false))
        {
            Logger.AuthorizationLost(Authorization.GetType());
            await Authorization.Authorize(this).ConfigureAwait(false);
        }
    }
    // TODO tests
    public static Uri GetJiraServerUrl(string serverAddress)
    {
        var urlMatch = UrlValidator().Match(serverAddress);
        string scheme = "https";
        if (!urlMatch.Success)
        {
            throw new InvalidOperationException($"Specified address: \"{serverAddress}\" is invalid");
        }
        string host = urlMatch.Groups["domain"].Value;
        if (urlMatch.Groups.TryGetValue("scheme", out var protocolGroup))
        {
            scheme = protocolGroup.Value;
        }
        return new Uri($"{scheme}://{host}", UriKind.Absolute);

    }

    internal async Task GetCurrentUser()
    {
        var machine = GetCurrentUserMachine();
        var state = machine.GetStartState();
        state = await ThisMachine(machine, state).ConfigureAwait(false);
        Myself = state.User ?? throw new ArgumentNullException("Current user");
        // Log.Verbose("Parsed current user: {currentUser}", userDetails);
    }
    #region Main methods
    public async Task<IList<UserDetails>> GetUsers(string username)
    {
        var machine = GetUsersStateMachine();
        var state = machine.GetStartState(username);
        state = await ThisMachine(machine, state).ConfigureAwait(false);
        return state.Users;
    }
    /// <summary>
    /// I am...
    /// <para/>
    /// ALL OF ME
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="machine"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    private async Task<TState> ThisMachine<TState>(IStateMachine<TState> machine, TState state) where TState : IState
    {
        while (state.ShouldContinue)
        {
            state = await machine.Process(state).ConfigureAwait(false);
        }
        return state;
    }
    public async Task<Issue?> GetIssue(string issueId)
    {
        var machine = GetIssueStateMachine();
        var state = machine.GetStartState(issueId);
        state = await ThisMachine(machine, state).ConfigureAwait(false);
        return state.Issue;
    }
    public async Task<IList<Worklog>> GetWorklogs(JqlQuery query)
    {
        var machine = GetWorklogStateMachine();
        var state = machine.GetStartState(query);
        state = await ThisMachine(machine, state).ConfigureAwait(false);
        return state.Worklogs;
    }
    public Task<Worklog?> AddWorklog(string issueId, DateTimeOffset started, TimeSpan timeSpent, string? comment) => AddWorklog(issueId, new(started, timeSpent, comment));
    public async Task<Worklog?> AddWorklog(string issueId, WorklogToAdd worklogToAdd)
    {
        var machine = GetAddWorklogMachine();
        var state = machine.GetStartState(issueId, worklogToAdd);
        state = await ThisMachine(machine, state).ConfigureAwait(false);
        return state.AddedWorklog;
    }
    #endregion Main methods
    //var log = new WorklogToAdd(comment, timeSpent, started);
    //var address = $"{GetIssueEndpoint(issueId)}/worklog";
    //var response = await PostAsync(address, log).ConfigureAwait(false);
    //await HandleErrorResponse(response).ConfigureAwait(false);
    //var responseContent = await ReadContentString(response).ConfigureAwait(false);
    //var addedWorklog = JsonHelper.Deserialize<Worklog>(responseContent);
    //return addedWorklog;

    internal async Task<bool> HandleErrorResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        Exception? exception = null;
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            exception = await Authorization.CreateExceptionForUnauthorized(response).ConfigureAwait(false);
        }
        if (exception is null)
        {
            var errorResponse = await BaseHttpException.GetErrorResponse(response).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                exception = new BadRequestException(errorResponse);
            }
            else
            {
                exception = new BaseHttpException(response.StatusCode, errorResponse);
            }
        }
        Logger.LogErrorResponse(exception);
        throw exception;
    }
    //var stamp = LogStart();
    //await EnsureAuthorized().ConfigureAwait(false);
    //var endpoint = GetIssueEndpoint(issueId);
    //var response = await GetAsync(endpoint).ConfigureAwait(false);
    //await HandleErrorResponse(response).ConfigureAwait(false);

    //var stringContent = await ReadContentString(response).ConfigureAwait(false);
    //var issue = JsonHelper.Deserialize<Issue>(stringContent);
    //if (issue is null)
    //{
    //    return null;
    //}
    //await issue.LoadWorklogsRecurse(this).ConfigureAwait(false);
    //LogEnd(stamp);
    //return issue;
    //public async Task<IList<Worklog>> GetWorklogs_Deprecated(JqlQuery query)
    //{
    //    var stamp = LogStart();
    //    await EnsureAuthorized().ConfigureAwait(false);
    //    var queryString = query.BuildQueryString(this);
    //    HttpQuery httpQuery = [HttpQuery.JqlSearchQuery(queryString), HttpQuery.MaxResults(100)];
    //    var endpointAddress = httpQuery.AddQueryToEndpoint(SearchEndpoint);
    //    var issues = await GetPaginatedResponse<Issue>(new Uri(SearchEndpoint, UriKind.Relative), httpQuery, "issues").ConfigureAwait(false);
    //    var collection = issues.ToList();

    //    Logger.GotIssueFromQuery(collection.Count);
    //    await collection.LoadWorklogs(this).ConfigureAwait(false);
    //    var allWorklogs = collection.SelectMany(x => x.Worklogs);
    //    // Log.Information("Filtering worklogs");
    //    var filtered = query.FilterItems(allWorklogs, this).ToList();
    //    LogEnd(stamp);
    //    return filtered;
    //}

    /// <summary>
    /// Return all worklogs that match the <paramref name="query"/>
    /// </summary>
    /// <remarks>
    /// This method is a wrapper along <see cref="WorklogMachine"/>. If you want to inject additional work between steps of the workflow, use the machine.</remarks>
    /// <param name="query"></param>
    /// <returns></returns>



    private long LogStart([CallerMemberName] string? name = null)
    {
        Logger.StartedMethod(name);
        return Stopwatch.GetTimestamp();
    }
    private void LogEnd(long timestamp, [CallerMemberName] string? name = null)
    {
        var diff = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - timestamp);
        Logger.EndedMethod(diff, name);
    }


    //public async Task<IList<T>> GetPaginatedResponse<T>(string endpoint, HttpQuery query, string propertyName)
    //    => await GetPaginatedResponse<T>(new Uri(endpoint, UriKind.Relative), query, propertyName).ConfigureAwait(false);
    //public async Task<IList<T>> GetPaginatedResponse<T>(Uri endpoint, HttpQuery query, string propertyName)
    //{
    //    await EnsureAuthorized().ConfigureAwait(false);
    //    PaginationParams paginationParams;
    //    List<T> values = [];
    //    do
    //    {
    //        var queryAddress = query.AddQueryToEndpoint(endpoint);
    //        var response = await GetAsync(queryAddress).ConfigureAwait(false);
    //        await HandleErrorResponse(response).ConfigureAwait(false);

    //        var responseString = await ReadContentString(response).ConfigureAwait(false);
    //        paginationParams = PaginationParams.FromResponse(responseString);
    //        Logger.PaginatedResponse(Uri.UnescapeDataString(queryAddress), paginationParams);
    //        var pageValues = JsonHelper.Deserialize<IList<T>>(responseString, propertyName);
    //        if (pageValues is not null)
    //        {
    //            values.AddRange(pageValues);
    //        }
    //        paginationParams.UpdateQuery(query);
    //    } while (paginationParams.ShouldRequestNewPage);
    //    return values;
    //}

    //public async IAsyncEnumerable<T> GetPaginatedResponseEnumerable<T>(Uri endpoint, HttpQuery query, string propertyName)
    //{
    //    await EnsureAuthorized().ConfigureAwait(false);
    //    PaginationParams paginationParams;
    //    do
    //    {
    //        var queryAddress = query.AddQueryToEndpoint(endpoint);
    //        var response = await GetAsync(queryAddress).ConfigureAwait(false);
    //        await HandleErrorResponse(response).ConfigureAwait(false);

    //        var responseString = await ReadContentString(response).ConfigureAwait(false);
    //        paginationParams = PaginationParams.FromResponse(responseString);
    //        var pageValues = JsonHelper.Deserialize<IList<T>>(responseString, propertyName);
    //        if (pageValues is not null)
    //        {
    //            foreach (var value in pageValues)
    //            {
    //                yield return value;
    //            }
    //        }
    //        paginationParams.UpdateQuery(query);
    //    } while (paginationParams.ShouldRequestNewPage);
    //}


    //    public Task<HttpResponseMessage> GetAsync(string requestAddress)
    //    {
    //        return HttpClient.GetAsync(requestAddress, GetToken());
    //    }
    //    public Task<HttpResponseMessage> PostAsync(string requestAddress, HttpContent content)
    //    {
    //        return HttpClient.PostAsync(requestAddress, content, GetToken());
    //    }
    //    public Task<HttpResponseMessage> PostAsync<T>(string requestAddress, T contentToJsonify)
    //    {
    //        var json = JsonHelper.Serialize<T>(contentToJsonify);
    //        var content = new StringContent(json, Encoding.UTF8, "application/json");
    //        return HttpClient.PostAsync(requestAddress, content, GetToken());
    //    }
    //    internal Task<string> ReadContentString(HttpResponseMessage response)
    //    {
    //#if NETSTANDARD2_0
    //        return response.Content.ReadAsStringAsync();
    //#else
    //        return response.Content.ReadAsStringAsync(GetToken());
    //#endif
    //    }
    internal CancellationToken GetToken() => CancellationTokenSource.Token;

    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                HttpClient.Dispose();
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            IsDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    private const string UrlRegex = @"^((?<scheme>https?):\/\/)?(?<domain>[A-Z0-9\.\-]+\.[A-Z0-9\-]+)((?<subpath>\/.*))?";
    private const RegexOptions UrlRegexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;
    private const int RegexTimeout = 250;
#if NETSTANDARD2_0
    private static readonly Regex _urlValidator=new(UrlRegex,UrlRegexOptions, TimeSpan.FromMilliseconds(RegexTimeout));
    private static Regex UrlValidator()=>_urlValidator;
#else
    [GeneratedRegex(UrlRegex, UrlRegexOptions, RegexTimeout)]
    private static partial Regex UrlValidator();
#endif
    //public async Task GetAsync(Uri requestUri)
    //{
    //    await Client.GetAsync(requestUri, GetToken()).ConfigureAwait(false);
    //}
    //public async Task PostAsync(Uri requestUri,HttpContent content)
    //{
    //    await Client.PostAsync(requestUri, content, GetToken()).ConfigureAwait(false);
    //}
    //public async Task PostAsync<T>(Uri requestUri, T contentToJsonify)
    //{
    //    var json = JsonHelper.Serialize<T>(contentToJsonify);
    //    var content = new StringContent(json, Encoding.UTF8, "application/json");
    //    await Client.PostAsync(requestUri, contentToJsonify, GetToken()).ConfigureAwait(false);
    //}

}
