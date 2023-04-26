using EpicRatingsUpdater.EGSApi;
using EpicRatingsUpdater.GameDatabase;

using GraphQL.Client.Http;

namespace EpicRatingsUpdater
{
    internal class UpdateAchievements
    {
        internal UpdateAchievements()
        {
        }

        public async Task Run(List<GameDbItem> items)
        {
            Console.WriteLine("Updating Achievements");

            var orderedItems = items.OrderBy(x => x.LastUpdate_Achievements ?? DateTimeOffset.MinValue).ToList();

            await Parallel.ForEachAsync(orderedItems, new ParallelOptions { MaxDegreeOfParallelism = 2 },
                async (item, ct) =>
                {
                    try
                    {
                        await UpdateItem(item);
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
        }

        public async Task UpdateItem(GameDbItem item)
        {
            var ach = await EpicApi.QueryAchievements(item.ID);

            if (ach?.productId != null)
            {
                var baseSet = ach?.achievementSets?.FirstOrDefault(x => x.isBase);

                if (baseSet != null)
                {
                    var isChanged = item.EOS_Progressed != baseSet.numProgressed || item.EOS_Completed != baseSet.numCompleted;

                    item.LastChanged_Achievements = DateTimeOffset.UtcNow;
                    item.EOS_Progressed = baseSet.numProgressed;
                    item.EOS_Completed = baseSet.numCompleted;
                    item.EOS_Completed_Percentage = baseSet.numProgressed > 0 ? Math.Round((double)baseSet.numCompleted / baseSet.numProgressed * 100, 2) : 0;

                    if (isChanged)
                    {
                        item.EosHistory.Add(new GameDbItemEOSHistory
                        {
                            Time = DateTimeOffset.UtcNow,
                            NumProgressed = baseSet.numProgressed,
                            NumCompleted = baseSet.numCompleted,
                        });
                    }

                    if (item.FirstSeenAchievements == null)
                    {
                        item.FirstSeenAchievements = DateTimeOffset.UtcNow;
                    }
                }
            }

            item.LastUpdate_Achievements = DateTimeOffset.UtcNow;
        }
    }
}
