using EpicRatingsUpdater.EGSApi;
using EpicRatingsUpdater.GameDatabase;

namespace EpicRatingsUpdater
{
    internal class UpdateCatalog
    {
        public async Task Run(GlobalDb globalDb, GameDb db)
        {
            var i = 0;
            var total = 1;
            var pageSize = 1000;

            Console.WriteLine("::group::Catalog Update");

            var catalogDate = globalDb.CatalogLastModifiedDate;

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

                    if (el.lastModifiedDate != null && el.lastModifiedDate > globalDb.CatalogLastModifiedDate)
                    {
                        globalDb.CatalogLastModifiedDate = el.lastModifiedDate.Value;
                    }
         
                    // Mappings (storepage link)
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

                        item.Store.ReleaseDate = el.releaseDate;
                        item.Store.PcReleaseDate = el.pcReleaseDate;

                        var blockChain = el.customAttributes.FirstOrDefault(x => x.key == "isBlockchainUsed");
                        item.Store.isBlockchainUsed = blockChain?.value == "true";

                        foreach (var attr in el.customAttributes)
                        {
                            if (attr.value == "false")
                            {
                                continue;
                            }

                            if (!globalDb.KnownCustomAttributes.ContainsKey(attr.key))
                            {
                                globalDb.KnownCustomAttributes[attr.key] = new();
                            }

                            if (!globalDb.KnownCustomAttributes[attr.key].KnownValues.ContainsKey(attr.value))
                            {
                                globalDb.KnownCustomAttributes[attr.key].KnownValues[attr.key] = el.ns;
                                Console.WriteLine($"::notice::New Atrribute '{attr.key}' (with value '{attr.value}') in {el.ns} / {el.title}");
                            }
                        }
                    }
                }

                total = page.paging.total;
                i += pageSize;
            }

            Console.WriteLine("::endgroup::");
        }
    }
}
