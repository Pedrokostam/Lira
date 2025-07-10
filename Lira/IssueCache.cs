using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;

namespace Lira;
public class QueryCache
{
    public TimeSpan InvalidationPeriod { get; } = TimeSpan.FromMinutes(10);
    private DateTimeOffset _oldestItemDate = DateTimeOffset.MaxValue;

    private readonly record struct QueryEntry(ImmutableHashSet<string> Issues, DateTimeOffset Fetched)
    {
        public QueryEntry(IEnumerable<string> issues) : this(
            ImmutableHashSet.CreateRange(StringComparer.OrdinalIgnoreCase, issues),
            DateTimeOffset.UtcNow)
        { }
    }
    private readonly object _lock = new();
    private Dictionary<string, QueryEntry> _dict = new Dictionary<string, QueryEntry>(StringComparer.OrdinalIgnoreCase);
    private void PurgeOld()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _oldestItemDate < InvalidationPeriod)
        {
            return;
        }
        Debug.WriteLine("Invalidation!");
        List<KeyValuePair<string, QueryEntry>> vals = _dict.ToList();
        foreach (var entry in vals)
        {
            if (now - entry.Value.Fetched > InvalidationPeriod)
            {
                Debug.WriteLine($"Cached query removed {entry.Key}");
                _dict.Remove(entry.Key);
            }
        }
    }

    public void Add(string query, IEnumerable<IssueCommon> issues)
    {
        lock (_lock)
        {
            _dict[query] = new(issues.Select(x => x.Key));
            if (_dict[query].Fetched < _oldestItemDate)
            {
                _oldestItemDate = _dict[query].Fetched;
            }
        }
    }

    public bool Remove(string query)
    {
        lock (_lock)
        {
            if (_dict.TryGetValue(query, out var removee))
            {
                _dict.Remove(query);
                if (_dict.Count == 0)
                {
                    _oldestItemDate = DateTimeOffset.MaxValue;
                }
                else if (removee.Fetched == _oldestItemDate)
                {
                    _oldestItemDate = _dict.Select(x => x.Value.Fetched).Min();
                }
                return true;
            }
            return false;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _dict.Clear();
            _oldestItemDate = DateTimeOffset.MaxValue;
        }
    }

    public void InvalidateEntryByIssue(IssueCommon issue) => RemoveEntryByIssue(issue.Key);
    public void RemoveEntryByIssue(string issue)
    {
        lock (_lock)
        {
            List<string> toRemove = [];
            foreach (var entry in _dict)
            {
                if (entry.Value.Issues.Contains(issue))
                {
                    toRemove.Add(entry.Key);
                }
            }
            foreach (var entry in toRemove)
            {
                _dict.Remove(entry);
            }
            if (_dict.Count == 0)
            {
                _oldestItemDate = DateTimeOffset.MaxValue;
            }
            else
            {
                _oldestItemDate = _dict.Select(x => x.Value.Fetched).Min();
            }
        }
    }

    public bool TryGetValue(string key, [NotNullWhen(returnValue: true)] out IReadOnlyCollection<string>? issueKeys)
    {
        lock (_lock)
        {
            PurgeOld();
            bool found = _dict.TryGetValue(key, out var entry);
            if (found)
            {
                issueKeys = entry.Issues;
                return true;
            }
            issueKeys = null;
            return false;
        }
    }
}
/// <summary>
/// Class used to store already loaded Issues. It may speed up fetching.
/// </summary>
public class IssueCache<T> where T : IssueCommon
{
    public TimeSpan InvalidationPeriod { get; } = TimeSpan.FromMinutes(15);
    public IssueCache()
    {

    }
    public IssueCache(TimeSpan invalidationPeriod)
    {
        InvalidationPeriod = invalidationPeriod;
    }

    private readonly Dictionary<string, T> _dict = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private DateTimeOffset _oldestItemDate = DateTimeOffset.MaxValue;
    public T this[string key]
    {
        get
        {
            lock (_lock)
            {
                PurgeOld();
                return _dict[key];
            }
        }

        set
        {
            lock (_lock)
            {
                _dict[key] = value;
                if (value.Fetched < _oldestItemDate)
                {
                    _oldestItemDate = value.Fetched;
                }
            }
        }
    }

    public ICollection<string> Keys
    {
        get
        {
            lock (_lock)
            {
                PurgeOld();
                return _dict.Keys;
            }
        }
    }

    public ICollection<T> Values
    {
        get
        {
            lock (_lock)
            {
                PurgeOld();
                return _dict.Values;
            }
        }
    }

    public int Count => _dict.Count;

    public void Add(T value)
    {
        lock (_lock)
        {
            _dict[value.Key] = value;
            if (value.Fetched < _oldestItemDate)
            {
                _oldestItemDate = value.Fetched;
            }
        }
    }

    public bool ContainsKey(string key)
    {

        lock (_lock)
        {
            PurgeOld();
            return _dict.ContainsKey(key);
        }
    }

    public T? Remove(string key)
    {
        lock (_lock)
        {
            if (_dict.TryGetValue(key, out var removee))
            {
                _dict.Remove(key);
                if (_dict.Count == 0)
                {
                    _oldestItemDate = DateTimeOffset.MaxValue;
                }
                else if (removee.Fetched == _oldestItemDate)
                {
                    _oldestItemDate = _dict.Select(x => x.Value.Fetched).Min();
                }
                return removee;
            }
            return default;
        }
    }

    public bool TryGetValue(string key, [NotNullWhen(returnValue: true)] out T? value)
    {
        lock (_lock)
        {
            PurgeOld();
            return _dict.TryGetValue(key, out value);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _dict.Clear();
            _oldestItemDate = DateTimeOffset.MaxValue;
        }
    }

    public void PurgeOld()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _oldestItemDate < InvalidationPeriod)
        {
            return;
        }
        Debug.WriteLine("Invalidation!");
        var vals = _dict.Values.ToList();
        foreach (var issue in vals)
        {
            if (now - issue.Fetched > InvalidationPeriod)
            {
                Debug.WriteLine($"CacheFull removed {issue}");
                _dict.Remove(issue.Key);
            }
        }
    }
}
