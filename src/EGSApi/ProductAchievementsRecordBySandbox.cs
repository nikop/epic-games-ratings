using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public class AchievementSet
    {
        public string achievementSetId { get; set; }

        public bool isBase { get; set; }

        public int numProgressed { get; set; }

        public int numCompleted { get; set; }
    }

    public class AchievementData
    {
        public string? productId { get; set; }
        public string? sandboxId { get; set; }

        public List<AchievementSet>? achievementSets { get; set; }
    }
}
