using EpicRatingsUpdater.EGSApi;
using EpicRatingsUpdater.GameDatabase;

using GraphQL.Client.Http;

namespace EpicRatingsUpdater
{
    internal class UpdateRatings
    {
        internal UpdateRatings()
        {
        }

        public async Task Run(List<GameDbItem> items)
        {
            Console.WriteLine("::group::Ratings Update");

            var orderedItems = items.OrderBy(x => x.LastUpdate_Ratings ?? DateTimeOffset.MinValue).ToList();

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
            var pi = await EpicApi.GetProductResult(item.ID).ConfigureAwait(false);

            if (pi != null)
            {
                if (item.FirstSeen == null)
                {
                    item.FirstSeen = DateTimeOffset.UtcNow;
                }

                var ratingChanged = item.Rating != pi.averageRating;

                //dbItem.ProductSlug = ns.ProductSlug;
                item.Rating = pi.averageRating;

                if (pi.pollResult != null)
                {
                    var topAward = pi.pollResult.MaxBy(x => x.total ?? 0);

                    var NumberOfAwards = pi.pollResult.Sum(x => x.total ?? 0);
                    var NumberOfAwardsMax = topAward?.total ?? 0;

                    if (NumberOfAwards != item.NumberOfAwards || NumberOfAwardsMax != item.NumberOfAwardsMax)
                    {
                        ratingChanged = true;
                    }

                    item.NumberOfAwards = NumberOfAwards;
                    item.NumberOfAwardsMax = NumberOfAwardsMax;
                    item.MaxAwardTitle = topAward?.localizations.resultTitle;

                    foreach (var pr in pi.pollResult.OrderByDescending(x => x.total))
                    {
                        var text = $"{pr.localizations.resultText} {pr.localizations.resultTitle}";
                        var current = item.Tags.FirstOrDefault(x => x.Text == text);

                        if (current != null)
                        {
                            current.Prefix = pr.localizations.resultText;
                            current.Type = pr.localizations.resultTitle;
                            current.Count = pr.total ?? 0;
                        }
                        else
                        {
                            item.Tags.Add(new GameDbItemTag
                            {
                                Text = text,
                                Prefix = pr.localizations.resultText,
                                Type = pr.localizations.resultTitle,
                                Count = pr.total ?? 0,
                            });
                        }
                    }
                }
                else if (item.NumberOfAwards != null)
                {
                    item.NumberOfAwards = 0;
                    item.NumberOfAwardsMax = 0;
                }

                if (ratingChanged)
                {
                    if (item.RatingHistory.Count == 0)
                    {
                        item.FirstSeenRating = DateTimeOffset.UtcNow;
                    }

                    item.LastChanged = DateTimeOffset.UtcNow;
                    item.RatingHistory.Add(new GameDbItemRatingHistory
                    {
                        Time = DateTimeOffset.UtcNow,
                        Rating = pi.averageRating,
                        NumberOfAwards = item.NumberOfAwards,
                        NumberOfAwardsMax = item.NumberOfAwardsMax,
                    });
                }
            }

            item.LastUpdate_Ratings = DateTimeOffset.UtcNow;
        }
    }
}
