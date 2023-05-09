using EpicRatingsUpdater.EGSApi;
using EpicRatingsUpdater.GameDatabase;

namespace EpicRatingsUpdater
{
    internal class UpdateCatalog
    {
        List<string> IgnoredAttributes = new List<string>
        {
            "epicgames.app.productSlug",
            "com.epicgames.app.urlSlug",
            "com.epicgames.app.productSlug",
            "com.epicgames.app.productSlu",
            "publisherName",
            "developerName",
            "availableDate",
            "extraLaunchOption_001_Args",
            "extraLaunchOption_001_Name",
        };

        List<string> NonStoreAttributes = new List<string>
        {
            "isBlockchainUsed",
        };

        public bool IsIgnoredAttribute(string attrib)
        {
            foreach (var attr in IgnoredAttributes)
            {
                if (attr.Equals(attrib, StringComparison.InvariantCultureIgnoreCase)) {
                    return true;
                }
            }

            return false;
        }

        public bool IsNonStored(string attrib)
        {
            foreach (var attr in NonStoreAttributes)
            {
                if (attr.Equals(attrib, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task Run(GlobalDb globalDb, GameDb db)
        {
            var i = 0;
            var total = 1;
            var pageSize = 1000;

            Console.WriteLine("::group::Catalog Update");

            var catalogDate = globalDb.CatalogLastModifiedDate;

            foreach (var key in globalDb.KnownCustomAttributes.Keys.ToList())
            {
                if (IsIgnoredAttribute(key))
                {
                    globalDb.KnownCustomAttributes.Remove(key);
                }
            }

            while (i < total)
            {
                Console.WriteLine($"Catalog Update: {i} / {total}");
                var page = await EpicApi.QueryCatalog("en", "US", pageSize, i, "lastModifiedDate", "DESC");

                var endReached = false;

                total = page.paging.total;
                i += pageSize;

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

                    if (catalogDate > el.lastModifiedDate)
                    {
                        // Reached end of updates
                        endReached = true;
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

                        item.Store.CustomAttributes.Clear();

                        foreach (var attr in el.customAttributes)
                        {
                            if (IsIgnoredAttribute(attr.key))
                            {
                                continue;
                            }

                            if (!IsNonStored(attr.key))
                            {
                                item.Store.CustomAttributes[attr.key] = attr.value;
                            }

                            // Tracking for new attributes
                            if (!globalDb.KnownCustomAttributes.ContainsKey(attr.key))
                            {
                                globalDb.KnownCustomAttributes[attr.key] = new();
                            }

                            if (!globalDb.KnownCustomAttributes[attr.key].KnownValues.ContainsKey(attr.value))
                            {
                                globalDb.KnownCustomAttributes[attr.key].KnownValues[attr.value] = el.ns;
                                Console.WriteLine($"::notice::New Atrribute '{attr.key}' (with value '{attr.value}') in {el.ns} / {el.title}");
                            }
                        }
                    }
                }

                if (endReached)
                {
                    // break;
                }
            }

            Console.WriteLine("::endgroup::");
        }
    }
}
