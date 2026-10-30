using System.Text.Json.Serialization;

namespace Lira.Toggl
{
    internal class TogglDtoProject
    {
        public long Id { get; init; }
        [JsonPropertyName("client_id")]
        public long? ClientId { get; init; }
    }
}
