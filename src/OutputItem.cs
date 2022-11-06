public class OutputItem
{
    public string Namespace { get; set; } = "";

    public string? ProductSlug { get; set; }

    public string Name { get; set; } = "";

    public double? Rating { get; set; }

    public int? NumberOfRatings { get; set; }

    public List<OutputItemTag> Tags { get; set; } = new();
}

public class OutputItemTag
{
    public string Text { get; set; } = "";

    public int Count { get; set; }
}