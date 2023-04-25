// See https://aka.ms/new-console-template for more information
internal class RatingsResponse
{
    public RatingsPolls? RatingsPolls { get; set; }
}

public class RatingsPolls
{
    public getProductResult? getProductResult { get; set; }
}

public class getProductResult
{
    public double? averageRating { get; set; }

    //public int? ratingCount { get; set; }

    public List<pollResult> pollResult { get; set; } = new();
}

public class pollResult
{
    public pollresult_localizations localizations { get; set; }

    public int? total { get; set; }
}