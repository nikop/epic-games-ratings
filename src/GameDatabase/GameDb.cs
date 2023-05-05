using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicRatingsUpdater.GameDatabase
{
    internal class GameDb : JsonIndexDb<GameDbItem>
    {
        public GameDb(string path) : base(path)
        {
        }

        public async ValueTask<GameDbItem> GetOrCreate(string ns, string? name = null)
        {
            var item = await GetItemByKey(ns).ConfigureAwait(false);

            if (item == null)
            {
                item = new GameDbItem
                {
                    ID = ns,
                    Name = name,
                    IsNew = true,
                };

                await SaveItem(item).ConfigureAwait(false);
            }

            return item;
        }
    }
}
