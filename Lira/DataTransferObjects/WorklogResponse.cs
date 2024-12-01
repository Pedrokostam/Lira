using System.Collections.Generic;
using System.Text.Json.Serialization;
using Lira.Objects;

namespace Lira.DataTransferObjects;

public class WorklogResponse
{
    [JsonPropertyName("maxResults")]
    public long MaxResults { get; set; }
    [JsonPropertyName("total")]
    public long Total { get; set; }
    [JsonPropertyName("worklogs")]
    public IList<Worklog> InitialWorklogs { get; set; }

    public WorklogResponse()
    {
        InitialWorklogs = [];
    }
    public bool HasAllWorklogs => MaxResults >= Total && MaxResults > 0;
}
