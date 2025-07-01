using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lira.Objects;
/// <summary>
/// Class used to store already loaded Issues. It may speed up fetching.
/// </summary>
public class IssueCache
{
    public TimeSpan InvalidationPeriod { get; } = TimeSpan.FromMinutes(15);
    public IssueCache()
    {
        
    }
    public IssueCache(TimeSpan invalidationPeriod)
    {
        InvalidationPeriod = invalidationPeriod;
    }

    private readonly Dictionary<string, Issue> _dict = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new ();
    private DateTime OldestItemDate = DateTime.MaxValue;
    public Issue this[string key]
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
                if (value.Fetched < OldestItemDate)
                {
                    OldestItemDate = value.Fetched;
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

    public ICollection<Issue> Values
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

    public void Add(Issue value)
    {
        lock (_lock)
        {
            _dict[value.Key] = value;
            if (value.Fetched < OldestItemDate)
            {
                OldestItemDate = value.Fetched;
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
                    OldestItemDate = DateTime.MaxValue;
                }
                else if (removee.Fetched == OldestItemDate)
                {
                    OldestItemDate = _dict.Select(x => x.Value.Fetched).Min();
                }
                return true;
            }
            return false;
        }
    }

    public bool TryGetValue(string key, [NotNullWhen(returnValue: true)] out Issue? value)
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
            OldestItemDate = DateTime.MaxValue;
        }
    }

    public void PurgeOld()
    {
        var now = DateTime.UtcNow;
        if (OldestItemDate - now < InvalidationPeriod)
        {
            return;
        }
        var vals = _dict.Values.ToList();
        foreach (var issue in vals)
        {
            if (issue.Fetched - now > InvalidationPeriod)
            {
                _dict.Remove(issue.Key);
            }
        }
    }
}
