using System.Text.Json.Serialization;

namespace EpicRatingsUpdater.GameDatabase
{
    public class GameDbItem : JsonIndexDbItem
    {
        [JsonIgnore]
        public bool IsNew { get; set; }

        [JsonPropertyOrder(1)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ProductSlug { get; set; }

        [JsonPropertyOrder(2)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Ranking_Rating { get; set; }

        [JsonPropertyOrder(3)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Ranking_Popularity { get; set; }

        [JsonPropertyOrder(4)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Ranking_PopularitySum { get; set; }

        [JsonPropertyOrder(5)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Ranking_EOS_Progress { get; set; }

        [JsonPropertyOrder(6)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Ranking_EOS_Completed { get; set; }

        [JsonPropertyOrder(7)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Ranking_EOS_NewPlayers { get; set; }

        [JsonPropertyOrder(8)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Ranking_EOS_NewCompleters { get; set; }

        [JsonPropertyOrder(20)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Rating { get; set; }

        [JsonPropertyOrder(21)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumberOfRatings { get; set; }

        [JsonPropertyOrder(22)]
        public List<GameDbItemRatingHistory> RatingHistory { get; set; } = new();

        [JsonPropertyOrder(23)]
        public bool ReviewsDisabled { get; set; }

        [JsonPropertyOrder(30)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumberOfAwards { get; set; }

        [JsonPropertyOrder(31)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumberOfAwardsMax { get; set; }

        [JsonPropertyOrder(32)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MaxAwardTitle { get; set; }

        [JsonPropertyOrder(33)]
        public List<GameDbItemTag> Tags { get; set; } = new();

        [JsonPropertyOrder(100)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? FirstSeen { get; set; }

        [JsonPropertyOrder(101)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? FirstSeenRating { get; set; }

        [JsonPropertyOrder(102)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? FirstSeenAchievements { get; set; }

        [JsonPropertyOrder(103)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? LastChanged { get; set; }

        [JsonPropertyOrder(104)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? LastChanged_Achievements { get; set; }

        [JsonPropertyOrder(105)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? LastUpdate_AppInfo { get; set; }

        [JsonPropertyOrder(106)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? LastUpdate_Ratings { get; set; }

        [JsonPropertyOrder(107)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? LastUpdate_Achievements { get; set; }

        [JsonPropertyOrder(50)]
        public int EOS_Progressed { get; set; }

        [JsonPropertyOrder(51)]
        public int EOS_Completed { get; set; }

        [JsonPropertyOrder(52)]
        public int EOS_NewPlayers { get; set; }

        [JsonPropertyOrder(53)]
        public int EOS_NewCompleters { get; set; }

        [JsonPropertyOrder(54)]
        public double EOS_Completed_Percentage { get; set; }

        [JsonPropertyOrder(55)]
        public List<GameDbItemEOSHistory> EosHistory { get; set; } = new();

        [JsonPropertyOrder(80)]
        public int TotalAchievements { get; set; }

        [JsonPropertyOrder(81)]
        public int TotalAchievementsXP { get; set; }

        [JsonPropertyOrder(82)]
        public List<AchievementItem> Achievements { get; set; } = new();

        [JsonPropertyOrder(83)]
        public List<AchievementSet> AchievementSets { get; set; } = new();
    }
}
