using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lira.Objects;
/// <summary>
/// Class used to store already loaded Issues. It may speed up fetching.
/// </summary>
public class IssueCache<T> where T:IssueLite
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
    private readonly object _lock = new ();
    private DateTime _oldestItemDate = DateTime.MaxValue;
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

    public bool Remove(string key)
    {
        lock (_lock)
        {
            if (_dict.TryGetValue(key, out var removee))
            {
                _dict.Remove(key);
                if (_dict.Count == 0)
                {
                    _oldestItemDate = DateTime.MaxValue;
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
            _oldestItemDate = DateTime.MaxValue;
        }
    }

    public void PurgeOld()
    {
        var now = DateTime.UtcNow;
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
                Debug.WriteLine($"Cache removed {issue}");
                _dict.Remove(issue.Key);
            }
        }
    }
}
