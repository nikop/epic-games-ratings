using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EpicRatingsUpdater
{
    public class GameDbItemEOSHistory
    {
        public DateTimeOffset Time { get; set; }

        public int NumProgressed { get; set; }

        public int NumCompleted { get; set; }
    }
}
