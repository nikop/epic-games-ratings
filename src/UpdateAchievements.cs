using EpicRatingsUpdater.EGSApi;
using EpicRatingsUpdater.GameDatabase;

using GraphQL.Client.Http;

namespace EpicRatingsUpdater
{
    internal class UpdateAchievements
    {
        DateTimeOffset newPlayersCutOff = DateTimeOffset.UtcNow.AddDays(-7);

        internal UpdateAchievements()
        {
        }

        public async Task Run(List<GameDbItem> items)
        {
            Console.WriteLine("Updating Achievements");

            var orderedItems = items.OrderBy(x => x.LastUpdate_Achievements ?? DateTimeOffset.MinValue).ToList();

            var cts = new CancellationTokenSource();

            var c = 0;
            var total = orderedItems.Count;

            var task = Parallel.ForEachAsync(orderedItems, new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = cts.Token },
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
                        await Task.Delay(10000, ct);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex}");
                    }

                    await Task.Delay(100, ct);
                }
            );

            while (!task.IsCompleted)
            {
                Console.WriteLine($"{c} / {total}");
                await Task.WhenAny(task, Task.Delay(5000));
            }
        }

        GameDbItemEOSHistory? GetComparisonPoint(IEnumerable<GameDbItemEOSHistory> items)
        {
            return items.OrderBy(x => x.Time.Subtract(newPlayersCutOff).Duration()).FirstOrDefault();
        }

        public async Task UpdateItem(GameDbItem item)
        {
            var data = await EpicApi.QueryAchievements(item.ID);

            if (data != null && data?.productId != null)
            {
                var baseSet = data.achievementSets.FirstOrDefault(x => x.isBase);

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

                    var recent = GetComparisonPoint(item.EosHistory);

                    if (recent != null)
                    {
                        item.EOS_NewPlayers = item.EOS_Progressed - recent.NumProgressed;
                        item.EOS_NewCompleters = item.EOS_Completed - recent.NumCompleted;
                    }
                    else
                    {
                        item.EOS_NewPlayers = 0;
                        item.EOS_NewCompleters = 0;
                    }

                    if (item.FirstSeenAchievements == null)
                    {
                        item.FirstSeenAchievements = DateTimeOffset.UtcNow;
                    }
                }

                // Achievement Totals
                item.TotalAchievements = data.totalAchievements ?? 0;
                item.TotalAchievementsXP = data.totalProductXP ?? 0;

                // Sets
                foreach (var set in data.achievementSets)
                {
                    var current = item.AchievementSets.FirstOrDefault(x => x.ID == set.achievementSetId);

                    if (current == null)
                    {
                        current = new AchievementSet
                        {
                            ID = set.achievementSetId,
                        };
                        item.AchievementSets.Add(current);
                    }

                    current.IsBase = set.isBase;
                    current.Progressed = set.numProgressed;
                    current.Completed = set.numCompleted;
                }

                // Achievements

                foreach (var kv in data.achievements)
                {
                    var ach = kv.achievement; // why?

                    if (ach == null)
                        continue;

                    var current = item.Achievements.FirstOrDefault(x => x.ID == ach.name);

                    if (current == null)
                    {
                        current = new AchievementItem
                        {
                            ID = ach.name,
                            SetID = ach.achievementSetId,
                        };
                        item.Achievements.Add(current);
                    }

                    var set = item.AchievementSets.FirstOrDefault(x => x.ID == ach.achievementSetId);

                    current.Name = ach.unlockedDisplayName;
                    current.Percentage = ach.rarity.percent;
                    current.XP = ach.XP;
                    current.UsersEstimate = (int) Math.Round((set?.Progressed ?? 0) * (ach.rarity.percent / 100), 0);
                }
            }

            item.LastUpdate_Achievements = DateTimeOffset.UtcNow;
        }
    }
}
