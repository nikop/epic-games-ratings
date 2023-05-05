using EpicRatingsUpdater.EGSApi;
using EpicRatingsUpdater.GameDatabase;

using GraphQL.Client.Http;

using System.Net.Http.Json;

namespace EpicRatingsUpdater
{
    internal class UpdateAppInfo
    {
        internal UpdateAppInfo()
        {
        }

        public async Task Run(List<GameDbItem> items)
        {
            Console.WriteLine("::group::Product Pages Update");

            var cutOff = DateTimeOffset.UtcNow.AddDays(-7);
            var forceUpdate = false;

#if DEBUG
            forceUpdate = true;
#endif

            var orderedItems = items
                .Where(x => x.ProductSlug != null)
                .Where(x => forceUpdate || x.LastUpdate_AppInfo == null || x.LastUpdate_AppInfo < cutOff)
                .OrderBy(x => x.LastUpdate_Ratings ?? DateTimeOffset.MinValue).ToList();

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

            Console.WriteLine("::endgroup::");
        }

        public async Task UpdateItem(GameDbItem item)
        {
            var page = await EpicApi.GetProductPage(item.ProductSlug!);

            if (page != null)
            {
                item.ReviewsDisabled = page.reviewOptOut;
            }

            item.LastUpdate_AppInfo = DateTimeOffset.UtcNow;
        }
    }
}
