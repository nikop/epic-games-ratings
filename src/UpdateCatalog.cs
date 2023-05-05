using EpicRatingsUpdater.EGSApi;
using EpicRatingsUpdater.GameDatabase;

namespace EpicRatingsUpdater
{
    internal class UpdateCatalog
    {
        public async Task Run(GameDb db)
        {
            var i = 0;
            var total = 1;
            var pageSize = 1000;

            Console.WriteLine("::group::Catalog Update");

            while (i < total)
            {
                Console.WriteLine($"Catalog Update: {i} / {total}");
                var page = await EpicApi.QueryCatalog("en", "US", pageSize, i, "lastModifiedDate", "DESC");

                foreach (var el in page.elements)
                {
                    if (el.ns == null)
                    {
                        continue;
                    }

                    var item = await db.GetOrCreate(el.ns);
                    var isBaseApp = el.categories.Any(x => x.path == "games/edition/base");

                    if (item.IsNew)
                    {
                        Console.WriteLine($"::notice::New Namespace {el.ns} / {el.title}");
                    }

                    if (el.catalogNs.mappings != null)
                    {
                        var mapping = el.catalogNs.mappings.FirstOrDefault(x => x.pageType == "productHome");

                        if (mapping != null)
                        {
                            item.ProductSlug = mapping.pageSlug;
                        }
                    }

                    if (isBaseApp)
                    {
                        item.Name = el.title;
                    }
                }

                total = page.paging.total;
                i += pageSize;
            }

            Console.WriteLine("::endgroup::");
        }
    }
}
