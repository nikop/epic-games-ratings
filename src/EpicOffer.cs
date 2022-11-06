internal class EpicOffer
{
    public string id { get; set; } = "";

    public string title { get; set; } = "";

    public string entitlementType { get; set; } = "";

    public string? productSlug { get; set; }

    public List<EpicOfferCategory> categories { get; set; } = new();
}

public class EpicOfferCategory
{
    public string? path { get; set; }
}