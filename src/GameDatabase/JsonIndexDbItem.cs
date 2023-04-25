using System.Text.Json.Serialization;

namespace EpicRatingsUpdater.GameDatabase
{
    public abstract class JsonIndexDbItem
    {
        [JsonPropertyOrder(-20)]
        public string ID { get; set; } = string.Empty;

        [JsonPropertyOrder(-19)]
        public string? Name { get; set; }
    }
}
