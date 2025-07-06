using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lira.Objects;

namespace Lira.Extensions;
public static class WorklogExtensions
{
    private static readonly OrderedDictionary Accesors = new OrderedDictionary()
    {
        [nameof(Worklog.Issue)] = (Func<Worklog, object>)((Worklog log) => log.Issue.Key),
        [nameof(Worklog.Author)] = (Func<Worklog, object>)((Worklog log) => log.Author.Name),
        [nameof(Worklog.UpdateAuthor)] = (Func<Worklog, object>)((Worklog log) => log.UpdateAuthor.Name),
        [nameof(Worklog.TimeSpent)] = (Func<Worklog, object>)((Worklog log) => log.TimeSpent),
        [nameof(Worklog.TimeSpent) + "Seconds"] = (Func<Worklog, object>)((Worklog log) => (int)log.TimeSpent.TotalSeconds),
        [nameof(Worklog.Started)] = (Func<Worklog, object>)((Worklog log) => log.Started.ToString("O")),
        [nameof(Worklog.Created)] = (Func<Worklog, object>)((Worklog log) => log.Created.ToString("O")),
        [nameof(Worklog.Updated)] = (Func<Worklog, object>)((Worklog log) => log.Updated.ToString("O")),
        [nameof(Worklog.SelfLink)] = (Func<Worklog, object>)((Worklog log) => log.SelfLink),
        [nameof(Worklog.Comment)] = (Func<Worklog, object>)((Worklog log) => log.Comment),
    };

    public static string GetJsonString(this Worklog log, JsonSerializerOptions? options = null)
    {
        options ??= JsonSerializerOptions.Default;
        var dict = GetDict(log);
        return JsonSerializer.Serialize(dict, options);
    }
    private static List<string>? _csvColumns = null;
    public static IReadOnlyCollection<string> GetCsvColumns()
    {
        _csvColumns ??= Accesors.Keys.Cast<string>().Where(x => !x.Equals(nameof(Worklog.Comment), StringComparison.Ordinal)).ToList();
        return _csvColumns.AsReadOnly();
    }

    public static string GetCsvHeaderLine(string separator = ",")
    {
        return string.Join(separator, GetCsvColumns());
    }
    public static string GetCsvLine(this Worklog log,string separator = ",")
    {
        var keys = GetCsvColumns();
        var dict = GetDict(log);
        return string.Join(separator, keys.Select(k => dict[k]!.ToString()));
    }

    /// <summary>
    /// Get simplified representation of the object. Recommended for serialization.
    /// </summary>
    /// <returns></returns>
    public static OrderedDictionary GetDict(this Worklog log)
    {
        var d = new OrderedDictionary();
        foreach (DictionaryEntry kv in Accesors)
        {
            var func = (Func<Worklog, object>)kv.Value!;
            d[kv.Key] = func(log);
        }
        return d;
    }
}
