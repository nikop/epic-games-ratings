using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace EpicRatingsUpdater.GameDatabase
{
    public class GlobalDbAttrib
    {
        public Dictionary<string, string> KnownValues { get; set; } = new();
    }

    public class GlobalDb
    {
        public DateTimeOffset CatalogLastModifiedDate { get; set; } = DateTimeOffset.MinValue;

        [JsonPropertyName("KnownCustomAttributes_v2")]
        public Dictionary<string, GlobalDbAttrib> KnownCustomAttributes { get; set; } = new();
    }
}
