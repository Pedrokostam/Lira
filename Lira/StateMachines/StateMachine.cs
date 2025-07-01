using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Lira.Exceptions;
using Lira.Objects;
using Microsoft.Extensions.Logging;

namespace Lira.StateMachines;

public abstract class StateMachine<TState,TStep>(LiraClient client) : IStateMachine<TState>
    where TState : struct,IState<TStep>
    where TStep : Enum
{
    private readonly LiraClient _liraClient = client;

    protected IssueCache<Issue> Cache => LiraClient.Cache;
    protected IssueCache<IssueLite> CacheLite => LiraClient.CacheLite;
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
#if NETSTANDARD2_0
    protected TState AdjustState(in TState state)
#else
    protected TState AdjustState(ref readonly TState state)
#endif
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
        return HttpClient.GetAsync(requestAddress, LiraClient.GetToken());
    }
    protected Task<HttpResponseMessage> PostAsync(string requestAddress, HttpContent content)
    {
        return HttpClient.PostAsync(requestAddress, content, LiraClient.GetToken());
    }
    protected Task<HttpResponseMessage> PostAsync<T>(string requestAddress, T contentToJsonify)
    {
        var json = JsonHelper.Serialize<T>(contentToJsonify);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return HttpClient.PostAsync(requestAddress, content, LiraClient.GetToken());
    }
    protected Task<string> ReadContentString(HttpResponseMessage response)
    {
#if NETSTANDARD2_0
        return response.Content.ReadAsStringAsync();
#else
        return response.Content.ReadAsStringAsync(LiraClient.GetToken());
#endif
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

