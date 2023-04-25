using EpicRatingsUpdater;
using EpicRatingsUpdater.Markdown;

using GraphQL.Client.Http;

using System.Text;
using System.Text.Json;

// Configs
var path = new DirectoryInfo(".").FullName;
var index = new Dictionary<string, string>();

var forceUpdateNames = args.Any(x => x == "--update-names");
var skipRatingsUpdate = args.Any(x => x == "--skip-update");
var skipOffers = args.Any(x => x == "--skip-offers");
var skipItems = args.Any(x => x == "--skip-items");

var ratingsCutOffNew = DateTimeOffset.UtcNow.AddDays(-30);
var dirDb = Path.Combine(path, "db");
var dirGames = Path.Combine(path, "games");
var dirItems = Path.Combine(path, "items-tracker", "database");
var dirOffers = Path.Combine(path, "offers-tracker", "database");

var gameIndex = new JsonIndexDb<GameDbItem>(dirDb);

if (!Directory.Exists(dirItems) || !Directory.Exists(dirOffers))
{
    Console.WriteLine("Databases missing");
    Environment.Exit(1);
    return;
}

if (forceUpdateNames)
    Console.WriteLine("Updating names");

// Known namespaces
var namespaces = new Dictionary<string, NamespaceDef>{};

NamespaceDef AddNamespace(string ns)
{
    if (namespaces.ContainsKey(ns))
    {
        return namespaces[ns];
    }

    var def = new NamespaceDef
    {
        Namespace = ns,
    };

    namespaces.Add(ns, def);

    return def;
}

// Find namespaces from offers
if (!skipOffers)
{
    Console.WriteLine("Reading offers");

    var offersText = File.ReadAllText(Path.Combine(dirOffers, "namespaces.json"));
    var typeOffers = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(offersText);

    foreach (var i in typeOffers!)
    {
        var def = AddNamespace(i.Key);

        var item = await GetOrCreate(gameIndex, i.Key).ConfigureAwait(false);
        var requiresSave = false;

        List<EpicOffer> offers = new();
        EpicOffer? baseAppOffer = null;

        foreach (var v in i.Value)
        {
            var fileName = Path.Combine(dirOffers, "offers", $"{v}.json");

            try
            {
                var fileContent = await File.ReadAllTextAsync(fileName);
                var epicOffer = JsonSerializer.Deserialize<EpicOffer>(fileContent);

                if (epicOffer == null)
                    continue;

                offers.Add(epicOffer);

                if (epicOffer.categories.Any(x => x.path == "games/edition/base"))
                {
                    baseAppOffer ??= epicOffer;
                }
            }
            catch (Exception)
            {
            }
        }

        if (baseAppOffer != null)
        {
            if (item.Name != baseAppOffer.title)
            {
                requiresSave = true;
                item.Name = baseAppOffer.title;

                await gameIndex.RenameItem(item).ConfigureAwait(false);
            }
        }

        if (requiresSave)
        {
            await gameIndex.SaveItem(item).ConfigureAwait(false);
        }
    }
}

// Find namespaces from items
if (!skipItems)
{
    Console.WriteLine("Reading items");

    var itemsText = File.ReadAllText(Path.Combine(dirItems, "namespaces.json"));
    var typeItems = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(itemsText);

    foreach (var i in typeItems!)
    {
        AddNamespace(i.Key);

        var item = await GetOrCreate(gameIndex, i.Key).ConfigureAwait(false);

        List<EpicItem> items = new();

        foreach (var v in i.Value)
        {
            var fileName = Path.Combine(dirItems, "items", $"{v}.json");

            try
            {
                var fileContent = await File.ReadAllTextAsync(fileName);
                var epicItem = JsonSerializer.Deserialize<EpicItem>(fileContent);

                if (epicItem == null)
                    continue;

                items.Add(epicItem);
            }
            catch (Exception)
            {
            }
        }
    }
}

var c = 0;
var total = namespaces.Count;

async ValueTask<GameDbItem> GetOrCreate(JsonIndexDb<GameDbItem> gameIndex, string ns, string? name = null)
{
    var item = await gameIndex.GetItemByKey(ns).ConfigureAwait(false);

    if (item == null)
    {
        item = new GameDbItem
        {
            ID = ns,
            Name = name,
        };

        await gameIndex.SaveItem(item).ConfigureAwait(false);
    }

    return item;
}

/**
 * Update rating by namespace
 */
async ValueTask UpdateRating(JsonIndexDb<GameDbItem> gameIndex, NamespaceDef ns, CancellationToken ct)
{
    Interlocked.Increment(ref c);
    Console.Write($"\rUpdating {c} / {total}");

    var dbItem = await GetOrCreate(gameIndex, ns.Namespace).ConfigureAwait(false);
    var requiresSave = false;

    try
    {
        var pi = await EpicApi.GetProductResult(ns.Namespace).ConfigureAwait(false);

        if (pi != null)
        {
            requiresSave = true;

            if (dbItem.FirstSeen == null)
            {
                dbItem.FirstSeen = DateTimeOffset.UtcNow;
            }

            var ratingChanged = dbItem.Rating != pi.averageRating;

            dbItem.ProductSlug = ns.ProductSlug;
            dbItem.Rating = pi.averageRating;

            if (pi.pollResult != null)
            {
                var topAward = pi.pollResult.MaxBy(x => x.total ?? 0);

                var NumberOfAwards = pi.pollResult.Sum(x => x.total ?? 0);
                var NumberOfAwardsMax = topAward?.total ?? 0;

                if (NumberOfAwards != dbItem.NumberOfAwards || NumberOfAwardsMax != dbItem.NumberOfAwardsMax)
                {
                    ratingChanged = true;
                }

                dbItem.NumberOfAwards = NumberOfAwards;
                dbItem.NumberOfAwardsMax = NumberOfAwardsMax;
                dbItem.MaxAwardTitle = topAward?.localizations.resultTitle;

                foreach (var pr in pi.pollResult.OrderByDescending(x => x.total))
                {
                    var text = $"{pr.localizations.resultText} {pr.localizations.resultTitle}";
                    var current = dbItem.Tags.FirstOrDefault(x => x.Text == text);

                    if (current != null)
                    {
                        current.Prefix = pr.localizations.resultText;
                        current.Type = pr.localizations.resultTitle;
                        current.Count = pr.total ?? 0;
                    }
                    else
                    {
                        dbItem.Tags.Add(new GameDbItemTag
                        {
                            Text = text,
                            Prefix = pr.localizations.resultText,
                            Type = pr.localizations.resultTitle,
                            Count = pr.total ?? 0,
                        });
                    }
                }
            }
            else if (dbItem.NumberOfAwards != null)
            {
                dbItem.NumberOfAwards = 0;
                dbItem.NumberOfAwardsMax = 0;
            }

            if (ratingChanged)
            {
                if (dbItem.RatingHistory.Count == 0)
                {
                    dbItem.FirstSeenRating = DateTimeOffset.UtcNow;
                }

                dbItem.LastChanged = DateTimeOffset.UtcNow;
                dbItem.RatingHistory.Add(new GameDbItemRatingHistory
                {
                    Time = DateTimeOffset.UtcNow,
                    Rating = pi.averageRating,
                    NumberOfAwards = dbItem.NumberOfAwards,
                    NumberOfAwardsMax = dbItem.NumberOfAwardsMax,
                });
            }
        }

        var ach = await EpicApi.QueryAchievements(ns.Namespace);

        if (ach?.productId != null)
        {
            var baseSet = ach?.achievementSets?.FirstOrDefault(x => x.isBase);

            if (baseSet != null)
            {
                var isChanged = dbItem.EOS_Progressed != baseSet.numProgressed || dbItem.EOS_Completed != baseSet.numCompleted;

                dbItem.EOS_Progressed = baseSet.numProgressed;
                dbItem.EOS_Completed = baseSet.numCompleted;
                dbItem.EOS_Completed_Percentage = baseSet.numProgressed > 0 ? Math.Round((double) baseSet.numCompleted / baseSet.numProgressed * 100, 2) : 0;

                if (isChanged)
                {
                    dbItem.EosHistory.Add(new GameDbItemEOSHistory
                    {
                        Time = DateTimeOffset.UtcNow,
                        NumProgressed = baseSet.numProgressed,
                        NumCompleted = baseSet.numCompleted,
                    });
                }

                if (dbItem.FirstSeenAchievements == null)
                {
                    dbItem.FirstSeenAchievements = DateTimeOffset.UtcNow;
                }
            }
        }
    }
    catch (GraphQLHttpRequestException)
    {
        Console.WriteLine("Ratelimited...?");
        await Task.Delay(10000, ct);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] {ex}");
        // ??
    }

    if (requiresSave)
    {
        await gameIndex.SaveItem(dbItem).ConfigureAwait(false);
    }

    await Task.Delay(forceUpdateNames ? 200 : 75, ct);
}

// Fetch ratings
if (!skipRatingsUpdate)
{
    Console.WriteLine("Updating rating");

    await Parallel.ForEachAsync(
        namespaces.Values, 
        new ParallelOptions {  MaxDegreeOfParallelism = forceUpdateNames ? 2 : 3 },
        (ns, ct) => UpdateRating(gameIndex, ns, ct)
    );
}

// Create combined files
var output = await gameIndex.GetAllItems().ConfigureAwait(false);

// Only games with rating
var filteredList = output.Where(x => x.Rating != null).ToList();

// Rating Ranking
MarkdownHelpers.RankItems(
    filteredList.OrderByDescending(x => x.Rating),
    x => x.Rating ?? 0,
    (x, rank) => x.Ranking_Rating = rank
);

// Popularity/Award Ranking
MarkdownHelpers.RankItems(
    filteredList.OrderByDescending(x => x.NumberOfAwardsMax),
    x => x.NumberOfAwardsMax ?? 0,
    (x, rank) => x.Ranking_Popularity = rank
);

MarkdownHelpers.RankItems(
    filteredList.OrderByDescending(x => x.NumberOfAwards),
    x => x.NumberOfAwards ?? 0,
    (x, rank) => x.Ranking_PopularitySum = rank
);

// EOS Achievements
MarkdownHelpers.RankItems(
    filteredList.OrderByDescending(x => x.EOS_Progressed),
    x => x.EOS_Progressed,
    (x, rank) => x.Ranking_EOS_Progress = rank
);

MarkdownHelpers.RankItems(
    filteredList.OrderByDescending(x => x.EOS_Completed_Percentage),
    x => x.EOS_Completed_Percentage,
    (x, rank) => x.Ranking_EOS_Completed = rank
);

foreach (var item in filteredList)
{
    await gameIndex.SaveItem(item).ConfigureAwait(false);
}

foreach (var item in output)
{
    var name = gameIndex.Files[item.ID] + ".md";
    var fullName = Path.Combine(dirGames, name);

    Directory.CreateDirectory(Path.GetDirectoryName(fullName)!);

    var page = MarkdownHelpers.BuildMarkdownGamePage(item);

    await File.WriteAllTextAsync(fullName, page);
}

string GamesLink(GameDbItem item)
{
    return "games/" + gameIndex?.Files[item.ID].Replace(@"\", "/") + ".md";
}


var nameTable = new MarkdownTable<GameDbItem>()
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Rating", x => MarkdownHelpers.FormatRating(x.Rating))
    .AddColumn("Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Rating))
    .AddColumn("Awards", x => MarkdownHelpers.FormatVotes(x.NumberOfAwardsMax))
    .AddColumn("Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Popularity));

var ratingTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_Rating))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Rating", x => MarkdownHelpers.FormatRating(x.Rating))
    .AddColumn("Awards", x => MarkdownHelpers.FormatVotes(x.NumberOfAwardsMax))
    .AddColumn("Popularity Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Popularity));

var awardsTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_Popularity))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Awards", x => MarkdownHelpers.FormatVotes(x.NumberOfAwardsMax))
    .AddColumn("Rating", x => MarkdownHelpers.FormatRating(x.Rating))
    .AddColumn("Rating Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Rating));

var awardsSumTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_PopularitySum))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Awards", x => MarkdownHelpers.FormatVotes(x.NumberOfAwards))
    .AddColumn("Rating", x => MarkdownHelpers.FormatRating(x.Rating))
    .AddColumn("Rating Ranking", x => MarkdownHelpers.FormatRanking(x.Ranking_Rating));

var eosTable = new MarkdownTable<GameDbItem>()
    .AddColumn("#", x => MarkdownHelpers.FormatRanking(x.Ranking_EOS_Progress))
    .AddColumn("Game", x => $"[{x.Name}]({GamesLink(x)})")
    .AddColumn("Progressed", x => MarkdownHelpers.FormatVotes(x.EOS_Progressed))
    .AddColumn("Completed", x => MarkdownHelpers.FormatVotes(x.EOS_Completed));

// Markdown
await File.WriteAllTextAsync(
    Path.Combine(path, "by_name.md"),
    nameTable.FormatTable(filteredList.OrderBy(x => x.Name))
);
await File.WriteAllTextAsync(
    Path.Combine(path, "by_rating.md"), 
    ratingTable.FormatTable(filteredList.OrderByDescending(x => x.Rating).ThenBy(x => x.Name))
);
await File.WriteAllTextAsync(
    Path.Combine(path, "by_awards.md"),
    awardsTable.FormatTable(filteredList.Where(x => x.NumberOfAwardsMax > 0).OrderByDescending(x => x.NumberOfAwardsMax).ThenBy(x => x.Name))
);
await File.WriteAllTextAsync(
    Path.Combine(path, "by_awards_sum.md"),
    awardsSumTable.FormatTable(filteredList.Where(x => x.NumberOfAwards > 0).OrderByDescending(x => x.NumberOfAwards).ThenBy(x => x.Name))
);
await File.WriteAllTextAsync(
    Path.Combine(path, "new_games.md"),
    nameTable.FormatTable(filteredList.Where(x => x.FirstSeen != null && ratingsCutOffNew < x.FirstSeen).OrderByDescending(x => x.FirstSeen).ThenBy(x => x.Name))
);

await File.WriteAllTextAsync(
    Path.Combine(path, "eos_achievers.md"),
    eosTable.FormatTable(filteredList.OrderByDescending(x => x.EOS_Progressed).ThenBy(x => x.Name))
);

// Stats
await File.WriteAllTextAsync(
    Path.Combine(path, "stats.md"),
    MarkdownHelpers.BuildMarkdownStats(filteredList, output)
);