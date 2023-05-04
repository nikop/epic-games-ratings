using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicRatingsUpdater.GameDatabase
{
    public class AchievementItem
    {
        public string ID { get; set; } = "";

        public string SetID { get; set; } = "";

        public string Name { get; set; } = "";

        public double Percentage { get; set; }

        public int XP { get; set; }

        public int UsersEstimate { get; set; }
    }
}
