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

        static public string BuildMarkdownGamePage(GameDbItem item)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"# {item.Name}");

            sb.AppendLine($"Rating: {FormatRating(item.Rating)} ({FormatVotes(item.NumberOfRatings)})  (as of 23.09.2022)  ");
            //sb.AppendLine($"Ratings Per Day: {FormatPeriodVotes(item.DailyRatings)}  ");

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

            sb.AppendLine("|  #  | Name | Rating | ");
            sb.AppendLine("| --- | ---- | ------ | ");

            var i = 1;
            var lastRanking = i;
            double? lastRating = null;

            foreach (var item in items)
            {
                var link = "games/" + gameIndex.Files[item.ID].Replace(@"\", "/") + ".md";

                var displayRanking = !groupByRating ? i : (item.Rating == lastRating ? lastRanking : i);

                sb.AppendLine($"| {displayRanking} | [{item.Name}]({link}) | {FormatRating(item.Rating)} | ");

                lastRanking = displayRanking;
                lastRating = item.Rating;

                i++;
            }

            return sb.ToString();
        }
    }
}
