using System.Globalization;
using System.Text;

namespace EpicRatingsUpdater
{
    internal static class Markdown
    {
        static CultureInfo usCulture = CultureInfo.GetCultureInfo("en-US");

        static string FormatRating(double? rating)
        {
            if (rating == null)
                return "-";

            return rating.Value.ToString("F2", usCulture);
        }

        static string FormatRating(decimal? rating)
        {
            if (rating == null)
                return "-";

            return rating.Value.ToString("F2", usCulture);
        }

        static string FormatRating1digit(decimal? rating)
        {
            if (rating == null)
                return "-";

            return rating.Value.ToString("F1", usCulture);
        }

        static string FormatVotes(int? votes)
        {
            if (votes == null)
                return "-";

            return votes.Value.ToString("N0");
        }

        static string FormatPeriodVotes(double? votes)
        {
            if (votes == null)
                return "-";

            return votes.Value.ToString("F2", usCulture);
        }

        static public string BuildMarkdownStats(List<GameDbItem> filteredList, List<GameDbItem> allList)
        {
            var average = filteredList.Sum(x => x.Rating ?? 0) / filteredList.Count;

            var decRatings = new Dictionary<decimal, int>();

            foreach (var item in filteredList)
            {
                var decRating = Math.Round((decimal?) item.Rating ?? 0m, 1);

                if (!decRatings.ContainsKey(decRating))
                {
                    decRatings[decRating] = 0;
                }

                decRatings[decRating]++;
            }

            var sb = new StringBuilder();

            sb.AppendLine($"# Stats");

            sb.AppendLine($"Games with rating: {FormatVotes(filteredList.Count)}  ");
            sb.AppendLine($"Games without rating: {FormatVotes(allList.Count - filteredList.Count)}  ");
            sb.AppendLine($"Average rating: {FormatRating(average)}  ");

            var minRating = decRatings.Keys.Min();

            sb.AppendLine($"## Ratings ");

            sb.AppendLine("| Rating | Number of Games |");
            sb.AppendLine("| ----  | --------------- |");

            for (var i = 5.0m; i >= minRating; i -= 0.1m)
            {
                if (!decRatings.TryGetValue(i, out var count))
                {
                    count = 0;
                }

                sb.AppendLine($"| {FormatRating1digit(i)} | {FormatVotes(count)} |");
            }

            return sb.ToString();
        }

        static public string BuildMarkdownGamePage(GameDbItem item)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"# {item.Name}");

            sb.AppendLine($"Rating: {FormatRating(item.Rating)} ({FormatVotes(item.NumberOfRatings)})  (as of 23.09.2022)  ");

            sb.AppendLine("## Awards");

            sb.AppendLine("| Award | Number of Ratings |");
            sb.AppendLine("| ----- | ----------------- |");

            foreach (var tag in item.Tags.OrderByDescending(x => x.Count))
            {
                sb.AppendLine($"| {tag.Text} | {FormatVotes(tag.Count)} |");
            }

            sb.AppendLine("## Ratings History");

            sb.AppendLine("| Date | Rating | Number of Ratings |");
            sb.AppendLine("| ---- | ------ | ----------------- |");

            foreach (var h in item.RatingHistory.GroupBy(x => x.Time.Date))
            {
                var sub = h.MaxBy(x => x.NumberOfRatings)!;

                sb.AppendLine($"| {sub.Time.ToString("yyyy-MM-dd")} | {FormatRating(sub.Rating)} | {FormatVotes(sub.NumberOfRatings)} |");
            }

            return sb.ToString();
        }

        static public string BuildMarkdownTable(JsonIndexDb<GameDbItem> gameIndex, IEnumerable<GameDbItem> items, bool groupByRating = false)
        {
            var sb = new StringBuilder();

            sb.AppendLine("|  #  | Name | Rating | Number of Awards | ");
            sb.AppendLine("| --- | ---- | ------ | ---------------- | ");

            var i = 1;
            var lastRanking = i;
            double? lastRating = null;

            foreach (var item in items)
            {
                var link = "games/" + gameIndex.Files[item.ID].Replace(@"\", "/") + ".md";

                var displayRanking = !groupByRating ? i : (item.Rating == lastRating ? lastRanking : i);

                sb.AppendLine($"| {displayRanking} | [{item.Name}]({link}) | {FormatRating(item.Rating)} | {FormatVotes(item.NumberOfAwards)} |");

                lastRanking = displayRanking;
                lastRating = item.Rating;

                i++;
            }

            return sb.ToString();
        }
    }
}
