namespace EpicRatingsUpdater.EGSApi
{
    public class CatalogItemPrice
    {
        /*price(country: $country) {
          totalPrice {
            discountPrice
            originalPrice
            voucherDiscount
            discount
            currencyCode
            currencyInfo {
              decimals
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
        }*/

        public class FormattedPrice
        {
            public string? originalPrice { get; set; }

            public string? discountPrice { get; set; }

            public string? intermediatePrice { get; set; }
        }

        public FormattedPrice fmtPrice { get; set; } = new FormattedPrice();
    }
}