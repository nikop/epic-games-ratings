using System.Text.Json.Serialization;

namespace EpicRatingsUpdater.EGSApi
{
    public class CatalogItemAttrib
    {
        public string key { get; set; } = null!;

        public string value {  get; set; } = null!;
    }

    public class CatalogItem
    {
        public string? title { get; set; }

        public string? id { get; set; }

        [JsonPropertyName("namespace")]
        public string? ns { get; set; }

        public EpicCatalogNs catalogNs { get; set; } = new();

        public List<EpicOfferCategory> categories { get; set; } = new();

        public DateTimeOffset? lastModifiedDate { get; set; }

        public DateTimeOffset? releaseDate { get; set; }

        public DateTimeOffset? pcReleaseDate { get; set; }

        public CatalogItemPrice price { get; set; } = new CatalogItemPrice();

        public string? developerDisplayName { get; set; }

        public string? publisherDisplayName { get; set; }

        public List<CatalogItemAttrib> customAttributes { get; set; } = new();

        /*elements {
        description
        effectiveDate
        isCodeRedemptionOnly
        keyImages {
          type
          url
        }
        currentPrice
        seller {
          id
          name
        }
        productSlug
        urlSlug
        url
        tags {
          id
        }
        items {
          id
          namespace
        }
        offerMappings {
          pageSlug
          pageType
        }
        prePurchase
        viewableDate
        approximateReleasePlan {
          day
          month
          quarter
          year
          releaseDateType
        }
      }*/
    }
}