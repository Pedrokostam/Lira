using Lira.Exceptions;
using System.Net;
using System.Text;

namespace Lira.Toggl
{
    public class TogglClient
    {
        private readonly TogglCache _cache = new();
        private static readonly object _clientLock = new();
        private static Dictionary<string, TogglClient> _clients = new(StringComparer.Ordinal);
        public static TogglClient GetTogglClient(string apiKey)
        {
            lock (_clientLock)
            {
                if (!_clients.TryGetValue(apiKey, out var client))
                {
                    client = new TogglClient(apiKey);
                    _clients[apiKey] = client;
                }
                return client;
            }
        }

        /// <summary>
        /// Clears cached entries of every created client. If <paramref name="complete"/> is set, also clears projects and clients.
        /// </summary>
        public static void ClearAllCaches(bool complete)
        {
            lock (_clientLock)
            {
                foreach (var client in _clients.Values)
                {
                    client.ClearCache(complete);
                }
            }
        }

        private readonly HttpClient _client;

        private TogglClient(string apiKey)
        {
            _client = new HttpClient() { BaseAddress = new Uri("https://api.track.toggl.com/api/v9/") };
            string token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:api_token"));
            _client.DefaultRequestHeaders.Authorization = new("Basic", token);
        }

        public void ClearCache(bool complete)
        {
            if (complete)
            {
                _cache.Purge(complete: true);
            }
            else
            {
                _cache.ClearEntries();
            }
        }

        public async Task<IReadOnlyList<TogglTimeEntry>> GetEntries(DateTimeOffset from, DateTimeOffset to, bool forceRefresh = false)
        {
            // toggl has exlusive end dates
            string uri = $"me/time_entries?meta=true&start_date={from:yyyy-MM-dd}&end_date={to.AddDays(1):yyyy-MM-dd}";
            if (!forceRefresh && _cache.TryGetCachedEntries(uri, out var cached))
            {
                return cached;
            }
            var entries = await Get<List<TogglTimeEntry>>(uri).ConfigureAwait(false) ?? [];
            entries.RemoveAll(x => x.Duration < TimeSpan.Zero);
            if (!_cache.CanComplete(entries))
            {
                await LoadProjectsClients().ConfigureAwait(false);
            }
            return _cache.CompleteAndAddEntries(uri, entries);
        }

        private async Task LoadProjectsClients()
        {
            // Both endpoints return everything in one call - never query per project.
            var projects = await Get<List<TogglDtoProject>>("me/projects?include_archived=true").ConfigureAwait(false) ?? [];
            var clients = await Get<List<TogglDtoClient>>("me/clients").ConfigureAwait(false) ?? [];
            _cache.SetProjectsClients(projects, clients);
        }

        private async Task<T?> Get<T>(string uri)
        {
            var payload = await _client.GetAsync(uri).ConfigureAwait(false);
            if (!payload.IsSuccessStatusCode)
            {
                throw await CreateException(payload).ConfigureAwait(false);
            }
            var content = await payload.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonHelper.Deserialize<T>(content);
        }

        private static async Task<BaseHttpException> CreateException(HttpResponseMessage response)
        {
            var errorResponse = await BaseHttpException.GetErrorResponse(response).ConfigureAwait(false);
            return response.StatusCode switch
            {
                // Toggl answers an invalid API token with 403, not 401.
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    => new InvalidCredentialsException($"Toggl rejected the API key. Update it with Set-LiraTogglConfiguration. Response: {errorResponse}"),
                HttpStatusCode.BadRequest => new BadRequestException(errorResponse),
                HttpStatusCode.TooManyRequests => new BaseHttpException(response.StatusCode, $"Toggl rate limit exceeded. Try again later. Response: {errorResponse}"),
                _ => new BaseHttpException(response.StatusCode, errorResponse),
            };
        }
    }
}
