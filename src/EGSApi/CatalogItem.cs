using System.Text.Json.Serialization;

namespace EpicRatingsUpdater.EGSApi
{
    public class CatalogItem
    {
        public string? title { get; set; }

        public string? id { get; set; }

        [JsonPropertyName("namespace")]
        public string? ns { get; set; }

        public EpicCatalogNs catalogNs { get; set; } = new();

        public List<EpicOfferCategory> categories { get; set; } = new();

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
        customAttributes {
          key
          value
        }
        offerMappings {
          pageSlug
          pageType
        }
        developerDisplayName
        publisherDisplayName
        price(country: $country) {
          totalPrice {
            discountPrice
            originalPrice
            voucherDiscount
            discount
            currencyCode
            currencyInfo {
              decimals
            }
            fmtPrice(locale: $locale) {
              originalPrice
              discountPrice
              intermediatePrice
            }
          }
          lineOffers {
            appliedRules {
              id
              endDate
              discountSetting {
                discountType
              }
            }
          }
        }
        prePurchase
        releaseDate
        pcReleaseDate
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