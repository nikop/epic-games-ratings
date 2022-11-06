internal class ProductsPage
{
    public string? productName { get; set; }

    public string? _title { get; set; }

    public List<ProductsPageSubPage> pages { get; set; } = new();
}

public class ProductsPageSubPage
{
    public string _templateName { get; set; } = "";

    public string? _title { get; set; }

    public ProductsPageSubPageOffer? offer { get; set; } 
}

public class ProductsPageSubPageOffer
{
    public string? id { get; set; }

    public bool hasOffer { get; set; }
}