using System.Text.Json.Serialization;

namespace EpicRatingsUpdater
{
    public class GameDbItem : JsonIndexDbItem
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ProductSlug { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Rating { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumberOfRatings { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? FirstSeen { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? FirstSeenRating { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? LastChanged { get; set; }

        public List<GameDbItemRatingHistory> RatingHistory { get; set; } = new();

        public List<GameDbItemTag> Tags { get; set; } = new();
    }
}
