using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;

using System.Text.Json;

namespace EpicRatingsUpdater.EGSApi
{
    public class EpicApi
    {
        private static GraphQLHttpClient graphQLClient = new GraphQLHttpClient("https://graphql.epicgames.com/graphql", new SystemTextJsonSerializer());
        public static HttpClient httpClient = new HttpClient();

        public static async Task<QueryCatalogResult.SearchStore> QueryCatalog(string locale, string country, int count, int start, string sortBy, string sortDir)
        {
            var request = new GraphQLRequest
            {
                Query = @"query catalogQuery($locale:String, $count:Int, $start:Int, $country: String!, $sortBy: String, $sortDir: String) {
  Catalog {
    searchStore (
      locale: $locale,
      count: $count
      start: $start
      country: $country
      sortBy: $sortBy
      sortDir: $sortDir
    ) {
      elements {
        title
        id
        namespace
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
        categories {
          path
        }
        catalogNs {
          mappings(pageType: ""productHome"") {
            pageSlug
            pageType
          }
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
      }
      paging {
        count
        start
        total
      }
    }
  }
}",
                Variables =
                new {
                    locale,
                    count,
                    start,
                    country,
                    sortBy,
                    sortDir,
                }
            };

            var r = await graphQLClient.SendQueryAsync<QueryCatalogResult>(request);

            return r.Data.Catalog.searchStore;
        }

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
                    offerId,
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

        public static async Task<AchievementData?> QueryAchievements(string ns)
        {
            var ratingRequest = new GraphQLRequest
            {
                Query = @"
query Achievement($sandboxId: String!, $locale: String!) {
  Achievement {
    productAchievementsRecordBySandbox(sandboxId: $sandboxId, locale: $locale) {
      productId
      sandboxId
      totalAchievements
      totalProductXP
      achievementSets {
        achievementSetId
        isBase
        numProgressed
        numCompleted
        totalAchievements
        totalXP
      }
      platinumRarity {
        percent
      }
      achievements {
        achievement {
          sandboxId
          deploymentId
          name
          hidden
          isBase
          achievementSetId
          unlockedDisplayName
          lockedDisplayName
          unlockedDescription
          lockedDescription
          unlockedIconId
          lockedIconId
          XP
          flavorText
          unlockedIconLink
          lockedIconLink
          tier {
            name
            hexColor
            min
            max
          }
          rarity {
            percent
          }
        }
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

            var r = await graphQLClient.SendQueryAsync<ProductAchievementsRecordBySandboxResponse>(ratingRequest);

            return r.Data?.Achievement?.productAchievementsRecordBySandbox;
        }

        public static async Task<ProductsPage?> GetProductPage(string slug)
        {
            try
            {
                var req = await httpClient.GetStringAsync($"https://store-content-ipv4.ak.epicgames.com/api/en-US/content/products/{slug}");
                var rr = JsonSerializer.Deserialize<ProductsPage>(req);

                return rr;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }
    }
}