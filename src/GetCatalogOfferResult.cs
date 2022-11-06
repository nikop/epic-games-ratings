namespace EpicRatingsUpdater
{
    public class GetCatalogOfferResult
    {
        public GetCatalogOfferResultCatalog? Catalog { get; set; }
    }

    public class GetCatalogOfferResultCatalog
    {
        public EpicCatalogOffer? catalogOffer { get; set; }
    }

    public class EpicCatalogOffer
    {
        public string Title { get; set; } = string.Empty;
    }
}