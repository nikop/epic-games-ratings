using System.Collections.Concurrent;

namespace EpicRatingsUpdater.GameDatabase
{
    public class GlobalDb
    {
        public DateTimeOffset CatalogLastModifiedDate { get; set; } = DateTimeOffset.MinValue;

        public ConcurrentDictionary<string, string> KnownCustomAttributes { get; } = new ConcurrentDictionary<string, string>();
    }
}
