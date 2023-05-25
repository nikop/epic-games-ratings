using EpicRatingsUpdater.GameDatabase;

using System.Globalization;
using System.Text;

namespace EpicRatingsUpdater.Markdown
{
    internal static class MarkdownHelpers
    {
        static CultureInfo usCulture = CultureInfo.GetCultureInfo("en-US");

        public static string Escape(string str)
        {
            return str.Replace("|", @"\|");
        }

        public static string FormatRating(double? rating)
        {
            if (rating == null)
                return "-";

            return rating.Value.ToString("F2", usCulture);
        }

        public static string FormatRating(decimal? rating)
        {
            if (rating == null)
                return "-";

            return rating.Value.ToString("F2", usCulture);
        }

        public static string FormatRating1digit(decimal? rating)
        {
            if (rating == null)
                return "-";

            return rating.Value.ToString("F1", usCulture);
        }

        public static string FormatNumber(int? votes)
        {
            if (votes == null)
                return "-";

            return votes.Value.ToString("N0", usCulture);
        }

        public static string FormatRanking(int? ranking)
        {
            if (ranking == null)
                return "-";

            return ranking.Value.ToString("N0", usCulture);
        }

        public static string FormatPeriodVotes(double? votes)
        {
            if (votes == null)
                return "-";

            return votes.Value.ToString("F2", usCulture);
        }

        public static string FormatDate(DateTimeOffset? date)
        {
            if (date == null)
                return "-";

            return date.Value.ToString("yyyy-MM-dd");
        }

        static public void RankItems<TItem, TRanking>(
            IEnumerable<TItem> rankedItems,
            Func<TItem, TRanking> ratingRead,
            Action<TItem, int> rankWrite) where TRanking : IEquatable<TRanking>
        {
            var i = 1;
            var lastRanking = i;
            TRanking? lastRating = default;

            foreach (var item in rankedItems)
            {
                var rating = ratingRead(item);
                var displayRanking = rating.Equals(lastRating) ? lastRanking : i;

                rankWrite(item, displayRanking);

                lastRanking = displayRanking;
                lastRating = rating;
                i++;
            }
        }

        static public string BuildMarkdownStats(List<GameDbItem> filteredList, List<GameDbItem> allList)
        {
            var games = allList.Where(x => x.ProductSlug != null).ToList();
            var average = filteredList.Sum(x => x.Rating ?? 0) / filteredList.Count;

            var decRatings = new Dictionary<decimal, int>();

            foreach (var item in filteredList)
            {
                var decRating = Math.Round((decimal?)item.Rating ?? 0m, 1);

                if (!decRatings.ContainsKey(decRating))
                {
                    decRatings[decRating] = 0;
                }

                decRatings[decRating]++;
            }

            var sb = new StringBuilder();

            sb.AppendLine($"# Stats");

            sb.AppendLine($"Games: {FormatNumber(games.Count)}  ");
            sb.AppendLine($"Games with rating: {FormatNumber(filteredList.Count)}  ");
            sb.AppendLine($"Games without rating: {FormatNumber(games.Count - filteredList.Count)}  ");
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

                sb.AppendLine($"| {FormatRating1digit(i)} | {FormatNumber(count)} |");
            }

            return sb.ToString();
        }

        static public string BuildMarkdownGamePage(GameDbItem item, string rawLink)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"# {item.Name}");

            if (item.ProductSlug != null)
            {
                sb.AppendLine($"[Store](https://store.epicgames.com/en-US/p/{item.ProductSlug})  ");
            }

            sb.AppendLine($"[View Rawdata](../../{rawLink})  ");

            if (item.Rating != null)
            {
                sb.AppendLine($"Rating: {FormatRating(item.Rating)} (Ranked {FormatRanking(item.Ranking_Rating)})  ");
            }

            if (item.NumberOfRatings != null)
            {
                sb.AppendLine($"Number of Ratings: {FormatNumber(item.NumberOfRatings)}  (23.09.2022)  ");
            }

            if (item.Achievements.Count > 0)
            {
                sb.AppendLine("## Achievements");

                var achTable = new MarkdownTable<AchievementItem>();
                achTable.AddColumn("Name", x => x.Name);
                achTable.AddColumn("Percentage", x => FormatRating(x.Percentage) + "%");
                achTable.AddColumn("Users", x => "~" + FormatNumber(x.UsersEstimate));

                sb.Append(achTable.FormatTable(item.Achievements.OrderByDescending(x => x.Percentage)));
            }

            if (item.EOS_Progressed > 0)
            {
                sb.AppendLine("## Popularity (Based on EOS Achievements)");

                sb.AppendLine($"Progressed: {FormatNumber(item.EOS_Progressed)} (Ranked {FormatRanking(item.Ranking_EOS_Progress)})  ");
                sb.AppendLine($"Completed: {FormatNumber(item.EOS_Completed)} ({FormatRating(item.EOS_Completed_Percentage)}%) (Ranked {FormatRanking(item.Ranking_EOS_Completed)})  ");

                sb.AppendLine("## EOS Players History");

                var eosHistoryTable = new MarkdownTable<GameDbItemEOSHistory>();

                eosHistoryTable.AddColumn("Date", x => x.Time.ToString("yyyy-MM-dd"));
                eosHistoryTable.AddColumn("Progressed", x => FormatNumber(x.NumProgressed));
                eosHistoryTable.AddColumn("Completed", x => FormatNumber(x.NumCompleted));

                var itemsToShow = item.EosHistory.GroupBy(x => x.Time.Date).Select(x => new GameDbItemEOSHistory
                {
                    Time = x.Key,
                    NumProgressed = x.MaxBy(x => x.NumProgressed)?.NumProgressed ?? 0,
                    NumCompleted = x.MaxBy(x => x.NumCompleted)?.NumCompleted ?? 0,
                }).ToList();

                sb.Append(eosHistoryTable.FormatTable(itemsToShow));
            }

            if (item.Tags.Count > 0)
            {
                sb.AppendLine("## Awards");

                if (item.NumberOfAwards != null && item.NumberOfAwards > 0)
                {
                    sb.AppendLine($"Max ({item.MaxAwardTitle}): {FormatNumber(item.NumberOfAwardsMax)}  (Ranked {FormatRanking(item.Ranking_Popularity)})  ");
                    sb.AppendLine($"Sum: {FormatNumber(item.NumberOfAwards)} (Ranked {FormatRanking(item.Ranking_PopularitySum)})  ");
                    sb.AppendLine($"Diff (max vs sum): {FormatRanking(item.Ranking_Popularity - item.Ranking_PopularitySum)}  ");
                }

                sb.AppendLine("| Award | Count |");
                sb.AppendLine("| ----- | ----- |");

                foreach (var tag in item.Tags.OrderByDescending(x => x.Count))
                {
                    sb.AppendLine($"| {tag.Text} | {FormatNumber(tag.Count)} |");
                }
            }

            if (item.RatingHistory.Count > 0)
            {
                sb.AppendLine("## Ratings History");

                var ratingsTable = new MarkdownTable<GameDbItemRatingHistory>();

                ratingsTable.AddColumn("Date", x => x.Time.ToString("yyyy-MM-dd"));
                ratingsTable.AddColumn("Rating", x => FormatRating(x.Rating));

                if (item.RatingHistory.Any(x => x.NumberOfRatings != null))
                {
                    ratingsTable.AddColumn("Number of Ratings", x => FormatNumber(x?.NumberOfRatings));
                }

                ratingsTable.AddColumn("Number of Awards (Max)", x => FormatNumber(x?.NumberOfAwardsMax));
                ratingsTable.AddColumn("Number of Awards (Sum)", x => FormatNumber(x?.NumberOfAwards));

                var itemsToShow = item.RatingHistory.GroupBy(x => x.Time.Date).Select(x => new GameDbItemRatingHistory
                {
                    Time = x.Key,
                    NumberOfAwards = x.MaxBy(x => x.NumberOfAwards)?.NumberOfAwards,
                    NumberOfAwardsMax = x.MaxBy(x => x.NumberOfAwardsMax)?.NumberOfAwardsMax,
                    NumberOfRatings = x.MaxBy(x => x.NumberOfRatings)?.NumberOfRatings,
                    Rating = x.MaxBy(x => x.Rating)?.Rating,
                }).ToList();

                sb.Append(ratingsTable.FormatTable(itemsToShow));
            }

            return sb.ToString();
        }
    }
}
