namespace EpicRatingsUpdater.EGSApi
{
    public class ProductAchievementsRecordBySandboxResponse
    {
        public ProductAchievementsRecordBySandbox? Achievement { get; set; }
    }

    public class ProductAchievementsRecordBySandbox
    {
        public AchievementData? productAchievementsRecordBySandbox { get; set; }
    }

    public class AchievementSetEpic
    {
        public string achievementSetId { get; set; } = null!;

        public bool isBase { get; set; }

        public int numProgressed { get; set; }

        public int numCompleted { get; set; }

        public int totalAchievements { get; set; }

        public int totalXP { get; set; }
    }

    public class AchievementRarity
    {
        public double percent { get; set; }
    }

    public class AchievementOuter
    {
        public class AchievementInner
        {
            public string sandboxId { get; set; } = "";

            public string deploymentId { get; set; } = "";

            public string name { get; set; } = "";

            public string unlockedDisplayName { get; set; } = "";

            public string lockedDisplayName { get; set; } = "";

            public string unlockedDescription { get; set; } = "";

            public string lockedDescription { get; set; } = "";

            public int XP { get; set; }

            public bool isBase { get; set; }

            public string achievementSetId { get; set; } = null!;

            public AchievementRarity rarity { get; set; } = null!;
            
            /*
          unlockedIconId
          lockedIconId
          flavorText
          unlockedIconLink
          lockedIconLink
          tier {
            name
            hexColor
            min
            max
          }*/
    }

    public AchievementInner achievement { get; set; } = new();
    }

    public class AchievementData
    {
        public string? productId { get; set; }

        public string? sandboxId { get; set; }

        public int totalAchievements { get; set; }

        public int totalProductXP { get; set; }

        public AchievementRarity platinumRarity { get; set; } = new();

        public List<AchievementSetEpic> achievementSets { get; set; } = new();

        public List<AchievementOuter> achievements { get; set; } = new();
    }
}
