using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;

using System.Text.Json;

namespace EpicRatingsUpdater
{
    public class EpicApi
    {
        private static GraphQLHttpClient graphQLClient = new GraphQLHttpClient("https://graphql.epicgames.com/graphql", new SystemTextJsonSerializer());
        public static HttpClient httpClient = new HttpClient();

        public static async Task<EpicCatalogOffer?> GetCatalogOffer(string ns, string offerId)
        {
            var ratingRequest = new GraphQLRequest
            {
                Query = @"
query getCatalogOffer($sandboxId: String!, $offerId: String!, $locale: String, $country: String!) {
  Catalog {
    catalogOffer(namespace: $sandboxId, id: $offerId, locale: $locale) {
      title
      id
      namespace
      countriesBlacklist
      countriesWhitelist
      developerDisplayName
      description
      effectiveDate
      expiryDate
      allowPurchaseForPartialOwned
      offerType
      externalLinks {
        text
        url
      }
      isCodeRedemptionOnly
      keyImages {
        type
        url
      }
      longDescription
      seller {
        id
        name
      }
      productSlug
      publisherDisplayName
      releaseDate
      urlSlug
      url
      tags {
        id
        name
        groupName
      }
      items {
        id
        namespace
        releaseInfo {
          appId
          platform
        }
      }
      customAttributes {
        key
        value
      }
      categories {
        path
      }
      catalogNs {
        ageGatings {
          ageControl
          descriptor
          elements
          gameRating
          ratingImage
          ratingSystem
          title
        }
        displayName
        mappings {
          createdDate
          deletedDate
          mappings {
            cmsSlug
            offerId
            prePurchaseOfferId
          }
          pageSlug
          pageType
          productId
          sandboxId
          updatedDate
        }
        store
      }
      offerMappings {
        createdDate
        deletedDate
        mappings {
          cmsSlug
        }
        pageSlug
        pageType
        productId
        sandboxId
        updatedDate
      }
      pcReleaseDate
      prePurchase
      approximateReleasePlan {
        day
        month
        quarter
        year
        releaseDateType
      }
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
      allDependNsOfferIds
      majorNsOffers {
        categories {
          path
        }
        id
        namespace
        title
      }
      subNsOffers {
        categories {
          path
        }
        id
        namespace
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
        title
      }
      status
    }
  }
}",
                Variables = new
                {
                    sandboxId = ns,
                    offerId = offerId,
                    locale = "en-US",
                    country = "FI",
                }
            };

            var r = await graphQLClient.SendQueryAsync<GetCatalogOfferResult>(ratingRequest);

            return r.Data.Catalog?.catalogOffer;
        }

        public static async Task<EpicCatalogNs?> GetCatalogNamespace(string ns)
        {
            var ratingRequest = new GraphQLRequest
            {
                Query = @"
query getCatalogNamespace($sandboxId: String!) {
  Catalog {
    catalogNs(namespace: $sandboxId) {
      ageGated
      ageGatings {
        ageControl
        descriptor
        element
        gameRating
        ratingImage
        ratingSystem
        title
      }
      displayName
      mappings {
        createdDate
        deletedDate
        mappings {
          cmsSlug
          offerId
          prePurchaseOfferId
        }
        pageSlug
        pageType
        productId
        sandboxId
        updatedDate
      }
      store
    }
  }
}",
                Variables = new
                {
                    sandboxId = ns,
                }
            };

            var r = await graphQLClient.SendQueryAsync<GetCatalogNamespaceResult>(ratingRequest);

            return r.Data.Catalog?.catalogNs;
        }

        public static async Task<getProductResult?> GetProductResult(string ns)
        {
            var ratingRequest = new GraphQLRequest
            {
                Query = @"
query getProductResult($sandboxId: String!, $locale: String!) {
  RatingsPolls {
    getProductResult(sandboxId: $sandboxId, locale: $locale) {
      averageRating
      pollResult {
        id
        pollDefinitionId
        localizations {
          text
          emoji
          resultEmoji
          resultTitle
          resultText
        }
        total
      }
    }
  }
}",
                Variables = new
                {
                    sandboxId = ns,
                    locale = "en-US",
                }
            };

            var r = await graphQLClient.SendQueryAsync<RatingsResponse>(ratingRequest);

            return r.Data.RatingsPolls?.getProductResult;
        }

        public static async Task<string?> ResolveNameForNamespace(string ns)
        {
            string? name = null;

            try
            {
                var res = await EpicApi.GetCatalogNamespace(ns).ConfigureAwait(false);

                if (res != null)
                {
                    name = res.displayName?.Trim();

                    if (res.mappings != null)
                    {
                        var mapping = res.mappings.FirstOrDefault(x => x.pageType == "productHome");
                        var offerId = mapping?.mappings?.offerId;
                        var found = false;

                        // Name from store content
                        if (!found && mapping?.pageSlug != null)
                        {
                            try
                            {
                                var req = await EpicApi.httpClient.GetStringAsync($"https://store-content-ipv4.ak.epicgames.com/api/en-US/content/products/{mapping.pageSlug}");

                                var rr = JsonSerializer.Deserialize<ProductsPage>(req);

                                if (rr != null)
                                {
                                    if (rr.productName != null)
                                    {
                                        name = rr.productName.Trim();
                                        found = true;
                                    }

                                    if (!found)
                                    {
                                        foreach (var p in rr.pages)
                                        {
                                            if (p.offer != null && p._title == "home")
                                            {
                                                offerId = p.offer.id;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }

                        // Name by offer
                        if (!found && offerId != null)
                        {
                            try
                            {
                                var catalogOffer = await EpicApi.GetCatalogOffer(ns, offerId).ConfigureAwait(false);

                                if (catalogOffer != null)
                                {
                                    if (catalogOffer.Title != null)
                                    {
                                        name = catalogOffer.Title;
                                        found = true;
                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }

            }
            catch (Exception)
            {
            }

            return name;
        }
    }
}