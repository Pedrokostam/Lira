using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lira;

[StructLayout(LayoutKind.Auto)]
public readonly record struct PaginationParams(
    [property: JsonPropertyName("startAt")] long StartAt,
    [property: JsonPropertyName("maxResults")] long MaxResults,
    [property: JsonPropertyName("total")] long Total
    )
{
    public long EndsAt => StartAt + MaxResults;
    public static PaginationParams FromResponse(string responseString)
    {
        return JsonHelper.Deserialize<PaginationParams>(responseString);
    }
    public static PaginationParams FromResponse(JsonElement responseElement)
    {
        return JsonHelper.Deserialize<PaginationParams>(responseElement);
    }
    public static async Task<PaginationParams> FromResponse(Stream responseStream)
    {
        return await JsonHelper.DeserializeAsync<PaginationParams>(responseStream).ConfigureAwait(false);
    }
    /// <summary>
    /// The pagination params were defined in the response
    /// </summary>
    public bool IsPresent => MaxResults > 0 && Total > 0;
    public bool ShouldRequestNewPage
    {
        get
        {
            if (!IsPresent)
                return false;
            return EndsAt < Total;
        }
    }

    public void UpdateQuery(HttpQuery query)
    {
        if (ShouldRequestNewPage)
        {
            query.Add(HttpQuery.StartsAt(StartAt + MaxResults));
            query.Add(HttpQuery.MaxResults(MaxResults));
        }
    }
    public override string ToString()
    {
        return $"{StartAt} to {EndsAt} out of {Total}";
    }
    public static void RemoveFromQuery(HttpQuery query)
    {
        query.Remove("maxResults");
        query.Remove("startAt");
    }
}
