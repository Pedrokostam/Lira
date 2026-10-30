using System.Diagnostics.CodeAnalysis;

namespace Lira.Toggl
{
    internal class TogglCache
    {
        public TimeSpan InvalidationPeriod { get; } = TimeSpan.FromMinutes(10);
        private DateTimeOffset _oldestDate = DateTimeOffset.MaxValue;
        private readonly object _cacheLock = new();
        private Dictionary<string, CacheItem<List<TogglTimeEntry>>> _entries = [];
        private Dictionary<long, TogglDtoClient> _clients = [];
        private Dictionary<long, TogglDtoProject> _projects = [];
        bool _loaded = false;

        public void Purge(bool complete = false)
        {

            if (complete)
            {
                lock (_cacheLock)
                {
                    _entries.Clear();
                    _clients.Clear();
                    _projects.Clear();
                    _loaded = false;
                }
            }
            else
            {
                PurgeOld();
            }
        }

        public void ClearEntries()
        {
            lock (_cacheLock)
            {
                _entries.Clear();
                _oldestDate = DateTimeOffset.MaxValue;
            }
        }

        private void PurgeOld()
        {
            lock (_cacheLock)
            {
                var start = DateTimeOffset.UtcNow;
                if (start - _oldestDate <= InvalidationPeriod)
                {
                    return;
                }
                DateTimeOffset oldestDate = DateTimeOffset.MaxValue;
                List<string> keysToRemove = [];
                foreach (var kvp in _entries)
                {
                    if (start - kvp.Value.Fetched > InvalidationPeriod)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                    else
                    {
                        if (kvp.Value.Fetched < oldestDate)
                        {
                            oldestDate = kvp.Value.Fetched;
                        }
                    }
                }
                foreach (var key in keysToRemove)
                {
                    _entries.Remove(key);
                }
                if (oldestDate == DateTimeOffset.MaxValue)
                {
                    oldestDate = DateTimeOffset.MinValue;
                }
                _oldestDate = oldestDate;
            }
        }

        public void SetProjectsClients(IEnumerable<TogglDtoProject> projects, IEnumerable<TogglDtoClient> clients)
        {
            var projectMap = projects.ToDictionary(x => x.Id);
            var clientMap = clients.ToDictionary(x => x.Id);
            lock (_cacheLock)
            {
                _projects = projectMap;
                _clients = clientMap;
                _loaded = true;
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if every project and client referenced by <paramref name="entries"/> is cached.
        /// </summary>
        public bool CanComplete(IEnumerable<TogglTimeEntry> entries)
        {
            lock (_cacheLock)
            {
                if (!_loaded)
                {
                    return false;
                }
                foreach (var e in entries)
                {
                    if (e.ProjectId is not long pid)
                    {
                        continue;
                    }
                    if (!_projects.TryGetValue(pid, out var proj))
                    {
                        return false;
                    }
                    if (proj.ClientId is long cid && !_clients.ContainsKey(cid))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public IReadOnlyList<TogglTimeEntry> CompleteAndAddEntries(string query, IEnumerable<TogglTimeEntry> entries)
        {
            List<TogglTimeEntry> completedEntries = [];
            if (entries.TryGetNonEnumeratedCount(out var c))
            {
                completedEntries.EnsureCapacity(c);
            }
            lock (_cacheLock)
            {
                if (!_loaded)
                {
                    throw new InvalidOperationException("You must first populate the client and project cache!");
                }
                foreach (var e in entries)
                {
                    // Projects deleted in Toggl are not returned by the API, so their client stays unknown.
                    if (e.ProjectId is long pid
                        && _projects.TryGetValue(pid, out var proj)
                        && proj.ClientId is long cid
                        && _clients.TryGetValue(cid, out var client))
                    {
                        e.ClientName = client.Name;
                    }
                    completedEntries.Add(e);
                }
                if (_entries.Count == 0)
                {
                    _oldestDate = DateTimeOffset.UtcNow;
                }
                _entries[query] = new CacheItem<List<TogglTimeEntry>>(completedEntries);
            }
            return completedEntries.AsReadOnly();
        }

        public bool TryGetCachedEntries(string query, [NotNullWhen(returnValue: true)] out IReadOnlyList<TogglTimeEntry>? entries)
        {
            lock (_cacheLock)
            {
                if (_entries.TryGetValue(query, out var items))
                {
                    var elapsed = DateTimeOffset.UtcNow - items.Fetched;
                    if (elapsed > InvalidationPeriod)
                    {
                        _entries.Remove(query);
                        _oldestDate = _entries.Count > 0 ? _entries.Min(x => x.Value.Fetched) : DateTimeOffset.MinValue;
                        entries = null;
                        return false;
                    }
                    entries = items.Item.AsReadOnly();
                    return true;
                }
                entries = null;
                return false;
            }
        }
    }
}
