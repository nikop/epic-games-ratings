using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicRatingsUpdater.GameDatabase
{
    public class AchievementSet
    {
        public string ID { get; set; } = "";

        public bool IsBase { get; set; }

        public int Progressed { get; set; }

        public int Completed { get; set; }
    }
}
