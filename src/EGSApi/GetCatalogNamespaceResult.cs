internal class GetCatalogNamespaceResult
{
    public EpicCatalog? Catalog { get; set; }
}

public class EpicCatalog
{
    public EpicCatalogNs? catalogNs { get; set; }
}

public class EpicCatalogNs
{
    public string? displayName { get; set; }

    public List<EpicCatalogMapping>? mappings { get; set; }
}

public class EpicCatalogMapping
{
    public string? pageSlug { get; set; }

    public string? pageType { get; set; }

    public EpicCatalogMappingMapping? mappings { get; set; }
}

public class EpicCatalogMappingMapping
{
    public string? offerId { get; set; }
}