using EpicRatingsUpdater.EGSApi;
using EpicRatingsUpdater.GameDatabase;

using GraphQL.Client.Http;

namespace EpicRatingsUpdater
{
    internal class UpdateAppInfo
    {
        internal UpdateAppInfo()
        {
        }

        public async Task Run(List<GameDbItem> items)
        {
            Console.WriteLine("Updating AppInfo");

            var cutOff = DateTimeOffset.UtcNow.AddDays(-7);

            var orderedItems = items.Where(x => x.LastUpdate_AppInfo == null || x.LastUpdate_AppInfo < cutOff).OrderBy(x => x.LastUpdate_Ratings ?? DateTimeOffset.MinValue).ToList();

            var c = 0;
            var total = orderedItems.Count;

            var task = Parallel.ForEachAsync(orderedItems, new ParallelOptions { MaxDegreeOfParallelism = 2 },
                async (item, ct) =>
                {
                    try
                    {
                        await UpdateItem(item);
                        Interlocked.Increment(ref c);
                    }
                    catch (GraphQLHttpRequestException)
                    {
                        Console.WriteLine("Ratelimited...");
                        await Task.Delay(10000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex}");
                    }

                    await Task.Delay(100);
                }
            );

            while (!task.IsCompleted)
            {
                Console.WriteLine($"{c} / {total}");
                await Task.WhenAny(task, Task.Delay(5000));
            }
        }

        public async Task UpdateItem(GameDbItem item)
        {
            if (item.ProductSlug == null)
            {
                var res = await EpicApi.GetCatalogNamespace(item.ID).ConfigureAwait(false);

                if (res != null)
                {
                    if (res.mappings != null)
                    {
                        var mapping = res.mappings.FirstOrDefault(x => x.pageType == "productHome");

                        if (mapping != null)
                        {
                            item.ProductSlug = mapping.pageSlug;
                        }
                    }
                }
            }

            item.LastUpdate_AppInfo = DateTimeOffset.UtcNow;
        }
    }
}
