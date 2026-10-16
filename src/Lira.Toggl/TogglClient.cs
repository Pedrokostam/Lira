using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lira.Toggl
{
    public class TogglDateConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDateTimeOffset();

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }
    public class TogglTimeEntry {
        [JsonPropertyName("start")]
        [JsonConverter(typeof(TogglDateConverter))]
        public DateTimeOffset Start { get; set; }
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("project_name")]
        public string? ProjectName { get; set; }
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = [];
    }
    public class TogglClient
    {
        private string ApiKey { get; }
        private readonly HttpClient _client;

        public TogglClient(string apiKey)
        {
            ApiKey = apiKey;
            _client = new HttpClient() { BaseAddress= new Uri("https://api.track.toggl.com/api/v9/")};
            string token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:api_token"));
            _client.DefaultRequestHeaders.Authorization = new("Basic",token);
        }

        public async Task<List<TogglTimeEntry>> GetEntries(DateTimeOffset from, DateTimeOffset to) {
            // toggl has exlusive end dates
            string uri = $"me/time_entries?meta=true&start_date={from:yyyy-MM-dd}&end_date={to.AddDays(1):yyyy-MM-dd}";
            var payload = await _client.GetAsync(uri).ConfigureAwait(false);
            if (!(payload?.IsSuccessStatusCode ?? false)) {
                throw new Exception("todo");
            }
            var content =await payload.Content.ReadAsStringAsync().ConfigureAwait(false);
            var entries = JsonHelper.Deserialize<List<TogglTimeEntry>>(content) ?? [];
            return entries;
        }
    }
}

