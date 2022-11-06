using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicRatingsUpdater
{
    public class GameDbItemRatingHistory
    {
        public DateTimeOffset Time { get; set; }

        public double? Rating { get; set; }

        public int? NumberOfRatings { get; set; }
    }
}
