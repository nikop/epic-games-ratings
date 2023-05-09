using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicRatingsUpdater.GameDatabase
{
    public class EpicStoreInfo
    {
        public DateTimeOffset? ReleaseDate { get; set; }

        public DateTimeOffset? PcReleaseDate { get; set; }

        public bool isBlockchainUsed { get; set; }
    }
}
