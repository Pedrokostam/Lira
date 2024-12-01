using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lira.Jql;
using Microsoft.AspNetCore.WebUtilities;

namespace Lira;
public class HttpQuery:IEnumerable<HttpQuery.QueryPart>
{
    public static QueryPart MaxResults(long maxResults) => new ("maxResults", maxResults);
    public static QueryPart StartsAt(long startsAt) => new ("startAt", startsAt);
    public static QueryPart JqlSearchQuery(string jqlString) => new ("jql", jqlString);
    [DebuggerDisplay("{Name,nq} = {ValueString,nq}")]
    public readonly record struct QueryPart(string Name, object Value)
    {
        public string ValueString => Value.ToString()!;
        public static implicit operator (string name, string value)(QueryPart value)
        {
            return (value.Name, value.ValueString);
        }

        public static implicit operator QueryPart((string name, object value) value)
        {
            return new QueryPart(value.name, value.value);
        }
        public KeyValuePair<string, string?> ToKeyValuePair() => new (Name, ValueString);

    }
    private Dictionary<string, QueryPart> _parts = new(StringComparer.OrdinalIgnoreCase);

    public void Add(QueryPart part)
    {
        _parts[part.Name] = part;
    }
    public void Add(string name, object value) => Add(new QueryPart(name, value));

    public bool Remove(string name) => _parts.Remove(name);

    public string AddQueryToEndpoint(Uri endpoint) => AddQueryToEndpoint(endpoint.OriginalString);
    public string AddQueryToEndpoint(string endpoint)
    {
#if NETSTANDARD2_0
        Dictionary<string, string> enumeration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in _parts.Values)
        {
            enumeration.Add(part.Name, part.ValueString);
        }
#else
        var enumeration = _parts.Values.Select(x => x.ToKeyValuePair());
#endif
        return QueryHelpers.AddQueryString(endpoint, enumeration);
    }

    public IEnumerator<QueryPart> GetEnumerator()=>_parts.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
