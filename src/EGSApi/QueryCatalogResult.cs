namespace EpicRatingsUpdater.EGSApi
{
    public class QueryCatalogResult
    {
        public class Paging
        {
            public int count { get; set; }

            public int start { get; set; }

            public int total { get; set; }
        }

        public class SearchStore
        {
            public List<CatalogItem> elements { get; set; } = new();

            public Paging paging { get; set; } = new();
        }

        public class QueryCatalogResultInner
        {
            public SearchStore searchStore { get; set; } = new();
        }

        public QueryCatalogResultInner Catalog { get; set; } = new();
    }
}