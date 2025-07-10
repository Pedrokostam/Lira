using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lira.Exceptions;
using Lira.Objects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lira.StateMachines;

public abstract class StateMachine<TState, TStep>(LiraClient client) : IStateMachine<TState>
    where TState : struct, IState<TStep, TState>
    where TStep : Enum
{
    private readonly LiraClient _liraClient = client;

    private IssueCache<Issue> CacheFull => LiraClient.Cache;
    private IssueCache<IssueLite> CacheLite => LiraClient.CacheLite;
    protected bool TryGetValue<T>(string key, [NotNullWhen(true)] out T? issue) where T : IssueCommon
    {
        if (typeof(T) == typeof(Issue))
        {
            bool res = CacheFull.TryGetValue(key, out var full);
            issue = full as T;
            LiraClient.Logger.UsingCachedIssue(full!);
            return res && issue is not null;
        }
        if (typeof(T) == typeof(IssueLite))
        {
            bool res = CacheLite.TryGetValue(key, out var lite);
            issue = lite as T;
            LiraClient.Logger.UsingCachedIssueLite(lite!);
            return res && issue is not null;
        }
        issue = null;
        return false;
    }
    protected void AddToCache(IssueCommon issue)
    {
        if (issue is Issue full)
        {
            CacheFull.Add(full);
            // When fetching a full issue, the lite version should be removed
            CacheLite.Remove(full.Key);
        }
        if (issue is IssueLite lite)
        {
            CacheLite.Add(lite);
            // One scenario where we add a lite issue is when adding a worklog to a cached issue
            // in that case the lite has newer information than the full
            if (CacheFull.TryGetValue(lite.Key, out var cachedFull) && cachedFull.Fetched < lite.Fetched)
            {
                CacheFull.Remove(lite.Key);
            }
        }
    }
    protected LiraClient LiraClient
    {
        get
        {
            if (_liraClient.IsDisposed)
            {
                throw new InvalidOperationException("Cannot used disposed client");
            }
            return _liraClient;
        }
    }

    protected HttpClient HttpClient => LiraClient.HttpClient;

    public ILogger Logger => LiraClient.Logger;

    public abstract Task<TState> Process(TState state);
    /// <summary>
    /// Creates new instance of <typeparamref name="TState"/> from the parameterless constructor if <paramref name="state"/> is equal to its <see langword="default"/> value.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    protected TState AdjustState(ref readonly TState state)
    {
        Debug.WriteLine($"Executing state {state}");
        if (EqualityComparer<TState>.Default.Equals(state, default!))
        {
            return new TState();
        }
        return state;
    }
    protected virtual async Task<TState> EnsureAuthorization(TState state)
    {
        if (!await Authorization.EnsureAuthorized(LiraClient).ConfigureAwait(false))
        {
            Logger.AuthorizationLost(Authorization.GetType());
            await Authorization.Authorize(LiraClient).ConfigureAwait(false);
        }
        return state with
        {
            FinishedStep = state.NextStep,
        };
    }

    private Authorization.IAuthorization Authorization => LiraClient.Authorization;

    protected Task<HttpResponseMessage> GetAsync(string requestAddress)
    {
        InvalidClientModeException.CheckGet(LiraClient);
        return HttpClient.GetAsync(requestAddress, LiraClient.GetToken());
    }
    protected Task<HttpResponseMessage> DeleteAsync(string requestAddress)
    {
        InvalidClientModeException.CheckDelete(LiraClient);
        return HttpClient.DeleteAsync(requestAddress, LiraClient.GetToken());
    }
    protected Task<HttpResponseMessage> PutAsync<T>(string requestAddress, T contentToJsonify)
    {
        InvalidClientModeException.CheckPut(LiraClient);
        var json = JsonHelper.Serialize<T>(contentToJsonify);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return HttpClient.PutAsync(requestAddress, content, LiraClient.GetToken());
    }
    protected Task<HttpResponseMessage> PostAsync(string requestAddress, HttpContent content)
    {
        InvalidClientModeException.CheckPost(LiraClient);
        return HttpClient.PostAsync(requestAddress, content, LiraClient.GetToken());
    }
    protected Task<HttpResponseMessage> PostAsync<T>(string requestAddress, T contentToJsonify)
    {
        InvalidClientModeException.CheckPost(LiraClient);
        var json = JsonHelper.Serialize<T>(contentToJsonify);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return HttpClient.PostAsync(requestAddress, content, LiraClient.GetToken());
    }
    protected Task<string> ReadContentString(HttpResponseMessage response)
    {
        return response.Content.ReadAsStringAsync(LiraClient.GetToken());
    }
    protected async Task<bool> HandleErrorResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        Exception? exception = null;
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            exception = await LiraClient.Authorization.CreateExceptionForUnauthorized(response).ConfigureAwait(false);
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
}

